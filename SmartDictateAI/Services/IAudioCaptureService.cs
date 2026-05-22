using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace SmartDictateAI.Services
{
    public interface IAudioCaptureService : IDisposable
    {
        bool IsRecording { get; }
        void StartRecording(int deviceNumber, WaveFormat waveFormat);
        void StopRecording();
        event EventHandler<WaveInEventArgs>? DataAvailable;
        event EventHandler<StoppedEventArgs>? RecordingStopped;
        List<(int Index, string Name)> GetAvailableMicrophones();
    }
}
