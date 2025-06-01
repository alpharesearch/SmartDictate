// Program.cs
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using NAudio.Wave;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using WhisperNetConsoleDemo; // Assuming AppSettings class is in this namespace

public class Program
{
    // Constants
    private const double MAX_CHUNK_DURATION_SECONDS = 20.0;
    private const double SILENCE_THRESHOLD_SECONDS = 2.0;
    // These now get their initial values from AppSettings defaults via the appSettings field initializer
    // public const float DEFAULT_ENERGY_SILENCE_THRESHOLD = AppSettings.APPSETTINGS_DEFAULT_ENERGY_THRESHOLD;
    // public const string DEFAULT_MODEL_FILE_PATH = AppSettings.APPSETTINGS_DEFAULT_MODEL_PATH;
    private const int SILENCE_DETECTION_BUFFER_MILLISECONDS = 250;
    private const int CALIBRATION_DURATION_SECONDS = 3;

    // State Fields
    private static bool isRecording = false;
    private static WaveInEvent? waveSource = null;
    private static MemoryStream? currentAudioChunkStream = null;
    private static WaveFileWriter? chunkWaveFile = null;
    private static DateTime chunkStartTime = DateTime.MinValue;
    private static DateTime lastSpeechTime = DateTime.MinValue;
    private static bool silenceDetectedRecently = false;
    private static byte[] silenceDetectionBuffer = new byte[0];
    private static int silenceDetectionBufferBytesRecorded = 0;
    private static bool activelyProcessingChunk = false;
    private static Task? currentTranscriptionTask = null;
    private static List<string> currentSessionTranscribedText = new List<string>();

    // Whisper & App Settings
    private static string currentModelFilePath; // Initialized by LoadAppSettings
    private static WhisperFactory? whisperFactoryInstance = null;
    private static WhisperProcessor? whisperProcessorInstance = null;
    private static WaveFormat waveFormatForWhisper = new WaveFormat(16000, 16, 1);
    private static float calibratedEnergySilenceThreshold; // Initialized by LoadAppSettings

    private enum CalibrationStep { None, SamplingSilence, SamplingSpeech_Prompt, SamplingSpeech_Recording }
    private static CalibrationStep currentCalibrationStep = CalibrationStep.None;
    private static List<float> calibrationSamples = new List<float>();
    private static AppSettings appSettings = new AppSettings(); // Initializes with AppSettings class defaults
    private static string appSettingsFilePath = "appsettings.json";

    // Shutdown coordination
    private static bool exitRequested = false;
    private static TaskCompletionSource<bool>? stopProcessingTcs = null;


    private static void DebugLog(string message)
    {
        if (appSettings.ShowDebugMessages)
        {
            Console.WriteLine($"DEBUG: {message}");
        }
    }

    private static void LoadAppSettings()
    {
        Console.WriteLine("INFO: Loading application settings...");
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(appSettingsFilePath, optional: true, reloadOnChange: false);

        IConfigurationRoot configurationRoot = builder.Build();
        var settingsSection = configurationRoot.GetSection("AppSettings");

        if (settingsSection.Exists())
        {
            settingsSection.Bind(appSettings); // Populates our appSettings instance
            Console.WriteLine($"INFO: Loaded ModelFilePath: {appSettings.ModelFilePath}");
            Console.WriteLine($"INFO: Loaded CalibratedEnergySilenceThreshold: {appSettings.CalibratedEnergySilenceThreshold}");
            DebugLog($"Loaded SelectedMicrophoneDevice: {appSettings.SelectedMicrophoneDevice}");
            DebugLog($"Loaded ShowRealtimeTranscription: {appSettings.ShowRealtimeTranscription}");
            DebugLog($"Loaded ShowDebugMessages: {appSettings.ShowDebugMessages}");
        }
        else
        {
            Console.WriteLine($"INFO: '{appSettingsFilePath}' or AppSettings section not found. Using default settings and creating file.");
            // appSettings instance already has defaults from its class definition
            SaveAppSettings(); // Create the file with these defaults
        }
        // Apply loaded settings to runtime variables
        currentModelFilePath = appSettings.ModelFilePath;
        calibratedEnergySilenceThreshold = appSettings.CalibratedEnergySilenceThreshold;
    }

