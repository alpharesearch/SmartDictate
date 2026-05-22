using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SmartDictateAI.Services
{
    public class SettingsService : ISettingsService
    {
        public AppSettings LoadSettings(string filePath, Action<string>? onDebugMessage = null)
        {
            onDebugMessage?.Invoke("[Settings] Loading application settings...");

            var settings = new AppSettings();

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(filePath, optional: true, reloadOnChange: false);

            IConfigurationRoot configurationRoot = builder.Build();
            var settingsSection = configurationRoot.GetSection("AppSettings");

            if (settingsSection.Exists())
            {
                // IMPORTANT: reset list so binder doesn't append.
                settings.PromptProfiles = new List<PromptProfile>();

                settingsSection.Bind(settings);

                // If file has no profiles, inject defaults
                settings.EnsureDefaultPromptProfiles();

                onDebugMessage?.Invoke($"[VAD] Loaded Whisper Model: {settings.ModelFilePath}, LLM Model: {settings.LocalLLMModelPath}, VAD Mode: {settings.VadMode}, Mic: {settings.SelectedMicrophoneDevice}");
            }
            else
            {
                onDebugMessage?.Invoke($"[Settings] '{filePath}' not found. Using/Creating defaults.");
                settings.EnsureDefaultPromptProfiles();
                SaveSettings(filePath, settings, onDebugMessage);
            }

            return settings;
        }

        public void SaveSettings(string filePath, AppSettings settings, Action<string>? onDebugMessage = null)
        {
            onDebugMessage?.Invoke("[Settings] Saving application settings...");
            try
            {
                var configurationToSave = new
                {
                    AppSettings = settings
                };
                string json = JsonSerializer.Serialize(configurationToSave, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
                onDebugMessage?.Invoke($"[Settings] Settings saved to {Path.GetFullPath(filePath)}");
            }
            catch (Exception ex)
            {
                onDebugMessage?.Invoke($"[Settings] Error saving app settings: {ex.Message}");
            }
        }
    }
}
