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
using LLama.Common; // For ModelParams, InferenceParams
// using LLama.Abstractions; // If using interfaces like ILLamaExecutor
// using WhisperNetConsoleDemo; // If AppSettings is in a separate file in this namespace

namespace WhisperNetConsoleDemo
{
    public class TranscriptionService : IDisposable // Consider IAsyncDisposable
    {
        // --- Constants ---
        private const double NORMAL_MAX_CHUNK_DURATION_SECONDS = 20.0;
        private const double NORMAL_SILENCE_THRESHOLD_SECONDS = 2.0;
        // DEFAULT_ENERGY_SILENCE_THRESHOLD is now calibratedEnergySilenceThreshold

        // --- Constants for Dictation Mode ---
        private const double DICTATION_MAX_CHUNK_DURATION_SECONDS = 7.0;  // Shorter max duration
        private const double DICTATION_SILENCE_THRESHOLD_SECONDS = 0.8; // Much shorter silence (e.g., 0.7 to 1.2 seconds)
        // DEFAULT_ENERGY_SILENCE_THRESHOLD and DEFAULT_MODEL_FILE_PATH now come from AppSettings class
        private const int SILENCE_DETECTION_BUFFER_MILLISECONDS = 250;
        public const int CALIBRATION_DURATION_SECONDS = 3; // public if Form1 needs it for display

        // --- Events for UI Updates ---
        public event Action<string, string>? SegmentTranscribed; // (timestampedText, rawText)
        public event Action<string>? FullTranscriptionReady;
        public event Action<string>? DebugMessageGenerated;
        public event Action<bool>? RecordingStateChanged;
        public event Action? SettingsUpdated;
        public event Action? ProcessingStarted;
        public event Action? ProcessingFinished;

        // --- State Fields ---
        private bool isRecording = false;
        private WaveInEvent? waveSource = null;
        private MemoryStream? currentAudioChunkStream = null;
        private WaveFileWriter? chunkWaveFile = null;
        private DateTime chunkStartTime = DateTime.MinValue;
        private DateTime lastSpeechTime = DateTime.MinValue;
        private bool silenceDetectedRecently = false;
        private byte[] silenceDetectionBuffer = new byte[0];
        private int silenceDetectionBufferBytesRecorded = 0;
        private bool activelyProcessingChunk = false;
        private Task? currentTranscriptionTask = null;
        private readonly List<string> currentSessionTranscribedText = new List<string>();
        public string LastRawFilteredText { get; private set; } = string.Empty;
        public string LastLLMProcessedText { get; private set; } = string.Empty;
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
        private WhisperProcessor? whisperProcessorInstance = null;
        private readonly WaveFormat waveFormatForWhisper = new WaveFormat(16000, 16, 1);
        private float calibratedEnergySilenceThreshold;

        public AppSettings Settings { get; private set; } = new AppSettings();
        private readonly string appSettingsFilePath = "appsettings.json";

        private enum CalibrationStep
        {
            None, SamplingSilence, SamplingSpeech_Prompt, SamplingSpeech_Recording
        }
        private CalibrationStep currentCalibrationStep = CalibrationStep.None;
        private readonly List<float> calibrationSamples = new List<float>();
        private bool isCalibrating = false;

        private TaskCompletionSource<bool>? _currentStopProcessingTcs;

        public bool IsDictationModeActive { get; private set; } = false;
        private TaskCompletionSource<bool>? _dictationModeStopSignal;

        public TranscriptionService()
        {
            LoadAppSettings();
            currentWhisperModelPath = Settings.ModelFilePath; // For Whisper
            calibratedEnergySilenceThreshold = Settings.CalibratedEnergySilenceThreshold;
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
                OnDebugMessage($"Loaded Whisper Model: {Settings.ModelFilePath}, LLM Model: {Settings.LocalLLMModelPath}, Threshold: {Settings.CalibratedEnergySilenceThreshold}, Mic: {Settings.SelectedMicrophoneDevice}");
            }
            else
            {
                OnDebugMessage($"'{appSettingsFilePath}' not found. Using/Creating defaults.");
                SaveAppSettings();
            }
            currentWhisperModelPath = Settings.ModelFilePath;
            calibratedEnergySilenceThreshold = Settings.CalibratedEnergySilenceThreshold;
            OnSettingsUpdated();
        }

