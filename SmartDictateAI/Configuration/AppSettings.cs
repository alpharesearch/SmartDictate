// AppSettings.cs
/// <summary>
/// Represents the application settings for the SmartDictateAI,
/// including audio input, model configuration, and language model processing options.
/// </summary>
using System.Collections.Generic;
using System.Linq;
namespace SmartDictateAI
{
    public class AppSettings
    {

        public const string APPSETTINGS_DEFAULT_MODEL_PATH = "ggml-base.bin";

        public int SelectedMicrophoneDevice { get; set; } = 0;
        public string ModelFilePath { get; set; } = APPSETTINGS_DEFAULT_MODEL_PATH;

        // Replaced amplitude threshold with VAD operating mode.
        // 0 = Low, 1 = Medium, 2 = High, 3 = Max (VeryAggressive)
        public int VadMode { get; set; } = 3;

        public bool ShowRealtimeTranscription { get; set; } = true;
        public bool ShowDebugMessages { get; set; } = false;
        public bool ProcessWithLLM { get; set; } = true;
        public string LocalLLMModelPath { get; set; } = "qwen2-0_5b-instruct-q8_0.gguf"; // Example path
        public int LLMContextSize { get; set; } = 16384; // Or a sensible default for 0.5B model like 2048
        public int LLMSeed { get; set; } = 0; // 0 for random, any other int for fixed seed
        public float LLMTemperature { get; set; } = 0.2f;
        public int LLMMaxOutputTokens { get; set; } = -1; // Max tokens LLM should generate
        public List<string> LLMAntiPrompts { get; set; } = GetDefaultLLMAntiPrompts();
        public string LLMPromptTemplate { get; set; } = ""; // Empty string enables Auto-Prompt Formatting
        public string LLMSystemPrompt { get; set; } = "You are an expert copy editor. Your task is to refine the raw transcription. Correct spelling and grammar errors, insert proper punctuation, remove conversational filler, and enhance clarity. Output ONLY the refined text.";
        public string LLMUserPrompt { get; set; } = "Refine the following text. Do not include any explanations, preamble, or notes. Text:\n";
        public bool UseGpu { get; set; } = true; // Default to trying GPU. llama.cpp usually falls back to CPU if GPU init fails.

        // Audio Chunking & VAD Settings
        public double NormalMaxChunkDurationSeconds { get; set; } = 6.0;
        public double NormalSilenceThresholdSeconds { get; set; } = 1.5;
        public double DictationMaxChunkDurationSeconds { get; set; } = 3.0;
        public double DictationSilenceThresholdSeconds { get; set; } = 0.75;
        public float VadGainMultiplier { get; set; } = 1.0f;
        public bool MaintainContextAcrossChunks { get; set; } = true;
        public string CustomVocabulary { get; set; } = "SIMATIC, WinCC, WinCC flexible, WinCC Unified, TIA Portal, Comfort Panel, Basic Panel, Mobile Panel, Key Panel, Sm@rtServer, PROFINET, PROFIBUS, SCALANCE, RUGGEDCOM, OPC UA, Industrial Ethernet, SINEMA, STEP 7, S7-1200, S7-1500, S7-300, S7-400, ET 200SP, ET 200MP, SITOP, SIPLUS, LOGO!";

        // Hotkey Settings
        public string DictationHotkeyModifiers { get; set; } = "Control, Alt";
        public string DictationHotkeyKey { get; set; } = "D";
        public string ProofreadHotkeyModifiers { get; set; } = "Control, Alt";
        public string ProofreadHotkeyKey { get; set; } = "P";

        // Always start with an empty list so Binder can fill it cleanly.
        public List<PromptProfile> PromptProfiles { get; set; } = new();

        public string ActivePromptProfileName { get; set; } = "Copy Editor";

