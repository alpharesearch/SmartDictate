// TranscriptionService.cs
using LLama; // Core LLamaSharp
using LLama.Common;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using SmartDictateAI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text; // For StringBuilder
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;

namespace SmartDictateAI
{
    public class TranscriptionService : IDisposable
    {
        // --- Events for UI Updates ---
        public event Action<string, string>? SegmentTranscribed; // (timestampedText, rawText)
        public event Action<string>? FullTranscriptionReady;
        public event Action<string>? DebugMessageGenerated;
        public event Action<bool>? RecordingStateChanged;
        public event Action? SettingsUpdated;
        public event Action? ProcessingStarted;
        public event Action? ProcessingFinished;
        public event Action<DictationVisualState>? VisualStateChanged;

        private DictationVisualState _currentVisualState = DictationVisualState.Idle;
        public DictationVisualState CurrentVisualState
        {
            get => _currentVisualState;
            private set
            {
                if (_currentVisualState != value)
                {
                    _currentVisualState = value;
                    VisualStateChanged?.Invoke(_currentVisualState);
                }
            }
        }

        // --- Services ---
        private readonly ISettingsService _settingsService;
        private readonly IVadService _vadService;
        private readonly IWhisperService _whisperService;
        private readonly ILLMService _llmService;
        private readonly IAudioCaptureService _audioCaptureService;

        // --- State Fields ---
        private bool _hasSpeechInCurrentChunk = false;
        private readonly object _audioStreamLock = new object();
        private bool isRecording = false;
        private MemoryStream? currentAudioChunkStream = null;
        private WaveFileWriter? chunkWaveFile = null;
        private DateTime chunkStartTime = DateTime.MinValue;
        private DateTime lastSpeechTime = DateTime.MinValue;
        private bool silenceDetectedRecently = false;
        private bool activelyProcessingChunk = false;
        private Task? currentTranscriptionTask = null;
        private readonly List<string> currentSessionTranscribedText = new List<string>();
        public string LastRawFilteredText { get; set; } = string.Empty;
        public string LastLLMProcessedText { get; set; } = string.Empty;
        public bool WasLastProcessingWithLLM { get; set; } = false; // To know if LLM text is valid
        private bool _loggedSilenceProcessThisChunk = false;
        private bool _loggedMaxDurationThisChunk = false;

        // --- Whisper & App Settings ---
        private string currentWhisperModelPath = string.Empty;
        private readonly WaveFormat waveFormatForWhisper = new WaveFormat(16000, 16, 1);

        public AppSettings Settings { get; private set; } = new AppSettings();
        private readonly string appSettingsFilePath = "appsettings.json";

        private TaskCompletionSource<bool>? _currentStopProcessingTcs;

        public bool IsDictationModeActive { get; private set; } = false;
        private TaskCompletionSource<bool>? _dictationModeStopSignal;

        private List<byte> _vadAudioBuffer = new List<byte>();
        private const int VAD_FRAME_BYTES = 640;

        // --- Constructors ---
        public TranscriptionService() : this(
            new SettingsService(),
            new VadService(),
            new WhisperService(),
            new LLMService(),
            new AudioCaptureService())
        {
        }

        public TranscriptionService(
            ISettingsService settingsService,
            IVadService vadService,
            IWhisperService whisperService,
            ILLMService llmService,
            IAudioCaptureService audioCaptureService)
        {
            _settingsService = settingsService;
            _vadService = vadService;
            _whisperService = whisperService;
            _llmService = llmService;
            _audioCaptureService = audioCaptureService;

            LoadAppSettings();
            currentWhisperModelPath = Settings.ModelFilePath;

            _vadService.Initialize(Settings.VadMode, OnDebugMessage);
        }

        private void OnDebugMessage(string message) => DebugMessageGenerated?.Invoke(message);
        private void OnSegmentTranscribed(string timestamped, string raw) => SegmentTranscribed?.Invoke(timestamped, raw);
        private void OnFullTranscriptionReady(string fullText) => FullTranscriptionReady?.Invoke(fullText);
        private void OnRecordingStateChanged(bool nowRecording) => RecordingStateChanged?.Invoke(nowRecording);
        private void OnSettingsUpdated() => SettingsUpdated?.Invoke();

