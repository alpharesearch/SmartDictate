using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SmartDictateAI;
using SmartDictateAI.Services;
using NAudio.Wave;
using Xunit;

namespace SmartDictateAI.Tests
{
    public class MockSettingsService : ISettingsService
    {
        public AppSettings Settings { get; set; } = new AppSettings();
        public string? SavedFilePath { get; private set; }
        public AppSettings? SavedSettings { get; private set; }

        public AppSettings LoadSettings(string filePath, Action<string>? onDebugMessage = null)
        {
            return Settings;
        }

        public void SaveSettings(string filePath, AppSettings settings, Action<string>? onDebugMessage = null)
        {
            SavedFilePath = filePath;
            SavedSettings = settings;
        }
    }

    public class MockVadService : IVadService
    {
        public int Mode { get; private set; }
        public bool ShouldHasSpeechReturn { get; set; } = false;
        public bool HasSpeechCalled { get; private set; }
        public bool IsDisposed { get; private set; }

        public void Initialize(int mode, Action<string>? onDebugMessage = null)
        {
            Mode = mode;
        }

        public bool HasSpeech(byte[] rawFrame, float gainMultiplier, Action<string>? onDebugMessage = null)
        {
            HasSpeechCalled = true;
            return ShouldHasSpeechReturn;
        }