    private static void SaveAppSettings()
    {
        DebugLog("Saving application settings...");
        try
        {
            // Ensure appSettings object reflects current runtime values before saving
            appSettings.ModelFilePath = currentModelFilePath; // Already updated by ChangeModelPath
            appSettings.CalibratedEnergySilenceThreshold = calibratedEnergySilenceThreshold; // Already updated by CalibrateThresholds
            // appSettings.SelectedMicrophoneDevice is updated directly when user selects in SelectMicrophone or Main startup

            var configurationToSave = new { AppSettings = appSettings };
            string json = JsonSerializer.Serialize(configurationToSave, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(appSettingsFilePath, json);
            DebugLog($"Settings saved to {Path.GetFullPath(appSettingsFilePath)}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error saving app settings: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task TranscribeAudioStream(Stream audioStream)
    {
        DebugLog($"TranscribeAudioStream - Received stream length: {audioStream.Length}");
        if (whisperProcessorInstance == null)
        {
            Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("ERROR: WhisperProcessor not initialized."); Console.ResetColor();
            audioStream.Dispose(); return;
        }
        if (audioStream.Length < (waveFormatForWhisper.AverageBytesPerSecond / 10))
        {
            DebugLog("TranscribeAudioStream - Audio stream too short, skipping.");
            audioStream.Dispose(); return;
        }

        try
        {
            await Task.Yield();
            DebugLog("Processing audio stream with existing WhisperProcessor...");
            List<string> newSegments = new List<string>();
            await foreach (var segment in whisperProcessorInstance.ProcessAsync(audioStream))
            {
                if (appSettings.ShowRealtimeTranscription)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"[{segment.Start.TotalSeconds:F2}s -> {segment.End.TotalSeconds:F2}s]: ");
                    Console.ResetColor(); Console.WriteLine(segment.Text);
                }
                newSegments.Add(segment.Text.Trim());
            }
            lock (currentSessionTranscribedText) { currentSessionTranscribedText.AddRange(newSegments); }
        }
        catch (Exception ex) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"\nTranscription error: {ex.Message}"); Console.ResetColor(); }
        finally { audioStream.Dispose(); }
    }

    private static void StartNewChunk()
    {
        try { chunkWaveFile?.Dispose(); } catch { /* ignore */ } // Disposes underlying stream if not careful
        try { currentAudioChunkStream?.Dispose(); } catch { /* ignore */ }

        currentAudioChunkStream = new MemoryStream();
        // WaveFileWriter will dispose currentAudioChunkStream when chunkWaveFile is disposed
        chunkWaveFile = new WaveFileWriter(currentAudioChunkStream, waveFormatForWhisper);
        chunkStartTime = DateTime.UtcNow;
        lastSpeechTime = DateTime.UtcNow;
        silenceDetectedRecently = false;

        int bytesPerSample = waveFormatForWhisper.BitsPerSample / 8;
        int samplesPerSilenceBuffer = waveFormatForWhisper.SampleRate * SILENCE_DETECTION_BUFFER_MILLISECONDS / 1000;
        int bytesPerSilenceDetectionBuffer = samplesPerSilenceBuffer * bytesPerSample * waveFormatForWhisper.Channels;
        if (bytesPerSilenceDetectionBuffer == 0 && waveFormatForWhisper.AverageBytesPerSecond > 0)
        { bytesPerSilenceDetectionBuffer = waveFormatForWhisper.BlockAlign > 0 ? waveFormatForWhisper.BlockAlign : 2; }
        else if (waveFormatForWhisper.BlockAlign > 0 && bytesPerSilenceDetectionBuffer % waveFormatForWhisper.BlockAlign != 0)
        { bytesPerSilenceDetectionBuffer = ((bytesPerSilenceDetectionBuffer / waveFormatForWhisper.BlockAlign) + 1) * waveFormatForWhisper.BlockAlign; }
        silenceDetectionBuffer = new byte[bytesPerSilenceDetectionBuffer];
        silenceDetectionBufferBytesRecorded = 0;
    }