        public void SaveAppSettings()
        {
            OnDebugMessage("Saving application settings...");
            try
            {
                Settings.ModelFilePath = currentWhisperModelPath; // Whisper model
                Settings.CalibratedEnergySilenceThreshold = calibratedEnergySilenceThreshold;
                // LLMModelPath, SelectedMicrophoneDevice are updated in Settings object directly

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
            if (whisperProcessorInstance != null)
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
                whisperProcessorInstance = whisperFactoryInstance.CreateBuilder().WithLanguage("auto").Build();
                OnDebugMessage("WhisperFactory and WhisperProcessor initialized.");
                return true;
            }
            catch (Exception ex)
            {
                OnDebugMessage($"FATAL: Could not initialize Whisper.net: {ex.Message}");
                whisperProcessorInstance = null;
                whisperFactoryInstance = null;
                return false;
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
            OnDebugMessage("Disposing WhisperProcessor and WhisperFactory (async).");
            if (whisperProcessorInstance != null)
            {
                await whisperProcessorInstance.DisposeAsync(); // Use Async for processor
                whisperProcessorInstance = null;
            }
            whisperFactoryInstance?.Dispose();
            whisperFactoryInstance = null;
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
                // await Task.Run(() => DisposeLLMResourcesInternal()); // Comment out Task.Run
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
                // It is safe to ignore exceptions here because disposing the previous chunkWaveFile is a cleanup step,
                // and any errors (such as already disposed) do not affect the logic for starting a new chunk.
            }

            currentAudioChunkStream = new MemoryStream();
            chunkWaveFile = new WaveFileWriter(currentAudioChunkStream, waveFormatForWhisper);
            chunkStartTime = DateTime.UtcNow;
            lastSpeechTime = DateTime.UtcNow;
            silenceDetectedRecently = false;

            int bytesPerSample = waveFormatForWhisper.BitsPerSample / 8;
            int samplesPerBuffer = waveFormatForWhisper.SampleRate * SILENCE_DETECTION_BUFFER_MILLISECONDS / 1000;
            int bufferSize = samplesPerBuffer * bytesPerSample * waveFormatForWhisper.Channels;
            if (bufferSize == 0 && waveFormatForWhisper.AverageBytesPerSecond > 0)
            {
                bufferSize = waveFormatForWhisper.BlockAlign > 0 ? waveFormatForWhisper.BlockAlign : 2;
            }
            else if (waveFormatForWhisper.BlockAlign > 0 && bufferSize % waveFormatForWhisper.BlockAlign != 0)
            {
                bufferSize = ((bufferSize / waveFormatForWhisper.BlockAlign) + 1) * waveFormatForWhisper.BlockAlign;
            }
            silenceDetectionBuffer = new byte[bufferSize];
            silenceDetectionBufferBytesRecorded = 0;
        }

