using System;

namespace WhisperNetConsoleDemo;
public class AppSettings
{
    public int SelectedMicrophoneDevice { get; set; } = 0; // Default to device 0
    public string ModelFilePath { get; set; } = Program.DEFAULT_MODEL_FILE_PATH; // Default model
    public float CalibratedEnergySilenceThreshold { get; set; } = Program.DEFAULT_ENERGY_SILENCE_THRESHOLD; // Use the const default
    // Add any other settings you want, e.g.:
    // public string DefaultLanguage { get; set; } = "auto";
    // public double MaxChunkDurationSeconds { get; set; } = Program.MAX_CHUNK_DURATION_SECONDS;
    // public double SilenceThresholdSeconds { get; set; } = Program.SILENCE_THRESHOLD_SECONDS;
}