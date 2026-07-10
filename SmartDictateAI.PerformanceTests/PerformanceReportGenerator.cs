using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SmartDictateAI.PerformanceTests
{
    public class ModelBenchmarkResult
    {
        public string ModelName { get; set; } = "";
        public string ModelType { get; set; } = ""; // "LLM" or "Whisper"
        public double FileSizeGb { get; set; }
        public double? Temperature { get; set; }
        public double LoadTimeSec { get; set; }
        public double AvgDurationSec { get; set; }
        public double AvgSpeedTpsOrRtf { get; set; } // Tokens/sec for LLM, Real-time factor (RTF) for Whisper
        public double PeakRamMb { get; set; }
        public double PeakVramMb { get; set; }
        public int TotalCases { get; set; }
        public int PassedCases { get; set; }
        public double AccuracyScore { get; set; } // Raw accuracy score (0.0 to 1.0)
        public List<TestCaseResult> TestCases { get; set; } = new();
    }

    public class TestCaseResult
    {
        public string TestCaseName { get; set; } = "";
        public string InputText { get; set; } = "";
        public string OutputText { get; set; } = "";
        public double DurationSec { get; set; }
        public double SpeedTpsOrRtf { get; set; }
        public bool Passed { get; set; }
        public string AssertionSummary { get; set; } = "";
    }

    public static class PerformanceReportGenerator
    {
        private static readonly List<ModelBenchmarkResult> _results = new();
        private static readonly object _lock = new();

        public static void AddResult(ModelBenchmarkResult result)
        {
            lock (_lock)
            {
                // Remove previous runs of the same model and temperature to avoid duplicates if rerun
                _results.RemoveAll(r => r.ModelName.Equals(result.ModelName, StringComparison.OrdinalIgnoreCase) && r.Temperature == result.Temperature);
                _results.Add(result);
                
                // Write report incrementally so it updates during the test run
                WriteReportFile();
            }
        }

        private static double CalculateLLMScore(ModelBenchmarkResult res, double maxTps)
        {
            if (res.LoadTimeSec == 0 || res.AvgSpeedTpsOrRtf == 0) return 0;
            
            // 85% weight on correctness
            double correctnessPart = res.AccuracyScore * 85.0;
            // 15% weight on inference speed (relative to highest TPS in the run)
            double speedPart = (res.AvgSpeedTpsOrRtf / maxTps) * 15.0;
            
            return Math.Round(correctnessPart + speedPart, 1);
        }

        private static double CalculateWhisperScore(ModelBenchmarkResult res, double minRtf)
        {
            if (res.LoadTimeSec == 0 || res.AvgSpeedTpsOrRtf == 0) return 0;
            
            // 85% weight on transcription similarity
            double correctnessPart = res.AccuracyScore * 85.0;
            // 15% weight on speed (Relative RTF where lower RTF gets more points)
            double speedPart = (minRtf / Math.Max(0.0001, res.AvgSpeedTpsOrRtf)) * 15.0;
            
            return Math.Round(correctnessPart + speedPart, 1);
        }

        private static string GetLetterGrade(double score)
        {
            if (score >= 90.0) return "A";
            if (score >= 80.0) return "B";
            if (score >= 70.0) return "C";
            if (score >= 50.0) return "D";
            return "F";
        }

        private static string GenerateInlineDiff(string original, string actual)
        {
            var origWords = original.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var actWords = actual.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int[,] lcs = new int[origWords.Length + 1, actWords.Length + 1];
            for (int i = 1; i <= origWords.Length; i++)
            {
                for (int j = 1; j <= actWords.Length; j++)
                {
                    if (origWords[i - 1].Equals(actWords[j - 1], StringComparison.OrdinalIgnoreCase))
                    {
                        lcs[i, j] = lcs[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        lcs[i, j] = Math.Max(lcs[i - 1, j], lcs[i, j - 1]);
                    }
                }
            }

            var diffElements = new List<string>();
            int x = origWords.Length;
            int y = actWords.Length;

            while (x > 0 || y > 0)
            {
                if (x > 0 && y > 0 && origWords[x - 1].Equals(actWords[y - 1], StringComparison.OrdinalIgnoreCase))
                {
                    diffElements.Add(origWords[x - 1]);
                    x--;
                    y--;
                }
                else if (y > 0 && (x == 0 || lcs[x, y - 1] >= lcs[x - 1, y]))
                {
                    diffElements.Add($"<ins>{actWords[y - 1]}</ins>");
                    y--;
                }
                else if (x > 0 && (y == 0 || lcs[x, y - 1] < lcs[x - 1, y]))
                {
                    diffElements.Add($"<del>{origWords[x - 1]}</del>");
                    x--;
                }
            }

            diffElements.Reverse();
            return string.Join(" ", diffElements);
        }

        private static void WriteReportFile()
        {
            try
            {
                var root = ModelPathHelper.GetModelsDirectory();
                var solutionRoot = Directory.GetParent(root)?.FullName ?? root;
                var reportPath = Path.Combine(solutionRoot, "llm_performance_report.md");

                var sb = new StringBuilder();
                sb.AppendLine("# Local AI Models Performance Report");
                sb.AppendLine();
                sb.AppendLine($"**Generated on:** {DateTime.Now:yyyy-MM-dd HH:mm:ss} (Local Time)");
                sb.AppendLine($"- **OS:** {Environment.OSVersion}");
                sb.AppendLine($"- **Processor Count:** {Environment.ProcessorCount}");
                sb.AppendLine($"- **Architecture:** {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
                sb.AppendLine();

                // Split results by type
                var llmResults = _results.Where(r => r.ModelType == "LLM").ToList();
                var whisperResults = _results.Where(r => r.ModelType == "Whisper").ToList();

                // Normalize parameters
                double maxLlmTps = llmResults.Count > 0 ? llmResults.Max(r => r.AvgSpeedTpsOrRtf) : 0.001;
                if (maxLlmTps <= 0) maxLlmTps = 0.001;

                double minWhisperRtf = whisperResults.Count > 0 ? whisperResults.Where(r => r.AvgSpeedTpsOrRtf > 0).Select(r => r.AvgSpeedTpsOrRtf).DefaultIfEmpty(999.0).Min() : 999.0;
                if (minWhisperRtf <= 0) minWhisperRtf = 0.01;

                // Rank results
                var rankedLlm = llmResults
                    .Select(r => new { Result = r, Score = CalculateLLMScore(r, maxLlmTps) })
                    .OrderByDescending(x => x.Score)
                    .ToList();

                var rankedWhisper = whisperResults
                    .Select(r => new { Result = r, Score = CalculateWhisperScore(r, minWhisperRtf) })
                    .OrderByDescending(x => x.Score)
                    .ToList();

                // LLM Table
                sb.AppendLine("## LLM Models Performance Summary (Ranked)");
                sb.AppendLine();
                sb.AppendLine("Models are ranked based on a composite rating: **85% Correctness + 15% Generation Speed (TPS)**.");
                sb.AppendLine();
                sb.AppendLine("| Rank | Model Name | Temp | Score | Grade | Size | Load Time | Total Time | Avg Speed | Peak RAM | Peak VRAM | Correctness |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");

                for (int i = 0; i < rankedLlm.Count; i++)
                {
                    var r = rankedLlm[i].Result;
                    var score = rankedLlm[i].Score;
                    var grade = GetLetterGrade(score);
                    var speedStr = $"{r.AvgSpeedTpsOrRtf:F1} tok/s";
                    var ramStr = r.PeakRamMb > 0 ? $"{r.PeakRamMb:F0} MB" : "N/A";
                    var vramStr = r.PeakVramMb > 0 ? $"{r.PeakVramMb:F0} MB" : "N/A";
                    var totalTime = r.LoadTimeSec + r.TestCases.Sum(tc => tc.DurationSec);
                    var tempStr = r.Temperature.HasValue ? r.Temperature.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "N/A";

                    sb.AppendLine($"| {i + 1} | **{r.ModelName}** | {tempStr} | **{score:F1}/100** | **{grade}** | {r.FileSizeGb:F2} GB | {r.LoadTimeSec:F2}s | {totalTime:F2}s | {speedStr} | {ramStr} | {vramStr} | {r.PassedCases}/{r.TotalCases} ({r.AccuracyScore * 100:F0}%) |");
                }
                sb.AppendLine();

                // Whisper Table
                sb.AppendLine("## Whisper Models Performance Summary (Ranked)");
                sb.AppendLine();
                sb.AppendLine("Models are ranked based on a composite rating: **85% Transcription Similarity + 15% Real-Time Factor (RTF)**.");
                sb.AppendLine();

                // Extract ground truth from the first Whisper result to print it once at the top of the Whisper section
                string globalGroundTruth = "";
                var firstWhisperWithGt = whisperResults.FirstOrDefault(r => r.TestCases.Any(tc => (tc.AssertionSummary ?? "").Contains("Ground Truth: \"")));
                if (firstWhisperWithGt != null)
                {
                    var firstTc = firstWhisperWithGt.TestCases.First(tc => (tc.AssertionSummary ?? "").Contains("Ground Truth: \""));
                    string assertionSummary = firstTc.AssertionSummary ?? "";
                    int startIdx = assertionSummary.IndexOf("Ground Truth: \"") + 15;
                    int endIdx = assertionSummary.LastIndexOf("\"");
                    if (endIdx > startIdx && startIdx >= 15)
                    {
                        globalGroundTruth = assertionSummary.Substring(startIdx, endIdx - startIdx);
                    }
                }

                if (!string.IsNullOrEmpty(globalGroundTruth))
                {
                    sb.AppendLine("#### Whisper Ground Truth Reference");
                    sb.AppendLine();
                    sb.AppendLine($"> {globalGroundTruth}");
                    sb.AppendLine();
                }

                sb.AppendLine("| Rank | Model Name | Score | Grade | Size | Load Time | Total Time | Speed (RTF) | Peak RAM | Peak VRAM | Accuracy |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");

                for (int i = 0; i < rankedWhisper.Count; i++)
                {
                    var r = rankedWhisper[i].Result;
                    var score = rankedWhisper[i].Score;
                    var grade = GetLetterGrade(score);
                    var speedStr = $"{r.AvgSpeedTpsOrRtf:F4}x RTF (lower is better)";
                    var ramStr = r.PeakRamMb > 0 ? $"{r.PeakRamMb:F0} MB" : "N/A";
                    var vramStr = r.PeakVramMb > 0 ? $"{r.PeakVramMb:F0} MB" : "N/A";
                    var totalTime = r.LoadTimeSec + r.TestCases.Sum(tc => tc.DurationSec);

                    sb.AppendLine($"| {i + 1} | **{r.ModelName}** | **{score:F1}/100** | **{grade}** | {r.FileSizeGb:F2} GB | {r.LoadTimeSec:F2}s | {totalTime:F2}s | {speedStr} | {ramStr} | {vramStr} | {r.AccuracyScore * 100:F1}% |");
                }
                sb.AppendLine();

                // Detailed Cases Section
                sb.AppendLine("## Detailed Test Cases");
                sb.AppendLine();

                // Output details in ranked order (LLM first, Whisper second)
                foreach (var item in rankedLlm)
                {
                    var r = item.Result;
                    var tempSuffix = r.Temperature.HasValue ? $" (temp: {r.Temperature.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)})" : "";
                    sb.AppendLine($"### [LLM Rank {rankedLlm.IndexOf(item) + 1}] {r.ModelName}{tempSuffix} (Score: {item.Score:F1}/100 - Grade {GetLetterGrade(item.Score)})");
                    sb.AppendLine();
                    sb.AppendLine($"- **File Size:** {r.FileSizeGb:F2} GB");
                    sb.AppendLine($"- **Load Time:** {r.LoadTimeSec:F2} seconds");
                    sb.AppendLine($"- **Success Rate:** {r.PassedCases} / {r.TotalCases}");
                    sb.AppendLine();

                    sb.AppendLine("| Test Case | Input | Output | Speed | Status | Notes |");
                    sb.AppendLine("|---|---|---|---|---|---|");

                    foreach (var tc in r.TestCases)
                    {
                        var status = tc.Passed ? "✅ Pass" : "❌ Fail";
                        var speedStr = $"{tc.SpeedTpsOrRtf:F1} tok/s";
                        var safeInput = tc.InputText.Replace("\r", "").Replace("\n", " ").Replace("|", "\\|").Trim();
                        var safeOutput = tc.OutputText.Replace("\r", "").Replace("\n", " ").Replace("|", "\\|").Trim();
                        var safeNotes = tc.AssertionSummary.Replace("\r", "").Replace("\n", " ").Replace("|", "\\|").Trim();

                        sb.AppendLine($"| {tc.TestCaseName} | `{safeInput}` | `{safeOutput}` | {speedStr} | {status} | {safeNotes} |");
                    }
                    sb.AppendLine();
                }

                foreach (var item in rankedWhisper)
                {
                    var r = item.Result;
                    sb.AppendLine($"### [Whisper Rank {rankedWhisper.IndexOf(item) + 1}] {r.ModelName} (Score: {item.Score:F1}/100 - Grade {GetLetterGrade(item.Score)})");
                    sb.AppendLine();
                    sb.AppendLine($"- **File Size:** {r.FileSizeGb:F2} GB");
                    sb.AppendLine($"- **Load Time:** {r.LoadTimeSec:F2} seconds");
                    sb.AppendLine($"- **Success Rate:** {r.PassedCases} / {r.TotalCases}");
                    sb.AppendLine();

                    // Whisper comparison info
                    string groundTruthText = "";
                    string transcribedText = "";
                    double durationSec = 0;
                    double rtf = 0;
                    bool passed = false;
                    string assertionSummary = "";

                    foreach (var tc in r.TestCases)
                    {
                        var status = tc.Passed ? "✅ Pass" : "❌ Fail";
                        var speedStr = $"{tc.SpeedTpsOrRtf:F4}x RTF";
                        var safeInput = tc.InputText.Replace("\r", "").Replace("\n", " ").Replace("|", "\\|").Trim();
                        var safeOutput = tc.OutputText.Replace("\r", "").Replace("\n", " ").Replace("|", "\\|").Trim();
                        var rawNotes = tc.AssertionSummary ?? "";
                        if (rawNotes.Contains(" | Ground Truth: \""))
                        {
                            int gtIdx = rawNotes.IndexOf(" | Ground Truth: \"");
                            rawNotes = rawNotes.Substring(0, gtIdx);
                        }
                        var safeNotes = rawNotes.Replace("\r", "").Replace("\n", " ").Replace("|", "\\|").Trim();

                        sb.AppendLine("| Test Case | Input | Output Summary | Speed | Status | Notes |");
                        sb.AppendLine("|---|---|---|---|---|---|");
                        sb.AppendLine($"| {tc.TestCaseName} | `{safeInput}` | `See diff below` | {speedStr} | {status} | {safeNotes} |");
                        sb.AppendLine();

                        transcribedText = tc.OutputText;
                        durationSec = tc.DurationSec;
                        rtf = tc.SpeedTpsOrRtf;
                        passed = tc.Passed;
                        assertionSummary = tc.AssertionSummary ?? "";
                    }

                    // Extract ground truth from the assertionSummary if possible
                    if (assertionSummary.Contains("Ground Truth: \""))
                    {
                        int startIdx = assertionSummary.IndexOf("Ground Truth: \"") + 15;
                        int endIdx = assertionSummary.LastIndexOf("\"");
                        if (endIdx > startIdx && startIdx >= 15)
                        {
                            groundTruthText = assertionSummary.Substring(startIdx, endIdx - startIdx);
                        }
                    }

                    if (!string.IsNullOrEmpty(groundTruthText) && !string.IsNullOrEmpty(transcribedText))
                    {
                        var inlineDiff = GenerateInlineDiff(groundTruthText, transcribedText);
                        sb.AppendLine("#### Transcription Word Diff");
                        sb.AppendLine();
                        sb.AppendLine("Showing word-by-word difference (<del style=\"background-color:#ffeef0;color:#b31d28;\">deleted original word</del> &nbsp; <ins style=\"background-color:#e6ffed;color:#22863a;text-decoration:none;\">inserted transcribed word</ins>):");
                        sb.AppendLine();
                        sb.AppendLine($"> {inlineDiff}");
                        sb.AppendLine();
                    }
                }

                File.WriteAllText(reportPath, sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Performance Tests] Error generating report file: {ex.Message}");
            }
        }
    }
}
