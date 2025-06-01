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

public class AppSettings
{
    public int SelectedMicrophoneDevice { get; set; } = 0; 
    public string ModelFilePath { get; set; } = Program.DEFAULT_MODEL_FILE_PATH; 
    public float CalibratedEnergySilenceThreshold { get; set; } = Program.DEFAULT_ENERGY_SILENCE_THRESHOLD; 
}

public class Program
{
    // Configuration for chunking and silence detection
    private const double MAX_CHUNK_DURATION_SECONDS = 20.0; 
    private const double SILENCE_THRESHOLD_SECONDS = 2.0;   
    public const float DEFAULT_ENERGY_SILENCE_THRESHOLD = 0.025f; 
    public const string DEFAULT_MODEL_FILE_PATH = "ggml-base.bin";
    private const int SILENCE_DETECTION_BUFFER_MILLISECONDS = 250; 
    private const int CALIBRATION_DURATION_SECONDS = 3;

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
    private static string currentModelFilePath = DEFAULT_MODEL_FILE_PATH; 
    private static Task? currentTranscriptionTask = null; 
    private static WaveFormat waveFormatForWhisper = new WaveFormat(16000, 16, 1);

    private static WhisperFactory? whisperFactoryInstance = null;
    private static WhisperProcessor? whisperProcessorInstance = null;

    private static float calibratedEnergySilenceThreshold = DEFAULT_ENERGY_SILENCE_THRESHOLD; 
    private enum CalibrationStep { None, SamplingSilence, SamplingSpeech_Prompt, SamplingSpeech_Recording }
    private static CalibrationStep currentCalibrationStep = CalibrationStep.None;
    private static List<float> calibrationSamples = new List<float>(); 

    private static AppSettings appSettings = new AppSettings(); 
    private static string appSettingsFilePath = "appsettings.json";

    private static void LoadAppSettings()
    {
        Console.WriteLine("DEBUG: Loading application settings...");
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) 
            .AddJsonFile(appSettingsFilePath, optional: true, reloadOnChange: false); 

        IConfigurationRoot configurationRoot = builder.Build();
        
        var settingsSection = configurationRoot.GetSection("AppSettings");
        if (settingsSection.Exists())
        {
            settingsSection.Bind(appSettings);
            Console.WriteLine($"DEBUG: Loaded ModelFilePath from settings: {appSettings.ModelFilePath}");
            Console.WriteLine($"DEBUG: Loaded CalibratedEnergySilenceThreshold from settings: {appSettings.CalibratedEnergySilenceThreshold}");
            Console.WriteLine($"DEBUG: Loaded SelectedMicrophoneDevice from settings: {appSettings.SelectedMicrophoneDevice}");
        }
        else
        {
            Console.WriteLine($"DEBUG: '{appSettingsFilePath}' or AppSettings section not found. Using default settings and creating file.");
            SaveAppSettings(); 
        }
        