        public void LoadAppSettings()
        {
            Settings = _settingsService.LoadSettings(appSettingsFilePath, OnDebugMessage);
            currentWhisperModelPath = Settings.ModelFilePath;
            OnSettingsUpdated();
        }

        public void SaveAppSettings()
        {
            Settings.ModelFilePath = currentWhisperModelPath;
            _settingsService.SaveSettings(appSettingsFilePath, Settings, OnDebugMessage);
            OnSettingsUpdated();
        }

        public async Task<bool> InitializeWhisperAsync()
        {
            return await _whisperService.InitializeAsync(currentWhisperModelPath, OnDebugMessage);
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

                OnDebugMessage($"[Audio] Saved debug audio: {filename}");
            }
            catch (Exception ex)
            {
                OnDebugMessage($"[Audio] Failed to save debug audio: {ex.Message}");
            }
        }

        public async Task DisposeWhisperResourcesAsync()
        {
            await _whisperService.DisposeResourcesAsync(OnDebugMessage);
        }

        private void CleanupDebugAudioFolder()
        {
            try
            {
                string debugDir = Path.Combine(Directory.GetCurrentDirectory(), "DebugAudio");

                if (Directory.Exists(debugDir))
                {
                    Directory.Delete(debugDir, recursive: true);
                    OnDebugMessage("[Audio] DebugAudio folder deleted on exit.");
                }
            }
            catch (Exception ex)
            {
                OnDebugMessage($"[Audio] Failed to delete DebugAudio folder: {ex.Message}");
            }
        }

        public async Task DisposeLLMResourcesAsync()
        {
            await _llmService.DisposeResourcesAsync(OnDebugMessage);
        }

        public async Task PreloadModelsAsync()
        {
            CurrentVisualState = DictationVisualState.Loading;
            OnDebugMessage("[App] Preloading models in background...");
            try
            {
                var whisperTask = InitializeWhisperAsync();
                var llmTask = Task.Run(() =>
                {
                    if (Settings.ProcessWithLLM)
                    {
                        _llmService.Initialize(Settings.LocalLLMModelPath, Settings.LLMContextSize, Settings.UseGpu, OnDebugMessage);
                    }
                });
                await Task.WhenAll(whisperTask, llmTask);
                OnDebugMessage("[App] Background model preloading completed.");
                CurrentVisualState = DictationVisualState.Idle;
            }
            catch (Exception ex)
            {
                OnDebugMessage($"[App] Background model preloading failed: {ex.Message}");
                CurrentVisualState = DictationVisualState.Idle;
            }
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
            _loggedSilenceProcessThisChunk = false;
            _loggedMaxDurationThisChunk = false;
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
                catch (ObjectDisposedException) { OnDebugMessage("[Whisper] DataAvailable - Write to disposed chunkWaveFile."); return; }
            }

            bool speechInSeg = false;

            try
            {
                _vadAudioBuffer.AddRange(e.Buffer.Take(e.BytesRecorded));

                while (_vadAudioBuffer.Count >= VAD_FRAME_BYTES)
                {
                    var rawFrame = _vadAudioBuffer.GetRange(0, VAD_FRAME_BYTES).ToArray();
                    _vadAudioBuffer.RemoveRange(0, VAD_FRAME_BYTES);

                    try
                    {
                        if (_vadService.HasSpeech(rawFrame, Settings.VadGainMultiplier, OnDebugMessage))
                        {
                            if (!_hasSpeechInCurrentChunk)
                            {
                                OnDebugMessage("[VAD] >>> VAD TRIGGERED: Speech Detected with Gain! <<<");
                                CurrentVisualState = DictationVisualState.SpeechDetected;
                            }
                            lastSpeechTime = DateTime.UtcNow;
                            silenceDetectedRecently = false;
                            speechInSeg = true;
                            _hasSpeechInCurrentChunk = true;
                        }
                    }
                    catch (Exception ex) { OnDebugMessage($"[VAD] VAD processing error: {ex.Message}"); }
                }
            }
            catch (Exception ex) { OnDebugMessage($"[VAD] VAD buffer error: {ex.Message}"); }

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

