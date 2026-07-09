# SmartDictate Performance Tests DOX

## Purpose
A dedicated benchmarking and quality assurance suite for evaluating speech-to-text (Whisper) and text-refinement (LLM) models. It verifies model performance (load times, memory consumption, execution speed) and output correctness under deterministic conditions.

## Ownership
SmartDictate Quality Assurance / Core Performance.

## Local Contracts
- **Framework**: xUnit is the testing framework.
- **Platform Target**: Must target `net9.0-windows` and platform `x64` to load native backends for Whisper.net and LLamaSharp.
- **Skipping Protocol**: Performance tests require real GGUF and GGML files to be present on disk, which are large and resource-intensive. Tests use standard xUnit attributes but implement a runtime bypass: they exit early as "Passed" unless the environment variable `RUN_LLM_PERF=true` is set OR an empty file named `.run_perf` is created in the solution root or `models/` folder. This ensures the tests are always listed in the IDE.
- **Model Storage**: Tests look for models inside `models/llm/` and `models/whisper/` folders, with a fallback to the root `models/` directory.
- **Metrics Outputs**: Results from each run are compiled dynamically into `llm_performance_report.md` at the solution root folder.

## Work Guidance
- Models should be configured with a fixed seed (e.g. `LLMSeed = 42`) and low temperature (`LLMTemperature = 0.2f`) to ensure deterministic output comparisons.
- The Whisper test requires `whisper_benchmark.wav` and `whisper_benchmark.txt` to be present in the `SmartDictateAI.PerformanceTests/Assets/` folder.


## Verification
- To execute the performance tests via CLI, set the environment variable and run:
  ```powershell
  $env:RUN_LLM_PERF="true"
  dotnet test --filter Category=Performance
  ```
- Alternatively, when running tests from an IDE (like VS Code, Visual Studio, or Antigravity IDE) where setting environment variables is less convenient, create an empty `.run_perf` file in the solution root or `models/` directory, and run the tests from your IDE's Test Explorer.
- Verify that `llm_performance_report.md` is updated at the repository root.


## Child DOX Index
This directory has no nested subdirectories with a DOX contract.