        public void CopyFrom(AppSettings source)
        {
            if (source == null) return;

            SelectedMicrophoneDevice = source.SelectedMicrophoneDevice;
            ModelFilePath = source.ModelFilePath;
            VadMode = source.VadMode;
            ShowRealtimeTranscription = source.ShowRealtimeTranscription;
            ShowDebugMessages = source.ShowDebugMessages;
            ProcessWithLLM = source.ProcessWithLLM;
            LocalLLMModelPath = source.LocalLLMModelPath;
            LLMContextSize = source.LLMContextSize;
            LLMSeed = source.LLMSeed;
            LLMTemperature = source.LLMTemperature;
            LLMMaxOutputTokens = source.LLMMaxOutputTokens;
            LLMAntiPrompts = source.LLMAntiPrompts != null ? new List<string>(source.LLMAntiPrompts) : new List<string>();
            LLMPromptTemplate = source.LLMPromptTemplate;
            LLMSystemPrompt = source.LLMSystemPrompt;
            LLMUserPrompt = source.LLMUserPrompt;
            UseGpu = source.UseGpu;
            NormalMaxChunkDurationSeconds = source.NormalMaxChunkDurationSeconds;
            NormalSilenceThresholdSeconds = source.NormalSilenceThresholdSeconds;
            DictationMaxChunkDurationSeconds = source.DictationMaxChunkDurationSeconds;
            DictationSilenceThresholdSeconds = source.DictationSilenceThresholdSeconds;
            VadGainMultiplier = source.VadGainMultiplier;
            MaintainContextAcrossChunks = source.MaintainContextAcrossChunks;
            CustomVocabulary = source.CustomVocabulary;
            DictationHotkeyModifiers = source.DictationHotkeyModifiers;
            DictationHotkeyKey = source.DictationHotkeyKey;
            ProofreadHotkeyModifiers = source.ProofreadHotkeyModifiers;
            ProofreadHotkeyKey = source.ProofreadHotkeyKey;
            ActivePromptProfileName = source.ActivePromptProfileName;
            PromptProfiles = source.PromptProfiles != null
                ? source.PromptProfiles.Select(p => new PromptProfile
                {
                    Name = p.Name,
                    SystemPrompt = p.SystemPrompt,
                    UserPrompt = p.UserPrompt
                }).ToList()
                : new List<PromptProfile>();
        }

