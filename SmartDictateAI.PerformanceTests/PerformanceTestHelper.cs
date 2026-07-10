using System;
using System.IO;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SmartDictateAI.PerformanceTests
{


    public static class PerformanceTestHelper
    {
        /// <summary>
        /// Determines if the performance tests should run or be bypassed.
        /// Checks the environment variable RUN_LLM_PERF and the presence of a .run_perf file.
        /// </summary>
        public static bool ShouldRun()
        {
            var runPerfEnv = Environment.GetEnvironmentVariable("RUN_LLM_PERF");
            if (!string.IsNullOrEmpty(runPerfEnv) && runPerfEnv.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                var modelsDir = ModelPathHelper.GetModelsDirectory();
                if (File.Exists(Path.Combine(modelsDir, ".run_perf")))
                {
                    return true;
                }
                
                var solutionDir = Directory.GetParent(modelsDir)?.FullName;
                if (solutionDir != null && File.Exists(Path.Combine(solutionDir, ".run_perf")))
                {
                    return true;
                }
            }
            catch
            {
                // Fallback: check other standard paths if model path helper resolution fails
                try
                {
                    if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".run_perf")))
                    {
                        return true;
                    }
                    if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".run_perf")))
                    {
                        return true;
                    }
                }
                catch { }
            }

            return false;
        }
    }
}
