// Program.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml; // Still useful for GgmlType if you were to use it elsewhere, but not for FromPath parameter

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Whisper.net Console Demo");

        string modelFilePath = "ggml-base.bin";
        string wavFilePath = "my_audio.wav";

        if (!File.Exists(modelFilePath))
        {
            string fullModelPath = Path.GetFullPath(modelFilePath);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Model file not found: {fullModelPath}");
            Console.WriteLine("Please download a GGUF model (e.g., ggml-base.bin from ggerganov/whisper.cpp on Hugging Face)");
            Console.WriteLine($"and place it in the application's output directory or provide a full path.");
            Console.ResetColor();
            return;
        }

        if (!File.Exists(wavFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Audio file not found: {Path.GetFullPath(wavFilePath)}");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"Using model: {modelFilePath}");
        Console.WriteLine($"Processing audio file: {wavFilePath}");
        Console.WriteLine("Initializing Whisper...");

        try
        {
            // --- Initialization for sandrohanea/whisper.net ---
            // Call FromPath with just the model file path.
            // The library should infer model properties from the GGUF file.
            using var whisperFactory = WhisperFactory.FromPath(modelFilePath);

            // --- Building the Processor ---
            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage("auto") // Using "auto" for language detection
                //.WithTranslate()
                .Build();

            Console.WriteLine("Whisper initialized successfully.");
            Console.WriteLine("Processing audio (this may take a moment)...");

            using var fileStream = File.OpenRead(wavFilePath);

            await foreach (var segment in processor.ProcessAsync(fileStream))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"[{segment.Start.TotalSeconds:F2}s -> {segment.End.TotalSeconds:F2s}]: ");
                Console.ResetColor();
                Console.WriteLine(segment.Text);
            }

            Console.WriteLine("\nProcessing complete.");
        }
        catch (DllNotFoundException dllEx)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nDLL Not Found Error: {dllEx.Message}");
            Console.WriteLine("This usually means the native whisper.dll (or .so/.dylib) could not be found or loaded.");
            Console.WriteLine("Ensure the Whisper.net.Runtime package is correctly installed and its native assets are deployed.");
            Console.ResetColor();
        }
        catch (Exception ex) // General catch-all
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn error occurred: {ex.Message}");
            if (ex.Message.ToLower().Contains("whisper_init_from_file") || ex.Message.ToLower().Contains("failed to initialize"))
            {
                Console.WriteLine("This might be due to an issue loading the model file (e.g., file corrupted, wrong format, or path issue).");
            }
            Console.WriteLine($"Details: {ex.ToString()}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            Console.ResetColor();
        }
    }
}