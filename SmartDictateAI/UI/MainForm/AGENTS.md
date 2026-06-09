# SmartDictate MainForm DOX

## Purpose
Main application window of SmartDictate. Responsible for the UI layout, system tray initialization, global hotkey registration callbacks, performance monitoring (CPU, RAM, GPU/VRAM), and visual status pipeline transitions (Idle, Listening, Processing, etc.).

## Ownership
SmartDictate UI / Presentation layer.

## Local Contracts
- WinForms Designer Compliance: All control instantiations, sizes, bounds, positions, font specifications, and parent additions must reside in the `InitializeComponent()` method inside `MainForm.Designer.cs`. Do not initialize coordinates or layout rules programmatically in the code-behind file (`MainForm.cs`).
- Thread Safety: MainForm UI updates from background threads (e.g. `TranscriptionService`) must be safely wrapped in `Invoke` or `BeginInvoke`.
- Coordination: Maintains the reference to `TranscriptionService` and maps its states to pipeline color-coded indicators.

## Work Guidance
- UI logic should remain separated from audio recording or model inference.
- Delegate transcription pipelines to `TranscriptionService` and settings loading to `ISettingsService`.

## Verification
- Run the application in Visual Studio 2022 and verify the UI rendering.
- Open `MainForm.cs` in the Visual Studio Form Designer and verify that it loads without error.

## Child DOX Index
This directory has no nested subdirectories with a DOX contract.
