// TranscriptionService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using System.Text.Json;
using Whisper.net;
// Ensure AppSettings is accessible (e.g., if in its own file and namespace)
// using WhisperNetConsoleDemo; 

namespace WhisperNetConsoleDemo // Or your chosen namespace
    {
    public class TranscriptionService : IDisposable // Implement IDisposable for cleanup
        {
        // --- Constants ---
        private const double MAX_CHUNK_DURATION_SECONDS = 20.0;
        private const double SILENCE_THRESHOLD_SECONDS = 2.0;
        private const int SILENCE_DETECTION_BUFFER_MILLISECONDS = 250;
        public const int CALIBRATION_DURATION_SECONDS = 3;

        // --- Events for UI Updates ---
        public event Action<string, string>? SegmentTranscribed; // (timestampedText, rawText)
        public event Action<string>? FullTranscriptionReady;
        public event Action<string>? DebugMessageGenerated;
        public event Action<bool>? RecordingStateChanged;
        public event Action? SettingsUpdated; // To notify UI to refresh displayed settings

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
        private List<string> currentSessionTranscribedText = new List<string>();

        // --- Whisper & App Settings ---
        private string currentModelFilePath;
        private WhisperFactory? whisperFactoryInstance = null;
        private WhisperProcessor? whisperProcessorInstance = null;
        private WaveFormat waveFormatForWhisper = new WaveFormat(16000, 16, 1);
        private float calibratedEnergySilenceThreshold;

        public AppSettings Settings { get; private set; } = new AppSettings();
        private string appSettingsFilePath = "appsettings.json";

        private enum CalibrationStep
            {
            None, SamplingSilence, SamplingSpeech_Prompt, SamplingSpeech_Recording
            }
        private CalibrationStep currentCalibrationStep = CalibrationStep.None;
        private List<float> calibrationSamples = new List<float>();
        private bool isCalibrating = false; // Specific flag for calibration recording


        public TranscriptionService()
            {
            LoadAppSettings();
            // Initialize derived settings
            currentModelFilePath = Settings.ModelFilePath;
            calibratedEnergySilenceThreshold = Settings.CalibratedEnergySilenceThreshold;
            }

        private void OnDebugMessage(string message) => DebugMessageGenerated?.Invoke(message);
        private void OnSegmentTranscribed(string timestamped, string raw) => SegmentTranscribed?.Invoke(timestamped, raw);
        private void OnFullTranscriptionReady(string fullText) => FullTranscriptionReady?.Invoke(fullText);
        private void OnRecordingStateChanged(bool nowRecording) => RecordingStateChanged?.Invoke(nowRecording);
        private void OnSettingsUpdated() => SettingsUpdated?.Invoke();


        // --- Settings Management ---
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
                OnDebugMessage($"Loaded Model: {Settings.ModelFilePath}, Threshold: {Settings.CalibratedEnergySilenceThreshold}, Mic: {Settings.SelectedMicrophoneDevice}");
                }
            else
                {
                OnDebugMessage($"'{appSettingsFilePath}' not found. Using/Creating defaults.");
                SaveAppSettings(); // Create with defaults
                }
            // Apply to working variables
            currentModelFilePath = Settings.ModelFilePath;
            calibratedEnergySilenceThreshold = Settings.CalibratedEnergySilenceThreshold;
            OnSettingsUpdated();
            }

        public void SaveAppSettings()
            {
            OnDebugMessage("Saving application settings...");
            try
                {
                // Ensure Settings object has the latest runtime values before saving
                Settings.ModelFilePath = currentModelFilePath;
                Settings.CalibratedEnergySilenceThreshold = calibratedEnergySilenceThreshold;
                // SelectedMicrophoneDevice is updated within Settings object directly

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

        // --- Whisper Initialization ---
        public async Task<bool> InitializeWhisperAsync()
            {
            if (whisperProcessorInstance != null)
                return true;
            if (string.IsNullOrWhiteSpace(currentModelFilePath) || !File.Exists(currentModelFilePath))
                {
                OnDebugMessage("InitializeWhisperAsync: Model path invalid or file not found.");
                return false;
                }
            OnDebugMessage("Initializing WhisperFactory and WhisperProcessor...");
            try
                {
                await DisposeWhisperResourcesAsync(); // Ensure old ones are gone

                whisperFactoryInstance = WhisperFactory.FromPath(currentModelFilePath);
                whisperProcessorInstance = whisperFactoryInstance.CreateBuilder()
                    .WithLanguage("auto") // Or from Settings
                    .Build();
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

        public async Task DisposeWhisperResourcesAsync() // Changed to async
            {
            OnDebugMessage("Disposing WhisperProcessor and WhisperFactory (async).");
            if (whisperProcessorInstance != null)
                {
                await whisperProcessorInstance.DisposeAsync();
                whisperProcessorInstance = null;
                }
            whisperFactoryInstance?.Dispose(); // Factory is usually IDisposable only
            whisperFactoryInstance = null;
            }


        // --- Core Recording and Transcription Logic ---
        private void StartNewChunk()
            {
            try
                {
                chunkWaveFile?.Dispose();
                }
            catch { }
            try
                {
                currentAudioChunkStream?.Dispose();
                }
            catch { }
            currentAudioChunkStream = new MemoryStream();
            chunkWaveFile = new WaveFileWriter(currentAudioChunkStream, waveFormatForWhisper); // This will own the stream for writing
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
                return; // Check isCalibrating
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
                if (currentAudioChunkStream.Length > 0 && (DateTime.UtcNow - lastSpeechTime) > TimeSpan.FromSeconds(SILENCE_THRESHOLD_SECONDS))
                    silenceDetectedRecently = true;
                }

            TimeSpan chunkDur = DateTime.UtcNow - chunkStartTime;
            bool process = false;
            if (currentAudioChunkStream.Length > (waveFormatForWhisper.AverageBytesPerSecond / 2))
                {
                if (chunkDur >= TimeSpan.FromSeconds(MAX_CHUNK_DURATION_SECONDS))
                    {
                    OnDebugMessage($"Max duration reached.");
                    process = true;
                    }
                else if (silenceDetectedRecently)
                    {
                    OnDebugMessage($"Silence detected, processing.");
                    process = true;
                    }
                }
            if (!activelyProcessingChunk && process)
                {
                activelyProcessingChunk = true;
                OnDebugMessage($"Chunk ready. Dur: {chunkDur.TotalSeconds:F1}s, Silence: {silenceDetectedRecently}, Len: {currentAudioChunkStream.Length}");
                MemoryStream? streamToSend = null;
                WaveFileWriter? tempChunkFile = chunkWaveFile; // Capture current writer
                MemoryStream? tempAudioChunkStream = currentAudioChunkStream; // Capture current stream

                chunkWaveFile = null; // Null out to prevent further writes by this instance of DataAvailable
                currentAudioChunkStream = null; // This stream will be processed

                try
                    {
                    tempChunkFile?.Flush(); // Flush the captured writer
                    if (tempAudioChunkStream != null)
                        {
                        tempAudioChunkStream.Position = 0;
                        streamToSend = new MemoryStream();
                        await tempAudioChunkStream.CopyToAsync(streamToSend);
                        streamToSend.Position = 0;
                        }
                    }
                catch (Exception ex) { OnDebugMessage($"Error prep stream: {ex.Message}"); activelyProcessingChunk = false; streamToSend?.Dispose(); tempChunkFile?.Dispose(); tempAudioChunkStream?.Dispose(); return; }
                finally
                    {
                    tempChunkFile?.Dispose(); // This will dispose tempAudioChunkStream
                    }

                StartNewChunk(); // Prepare for the *next* segment of audio immediately

                if (streamToSend != null && streamToSend.Length > 0)
                    {
                    currentTranscriptionTask = TranscribeAudioChunkAsync(streamToSend); // Renamed
                    try
                        {
                        await currentTranscriptionTask;
                        }
                    catch (Exception ex) { OnDebugMessage($"Err transcription task: {ex.Message}"); }
                    finally { currentTranscriptionTask = null; activelyProcessingChunk = false; }
                    }
                else
                    {
                    activelyProcessingChunk = false; // No stream to send
                    streamToSend?.Dispose(); // Dispose if created but empty
                    }
                }
            }

        private async void WaveSource_RecordingStopped(object? sender, StoppedEventArgs e)
            {
            OnDebugMessage("WaveSource_RecordingStopped - Event ENTERED.");
            bool wasRec = isRecording;
            isRecording = false;
            OnRecordingStateChanged(false);
            MemoryStream? finalActiveStream = currentAudioChunkStream;
            WaveFileWriter? finalFile = chunkWaveFile;
            currentAudioChunkStream = null;
            chunkWaveFile = null;

            if (sender is WaveInEvent ws)
                {
                ws.DataAvailable -= WaveSource_DataAvailable;
                ws.RecordingStopped -= WaveSource_RecordingStopped;
                try
                    {
                    ws.Dispose();
                    }
                catch (Exception ex) { OnDebugMessage($"Err disposing WaveInEvent: {ex.Message}"); }
                }
            if (ReferenceEquals(sender, waveSource))
                waveSource = null;

            MemoryStream? streamToTranscribe = null;
            if (finalFile != null && finalActiveStream != null)
                {
                try
                    {
                    finalFile.Flush();
                    if (wasRec && finalActiveStream.Length > (waveFormatForWhisper.AverageBytesPerSecond / 10))
                        {
                        finalActiveStream.Position = 0;
                        streamToTranscribe = new MemoryStream();
                        await finalActiveStream.CopyToAsync(streamToTranscribe);
                        streamToTranscribe.Position = 0;
                        }
                    }
                catch (Exception ex) { OnDebugMessage($"Err flush/copy final: {ex.Message}"); }
                try
                    {
                    finalFile.Dispose();
                    }
                catch (Exception ex) { OnDebugMessage($"Err disposing finalFile: {ex.Message}"); }
                }
            if (finalFile == null || (finalActiveStream != null && finalActiveStream.CanRead))
                {
                try
                    {
                    finalActiveStream?.Dispose();
                    }
                catch { }
                }

            Task? finalTranscribeTask = null;
            if (wasRec && streamToTranscribe != null && streamToTranscribe.Length > 0)
                {
                activelyProcessingChunk = true;
                finalTranscribeTask = TranscribeAudioChunkAsync(streamToTranscribe);
                currentTranscriptionTask = finalTranscribeTask;
                try
                    {
                    await finalTranscribeTask;
                    }
                catch (Exception ex) { OnDebugMessage($"Err FINAL transcription: {ex.Message}"); }
                finally { if (ReferenceEquals(currentTranscriptionTask, finalTranscribeTask)) currentTranscriptionTask = null; activelyProcessingChunk = false; }
                }
            else if (wasRec)
                {
                OnDebugMessage("No audio in FINAL chunk.");
                }
            if (finalTranscribeTask == null && streamToTranscribe != null && streamToTranscribe.CanRead)
                {
                try
                    {
                    streamToTranscribe.Dispose();
                    }
                catch { }
                }

            if (wasRec)
                {
                string fullText;
                lock (currentSessionTranscribedText)
                    {
                    fullText = string.Join(" ", currentSessionTranscribedText).Trim();
                    }
                var knownPlaceholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "[BLANK_AUDIO]", "(silence)", "[ Silence ]", "...", "[INAUDIBLE]", "[MUSIC PLAYING]", "[SOUND]", "[CLICK]" };
                var segmentsToPrint = currentSessionTranscribedText.Select(segment => segment.Trim()).Where(trimmedSegment => !string.IsNullOrWhiteSpace(trimmedSegment) && !knownPlaceholders.Contains(trimmedSegment) && !(trimmedSegment.StartsWith("[") && trimmedSegment.EndsWith("]") && trimmedSegment.Length <= 25)).ToList();
                fullText = string.Join(" ", segmentsToPrint).Trim();
                OnFullTranscriptionReady(fullText); // Raise event
                }
            if (e.Exception != null)
                {
                OnDebugMessage($"NAudio stop exception: {e.Exception.Message}");
                }
            // stopProcessingTcs?.TrySetResult(true); // If Form1 uses TCS for coordination
            }

        private async Task TranscribeAudioChunkAsync(Stream audioStream) // Changed name
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
                OnDebugMessage("Audio stream too short, skipping.");
                audioStream.Dispose();
                return;
                }

            try
                {
                await Task.Yield();
                OnDebugMessage("Processing audio chunk...");
                List<string> chunkSegmentsRaw = new List<string>();
                await foreach (var segment in whisperProcessorInstance.ProcessAsync(audioStream))
                    {
                    string timestampedText = $"[{segment.Start.TotalSeconds:F2}s -> {segment.End.TotalSeconds:F2}s]: {segment.Text}";
                    string rawText = segment.Text.Trim();
                    OnSegmentTranscribed(timestampedText, rawText); // Raise event for each segment
                    chunkSegmentsRaw.Add(rawText);
                    }
                lock (currentSessionTranscribedText)
                    {
                    currentSessionTranscribedText.AddRange(chunkSegmentsRaw);
                    }
                }
            catch (Exception ex) { OnDebugMessage($"Transcription error in chunk: {ex.Message}"); }
            finally { await audioStream.DisposeAsync(); } // Use DisposeAsync for streams where possible
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

        public void StopRecording()
            {
            if (!isRecording || waveSource == null)
                {
                OnDebugMessage("Not recording or waveSource is null.");
                return;
                }
            OnDebugMessage("StopRecording called externally (e.g., from UI).");
            waveSource.StopRecording(); // Triggers RecordingStopped event
                                        // isRecording will be set to false in RecordingStopped
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
                catch { /* ignore errors here, already logged */ }
                }
            }


        // --- Calibration Logic (can be adapted similarly with events) ---
        // TranscriptionService.cs

        // (Make sure CALIBRATION_DURATION_SECONDS is a public const if Form1 needs it for display)
        // public const int CALIBRATION_DURATION_SECONDS = 3;

        // TranscriptionService.cs

        public async Task CalibrateThresholdsAsync(int deviceNumber, Action<string> statusUpdateCallback)
            {
            statusUpdateCallback("--- Starting Silence Threshold Calibration ---");
            WaveInEvent? calWaveIn = null;
            if (isRecording)
                {
                statusUpdateCallback("Cannot calibrate while main recording is active. Please stop first.");
                return;
                }

            isCalibrating = true;
            float typicalSilenceLevel = 0.001f; // Initialize with a default
            float typicalSpeechLevel = 0.1f;    // Initialize with a default

            try
                {
                // --- Step 1: Sample Silence ---
                currentCalibrationStep = CalibrationStep.SamplingSilence;
                calibrationSamples.Clear();
                statusUpdateCallback($"Please remain completely silent for {CALIBRATION_DURATION_SECONDS}s...");
                for (int i = CALIBRATION_DURATION_SECONDS; i > 0; i--)
                    {
                    statusUpdateCallback($"Sampling silence in {i}... ");
                    if (i == 1)
                        await Task.Delay(700);
                    else
                        await Task.Delay(1000); // Shorter delay before NOW
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
                    var orderedSamples = calibrationSamples.OrderBy(x => x).ToList();
                    typicalSilenceLevel = orderedSamples.ElementAtOrDefault((int)(orderedSamples.Count * 0.95));
                    if (typicalSilenceLevel < 0.00001f && typicalSilenceLevel >= 0)
                        typicalSilenceLevel = 0.0001f;
                    else if (typicalSilenceLevel < 0)
                        typicalSilenceLevel = 0.0001f; // Should not happen with Math.Abs
                    }
                else
                    {
                    statusUpdateCallback("No silence samples collected. Using low default for silence level.");
                    // typicalSilenceLevel keeps its initialized default
                    }
                OnDebugMessage($"Calibrate: Typical silence (95th percentile): {typicalSilenceLevel:F4}");
                statusUpdateCallback($" (Detected silence level: {typicalSilenceLevel:F4})");


                // --- Step 2: Sample Speech ---
                currentCalibrationStep = CalibrationStep.SamplingSpeech_Prompt;
                calibrationSamples.Clear();
                statusUpdateCallback($"\nNow, please speak normally for {CALIBRATION_DURATION_SECONDS}s...");
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
                // calWaveIn will be disposed in the finally block

                if (calibrationSamples.Count > 0)
                    {
                    var orderedSamples = calibrationSamples.OrderBy(x => x).ToList();
                    typicalSpeechLevel = orderedSamples.ElementAtOrDefault((int)(orderedSamples.Count * 0.10));
                    if (typicalSpeechLevel < 0.001f && typicalSpeechLevel >= 0)
                        typicalSpeechLevel = 0.05f;
                    else if (typicalSpeechLevel < 0)
                        typicalSpeechLevel = 0.05f; // Should not happen
                    }
                else
                    {
                    statusUpdateCallback("No speech samples collected. Using default for speech level.");
                    // typicalSpeechLevel keeps its initialized default
                    }
                OnDebugMessage($"Calibrate: Typical speech (10th percentile): {typicalSpeechLevel:F4}");
                statusUpdateCallback($" (Detected speech level: {typicalSpeechLevel:F4})");


                // --- Step 3: Calculate and Set New Threshold ---
                if (typicalSpeechLevel <= typicalSilenceLevel + 0.005f)
                    {
                    statusUpdateCallback("Warning: Speech level not significantly louder than silence. Calibration may be inaccurate.");
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
                calibratedEnergySilenceThreshold = Math.Max(0.002f, Math.Min(0.35f, calibratedEnergySilenceThreshold)); // Clamp

                Settings.CalibratedEnergySilenceThreshold = calibratedEnergySilenceThreshold;
                SaveAppSettings(); // Save to appSettings object and file
                statusUpdateCallback($"--- Calibration Complete ---\nNew Threshold: {calibratedEnergySilenceThreshold:F4} (Saved)");
                }
            catch (Exception ex)
                {
                statusUpdateCallback($"Calibration Error: {ex.Message}");
                OnDebugMessage($"Full Calibration Exception: {ex.ToString()}"); // Log full exception for debugging
                }
            finally
                {
                isCalibrating = false;
                currentCalibrationStep = CalibrationStep.None;
                calWaveIn?.Dispose();
                OnDebugMessage("Calibration process finished in service.");
                OnSettingsUpdated(); // Notify UI that settings (threshold) might have changed
                }
            }
        private void CollectCalibrationSamples_Handler(object? sender, WaveInEventArgs e)
            {
            if (!isCalibrating || !(currentCalibrationStep == CalibrationStep.SamplingSilence || currentCalibrationStep == CalibrationStep.SamplingSpeech_Recording))
                return;
            // This method's VAD logic is the same as before, adds to `calibrationSamples`
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


        // --- Microphone and Model Management ---
        public List<(int Index, string Name)> GetAvailableMicrophones()
            {
            var mics = new List<(int, string)>();
            for (int i = 0; i < WaveIn.DeviceCount; i++)
                {
                try
                    {
                    mics.Add((i, WaveIn.GetCapabilities(i).ProductName));
                    }
                catch { mics.Add((i, $"Error reading mic {i}")); }
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
            if (deviceIndex >= 0 && deviceIndex < WaveIn.DeviceCount)
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
            OnDebugMessage($"Invalid mic index: {deviceIndex}");
            return false;
            }

        public async Task<bool> ChangeModelPathAsync(string newModelPath)
            {
            if (isRecording)
                {
                OnDebugMessage("Cannot change model while recording.");
                return false;
                }
            if (!string.IsNullOrWhiteSpace(newModelPath) && File.Exists(newModelPath))
                {
                if (currentModelFilePath != newModelPath)
                    {
                    currentModelFilePath = newModelPath;
                    Settings.ModelFilePath = newModelPath;
                    SaveAppSettings();
                    OnDebugMessage($"Model path updated to: {currentModelFilePath}");
                    await DisposeWhisperResourcesAsync(); // Dispose old to force re-init
                    OnSettingsUpdated();
                    return true;
                    }
                OnDebugMessage("New model path is same as current.");
                return true; // No change but valid
                }
            OnDebugMessage("Invalid model path or file not found.");
            return false;
            }


        // --- IDisposable ---
        private bool disposedValue;
        protected virtual async Task DisposeAsync(bool disposing) // Changed to async
            {
            if (!disposedValue)
                {
                if (disposing)
                    {
                    OnDebugMessage("TranscriptionService DisposeAsync called.");
                    if (isRecording && waveSource != null)
                        {
                        waveSource.StopRecording(); // This is async void, ideally needs more robust shutdown
                        }
                    waveSource?.Dispose();
                    chunkWaveFile?.Dispose(); // This will dispose currentAudioChunkStream due to WaveFileWriter behavior
                    currentAudioChunkStream?.Dispose(); // Explicit just in case

                    await DisposeWhisperResourcesAsync();
                    }
                disposedValue = true;
                }
            }

        public async ValueTask DisposeAsync() // Implement IAsyncDisposable if class implements it
            {
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
            }

        public void Dispose()
            {
            DisposeAsync(disposing: true).GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
            }
        }
    }
