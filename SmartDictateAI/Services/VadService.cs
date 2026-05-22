using System;
using WebRtcVadSharp;

namespace SmartDictateAI.Services
{
    public class VadService : IVadService
    {
        private WebRtcVad? _vad;
        private const int VAD_FRAME_BYTES = 640;

        public void Initialize(int mode, Action<string>? onDebugMessage = null)
        {
            try
            {
                if (mode < 0) mode = 0;
                if (mode > 3) mode = 3;

                _vad = new WebRtcVad() { OperatingMode = (OperatingMode)mode };
                onDebugMessage?.Invoke($"[VAD] VAD initialized in mode {mode}.");
            }
            catch (Exception ex)
            {
                onDebugMessage?.Invoke($"[VAD] VAD initialization failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    onDebugMessage?.Invoke($"[App] Inner Error: {ex.InnerException.Message}");
                }
                _vad = null;
            }
        }

        public bool HasSpeech(byte[] rawFrame, float gainMultiplier, Action<string>? onDebugMessage = null)
        {
            if (_vad == null)
            {
                // Fallback: If VAD failed to initialize, assume speech so we don't discard everything
                return true;
            }

            try
            {
                // --- SOFTWARE GAIN FOR VAD ---
                // Convert bytes to 16-bit samples, amplify, and convert back
                byte[] amplifiedFrame = new byte[rawFrame.Length];
                for (int i = 0; i < rawFrame.Length && i + 1 < rawFrame.Length; i += 2)
                {
                    short sample = BitConverter.ToInt16(rawFrame, i);
                    // Multiply and clamp to prevent digital clipping
                    int boosted = (int)(sample * gainMultiplier);
                    short clamped = (short)Math.Clamp(boosted, short.MinValue, short.MaxValue);

                    byte[] bytes = BitConverter.GetBytes(clamped);
                    amplifiedFrame[i] = bytes[0];
                    amplifiedFrame[i + 1] = bytes[1];
                }

                // Check speech on amplified frame
                return _vad.HasSpeech(amplifiedFrame, SampleRate.Is16kHz, FrameLength.Is20ms);
            }
            catch (Exception ex)
            {
                onDebugMessage?.Invoke($"[VAD] VAD processing error: {ex.Message}");
                return true; // Return true as a fallback on processing errors to avoid silent drops
            }
        }

        public void SetMode(int mode, Action<string>? onDebugMessage = null)
        {
            try
            {
                if (mode < 0) mode = 0;
                if (mode > 3) mode = 3;

                if (_vad != null)
                {
                    _vad.OperatingMode = (OperatingMode)mode;
                    onDebugMessage?.Invoke($"[VAD] VAD mode updated to {mode}.");
                }
                else
                {
                    Initialize(mode, onDebugMessage);
                }
            }
            catch (Exception ex)
            {
                onDebugMessage?.Invoke($"[VAD] SetVadMode error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _vad?.Dispose();
            _vad = null;
        }
    }
}
