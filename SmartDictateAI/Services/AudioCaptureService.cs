using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace SmartDictateAI.Services
{
    public class AudioCaptureService : IAudioCaptureService
    {
        private WaveInEvent? _waveIn;
        public bool IsRecording { get; private set; }

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        public void StartRecording(int deviceNumber, WaveFormat waveFormat)
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("Already recording.");
            }

            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = waveFormat
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            _waveIn.StartRecording();
            IsRecording = true;
        }

        public void StopRecording()
        {
            if (!IsRecording || _waveIn == null)
            {
                return;
            }

            _waveIn.StopRecording();
        }

        public List<(int Index, string Name)> GetAvailableMicrophones()
        {
            var mics = new List<(int, string)>();
            if (WaveIn.DeviceCount == 0)
            {
                return mics;
            }

            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                try
                {
                    mics.Add((i, WaveIn.GetCapabilities(i).ProductName));
                }
                catch
                {
                    mics.Add((i, $"Err mic {i}"));
                }
            }
            return mics;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            DataAvailable?.Invoke(this, e);
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            IsRecording = false;
            
            // Clean up resources for this recording session
            if (_waveIn != null)
            {
                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.RecordingStopped -= OnRecordingStopped;
                
                var currentWaveIn = _waveIn;
                _waveIn = null;

                // Dispose of NAudio on a background thread to avoid UI hangs
                System.Threading.Tasks.Task.Run(() => currentWaveIn.Dispose());
            }

            RecordingStopped?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (_waveIn != null)
            {
                try
                {
                    _waveIn.StopRecording();
                }
                catch { }

                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.RecordingStopped -= OnRecordingStopped;
                _waveIn.Dispose();
                _waveIn = null;
            }
            IsRecording = false;
        }
    }
}
