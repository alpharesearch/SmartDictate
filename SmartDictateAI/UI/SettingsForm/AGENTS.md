# SmartDictate SettingsForm DOX

## Purpose
Unified settings form managing tabbed controls for Whisper paths, LLM/GGUF models, silence/VAD thresholds, prompts, and global hotkeys.

## Ownership
SmartDictate UI / Presentation layer.

## Local Contracts
- WinForms Designer Compliance: All control instantiations, sizes, bounds, positions, font specifications, and parent additions must reside in the `InitializeComponent()` method inside `SettingsForm.Designer.cs`. Do not initialize coordinates or layout rules programmatically in the code-behind file (`SettingsForm.cs`).
- Atomic Transactions: Uses cloned copies of configuration settings so that pressing "Cancel" rolls back modifications atomically and "OK" persists them.

## Work Guidance
- Manage SettingsForm design changes strictly inside the Visual Studio Designer to avoid corrupting layout coordinates.

## Verification
- Open `SettingsForm.cs` in the Visual Studio Form Designer and verify that it loads without error.

## Child DOX Index
This directory has no nested subdirectories with a DOX contract.