        public void SetMode(int mode, Action<string>? onDebugMessage = null)
        {
            Mode = mode;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public class MockWhisperService : IWhisperService
    {
        public bool IsInitialized { get; set; } = false;
        public bool ShouldInitializeAsyncReturn { get; set; } = true;
        public bool InitializeAsyncCalled { get; private set; }
        public bool DisposeResourcesAsyncCalled { get; private set; }
        public bool IsDisposed { get; private set; }
        public List<string> TranscriptionSegments { get; set; } = new List<string> { "Hello world" };

        public Task<bool> InitializeAsync(string modelPath, Action<string>? onDebugMessage = null)
        {
            InitializeAsyncCalled = true;
            IsInitialized = ShouldInitializeAsyncReturn;
            return Task.FromResult(ShouldInitializeAsyncReturn);
        }

        public Task<List<string>> TranscribeAsync(Stream audioStream, string? promptText, Action<string, string>? onSegmentTranscribed = null, Action<string>? onDebugMessage = null)
        {
            onSegmentTranscribed?.Invoke("00:00:00 -> 00:00:02", "Hello world");
            return Task.FromResult(TranscriptionSegments);
        }

        public Task DisposeResourcesAsync(Action<string>? onDebugMessage = null)
        {
            DisposeResourcesAsyncCalled = true;
            IsInitialized = false;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public class MockLLMService : ILLMService
    {
        public bool IsInitialized { get; set; } = false;
        public bool InitializeCalled { get; private set; }
        public bool DisposeResourcesAsyncCalled { get; private set; }
        public bool IsDisposed { get; private set; }
        public string RefinementOutput { get; set; } = "Refined Hello world";

        public bool Initialize(string modelPath, int contextSize, bool useGpu, Action<string>? onDebugMessage = null)
        {
            InitializeCalled = true;
            IsInitialized = true;
            return true;
        }

        public Task<string> RefineTextAsync(string inputText, AppSettings settings, string systemPromptOverride = "", string userPromptOverride = "", Action<string>? onDebugMessage = null)
        {
            return Task.FromResult(RefinementOutput);
        }

        public Task DisposeResourcesAsync(Action<string>? onDebugMessage = null)
        {
            DisposeResourcesAsyncCalled = true;
            IsInitialized = false;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public class MockAudioCaptureService : IAudioCaptureService
    {
        public bool IsRecording { get; private set; }
        public bool IsDisposed { get; private set; }
        public int? StartedWithDeviceNumber { get; private set; }
        public WaveFormat? StartedWithWaveFormat { get; private set; }
        public List<(int Index, string Name)> Microphones { get; set; } = new List<(int Index, string Name)> { (0, "Mock Microphone") };

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        public void StartRecording(int deviceNumber, WaveFormat waveFormat)
        {
            IsRecording = true;
            StartedWithDeviceNumber = deviceNumber;
            StartedWithWaveFormat = waveFormat;
        }

        public void StopRecording()
        {
            IsRecording = false;
            RecordingStopped?.Invoke(this, new StoppedEventArgs());
        }

        public List<(int Index, string Name)> GetAvailableMicrophones()
        {
            return Microphones;
        }

        public void TriggerDataAvailable(byte[] buffer, int bytesRecorded)
        {
            DataAvailable?.Invoke(this, new WaveInEventArgs(buffer, bytesRecorded));
        }

        public void TriggerRecordingStopped(Exception? ex = null)
        {
            IsRecording = false;
            RecordingStopped?.Invoke(this, new StoppedEventArgs(ex));
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public class TranscriptionServiceTests : IDisposable
    {
        private readonly MockSettingsService _settingsMock;
        private readonly MockVadService _vadMock;
        private readonly MockWhisperService _whisperMock;
        private readonly MockLLMService _llmMock;
        private readonly MockAudioCaptureService _captureMock;
        private readonly TranscriptionService _service;
        private readonly string _tempModelPath;

        public TranscriptionServiceTests()
        {
            _settingsMock = new MockSettingsService();
            _vadMock = new MockVadService();
            _whisperMock = new MockWhisperService();
            _llmMock = new MockLLMService();
            _captureMock = new MockAudioCaptureService();

            _tempModelPath = Path.Combine(Path.GetTempPath(), $"temp_model_{Guid.NewGuid()}.bin");
            File.WriteAllBytes(_tempModelPath, new byte[] { 1, 2, 3 });

            _settingsMock.Settings.ModelFilePath = _tempModelPath;
            _settingsMock.Settings.LocalLLMModelPath = _tempModelPath;

            _service = new TranscriptionService(
                _settingsMock,
                _vadMock,
                _whisperMock,
                _llmMock,
                _captureMock
            );
        }

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            Assert.NotNull(_service.Settings);
            Assert.Equal(_tempModelPath, _settingsMock.Settings.ModelFilePath);
            Assert.Equal(3, _vadMock.Mode); // Default settings VAD mode is 3
        }

        [Fact]
        public async Task StartRecordingAsync_NoMics_ReturnsFalse()
        {
            // Arrange
            _captureMock.Microphones.Clear();

            // Act
            var result = await _service.StartRecordingAsync(0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task StartRecordingAsync_WhisperInitFails_ReturnsFalse()
        {
            // Arrange
            _whisperMock.ShouldInitializeAsyncReturn = false;

            // Act
            var result = await _service.StartRecordingAsync(0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task StartRecordingAsync_Success_StartsRecordingAndFiresEvents()
        {
            // Arrange
            bool eventFired = false;
            _service.RecordingStateChanged += (recording) => {
                if (recording) eventFired = true;
            };

            // Act
            var result = await _service.StartRecordingAsync(0);

            // Assert
            Assert.True(result);
            Assert.True(eventFired);
            Assert.Equal(0, _captureMock.StartedWithDeviceNumber);
            Assert.NotNull(_captureMock.StartedWithWaveFormat);
        }

        [Fact]
        public async Task ChangeWhisperModelPathAsync_WhileRecording_ReturnsFalse()
        {
            // Arrange
            await _service.StartRecordingAsync(0);

            // Act
            var result = await _service.ChangeModelPathAsync(_tempModelPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ChangeWhisperModelPathAsync_NotRecording_Succeeds()
        {
            // Arrange
            var newModelPath = Path.Combine(Path.GetTempPath(), $"new_model_{Guid.NewGuid()}.bin");
            File.WriteAllBytes(newModelPath, new byte[] { 4, 5, 6 });

            try
            {
                // Act
                var result = await _service.ChangeModelPathAsync(newModelPath);

                // Assert
                Assert.True(result);
                Assert.True(_whisperMock.DisposeResourcesAsyncCalled);
                Assert.Equal(newModelPath, _service.Settings.ModelFilePath);
            }
            finally
            {
                if (File.Exists(newModelPath))
                {
                    File.Delete(newModelPath);
                }
            }
        }

        [Fact]
        public async Task ChangeLLMModelPathAsync_WhileRecording_ReturnsFalse()
        {
            // Arrange
            await _service.StartRecordingAsync(0);

            // Act
            var result = await _service.ChangeLLMModelPathAsync(_tempModelPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ChangeLLMModelPathAsync_NotRecording_Succeeds()
        {
            // Arrange
            var newModelPath = Path.Combine(Path.GetTempPath(), $"new_llm_{Guid.NewGuid()}.bin");
            File.WriteAllBytes(newModelPath, new byte[] { 7, 8, 9 });

            try
            {
                // Act
                var result = await _service.ChangeLLMModelPathAsync(newModelPath);

                // Assert
                Assert.True(result);
                Assert.True(_llmMock.DisposeResourcesAsyncCalled);
                Assert.Equal(newModelPath, _service.Settings.LocalLLMModelPath);
            }
            finally
            {
                if (File.Exists(newModelPath))
                {
                    File.Delete(newModelPath);
                }
            }
        }

        [Fact]
        public async Task RecordingStopped_ProcessesAudioAndTriggersEvents()
        {
            // Arrange
            var debugMessages = new List<string>();
            _service.DebugMessageGenerated += (msg) => debugMessages.Add(msg);

            _settingsMock.Settings.ProcessWithLLM = true;
            await _service.StartRecordingAsync(0);

            // Trigger speech detection in VAD so chunk is processed and not discarded
            _vadMock.ShouldHasSpeechReturn = true;

            // Generate frame data of 640 bytes (VAD frame size) and trigger 6 times to exceed 3200 bytes
            var frame = new byte[640];
            _captureMock.TriggerDataAvailable(frame, frame.Length);
            _captureMock.TriggerDataAvailable(frame, frame.Length);
            _captureMock.TriggerDataAvailable(frame, frame.Length);
            _captureMock.TriggerDataAvailable(frame, frame.Length);
            _captureMock.TriggerDataAvailable(frame, frame.Length);
            _captureMock.TriggerDataAvailable(frame, frame.Length);

            string? finalTranscription = null;
            var tcs = new TaskCompletionSource<string>();
            _service.FullTranscriptionReady += (text) =>
            {
                finalTranscription = text;
                tcs.TrySetResult(text);
            };

            // Act
            var stopTask = _service.StopRecording();
            await stopTask;
            var completedText = await Task.WhenAny(tcs.Task, Task.Delay(2000)) == tcs.Task ? await tcs.Task : null;

            // Assert
            try
            {
                Assert.NotNull(completedText);
                Assert.Contains("RAW Transcription", completedText);
                Assert.Contains("Hello world", completedText);
                Assert.Contains("LLM Refined", completedText);
                Assert.Contains("Refined Hello world", completedText);
                Assert.Equal("Hello world", _service.LastRawFilteredText);
                Assert.Equal("Refined Hello world", _service.LastLLMProcessedText);
                Assert.True(_service.WasLastProcessingWithLLM);
            }
            catch (Exception ex)
            {
                throw new Exception($"Test failed. Debug messages:\n{string.Join("\n", debugMessages)}", ex);
            }
        }

        public void Dispose()
        {
            _service.Dispose();
            if (File.Exists(_tempModelPath))
            {
                try { File.Delete(_tempModelPath); } catch { }
            }
        }
    }
}
