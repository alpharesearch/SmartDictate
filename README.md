﻿# SmartDictate

## Getting Started

1. **Open in Visual Studio 2022:**  
   Open the `ConsoleApp1.sln` solution file.

2. **Restore NuGet packages:**  
   Visual Studio will prompt you to restore packages on first open.

3. **Build and run:**  
   Press `F5` or click **Start** to build and launch the application.

## Key Features

- **Global Dictation Mode (`CTRL + ALT + D`):** Dictate directly into any application. The app types out your transcribed text at your cursor.
- **Clipboard Proofreading (`CTRL + ALT + P`):** Send your currently copied text through the local LLM to correct grammar, spelling, and punctuation, then auto-paste it back.
- **Local LLM Refinement:** Automatically proofreads and refines your dictations using local GGUF models.
- **Auto-Prompt Formatting:** Automatically detects and applies the correct instruction templates for Qwen, Llama, and Gemma models based on the file name. You can manually override this behavior by setting `LLMPromptTemplate` in the `appsettings.json` file.
- **Real-time Resource Monitoring:** Live tracking of System RAM and GPU VRAM (Application footprint vs. Total system usage).
- **Advanced VAD (Voice Activity Detection):** Adjustable sensitivity settings to ignore background noise and handle automatic audio chunking.

## Basic Usage

1. Select your microphone ("Mic input") and local models ("Model").
2. Set your VAD sensitivity (Low, Medium, High, Max) from the dropdown.
3. Click "Start" to dictate locally, or use the **Global Hotkeys** mentioned above.
4. Copy raw or LLM-refined text, or click "Rerun LLM" to re-process the last dictation.
5. Toggle the "Debug" checkbox to view underlying logs and memory stats.

## Required Models

You must download and provide your own Whisper and LLM models:

- **Whisper GGML models:**  
  Download from [ggerganov/whisper.cpp releases](https://huggingface.co/ggerganov/whisper.cpp/tree/main)  
  Example: [ggml-base.bin](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin?download=true)

- **LLM GGUF models:**  
  The app automatically supports and formats prompts for **Qwen**, **Llama**, and **Gemma** models. Examples:
  - **Qwen 2 (0.5B):** Download [qwen2-0_5b-instruct-q8_0.gguf](https://huggingface.co/bartowski/Qwen2-0.5B-Instruct-GGUF/resolve/main/Qwen2-0.5B-Instruct-Q8_0.gguf?download=true) from [bartowski/Qwen2-0.5B-Instruct-GGUF](https://huggingface.co/bartowski/Qwen2-0.5B-Instruct-GGUF)
  - **Llama 3.2 (3B):** Download [Llama-3.2-3B-Instruct-Q8_0.gguf](https://huggingface.co/hugging-quants/Llama-3.2-3B-Instruct-Q8_0-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q8_0.gguf?download=true) from [hugging-quants/Llama-3.2-3B-Instruct-Q8_0-GGUF](https://huggingface.co/hugging-quants/Llama-3.2-3B-Instruct-Q8_0-GGUF)
  - **Gemma 4 (E2B):** Download [gemma-4-E2B-it-Q4_0.gguf](https://huggingface.co/unsloth/gemma-4-E2B-it-GGUF/resolve/main/gemma-4-E2B-it-Q4_0.gguf?download=true) from [unsloth/gemma-4-E2B-it-GGUF](https://huggingface.co/unsloth/gemma-4-E2B-it-GGUF)

## Example appsettings.json

Configure your model paths in `appsettings.json`:

```json
{
  "WhisperModelPath": "models/ggml-base.bin",
  "LlmModelPath": "models/qwen2-0_5b-instruct-q8_0.gguf"
}
```

## Project Structure

- `MainForm.cs` - Main application logic and UI event handling.
- `MainForm.Designer.cs` - UI layout and control definitions.
- `MainForm.resx` - Resource file for form localization and assets.

## Notes

- Model files are not included. Download them as described above.
- Debug output can be enabled for troubleshooting.
- All processing is done locally; no audio is sent to external servers.

## Screenshot

<img width="640" alt="image" src="https://github.com/user-attachments/assets/79e38214-8940-4801-8090-8ff878fa11ca" />

## License

[MIT](LICENSE)

---

*Built with ❤️ using .NET 9 and Windows Forms.*

![icon](https://github.com/user-attachments/assets/4c0f3e6c-e1cd-4110-9468-e9102d7f2681)