            if (currentAudioChunkStream != null && currentAudioChunkStream.Length > (waveFormatForWhisper.AverageBytesPerSecond / 2))
            {
                if (chunkDur >= TimeSpan.FromSeconds(currentMaxChunkDurationSeconds))
                {
                    if (_hasSpeechInCurrentChunk)
                    {
                        if (!_loggedMaxDurationThisChunk)
                        {
                            OnDebugMessage($"[Whisper] Max chunk duration ({currentMaxChunkDurationSeconds}s) reached.");
                            _loggedMaxDurationThisChunk = true;
                        }
                        process = true;
                    }
                    else
                    {
                        discardChunk = true;
                    }
                }
                else if (silenceDetectedRecently)
                {
                    if (!_loggedSilenceProcessThisChunk)
                    {
                        OnDebugMessage("[VAD] Silence detected after speech, processing chunk.");
                        _loggedSilenceProcessThisChunk = true;
                    }
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
                    _loggedSilenceProcessThisChunk = false;
                    _loggedMaxDurationThisChunk = false;
                    CurrentVisualState = DictationVisualState.ListeningSilent;
                }

                if (process)
                {
                    CurrentVisualState = DictationVisualState.Processing;
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
                        OnDebugMessage($"[App] Error prep stream: {ex.Message}");
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
                            OnDebugMessage($"[App] Err transcription task: {ex.Message}");
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

                    if (isRecording)
                    {
                        CurrentVisualState = _hasSpeechInCurrentChunk ? 
                            DictationVisualState.SpeechDetected : 
                            DictationVisualState.ListeningSilent;
                    }
                }
                else if (discardChunk)
                {
                    // SAVE THE AUDIO IT THREW AWAY SO WE CAN LISTEN TO IT
                    OnDebugMessage("[VAD] Discarding chunk (No speech detected by VAD).");
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
            OnDebugMessage("[Audio] WaveSource_RecordingStopped - Event ENTERED.");
            bool wasActuallyRecording = this.isRecording; // Capture current state
            this.isRecording = false;       // Set recording state to false immediately
            OnRecordingStateChanged(false); // Notify UI
            CurrentVisualState = DictationVisualState.Processing;

            await WaitForCurrentTranscriptionToCompleteAsync();

            // Capture the stream and writer that were active for the very last segment of audio
            MemoryStream? finalActiveStream = this.currentAudioChunkStream;
            WaveFileWriter? finalFile = this.chunkWaveFile;

            // Null out class fields so new recordings
            this.currentAudioChunkStream = null;
            this.chunkWaveFile = null;

            // Unsubscribe from events
            _audioCaptureService.DataAvailable -= WaveSource_DataAvailable;
            _audioCaptureService.RecordingStopped -= WaveSource_RecordingStopped;

            MemoryStream? streamToTranscribeThisFinalChunk = null;
            if (wasActuallyRecording && finalFile != null && finalActiveStream != null)
            {
                OnDebugMessage("[Whisper] WaveSource_RecordingStopped - Processing final audio chunk.");
                try
                {
                    finalFile.Flush(); // Ensure all buffered data is written to finalActiveStream
                    OnDebugMessage($"[Audio] WaveSource_RecordingStopped - Final active stream length after flush: {finalActiveStream.Length}");

                    // Process even if slightly shorter than normal chunks, but not if effectively empty
                    if (_hasSpeechInCurrentChunk && finalActiveStream.Length > (this.waveFormatForWhisper.AverageBytesPerSecond / 20))
                    {
                        finalActiveStream.Position = 0; // Rewind the stream to be read from the beginning
                        streamToTranscribeThisFinalChunk = new MemoryStream();
                        await finalActiveStream.CopyToAsync(streamToTranscribeThisFinalChunk);
                        streamToTranscribeThisFinalChunk.Position = 0; // Rewind the new stream for transcription
                        OnDebugMessage($"[Whisper] WaveSource_RecordingStopped - Copied final chunk of {streamToTranscribeThisFinalChunk.Length} bytes for transcription.");
                    }
                    else
                    {
                        OnDebugMessage("[Audio] WaveSource_RecordingStopped - Final active stream was too short or contained no speech.");
                    }
                }
                catch (Exception ex) { OnDebugMessage($"[Whisper] Err flush/copy final chunk: {ex.Message}"); }
                finally
                {
                    OnDebugMessage("[Audio] WaveSource_RecordingStopped - Disposing final WaveFileWriter (and its stream).");
                    try
                    {
                        finalFile.Dispose();
                    }
                    catch (Exception ex) { OnDebugMessage($"[App] Err disposing finalFile: {ex.Message}"); }
                }
            }
            else if (finalActiveStream != null && finalActiveStream.CanRead)
            {
                OnDebugMessage("[Audio] WaveSource_RecordingStopped - Disposing lingering finalActiveStream.");
                try
                {
                    finalActiveStream.Dispose();
                }
                catch { }
            }

            Task? transcriptionOfThisFinalChunkTask = null;
            if (wasActuallyRecording && streamToTranscribeThisFinalChunk != null && streamToTranscribeThisFinalChunk.Length > 0)
            {
                OnDebugMessage("[Whisper] WaveSource_RecordingStopped - Starting transcription for the final chunk.");
                if (Settings.ShowDebugMessages)
                {
                    SaveDebugAudioChunk(streamToTranscribeThisFinalChunk, "final_chunk");
                }
                transcriptionOfThisFinalChunkTask = TranscribeAudioChunkAsync(streamToTranscribeThisFinalChunk, this.IsDictationModeActive);

                try
                {
                    await transcriptionOfThisFinalChunkTask; // Wait for THIS final transcription
                    OnDebugMessage("[Whisper] WaveSource_RecordingStopped - Transcription of this final chunk completed.");
                }
                catch (Exception ex) { OnDebugMessage($"[App] Err awaiting FINAL transcription task: {ex.Message}"); }
            }
            else if (wasActuallyRecording)
            {
                OnDebugMessage("[Whisper] WaveSource_RecordingStopped - No substantial final audio chunk to transcribe.");
            }

            if (transcriptionOfThisFinalChunkTask == null && streamToTranscribeThisFinalChunk != null && streamToTranscribeThisFinalChunk.CanRead)
            {
                OnDebugMessage("[Whisper] WaveSource_RecordingStopped - Disposing unused streamToTranscribeThisFinalChunk.");
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
                    OnDebugMessage("[LLM] WaveSource_RecordingStopped: Attempting LLM processing for the full text...");
                    string refinedText = await ProcessTextWithLLMAsync(rawFilteredFullText);
                    this.LastLLMProcessedText = refinedText;
                    this.WasLastProcessingWithLLM = true;
                    OnDebugMessage("[LLM] WaveSource_RecordingStopped: LLM processing complete.");
                    finalTextToDisplay += $"{Environment.NewLine}{Environment.NewLine}--- LLM Refined ---{Environment.NewLine}{refinedText}";
                }
                else if (this.Settings.ProcessWithLLM && string.IsNullOrWhiteSpace(rawFilteredFullText))
                {
                    OnDebugMessage("[LLM] WaveSource_RecordingStopped: Full text is empty after filtering, skipping LLM.");
                    finalTextToDisplay += $"{Environment.NewLine}{Environment.NewLine}[No speech detected to refine with LLM]";
                }

                OnFullTranscriptionReady(finalTextToDisplay);
            }

            if (e.Exception != null)
            {
                OnDebugMessage($"[Audio] NAudio stop exception: {e.Exception.Message}");
            }

            IsDictationModeActive = false;
            this._currentStopProcessingTcs?.TrySetResult(true); // Signal completion to StopRecording() caller
            this._dictationModeStopSignal?.TrySetResult(true); // Also signal dictation mode stop
            CurrentVisualState = DictationVisualState.Idle;
            OnDebugMessage("[Audio] WaveSource_RecordingStopped - Signalled _currentStopProcessingTcs. Event EXITED.");
        }

