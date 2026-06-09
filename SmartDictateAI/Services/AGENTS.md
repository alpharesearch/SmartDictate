# SmartDictate Services DOX

## Purpose
Houses decoupled services and interfaces implementing local audio capture, VAD, Whisper GGML speech-to-text, LLM post-processing, and configuration storage.

## Ownership
SmartDictate services layer.

## Local Contracts
- Decoupled Interface Architecture: All business logic components must expose an interface (e.g., `ISettingsService`, `IVadService`, `IWhisperService`, `ILLMService`, `IAudioCaptureService`).
- UI Decoupling: Code in this folder must have no dependency on UI assemblies/types. Communication with the UI is handled through event-driven callbacks (e.g., `SegmentTranscribed`).
- Mockability: Services must remain fully mockable without requiring native libraries (e.g., Whisper, WebRtcVad, LLamaSharp) to run, facilitating unit tests.

## Work Guidance
- Whisper: Manages GGML model contexts and speech-to-text inference.
- LLM: Handles GGUF model contexts and text post-processing refinements.
- VAD: Handles voice activity detection and software amplification of audio frames.
- Audio Capture: Encapsulates NAudio capture state.

## Verification
- Validate mock implementations in tests.

## Child DOX Index
- [Transcription](file:///d:/GitHub/SmartDictate/SmartDictateAI/Services/Transcription/AGENTS.md) - Transcription pipeline orchestrator
