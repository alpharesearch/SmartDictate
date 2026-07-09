using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SmartDictateAI.Services;
using System.Threading;

namespace SmartDictateAI.PerformanceTests
{
    internal class TestPrompt
    {
        public string Name { get; set; } = "";
        public string Input { get; set; } = "";
        public string[] ExpectedSubstrings { get; set; } = Array.Empty<string>();
        public string[] ForbiddenSubstrings { get; set; } = Array.Empty<string>();
    }

    public class LLMPerformanceTests
    {


        private static readonly List<TestPrompt> TestCases = new()
        {
            new TestPrompt
            {
                Name = "Grammar & Spelling Correction",
                Input = "she do not likes the new apple phone because it are too big",
                ExpectedSubstrings = new[] { "does not", "like", "Apple", "is too big" },
                ForbiddenSubstrings = new[] { "do not likes", "it are" }
            },
            new TestPrompt
            {
                Name = "Filler Word Removal",
                Input = "uh so basically like I went to the store and um bought some milk",
                ExpectedSubstrings = new[] { "went to the store", "bought some milk" },
                ForbiddenSubstrings = new[] { "uh", "um", "basically like" }
            },
            new TestPrompt
            {
                Name = "Custom Vocabulary Formatting",
                Input = "we configured the siemetic step 7 s7 1500 plc using tia portal",
                ExpectedSubstrings = new[] { "SIMATIC", "STEP 7", "S7-1500", "TIA Portal" },
                ForbiddenSubstrings = new[] { "siemetic", "step 7", "s7 1500", "tia portal" }
            }
        };

        public static IEnumerable<object[]> GetLLMModels()
        {
            try
            {
                var llmDir = ModelPathHelper.GetLLMModelsDirectory();
                var ggufFiles = Directory.GetFiles(llmDir, "*.gguf");

                if (ggufFiles.Length == 0)
                {
                    var rootDir = ModelPathHelper.GetModelsDirectory();
                    ggufFiles = Directory.GetFiles(rootDir, "*.gguf");
                }

                if (ggufFiles.Length > 0)
                {
                    return ggufFiles.Select(f => new object[] { Path.GetFileName(f), f });
                }
            }
            catch
            {
                // Directory scanning failed or directory not found during discovery
            }

            // Fallback list of models so they are ALWAYS listed in the Test Explorer
            return new List<object[]>
            {
                new object[] { "gemma-4-E4B-it-Q4_K_M.gguf", "" },
                new object[] { "Llama-3.2-3B-Instruct-Q8_0.gguf", "" },
                new object[] { "qwen2-0_5b-instruct-q8_0.gguf", "" },
                new object[] { "gemma-4-E2B-it-Q4_0.gguf", "" }
            };
        }

