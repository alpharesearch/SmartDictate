# SmartDictate Keyboard Simulator DOX

## Purpose
Input simulation helper (`KeyboardSimulator.cs`) which simulates user typing to send transcribed or LLM-proofread text into the active window.

## Ownership
SmartDictate System / Input simulation layer.

## Local Contracts
- Win32 P/Invoke: Uses native `SendInput` inputs to safely handle typing characters, text buffers, and clipboard manipulations.

## Work Guidance
- Provides clean character-by-character output or clipboard pasting depending on size/text constraints.

## Verification
- Run the main application and verify that transcribed text is correctly sent to active text editors (e.g. Notepad).

## Child DOX Index
This directory has no nested subdirectories with a DOX contract.
