# SmartDictate Transcription Service DOX

## Purpose
The primary orchestrator service (`TranscriptionService.cs`) coordinating Audio Capture, VAD (Voice Activity Detection), Whisper transcribing, and LLM refining.

## Ownership
SmartDictate Services / Orchestration layer.

## Local Contracts
- UI Decoupling: The transcription service must have no dependency on UI assemblies or forms types.
- Dependency Injection: Leverages Dependency Injection to receive service interfaces: `IAudioCaptureService`, `IVadService`, `IWhisperService`, `ILLMService`, and `ISettingsService`.
- Asynchronous Events: Communication with the UI is asynchronous and event-driven (e.g., `SegmentTranscribed`, `FullTranscriptionReady`).

## Work Guidance
- Maintains internal dictation state machines (Idle, Listening, Processing).
- Handles queuing of audio frames and calling transcription pipelines.

## Verification
- Run xUnit tests inside `SmartDictateAI.Tests` focusing on `TranscriptionServiceTests.cs`.

## Child DOX Index
This directory has no nested subdirectories with a DOX contract.
