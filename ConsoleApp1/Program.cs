// Program.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using NAudio.Wave;

public class Program
{
    private static async Task TranscribeAudioStream(Stream audioStream, string modelFilePath)
    {
        Console.WriteLine("Initializing Whisper for transcription...");
        try
        {
            await Task.Yield(); 

            using var whisperFactory = WhisperFactory.FromPath(modelFilePath);
            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage("auto")
                .Build();

            Console.WriteLine("Whisper initialized. Processing audio stream...");

            await foreach (var segment in processor.ProcessAsync(audioStream))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"[{segment.Start.TotalSeconds:F2}s -> {segment.End.TotalSeconds:F2}s]: ");
                Console.ResetColor();
                Console.WriteLine(segment.Text);
            }
            Console.WriteLine("\nTranscription complete for this segment.");
        }
        catch (DllNotFoundException dllEx)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nDLL Not Found Error: {dllEx.Message}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn error occurred during transcription: {ex.Message}");
            if (ex.Message.ToLower().Contains("whisper_init_from_file") || ex.Message.ToLower().Contains("failed to initialize"))
            {
                Console.WriteLine("This might be due to an issue loading the model file.");
            }
            Console.ResetColor();
        }
    }

    private static bool isRecording = false;
    private static WaveInEvent? waveSource = null;
    private static MemoryStream? activeRecordingStream = null; // Stream WaveFileWriter writes to
    private static WaveFileWriter? waveFile = null;
    private static string currentModelFilePath = "ggml-base.bin";
    private static CancellationTokenSource? recordingStopCts = null;
    private static Task? currentTranscriptionTask = null; 

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Whisper.net Console Demo - Live Recording (Simplified Device Selection)");
        currentModelFilePath = "ggml-base.bin"; 

        if (!File.Exists(currentModelFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Model file not found: {Path.GetFullPath(currentModelFilePath)}");
            Console.ResetColor();
            return;
        }

        var waveFormatForWhisper = new WaveFormat(16000, 16, 1); 

        try
        {
            Console.WriteLine("Using default microphone (device 0).");
            int deviceNumber = 0; 

            Console.WriteLine($"\nPress 'R' to start recording (up to 30s or 'S' to stop). 'Q' to quit.");

            while (true)
            {
                if (Console.KeyAvailable) 
                {
                    ConsoleKeyInfo key = Console.ReadKey(true); 

                    if (key.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("Q pressed. Attempting graceful exit...");
                        if (isRecording && recordingStopCts != null && !recordingStopCts.IsCancellationRequested)
                        {
                            Console.WriteLine("DEBUG: Q - Actively recording, signalling stop to MonitorRecording.");
                            recordingStopCts.Cancel();
                        }
                        
                        if (currentTranscriptionTask != null && !currentTranscriptionTask.IsCompleted)
                        {
                            Console.WriteLine("DEBUG: Q - Waiting for current transcription to finish...");
                            try { await currentTranscriptionTask; Console.WriteLine("DEBUG: Q - Transcription task completed."); }
                            catch (Exception ex) { Console.WriteLine($"DEBUG: Q - Exception while awaiting transcription task: {ex.Message}"); }
                        }
                        break; 
                    }

                    if (key.Key == ConsoleKey.R && !isRecording)
                    {
                        Console.WriteLine("\nStarting recording...");
                        isRecording = true;
                        currentTranscriptionTask = null; 
                        activeRecordingStream = new MemoryStream(); // This is where WaveFileWriter writes
                        
                        waveSource = new WaveInEvent
                        {
                            DeviceNumber = deviceNumber,
                            WaveFormat = waveFormatForWhisper
                        };

                        waveFile = new WaveFileWriter(activeRecordingStream, waveSource.WaveFormat); 

                        waveSource.DataAvailable += WaveSource_DataAvailable;
                        waveSource.RecordingStopped += WaveSource_RecordingStopped;
                        
                        waveSource.StartRecording();
                        Console.WriteLine("Recording... Press 'S' to stop early or wait for 30s.");

                        recordingStopCts = new CancellationTokenSource();
                        _ = Task.Run(async () => await MonitorRecording(recordingStopCts.Token), recordingStopCts.Token);
                    }
                    else if (key.Key == ConsoleKey.S && isRecording)
                    {
                        Console.WriteLine("S pressed. Signaling stop to MonitorRecording...");
                        if (recordingStopCts != null && !recordingStopCts.IsCancellationRequested)
                        {
                           recordingStopCts.Cancel();
                        }
                    }
                    else if (key.Key == ConsoleKey.R && isRecording)
                    {
                        Console.WriteLine("Already recording. Press 'S' to stop the current recording.");
                    }
                }
                await Task.Delay(50); 
            }
        }
        finally 
        {
            Console.WriteLine("DEBUG: Main finally block reached.");
            
            if (recordingStopCts != null && !recordingStopCts.IsCancellationRequested)
            {
                Console.WriteLine("DEBUG: Main finally - Cancelling recordingStopCts.");
                recordingStopCts.Cancel();
            }
            recordingStopCts?.Dispose(); 

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
                waveFile?.Dispose();      
                activeRecordingStream?.Dispose(); // Dispose the stream WaveFileWriter used
            }
            Console.WriteLine("\nApplication exited.");
        }
    }

    private static async Task MonitorRecording(CancellationToken stopToken)
    {
        Console.WriteLine("DEBUG: MonitorRecording - Task started.");
        bool manuallyStopped = false;
        try
        {
            for (int i = 0; i < 300; i++) 
            {
                if (stopToken.IsCancellationRequested)
                {
                    manuallyStopped = true; 
                    break;
                }
                await Task.Delay(100, stopToken); 
            }
        }
        catch (TaskCanceledException) 
        { 
            Console.WriteLine("DEBUG: MonitorRecording - Task.Delay was cancelled (S or Q key).");
            manuallyStopped = true; 
        }
        catch (ObjectDisposedException) { Console.WriteLine("DEBUG: MonitorRecording - CancellationTokenSource was disposed prematurely."); return; }
        catch (Exception ex) { Console.WriteLine($"DEBUG: MonitorRecording - Error in loop: {ex.Message}"); return; }

        if (isRecording && waveSource != null) 
        {
            if (manuallyStopped)
            {
                Console.WriteLine("DEBUG: MonitorRecording - Stop requested by S/Q. Calling waveSource.StopRecording().");
            }
            else // Timeout
            {
                Console.WriteLine("DEBUG: MonitorRecording - 30 seconds reached. Calling waveSource.StopRecording().");
            }
            try { waveSource.StopRecording(); }
            catch (ObjectDisposedException) { Console.WriteLine("DEBUG: MonitorRecording - waveSource was already disposed when trying to stop."); }
            catch (Exception ex) { Console.WriteLine($"DEBUG: MonitorRecording - Error calling StopRecording: {ex.Message}"); }
        }
        else
        {
            Console.WriteLine("DEBUG: MonitorRecording - No active recording or waveSource is null when stop condition met.");
        }
        Console.WriteLine("DEBUG: MonitorRecording - Task finished.");
    }

    private static void WaveSource_DataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded > 0) { /* Console.WriteLine($"DEBUG: DataAvailable - BytesRecorded: {e.BytesRecorded}"); */ }
        if (waveFile != null && isRecording) 
        {
            try { waveFile.Write(e.Buffer, 0, e.BytesRecorded); }
            catch (ObjectDisposedException) { /* Expected if stop is very abrupt */ }
        }
    }

    private static async void WaveSource_RecordingStopped(object? sender, StoppedEventArgs e)
    {
        Console.WriteLine("DEBUG: WaveSource_RecordingStopped - Event ENTERED.");
        bool wasActuallyRecording = isRecording; 
        isRecording = false; 

        MemoryStream? originalStreamUsedByWriter = activeRecordingStream; // Capture the stream WaveFileWriter was using
        MemoryStream? streamForTranscription = null; // This will be the NEW stream
        activeRecordingStream = null; // Null out class field

        Console.WriteLine($"DEBUG: WaveSource_RecordingStopped - originalStreamUsedByWriter captured. Is null? {originalStreamUsedByWriter == null}");
        if (originalStreamUsedByWriter != null)
        {
            Console.WriteLine($"DEBUG: WaveSource_RecordingStopped - originalStreamUsedByWriter initial length: {originalStreamUsedByWriter.Length}");
        }

        if (sender is WaveInEvent ws)
        {
            Console.WriteLine("DEBUG: WaveSource_RecordingStopped - Unsubscribing and disposing WaveInEvent sender.");
            ws.DataAvailable -= WaveSource_DataAvailable;
            ws.RecordingStopped -= WaveSource_RecordingStopped;
            try { ws.Dispose(); } catch (Exception ex) { Console.WriteLine($"DEBUG: Error disposing WaveInEvent: {ex.Message}");}
        }
        if (ReferenceEquals(sender, waveSource)) { waveSource = null; }

        // Finalize and dispose WaveFileWriter. This will also dispose originalStreamUsedByWriter.
        if (waveFile != null)
        {
            Console.WriteLine("DEBUG: WaveSource_RecordingStopped - Flushing waveFile.");
            try
            {
                waveFile.Flush(); 
                // --- CRITICAL: Copy data BEFORE waveFile.Dispose() if it disposes the stream ---
                if (wasActuallyRecording && originalStreamUsedByWriter != null && originalStreamUsedByWriter.Length > 0)
                {
                    Console.WriteLine($"DEBUG: WaveSource_RecordingStopped - Copying {originalStreamUsedByWriter.Length} bytes to new stream for transcription.");
                    originalStreamUsedByWriter.Position = 0; // Rewind before copying
                    streamForTranscription = new MemoryStream();
                    await originalStreamUsedByWriter.CopyToAsync(streamForTranscription); // Asynchronously copy
                    streamForTranscription.Position = 0; // Rewind the new stream for reading
                    Console.WriteLine($"DEBUG: WaveSource_RecordingStopped - streamForTranscription length: {streamForTranscription.Length}");
                }
            }
            catch (Exception ex) { Console.WriteLine($"DEBUG: Error during waveFile flush or stream copy: {ex.Message}"); }
            
            Console.WriteLine("DEBUG: WaveSource_RecordingStopped - Disposing waveFile.");
            try { waveFile.Dispose(); } // This will dispose originalStreamUsedByWriter
            catch (Exception ex) { Console.WriteLine($"DEBUG: Error disposing waveFile: {ex.Message}"); }
            waveFile = null;
        }
        // originalStreamUsedByWriter is now disposed by waveFile.Dispose(). Dispose it explicitly just in case waveFile was null.
        try { originalStreamUsedByWriter?.Dispose(); } catch { /*ignore*/ }


        if (wasActuallyRecording && streamForTranscription != null && streamForTranscription.Length > 0)
        {
            Console.WriteLine($"DEBUG: WaveSource_RecordingStopped - Preparing to transcribe with streamForTranscription. Length: {streamForTranscription.Length / 1024.0:F2} KB.");
            
            var transcription = TranscribeAudioStream(streamForTranscription, currentModelFilePath);
            currentTranscriptionTask = transcription; 
            try
            {
                await transcription; 
                Console.WriteLine("DEBUG: WaveSource_RecordingStopped - Transcription task completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: WaveSource_RecordingStopped - Exception during transcription: {ex.Message}");
            }
            finally
            {
                 currentTranscriptionTask = null; 
            }
        }
        else if (wasActuallyRecording)
        {
            Console.WriteLine("DEBUG: WaveSource_RecordingStopped - No audio data in streamForTranscription, or stream was null. Not transcribing.");
        }
        else
        {
            Console.WriteLine("DEBUG: WaveSource_RecordingStopped - Event triggered but wasNotActuallyRecording. Not transcribing.");
        }

        Console.WriteLine("DEBUG: WaveSource_RecordingStopped - Disposing streamForTranscription.");
        try { streamForTranscription?.Dispose(); } catch (Exception ex) { Console.WriteLine($"DEBUG: Error disposing streamForTranscription: {ex.Message}");}

        if (e.Exception != null)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"DEBUG: WaveSource_RecordingStopped - NAudio reported an exception during stop: {e.Exception.Message}");
            Console.ResetColor();
        }
        
        Console.WriteLine($"\nDEBUG: WaveSource_RecordingStopped - Event EXITED. Press 'R' to record again, 'Q' to quit.");
    }
}