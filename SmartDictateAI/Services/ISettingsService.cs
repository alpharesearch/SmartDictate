using System;

namespace SmartDictateAI.Services
{
    public interface ISettingsService
    {
        AppSettings LoadSettings(string filePath, Action<string>? onDebugMessage = null);
        void SaveSettings(string filePath, AppSettings settings, Action<string>? onDebugMessage = null);
    }
}
