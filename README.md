# SmartDictate

## Getting Started

1. **Open in Visual Studio 2022:**  
   Open the `ConsoleApp1.sln` solution file.

2. **Restore NuGet packages:**  
   Visual Studio will prompt you to restore packages on first open.

3. **Build and run:**  
   Press `F5` or click **Start** to build and launch the application.

## Usage

1. **Select your microphone** using the "Mic input" button.
2. **(Optional) Calibrate** using the "Calibration" button for best results.
3. **Choose models** with the "Model" button.
4. **Start recording** with the "Start" button.
5. **View transcriptions** in the main window.
6. **Copy results** using "Copy Raw" or "Copy LLM" buttons.
7. **Toggle debug/LLM** with the checkboxes in the top right.

## Required Models

You must download and provide your own Whisper and LLM models:

- **Whisper GGML models:**  
  Download from [ggerganov/whisper.cpp releases](https://huggingface.co/ggerganov/whisper.cpp/tree/main)  
  Example: [ggml-base.bin](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin?download=true)

- **LLM GGUF models:**  
  Download from [bartowski/Qwen2-0.5B-Instruct-GGUF](https://huggingface.co/bartowski/Qwen2-0.5B-Instruct-GGUF)  
  Example: [Qwen2-0.5B-Instruct-GGUF](https://huggingface.co/bartowski/Qwen2-0.5B-Instruct-GGUF/resolve/main/Qwen2-0.5B-Instruct-Q8_0.gguf?download=true)  
  File: `qwen2-0_5b-instruct-q8_0.gguf`

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
