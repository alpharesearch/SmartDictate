// Form1.cs
using System;
using System.IO; // For Path
using System.Text.RegularExpressions;
using System.Windows.Forms;
// using WhisperNetConsoleDemo; // If AppSettings and TranscriptionService are in this namespace

namespace WhisperNetConsoleDemo
{
    public partial class MainForm : Form
    {
        private TranscriptionService transcriptionService;
        private GlobalHotkeyService globalHotkeyService;
        private bool isInDictationModeCurrently = false; // UI flag for dictation mode
        private List<(int Index, string Name)> availableMicrophones = new List<(int, string)>();

        private enum AppStatus
        {
            Idle, Calibrating, Listening, Processing, Error
        }
        private Color idleColor = SystemColors.ControlDark; // Or a light gray
        private Color listeningColor = Color.LightGreen;
        private Color processingColor = Color.LightSkyBlue;
        private Color errorColor = Color.LightCoral;
        private Color calibratingColor = Color.LightYellow;

        public MainForm()
        {
            InitializeComponent();
            transcriptionService = new TranscriptionService();
            // Subscribe to events from TranscriptionService
            transcriptionService.SegmentTranscribed += OnServiceSegmentTranscribedForDictation;
            transcriptionService.DebugMessageGenerated += OnDebugMessageReceived;
            transcriptionService.FullTranscriptionReady += OnFullTranscriptionCompleted;
            transcriptionService.RecordingStateChanged += OnServiceRecordingStateChanged;
            transcriptionService.SettingsUpdated += OnServiceSettingsUpdated;
            transcriptionService.ProcessingStarted += OnServiceProcessingStarted; // Subscribe
            transcriptionService.ProcessingFinished += OnServiceProcessingFinished; // Subscribe
            // Set initial UI state from settings
            textBoxDebug.Visible = transcriptionService.Settings.ShowDebugMessages; // txtDebugOutput
            chkDebug.Checked = transcriptionService.Settings.ShowDebugMessages;
            chkLLM.Checked = transcriptionService.Settings.ProcessWithLLM;
            InitializeHotkeyService();
            btnCopyRawText.Enabled = false;
            btnCopyLLMText.Enabled = false;
            }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                globalHotkeyService.ProcessHotKeyMessage(m.WParam.ToInt32());
            }
        }
        private void InitializeHotkeyService() 
        {
            globalHotkeyService = new GlobalHotkeyService(this.Handle);
            globalHotkeyService.HotKeyPressed += OnGlobalHotKeyPressed;
            // Example: Register Ctrl+Alt+D
            if (!globalHotkeyService.Register(GlobalHotkeyService.FsModifiers.Control | GlobalHotkeyService.FsModifiers.Alt, Keys.D))
            {
                AppendToDebugOutput("Failed to register global hotkey Ctrl+Alt+D!");
            }
            else
            {
                AppendToDebugOutput("Global hotkey Ctrl+Alt+D registered for dictation mode.");
            }
        }
        private async void OnGlobalHotKeyPressed()
        {
            AppendToDebugOutput("Global Hotkey Pressed!");
            if (!isInDictationModeCurrently) // If not in dictation mode, start it
            {
                if (isFormRecordingState) // If normal recording is active
                {
                    AppendToDebugOutput("Normal recording active. Stop it before starting dictation mode.");
                    MessageBox.Show("Please stop the current recording session before starting dictation mode.", "Info");
                    return;
                }
                AppendToDebugOutput("Attempting to start dictation mode...");
                UpdateStatusIndicator(AppStatus.Listening, "Dictation Starting...");
                isInDictationModeCurrently = true; // Optimistic
                bool success = await transcriptionService.StartDictationModeAsync(transcriptionService.Settings.SelectedMicrophoneDevice);
                if (success)
                {
                    AppendToDebugOutput("Dictation mode started.");
                    UpdateStatusIndicator(AppStatus.Listening, "Dictating...");
                    // Optionally minimize or hide your main form
                    // this.WindowState = FormWindowState.Minimized;
                }
                else
                {
                    AppendToDebugOutput("Failed to start dictation mode.");
                    UpdateStatusIndicator(AppStatus.Error, "Dictation start failed");
                    isInDictationModeCurrently = false; // Revert
                }
            }
            else // If already in dictation mode, stop it
            {
                AppendToDebugOutput("Attempting to stop dictation mode...");
                UpdateStatusIndicator(AppStatus.Processing, "Dictation Stopping...");
                await transcriptionService.StopDictationModeAsync();
                isInDictationModeCurrently = false;
                UpdateStatusIndicator(AppStatus.Idle, "Dictation Ended");
                AppendToDebugOutput("Dictation mode stopped.");
                // Optionally restore your main form if it was minimized
                // if (this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;
                // this.Activate();
            }
        }

        private static readonly Regex PlaceholderRegex = new Regex(
        @"(\[[A-Za-z _\-]+\]|\([A-Za-z _\-]+\)|\.\.\.)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private async void OnServiceSegmentTranscribedForDictation(string timestampedText, string rawText)
        {
            string rawTextFilter = PlaceholderRegex.Replace(rawText.Trim(), string.Empty).Trim();
            if (transcriptionService.Settings.ShowRealtimeTranscription)
            {
                if(!string.IsNullOrWhiteSpace(rawTextFilter)) AppendToTranscriptionOutput(rawTextFilter, false);
                AppendToDebugOutput("INFO: " + timestampedText + "\n");
            }

            if (isInDictationModeCurrently && !string.IsNullOrWhiteSpace(rawText))
            {
                AppendToDebugOutput($"Dictation output: {rawText}");
                // Add a small delay to allow focus to switch if user just pressed hotkey
                // This is a bit of a hack; more robust focus management might be needed.
                // Task.Delay(100).ContinueWith(_ =>
                // {
                // Ensure this runs on a thread that can send input, or use Invoke if necessary,
                // but SendInput usually works from various threads.
                await Task.Delay(50);
                Action<string> loggerAction = (logMsg) => AppendToDebugOutput($"SIMULATOR: {logMsg}");
                KeyboardSimulator.SendText(rawText + " ", true, loggerAction); // Add a space after each segment
            }
        }
        private void UpdateStatusIndicator(AppStatus status, string message = "")
        {
            if (lblStatusIndicator.InvokeRequired)
            {
                lblStatusIndicator.Invoke(() => UpdateStatusIndicator(status, message));
                return;
            }

            string displayText = message;
            Color displayColor = idleColor;

            switch (status)
            {
                case AppStatus.Idle:
                    displayText = string.IsNullOrWhiteSpace(message) ? "Ready" : message;
                    displayColor = idleColor;
                    break;
                case AppStatus.Listening:
                    displayText = string.IsNullOrWhiteSpace(message) ? "Listening..." : message;
                    displayColor = listeningColor;
                    break;
                case AppStatus.Processing:
                    displayText = string.IsNullOrWhiteSpace(message) ? "Processing..." : message;
                    displayColor = processingColor;
                    break;
                case AppStatus.Error:
                    displayText = string.IsNullOrWhiteSpace(message) ? "Error" : message;
                    displayColor = errorColor;
                    break;
                case AppStatus.Calibrating:
                    displayText = string.IsNullOrWhiteSpace(message) ? "Calibrating..." : message;
                    displayColor = calibratingColor;
                    break;
            }
            lblStatusIndicator.Text = displayText;
            lblStatusIndicator.BackColor = displayColor;
            lblStatusIndicator.ForeColor = displayColor.GetBrightness() < 0.5 ? Color.White : Color.Black; // Contrast for text
        }
        private bool activelyProcessingChunkInUI = false;
        private void OnServiceProcessingStarted()
        {
            activelyProcessingChunkInUI = true;
            UpdateStatusIndicator(AppStatus.Processing);
        }

        private void OnServiceProcessingFinished()
        {
            activelyProcessingChunkInUI = false;
            // If not recording anymore, go to Idle. If still recording, go back to Listening.
            UpdateStatusIndicator(isFormRecordingState ? AppStatus.Listening : AppStatus.Idle);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateUIFromServiceSettings();
            PopulateMicrophoneList();
            UpdateButtonStates();
            UpdateStatusIndicator(AppStatus.Idle);
            textBoxDebug.Visible = transcriptionService.Settings.ShowDebugMessages;
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (transcriptionService != null)
            {
                AppendToDebugOutput("Form1_FormClosing: Unsubscribing from TranscriptionService events.");
                transcriptionService.DebugMessageGenerated -= OnDebugMessageReceived;
                transcriptionService.SegmentTranscribed -= OnServiceSegmentTranscribedForDictation;
                transcriptionService.FullTranscriptionReady -= OnFullTranscriptionCompleted;
                transcriptionService.RecordingStateChanged -= OnServiceRecordingStateChanged;
                transcriptionService.SettingsUpdated -= OnServiceSettingsUpdated;
                transcriptionService.ProcessingStarted -= OnServiceProcessingStarted;
                transcriptionService.ProcessingFinished -= OnServiceProcessingFinished;
            }
            if (isFormRecordingState && transcriptionService != null)
            {
                AppendToDebugOutput("Form closing during recording, stopping service.");
                await transcriptionService.StopRecording();
            }
            if (globalHotkeyService != null)
            {
                globalHotkeyService.HotKeyPressed -= OnGlobalHotKeyPressed; // Unsubscribe
                globalHotkeyService.Dispose(); // This calls UnregisterHotKey
            }
     
            transcriptionService?.Dispose(); // Or await transcriptionService.DisposeAsync(); if FormClosing can be async
            Thread.Sleep(100); // Give time for any final debug messages to flush
        }

        // --- Event Handlers from TranscriptionService ---
        private void OnDebugMessageReceived(string message)
        {
            AppendToDebugOutput(message);
        }
        private void OnFullTranscriptionCompleted(string fullText)
        {
            AppendToTranscriptionOutput("", true);
            AppendToTranscriptionOutput("\n--- Full Transcription (Session Ended) ---", true);
            if (!string.IsNullOrWhiteSpace(fullText))
            {
                AppendToTranscriptionOutput(fullText, true, true); // Last param true to replace selection if any
            }
            else
            {
                AppendToTranscriptionOutput("[No speech detected in this session after filtering.]", true);
            }
            AppendToTranscriptionOutput("----------------------------------------", true);
            UpdateButtonStates();
        }

        private bool isFormRecordingState = false; // Separate UI recording state
        private void OnServiceRecordingStateChanged(bool nowRecording)
        {
            isFormRecordingState = nowRecording; // Update UI state
            if (nowRecording)
            {
                UpdateStatusIndicator(AppStatus.Listening);
                btnCopyRawText.Enabled = false; // Disable during recording
                btnCopyLLMText.Enabled = false; // Disable during recording
            }
            else
            {
                if (!activelyProcessingChunkInUI)
                {
                    UpdateStatusIndicator(AppStatus.Idle);
                }
            }
            UpdateButtonStates();
        }
        private void OnServiceSettingsUpdated()
        {
            UpdateUIFromServiceSettings();
        }


        // --- UI Update Helpers (Thread-Safe) ---
        private void AppendToTranscriptionOutput(string text, bool addNewLine = true, bool replaceSelection = false)
        {
            if (textBoxOutput.InvokeRequired)
            {
                textBoxOutput.Invoke(new Action<string, bool, bool>(AppendToTranscriptionOutput), text, addNewLine, replaceSelection);
            }
            else
            {
                if (replaceSelection && textBoxOutput.SelectionLength > 0)
                {
                    textBoxOutput.SelectedText = text + (addNewLine ? Environment.NewLine : "");
                }
                else
                {
                    textBoxOutput.AppendText(text + (addNewLine ? Environment.NewLine : ""));
                }
                textBoxOutput.ScrollToCaret();
            }
        }

        private void AppendToDebugOutput(string message)
        {
            if (textBoxDebug.InvokeRequired)
            {
                textBoxDebug.Invoke(new Action<string>(AppendToDebugOutput), message);
            }
            else
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff ");
                //textBoxDebug.AppendText($"{timestamp}{(message.StartsWith("DEBUG:") ? "" : "DEBUG: ")}{message}{Environment.NewLine}");
                textBoxDebug.AppendText($"{timestamp}: {message}{Environment.NewLine}");
                textBoxDebug.ScrollToCaret();
            }
        }

        private void UpdateButtonStates()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateButtonStates));
                return;
            }
            btnStartStop.Text = isFormRecordingState ? "Stop Recording" : "Start Recording";
            btnCalibration.Enabled = !isFormRecordingState; // Calibrate
            btnModelSettings.Enabled = !isFormRecordingState; // Model
            btnMicInput.Enabled = !isFormRecordingState; // Mic

            // Copy button states
            if (!isFormRecordingState) // Only enable copy buttons when not recording
            {
                btnCopyRawText.Enabled = !string.IsNullOrEmpty(transcriptionService.LastRawFilteredText);
                btnCopyLLMText.Enabled = transcriptionService.WasLastProcessingWithLLM &&
                                         !string.IsNullOrEmpty(transcriptionService.LastLLMProcessedText);
            }
            else
            {
                btnCopyRawText.Enabled = false;
                btnCopyLLMText.Enabled = false;
            }

        }

        private void UpdateUIFromServiceSettings()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateUIFromServiceSettings));
                return;
            }
            textBoxDebug.Visible = transcriptionService.Settings.ShowDebugMessages;
            if (transcriptionService.Settings.ShowDebugMessages)
                this.Size = new Size(800, 840);
            else
                this.Size = new Size(800, 480);
        }

        private void PopulateMicrophoneList() // For a ComboBox or ListBox later
        {
            availableMicrophones = TranscriptionService.GetAvailableMicrophones();
            if (availableMicrophones.Count == 0)
            {
                MessageBox.Show("No microphones found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnStartStop.Enabled = false; // Disable record button
                btnCalibration.Enabled = false; // Disable calibrate
                btnMicInput.Enabled = false; // Disable mic select
            }
            else
            {
                btnStartStop.Enabled = true;
                btnCalibration.Enabled = true;
                btnMicInput.Enabled = true;
                AppendToDebugOutput($"Populated mics. Current in settings: {transcriptionService.Settings.SelectedMicrophoneDevice}");
            }
        }

        // --- Button Click Handlers ---
        private async void btnStart_Stop_Click(object sender, EventArgs e) // Record/Stop
        {
            if (!isFormRecordingState)
            {
                if (availableMicrophones.Count == 0)
                {
                    MessageBox.Show("No microphone to record from.", "Error");
                    return;
                }

                textBoxOutput.Clear(); // Clear previous full transcription
                AppendToDebugOutput("Start recording button clicked.");
                UpdateStatusIndicator(AppStatus.Processing, "Starting...");
                bool success = await transcriptionService.StartRecordingAsync(transcriptionService.Settings.SelectedMicrophoneDevice);
                if (!success)
                {
                    UpdateStatusIndicator(AppStatus.Error, "Failed to start");
                    MessageBox.Show("Failed to start recording. Check debug log.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                // RecordingStateChanged event will update isFormRecordingState and button text
            }
            else
            {
                AppendToDebugOutput("Stop recording button clicked.");
                UpdateStatusIndicator(AppStatus.Processing, "Stopping..."); // Indicate stopping is a form of processing
                Task stopTask = transcriptionService.StopRecording();
                await stopTask;
                UpdateButtonStates();
                // RecordingStateChanged event will update isFormRecordingState and button text
                // Full transcription will be raised by FullTranscriptionReady event
            }
        }

        // Form1.cs

        // (Make sure you have a Label named lblCalibrationStatus on your form)

        private async void btnCalibration_Click(object sender, EventArgs e) // btnCalibrate_Click
        {
            if (isFormRecordingState) // Use UI's recording state flag
            {
                MessageBox.Show("Please stop the main recording before starting calibration.", "Recording Active", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (availableMicrophones.Count == 0 || transcriptionService.Settings.SelectedMicrophoneDevice < 0)
            {
                MessageBox.Show("No valid microphone selected for calibration. Please select one using the 'Mic' button.", "Microphone Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnStartStop.Enabled = false; // Disable Record/Stop
            btnCalibration.Enabled = false; // Disable Calibrate
            btnModelSettings.Enabled = false; // Disable Model
            btnMicInput.Enabled = false; // Disable Mic
            lblCalibrationIndicator.Text = "Calibration starting..."; // Update status label
            lblCalibrationIndicator.Visible = true;

            // Define the callback that updates the UI label
            Action<string> uiUpdateAction = (message) =>
            {
                if (lblCalibrationIndicator.InvokeRequired)
                {
                    lblCalibrationIndicator.Invoke(() =>
                    {
                        lblCalibrationIndicator.Text = message;
                    });
                }
                else
                {
                    lblCalibrationIndicator.Text = message;
                }
                AppendToDebugOutput($"CALIBRATION_UI: {message}"); // Also send to debug log
            };

            try
            {
                await transcriptionService.CalibrateThresholdsAsync(transcriptionService.Settings.SelectedMicrophoneDevice, uiUpdateAction);
                // Final message after completion
                string finalThresholdMessage = $"Calibration complete. New threshold: {transcriptionService.Settings.CalibratedEnergySilenceThreshold:F4}";
                lblCalibrationIndicator.Text = finalThresholdMessage;
                MessageBox.Show(finalThresholdMessage, "Calibration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                string errorMsg = $"Calibration failed: {ex.Message}";
                lblCalibrationIndicator.Text = errorMsg;
                MessageBox.Show(errorMsg, "Calibration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lblCalibrationIndicator.Visible = false; // Optionally hide after a delay or keep visible
                btnStartStop.Enabled = true; // Re-enable Record/Stop
                btnCalibration.Enabled = true; // Re-enable Calibrate
                btnModelSettings.Enabled = true; // Re-enable Model
                btnMicInput.Enabled = true; // Re-enable Mic
                UpdateUIFromServiceSettings(); // Refresh main UI elements with potentially new threshold
            }
        }
        // Form1.cs

        private async void btnModelSettings_Click(object sender, EventArgs e) // btnChangeModel_Click (now for both)
        {
            if (isFormRecordingState)
            {
                MessageBox.Show("Stop recording before changing models.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (ModelSelectionForm modelForm = new ModelSelectionForm(
                transcriptionService.Settings.ModelFilePath,
                transcriptionService.Settings.LocalLLMModelPath))
            {
                if (modelForm.ShowDialog(this) == DialogResult.OK)
                {
                    bool whisperChanged = transcriptionService.Settings.ModelFilePath != modelForm.SelectedWhisperModelPath;
                    bool llmChanged = transcriptionService.Settings.LocalLLMModelPath != modelForm.SelectedLLMModelPath;

                    if (whisperChanged)
                    {
                        AppendToDebugOutput($"New Whisper model selected: {modelForm.SelectedWhisperModelPath}");
                        await transcriptionService.ChangeModelPathAsync(modelForm.SelectedWhisperModelPath);
                        // ChangeModelPathAsync in service should update Settings and save
                    }

                    if (llmChanged)
                    {
                        AppendToDebugOutput($"New LLM model selected: {modelForm.SelectedLLMModelPath}");
                        await transcriptionService.ChangeLLMModelPathAsync(modelForm.SelectedLLMModelPath);
                        // ChangeLLMModelPathAsync in service should update Settings and save
                    }

                    if (whisperChanged || llmChanged)
                    {
                        AppendToDebugOutput("Model selections updated.");
                        // SettingsUpdated event from transcriptionService should refresh UI via OnServiceSettingsUpdated
                    }
                    else
                    {
                        AppendToDebugOutput("Model selections unchanged.");
                    }
                }
                else
                {
                    AppendToDebugOutput("Model selection cancelled.");
                }
            }
        }

        private void btnMicInput_Click(object sender, EventArgs e) // btnSelectMic_Click
        {
            if (isFormRecordingState)
            {
                MessageBox.Show("Stop recording before selecting microphone.", "Info");
                return;
            }

            if (availableMicrophones.Count == 0)
            {
                MessageBox.Show("No microphones detected to select from.", "Info");
                return;
            }

            using (Form micSelectionDialog = new Form())
            {
                micSelectionDialog.Text = "Select Microphone";
                micSelectionDialog.ClientSize = new System.Drawing.Size(350, 250); // Set a reasonable client size
                micSelectionDialog.FormBorderStyle = FormBorderStyle.FixedDialog; // Optional: make it non-resizable
                micSelectionDialog.StartPosition = FormStartPosition.CenterParent; // Good for dialogs
                micSelectionDialog.MaximizeBox = false; // Optional
                micSelectionDialog.MinimizeBox = false; // Optional

                ListBox listBoxMics = new ListBox
                {
                    Dock = DockStyle.Fill, // Fill the available space first
                    IntegralHeight = false // Allows partial items if list is long
                };

                // Panel to hold the OK button at the bottom
                Panel buttonPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 40 // Give some space for the button
                };

                Button btnOk = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    // Anchor to the right side of the panel
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                    Width = 80, // Set a width for the button
                    Height = 30 // Set a height
                };
                // Position the OK button within the panel
                btnOk.Location = new System.Drawing.Point(buttonPanel.ClientSize.Width - btnOk.Width - 10, (buttonPanel.ClientSize.Height - btnOk.Height) / 2);


                buttonPanel.Controls.Add(btnOk);

                // Add ListBox first so it fills space not taken by buttonPanel
                micSelectionDialog.Controls.Add(listBoxMics);
                micSelectionDialog.Controls.Add(buttonPanel); // Then add panel which docks to bottom

                micSelectionDialog.AcceptButton = btnOk; // Pressing Enter clicks OK

                foreach (var mic in availableMicrophones)
                {
                    listBoxMics.Items.Add($"[{mic.Index}] {mic.Name}");
                }

                // Try to pre-select the currently configured microphone
                int currentMicIndexInList = -1;
                for (int i = 0; i < availableMicrophones.Count; i++)
                {
                    if (availableMicrophones[i].Index == transcriptionService.Settings.SelectedMicrophoneDevice)
                    {
                        currentMicIndexInList = i;
                        break;
                    }
                }
                if (currentMicIndexInList != -1)
                {
                    listBoxMics.SelectedIndex = currentMicIndexInList;
                }
                else if (availableMicrophones.Count > 0) // If current not found, select first
                {
                    listBoxMics.SelectedIndex = 0;
                }


                if (micSelectionDialog.ShowDialog(this) == DialogResult.OK && listBoxMics.SelectedIndex != -1)
                {
                    // Get the actual device index from our availableMicrophones list,
                    // as listBoxMics.SelectedIndex is the index in the listbox items.
                    int selectedDeviceIndexInApp = availableMicrophones[listBoxMics.SelectedIndex].Index;
                    transcriptionService.SelectMicrophone(selectedDeviceIndexInApp);
                    // SettingsUpdated event from transcriptionService should trigger UI refresh.
                }
            }
        }

        // Event handler for btnCopyRawText
        private void btnCopyRawText_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(transcriptionService.LastRawFilteredText))
            {
                try
                {
                    Clipboard.SetText(transcriptionService.LastRawFilteredText);
                    AppendToDebugOutput("Raw text copied to clipboard.");
                }
                catch (Exception ex)
                {
                    AppendToDebugOutput($"Error copying raw text: {ex.Message}");
                }
            }
            else
            {
                AppendToDebugOutput("No raw text available to copy.");
            }
        }

        // Event handler for btnCopyLLMText
        private void btnCopyLLMText_Click(object sender, EventArgs e)
        {
            if (transcriptionService.WasLastProcessingWithLLM && !string.IsNullOrEmpty(transcriptionService.LastLLMProcessedText))
            {
                try
                {
                    Clipboard.SetText(transcriptionService.LastLLMProcessedText);
                    AppendToDebugOutput("LLM refined text copied to clipboard.");
                }
                catch (Exception ex)
                {
                    AppendToDebugOutput($"Error copying LLM text: {ex.Message}");
                }
            }
            else
            {
                AppendToDebugOutput("No LLM refined text available to copy (LLM might be off or produced no output).");
            }
        }

        private void chkDebug_CheckedChanged(object sender, EventArgs e)
        {
            transcriptionService.Settings.ShowDebugMessages = chkDebug.Checked;
            transcriptionService.SaveAppSettings();
        }

        private void chkLLM_CheckedChanged(object sender, EventArgs e)
        {
            transcriptionService.Settings.ProcessWithLLM = chkLLM.Checked;
            transcriptionService.SaveAppSettings();
        }
    }
}
