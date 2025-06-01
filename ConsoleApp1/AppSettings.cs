// AppSettings.cs
namespace WhisperNetConsoleDemo
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
        public bool ProcessWithLLM { get; set; } = false;
        public string LocalLLMModelPath { get; set; } = "qwen2-0_5b-instruct-q8_0.gguf"; // Example path
        public int LLMContextSize { get; set; } = 4096; // Or a sensible default for 0.5B model like 2048
        public int LLMSeed { get; set; } = 0; // 0 for random, any other int for fixed seed
        public float LLMTemperature { get; set; } = 0.6f;
        public int LLMMaxOutputTokens { get; set; } = 1024; // Max tokens LLM should generate
        public string LLMSystemPrompt { get; set; } = "You are an expert copy editor. Your task is to take the provided transcribed text and refine it into clear, grammatically correct, and professional-sounding prose. Correct any dictation errors, fix punctuation, and improve sentence structure where necessary. Output only the refined text.";
        public bool UseGpu { get; set; } = true; // Default to trying GPU. llama.cpp usually falls back to CPU if GPU init fails.
        }
    }
