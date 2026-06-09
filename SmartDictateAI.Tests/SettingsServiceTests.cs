using System;
using System.IO;
using SmartDictateAI.Services;
using Xunit;

namespace SmartDictateAI.Tests
{
    public class SettingsServiceTests : IDisposable
    {
        private readonly string _tempFilePath;

        public SettingsServiceTests()
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"appsettings_test_{Guid.NewGuid()}.json");
        }

        [Fact]
        public void LoadSettings_WhenFileDoesNotExist_CreatesDefaultSettingsAndSavesFile()
        {
            // Arrange
            var service = new SettingsService();
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }

            // Act
            var settings = service.LoadSettings(_tempFilePath);

            // Assert
            Assert.NotNull(settings);
            Assert.True(File.Exists(_tempFilePath));
            Assert.Equal("ggml-base.bin", settings.ModelFilePath);
            Assert.Equal(3, settings.VadMode);
            Assert.Contains(settings.PromptProfiles, p => p.Name == "Strict Proofreader");
        }

        [Fact]
        public void SaveSettings_SavesSettingsToFileCorrectly()
        {
            // Arrange
            var service = new SettingsService();
            var settings = new AppSettings
            {
                ModelFilePath = "custom-whisper.bin",
                VadMode = 1,
                ProcessWithLLM = true,
                LLMContextSize = 8192,
                LLMTemperature = 0.85f,
                VadGainMultiplier = 1.5f,
                NormalMaxChunkDurationSeconds = 12.0,
                DictationSilenceThresholdSeconds = 0.95,
                DictationHotkeyKey = "E"
            };

            // Act
            service.SaveSettings(_tempFilePath, settings);
            var loadedSettings = service.LoadSettings(_tempFilePath);

            // Assert
            Assert.NotNull(loadedSettings);
            Assert.Equal("custom-whisper.bin", loadedSettings.ModelFilePath);
            Assert.Equal(1, loadedSettings.VadMode);
            Assert.True(loadedSettings.ProcessWithLLM);
            Assert.Equal(8192, loadedSettings.LLMContextSize);
            Assert.Equal(0.85f, loadedSettings.LLMTemperature);
            Assert.Equal(1.5f, loadedSettings.VadGainMultiplier);
            Assert.Equal(12.0, loadedSettings.NormalMaxChunkDurationSeconds);
            Assert.Equal(0.95, loadedSettings.DictationSilenceThresholdSeconds);
            Assert.Equal("E", loadedSettings.DictationHotkeyKey);
        }

        [Fact]
        public void LoadSettings_DoesNotDuplicateLLMAntiPrompts()
        {
            // Arrange
            var service = new SettingsService();
            var settings = new AppSettings();
            // Save initial defaults
            service.SaveSettings(_tempFilePath, settings);

            // Act - Load multiple times
            var loaded1 = service.LoadSettings(_tempFilePath);
            service.SaveSettings(_tempFilePath, loaded1);
            var loaded2 = service.LoadSettings(_tempFilePath);

            // Assert
            Assert.Equal(settings.LLMAntiPrompts.Count, loaded2.LLMAntiPrompts.Count);
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                try { File.Delete(_tempFilePath); } catch { }
            }
        }
    }
}