    public static async Task Main(string[] args)
    {
        LoadAppSettings();
        Console.WriteLine("Whisper.net Console Demo - Live Recording with Silence Detection");

        if (!File.Exists(currentModelFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"Model file not found: {Path.GetFullPath(currentModelFilePath)}"); Console.ResetColor(); return;
        }

        int deviceNumber = appSettings.SelectedMicrophoneDevice; // Use loaded value
        try
        {
            if (WaveIn.DeviceCount == 0)
            {
                if (currentCalibrationStep == CalibrationStep.None)
                { // Allow calibration to proceed and handle no mic itself
                    Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("No microphones found! Connect mic & restart, or [K] to rescan."); Console.ResetColor();
                }
            }
            else if (deviceNumber < 0 || deviceNumber >= WaveIn.DeviceCount)
            {
                Console.WriteLine($"Warning: Saved mic device {deviceNumber} invalid. Defaulting to 0.");
                deviceNumber = 0; appSettings.SelectedMicrophoneDevice = 0;
            }

            if (WaveIn.DeviceCount > 0)
            {
                Console.WriteLine($"Using mic: [{deviceNumber}] {WaveIn.GetCapabilities(deviceNumber).ProductName} (from settings/default)");
            }
            else if (currentCalibrationStep == CalibrationStep.None)
            { // Only show this critical warning if not in calibration
                Console.WriteLine("No microphone currently selected or available. Use [K] if one becomes available.");
            }


            PrintInstructions();

            while (!exitRequested)
            {
                if (currentCalibrationStep != CalibrationStep.None && !isRecording)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
                    { Console.WriteLine("Calibration aborted by Q. Requesting exit..."); currentCalibrationStep = CalibrationStep.None; exitRequested = true; }
                    await Task.Delay(50); continue;
                }

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("Q pressed. Requesting exit..."); exitRequested = true;
                        if (isRecording && waveSource != null)
                        {
                            DebugLog("Q - Actively recording, initiating stop sequence.");
                            stopProcessingTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                            waveSource.StopRecording(); DebugLog("Q - Waiting for recording stop and final processing...");
                            await stopProcessingTcs.Task; DebugLog("Q - Recording stop processing confirmed complete.");
                        }
                        // No break here, !exitRequested loop condition handles it
                    }
                    else if (key.Key == ConsoleKey.K && !isRecording) { SelectMicrophone(); deviceNumber = appSettings.SelectedMicrophoneDevice; }
                    else if (key.Key == ConsoleKey.M && !isRecording) { ChangeModelPath(); PrintInstructions(); }
                    else if (key.Key == ConsoleKey.C && !isRecording)
                    {
                        if (WaveIn.DeviceCount == 0) { Console.WriteLine("Cannot calibrate: No microphones detected."); continue; }
                        if (appSettings.SelectedMicrophoneDevice < 0) { Console.WriteLine("Cannot calibrate: No valid mic selected. Use [K]."); continue; }
                        await CalibrateThresholds(appSettings.SelectedMicrophoneDevice); PrintInstructions();
                    }
                    else if (key.Key == ConsoleKey.R && !isRecording)
                    {
                        if (WaveIn.DeviceCount == 0) { Console.WriteLine("Cannot record: No microphones detected."); continue; }
                        deviceNumber = appSettings.SelectedMicrophoneDevice; // Ensure using latest from settings
                        if (deviceNumber < 0) { Console.WriteLine("Cannot record: No valid mic selected. Use [K]."); continue; }

                        Console.WriteLine($"\nStarting continuous recording with mic [{deviceNumber}] {WaveIn.GetCapabilities(deviceNumber).ProductName}...");
                        currentSessionTranscribedText.Clear(); exitRequested = false;

                        if (whisperProcessorInstance == null)
                        {
                            DebugLog("Initializing WhisperFactory and WhisperProcessor...");
                            try
                            {
                                whisperFactoryInstance = WhisperFactory.FromPath(currentModelFilePath);
                                whisperProcessorInstance = whisperFactoryInstance.CreateBuilder().WithLanguage("auto").Build();
                                DebugLog("WhisperFactory and WhisperProcessor initialized.");
                            }
                            catch (Exception ex) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"FATAL: Whisper init error: {ex.Message}"); Console.ResetColor(); continue; }
                        }
                        isRecording = true; currentTranscriptionTask = null; StartNewChunk();
                        waveSource = new WaveInEvent { DeviceNumber = deviceNumber, WaveFormat = waveFormatForWhisper };
                        waveSource.DataAvailable += WaveSource_DataAvailable; waveSource.RecordingStopped += WaveSource_RecordingStopped;
                        waveSource.StartRecording(); Console.WriteLine("Recording continuously... Silence or 'S' will process/stop.");
                    }
                    else if (key.Key == ConsoleKey.S && isRecording)
                    {
                        Console.WriteLine("S pressed. Stopping recording session...");
                        if (waveSource != null)
                        {
                            stopProcessingTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                            waveSource.StopRecording(); DebugLog("S - Waiting for recording stop and final processing...");
                            await stopProcessingTcs.Task; DebugLog("S - Recording stop processing confirmed complete.");
                        }
                    }
                    else if (key.Key == ConsoleKey.R && isRecording) { Console.WriteLine("Already recording."); }
                }
                await Task.Delay(50);
            }
            DebugLog("Main loop exited.");
            SaveAppSettings();
        }
        finally
        {
            DebugLog("Main finally block reached.");
            if (whisperProcessorInstance != null)
            {
                DebugLog("Attempting to dispose WhisperProcessor asynchronously...");
                await whisperProcessorInstance.DisposeAsync(); DebugLog("WhisperProcessor disposed.");
            }
            if (whisperFactoryInstance != null)
            {
                DebugLog("Attempting to dispose WhisperFactory...");
                whisperFactoryInstance.Dispose(); DebugLog("WhisperFactory disposed.");
            }
            waveSource?.Dispose(); chunkWaveFile?.Dispose(); currentAudioChunkStream?.Dispose();
            Console.WriteLine("\nApplication fully exited.");
        }
    }

    private static void PrintInstructions()
    {
        Console.WriteLine($"\n[R] Record | [S] Stop | [C] Calibrate | [M] Model | [K] Mic | [Q] Quit");
        Console.WriteLine($"Model: {Path.GetFileName(currentModelFilePath)} | Threshold: {calibratedEnergySilenceThreshold:F4}");
        Console.WriteLine($"Realtime TX: {(appSettings.ShowRealtimeTranscription ? "ON" : "OFF")} | Debug: {(appSettings.ShowDebugMessages ? "ON" : "OFF")}");
        string micInfo = "No microphones detected.";
        if (WaveIn.DeviceCount > 0)
        {
            int currentMicDevice = appSettings.SelectedMicrophoneDevice;
            if (currentMicDevice >= 0 && currentMicDevice < WaveIn.DeviceCount)
            {
                try { micInfo = $"Mic: [{currentMicDevice}] {WaveIn.GetCapabilities(currentMicDevice).ProductName}"; } catch { micInfo = $"Mic: [{currentMicDevice}] Error"; }
            }
            else
            {
                micInfo = "Mic: Invalid selection in settings. Will use default [0].";
            }
        }
        Console.WriteLine(micInfo);
    }

    private static void SelectMicrophone()
    {
        if (isRecording) { Console.WriteLine("\nCannot change microphone while recording. Press 'S' to stop first."); return; }
        Console.WriteLine("\n--- Select Microphone ---");
        if (WaveIn.DeviceCount == 0) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("No microphones found!"); Console.ResetColor(); return; }

        int currentDeviceNum = appSettings.SelectedMicrophoneDevice;
        if (currentDeviceNum < 0 || currentDeviceNum >= WaveIn.DeviceCount) currentDeviceNum = 0;

        Console.WriteLine("Available microphones:");
        string currentMicName = "Unknown";
        try { if (WaveIn.DeviceCount > 0) currentMicName = WaveIn.GetCapabilities(currentDeviceNum).ProductName; } catch { }
        for (int i = 0; i < WaveIn.DeviceCount; i++)
        {
            var caps = WaveIn.GetCapabilities(i);
            Console.WriteLine($"{i}: {caps.ProductName} {(i == currentDeviceNum ? "[Current Selection]" : "")}");
        }
        Console.Write($"\nSelect new microphone (Enter to keep '{currentMicName}' [{currentDeviceNum}]): ");
        string? micInput = Console.ReadLine();
        bool selectionChanged = false;
        if (!string.IsNullOrWhiteSpace(micInput) && int.TryParse(micInput, out int parsedMicNum) && parsedMicNum >= 0 && parsedMicNum < WaveIn.DeviceCount)
        {
            if (appSettings.SelectedMicrophoneDevice != parsedMicNum)
            { appSettings.SelectedMicrophoneDevice = parsedMicNum; selectionChanged = true; Console.WriteLine($"Mic set to: {WaveIn.GetCapabilities(parsedMicNum).ProductName}"); }
            else { Console.WriteLine($"Keeping current mic: {currentMicName}"); }
        }
        else { Console.WriteLine($"No/invalid input. Keeping current mic: {currentMicName}"); }
        if (selectionChanged) SaveAppSettings();
        PrintInstructions();
    }

    private static void ChangeModelPath()
    {
        Console.WriteLine("\n--- Change Whisper Model ---");
        Console.WriteLine($"Current model: {currentModelFilePath}");
        Console.Write("New model path (e.g., ggml-small.bin) or Enter to cancel: ");
        string? newPath = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(newPath) && File.Exists(newPath))
        {
            if (currentModelFilePath != newPath)
            {
                currentModelFilePath = newPath; appSettings.ModelFilePath = newPath; SaveAppSettings();
                Console.WriteLine($"Model updated: {currentModelFilePath}");
                DebugLog("Disposing Whisper instances for model reload...");
                whisperProcessorInstance?.Dispose(); whisperFactoryInstance?.Dispose();
                whisperProcessorInstance = null; whisperFactoryInstance = null;
                DebugLog("Whisper instances cleared. New model loads on next 'R'.");
            }
            else { Console.WriteLine("Same model path. No change."); }
        }
        else if (!string.IsNullOrWhiteSpace(newPath)) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Invalid path/file not found."); Console.ResetColor(); }
        else { Console.WriteLine("Model change cancelled."); }
    }

    private static async Task CalibrateThresholds(int deviceNumber)
    {
        Console.WriteLine("\n--- Starting Silence Threshold Calibration ---");
        WaveInEvent? calWaveIn = null;
        try
        {
            currentCalibrationStep = CalibrationStep.SamplingSilence; calibrationSamples.Clear();
            Console.WriteLine($"Remain silent for {CALIBRATION_DURATION_SECONDS}s...");
            Console.Write("Sampling silence in 3... "); await Task.Delay(1000); Console.Write("2... "); await Task.Delay(1000); Console.Write("1... "); await Task.Delay(1000); Console.WriteLine("NOW.");
            calWaveIn = new WaveInEvent { DeviceNumber = deviceNumber, WaveFormat = waveFormatForWhisper };
            calWaveIn.DataAvailable += CollectCalibrationSamples_Handler;
            isRecording = true; calWaveIn.StartRecording(); await Task.Delay(CALIBRATION_DURATION_SECONDS * 1000); calWaveIn.StopRecording(); isRecording = false;
            calWaveIn.DataAvailable -= CollectCalibrationSamples_Handler; calWaveIn.Dispose(); calWaveIn = null;

            float typicalSilence;
            if (calibrationSamples.Count > 0)
            { var ordered = calibrationSamples.OrderBy(x => x).ToList(); typicalSilence = ordered.ElementAtOrDefault((int)(ordered.Count * 0.95)); if (typicalSilence < 0.00001f && typicalSilence >= 0) typicalSilence = 0.0001f; else if (typicalSilence < 0) typicalSilence = 0.0001f; }
            else { Console.WriteLine("No silence samples. Low default used."); typicalSilence = 0.001f; }
            DebugLog($"Typical max silence (95th percentile): {typicalSilence:F4}");

            currentCalibrationStep = CalibrationStep.SamplingSpeech_Prompt; calibrationSamples.Clear();
            Console.WriteLine($"\nSpeak normally for {CALIBRATION_DURATION_SECONDS}s...");
            Console.Write("Sampling speech in 3... "); await Task.Delay(1000); Console.Write("2... "); await Task.Delay(1000); Console.Write("1... "); await Task.Delay(1000); Console.WriteLine("NOW.");
            currentCalibrationStep = CalibrationStep.SamplingSpeech_Recording;
            calWaveIn = new WaveInEvent { DeviceNumber = deviceNumber, WaveFormat = waveFormatForWhisper };
            calWaveIn.DataAvailable += CollectCalibrationSamples_Handler;
            isRecording = true; calWaveIn.StartRecording(); await Task.Delay(CALIBRATION_DURATION_SECONDS * 1000); calWaveIn.StopRecording(); isRecording = false;
            calWaveIn.DataAvailable -= CollectCalibrationSamples_Handler;

            float typicalSpeech;
            if (calibrationSamples.Count > 0)
            { var ordered = calibrationSamples.OrderBy(x => x).ToList(); typicalSpeech = ordered.ElementAtOrDefault((int)(ordered.Count * 0.10)); if (typicalSpeech < 0.001f && typicalSpeech >= 0) typicalSpeech = 0.05f; else if (typicalSpeech < 0) typicalSpeech = 0.05f; }
            else { Console.WriteLine("No speech samples. Default used."); typicalSpeech = 0.1f; }
            DebugLog($"Typical min speech (10th percentile): {typicalSpeech:F4}");

            if (typicalSpeech <= typicalSilence + 0.005f)
            { Console.WriteLine("Warning: Speech not significantly louder. Calibration may be off."); calibratedEnergySilenceThreshold = typicalSilence * 2.0f; if (calibratedEnergySilenceThreshold < (AppSettings.APPSETTINGS_DEFAULT_ENERGY_THRESHOLD / 2) && AppSettings.APPSETTINGS_DEFAULT_ENERGY_THRESHOLD > 0) calibratedEnergySilenceThreshold = AppSettings.APPSETTINGS_DEFAULT_ENERGY_THRESHOLD / 2; else if (calibratedEnergySilenceThreshold < 0.002f) calibratedEnergySilenceThreshold = 0.002f; }
            else { float diff = typicalSpeech - typicalSilence; calibratedEnergySilenceThreshold = typicalSilence + (diff * 0.25f); }
            calibratedEnergySilenceThreshold = Math.Max(0.002f, Math.Min(0.35f, calibratedEnergySilenceThreshold));
            appSettings.CalibratedEnergySilenceThreshold = calibratedEnergySilenceThreshold; SaveAppSettings();
            Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine($"\n--- Calibration Complete --- \nNew Threshold: {calibratedEnergySilenceThreshold:F4} (Saved)"); Console.ResetColor();
        }
        finally { isRecording = false; currentCalibrationStep = CalibrationStep.None; calWaveIn?.Dispose(); DebugLog("Calibration process finished."); }
    }

    private static void CollectCalibrationSamples_Handler(object? sender, WaveInEventArgs e)
    {
        if (!isRecording || !(currentCalibrationStep == CalibrationStep.SamplingSilence || currentCalibrationStep == CalibrationStep.SamplingSpeech_Recording)) return;
        int tempVADBytesRec = 0;
        int bufferSize = (silenceDetectionBuffer != null && silenceDetectionBuffer.Length > 0) ? silenceDetectionBuffer.Length : (waveFormatForWhisper.AverageBytesPerSecond * SILENCE_DETECTION_BUFFER_MILLISECONDS / 1000);
        if (bufferSize <= 0) bufferSize = waveFormatForWhisper.BlockAlign > 0 ? waveFormatForWhisper.BlockAlign * 5 : 10;
        if (waveFormatForWhisper.BlockAlign > 0 && bufferSize % waveFormatForWhisper.BlockAlign != 0) { bufferSize = ((bufferSize / waveFormatForWhisper.BlockAlign) + 1) * waveFormatForWhisper.BlockAlign; }
        byte[] tempBuf = new byte[bufferSize];
        int bytesProcEvent = e.BytesRecorded, offset = 0;
        while (bytesProcEvent > 0 && tempBuf.Length > 0)
        {
            int toCopy = Math.Min(bytesProcEvent, tempBuf.Length - tempVADBytesRec); if (toCopy <= 0) break;
            Buffer.BlockCopy(e.Buffer, offset, tempBuf, tempVADBytesRec, toCopy);
            tempVADBytesRec += toCopy; bytesProcEvent -= toCopy; offset += toCopy;
            if (tempVADBytesRec == tempBuf.Length)
            {
                float maxSample = 0f;
                for (int i = 0; i < tempBuf.Length; i += waveFormatForWhisper.BlockAlign)
                {
                    if (waveFormatForWhisper.BitsPerSample == 16 && waveFormatForWhisper.Channels == 1 && i + 1 < tempBuf.Length)
                    { short s = BitConverter.ToInt16(tempBuf, i); float sf = s / 32768.0f; if (Math.Abs(sf) > maxSample) maxSample = Math.Abs(sf); }
                }
                calibrationSamples.Add(maxSample); tempVADBytesRec = 0;
            }
        }
    }

    private static async void WaveSource_DataAvailable(object? sender, WaveInEventArgs e)
    {
        if (!isRecording || chunkWaveFile == null || currentAudioChunkStream == null) return;
        try { chunkWaveFile.Write(e.Buffer, 0, e.BytesRecorded); }
        catch (ObjectDisposedException) { DebugLog("DataAvailable - Write to disposed chunkWaveFile."); return; }

        bool speechInSeg = false; int bytesProcEvent = e.BytesRecorded, offset = 0;
        if (silenceDetectionBuffer.Length == 0) return;
        while (bytesProcEvent > 0)
        {
            int toCopy = Math.Min(bytesProcEvent, silenceDetectionBuffer.Length - silenceDetectionBufferBytesRecorded); if (toCopy <= 0) break;
            Buffer.BlockCopy(e.Buffer, offset, silenceDetectionBuffer, silenceDetectionBufferBytesRecorded, toCopy);
            silenceDetectionBufferBytesRecorded += toCopy; bytesProcEvent -= toCopy; offset += toCopy;
            if (silenceDetectionBufferBytesRecorded == silenceDetectionBuffer.Length)
            {
                float maxSample = 0f;
                for (int i = 0; i < silenceDetectionBuffer.Length; i += waveFormatForWhisper.BlockAlign)
                {
                    if (waveFormatForWhisper.BitsPerSample == 16 && waveFormatForWhisper.Channels == 1 && i + 1 < silenceDetectionBuffer.Length)
                    { short s = BitConverter.ToInt16(silenceDetectionBuffer, i); float sf = s / 32768.0f; if (Math.Abs(sf) > maxSample) maxSample = Math.Abs(sf); }
                }
                if (maxSample > calibratedEnergySilenceThreshold) speechInSeg = true;
                silenceDetectionBufferBytesRecorded = 0;
            }
        }
        if (speechInSeg) { lastSpeechTime = DateTime.UtcNow; silenceDetectedRecently = false; }
        else { if (currentAudioChunkStream.Length > 0 && (DateTime.UtcNow - lastSpeechTime) > TimeSpan.FromSeconds(SILENCE_THRESHOLD_SECONDS)) silenceDetectedRecently = true; }

        TimeSpan chunkDur = DateTime.UtcNow - chunkStartTime; bool process = false;
        if (currentAudioChunkStream.Length > (waveFormatForWhisper.AverageBytesPerSecond / 2))
        {
            if (chunkDur >= TimeSpan.FromSeconds(MAX_CHUNK_DURATION_SECONDS)) { DebugLog($"Max duration ({MAX_CHUNK_DURATION_SECONDS}s) reached."); process = true; }
            else if (silenceDetectedRecently) { DebugLog($"Silence detected, processing chunk."); process = true; }
        }
        if (!activelyProcessingChunk && process)
        {
            activelyProcessingChunk = true; DebugLog($"Chunk ready. Dur: {chunkDur.TotalSeconds:F1}s, Silence: {silenceDetectedRecently}, Len: {currentAudioChunkStream.Length}");
            MemoryStream? streamToSend = null;
            try
            {
                chunkWaveFile?.Flush(); currentAudioChunkStream.Position = 0;
                streamToSend = new MemoryStream(); await currentAudioChunkStream.CopyToAsync(streamToSend); streamToSend.Position = 0;
            }
            catch (Exception ex) { DebugLog($"Error prep stream: {ex.Message}"); activelyProcessingChunk = false; streamToSend?.Dispose(); return; }
            StartNewChunk(); // This disposes old currentAudioChunkStream and chunkWaveFile
            var transcription = TranscribeAudioStream(streamToSend); currentTranscriptionTask = transcription; // streamToSend disposed in TranscribeAudioStream
            try { await transcription; }
            catch (Exception ex) { DebugLog($"Err transcription task: {ex.Message}"); }
            finally { currentTranscriptionTask = null; activelyProcessingChunk = false; }
        }
    }

    private static async void WaveSource_RecordingStopped(object? sender, StoppedEventArgs e)
    {
        DebugLog("WaveSource_RecordingStopped - Event ENTERED.");
        bool wasRec = isRecording; isRecording = false;
        MemoryStream? finalActiveStream = currentAudioChunkStream; WaveFileWriter? finalFile = chunkWaveFile;
        currentAudioChunkStream = null; chunkWaveFile = null;

        if (sender is WaveInEvent ws)
        { ws.DataAvailable -= WaveSource_DataAvailable; ws.RecordingStopped -= WaveSource_RecordingStopped; try { ws.Dispose(); } catch (Exception ex) { DebugLog($"Err disposing WaveInEvent: {ex.Message}"); } }
        if (ReferenceEquals(sender, waveSource)) waveSource = null;

        MemoryStream? streamToTranscribe = null;
        if (finalFile != null && finalActiveStream != null)
        {
            try
            {
                finalFile.Flush();
                if (wasRec && finalActiveStream.Length > (waveFormatForWhisper.AverageBytesPerSecond / 10))
                { finalActiveStream.Position = 0; streamToTranscribe = new MemoryStream(); await finalActiveStream.CopyToAsync(streamToTranscribe); streamToTranscribe.Position = 0; }
            }
            catch (Exception ex) { DebugLog($"Err flush/copy final: {ex.Message}"); }
            try { finalFile.Dispose(); } catch (Exception ex) { DebugLog($"Err disposing finalFile: {ex.Message}"); } // Disposes finalActiveStream too
        }
        // If finalFile was null or finalActiveStream still readable (shouldn't be if finalFile disposed it)
        if (finalFile == null || (finalActiveStream != null && finalActiveStream.CanRead)) { try { finalActiveStream?.Dispose(); } catch { } }


        Task? finalTranscribeTask = null; // To track this specific final transcription
        if (wasRec && streamToTranscribe != null && streamToTranscribe.Length > 0)
        {
            activelyProcessingChunk = true;
            finalTranscribeTask = TranscribeAudioStream(streamToTranscribe); // streamToTranscribe disposed in TranscribeAudioStream
            currentTranscriptionTask = finalTranscribeTask; // Update global task tracker
            try { await finalTranscribeTask; }
            catch (Exception ex) { DebugLog($"Err FINAL transcription: {ex.Message}"); }
            finally { if (ReferenceEquals(currentTranscriptionTask, finalTranscribeTask)) currentTranscriptionTask = null; activelyProcessingChunk = false; }
        }
        else if (wasRec) { DebugLog("No audio in FINAL chunk."); }

        // If streamToTranscribe was created but not used for transcription (e.g., too short, or error before calling TranscribeAudioStream)
        if (finalTranscribeTask == null && streamToTranscribe != null && streamToTranscribe.CanRead) { try { streamToTranscribe.Dispose(); } catch { } }

        if (wasRec)
        {
            Console.WriteLine("\n--- Full Transcription (Session Ended) ---");
            string fullText; lock (currentSessionTranscribedText) { fullText = string.Join(" ", currentSessionTranscribedText).Trim(); }
            var knownPlaceholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "[BLANK_AUDIO]", "(silence)", "[ Silence ]", "...", "[INAUDIBLE]", "[MUSIC PLAYING]", "[SOUND]", "[CLICK]" };
            var segmentsToPrint = currentSessionTranscribedText.Select(segment => segment.Trim()).Where(trimmedSegment => !string.IsNullOrWhiteSpace(trimmedSegment) && !knownPlaceholders.Contains(trimmedSegment) && !(trimmedSegment.StartsWith("[") && trimmedSegment.EndsWith("]") && trimmedSegment.Length <= 25)).ToList();
            fullText = string.Join(" ", segmentsToPrint).Trim();
            if (!string.IsNullOrWhiteSpace(fullText)) { Console.ForegroundColor = ConsoleColor.Cyan; Console.WriteLine(fullText); Console.ResetColor(); }
            else { Console.WriteLine("[No speech detected in this session after filtering.]"); }
            Console.WriteLine("----------------------------------------");
        }
        if (e.Exception != null) { Console.ForegroundColor = ConsoleColor.DarkYellow; Console.WriteLine($"NAudio stop exception: {e.Exception.Message}"); Console.ResetColor(); }

        stopProcessingTcs?.TrySetResult(true); DebugLog("WaveSource_RecordingStopped - Signalled stopProcessingTcs.");
        if (!exitRequested) PrintInstructions();
    }
}