# SmartDictate

## Project Overview
SmartDictate is a C# .NET 9 Windows Forms application that provides local, privacy-first voice dictation and text proofreading capabilities. It utilizes local machine learning models for both speech-to-text and text refinement, ensuring no data is sent to external servers.

### Key Technologies
*   **Framework:** .NET 9 (Windows Forms)
*   **Speech-to-Text:** [Whisper.net](https://github.com/sandrohanea/whisper.net) using local GGML models.
*   **Local LLM Inference:** [LLamaSharp](https://github.com/SciSharp/LLamaSharp) using local GGUF models (e.g., Qwen, Llama, Gemma).
*   **Audio Capture:** NAudio.
*   **Voice Activity Detection (VAD):** WebRtcVadSharp.
*   **Configuration:** `Microsoft.Extensions.Configuration` for `appsettings.json` management.

### Key Features
*   **Global Dictation Mode:** Hotkey-triggered dictation that types transcribed text directly into any application.
*   **Clipboard Proofreading:** Hotkey-triggered proofreading of clipboard text using a local LLM.
*   **Local LLM Refinement:** Automatically corrects grammar, spelling, and punctuation.
*   **Auto-Prompt Formatting:** Dynamically formats instructions based on the loaded LLM model (Qwen, Llama, Gemma).
*   **Real-time Monitoring:** Tracks System RAM and GPU VRAM usage.

## Building and Running

The project requires Visual Studio 2022 to build and run effectively, particularly due to the reliance on Windows Forms and specific platform targeting.

1.  **Open Solution:** Open `SmartDictate.sln` in Visual Studio 2022.
2.  **Platform Target:** Ensure the platform target is set to `x64`.
3.  **Restore Packages:** Allow Visual Studio to restore NuGet packages.
4.  **Run:** Press `F5` or click **Start** to build and launch the application.

*Note: You must manually download the necessary Whisper (GGML) and LLM (GGUF) models and place them in the application directory or specify their paths in the UI/settings.*

## Development Conventions

*   **Architecture:** The application separates UI concerns (`MainForm.cs`, `ModelSelectionForm.cs`) from business logic (`TranscriptionService.cs`, `GlobalHotkeyService.cs`, `KeyboardSimulator.cs`).
*   **Communication:** Event-driven architecture is used to pass data from services back to the UI (e.g., `TranscriptionService.SegmentTranscribed`, `TranscriptionService.FullTranscriptionReady`).
*   **Configuration:** User settings and advanced tweaks are managed through `appsettings.json`, mapped to the `AppSettings.cs` class.
*   **Naming Conventions:** 
    *   Standard C# conventions apply: PascalCase for classes, methods, and public properties.
    *   camelCase for local variables and parameters.
    *   Private backing fields typically use an underscore prefix (e.g., `_isProofreadingClipboard`, `_vramTimer`).
*   **UI Threading:** When updating UI components from background threads (like transcription services), use `Control.Invoke` or `Control.BeginInvoke` to ensure thread safety.
