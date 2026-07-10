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

        /// <summary>
        /// Locates the specified asset file. First checks the output directory's Assets folder,
        /// then falls back to walking up to the source project's Assets directory.
        /// </summary>
        public static string GetAssetPath(string fileName)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 1. Check source project folder by walking up to solution root (development mode)
            var dir = new DirectoryInfo(baseDir);
            while (dir != null)
            {
                var target = Path.Combine(dir.FullName, "SmartDictateAI.PerformanceTests", "Assets", fileName);
                if (File.Exists(target))
                {
                    return target;
                }
                dir = dir.Parent;
            }

            // 2. Check output directory (fallback for published/standalone execution)
            var localAsset = Path.Combine(baseDir, "Assets", fileName);
            if (File.Exists(localAsset))
            {
                return localAsset;
            }

            throw new FileNotFoundException($"Could not find asset '{fileName}' in source folders or build output.");
        }
    }
}
