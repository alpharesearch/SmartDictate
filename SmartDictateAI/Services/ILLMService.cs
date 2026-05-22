using System;
using System.Threading.Tasks;

namespace SmartDictateAI.Services
{
    public interface ILLMService : IDisposable
    {
        bool IsInitialized { get; }
        bool Initialize(string modelPath, int contextSize, bool useGpu, Action<string>? onDebugMessage = null);
        Task<string> RefineTextAsync(string inputText, AppSettings settings, string systemPromptOverride = "", string userPromptOverride = "", Action<string>? onDebugMessage = null);
        Task DisposeResourcesAsync(Action<string>? onDebugMessage = null);
    }
}
