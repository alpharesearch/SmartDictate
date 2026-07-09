using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SmartDictateAI.PerformanceTests
{
    public class ModelBenchmarkResult
    {
        public string ModelName { get; set; } = "";
        public string ModelType { get; set; } = ""; // "LLM" or "Whisper"
        public double FileSizeGb { get; set; }
        public double LoadTimeSec { get; set; }
        public double AvgDurationSec { get; set; }
        public double AvgSpeedTpsOrRtf { get; set; } // Tokens/sec for LLM, Real-time factor (RTF) for Whisper
        public double PeakRamMb { get; set; }
        public double PeakVramMb { get; set; }
        public int TotalCases { get; set; }
        public int PassedCases { get; set; }
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
                // Remove previous runs of the same model to avoid duplicates if rerun
                _results.RemoveAll(r => r.ModelName.Equals(result.ModelName, StringComparison.OrdinalIgnoreCase));
                _results.Add(result);
                
                // Write report incrementally so it updates during the test run
                WriteReportFile();
            }
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

                sb.AppendLine("## Summary Table");
                sb.AppendLine();
                sb.AppendLine("| Model Name | Type | Size (GB) | Load Time (s) | Avg Speed | Peak RAM (MB) | Peak VRAM (MB) | Correctness |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|");

                foreach (var res in _results)
                {
                    var speedStr = res.ModelType == "LLM" 
                        ? $"{res.AvgSpeedTpsOrRtf:F1} tok/s" 
                        : $"{res.AvgSpeedTpsOrRtf:F2}x RTF (lower is better)";

                    var ramStr = res.PeakRamMb > 0 ? $"{res.PeakRamMb:F0} MB" : "N/A";
                    var vramStr = res.PeakVramMb > 0 ? $"{res.PeakVramMb:F0} MB" : "N/A";

                    sb.AppendLine($"| **{res.ModelName}** | {res.ModelType} | {res.FileSizeGb:F2} GB | {res.LoadTimeSec:F2}s | {speedStr} | {ramStr} | {vramStr} | {res.PassedCases}/{res.TotalCases} ({((double)res.PassedCases/res.TotalCases)*100:F0}%) |");
                }
                sb.AppendLine();

                sb.AppendLine("## Detailed Test Cases");
                sb.AppendLine();

                foreach (var res in _results)
                {
                    sb.AppendLine($"### Model: {res.ModelName} ({res.ModelType})");
                    sb.AppendLine();
                    sb.AppendLine($"- **File Size:** {res.FileSizeGb:F2} GB");
                    sb.AppendLine($"- **Load Time:** {res.LoadTimeSec:F2} seconds");
                    sb.AppendLine($"- **Success Rate:** {res.PassedCases} / {res.TotalCases}");
                    sb.AppendLine();

                    sb.AppendLine("| Test Case | Input | Output | Speed | Status | Notes |");
                    sb.AppendLine("|---|---|---|---|---|---|");

                    foreach (var tc in res.TestCases)
                    {
                        var status = tc.Passed ? "✅ Pass" : "❌ Fail";
                        var speedStr = res.ModelType == "LLM" 
                            ? $"{tc.SpeedTpsOrRtf:F1} tok/s" 
                            : $"{tc.SpeedTpsOrRtf:F2}x RTF";

                        // Escape characters that break markdown tables
                        var safeInput = tc.InputText.Replace("\r", "").Replace("\n", " ").Replace("|", "\\|").Trim();
                        var safeOutput = tc.OutputText.Replace("\r", "").Replace("\n", " ").Replace("|", "\\|").Trim();
                        var safeNotes = tc.AssertionSummary.Replace("\r", "").Replace("\n", " ").Replace("|", "\\|").Trim();

                        sb.AppendLine($"| {tc.TestCaseName} | `{safeInput}` | `{safeOutput}` | {speedStr} | {status} | {safeNotes} |");
                    }
                    sb.AppendLine();
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
