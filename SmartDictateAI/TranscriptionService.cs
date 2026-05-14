// TranscriptionService.cs
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text; // For StringBuilder
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using LLama; // Core LLamaSharp
using LLama.Common;
using System.Xml; // For ModelParams, InferenceParams
using System.Reflection;
using WebRtcVadSharp; // NEW: use the library directly

// using LLama.Abstractions; // If using interfaces like ILLamaExecutor
// using WhisperNetConsoleDemo; // If AppSettings is in a separate file in this namespace

namespace WhisperNetConsoleDemo
    {
    public class TranscriptionService : IDisposable // Consider IAsyncDisposable
        {
        // --- Events for UI Updates ---
        public event Action<string, string>? SegmentTranscribed; // (timestampedText, rawText)
        public event Action<string>? FullTranscriptionReady;
        public event Action<string>? DebugMessageGenerated;
        public event Action<bool>? RecordingStateChanged;
        public event Action? SettingsUpdated;
        public event Action? ProcessingStarted;
        public event Action? ProcessingFinished;

        // --- State Fields ---
        private bool _hasSpeechInCurrentChunk = false;
        private readonly object _audioStreamLock = new object();
        private bool isRecording = false;
        private WaveInEvent? waveSource = null;
        private MemoryStream? currentAudioChunkStream = null;
        private WaveFileWriter? chunkWaveFile = null;
        private DateTime chunkStartTime = DateTime.MinValue;
        private DateTime lastSpeechTime = DateTime.MinValue;
        private bool silenceDetectedRecently = false;
        private bool activelyProcessingChunk = false;
        private Task? currentTranscriptionTask = null;
        private readonly List<string> currentSessionTranscribedText = new List<string>();
        public string LastRawFilteredText { get; private set; } = string.Empty;
        public string LastLLMProcessedText { get; set; } = string.Empty;
        public bool WasLastProcessingWithLLM { get; private set; } = false; // To know if LLM text is valid

        // --- LLamaSharp Specific ---
        private LLamaWeights? llmModelWeights = null;
        private LLamaContext? llmContext = null;
        // Using StatelessExecutor as it's simpler for this kind of one-off processing per chunk
        // If you needed conversational context, ChatSession or InteractiveExecutor would be better.
        private StatelessExecutor? llmExecutor = null;

        // --- Whisper & App Settings ---
        private string currentWhisperModelPath; // Renamed for clarity
        private WhisperFactory? whisperFactoryInstance = null;
        private readonly WaveFormat waveFormatForWhisper = new WaveFormat(16000, 16, 1);

        public AppSettings Settings { get; private set; } = new AppSettings();
        private readonly string appSettingsFilePath = "appsettings.json";

        private TaskCompletionSource<bool>? _currentStopProcessingTcs;

        public bool IsDictationModeActive { get; private set; } = false;
        private TaskCompletionSource<bool>? _dictationModeStopSignal;

        // --- WebRtcVadSharp integration ---
        private WebRtcVad? _vad;
        private List<byte> _vadAudioBuffer = new List<byte>();
        private const int VAD_FRAME_BYTES = 640;

        public TranscriptionService()
        {
            LoadAppSettings();
            currentWhisperModelPath = Settings.ModelFilePath;

            try
            {
                _vad = new WebRtcVad() { OperatingMode = (OperatingMode)Settings.VadMode };
                OnDebugMessage($"VAD initialized in mode {Settings.VadMode}.");
            }
            catch (Exception ex)
            {
                // LOG THE FULL EXCEPTION HERE TO SEE THE ERROR
                OnDebugMessage($"VAD initialization failed: {ex.Message}");
                if (ex.InnerException != null)
                    OnDebugMessage($"Inner Error: {ex.InnerException.Message}");

                _vad = null;
            }
        }

        private void OnDebugMessage(string message) => DebugMessageGenerated?.Invoke(message);
        private void OnSegmentTranscribed(string timestamped, string raw) => SegmentTranscribed?.Invoke(timestamped, raw);
        private void OnFullTranscriptionReady(string fullText) => FullTranscriptionReady?.Invoke(fullText);
        private void OnRecordingStateChanged(bool nowRecording) => RecordingStateChanged?.Invoke(nowRecording);
        private void OnSettingsUpdated() => SettingsUpdated?.Invoke();

        public void LoadAppSettings()
            {
            OnDebugMessage("Loading application settings...");
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(appSettingsFilePath, optional: true, reloadOnChange: false);
            IConfigurationRoot configurationRoot = builder.Build();
            var settingsSection = configurationRoot.GetSection("AppSettings");

            if (settingsSection.Exists())
                {
                settingsSection.Bind(Settings);
                OnDebugMessage($"Loaded Whisper Model: {Settings.ModelFilePath}, LLM Model: {Settings.LocalLLMModelPath}, VAD Mode: {Settings.VadMode}, Mic: {Settings.SelectedMicrophoneDevice}");
                }
            else
                {
                OnDebugMessage($"'{appSettingsFilePath}' not found. Using/Creating defaults.");
                SaveAppSettings();
                }
            currentWhisperModelPath = Settings.ModelFilePath;
            OnSettingsUpdated();
            }

        public void SaveAppSettings()
            {
            OnDebugMessage("Saving application settings...");
            try
                {
                Settings.ModelFilePath = currentWhisperModelPath; // Whisper model

                var configurationToSave = new
                    {
                    AppSettings = this.Settings
                    };
                string json = JsonSerializer.Serialize(configurationToSave, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(appSettingsFilePath, json);
                OnDebugMessage($"Settings saved to {Path.GetFullPath(appSettingsFilePath)}");
                OnSettingsUpdated();
                }
            catch (Exception ex) { OnDebugMessage($"Error saving app settings: {ex.Message}"); }
            }

        public async Task<bool> InitializeWhisperAsync()
            {
            if (whisperFactoryInstance != null)
                return true;
            if (string.IsNullOrWhiteSpace(currentWhisperModelPath) || !File.Exists(currentWhisperModelPath))
                {
                OnDebugMessage($"InitializeWhisperAsync: Whisper Model path invalid: {currentWhisperModelPath}");
                return false;
                }
            OnDebugMessage($"Initializing Whisper with model: {currentWhisperModelPath}");
            try
                {
                await DisposeWhisperResourcesAsync();
                whisperFactoryInstance = WhisperFactory.FromPath(currentWhisperModelPath);
                OnDebugMessage("WhisperFactory initialized.");
                return true;
                }
            catch (Exception ex)
                {
                OnDebugMessage($"FATAL: Could not initialize Whisper.net: {ex.Message}");
                whisperFactoryInstance = null;
                return false;
                }
            }
        private void SaveDebugAudioChunk(MemoryStream stream, string prefix = "chunk")
        {
            try
            {
                string directory = Path.Combine(Directory.GetCurrentDirectory(), "DebugAudio");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string filename = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.wav";
                string filepath = Path.Combine(directory, filename);

                // Save current position to restore it for Whisper
                long originalPosition = stream.Position;
                stream.Position = 0;

                using (var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }

                // Rewind for Whisper
                stream.Position = originalPosition;

                OnDebugMessage($"Saved debug audio: {filename}");
            }
            catch (Exception ex)
            {
                OnDebugMessage($"Failed to save debug audio: {ex.Message}");
            }
        }

        private bool InitializeLLM()
            {
            if (llmExecutor != null)
                return true; // Or check llmModelWeights && llmContext

            if (string.IsNullOrWhiteSpace(Settings.LocalLLMModelPath) || !File.Exists(Settings.LocalLLMModelPath))
                {
                OnDebugMessage($"LLM Initialize: LocalLLMModelPath invalid: {Settings.LocalLLMModelPath}");
                return false;
                }
            OnDebugMessage($"Initializing LLamaSharp with model: {Settings.LocalLLMModelPath}");
            try
                {
                DisposeLLMResourcesInternal(); // Dispose previous if any

                var parameters = new ModelParams(Settings.LocalLLMModelPath)
                    {
                    ContextSize = (uint)Settings.LLMContextSize,
                    GpuLayerCount = Settings.UseGpu ? 99 : 0 // 99 for all layers to GPU if possible
                    };
                llmModelWeights = LLamaWeights.LoadFromFile(parameters);
                // llmContext = llmModelWeights.CreateContext(parameters); // This was an older way for context
                // For StatelessExecutor, you don't always need to create a context separately this way
                // The executor can manage it or you pass parameters directly to it if needed.
                // Let's create the executor directly with weights and model params.
                llmExecutor = new StatelessExecutor(llmModelWeights, parameters);


                OnDebugMessage("LLamaSharp initialized successfully.");
                return true;
                }
            catch (Exception ex)
                {
                OnDebugMessage($"FATAL: Could not initialize LLamaSharp: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                DisposeLLMResourcesInternal(); // Ensure cleanup on failure
                return false;
                }
            }

        public async Task DisposeWhisperResourcesAsync()
            {
            OnDebugMessage("Disposing WhisperFactory (async).");
            whisperFactoryInstance?.Dispose();
            whisperFactoryInstance = null;
            await Task.CompletedTask;
            }

        private void DisposeLLMResourcesInternal() // Synchronous for now, can be made async if LLamaSharp uses IAsyncDisposable heavily
            {
            OnDebugMessage("Disposing LLamaSharp internal resources.");
            //llmExecutor?.Dispose(); // StatelessExecutor is IDisposable
            llmContext?.Dispose(); // If we were creating a context separately
            llmModelWeights?.Dispose();
            llmExecutor = null;
            llmContext = null;
            llmModelWeights = null;
            }

        public async Task DisposeLLMResourcesAsync()
            {
            OnDebugMessage("Disposing LLamaSharp resources (synchronously within async method for test)...");
            if (llmModelWeights != null || llmExecutor != null)
                {
                DisposeLLMResourcesInternal(); // Call directly
                OnDebugMessage("LLamaSharp resources disposed state updated.");
                }
            else
                {
                OnDebugMessage("LLamaSharp resources already null or not initialized for dispose.");
                }
            await Task.CompletedTask; // Keep it awaitable if needed elsewhere
            }


        private void StartNewChunk()
            {
            try
                {
                chunkWaveFile?.Dispose();
                }
            catch
                {
                // ignore disposal exceptions
                }

            currentAudioChunkStream = new MemoryStream();
            chunkWaveFile = new WaveFileWriter(currentAudioChunkStream, waveFormatForWhisper);
            chunkStartTime = DateTime.UtcNow;
            lastSpeechTime = DateTime.UtcNow;
            silenceDetectedRecently = false;

            // Clear VAD buffer when starting a new chunk so frames don't cross chunk boundaries unnecessarily
            _vadAudioBuffer.Clear();
            _hasSpeechInCurrentChunk = false;
        }
        private async void WaveSource_DataAvailable(object? sender, WaveInEventArgs e)
        {
            lock (_audioStreamLock)
            {
                if (!isRecording || chunkWaveFile == null || currentAudioChunkStream == null)
                    return;
                try
                {
                    chunkWaveFile.Write(e.Buffer, 0, e.BytesRecorded);
                }
                catch (ObjectDisposedException) { OnDebugMessage("DataAvailable - Write to disposed chunkWaveFile."); return; }
            }

            bool speechInSeg = false;

            try
            {
                if (_vad != null)
                {
                    _vadAudioBuffer.AddRange(e.Buffer.Take(e.BytesRecorded));

                    while (_vadAudioBuffer.Count >= VAD_FRAME_BYTES)
                    {
                        var rawFrame = _vadAudioBuffer.GetRange(0, VAD_FRAME_BYTES).ToArray();
                        _vadAudioBuffer.RemoveRange(0, VAD_FRAME_BYTES);

                        // --- SOFTWARE GAIN FOR VAD ---
                        // Convert bytes to 16-bit samples, amplify, and convert back
                        byte[] amplifiedFrame = new byte[VAD_FRAME_BYTES];
                        for (int i = 0; i < VAD_FRAME_BYTES; i += 2)
                        {
                            short sample = BitConverter.ToInt16(rawFrame, i);
                            // Multiply and clamp to prevent digital clipping
                            int boosted = (int)(sample * Settings.VadGainMultiplier);
                            short clamped = (short)Math.Clamp(boosted, short.MinValue, short.MaxValue);

                            byte[] bytes = BitConverter.GetBytes(clamped);
                            amplifiedFrame[i] = bytes[0];
                            amplifiedFrame[i + 1] = bytes[1];
                        }

                        // Inside WaveSource_DataAvailable, in the VAD while loop:
                        int maxSample = 0;
                        for (int i = 0; i < VAD_FRAME_BYTES; i += 2)
                        {
                            short s = BitConverter.ToInt16(rawFrame, i);
                            int absSample = Math.Abs((int)s);
                            if (absSample > maxSample) maxSample = absSample;
                        }
                        // Log the peak once every second or so to avoid spam
                        //if (DateTime.UtcNow.Millisecond < 50)
                        //{
                        //   OnDebugMessage($"Mic Peak: {maxSample} (Boosted: {maxSample * VAD_GAIN_MULTIPLIER})");
                        //}

                        try
                        {
                            // Use the AMPLIFIED frame for the VAD check
                            if (_vad != null && _vad.HasSpeech(amplifiedFrame, SampleRate.Is16kHz, FrameLength.Is20ms))
                            {
                                if (!_hasSpeechInCurrentChunk)
                                {
                                    OnDebugMessage(">>> VAD TRIGGERED: Speech Detected with Gain! <<<");
                                }
                                lastSpeechTime = DateTime.UtcNow;
                                silenceDetectedRecently = false;
                                speechInSeg = true;
                                _hasSpeechInCurrentChunk = true;
                            }
                        }
                        catch (Exception ex) { OnDebugMessage($"VAD processing error: {ex.Message}"); }
                    }
                }
                else
                {
                    // Fallback: If VAD failed to initialize, assume speech so we don't discard everything
                    speechInSeg = true;
                    _hasSpeechInCurrentChunk = true;
                }
            }
            catch (Exception ex) { OnDebugMessage($"VAD buffer error: {ex.Message}"); }

            double currentSilenceThresholdSeconds = this.IsDictationModeActive ?
                                               Settings.DictationSilenceThresholdSeconds :
                                               Settings.NormalSilenceThresholdSeconds;

            if (!speechInSeg && _hasSpeechInCurrentChunk)
            {
                if (currentAudioChunkStream != null && currentAudioChunkStream.Length > 0 &&
                   (DateTime.UtcNow - lastSpeechTime) > TimeSpan.FromSeconds(currentSilenceThresholdSeconds))
                {
                    silenceDetectedRecently = true;
                }
            }

            TimeSpan chunkDur = DateTime.UtcNow - chunkStartTime;
            bool process = false;
            bool discardChunk = false;

            double currentMaxChunkDurationSeconds = this.IsDictationModeActive ?
                                                Settings.DictationMaxChunkDurationSeconds :
                                                Settings.NormalMaxChunkDurationSeconds;

            if (currentAudioChunkStream.Length > (waveFormatForWhisper.AverageBytesPerSecond / 2))
            {
                if (chunkDur >= TimeSpan.FromSeconds(currentMaxChunkDurationSeconds))
                {
                    if (_hasSpeechInCurrentChunk)
                    {
                        OnDebugMessage($"Max chunk duration ({currentMaxChunkDurationSeconds}s) reached.");
                        process = true;
                    }
                    else
                    {
                        discardChunk = true;
                    }
                }
                else if (silenceDetectedRecently)
                {
                    OnDebugMessage("Silence detected after speech, processing chunk.");
                    process = true;
                }
                else if (!_hasSpeechInCurrentChunk && chunkDur >= TimeSpan.FromSeconds(currentSilenceThresholdSeconds))
                {
                    // Discard the chunk early if it's just silence, to avoid building up a long silent prefix
                    // which would cause speech to be cut off if it starts near the max chunk duration limit.
                    discardChunk = true;
                }
            }

            if (!activelyProcessingChunk && (process || discardChunk))
            {
                if (process)
                {
                    ProcessingStarted?.Invoke();
                    activelyProcessingChunk = true;
                }

                MemoryStream? streamToSend = null;
                WaveFileWriter? capturedChunkFile;
                MemoryStream? capturedAudioChunkStream;

                var nextAudioChunkStream = new MemoryStream();
                var nextChunkWaveFile = new WaveFileWriter(nextAudioChunkStream, waveFormatForWhisper);

                lock (_audioStreamLock)
                {
                    capturedChunkFile = chunkWaveFile;
                    capturedAudioChunkStream = currentAudioChunkStream;

                    currentAudioChunkStream = nextAudioChunkStream;
                    chunkWaveFile = nextChunkWaveFile;

                    chunkStartTime = DateTime.UtcNow;
                    lastSpeechTime = DateTime.UtcNow;
                    silenceDetectedRecently = false;
                    _hasSpeechInCurrentChunk = false;
                }

                if (process)
                {
                    try
                    {
                        capturedChunkFile?.Flush();
                        if (capturedAudioChunkStream != null && capturedAudioChunkStream.Length > 0)
                        {
                            capturedAudioChunkStream.Position = 0;
                            streamToSend = new MemoryStream();
                            await capturedAudioChunkStream.CopyToAsync(streamToSend);
                            streamToSend.Position = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        OnDebugMessage($"Error prep stream: {ex.Message}");
                        activelyProcessingChunk = false;
                        streamToSend?.Dispose();
                    }
                    finally
                    {
                        capturedChunkFile?.Dispose();
                        capturedAudioChunkStream?.Dispose();
                    }

                    if (streamToSend != null && streamToSend.Length > 0)
                    {
                        if (Settings.ShowDebugMessages)
                        {
                            SaveDebugAudioChunk(streamToSend, "chunk");
                        }
                        currentTranscriptionTask = TranscribeAudioChunkAsync(streamToSend, this.IsDictationModeActive);
                        try
                        {
                            await currentTranscriptionTask;
                        }
                        catch (Exception ex)
                        {
                            OnDebugMessage($"Err transcription task: {ex.Message}");
                        }
                        finally
                        {
                            currentTranscriptionTask = null;
                            activelyProcessingChunk = false;
                            ProcessingFinished?.Invoke();
                        }
                    }
                    else
                    {
                        activelyProcessingChunk = false;
                        streamToSend?.Dispose();
                        ProcessingFinished?.Invoke();
                    }
                }
                else if (discardChunk)
                {
                    // SAVE THE AUDIO IT THREW AWAY SO WE CAN LISTEN TO IT
                    OnDebugMessage("Discarding chunk (No speech detected by VAD).");
                    try
                    {
                        capturedChunkFile?.Flush();
                        if (Settings.ShowDebugMessages && capturedAudioChunkStream != null)
                        {
                            SaveDebugAudioChunk(capturedAudioChunkStream, "discarded");
                        }
                    }
                    catch { }
                    finally
                    {
                        capturedChunkFile?.Dispose();
                        capturedAudioChunkStream?.Dispose();
                    }
                }
            }
        }
        private async void WaveSource_RecordingStopped(object? sender, StoppedEventArgs e)
            {
            OnDebugMessage("WaveSource_RecordingStopped - Event ENTERED.");
            bool wasActuallyRecording = this.isRecording; // Capture current state
            this.isRecording = false;       // Set recording state to false immediately
            OnRecordingStateChanged(false); // Notify UI

            // Capture the stream and writer that were active for the very last segment of audio
            MemoryStream? finalActiveStream = this.currentAudioChunkStream;
            WaveFileWriter? finalFile = this.chunkWaveFile;

            // Null out class fields so new recordings (if any started immediately after, though unlikely)
            // or any stray DataAvailable calls don't use these disposed/processed instances.
            this.currentAudioChunkStream = null;
            this.chunkWaveFile = null;

            // Dispose the NAudio WaveInEvent source first
            if (sender is WaveInEvent ws)
                {
                OnDebugMessage("WaveSource_RecordingStopped - Unsubscribing NAudio events.");
                ws.DataAvailable -= WaveSource_DataAvailable;
                ws.RecordingStopped -= WaveSource_RecordingStopped; // Unsubscribe from itself
                try
                    {
                    ws.Dispose();
                    OnDebugMessage("WaveSource_RecordingStopped - WaveInEvent sender disposed.");
                    }
                catch (Exception ex) { OnDebugMessage($"Err disposing WaveInEvent: {ex.Message}"); }
                }
            // Ensure the class field for waveSource is also nulled if it was the sender
            if (ReferenceEquals(sender, this.waveSource))
                {
                this.waveSource = null;
                }

            MemoryStream? streamToTranscribeThisFinalChunk = null;
            if (wasActuallyRecording && finalFile != null && finalActiveStream != null)
                {
                OnDebugMessage("WaveSource_RecordingStopped - Processing final audio chunk.");
                try
                    {
                    finalFile.Flush(); // Ensure all buffered data is written to finalActiveStream
                    OnDebugMessage($"WaveSource_RecordingStopped - Final active stream length after flush: {finalActiveStream.Length}");

                    // Process even if slightly shorter than normal chunks, but not if effectively empty
                    // Use a very small minimum, e.g., 0.05 seconds of audio data
                    if (_hasSpeechInCurrentChunk && finalActiveStream.Length > (this.waveFormatForWhisper.AverageBytesPerSecond / 20))
                        {
                        finalActiveStream.Position = 0; // Rewind the stream to be read from the beginning
                        streamToTranscribeThisFinalChunk = new MemoryStream();
                        await finalActiveStream.CopyToAsync(streamToTranscribeThisFinalChunk);
                        streamToTranscribeThisFinalChunk.Position = 0; // Rewind the new stream for transcription
                        OnDebugMessage($"WaveSource_RecordingStopped - Copied final chunk of {streamToTranscribeThisFinalChunk.Length} bytes for transcription.");
                        }
                    else
                        {
                        OnDebugMessage("WaveSource_RecordingStopped - Final active stream was too short or contained no speech.");
                        }
                    }
                catch (Exception ex) { OnDebugMessage($"Err flush/copy final chunk: {ex.Message}"); }
                finally
                    {
                    // IMPORTANT: Dispose the WaveFileWriter. This will also dispose the
                    // finalActiveStream it was writing to (MemoryStream in this case).
                    OnDebugMessage("WaveSource_RecordingStopped - Disposing final WaveFileWriter (and its stream).");
                    try
                        {
                        finalFile.Dispose();
                        }
                    catch (Exception ex) { OnDebugMessage($"Err disposing finalFile: {ex.Message}"); }
                    }
                }
            // If finalFile was null (e.g., recording stopped before first DataAvailable after StartNewChunk),
            // but finalActiveStream (the MemoryStream) existed, ensure it's disposed.
            else if (finalActiveStream != null && finalActiveStream.CanRead)
                {
                OnDebugMessage("WaveSource_RecordingStopped - Disposing lingering finalActiveStream.");
                try
                    {
                    finalActiveStream.Dispose();
                    }
                catch { }
                }


            Task? transcriptionOfThisFinalChunkTask = null;
            if (wasActuallyRecording && streamToTranscribeThisFinalChunk != null && streamToTranscribeThisFinalChunk.Length > 0)
                {
                OnDebugMessage("WaveSource_RecordingStopped - Starting transcription for the final chunk.");
                // No need to set activelyProcessingChunk = true here, as this is the end of a session,
                // and no new DataAvailable events for this waveSource should be coming.
                if (Settings.ShowDebugMessages)
                {
                    SaveDebugAudioChunk(streamToTranscribeThisFinalChunk, "final_chunk");
                }
                transcriptionOfThisFinalChunkTask = TranscribeAudioChunkAsync(streamToTranscribeThisFinalChunk, this.IsDictationModeActive);
                // currentTranscriptionTask = transcriptionOfThisFinalChunkTask; // Update global tracker if needed by Form1's Q

                try
                    {
                    await transcriptionOfThisFinalChunkTask; // Wait for THIS final transcription
                    OnDebugMessage("WaveSource_RecordingStopped - Transcription of this final chunk completed.");
                    }
                catch (Exception ex) { OnDebugMessage($"Err awaiting FINAL transcription task: {ex.Message}"); }
                // No finally block to reset activelyProcessingChunk here as it's the end of the line for this path
                }
            else if (wasActuallyRecording)
                {
                OnDebugMessage("WaveSource_RecordingStopped - No substantial final audio chunk to transcribe.");
                }

            // If streamToTranscribeThisFinalChunk was created but not used (e.g. too short)
            if (transcriptionOfThisFinalChunkTask == null && streamToTranscribeThisFinalChunk != null && streamToTranscribeThisFinalChunk.CanRead)
                {
                OnDebugMessage("WaveSource_RecordingStopped - Disposing unused streamToTranscribeThisFinalChunk.");
                try
                    {
                    streamToTranscribeThisFinalChunk.Dispose();
                    }
                catch { }
                }

            // --- Display Full Text (from all chunks in the session) and Signal Completion ---
            if (wasActuallyRecording)
                {
                string rawFilteredFullText;
                lock (this.currentSessionTranscribedText) // Access with lock
                    {
                    var knownPlaceholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "[BLANK_AUDIO]", "(silence)", "[ Silence ]", "...", "[INAUDIBLE]", "[MUSIC PLAYING]", "[SOUND]", "[CLICK]" };
                    var segmentsToUse = this.currentSessionTranscribedText
                        .Select(segment => segment.Trim())
                        .Where(trimmedSegment =>
                            !string.IsNullOrWhiteSpace(trimmedSegment) &&
                            !knownPlaceholders.Contains(trimmedSegment) &&
                            !(trimmedSegment.StartsWith("[") && trimmedSegment.EndsWith("]") && trimmedSegment.Length <= 25))
                        .ToList();
                    rawFilteredFullText = string.Join(" ", segmentsToUse).Trim();
                    }

                this.LastRawFilteredText = rawFilteredFullText; // Store for copy button
                this.WasLastProcessingWithLLM = false;
                this.LastLLMProcessedText = string.Empty;

                string finalTextToDisplay = $"--- RAW Transcription ---{Environment.NewLine}{rawFilteredFullText}";

                if (this.Settings.ProcessWithLLM && !string.IsNullOrWhiteSpace(rawFilteredFullText))
                    {
                    OnDebugMessage("WaveSource_RecordingStopped: Attempting LLM processing for the full text...");
                    string refinedText = await ProcessTextWithLLMAsync(rawFilteredFullText); // Assuming ProcessTextWithLLMAsync is instance method
                    this.LastLLMProcessedText = refinedText;
                    this.WasLastProcessingWithLLM = true;
                    OnDebugMessage("WaveSource_RecordingStopped: LLM processing complete.");
                    finalTextToDisplay += $"{Environment.NewLine}{Environment.NewLine}--- LLM Refined ---{Environment.NewLine}{refinedText}";
                    }
                else if (this.Settings.ProcessWithLLM && string.IsNullOrWhiteSpace(rawFilteredFullText))
                    {
                    OnDebugMessage("WaveSource_RecordingStopped: Full text is empty after filtering, skipping LLM.");
                    finalTextToDisplay += $"{Environment.NewLine}{Environment.NewLine}[No speech detected to refine with LLM]";
                    }

                OnFullTranscriptionReady(finalTextToDisplay); // Raise event with combined display text
                }

            if (e.Exception != null)
                {
                OnDebugMessage($"NAudio stop exception: {e.Exception.Message}");
                }

            this._currentStopProcessingTcs?.TrySetResult(true); // Signal completion to StopRecording() caller
            this._dictationModeStopSignal?.TrySetResult(true); // Also signal dictation mode stop
            OnDebugMessage("WaveSource_RecordingStopped - Signalled _currentStopProcessingTcs. Event EXITED.");
            // If not exiting, UI needs to be re-enabled / instructions printed
            // This is handled by MainForm awaiting the task from StopRecording() and then calling its own UI updates
            }
        private async Task TranscribeAudioChunkAsync(Stream audioStream, bool isDictationModeChunk)
            {
            OnDebugMessage($"TranscribeAudioChunkAsync - Stream length: {audioStream.Length}");
            if (whisperFactoryInstance == null)
                {
                OnDebugMessage("ERROR: WhisperFactory not initialized.");
                audioStream.Dispose();
                return;
                }
            if (audioStream.Length < (waveFormatForWhisper.AverageBytesPerSecond / 10))
                {
                OnDebugMessage("Audio chunk too short.");
                audioStream.Dispose();
                return;
                }

            try
                {
                await Task.Yield();
                OnDebugMessage("Processing audio chunk with Whisper...");
                List<string> chunkSegmentsRaw = new List<string>();

                string promptText = string.Empty;
                if (Settings.MaintainContextAcrossChunks)
                {
                    lock (currentSessionTranscribedText)
                    {
                        if (currentSessionTranscribedText.Count > 0)
                        {
                            // Grab the last few phrases to use as context for the new chunk
                            promptText = string.Join(" ", currentSessionTranscribedText.TakeLast(4));
                            if (promptText.Length > 200) promptText = promptText.Substring(promptText.Length - 200);
                        }
                    }
                }

                var builder = whisperFactoryInstance.CreateBuilder()
                    .WithLanguage("auto")
                    .WithNoSpeechThreshold(0.6f);

                if (!string.IsNullOrWhiteSpace(promptText))
                    builder = builder.WithPrompt(promptText);

                using var chunkProcessor = builder.Build();

                await foreach (var segment in chunkProcessor.ProcessAsync(audioStream))
                    {
                    string timestampedText = $"[{segment.Start.TotalSeconds:F2}s -> {segment.End.TotalSeconds:F2}s]: {segment.Text}";
                    string rawText = segment.Text.Trim();
                    // Normal mode: accumulate for full transcription display
                    OnSegmentTranscribed(timestampedText, rawText); // For real-time UI update
                    chunkSegmentsRaw.Add(rawText);
                    }
                if (chunkSegmentsRaw.Count > 0)
                    lock (currentSessionTranscribedText)
                        {
                        currentSessionTranscribedText.AddRange(chunkSegmentsRaw);
                        }

                }
            catch (Exception ex)
                {
                OnDebugMessage($"Transcription error in chunk: {ex.Message}");
                }
            finally
                {
                await audioStream.DisposeAsync();
                }
            }

        public async Task<string> ProcessTextWithLLMAsync(string inputText, string setSystemPrompt = "", string setUserPrompt = "")
        {
            if (string.IsNullOrWhiteSpace(inputText))
                return inputText;
            if (!Settings.ProcessWithLLM)
                return inputText;
            var systemPrompt = (setSystemPrompt == "") ? Settings.LLMSystemPrompt : setSystemPrompt;
            var userPrompt = (setUserPrompt == "") ? Settings.LLMUserPrompt : setUserPrompt;
            if (llmExecutor == null)
                {
                if (!InitializeLLM())
                    {
                    OnDebugMessage("LLM could not be initialized. Skipping LLM processing.");
                    return inputText;
                    }
                if (llmExecutor == null)
                    {
                    OnDebugMessage("LLM Executor still null after init attempt. Skipping.");
                    return inputText;
                    } // Should not happen if InitializeLLM returns true
                }

            OnDebugMessage("Sending text to LLamaSharp for processing..." + inputText);
            var outputBuffer = new StringBuilder();
            try
                {

                string fullPrompt =
                    $"<|im_start|>system \n{systemPrompt}<|im_end|>\n" +
                    $"<|im_start|>user \n{userPrompt}\n\n{inputText}<|im_end|>\n" +
                    $"<|im_start|>assistant \n";
                uint actualSeedToUse;
                if (Settings.LLMSeed == 0)
                    {
                    // Generate a random seed if setting is 0
                    actualSeedToUse = (uint)new Random().Next(); // Or a more robust RNG if needed
                    OnDebugMessage($"LLMSeed was 0, generated random seed for this inference: {actualSeedToUse}");
                    }
                else
                    {
                    actualSeedToUse = (uint)Settings.LLMSeed;
                    OnDebugMessage($"Using LLMSeed from settings: {actualSeedToUse}");
                    }
                var inferenceParams = new InferenceParams()
                    {
                    AntiPrompts = Settings.LLMAntiPrompts,
                    MaxTokens = Settings.LLMMaxOutputTokens,
                    SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline() // other pipelines, including custom ones, are possible
                        {
                        Seed = actualSeedToUse, // Pass the generated or settings-based seed
                        Temperature = Settings.LLMTemperature, // Temperature also goes here
                        }
                    // Consider adding other parameters like TopK, TopP, MinP, RepeatPenalty from Settings
                    // PenalizeRepeatLastNElements = 64, // Example
                    // PenaltyRepeat = 1.1f,            // Example
                    };
                OnDebugMessage("\nfullPrompt " + fullPrompt);
                OnDebugMessage("\ninferenceParams" + inferenceParams.ToString());

                await foreach (var textPart in llmExecutor.InferAsync(fullPrompt, inferenceParams))
                {
                    outputBuffer.Append(textPart);
                }

                // Clean up any leaked stop tokens from LLamaSharp's stream
                string finalResult = outputBuffer.ToString().Trim();
                var tagsToStrip = Settings.LLMAntiPrompts;

                foreach (var tag in tagsToStrip)
                {
                    finalResult = finalResult.Replace(tag, string.Empty).Trim();
                }

                OnDebugMessage("LLamaSharp processing successful. " + finalResult);
                return finalResult;
            }
            catch (Exception ex)
                {
                OnDebugMessage($"Generic error during LLamaSharp processing: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                return inputText;
                }
            }

        // NEW: Update VAD mode at runtime
        public void SetVadMode(int mode)
            {
            try
                {
                if (mode < 0) mode = 0;
                if (mode > 3) mode = 3;
                Settings.VadMode = mode;
                if (_vad != null)
                    _vad.OperatingMode = (OperatingMode)mode;
                SaveAppSettings();
                OnSettingsUpdated();
                OnDebugMessage($"VAD mode updated to {mode}.");
                }
            catch (Exception ex)
                {
                OnDebugMessage($"SetVadMode error: {ex.Message}");
                }
            }

        // --- Public Methods for Form1 to Call ---
        public async Task<bool> StartRecordingAsync(int deviceNumber)
            {
            if (isRecording)
                {
                OnDebugMessage("Already recording.");
                return false;
                }
            if (WaveIn.DeviceCount == 0)
                {
                OnDebugMessage("No microphone detected.");
                return false;
                }
            if (deviceNumber < 0 || deviceNumber >= WaveIn.DeviceCount)
                {
                OnDebugMessage("Invalid device number.");
                deviceNumber = 0;
                Settings.SelectedMicrophoneDevice = 0;
                }

            if (!await InitializeWhisperAsync())
                {
                OnDebugMessage("Whisper init failed.");
                return false;
                }

            if (llmExecutor == null && Settings.ProcessWithLLM && !InitializeLLM()) // Pre-initialize LLM only if necessary
                {
                OnDebugMessage("LLM init failed, proceeding without LLM for this session if StartRecording is called again.");
                }
            OnDebugMessage("LLM initialization failed. The current session will not use LLM, but recording can proceed without it if started again.");

            OnDebugMessage($"Starting recording with mic [{deviceNumber}]...");
            lock (currentSessionTranscribedText)
            {
                currentSessionTranscribedText.Clear();
            }
            isRecording = true;
            StartNewChunk();
            waveSource = new WaveInEvent { DeviceNumber = deviceNumber, WaveFormat = waveFormatForWhisper };
            waveSource.DataAvailable += WaveSource_DataAvailable;
            waveSource.RecordingStopped += WaveSource_RecordingStopped;
            waveSource.StartRecording();
            OnRecordingStateChanged(true);
            return true;
            }

        public Task StopRecording()
            {
            if (!isRecording || waveSource == null)
                {
                OnDebugMessage("Not recording or waveSource is null.");
                return Task.CompletedTask;
                }
            OnDebugMessage("StopRecording called externally (e.g., from UI).");
            if (_currentStopProcessingTcs == null || _currentStopProcessingTcs.Task.IsCompleted)
                {
                _currentStopProcessingTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            waveSource.StopRecording();
            return _currentStopProcessingTcs.Task;
            }

        public async Task WaitForCurrentTranscriptionToCompleteAsync()
            {
            if (currentTranscriptionTask != null && !currentTranscriptionTask.IsCompleted)
                {
                OnDebugMessage("Waiting for ongoing transcription to complete...");
                try
                    {
                    await currentTranscriptionTask;
                    }
                catch { /* ignore */ }
                }
            }

        public static List<(int Index, string Name)> GetAvailableMicrophones()
            {
            var mics = new List<(int, string)>();
            if (WaveIn.DeviceCount == 0)
                return mics; // Return empty if no devices
            for (int i = 0; i < WaveIn.DeviceCount; i++)
                {
                try
                    {
                    mics.Add((i, WaveIn.GetCapabilities(i).ProductName));
                    }
                catch { mics.Add((i, $"Err mic {i}")); }
                }
            return mics;
            }

        public bool SelectMicrophone(int deviceIndex)
            {
            if (isRecording)
                {
                OnDebugMessage("Cannot change mic while recording.");
                return false;
                }
            if (WaveIn.DeviceCount > 0 && deviceIndex >= 0 && deviceIndex < WaveIn.DeviceCount) // Check DeviceCount > 0
                {
                if (Settings.SelectedMicrophoneDevice != deviceIndex)
                    {
                    Settings.SelectedMicrophoneDevice = deviceIndex;
                    SaveAppSettings();
                    OnDebugMessage($"Mic selected: [{deviceIndex}]");
                    OnSettingsUpdated();
                    }
                return true;
                }
            OnDebugMessage($"Invalid mic index: {deviceIndex} or no mics available.");
            return false;
            }

        public async Task<bool> ChangeModelPathAsync(string newModelPath) // For Whisper model
            {
            if (isRecording)
                {
                OnDebugMessage("Cannot change Whisper model while recording.");
                return false;
                }
            if (!string.IsNullOrWhiteSpace(newModelPath) && File.Exists(newModelPath))
                {
                if (currentWhisperModelPath != newModelPath)
                    {
                    currentWhisperModelPath = newModelPath;
                    Settings.ModelFilePath = newModelPath;
                    SaveAppSettings();
                    OnDebugMessage($"Whisper model path updated: {currentWhisperModelPath}");
                    await DisposeWhisperResourcesAsync();
                    OnSettingsUpdated();
                    return true;
                    }
                OnDebugMessage("New Whisper model path is same as current.");
                return true;
                }
            OnDebugMessage("Invalid Whisper model path or file not found.");
            return false;
            }

        public async Task<bool> ChangeLLMModelPathAsync(string newLLMModelPath)
            {
            if (isRecording || activelyProcessingChunk)
                {
                OnDebugMessage("Cannot change LLM model while busy.");
                return false;
                }
            if (!string.IsNullOrWhiteSpace(newLLMModelPath) && File.Exists(newLLMModelPath))
                {
                if (Settings.LocalLLMModelPath != newLLMModelPath || llmModelWeights == null)
                    {
                    Settings.LocalLLMModelPath = newLLMModelPath;
                    SaveAppSettings();
                    OnDebugMessage($"LLM model path updated: {Settings.LocalLLMModelPath}");
                    await DisposeLLMResourcesAsync();
                    OnSettingsUpdated();
                    return true;
                    }
                OnDebugMessage("New LLM model path is same as current.");
                return true;
                }
            OnDebugMessage("Invalid LLM model path or file not found.");
            return false;
            }

        // --- IDisposable ---
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
            {
            if (!disposedValue)
                {
                if (disposing)
                    {
                    // Dispose managed state (managed objects)
                    try
                        {
                        waveSource?.Dispose();
                        chunkWaveFile?.Dispose();
                        currentAudioChunkStream?.Dispose();
                        whisperFactoryInstance?.Dispose();

                        // StatelessExecutor may not implement IDisposable in this LLama build.
                        // Dispose it only if it actually implements IDisposable to avoid compile errors.
                        if (llmExecutor is IDisposable disposableExecutor)
                        {
                            try { disposableExecutor.Dispose(); }
                            catch { /* ignore dispose errors */ }
                        }

                        llmContext?.Dispose();
                        llmModelWeights?.Dispose();

                        // Dispose VAD
                        try { _vad?.Dispose(); } catch { }

                        llmExecutor = null;
                        llmContext = null;
                        llmModelWeights = null;
                        }
                    catch { /* Optionally log or ignore */ }
                }
                // Free unmanaged resources (if any) here

                disposedValue = true;
                }
            }

        public void Dispose()
            {
            Dispose(true);
            GC.SuppressFinalize(this);
            }
        // --- Public methods for Dictation Mode ---
        public async Task<bool> StartDictationModeAsync(int deviceNumber)
            {
            if (isRecording)
                {
                OnDebugMessage("Already recording (normal or dictation).");
                return false;
                }

            IsDictationModeActive = true; // Set mode
            _dictationModeStopSignal = new TaskCompletionSource<bool>(); // For awaiting stop of this mode

            // Use the same StartRecordingAsync logic but it will know it's dictation mode
            bool success = await StartRecordingAsync(deviceNumber);
            if (!success)
                {
                IsDictationModeActive = false; // Revert if start failed
                _dictationModeStopSignal.TrySetResult(false); // Signal failure
                }
            return success;
            }

        public async Task StopDictationModeAsync()
            {
            if (!isRecording || !IsDictationModeActive)
                {
                OnDebugMessage("Not in active dictation mode to stop.");
                IsDictationModeActive = false; // Ensure it's false
                _dictationModeStopSignal?.TrySetResult(true); // Signal if anyone is waiting
                return;
                }

            OnDebugMessage("Stopping dictation mode...");
            await StopRecording(); // This will use the _currentStopProcessingTcs

            if (_dictationModeStopSignal != null)
                {
                await _dictationModeStopSignal.Task; // Wait for RecordingStopped to signal this TCS
                }
            IsDictationModeActive = false; // Ensure final state
            OnDebugMessage("Dictation mode fully stopped.");
            }
        }
    }
