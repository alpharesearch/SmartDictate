// AppSettings.cs
namespace WhisperNetConsoleDemo // Assuming this is your root namespace
{
    public class AppSettings
    {
        public const float APPSETTINGS_DEFAULT_ENERGY_THRESHOLD = 0.025f;
        public const string APPSETTINGS_DEFAULT_MODEL_PATH = "ggml-base.bin";

        public int SelectedMicrophoneDevice { get; set; } = 0;
        public string ModelFilePath { get; set; } = APPSETTINGS_DEFAULT_MODEL_PATH;
        public float CalibratedEnergySilenceThreshold { get; set; } = APPSETTINGS_DEFAULT_ENERGY_THRESHOLD;
        public bool ShowRealtimeTranscription { get; set; } = true;
        public bool ShowDebugMessages { get; set; } = false;
    }
}