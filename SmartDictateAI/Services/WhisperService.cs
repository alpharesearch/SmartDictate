using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Whisper.net;

namespace SmartDictateAI.Services
{
    public class WhisperService : IWhisperService
    {
        private WhisperFactory? _whisperFactory;
        private string? _currentModelPath;

        public bool IsInitialized => _whisperFactory != null;

        public async Task<bool> InitializeAsync(string modelPath, Action<string>? onDebugMessage = null)
        {
            if (_whisperFactory != null && _currentModelPath == modelPath)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
            {
                onDebugMessage?.Invoke($"[Whisper] InitializeAsync: Whisper Model path invalid: {modelPath}");
                return false;
            }

            onDebugMessage?.Invoke($"[Whisper] Initializing Whisper with model: {modelPath}");
            try
            {
                await DisposeResourcesAsync(onDebugMessage);
                _whisperFactory = WhisperFactory.FromPath(modelPath);
                _currentModelPath = modelPath;

                onDebugMessage?.Invoke($"[Whisper] Factory ready | model={Path.GetFileName(modelPath)} | Arch={System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
                
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name?.ToLowerInvariant());
                string backend = assemblies.Any(a => a != null && a.Contains("vulkan"))
                    ? "Vulkan"
                    : "CPU";

                onDebugMessage?.Invoke($"[Whisper] Backend={backend} | model={Path.GetFileName(modelPath)}");
                onDebugMessage?.Invoke("[Whisper] WhisperFactory initialized.");
                return true;
            }
            catch (Exception ex)
            {
                onDebugMessage?.Invoke($"[Whisper] FATAL: Could not initialize Whisper.net: {ex.Message}");
                _whisperFactory = null;
                _currentModelPath = null;
                return false;
            }
        }

        public async Task<List<string>> TranscribeAsync(
            Stream audioStream, 
            string? promptText, 
            Action<string, string>? onSegmentTranscribed = null, 
            Action<string>? onDebugMessage = null)
        {
            if (_whisperFactory == null)
            {
                onDebugMessage?.Invoke("[Whisper] ERROR: WhisperFactory not initialized.");
                return new List<string>();
            }

            var chunkSegmentsRaw = new List<string>();
            try
            {
                await Task.Yield();
                onDebugMessage?.Invoke("[Whisper] Processing audio chunk with Whisper...");
                
                // AverageBytesPerSecond for Whisper standard 16kHz 16-bit mono = 32000
                double audioSeconds = (double)audioStream.Length / 32000.0;
                var swWhisper = System.Diagnostics.Stopwatch.StartNew();
                int segCount = 0;

                onDebugMessage?.Invoke($"[Whisper] Begin | audio={audioSeconds:F2}s | streamBytes={audioStream.Length}");

                var builder = _whisperFactory.CreateBuilder()
                    .WithLanguage("auto")
                    .WithNoSpeechThreshold(0.6f);

                if (!string.IsNullOrWhiteSpace(promptText))
                {
                    builder = builder.WithPrompt(promptText);
                }

                using var chunkProcessor = builder.Build();

                await foreach (var segment in chunkProcessor.ProcessAsync(audioStream))
                {
                    string timestampedText = $"[{segment.Start.TotalSeconds:F2}s -> {segment.End.TotalSeconds:F2}s]: {segment.Text}";
                    string rawText = segment.Text.Trim();
                    
                    onSegmentTranscribed?.Invoke(timestampedText, rawText);
                    chunkSegmentsRaw.Add(rawText);
                    segCount++;
                }

                swWhisper.Stop();
                double rtf = swWhisper.Elapsed.TotalSeconds / Math.Max(audioSeconds, 0.01);
                onDebugMessage?.Invoke($"[Whisper] Done | proc={swWhisper.Elapsed.TotalSeconds:F2}s | RTF={rtf:F2}x | segs={segCount} | {(rtf <= 1.0 ? "✅ realtime+" : "⚠️ slower")}");
            }
            catch (Exception ex)
            {
                onDebugMessage?.Invoke($"[Whisper] Transcription error in chunk: {ex.Message}");
            }

            return chunkSegmentsRaw;
        }

        public async Task DisposeResourcesAsync(Action<string>? onDebugMessage = null)
        {
            if (_whisperFactory != null)
            {
                onDebugMessage?.Invoke("[Whisper] Disposing WhisperFactory.");
                _whisperFactory.Dispose();
                _whisperFactory = null;
                _currentModelPath = null;
            }
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _whisperFactory?.Dispose();
            _whisperFactory = null;
            _currentModelPath = null;
        }
    }
}
