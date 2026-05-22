using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartDictateAI.Services
{
    public interface IWhisperService : IDisposable
    {
        bool IsInitialized { get; }
        Task<bool> InitializeAsync(string modelPath, Action<string>? onDebugMessage = null);
        Task<List<string>> TranscribeAsync(Stream audioStream, string? promptText, Action<string, string>? onSegmentTranscribed = null, Action<string>? onDebugMessage = null);
        Task DisposeResourcesAsync(Action<string>? onDebugMessage = null);
    }
}
