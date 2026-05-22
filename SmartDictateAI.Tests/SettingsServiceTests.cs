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
                ProcessWithLLM = true
            };

            // Act
            service.SaveSettings(_tempFilePath, settings);
            var loadedSettings = service.LoadSettings(_tempFilePath);

            // Assert
            Assert.NotNull(loadedSettings);
            Assert.Equal("custom-whisper.bin", loadedSettings.ModelFilePath);
            Assert.Equal(1, loadedSettings.VadMode);
            Assert.True(loadedSettings.ProcessWithLLM);
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
