using System;
using System.IO;

namespace SmartDictateAI.PerformanceTests
{
    public static class ModelPathHelper
    {
        /// <summary>
        /// Walks up from the execution directory to locate the root "models" folder.
        /// </summary>
        public static string GetModelsDirectory()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dir = new DirectoryInfo(baseDir);
            
            while (dir != null)
            {
                var target = Path.Combine(dir.FullName, "models");
                if (Directory.Exists(target))
                {
                    return target;
                }
                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException($"Could not find the 'models' directory in any parent directory starting from {baseDir}");
        }

        /// <summary>
        /// Gets the LLM models subdirectory if it exists, otherwise falls back to the models root.
        /// </summary>
        public static string GetLLMModelsDirectory()
        {
            var root = GetModelsDirectory();
            var sub = Path.Combine(root, "llm");
            return Directory.Exists(sub) ? sub : root;
        }

        /// <summary>
        /// Gets the Whisper models subdirectory if it exists, otherwise falls back to the models root.
        /// </summary>
        public static string GetWhisperModelsDirectory()
        {
            var root = GetModelsDirectory();
            var sub = Path.Combine(root, "whisper");
            return Directory.Exists(sub) ? sub : root;
        }
    }
}