        currentModelFilePath = appSettings.ModelFilePath;
        calibratedEnergySilenceThreshold = appSettings.CalibratedEnergySilenceThreshold;
    }

    private static void SaveAppSettings()
    {
        Console.WriteLine("DEBUG: Saving application settings...");
        try
        {
            appSettings.ModelFilePath = currentModelFilePath;
            appSettings.CalibratedEnergySilenceThreshold = calibratedEnergySilenceThreshold;
            // appSettings.SelectedMicrophoneDevice is updated directly when user selects

            var configurationToSave = new { AppSettings = appSettings }; 
            string json = JsonSerializer.Serialize(configurationToSave, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(appSettingsFilePath, json);
            Console.WriteLine($"DEBUG: Settings saved to {Path.GetFullPath(appSettingsFilePath)}");
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
        Console.WriteLine($"DEBUG: TranscribeAudioStream - Received stream length: {audioStream.Length}");
        if (whisperProcessorInstance == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: WhisperProcessor not initialized. Cannot transcribe.");
            Console.ResetColor();
            audioStream.Dispose();
            return;
        }
        
        if (audioStream.Length < (waveFormatForWhisper.AverageBytesPerSecond / 10)) 
        {
            Console.WriteLine("DEBUG: TranscribeAudioStream - Audio stream too short, skipping transcription.");
            audioStream.Dispose(); 
            return;
        }

        try
        {
            await Task.Yield();
            Console.WriteLine("Processing audio stream with existing WhisperProcessor...");

            await foreach (var segment in whisperProcessorInstance.ProcessAsync(audioStream))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"[{segment.Start.TotalSeconds:F2}s -> {segment.End.TotalSeconds:F2}s]: ");
                Console.ResetColor();
                Console.WriteLine(segment.Text);
            }
            Console.WriteLine("\nTranscription complete for this segment.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn error occurred during transcription: {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            audioStream.Dispose(); 
        }
    }
    
    private static void StartNewChunk()
    {
        // Console.WriteLine("DEBUG: StartNewChunk called."); 
        
        try { chunkWaveFile?.Dispose(); } catch { /* ignore */ }
        try { currentAudioChunkStream?.Dispose(); } catch { /* ignore */ }

        currentAudioChunkStream = new MemoryStream();
        chunkWaveFile = new WaveFileWriter(currentAudioChunkStream, waveFormatForWhisper);

        chunkStartTime = DateTime.UtcNow;
        lastSpeechTime = DateTime.UtcNow; 
        silenceDetectedRecently = false;
        
        int bytesPerSample = waveFormatForWhisper.BitsPerSample / 8;
        int samplesPerSilenceBuffer = waveFormatForWhisper.SampleRate * SILENCE_DETECTION_BUFFER_MILLISECONDS / 1000;
        int bytesPerSilenceDetectionBuffer = samplesPerSilenceBuffer * bytesPerSample * waveFormatForWhisper.Channels;
        
        if (bytesPerSilenceDetectionBuffer == 0 && waveFormatForWhisper.AverageBytesPerSecond > 0)
        {
            bytesPerSilenceDetectionBuffer = waveFormatForWhisper.BlockAlign > 0 ? waveFormatForWhisper.BlockAlign : 2 ; 
        }
        else if (waveFormatForWhisper.BlockAlign > 0 && bytesPerSilenceDetectionBuffer % waveFormatForWhisper.BlockAlign != 0) 
        {
            bytesPerSilenceDetectionBuffer = 
                ((bytesPerSilenceDetectionBuffer / waveFormatForWhisper.BlockAlign) + 1) * waveFormatForWhisper.BlockAlign;
        }

        silenceDetectionBuffer = new byte[bytesPerSilenceDetectionBuffer];
        silenceDetectionBufferBytesRecorded = 0;
        // Console.WriteLine($"DEBUG: New chunk started. Silence VAD buffer size: {bytesPerSilenceDetectionBuffer} bytes.");
    }

    public static async Task Main(string[] args)
    {
        LoadAppSettings(); 

        Console.WriteLine("Whisper.net Console Demo - Live Recording with Silence Detection");

        if (!File.Exists(currentModelFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Model file not found: {Path.GetFullPath(currentModelFilePath)} (check appsettings.json or place it here)");
            Console.ResetColor();
            return;
        }
        
        try
        {
            int deviceNumber = appSettings.SelectedMicrophoneDevice; 

            if (WaveIn.DeviceCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No microphones found! Please ensure a microphone is connected and enabled.");
                Console.ResetColor();
                return;
            }

            if (deviceNumber < 0 || deviceNumber >= WaveIn.DeviceCount)
            {
                Console.WriteLine($"Warning: Saved microphone device number {deviceNumber} is invalid. Defaulting to device 0.");
                deviceNumber = 0;
                appSettings.SelectedMicrophoneDevice = 0; 
            }

            Console.WriteLine("\nAvailable microphones:");
            string currentMicName = "Unknown (defaulting to 0)";
            try {
                if (WaveIn.DeviceCount > 0) // Ensure there's at least one device before accessing
                   currentMicName = WaveIn.GetCapabilities(deviceNumber).ProductName;
            } catch { /* deviceNumber might be invalid if list changed drastically, or no devices */ }


            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var caps = WaveIn.GetCapabilities(i);
                Console.WriteLine($"{i}: {caps.ProductName} {(i == deviceNumber ? "[Current Setting]" : "")}");
            }
            
            Console.Write($"\nSelect microphone by number (or press Enter to use '{currentMicName}' [{deviceNumber}]): ");
            string? micInput = Console.ReadLine();

            bool settingsChangedByMicSelection = false;
            if (!string.IsNullOrWhiteSpace(micInput) && int.TryParse(micInput, out int parsedMicNumber))
            {
                if (parsedMicNumber >= 0 && parsedMicNumber < WaveIn.DeviceCount)
                {
                    if (deviceNumber != parsedMicNumber)
                    {
                        deviceNumber = parsedMicNumber;
                        appSettings.SelectedMicrophoneDevice = deviceNumber; 
                        settingsChangedByMicSelection = true; // Mark that settings changed
                        Console.WriteLine($"Microphone selection changed to: {WaveIn.GetCapabilities(deviceNumber).ProductName} [{deviceNumber}]");
                    }
                    else
                    {
                        Console.WriteLine($"Keeping current microphone: {currentMicName} [{deviceNumber}]");
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid selection. Using current microphone: {currentMicName} [{deviceNumber}]");
                }
            }
            else
            {
                 Console.WriteLine($"No input. Using current microphone: {currentMicName} [{deviceNumber}]");
            }

            if(settingsChangedByMicSelection) {
                // SaveAppSettings(); // Optionally save immediately, or just rely on Save on Quit
            }


            PrintInstructions();

            while (true)
            {
                if (currentCalibrationStep != CalibrationStep.None && !isRecording) 
                {
                     if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q) {
                        Console.WriteLine("Calibration aborted by Q. Exiting...");
                        currentCalibrationStep = CalibrationStep.None; 
                        SaveAppSettings(); 
                        break; 
                     }
                     await Task.Delay(50); 
                     continue; 
                }

                if (Console.KeyAvailable) 
                {
                    ConsoleKeyInfo key = Console.ReadKey(true); 

                    if (key.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("Q pressed. Attempting graceful exit...");
                        if (isRecording && waveSource != null) 
                        {
                            Console.WriteLine("DEBUG: Q - Actively recording, stopping WaveSource.");
                            waveSource.StopRecording(); 
                        }
                        
                        if (currentTranscriptionTask != null && !currentTranscriptionTask.IsCompleted)
                        {
                            Console.WriteLine("DEBUG: Q - Waiting for final transcription to finish...");
                            try { await currentTranscriptionTask; Console.WriteLine("DEBUG: Q - Transcription task completed."); }
                            catch (Exception ex) { Console.WriteLine($"DEBUG: Q - Exception while awaiting transcription task: {ex.Message}"); }
                        }
                        SaveAppSettings(); 
                        break; 
                    }
                    
                    if (key.Key == ConsoleKey.M && !isRecording) 
                    {
                        ChangeModelPath(); 
                        PrintInstructions();
                    }
                    else if (key.Key == ConsoleKey.C && !isRecording)
                    {
                        await CalibrateThresholds(deviceNumber); 
                        PrintInstructions();
                    }
                    else if (key.Key == ConsoleKey.R && !isRecording)
                    {
                        Console.WriteLine("\nStarting continuous recording...");
                        
                        if (whisperProcessorInstance == null) 
                        {
                            Console.WriteLine("DEBUG: Initializing WhisperFactory and WhisperProcessor for the session...");
                            try
                            {
                                whisperFactoryInstance = WhisperFactory.FromPath(currentModelFilePath);
                                whisperProcessorInstance = whisperFactoryInstance.CreateBuilder()
                                    .WithLanguage("auto") 
                                    .Build();
                                Console.WriteLine("DEBUG: WhisperFactory and WhisperProcessor initialized.");
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"FATAL: Could not initialize Whisper.net: {ex.Message}");
                                Console.ResetColor();
                                continue; 
                            }
                        }

                        isRecording = true;
                        currentTranscriptionTask = null;
                        StartNewChunk(); 
                        
                        waveSource = new WaveInEvent
                        {
                            DeviceNumber = deviceNumber,
                            WaveFormat = waveFormatForWhisper
                        };

                        waveSource.DataAvailable += WaveSource_DataAvailable;
                        waveSource.RecordingStopped += WaveSource_RecordingStopped;
                        
                        waveSource.StartRecording();
                        Console.WriteLine("Recording continuously... Silence or 'S' will process/stop.");
                    }
                    else if (key.Key == ConsoleKey.S && isRecording)
                    {
                        Console.WriteLine("S pressed. Stopping recording session...");
                        if (waveSource != null) 
                        {
                            waveSource.StopRecording(); 
                        }
                    }
                    else if (key.Key == ConsoleKey.R && isRecording)
                    {
                        Console.WriteLine("Already recording.");
                    }
                }
                await Task.Delay(50); 
            }
        }
        finally 
        {
            Console.WriteLine("DEBUG: Main finally block reached.");
            if (waveSource != null && isRecording) 
            {
                Console.WriteLine("DEBUG: Main finally - waveSource indicates it might still be recording. Calling StopRecording().");
                waveSource.StopRecording(); 
                if (currentTranscriptionTask != null && !currentTranscriptionTask.IsCompleted) {
                    Console.WriteLine("DEBUG: Main finally - Waiting for transcription from final stop...");
                    try { await currentTranscriptionTask; } catch { /* ignore */ }
                }
            }
            else 
            {
                Console.WriteLine("DEBUG: Main finally - Ensuring NAudio objects are disposed if not recording.");
                waveSource?.Dispose();    
            }
            
            Console.WriteLine("DEBUG: Main finally - Disposing WhisperProcessor and WhisperFactory.");
            whisperProcessorInstance?.Dispose();
            whisperFactoryInstance?.Dispose(); 

            chunkWaveFile?.Dispose();      
            currentAudioChunkStream?.Dispose(); 
            Console.WriteLine("\nApplication exited.");
        }
    }
    
    private static void PrintInstructions()
    {
        Console.WriteLine($"\n[R] Record/Start | [S] Stop Recording | [C] Calibrate Threshold | [M] Change Model | [Q] Quit");
        Console.WriteLine($"Using Model: {currentModelFilePath}");
        Console.WriteLine($"Current Silence Threshold: {calibratedEnergySilenceThreshold:F4}");
        if (WaveIn.DeviceCount > 0 && appSettings.SelectedMicrophoneDevice >=0 && appSettings.SelectedMicrophoneDevice < WaveIn.DeviceCount)
        {
            try { Console.WriteLine($"Current Mic: [{appSettings.SelectedMicrophoneDevice}] {WaveIn.GetCapabilities(appSettings.SelectedMicrophoneDevice).ProductName}"); } catch {}
        }
    }

    private static void ChangeModelPath()
    {
        Console.WriteLine("\n--- Change Whisper Model ---");
        Console.WriteLine($"Current model path: {currentModelFilePath}");
        Console.Write("Enter new model file path (e.g., ggml-small.bin) or press Enter to cancel: ");
        string? newModelPath = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(newModelPath) && File.Exists(newModelPath))
        {
            if (currentModelFilePath != newModelPath) 
            {
                currentModelFilePath = newModelPath;
                appSettings.ModelFilePath = newModelPath; 
                SaveAppSettings(); 

                Console.WriteLine($"Model path updated to: {currentModelFilePath}");
                Console.WriteLine("Disposing existing Whisper instances to reload with new model on next recording...");
                whisperProcessorInstance?.Dispose();
                whisperFactoryInstance?.Dispose();
                whisperProcessorInstance = null;
                whisperFactoryInstance = null;
                Console.WriteLine("Whisper instances cleared. New model will be loaded next time you press 'R'.");
            }
            else { Console.WriteLine("New path is same as current. No change made."); }
        }
        else if (!string.IsNullOrWhiteSpace(newModelPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid model path or file not found. Model not changed.");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("Model change cancelled.");
        }
    }

    private static async Task CalibrateThresholds(int deviceNumber)
    {
        Console.WriteLine("\n--- Starting Silence Threshold Calibration ---");
        WaveInEvent? calibrationWaveIn = null; 

        try
        {
            currentCalibrationStep = CalibrationStep.SamplingSilence;
            calibrationSamples.Clear();
            Console.WriteLine($"Please remain completely silent for {CALIBRATION_DURATION_SECONDS} seconds...");
            Console.Write("Sampling silence in 3... "); await Task.Delay(1000); Console.Write("2... "); await Task.Delay(1000); Console.Write("1... "); await Task.Delay(1000); Console.WriteLine("NOW.");

            calibrationWaveIn = new WaveInEvent { DeviceNumber = deviceNumber, WaveFormat = waveFormatForWhisper };
            calibrationWaveIn.DataAvailable += CollectCalibrationSamples_Handler; 
            
            isRecording = true; 
            calibrationWaveIn.StartRecording();
            await Task.Delay(CALIBRATION_DURATION_SECONDS * 1000);
            calibrationWaveIn.StopRecording(); 
            isRecording = false; 
            calibrationWaveIn.DataAvailable -= CollectCalibrationSamples_Handler;
            calibrationWaveIn.Dispose(); 
            calibrationWaveIn = null; 

            float typicalSilenceLevel;
            if (calibrationSamples.Count > 0)
            {
                var orderedSilenceSamples = calibrationSamples.OrderBy(x => x).ToList();
                typicalSilenceLevel = orderedSilenceSamples.ElementAtOrDefault((int)(orderedSilenceSamples.Count * 0.95));
                if (typicalSilenceLevel < 0.00001f && typicalSilenceLevel >= 0) typicalSilenceLevel = 0.0001f; 
                else if (typicalSilenceLevel < 0) typicalSilenceLevel = 0.0001f; 
            }
            else
            {
                Console.WriteLine("No silence samples collected. Using a low default for silence level.");
                typicalSilenceLevel = 0.001f; 
            }
            Console.WriteLine($"DEBUG: Typical max silence sample (95th percentile or default): {typicalSilenceLevel:F4}");


            currentCalibrationStep = CalibrationStep.SamplingSpeech_Prompt;
            calibrationSamples.Clear();
            Console.WriteLine($"\nNow, please speak normally for {CALIBRATION_DURATION_SECONDS} seconds (e.g., read a sentence)...");
            Console.Write("Sampling speech in 3... "); await Task.Delay(1000); Console.Write("2... "); await Task.Delay(1000); Console.Write("1... "); await Task.Delay(1000); Console.WriteLine("NOW.");
            currentCalibrationStep = CalibrationStep.SamplingSpeech_Recording;

            calibrationWaveIn = new WaveInEvent { DeviceNumber = deviceNumber, WaveFormat = waveFormatForWhisper };
            calibrationWaveIn.DataAvailable += CollectCalibrationSamples_Handler;
            isRecording = true; 
            calibrationWaveIn.StartRecording();
            await Task.Delay(CALIBRATION_DURATION_SECONDS * 1000);
            calibrationWaveIn.StopRecording();
            isRecording = false; 
            calibrationWaveIn.DataAvailable -= CollectCalibrationSamples_Handler;
            // Dispose is in finally block

            float typicalSpeechLevel;
            if (calibrationSamples.Count > 0)
            {
                var orderedSpeechSamples = calibrationSamples.OrderBy(x => x).ToList();
                typicalSpeechLevel = orderedSpeechSamples.ElementAtOrDefault((int)(orderedSpeechSamples.Count * 0.10));
                if (typicalSpeechLevel < 0.001f && typicalSpeechLevel >=0) typicalSpeechLevel = 0.05f; 
                else if (typicalSpeechLevel < 0) typicalSpeechLevel = 0.05f;
            }
            else
            {
                Console.WriteLine("No speech samples collected. Using a default for speech level.");
                typicalSpeechLevel = 0.1f; 
            }
            Console.WriteLine($"DEBUG: Typical min speech sample (10th percentile or default): {typicalSpeechLevel:F4}");

            if (typicalSpeechLevel <= typicalSilenceLevel + 0.005f) 
            {
                Console.WriteLine("Warning: Speech level was not significantly higher than silence level. Calibration might be inaccurate. Setting a threshold slightly above detected silence.");
                calibratedEnergySilenceThreshold = typicalSilenceLevel * 2.0f; 
                if (calibratedEnergySilenceThreshold < (DEFAULT_ENERGY_SILENCE_THRESHOLD / 2) && DEFAULT_ENERGY_SILENCE_THRESHOLD > 0) calibratedEnergySilenceThreshold = DEFAULT_ENERGY_SILENCE_THRESHOLD / 2;
                else if (calibratedEnergySilenceThreshold < 0.002f) calibratedEnergySilenceThreshold = 0.002f; 
            }
            else
            {
                float difference = typicalSpeechLevel - typicalSilenceLevel;
                calibratedEnergySilenceThreshold = typicalSilenceLevel + (difference * 0.25f); 
            }
            calibratedEnergySilenceThreshold = Math.Max(0.002f, Math.Min(0.35f, calibratedEnergySilenceThreshold)); 

            appSettings.CalibratedEnergySilenceThreshold = calibratedEnergySilenceThreshold; 
            SaveAppSettings(); 

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n--- Calibration Complete ---");
            Console.WriteLine($"New Calibrated ENERGY_SILENCE_THRESHOLD: {calibratedEnergySilenceThreshold:F4} (Saved)");
            Console.ResetColor();
        }
        finally
        {
            isRecording = false; 
            currentCalibrationStep = CalibrationStep.None;
            calibrationWaveIn?.Dispose(); 
            Console.WriteLine("Calibration process finished.");
        }
    }

    private static void CollectCalibrationSamples_Handler(object? sender, WaveInEventArgs e)
    {
        if (!isRecording || !(currentCalibrationStep == CalibrationStep.SamplingSilence || currentCalibrationStep == CalibrationStep.SamplingSpeech_Recording)) return;

        int tempVADBufferBytesRecorded = 0; 
        
        int analysisBufferSize = (silenceDetectionBuffer != null && silenceDetectionBuffer.Length > 0) ? 
                                 silenceDetectionBuffer.Length : 
                                 (waveFormatForWhisper.AverageBytesPerSecond * SILENCE_DETECTION_BUFFER_MILLISECONDS / 1000);
        if (analysisBufferSize <= 0) analysisBufferSize = waveFormatForWhisper.BlockAlign > 0 ? waveFormatForWhisper.BlockAlign * 5 : 10; 
        if (waveFormatForWhisper.BlockAlign > 0 && analysisBufferSize % waveFormatForWhisper.BlockAlign != 0 ) {
             analysisBufferSize = ((analysisBufferSize / waveFormatForWhisper.BlockAlign) + 1) * waveFormatForWhisper.BlockAlign;
        }
        byte[] tempAnalysisBuffer = new byte[analysisBufferSize];


        int bytesToProcessInEvent = e.BytesRecorded;
        int currentBufferOffset = 0;

        while(bytesToProcessInEvent > 0 && tempAnalysisBuffer.Length > 0)
        {
            int bytesToCopyToBuffer = Math.Min(bytesToProcessInEvent, tempAnalysisBuffer.Length - tempVADBufferBytesRecorded);
            if (bytesToCopyToBuffer <= 0) break; 

            Buffer.BlockCopy(e.Buffer, currentBufferOffset, tempAnalysisBuffer, tempVADBufferBytesRecorded, bytesToCopyToBuffer);
            tempVADBufferBytesRecorded += bytesToCopyToBuffer;
            bytesToProcessInEvent -= bytesToCopyToBuffer;
            currentBufferOffset += bytesToCopyToBuffer;

            if (tempVADBufferBytesRecorded == tempAnalysisBuffer.Length)
            {
                float maxSample = 0f;
                for (int i = 0; i < tempAnalysisBuffer.Length; i += waveFormatForWhisper.BlockAlign)
                {
                    if (waveFormatForWhisper.BitsPerSample == 16 && waveFormatForWhisper.Channels == 1)
                    {
                         if (i + 1 < tempAnalysisBuffer.Length)
                         {
                            short sample = BitConverter.ToInt16(tempAnalysisBuffer, i);
                            float sampleFloat = sample / 32768.0f; 
                            if (Math.Abs(sampleFloat) > maxSample) maxSample = Math.Abs(sampleFloat);
                         }
                    }
                }
                calibrationSamples.Add(maxSample);
                tempVADBufferBytesRecorded = 0; 
            }
        }
    }


    private static async void WaveSource_DataAvailable(object? sender, WaveInEventArgs e)
    {
        if (!isRecording || chunkWaveFile == null || currentAudioChunkStream == null) return;

        try { chunkWaveFile.Write(e.Buffer, 0, e.BytesRecorded); }
        catch (ObjectDisposedException) { Console.WriteLine("DEBUG: DataAvailable - Attempted to write to disposed chunkWaveFile."); return; }

        bool speechInCurrentAnalysisBufferSegment = false; 
        int bytesToProcessInEvent = e.BytesRecorded;
        int currentBufferOffset = 0;

        if (silenceDetectionBuffer.Length == 0) { return; }


        while(bytesToProcessInEvent > 0)
        {
            int bytesToCopyToSilenceBuffer = Math.Min(bytesToProcessInEvent, silenceDetectionBuffer.Length - silenceDetectionBufferBytesRecorded);
            if (bytesToCopyToSilenceBuffer <= 0) break; 

            Buffer.BlockCopy(e.Buffer, currentBufferOffset, silenceDetectionBuffer, silenceDetectionBufferBytesRecorded, bytesToCopyToSilenceBuffer);
            silenceDetectionBufferBytesRecorded += bytesToCopyToSilenceBuffer;
            bytesToProcessInEvent -= bytesToCopyToSilenceBuffer;
            currentBufferOffset += bytesToCopyToSilenceBuffer;

            if (silenceDetectionBufferBytesRecorded == silenceDetectionBuffer.Length)
            {
                float maxSample = 0f;
                for (int i = 0; i < silenceDetectionBuffer.Length; i += waveFormatForWhisper.BlockAlign)
                {
                    if (waveFormatForWhisper.BitsPerSample == 16 && waveFormatForWhisper.Channels == 1)
                    {
                         if (i + 1 < silenceDetectionBuffer.Length)
                         {
                            short sample = BitConverter.ToInt16(silenceDetectionBuffer, i);
                            float sampleFloat = sample / 32768.0f; 
                            if (Math.Abs(sampleFloat) > maxSample) maxSample = Math.Abs(sampleFloat);
                         }
                    }
                }
                if (maxSample > calibratedEnergySilenceThreshold) 
                {
                    speechInCurrentAnalysisBufferSegment = true; 
                }
                silenceDetectionBufferBytesRecorded = 0; 
            }
        }

        if (speechInCurrentAnalysisBufferSegment) 
        {
            lastSpeechTime = DateTime.UtcNow;
            silenceDetectedRecently = false;
        }
        else 
        {
            if (currentAudioChunkStream.Length > 0 && (DateTime.UtcNow - lastSpeechTime) > TimeSpan.FromSeconds(SILENCE_THRESHOLD_SECONDS))
            {
                silenceDetectedRecently = true;
            }
        }
        
        TimeSpan currentChunkDuration = DateTime.UtcNow - chunkStartTime;
        bool shouldProcessChunk = false;

        if (currentAudioChunkStream.Length > (waveFormatForWhisper.AverageBytesPerSecond / 2)) 
        {
            if (currentChunkDuration >= TimeSpan.FromSeconds(MAX_CHUNK_DURATION_SECONDS))
            {
                Console.WriteLine($"DEBUG: Max chunk duration ({MAX_CHUNK_DURATION_SECONDS}s) reached.");
                shouldProcessChunk = true;
            }
            else if (silenceDetectedRecently)
            {
                Console.WriteLine($"DEBUG: Silence detected, processing chunk.");
                shouldProcessChunk = true;
            }
        }
        
        if (!activelyProcessingChunk && shouldProcessChunk)
        {
            activelyProcessingChunk = true; 
            Console.WriteLine($"DEBUG: Chunk ready. Duration: {currentChunkDuration.TotalSeconds:F1}s, Silence: {silenceDetectedRecently}, Length: {currentAudioChunkStream.Length}");

            MemoryStream? streamToSend = null;
            try
            {
                chunkWaveFile?.Flush(); 
                
                currentAudioChunkStream.Position = 0;
                streamToSend = new MemoryStream();
                await currentAudioChunkStream.CopyToAsync(streamToSend);
                streamToSend.Position = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error preparing streamToSend: {ex.Message}");
                activelyProcessingChunk = false;
                streamToSend?.Dispose();
                return;
            }

            StartNewChunk(); 

            var transcription = TranscribeAudioStream(streamToSend); 
            currentTranscriptionTask = transcription; 

            try { await transcription; }
            catch (Exception ex) { Console.WriteLine($"DEBUG: Error during transcription task: {ex.Message}"); }
            finally
            {
                currentTranscriptionTask = null;
                activelyProcessingChunk = false; 
            }
        }
    }

    private static async void WaveSource_RecordingStopped(object? sender, StoppedEventArgs e)
    {
        Console.WriteLine("DEBUG: WaveSource_RecordingStopped - Event ENTERED.");
        bool wasActuallyRecording = isRecording;
        isRecording = false; 

        MemoryStream? finalActiveStream = currentAudioChunkStream;
        WaveFileWriter? finalWaveFile = chunkWaveFile;
        currentAudioChunkStream = null;
        chunkWaveFile = null;

        if (sender is WaveInEvent ws)
        {
            ws.DataAvailable -= WaveSource_DataAvailable;
            ws.RecordingStopped -= WaveSource_RecordingStopped;
            try { ws.Dispose(); } catch (Exception ex) { Console.WriteLine($"DEBUG: Error disposing WaveInEvent: {ex.Message}");}
        }
        if (ReferenceEquals(sender, waveSource)) { waveSource = null; }

        MemoryStream? streamForTranscription = null;
        if (finalWaveFile != null && finalActiveStream != null)
        {
            try
            {
                finalWaveFile.Flush();
                if (wasActuallyRecording && finalActiveStream.Length > (waveFormatForWhisper.AverageBytesPerSecond / 10)) 
                {
                    finalActiveStream.Position = 0; 
                    streamForTranscription = new MemoryStream();
                    await finalActiveStream.CopyToAsync(streamForTranscription); 
                    streamForTranscription.Position = 0; 
                }
            }
            catch (Exception ex) { Console.WriteLine($"DEBUG: Error during finalWaveFile flush or stream copy: {ex.Message}"); }
            
            try { finalWaveFile.Dispose(); } 
            catch (Exception ex) { Console.WriteLine($"DEBUG: Error disposing finalWaveFile: {ex.Message}"); }
        }
        // finalActiveStream is disposed by finalWaveFile.Dispose()
        // Explicitly dispose if finalWaveFile was null for some reason, or if stream wasn't fully read (CanRead is true)
        if (finalWaveFile == null || (finalActiveStream != null && finalActiveStream.CanRead)) { 
            try { finalActiveStream?.Dispose(); } catch { /*ignore*/ }
        }


        if (wasActuallyRecording && streamForTranscription != null && streamForTranscription.Length > 0)
        {
            activelyProcessingChunk = true; 
            var transcription = TranscribeAudioStream(streamForTranscription); 
            currentTranscriptionTask = transcription; 
            try
            {
                await transcription; 
            }
            catch (Exception ex) { Console.WriteLine($"DEBUG: WaveSource_RecordingStopped - Exception during FINAL transcription: {ex.Message}"); }
            finally
            {
                 currentTranscriptionTask = null; 
                 activelyProcessingChunk = false; 
            }
        }
        else if (wasActuallyRecording) { /* Console.WriteLine("DEBUG: No audio data in FINAL chunk for transcription."); */ }
        
        // If streamForTranscription was created but not used (e.g., too short), ensure it's disposed.
        // TranscribeAudioStream disposes the stream it's given.
        if (currentTranscriptionTask == null && streamForTranscription != null && streamForTranscription.CanRead) {
             try { streamForTranscription.Dispose(); } catch {}
        }

        if (e.Exception != null)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"DEBUG: WaveSource_RecordingStopped - NAudio reported an exception: {e.Exception.Message}");
            Console.ResetColor();
        }
        
        PrintInstructions(); 
    }
}