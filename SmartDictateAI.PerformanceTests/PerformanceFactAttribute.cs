using System;
using Xunit;

namespace SmartDictateAI.PerformanceTests
{
    /// <summary>
    /// A custom xUnit Fact attribute that dynamically skips tests unless the 
    /// RUN_LLM_PERF environment variable is set to "true".
    /// </summary>
    public class PerformanceFactAttribute : FactAttribute
    {
        public PerformanceFactAttribute()
        {
            var runPerf = Environment.GetEnvironmentVariable("RUN_LLM_PERF");
            if (string.IsNullOrEmpty(runPerf) || !runPerf.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                Skip = "Skipped: RUN_LLM_PERF environment variable is not set to 'true'. Set RUN_LLM_PERF=true to run.";
            }
        }
    }
}
