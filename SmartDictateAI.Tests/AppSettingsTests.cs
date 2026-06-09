using Xunit;
using SmartDictateAI;

namespace SmartDictateAI.Tests
{
    public class AppSettingsTests
    {
        [Fact]
        public void EnsureDefaultPromptProfiles_WhenListIsEmpty_AddsDefaults()
        {
            // Arrange
            var settings = new AppSettings();
            settings.PromptProfiles.Clear(); // Ensure it is explicitly empty

            // Act
            settings.EnsureDefaultPromptProfiles();

            // Assert
            // Ensure exactly 12 defaults are added
            Assert.Equal(12, settings.PromptProfiles.Count);
            Assert.Contains(settings.PromptProfiles, p => p.Name == "Strict Proofreader");
            Assert.Contains(settings.PromptProfiles, p => p.Name == "German Copy Editor");
        }

        [Fact]
        public void EnsureDefaultPromptProfiles_DoesNotDuplicateExisting()
        {
            // Arrange
            var settings = new AppSettings();
            settings.PromptProfiles.Clear();
            settings.PromptProfiles.Add(new PromptProfile { Name = "Strict Proofreader", SystemPrompt = "CustomSP", UserPrompt = "CustomUP" });

            // Act
            settings.EnsureDefaultPromptProfiles();

            // Assert
            // It should have exactly 12 default profiles, but the one we customized beforehand should remain customized
            Assert.Equal(12, settings.PromptProfiles.Count);
            var strict = settings.PromptProfiles.Find(p => p.Name == "Strict Proofreader");
            Assert.NotNull(strict);
            Assert.Equal("CustomSP", strict.SystemPrompt);
            Assert.Equal("CustomUP", strict.UserPrompt);
        }

        [Fact]
        public void CopyFrom_CopiesAllFieldsCorrectly()
        {
            // Arrange
            var source = new AppSettings
            {
                SelectedMicrophoneDevice = 2,
                ModelFilePath = "whisper-test.bin",
                VadMode = 1,
                ShowRealtimeTranscription = false,
                ShowDebugMessages = true,
                ProcessWithLLM = true,
                LocalLLMModelPath = "test-llm.gguf",
                LLMContextSize = 4096,
                LLMSeed = 42,
                LLMTemperature = 0.75f,
                LLMMaxOutputTokens = 100,
                LLMSystemPrompt = "System Prompt Text",
                LLMUserPrompt = "User Prompt Text",
                UseGpu = false,
                NormalMaxChunkDurationSeconds = 8.5,
                NormalSilenceThresholdSeconds = 2.0,
                DictationMaxChunkDurationSeconds = 4.0,
                DictationSilenceThresholdSeconds = 0.5,
                VadGainMultiplier = 1.8f,
                MaintainContextAcrossChunks = false,
                DictationHotkeyModifiers = "Shift, Alt",
                DictationHotkeyKey = "X",
                ProofreadHotkeyModifiers = "Shift, Control",
                ProofreadHotkeyKey = "Y",
                ActivePromptProfileName = "Custom Profile"
            };
            source.PromptProfiles.Add(new PromptProfile { Name = "Custom Profile", SystemPrompt = "SP", UserPrompt = "UP" });

            var target = new AppSettings();

            // Act
            target.CopyFrom(source);

            // Assert
            Assert.Equal(source.SelectedMicrophoneDevice, target.SelectedMicrophoneDevice);
            Assert.Equal(source.ModelFilePath, target.ModelFilePath);
            Assert.Equal(source.VadMode, target.VadMode);
            Assert.Equal(source.ShowRealtimeTranscription, target.ShowRealtimeTranscription);
            Assert.Equal(source.ShowDebugMessages, target.ShowDebugMessages);
            Assert.Equal(source.ProcessWithLLM, target.ProcessWithLLM);
            Assert.Equal(source.LocalLLMModelPath, target.LocalLLMModelPath);
            Assert.Equal(source.LLMContextSize, target.LLMContextSize);
            Assert.Equal(source.LLMSeed, target.LLMSeed);
            Assert.Equal(source.LLMTemperature, target.LLMTemperature);
            Assert.Equal(source.LLMMaxOutputTokens, target.LLMMaxOutputTokens);
            Assert.Equal(source.LLMSystemPrompt, target.LLMSystemPrompt);
            Assert.Equal(source.LLMUserPrompt, target.LLMUserPrompt);
            Assert.Equal(source.UseGpu, target.UseGpu);
            Assert.Equal(source.NormalMaxChunkDurationSeconds, target.NormalMaxChunkDurationSeconds);
            Assert.Equal(source.NormalSilenceThresholdSeconds, target.NormalSilenceThresholdSeconds);
            Assert.Equal(source.DictationMaxChunkDurationSeconds, target.DictationMaxChunkDurationSeconds);
            Assert.Equal(source.DictationSilenceThresholdSeconds, target.DictationSilenceThresholdSeconds);
            Assert.Equal(source.VadGainMultiplier, target.VadGainMultiplier);
            Assert.Equal(source.MaintainContextAcrossChunks, target.MaintainContextAcrossChunks);
            Assert.Equal(source.DictationHotkeyModifiers, target.DictationHotkeyModifiers);
            Assert.Equal(source.DictationHotkeyKey, target.DictationHotkeyKey);
            Assert.Equal(source.ProofreadHotkeyModifiers, target.ProofreadHotkeyModifiers);
            Assert.Equal(source.ProofreadHotkeyKey, target.ProofreadHotkeyKey);
            Assert.Equal(source.ActivePromptProfileName, target.ActivePromptProfileName);
            Assert.Single(target.PromptProfiles);
            Assert.Equal("Custom Profile", target.PromptProfiles[0].Name);
            Assert.Equal("SP", target.PromptProfiles[0].SystemPrompt);
            Assert.Equal("UP", target.PromptProfiles[0].UserPrompt);
        }
    }
}