        private async void WaveSource_DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!isRecording || isCalibrating || chunkWaveFile == null || currentAudioChunkStream == null)
                return;
            try
            {
                chunkWaveFile.Write(e.Buffer, 0, e.BytesRecorded);
            }
            catch (ObjectDisposedException) { OnDebugMessage("DataAvailable - Write to disposed chunkWaveFile."); return; }

            bool speechInSeg = false;
            int bytesProcEvent = e.BytesRecorded, offset = 0;
            if (silenceDetectionBuffer.Length == 0)
                return;
            while (bytesProcEvent > 0)
            {
                int toCopy = Math.Min(bytesProcEvent, silenceDetectionBuffer.Length - silenceDetectionBufferBytesRecorded);
                if (toCopy <= 0)
                    break;
                Buffer.BlockCopy(e.Buffer, offset, silenceDetectionBuffer, silenceDetectionBufferBytesRecorded, toCopy);
                silenceDetectionBufferBytesRecorded += toCopy;
                bytesProcEvent -= toCopy;
                offset += toCopy;
                if (silenceDetectionBufferBytesRecorded == silenceDetectionBuffer.Length)
                {
                    float maxSample = 0f;
                    for (int i = 0; i < silenceDetectionBuffer.Length; i += waveFormatForWhisper.BlockAlign)
                    {
                        if (waveFormatForWhisper.BitsPerSample == 16 && waveFormatForWhisper.Channels == 1 && i + 1 < silenceDetectionBuffer.Length)
                        {
                            short s = BitConverter.ToInt16(silenceDetectionBuffer, i);
                            float sf = s / 32768.0f;
                            if (Math.Abs(sf) > maxSample)
                                maxSample = Math.Abs(sf);
                        }
                    }
                    if (maxSample > calibratedEnergySilenceThreshold)
                        speechInSeg = true;
                    silenceDetectionBufferBytesRecorded = 0;
                }
            }
            if (speechInSeg)
            {
                lastSpeechTime = DateTime.UtcNow;
                silenceDetectedRecently = false;
            }
            else
            {
                double currentSilenceThresholdSeconds = this.IsDictationModeActive ?
                                                   DICTATION_SILENCE_THRESHOLD_SECONDS :
                                                   NORMAL_SILENCE_THRESHOLD_SECONDS;
                if (currentAudioChunkStream.Length > 0 && (DateTime.UtcNow - lastSpeechTime) > TimeSpan.FromSeconds(currentSilenceThresholdSeconds))
                    silenceDetectedRecently = true;
            }

            TimeSpan chunkDur = DateTime.UtcNow - chunkStartTime;
            bool process = false;
            double currentMaxChunkDurationSeconds = this.IsDictationModeActive ?
                                                DICTATION_MAX_CHUNK_DURATION_SECONDS :
                                                NORMAL_MAX_CHUNK_DURATION_SECONDS;
            if (currentAudioChunkStream.Length > (waveFormatForWhisper.AverageBytesPerSecond / 2))
            {
                if (chunkDur >= TimeSpan.FromSeconds(currentMaxChunkDurationSeconds))
                {
                    OnDebugMessage($"Max chunk duration ({currentMaxChunkDurationSeconds}s) reached (Mode: {(this.IsDictationModeActive ? "Dictate" : "Normal")}).");
                    process = true;
                }
                else if (silenceDetectedRecently)
                {
                    OnDebugMessage($"Silence detected, processing chunk (Mode: {(this.IsDictationModeActive ? "Dictate" : "Normal")}).");
                    process = true;
                }
            }
            if (!activelyProcessingChunk && process)
            {
                ProcessingStarted?.Invoke();
                activelyProcessingChunk = true;
                OnDebugMessage($"Chunk ready. Dur: {chunkDur.TotalSeconds:F1}s, Silence: {silenceDetectedRecently}, Len: {currentAudioChunkStream.Length}");
                MemoryStream? streamToSend = null;
                WaveFileWriter? capturedChunkFile = chunkWaveFile; // Capture before StartNewChunk nulls it
                MemoryStream? capturedAudioChunkStream = currentAudioChunkStream;

                chunkWaveFile = null; // Prevent further writes to this instance
                currentAudioChunkStream = null; // Prevent further writes

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
                    capturedChunkFile?.Dispose();
                    capturedAudioChunkStream?.Dispose();
                }
                finally
                {
                    ProcessingFinished?.Invoke();
                    capturedChunkFile?.Dispose(); // This disposes capturedAudioChunkStream
                }

                StartNewChunk(); // Prepare for the *next* segment of audio immediately

                if (streamToSend != null && streamToSend.Length > 0)
                {
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
                    }
                }
                else
                {
                    activelyProcessingChunk = false;
                    streamToSend?.Dispose();
                }
            }
        }

        // TranscriptionService.cs

        // ... (Fields including: _currentStopProcessingTcs, currentAudioChunkStream, chunkWaveFile,
        //      currentSessionTranscribedText, whisperProcessorInstance, Settings, etc. are INSTANCE fields) ...

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
                    if (finalActiveStream.Length > (this.waveFormatForWhisper.AverageBytesPerSecond / 20))
                    {
                        finalActiveStream.Position = 0; // Rewind the stream to be read from the beginning
                        streamToTranscribeThisFinalChunk = new MemoryStream();
                        await finalActiveStream.CopyToAsync(streamToTranscribeThisFinalChunk);
                        streamToTranscribeThisFinalChunk.Position = 0; // Rewind the new stream for transcription
                        OnDebugMessage($"WaveSource_RecordingStopped - Copied final chunk of {streamToTranscribeThisFinalChunk.Length} bytes for transcription.");
                    }
                    else
                    {
                        OnDebugMessage("WaveSource_RecordingStopped - Final active stream was too short to process.");
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
            if (whisperProcessorInstance == null)
            {
                OnDebugMessage("ERROR: WhisperProcessor not initialized.");
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
                await foreach (var segment in whisperProcessorInstance.ProcessAsync(audioStream))
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

        private async Task<string> ProcessTextWithLLMAsync(string inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
                return inputText;
            if (!Settings.ProcessWithLLM)
                return inputText;

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
                    $"<|im_start|>system \n{Settings.LLMSystemPrompt}<|im_end|>\n" +
                    $"<|im_start|>user \nCorrect grammar, improve clarity, ensure punctuation is accurate, and make the following text sound more professional. Output only the revised text, without any preamble or explanation:\n\n{inputText}<|im_end|>\n" +
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
                    //Temperature = Settings.LLMTemperature,
                    AntiPrompts = new List<string> { "<|im_end|>", "user:", "User:", "<|user|>", System.Environment.NewLine + "<|im_start|>" }, // More robust anti-prompts
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
                OnDebugMessage("LLamaSharp processing successful." + outputBuffer.ToString().Trim());
                return outputBuffer.ToString().Trim();
            }
            catch (Exception ex)
            {
                OnDebugMessage($"Generic error during LLamaSharp processing: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                return inputText;
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
            currentSessionTranscribedText.Clear();
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

        public async Task CalibrateThresholdsAsync(int deviceNumber, Action<string> statusUpdateCallback)
        {
            statusUpdateCallback("--- Starting Silence Threshold Calibration ---");
            WaveInEvent? calWaveIn = null;
            if (isRecording)
            {
                statusUpdateCallback("Cannot calibrate while main recording active.");
                return;
            }
            isCalibrating = true;
            float typicalSilenceLevel = 0.001f;
            float typicalSpeechLevel = 0.1f;
            try
            {
                currentCalibrationStep = CalibrationStep.SamplingSilence;
                calibrationSamples.Clear();
                statusUpdateCallback($"Please remain completely silent for {CALIBRATION_DURATION_SECONDS}s...");
                for (int i = CALIBRATION_DURATION_SECONDS; i > 0; i--)
                {
                    statusUpdateCallback($"Sampling silence in {i}... ");
                    if (i == 1)
                        await Task.Delay(700);
                    else
                        await Task.Delay(1000);
                }
                statusUpdateCallback("NOW SAMPLING SILENCE...");
                calWaveIn = new WaveInEvent { DeviceNumber = deviceNumber, WaveFormat = waveFormatForWhisper };
                calWaveIn.DataAvailable += CollectCalibrationSamples_Handler;
                calWaveIn.StartRecording();
                await Task.Delay(CALIBRATION_DURATION_SECONDS * 1000);
                calWaveIn.StopRecording();
                calWaveIn.DataAvailable -= CollectCalibrationSamples_Handler;
                calWaveIn.Dispose();
                calWaveIn = null;
                statusUpdateCallback("Silence sampling complete.");
                if (calibrationSamples.Count > 0)
                {
                    var o = calibrationSamples.OrderBy(x => x).ToList();
                    typicalSilenceLevel = o.ElementAtOrDefault((int)(o.Count * 0.95));
                    if (typicalSilenceLevel < 0.00001f && typicalSilenceLevel >= 0)
                        typicalSilenceLevel = 0.0001f;
                    else if (typicalSilenceLevel < 0)
                        typicalSilenceLevel = 0.0001f;
                }
                else
                {
                    statusUpdateCallback("No silence samples. Low default used.");
                }
                OnDebugMessage($"Calibrate: Typical silence (95th): {typicalSilenceLevel:F4}");
                statusUpdateCallback($" (Detected silence level: {typicalSilenceLevel:F4})");

                currentCalibrationStep = CalibrationStep.SamplingSpeech_Prompt;
                calibrationSamples.Clear();
                statusUpdateCallback($"\nSpeak normally for {CALIBRATION_DURATION_SECONDS}s...");
                for (int i = CALIBRATION_DURATION_SECONDS; i > 0; i--)
                {
                    statusUpdateCallback($"Sampling speech in {i}... ");
                    if (i == 1)
                        await Task.Delay(700);
                    else
                        await Task.Delay(1000);
                }
                statusUpdateCallback("NOW SPEAKING...");
                currentCalibrationStep = CalibrationStep.SamplingSpeech_Recording;
                calWaveIn = new WaveInEvent { DeviceNumber = deviceNumber, WaveFormat = waveFormatForWhisper };
                calWaveIn.DataAvailable += CollectCalibrationSamples_Handler;
                calWaveIn.StartRecording();
                await Task.Delay(CALIBRATION_DURATION_SECONDS * 1000);
                calWaveIn.StopRecording();
                calWaveIn.DataAvailable -= CollectCalibrationSamples_Handler;
                if (calibrationSamples.Count > 0)
                {
                    var o = calibrationSamples.OrderBy(x => x).ToList();
                    typicalSpeechLevel = o.ElementAtOrDefault((int)(o.Count * 0.10));
                    if (typicalSpeechLevel < 0.001f && typicalSpeechLevel >= 0)
                        typicalSpeechLevel = 0.05f;
                    else if (typicalSpeechLevel < 0)
                        typicalSpeechLevel = 0.05f;
                }
                else
                {
                    statusUpdateCallback("No speech samples. Default used.");
                }
                OnDebugMessage($"Calibrate: Typical speech (10th): {typicalSpeechLevel:F4}");
                statusUpdateCallback($" (Detected speech level: {typicalSpeechLevel:F4})");

                if (typicalSpeechLevel <= typicalSilenceLevel + 0.005f)
                {
                    statusUpdateCallback("Warning: Speech not significantly louder.");
                    calibratedEnergySilenceThreshold = typicalSilenceLevel * 2.0f;
                    if (calibratedEnergySilenceThreshold < (AppSettings.APPSETTINGS_DEFAULT_ENERGY_THRESHOLD / 2) && AppSettings.APPSETTINGS_DEFAULT_ENERGY_THRESHOLD > 0)
                        calibratedEnergySilenceThreshold = AppSettings.APPSETTINGS_DEFAULT_ENERGY_THRESHOLD / 2;
                    else if (calibratedEnergySilenceThreshold < 0.002f)
                        calibratedEnergySilenceThreshold = 0.002f;
                }
                else
                {
                    float diff = typicalSpeechLevel - typicalSilenceLevel;
                    calibratedEnergySilenceThreshold = typicalSilenceLevel + (diff * 0.25f);
                }
                calibratedEnergySilenceThreshold = Math.Max(0.002f, Math.Min(0.35f, calibratedEnergySilenceThreshold));
                Settings.CalibratedEnergySilenceThreshold = calibratedEnergySilenceThreshold;
                SaveAppSettings();
                statusUpdateCallback($"--- Calibration Complete ---\nNew Threshold: {calibratedEnergySilenceThreshold:F4} (Saved)");
            }
            catch (Exception ex) { statusUpdateCallback($"Calibration Error: {ex.Message}"); OnDebugMessage($"Full CalibEx: {ex}"); }
            finally { isCalibrating = false; currentCalibrationStep = CalibrationStep.None; calWaveIn?.Dispose(); OnDebugMessage("Calibration process finished."); OnSettingsUpdated(); }
        }

        private void CollectCalibrationSamples_Handler(object? sender, WaveInEventArgs e)
        {
            if (!isCalibrating || !(currentCalibrationStep == CalibrationStep.SamplingSilence || currentCalibrationStep == CalibrationStep.SamplingSpeech_Recording))
                return;
            int tempVADBytesRec = 0;
            int bufferSize = (silenceDetectionBuffer != null && silenceDetectionBuffer.Length > 0) ? silenceDetectionBuffer.Length : (waveFormatForWhisper.AverageBytesPerSecond * SILENCE_DETECTION_BUFFER_MILLISECONDS / 1000);
            if (bufferSize <= 0)
                bufferSize = waveFormatForWhisper.BlockAlign > 0 ? waveFormatForWhisper.BlockAlign * 5 : 10;
            if (waveFormatForWhisper.BlockAlign > 0 && bufferSize % waveFormatForWhisper.BlockAlign != 0)
            {
                bufferSize = ((bufferSize / waveFormatForWhisper.BlockAlign) + 1) * waveFormatForWhisper.BlockAlign;
            }
            byte[] tempBuf = new byte[bufferSize];
            int bytesProcEvent = e.BytesRecorded, offset = 0;
            while (bytesProcEvent > 0 && tempBuf.Length > 0)
            {
                int toCopy = Math.Min(bytesProcEvent, tempBuf.Length - tempVADBytesRec);
                if (toCopy <= 0)
                    break;
                Buffer.BlockCopy(e.Buffer, offset, tempBuf, tempVADBytesRec, toCopy);
                tempVADBytesRec += toCopy;
                bytesProcEvent -= toCopy;
                offset += toCopy;
                if (tempVADBytesRec == tempBuf.Length)
                {
                    float maxSample = 0f;
                    for (int i = 0; i < tempBuf.Length; i += waveFormatForWhisper.BlockAlign)
                    {
                        if (waveFormatForWhisper.BitsPerSample == 16 && waveFormatForWhisper.Channels == 1 && i + 1 < tempBuf.Length)
                        {
                            short s = BitConverter.ToInt16(tempBuf, i);
                            float sf = s / 32768.0f;
                            if (Math.Abs(sf) > maxSample)
                                maxSample = Math.Abs(sf);
                        }
                    }
                    calibrationSamples.Add(maxSample);
                    tempVADBytesRec = 0;
                }
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
                        whisperProcessorInstance?.Dispose();
                        whisperFactoryInstance?.Dispose();
                        llmContext?.Dispose();
                        llmModelWeights?.Dispose();
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
