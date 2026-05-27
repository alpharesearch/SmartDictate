// DictationVisualState.cs
namespace SmartDictateAI
{
    public enum DictationVisualState
    {
        Idle,            // Not recording
        ListeningSilent, // Microphone is open, but no active speech detected yet
        SpeechDetected,  // VAD has triggered; user is actively talking
        Processing,       // Whisper or the LLM is baking the final text
        Loading       // Loading Whisper and LLM models
    }
}