        public void EnsureDefaultPromptProfiles()
        {
            if (PromptProfiles == null)
                PromptProfiles = new List<PromptProfile>();

            var defaultProfiles = GetDefaultPromptProfiles();
            foreach (var def in defaultProfiles)
            {
                if (!PromptProfiles.Any(p => p.Name.Equals(def.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    PromptProfiles.Add(def);
                }
            }
        }

        public void EnsureDefaultLLMAntiPrompts()
        {
            if (LLMAntiPrompts == null || LLMAntiPrompts.Count == 0)
            {
                LLMAntiPrompts = GetDefaultLLMAntiPrompts();
            }
        }

        public static List<string> GetDefaultLLMAntiPrompts() => new()
        {
            "<|im_end|>", "<|eot_id|>", "<|end_of_text|>", "<|fim_end|>", "<|im_start|>", "\nuser:", "\nUser:", "<|user|>", "<end_of_turn>", "<eos>"
        };

        public static List<PromptProfile> GetDefaultPromptProfiles() => new()
        {
            new PromptProfile
            {
                Name = "Strict Proofreader",
                SystemPrompt = "You are a strict proofreader. Your task is to fix spelling, grammar, and punctuation in American English. Do not rewrite, summarize, or change the author's original voice. Output ONLY the final corrected text without any conversational filler, explanations, or introductory phrases.",
                UserPrompt = "Correct the following text. Follow these strict rules:\n\n- Use American English\n- Fix grammar, spelling, and punctuation only\n- Keep the original words wherever possible\n- Preserve URLs exactly\n- Use straight quotes: \"like this\"\n- Do NOT use em dashes\n\nText:\n"
            },
            new PromptProfile
            {
                Name = "Copy Editor",
                SystemPrompt = "You are an expert copy editor. Your task is to refine the raw transcription. Correct spelling and grammar errors, insert proper punctuation, remove conversational filler, and enhance clarity. Output ONLY the refined text.",
                UserPrompt = "Refine the following text. Do not include any explanations, preamble, or notes. Text:\n"
            },
            new PromptProfile
            {
                Name = "German Strict Proofreader",
                SystemPrompt = "Du bist ein strenger Korrekturleser. Deine Aufgabe ist es, Rechtschreibung, Grammatik und Zeichensetzung im Deutschen zu korrigieren. Schreibe den Text nicht um, fasse ihn nicht zusammen und verändere nicht die ursprüngliche Stimme des Autors. Gib AUSSCHLIESSLICH den finalen, korrigierten Text aus, ohne konversationelle Füllwörter, Erklärungen oder einleitende Phrasen.",
                UserPrompt = "Korrigiere den folgenden Text. Befolge diese strengen Regeln:\n\n- Verwende die deutsche Sprache\n- Korrigiere nur Grammatik, Rechtschreibung und Zeichensetzung\n- Behalte die ursprünglichen Wörter bei, wo immer es möglich ist\n- Behalte URLs exakt bei\n\nText:\n"
            },
            new PromptProfile
            {
                Name = "German Copy Editor",
                SystemPrompt = "Du bist ein erfahrener Lektor. Deine Aufgabe ist es, die Rohübertragung zu verbessern. Korrigiere Rechtschreib- und Grammatikfehler, füge passende Zeichensetzung hinzu, entferne Füllwörter und verbessere die Klarheit. Gib NUR den korrigierten Text aus.",
                UserPrompt = "Überarbeite den folgenden Text. Gib keine Erklärungen oder Einleitungen. Text:\n"
            },
            new PromptProfile
            {
                Name = "Professional Email Drafter",
                SystemPrompt = "You are an expert executive assistant. Your task is to transform raw, rambling dictation into a polished, professional email. Maintain the core message and intent, but ensure the tone is polite, concise, and appropriate for business communication. Output ONLY the final email text without any conversational filler or meta-commentary.",
                UserPrompt = "Turn the following dictation into a professional email. Do not include any introductory text or explanations. Text:\n"
            },
            new PromptProfile
            {
                Name = "Meeting Notes & Action Items",
                SystemPrompt = "You are a highly efficient project manager. Your task is to extract the key points and action items from the provided dictation. Format the output as a clean, bulleted list of notes followed by a checklist of action items. Output ONLY the formatted notes.",
                UserPrompt = "Summarize the following dictation into 'Key Notes' and 'Action Items'. Keep it concise and easy to read. Text:\n"
            },
            new PromptProfile
            {
                Name = "Tone: Diplomatic & Polite",
                SystemPrompt = "You are a communications expert. Your task is to rewrite the provided text so that it sounds highly polite, diplomatic, and constructive, while keeping the original meaning intact. Output ONLY the rewritten text.",
                UserPrompt = "Rewrite the following text to be more diplomatic and polite. Do not include any explanations. Text:\n"
            },
            new PromptProfile
            {
                Name = "Tone: Casual & Friendly",
                SystemPrompt = "You are a friendly writing assistant. Your task is to rewrite the provided text to have a warm, casual, and friendly tone, while preserving the original meaning. Avoid formal or stiff phrasing. Output ONLY the rewritten text.",
                UserPrompt = "Rewrite the following text to sound casual and friendly. Do not include any explanations. Text:\n"
            },
            new PromptProfile
            {
                Name = "Tone: Concise & Direct",
                SystemPrompt = "You are a professional editor. Your task is to make the provided text highly concise and direct, removing redundant words or filler phrasing while retaining all key details. Output ONLY the rewritten text.",
                UserPrompt = "Rewrite the following text to be concise and direct. Do not include any explanations. Text:\n"
            },
            new PromptProfile
            {
                Name = "Tone: Academic & Scholarly",
                SystemPrompt = "You are an academic writing consultant. Your task is to rewrite the provided text in a formal, precise, and scholarly tone suitable for research papers or academic publishing. Output ONLY the rewritten text.",
                UserPrompt = "Rewrite the following text to be formal and academic. Do not include any explanations. Text:\n"
            },
            new PromptProfile
            {
                Name = "Translate: To German",
                SystemPrompt = "Du bist ein professioneller Übersetzer. Deine Aufgabe ist es, den bereitgestellten Text in natürliches, grammatikalisch korrektes Deutsch zu übersetzen. Gib AUSSCHLIESSLICH den übersetzten Text aus, ohne Erklärungen.",
                UserPrompt = "Übersetze den folgenden Text ins Deutsche. Text:\n"
            },
            new PromptProfile
            {
                Name = "Translate: To English",
                SystemPrompt = "You are a professional translator. Your task is to translate the provided text into natural, grammatically correct English. Output ONLY the translated text without any explanations.",
                UserPrompt = "Translate the following text to English. Text:\n"
            }
        };
    }
}