        private async Task TranscribeAudioChunkAsync(Stream audioStream, bool isDictationModeChunk)
        {
            OnDebugMessage($"[Whisper] TranscribeAudioChunkAsync - Stream length: {audioStream.Length}");
            if (!_whisperService.IsInitialized)
            {
                OnDebugMessage("[Whisper] ERROR: WhisperFactory not initialized.");
                audioStream.Dispose();
                return;
            }
            if (audioStream.Length < (waveFormatForWhisper.AverageBytesPerSecond / 10))
            {
                OnDebugMessage("[Whisper] Audio chunk too short.");
                audioStream.Dispose();
                return;
            }

            try
            {
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

                var chunkSegmentsRaw = await _whisperService.TranscribeAsync(audioStream, promptText, OnSegmentTranscribed, OnDebugMessage);
                if (chunkSegmentsRaw.Count > 0)
                {
                    lock (currentSessionTranscribedText)
                    {
                        currentSessionTranscribedText.AddRange(chunkSegmentsRaw);
                    }
                }
            }
            catch (Exception ex)
            {
                OnDebugMessage($"[Whisper] Transcription error in chunk: {ex.Message}");
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

            return await _llmService.RefineTextAsync(inputText, Settings, setSystemPrompt, setUserPrompt, OnDebugMessage);
        }

        public void SetVadMode(int mode)
        {
            try
            {
                if (mode < 0) mode = 0;
                if (mode > 3) mode = 3;
                Settings.VadMode = mode;
                _vadService.SetMode(mode, OnDebugMessage);
                OnSettingsUpdated();
            }
            catch (Exception ex)
            {
                OnDebugMessage($"[VAD] SetVadMode error: {ex.Message}");
            }
        }

        // --- Public Methods for Form1 to Call ---
        public async Task<bool> StartRecordingAsync(int deviceNumber)
        {
            if (isRecording)
            {
                OnDebugMessage("[Audio] Already recording.");
                return false;
            }
            var availableMics = _audioCaptureService.GetAvailableMicrophones();
            if (availableMics.Count == 0)
            {
                OnDebugMessage("[Audio] No microphone detected.");
                return false;
            }
            if (deviceNumber < 0 || deviceNumber >= availableMics.Count)
            {
                OnDebugMessage("[Audio] Invalid device number.");
                deviceNumber = 0;
                Settings.SelectedMicrophoneDevice = 0;
            }

            if (!await InitializeWhisperAsync())
            {
                OnDebugMessage("[Whisper] Whisper init failed.");
                return false;
            }

            if (!_llmService.IsInitialized && Settings.ProcessWithLLM) // Pre-initialize LLM in background so we don't delay the mic
            {
                _ = Task.Run(() => _llmService.Initialize(Settings.LocalLLMModelPath, Settings.LLMContextSize, Settings.UseGpu, OnDebugMessage));
            }

            OnDebugMessage($"[Audio] Starting recording with mic [{deviceNumber}]...");
            lock (currentSessionTranscribedText)
            {
                currentSessionTranscribedText.Clear();
            }
            isRecording = true;
            StartNewChunk();

            _audioCaptureService.DataAvailable += WaveSource_DataAvailable;
            _audioCaptureService.RecordingStopped += WaveSource_RecordingStopped;

            try
            {
                _audioCaptureService.StartRecording(deviceNumber, waveFormatForWhisper);
                OnRecordingStateChanged(true);
                CurrentVisualState = DictationVisualState.ListeningSilent;
                return true;
            }
            catch (Exception ex)
            {
                OnDebugMessage($"[Audio] StartRecording failed: {ex.Message}");
                _audioCaptureService.DataAvailable -= WaveSource_DataAvailable;
                _audioCaptureService.RecordingStopped -= WaveSource_RecordingStopped;
                isRecording = false;
                OnRecordingStateChanged(false);
                CurrentVisualState = DictationVisualState.Idle;
                return false;
            }
        }

        public Task StopRecording()
        {
            if (!isRecording)
            {
                OnDebugMessage("[Audio] Not recording.");
                return Task.CompletedTask;
            }
            OnDebugMessage("[Audio] StopRecording called externally (e.g., from UI).");
            if (_currentStopProcessingTcs == null || _currentStopProcessingTcs.Task.IsCompleted)
            {
                _currentStopProcessingTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            _audioCaptureService.StopRecording();
            return _currentStopProcessingTcs.Task;
        }

        public async Task WaitForCurrentTranscriptionToCompleteAsync()
        {
            var lastWaitLog = DateTime.MinValue;

            while (activelyProcessingChunk)
            {
                if ((DateTime.UtcNow - lastWaitLog) > TimeSpan.FromSeconds(2))
                {
                    OnDebugMessage("[Whisper] Waiting for chunk processing pipeline to finish...");
                    lastWaitLog = DateTime.UtcNow;
                }
                await Task.Delay(50);
            }

            if (currentTranscriptionTask != null && !currentTranscriptionTask.IsCompleted)
            {
                OnDebugMessage("[App] Waiting for ongoing transcription to complete...");
                try
                {
                    await currentTranscriptionTask;
                }
                catch { /* ignore */ }
            }
        }

        public static List<(int Index, string Name)> GetAvailableMicrophones()
        {
            using (var tempCapture = new AudioCaptureService())
            {
                return tempCapture.GetAvailableMicrophones();
            }
        }

        public bool SelectMicrophone(int deviceIndex)
        {
            if (isRecording)
            {
                OnDebugMessage("[Audio] Cannot change mic while recording.");
                return false;
            }
            var availableMics = _audioCaptureService.GetAvailableMicrophones();
            if (availableMics.Count > 0 && deviceIndex >= 0 && deviceIndex < availableMics.Count)
            {
                if (Settings.SelectedMicrophoneDevice != deviceIndex)
                {
                    Settings.SelectedMicrophoneDevice = deviceIndex;
                    SaveAppSettings();
                    OnDebugMessage($"[Audio] Mic selected: [{deviceIndex}]");
                    OnSettingsUpdated();
                }
                return true;
            }
            OnDebugMessage($"[Audio] Invalid mic index: {deviceIndex} or no mics available.");
            return false;
        }

        public async Task<bool> ChangeModelPathAsync(string newModelPath) // For Whisper model
        {
            if (isRecording)
            {
                OnDebugMessage("[Whisper] Cannot change Whisper model while recording.");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(newModelPath) && File.Exists(newModelPath))
            {
                if (currentWhisperModelPath != newModelPath)
                {
                    currentWhisperModelPath = newModelPath;
                    Settings.ModelFilePath = newModelPath;
                    SaveAppSettings();
                    OnDebugMessage($"[Whisper] Whisper model path updated: {currentWhisperModelPath}");
                    await DisposeWhisperResourcesAsync();
                    OnSettingsUpdated();
                    return true;
                }
                OnDebugMessage("[Whisper] New Whisper model path is same as current.");
                return true;
            }
            OnDebugMessage("[Whisper] Invalid Whisper model path or file not found.");
            return false;
        }

        public async Task<bool> ChangeLLMModelPathAsync(string newLLMModelPath)
        {
            if (isRecording || activelyProcessingChunk)
            {
                OnDebugMessage("[LLM] Cannot change LLM model while busy.");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(newLLMModelPath) && File.Exists(newLLMModelPath))
            {
                if (Settings.LocalLLMModelPath != newLLMModelPath || !_llmService.IsInitialized)
                {
                    Settings.LocalLLMModelPath = newLLMModelPath;
                    SaveAppSettings();
                    OnDebugMessage($"[Settings] LLM model path updated: {Settings.LocalLLMModelPath}");
                    await DisposeLLMResourcesAsync();
                    OnSettingsUpdated();
                    return true;
                }
                OnDebugMessage("[LLM] New LLM model path is same as current.");
                return true;
            }
            OnDebugMessage("[LLM] Invalid LLM model path or file not found.");
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
                    try
                    {
                        chunkWaveFile?.Dispose();
                        currentAudioChunkStream?.Dispose();

                        _settingsService.SaveSettings(appSettingsFilePath, Settings, OnDebugMessage);

                        _audioCaptureService.Dispose();
                        _whisperService.Dispose();
                        _llmService.Dispose();
                        _vadService.Dispose();

                        CleanupDebugAudioFolder();
                    }
                    catch { }
                }

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
                OnDebugMessage("[Audio] Already recording (normal or dictation).");
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
                OnDebugMessage("[App] Not in active dictation mode to stop.");
                IsDictationModeActive = false; // Ensure it's false
                _dictationModeStopSignal?.TrySetResult(true); // Signal if anyone is waiting
                return;
            }

            OnDebugMessage("[App] Stopping dictation mode...");
            await StopRecording(); // This will use the _currentStopProcessingTcs

            if (_dictationModeStopSignal != null)
            {
                await _dictationModeStopSignal.Task; // Wait for RecordingStopped to signal this TCS
            }
            IsDictationModeActive = false; // Ensure final state
            OnDebugMessage("[App] Dictation mode fully stopped.");
        }
    }
}