        [Theory]
        [MemberData(nameof(GetLLMModels))]
        [Trait("Category", "Performance")]
        public async Task Benchmark_LLM_Model(string modelName, string modelPath)
        {
            // Runtime skip check
            if (!PerformanceTestHelper.ShouldRun())
            {
                Console.WriteLine($"Bypassing LLM benchmark for {modelName} (performance runs are not enabled).");
                return;
            }

            // Resolve path if using the fallback model list
            if (string.IsNullOrEmpty(modelPath))
            {
                try
                {
                    var llmDir = ModelPathHelper.GetLLMModelsDirectory();
                    modelPath = Path.Combine(llmDir, modelName);
                    if (!File.Exists(modelPath))
                    {
                        var rootDir = ModelPathHelper.GetModelsDirectory();
                        modelPath = Path.Combine(rootDir, modelName);
                    }
                }
                catch
                {
                    modelPath = modelName; // Fallback
                }
            }

            Assert.True(File.Exists(modelPath), $"Model file '{modelName}' was not found on disk at '{modelPath}'. Place it in 'models/llm/' to run the benchmark.");

            var fileSizeGb = new FileInfo(modelPath).Length / (1024.0 * 1024.0 * 1024.0);

            var benchmarkResult = new ModelBenchmarkResult
            {
                ModelName = modelName,
                ModelType = "LLM",
                FileSizeGb = fileSizeGb,
                TotalCases = TestCases.Count
            };

            // Setup AppSettings specifically for this model benchmark
            var settings = new AppSettings
            {
                LocalLLMModelPath = modelPath,
                LLMContextSize = 2048, // Use a conservative context size for benchmark compatibility
                LLMSeed = 42,          // Fix the seed for deterministic outputs
                LLMTemperature = 0.2f, // Lower temperature for more deterministic/stable generation
                LLMMaxOutputTokens = 128,
                UseGpu = true          // Attempt GPU usage
            };

            using var llmService = new LLMService();

            // Memory peak tracking
            double peakRamMb = 0;
            double peakVramMb = 0;
            var cts = new CancellationTokenSource();

            // Setup Windows performance counters for VRAM if available
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

            // Measure initialization / model loading time
            var loadSw = Stopwatch.StartNew();
            bool initSuccess = llmService.Initialize(settings.LocalLLMModelPath, settings.LLMContextSize, settings.UseGpu, msg => Console.WriteLine(msg));
            loadSw.Stop();

            benchmarkResult.LoadTimeSec = loadSw.Elapsed.TotalSeconds;

            if (!initSuccess)
            {
                cts.Cancel();
                try { await memoryMonitorTask; } catch { }
                foreach (var c in vramCounters) c.Dispose();

                benchmarkResult.PassedCases = 0;
                benchmarkResult.TestCases = TestCases.Select(tc => new TestCaseResult
                {
                    TestCaseName = tc.Name,
                    InputText = tc.Input,
                    OutputText = "INIT_FAILED",
                    Passed = false,
                    AssertionSummary = "Model could not be initialized."
                }).ToList();

                PerformanceReportGenerator.AddResult(benchmarkResult);
                return;
            }

            double totalSpeedTps = 0;
            double totalDuration = 0;

            foreach (var testCase in TestCases)
            {
                double currentTps = 0;
                var debugLogs = new List<string>();

                var testCaseSw = Stopwatch.StartNew();
                var refinedOutput = await llmService.RefineTextAsync(
                    testCase.Input, 
                    settings, 
                    onDebugMessage: msg =>
                    {
                        debugLogs.Add(msg);
                        Console.WriteLine(msg);

                        if (msg.Contains("[LLM] Done | streamParts="))
                        {
                            // Parse speed: "[LLM] Done | streamParts=25 | sec=1.23 | 20.3 tok/s-ish"
                            var parts = msg.Split('|');
                            foreach (var part in parts)
                            {
                                if (part.Contains("tok/s-ish"))
                                {
                                    var valStr = part.Replace("tok/s-ish", "").Trim();
                                    if (double.TryParse(valStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedTps))
                                    {
                                        currentTps = parsedTps;
                                    }
                                }
                            }
                        }
                    });
                testCaseSw.Stop();

                double caseDurationSec = testCaseSw.Elapsed.TotalSeconds;
                totalDuration += caseDurationSec;

                // Fallback speed calculation if debug log parsing failed
                if (currentTps == 0 && caseDurationSec > 0)
                {
                    // Estimate token count as character count / 4
                    int estimatedTokens = Math.Max(1, refinedOutput.Length / 4);
                    currentTps = estimatedTokens / caseDurationSec;
                }
                totalSpeedTps += currentTps;

                // Evaluate Assertions (case-sensitive Ordinal comparison)
                var passedExpected = testCase.ExpectedSubstrings.Where(sub => refinedOutput.Contains(sub, StringComparison.Ordinal)).ToList();
                var failedExpected = testCase.ExpectedSubstrings.Where(sub => !refinedOutput.Contains(sub, StringComparison.Ordinal)).ToList();
                var triggeredForbidden = testCase.ForbiddenSubstrings.Where(sub => refinedOutput.Contains(sub, StringComparison.Ordinal)).ToList();

                bool passed = failedExpected.Count == 0 && triggeredForbidden.Count == 0;
                if (passed)
                {
                    benchmarkResult.PassedCases++;
                }

                var notes = new List<string>();
                if (failedExpected.Count > 0)
                {
                    notes.Add($"Missing required: {string.Join(", ", failedExpected.Select(s => $"\"{s}\""))}");
                }
                if (triggeredForbidden.Count > 0)
                {
                    notes.Add($"Failed to correct: {string.Join(", ", triggeredForbidden.Select(s => $"\"{s}\""))}");
                }
                if (passed)
                {
                    notes.Add("Correct spelling & grammar applied successfully.");
                }

                benchmarkResult.TestCases.Add(new TestCaseResult
                {
                    TestCaseName = testCase.Name,
                    InputText = testCase.Input,
                    OutputText = refinedOutput,
                    DurationSec = caseDurationSec,
                    SpeedTpsOrRtf = currentTps,
                    Passed = passed,
                    AssertionSummary = string.Join(" | ", notes)
                });
            }

            // Stop memory monitoring
            cts.Cancel();
            try { await memoryMonitorTask; } catch { }
            foreach (var c in vramCounters) c.Dispose();

            benchmarkResult.AvgDurationSec = totalDuration / TestCases.Count;
            benchmarkResult.AvgSpeedTpsOrRtf = totalSpeedTps / TestCases.Count;
            benchmarkResult.PeakRamMb = peakRamMb;
            benchmarkResult.PeakVramMb = peakVramMb;

            benchmarkResult.AccuracyScore = TestCases.Count > 0 ? (double)benchmarkResult.PassedCases / TestCases.Count : 0;

            // Add result & trigger report update
            PerformanceReportGenerator.AddResult(benchmarkResult);


            // Clean up resources explicitly
            await llmService.DisposeResourcesAsync(msg => Console.WriteLine(msg));
        }
    }
}
