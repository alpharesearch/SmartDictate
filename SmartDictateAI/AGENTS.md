# SmartDictateAI DOX

## Purpose
Main codebase of SmartDictate, containing the Windows Forms UI logic, hotkey hooking, user configurations, and orchestrating the transcription pipeline.

## Ownership
SmartDictate core development.

## Local Contracts
- Separation of Concerns: Separate UI views (`MainForm.cs`, `SettingsForm.cs`) from business logic.
- UI Threading: Always use `Control.Invoke` or `Control.BeginInvoke` when updating controls from background threads.
- WinForms Designer Compliance: All control instantiations, sizes, bounds, positions, font specifications, and parent additions must reside in the `InitializeComponent()` method inside `.Designer.cs` files. Do not initialize coordinates or layout rules programmatically in code-behind constructor files!

## Work Guidance
- Settings: App settings are mapped to the `AppSettings.cs` class. When updating values in the settings form, use cloned copy structures to support atomic Cancel/OK transactions.
- Logging: Ensure all key processing/post-processing steps (such as anti-prompt stripping, thinking blocks cleaning, and vocabulary replacements) log their details to the debug output for complete visibility.
- Naming:
  - PascalCase for classes, methods, and public properties.
  - camelCase for local variables and parameters.
  - Private backing fields prefixed with an underscore (e.g., `_vramTimer`).

## Verification
- Run the build using Visual Studio 2022 (targeted for `x64`).
- Open and verify the Windows Forms designer in Visual Studio to ensure compliance.

## Child DOX Index
- [UI/MainForm](file:///d:/GitHub/SmartDictate/SmartDictateAI/UI/MainForm/AGENTS.md) - MainForm view and tray/status tracking UI
- [UI/SettingsForm](file:///d:/GitHub/SmartDictate/SmartDictateAI/UI/SettingsForm/AGENTS.md) - Unified configuration dialog view
- [Services](file:///d:/GitHub/SmartDictate/SmartDictateAI/Services/AGENTS.md) - Core service interfaces and implementation details
- [Keyboard](file:///d:/GitHub/SmartDictate/SmartDictateAI/Keyboard/AGENTS.md) - Input typing simulator
- [Configuration](file:///d:/GitHub/SmartDictate/SmartDictateAI/Configuration/AGENTS.md) - App settings structure and schema mappings
