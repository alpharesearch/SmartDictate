using System;
using System.IO;
using Xunit;

namespace SmartDictateAI.PerformanceTests
{
    /// <summary>
    /// A custom xUnit Theory attribute that dynamically skips tests unless the 
    /// RUN_LLM_PERF environment variable is set to "true" or a ".run_perf" file exists.
    /// </summary>
    public class PerformanceTheoryAttribute : TheoryAttribute
    {
        public PerformanceTheoryAttribute()
        {
            var runPerfEnv = Environment.GetEnvironmentVariable("RUN_LLM_PERF");
            bool run = !string.IsNullOrEmpty(runPerfEnv) && runPerfEnv.Equals("true", StringComparison.OrdinalIgnoreCase);

            if (!run)
            {
                try
                {
                    var modelsDir = ModelPathHelper.GetModelsDirectory();
                    if (File.Exists(Path.Combine(modelsDir, ".run_perf")))
                    {
                        run = true;
                    }
                    else
                    {
                        var solutionDir = Directory.GetParent(modelsDir)?.FullName;
                        if (solutionDir != null && File.Exists(Path.Combine(solutionDir, ".run_perf")))
                        {
                            run = true;
                        }
                    }
                }
                catch { /* Fallback if paths cannot be resolved */ }
            }

            if (!run)
            {
                Skip = "Skipped: Set RUN_LLM_PERF=true env var or create an empty '.run_perf' file in the solution root or models/ folder to run.";
            }
        }
    }
}
