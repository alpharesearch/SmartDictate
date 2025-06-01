
2. **Open in Visual Studio 2022:**
- Open the `ConsoleApp1.sln` solution file.

3. **Restore NuGet packages:**
- Visual Studio will prompt you to restore packages on first open.

4. **Build and run:**
- Press `F5` or click __Start__ to build and launch the application.

### Usage

1. **Select your microphone** using the "Mic input" button.
2. **(Optional) Calibrate** using the "Calibration" button for best results.
3. **Choose models** with the "Model" button.
4. **Start recording** with the "Start" button.
5. **View transcriptions** in the main window.
6. **Copy results** using "Copy Raw" or "Copy LLM" buttons.
7. **Toggle debug/LLM** with the checkboxes in the top right.

## Project Structure

- `MainForm.cs` - Main application logic and UI event handling.
- `MainForm.Designer.cs` - UI layout and control definitions.
- `MainForm.resx` - Resource file for form localization and assets.

## Notes

- Model files are not included. You must provide your own Whisper and LLM models. 
  I used ggml-base.bin and qwen2-0_5b-instruct-q8_0.gguf for testing.
- Debug output can be enabled for troubleshooting.
- All processing is done locally; no audio is sent to external servers.

## License

[MIT](LICENSE)

---

*Built with ❤️ using .NET 9 and Windows Forms.*