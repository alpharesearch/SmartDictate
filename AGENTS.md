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

---

# DOX framework

- DOX is highly performant AGENTS.md hierarchy installed here
- Agent must follow DOX instructions across any edits

## Core Contract

- AGENTS.md files are binding work contracts for their subtrees
- Work products, source materials, instructions, records, assets, and durable docs must stay understandable from the nearest applicable AGENTS.md plus every parent AGENTS.md above it

## Read Before Editing

1. Read the root AGENTS.md
2. Identify every file or folder you expect to touch
3. Walk from the repository root to each target path
4. Read every AGENTS.md found along each route
5. If a parent AGENTS.md lists a child AGENTS.md whose scope contains the path, read that child and continue from there
6. Use the nearest AGENTS.md as the local contract and parent docs for repo-wide rules
7. If docs conflict, the closer doc controls local work details, but no child doc may weaken DOX

Do not rely on memory. Re-read the applicable DOX chain in the current session before editing.

## Update After Editing

Every meaningful change requires a DOX pass before the task is done.

Update the closest owning AGENTS.md when a change affects:

- purpose, scope, ownership, or responsibilities
- durable structure, contracts, workflows, or operating rules
- required inputs, outputs, permissions, constraints, side effects, or artifacts
- user preferences about behavior, communication, process, organization, or quality
- AGENTS.md creation, deletion, move, rename, or index contents

Update parent docs when parent-level structure, ownership, workflow, or child index changes. Update child docs when parent changes alter local rules. Remove stale or contradictory text immediately. Small edits that do not change behavior or contracts may leave docs unchanged, but the DOX pass still must happen.

## Hierarchy

- Root AGENTS.md is the DOX rail: project-wide instructions, global preferences, durable workflow rules, and the top-level Child DOX Index
- Child AGENTS.md files own domain-specific instructions and their own Child DOX Index
- Each parent explains what its direct children cover and what stays owned by the parent
- The closer a doc is to the work, the more specific and practical it must be

## Child Doc Shape

- Create a child AGENTS.md when a folder becomes a durable boundary with its own purpose, rules, responsibilities, workflow, materials, or quality standards
- Work Guidance must reflect the current standards of the project or user instructions; if there are no specific standards or instructions yet, leave it empty
- Verification must reflect an existing check; if no verification framework exists yet, leave it empty and update it when one exists

Default section order:
- Purpose
- Ownership
- Local Contracts
- Work Guidance
- Verification
- Child DOX Index

## Style

- Keep docs concise, current, and operational
- Document stable contracts, not diary entries
- Put broad rules in parent docs and concrete details in child docs
- Prefer direct bullets with explicit names
- Do not duplicate rules across many files unless each scope needs a local version
- Delete stale notes instead of explaining history
- Trim obvious statements, repeated rules, misplaced detail, and warnings for risks that no longer exist

## Closeout

1. Re-check changed paths against the DOX chain
2. Update nearest owning docs and any affected parents or children
3. Refresh every affected Child DOX Index
4. Remove stale or contradictory text
5. Run existing verification when relevant
6. Report any docs intentionally left unchanged and why

## User Preferences

When the user requests a durable behavior change, record it here or in the relevant child AGENTS.md

## Child DOX Index

- [SmartDictateAI](file:///d:/GitHub/SmartDictate/SmartDictateAI/AGENTS.md) - Main Windows Forms application logic and UI views
  - [UI/MainForm](file:///d:/GitHub/SmartDictate/SmartDictateAI/UI/MainForm/AGENTS.md) - MainForm view and tray/status tracking UI
  - [UI/SettingsForm](file:///d:/GitHub/SmartDictate/SmartDictateAI/UI/SettingsForm/AGENTS.md) - Unified configuration dialog view
  - [Services](file:///d:/GitHub/SmartDictate/SmartDictateAI/Services/AGENTS.md) - Low-level external/internal models and devices integration
    - [Transcription](file:///d:/GitHub/SmartDictate/SmartDictateAI/Services/Transcription/AGENTS.md) - Transcription pipeline orchestrator
  - [Keyboard](file:///d:/GitHub/SmartDictate/SmartDictateAI/Keyboard/AGENTS.md) - Input typing simulator
  - [Configuration](file:///d:/GitHub/SmartDictate/SmartDictateAI/Configuration/AGENTS.md) - App settings structure and schema mappings
- [SmartDictateAI.Tests](file:///d:/GitHub/SmartDictate/SmartDictateAI.Tests/AGENTS.md) - xUnit testing project for checking services and business logic
