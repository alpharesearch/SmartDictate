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
*   **Consolidated Tabbed Settings:** Adjust Whisper and LLM paths, VAD settings, silence thresholds, system/user prompt profiles, and custom hotkeys in a single unified settings form.
*   **Multi-State Visual Pipeline Indicator:** Color-coded, real-time pipeline status feedback (Idle, Listening, Speech Detected, Processing).
*   **Interactive Clipboard Rerun (`Rerun LLM` button):** Context-aware action that triggers LLM refinement on the active text buffer on-demand.
*   **Real-time Monitoring:** Tracks System RAM and GPU VRAM usage.

## Building and Running

The project requires Visual Studio 2022 to build and run effectively, particularly due to the reliance on Windows Forms and specific platform targeting.

1.  **Open Solution:** Open `SmartDictate.sln` in Visual Studio 2022.
2.  **Platform Target:** Ensure the platform target is set to `x64`.
3.  **Restore Packages:** Allow Visual Studio to restore NuGet packages.
4.  **Run:** Press `F5` or click **Start** to build and launch the application.

*Note: You must manually download the necessary Whisper (GGML) and LLM (GGUF) models and place them in the application directory or specify their paths in the UI/settings.*

## Development Conventions

*   **DOX Documentation Framework:** The project adheres strictly to the DOX framework detailed in the root [AGENTS.md](file:///d:/GitHub/SmartDictate/AGENTS.md). All agents must read root and child `AGENTS.md` files before making edits and perform a DOX pass to update them upon completion.
*   **Decoupled Architecture:** The application separates UI concerns (`MainForm.cs`, `SettingsForm.cs`) from business logic through single-responsibility service interfaces under `SmartDictateAI/Services`:
    *   `ISettingsService` / `SettingsService` — Load, clone, and save advanced application parameters.
    *   `IVadService` / `VadService` — Wraps WebRtcVad, handles software amplification, and checks frames for speech.
    *   `IWhisperService` / `WhisperService` — Manages GGML model contexts and speech-to-text inference.
    *   `ILLMService` / `LLMService` — Manages GGUF model contexts and text post-processing refinements.
    *   `IAudioCaptureService` / `AudioCaptureService` — Encapsulates NAudio capture state and events.
    *   `TranscriptionService.cs` acts as a unified orchestrator using Dependency Injection for all five services, allowing isolated testing without mocking complex native libraries.
*   **WinForms Designer Compliance:** To keep the Visual Studio visual designer fully functional, all control instantiations, positions, bounds, sizes, font specifications, and parent additions must reside inside the `InitializeComponent()` method within `Designer.cs` files. Do not initialize coordinates or layout rules programmatically in code-behind constructor files!
*   **Communication:** Event-driven architecture is used to pass data from services back to the UI (e.g., `TranscriptionService.SegmentTranscribed`, `TranscriptionService.FullTranscriptionReady`).
*   **Configuration:** User settings and advanced tweaks are managed through `appsettings.json`, mapped to the `AppSettings.cs` class. When updating values in the settings form, use cloned copy structures to support atomic Cancel/OK dialog transactions.
*   **Naming Conventions:** 
    *   Standard C# conventions apply: PascalCase for classes, methods, and public properties.
    *   camelCase for local variables and parameters.
    *   Private backing fields typically use an underscore prefix (e.g., `_isProofreadingClipboard`, `_vramTimer`).
*   **UI Threading:** When updating UI components from background threads (like transcription services), use `Control.Invoke` or `Control.BeginInvoke` to ensure thread safety.

## Testing

The project uses **xUnit** for unit testing, located in the `SmartDictateAI.Tests` project.

### Running Tests
You can run the tests using any of the following methods:
*   **Visual Studio:** Open the Test Explorer (`Test` > `Test Explorer`) and click "Run All".
*   **VS Code / C# Dev Kit:** Use the Testing sidebar (beaker icon) to discover and run tests.
*   **.NET CLI:** Run the following command from the repository root:
    ```bash
    dotnet test
    ```

### Testing Conventions
*   **Framework:** xUnit is used as the primary testing framework.
*   **Mocking:** Use lightweight, self-contained mock implementations of service interfaces (found inside the test project) rather than introducing third-party Mocking engines.
*   **Target Framework:** The test project must target `net9.0-windows` to ensure compatibility with the main Windows Forms application.
*   **Naming Conventions:** Test classes should append `Tests` to the name of the class being tested (e.g., `AppSettingsTests`). Test methods generally follow a descriptive pattern, such as `MethodName_StateUnderTest_ExpectedBehavior`.
