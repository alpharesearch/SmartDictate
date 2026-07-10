using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using SmartDictateAI.Services;
using NAudio.Wave;

namespace SmartDictateAI.PerformanceTests
{
    public partial class ModelPerformanceTests
    {
        public static IEnumerable<object[]> GetWhisperModels()
        {
            try
            {
                var whisperDir = ModelPathHelper.GetWhisperModelsDirectory();
                var binFiles = Directory.GetFiles(whisperDir, "*.bin");

                if (binFiles.Length == 0)
                {
                    var rootDir = ModelPathHelper.GetModelsDirectory();
                    binFiles = Directory.GetFiles(rootDir, "*.bin");
                }

                if (binFiles.Length > 0)
                {
                    return binFiles.Select(f => new object[] { Path.GetFileName(f), f });
                }
            }
            catch
            {
                // Directory scanning failed or directory not found during discovery
            }

            // Fallback list of models so they are ALWAYS listed in the Test Explorer
            return new List<object[]>
            {
                new object[] { "ggml-large-v3-turbo-q8_0.bin", "" },
                new object[] { "ggml-base.bin", "" }
            };
        }

        [Fact]
        [Trait("Category", "Performance")]
        public async Task Benchmark_Stage1_Whisper_Models()
        {
            if (!PerformanceTestHelper.ShouldRun())
            {
                Console.WriteLine("Bypassing Whisper benchmarks (performance runs are not enabled).");
                return;
            }

            var models = GetWhisperModels().ToList();
            foreach (var model in models)
            {
                var modelName = (string)model[0];
                var modelPath = (string)model[1];
                await RunSingleWhisperModelBenchmark(modelName, modelPath);
            }
        }

        private async Task RunSingleWhisperModelBenchmark(string modelName, string modelPath)
        {
            string wavPath;
            string txtPath;
            try
            {
                wavPath = ModelPathHelper.GetAssetPath("whisper_benchmark.wav");
                txtPath = ModelPathHelper.GetAssetPath("whisper_benchmark.txt");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Missing benchmark files: {ex.Message} Please ensure 'whisper_benchmark.wav' and 'whisper_benchmark.txt' are in the 'SmartDictateAI.PerformanceTests/Assets' folder.");
                return;
            }


            var whisperDir = ModelPathHelper.GetWhisperModelsDirectory();

            // Resolve path if using the fallback model list
            if (string.IsNullOrEmpty(modelPath))

            {
                try
                {
                    modelPath = Path.Combine(whisperDir, modelName);
                    if (!File.Exists(modelPath))
                    {
                        var rootDir = ModelPathHelper.GetModelsDirectory();
                        modelPath = Path.Combine(rootDir, modelName);
                    }
                }
                catch
                {
                    modelPath = modelName;
                }
            }

            Assert.True(File.Exists(modelPath), $"Whisper model file '{modelName}' was not found on disk at '{modelPath}'. Place it in 'models/whisper/' to run the benchmark.");

            string expectedText = File.ReadAllText(txtPath).Trim();
            double audioDurationSec = 0;

            try
            {
                using var reader = new WaveFileReader(wavPath);
                audioDurationSec = reader.TotalTime.TotalSeconds;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to read WAV file header for '{wavPath}': {ex.Message}");
            }

            var fileSizeGb = new FileInfo(modelPath).Length / (1024.0 * 1024.0 * 1024.0);

            var benchmarkResult = new ModelBenchmarkResult
            {
                ModelName = modelName,
                ModelType = "Whisper",
                FileSizeGb = fileSizeGb,
                TotalCases = 1
            };

            using var whisperService = new WhisperService();

            // Memory peak tracking
            double peakRamMb = 0;
            double peakVramMb = 0;
            var cts = new CancellationTokenSource();

            var vramCounters = new List<PerformanceCounter>();
            try
            {
                var pid = Process.GetCurrentProcess().Id;
                var category = new PerformanceCounterCategory("GPU Process Memory");
                var instances = category.GetInstanceNames();
                var pidPrefix = $"pid_{pid}_";
                foreach (var instance in instances)
                {
                    if (instance.StartsWith(pidPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        vramCounters.Add(new PerformanceCounter("GPU Process Memory", "Dedicated Usage", instance, true));
                    }
                }
            }
            catch { /* Headless or OS non-supported */ }

            // Start memory polling task
            var memoryMonitorTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    long currentRamBytes = Process.GetCurrentProcess().WorkingSet64;
                    double ramMb = currentRamBytes / (1024.0 * 1024.0);
                    if (ramMb > peakRamMb) peakRamMb = ramMb;

                    if (vramCounters.Count > 0)
                    {
                        float currentVramBytes = 0;
                        foreach (var c in vramCounters)
                        {
                            try { currentVramBytes += c.NextValue(); } catch { }
                        }
                        double vramMb = currentVramBytes / (1024.0 * 1024.0);
                        if (vramMb > peakVramMb) peakVramMb = vramMb;
                    }

                    await Task.Delay(50, cts.Token);
                }
            });

