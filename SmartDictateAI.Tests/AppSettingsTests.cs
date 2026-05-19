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
            // Ensure exactly 9 defaults are added (adjust if there are more)
            Assert.Equal(9, settings.PromptProfiles.Count);
            Assert.Contains(settings.PromptProfiles, p => p.Name == "Strict Proofreader");
            Assert.Contains(settings.PromptProfiles, p => p.Name == "German Copy Editor");
        }

        [Fact]
        public void EnsureDefaultPromptProfiles_WhenListHasItems_DoesNotDuplicate()
        {
            // Arrange
            var settings = new AppSettings();
            settings.PromptProfiles.Clear();
            settings.PromptProfiles.Add(new PromptProfile { Name = "Custom Profile" });

            // Act
            settings.EnsureDefaultPromptProfiles();

            // Assert
            Assert.Single(settings.PromptProfiles); // It should still only have 1 item
            Assert.Equal("Custom Profile", settings.PromptProfiles[0].Name);
        }
    }
}
