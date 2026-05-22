using System;

namespace SmartDictateAI.Services
{
    public interface IVadService : IDisposable
    {
        void Initialize(int mode, Action<string>? onDebugMessage = null);
        bool HasSpeech(byte[] rawFrame, float gainMultiplier, Action<string>? onDebugMessage = null);
        void SetMode(int mode, Action<string>? onDebugMessage = null);
    }
}