            // Measure initialization time
            var loadSw = Stopwatch.StartNew();
            bool initSuccess = await whisperService.InitializeAsync(modelPath, msg => Console.WriteLine(msg));
            loadSw.Stop();

            benchmarkResult.LoadTimeSec = loadSw.Elapsed.TotalSeconds;

            if (!initSuccess)
            {
                cts.Cancel();
                try { await memoryMonitorTask; } catch { }
                foreach (var c in vramCounters) c.Dispose();

                benchmarkResult.PassedCases = 0;
                benchmarkResult.TestCases.Add(new TestCaseResult
                {
                    TestCaseName = "Audio Transcription Accuracy",
                    InputText = "Audio File",
                    OutputText = "INIT_FAILED",
                    Passed = false,
                    AssertionSummary = "Whisper model could not be initialized."
                });

                PerformanceReportGenerator.AddResult(benchmarkResult);
                return;
            }

            var transSw = Stopwatch.StartNew();
            List<string> transcribedSegments;
            using (var audioStream = File.OpenRead(wavPath))
            {
                transcribedSegments = await whisperService.TranscribeAsync(audioStream, null, onDebugMessage: msg => Console.WriteLine(msg));
            }
            transSw.Stop();

            double transDurationSec = transSw.Elapsed.TotalSeconds;
            double rtf = transDurationSec / Math.Max(audioDurationSec, 0.01);

            var transcribedText = string.Join(" ", transcribedSegments).Trim();

            // Normalize both strings before comparison
            var normalizedTranscribed = NormalizeAudioText(transcribedText);
            var normalizedExpected = NormalizeAudioText(expectedText);

            // Calculate WER (Lower is better. 0.0 is perfect, 0.15 is 15% error rate)
            double wer = ComputeWordErrorRate(normalizedTranscribed, normalizedExpected);
            double accuracy = Math.Max(0, 1.0 - wer); // Convert to an accuracy percentage
            bool passed = accuracy >= 0.85; // Target 85% word accuracy benchmark

            if (passed)
            {
                benchmarkResult.PassedCases = 1;
            }

            benchmarkResult.TestCases.Add(new TestCaseResult
            {
                TestCaseName = "Audio Transcription Accuracy",
                InputText = $"Audio Length: {audioDurationSec:F2}s",
                OutputText = transcribedText,
                DurationSec = transDurationSec,
                SpeedTpsOrRtf = rtf,
                Passed = passed,
                AssertionSummary = $"WER: {wer * 100:F1}% | Word Accuracy: {accuracy * 100:F1}% (Expected >= 85%) | Ground Truth: \"{expectedText}\""
            });

            // Stop memory monitoring
            cts.Cancel();
            try { await memoryMonitorTask; } catch { }
            foreach (var c in vramCounters) c.Dispose();

            benchmarkResult.AvgDurationSec = transDurationSec;
            benchmarkResult.AvgSpeedTpsOrRtf = rtf;
            benchmarkResult.PeakRamMb = peakRamMb;
            benchmarkResult.PeakVramMb = peakVramMb;

            benchmarkResult.AccuracyScore = accuracy;

            // Add result & trigger report update
            PerformanceReportGenerator.AddResult(benchmarkResult);


            // Clean up resources explicitly
            await whisperService.DisposeResourcesAsync(msg => Console.WriteLine(msg));

            // Force Garbage Collection and wait for cooldown to let GPU driver free VRAM
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(1500);
        }

        private static double ComputeWordErrorRate(string actual, string expected)
        {
            if (string.IsNullOrWhiteSpace(expected)) return string.IsNullOrWhiteSpace(actual) ? 0.0 : 1.0;
            if (string.IsNullOrWhiteSpace(actual)) return 1.0;

            var actualWords = actual.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var expectedWords = expected.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int n = actualWords.Length;
            int m = expectedWords.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (expectedWords[j - 1] == actualWords[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), // Deletion / Insertion
                        d[i - 1, j - 1] + cost);                    // Substitution
                }
            }

            // WER is total edits divided by the number of words in the expected text
            return (double)d[n, m] / m;
        }

        private static string NormalizeAudioText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // Convert to lowercase
            text = text.ToLowerInvariant();

            // Strip all punctuation (keep only letters, digits, and spaces)
            var sb = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                }
            }

            text = sb.ToString();
            // Basic number normalization
            text = text.Replace(" fifty ", " 50 ").Replace(" one ", " 1 ");

            // Collapse multiple spaces
            return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        }
    }
}
