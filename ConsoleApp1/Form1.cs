// Form1.cs
using System;
using System.IO; // For Path
using System.Windows.Forms;
// using WhisperNetConsoleDemo; // If AppSettings and TranscriptionService are in this namespace

namespace WhisperNetConsoleDemo
    {
    public partial class Form1 : Form
        {
        private TranscriptionService transcriptionService;
        private List<(int Index, string Name)> availableMicrophones = new List<(int, string)>();

        public Form1()
            {
            InitializeComponent();
            transcriptionService = new TranscriptionService();

            // Subscribe to events from TranscriptionService
            transcriptionService.DebugMessageGenerated += OnDebugMessageReceived;
            transcriptionService.SegmentTranscribed += OnSegmentReceived;
            transcriptionService.FullTranscriptionReady += OnFullTranscriptionCompleted;
            transcriptionService.RecordingStateChanged += OnServiceRecordingStateChanged;
            transcriptionService.SettingsUpdated += OnServiceSettingsUpdated;

            // Set initial UI state from settings
            textBox2.Visible = transcriptionService.Settings.ShowDebugMessages; // txtDebugOutput
                                                                                // Consider adding a toggle for debug messages in the UI if desired
            }

        private void Form1_Load(object sender, EventArgs e)
            {
            // Settings are loaded by TranscriptionService constructor
            // Update UI based on loaded settings
            PopulateMicrophoneList(); // New method
            UpdateUIFromServiceSettings();
            UpdateButtonStates();
            }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
            {
            if (isFormRecordingState) // Use a local isRecording flag if needed, or check service's state
                {
                // Decide if you want to prevent closing or auto-stop
                // For now, let's auto-stop. The service's Dispose should handle it.
                AppendToDebugOutput("Form closing during recording, stopping service.");
                transcriptionService.StopRecording(); // This is synchronous but triggers async events
                }
            // Await any final processing if transcriptionService.StopRecording initiated it.
            // This requires more complex async coordination in FormClosing.
            // For now, we rely on the service's own internal await for the last chunk.
            // The main risk is the app closing before the async void RecordingStopped event fully completes.
            // A more robust shutdown might involve a dedicated async method in TranscriptionService.

            // transcriptionService.SaveAppSettings(); // Service should save on its own when settings change
            transcriptionService.Dispose(); // Or await transcriptionService.DisposeAsync(); if FormClosing can be async
            }

        // --- Event Handlers from TranscriptionService ---
        private void OnDebugMessageReceived(string message)
            {
            AppendToDebugOutput(message);
            }

        private void OnSegmentReceived(string timestampedText, string rawText)
            {
            if (transcriptionService.Settings.ShowRealtimeTranscription)
                {
                AppendToTranscriptionOutput(timestampedText, true);
                }
            }

        private void OnFullTranscriptionCompleted(string fullText)
            {
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
            }

        private bool isFormRecordingState = false; // Separate UI recording state
        private void OnServiceRecordingStateChanged(bool nowRecording)
            {
            isFormRecordingState = nowRecording; // Update UI state
            UpdateButtonStates();
            }
        private void OnServiceSettingsUpdated()
            {
            UpdateUIFromServiceSettings();
            }


        // --- UI Update Helpers (Thread-Safe) ---
        private void AppendToTranscriptionOutput(string text, bool addNewLine = true, bool replaceSelection = false)
            {
            if (textBox1.InvokeRequired)
                {
                textBox1.Invoke(new Action<string, bool, bool>(AppendToTranscriptionOutput), text, addNewLine, replaceSelection);
                }
            else
                {
                if (replaceSelection && textBox1.SelectionLength > 0)
                    {
                    textBox1.SelectedText = text + (addNewLine ? Environment.NewLine : "");
                    }
                else
                    {
                    textBox1.AppendText(text + (addNewLine ? Environment.NewLine : ""));
                    }
                textBox1.ScrollToCaret();
                }
            }

        private void AppendToDebugOutput(string message)
            {
            if (!transcriptionService.Settings.ShowDebugMessages)
                return; // Check service setting
            if (textBox2.InvokeRequired)
                {
                textBox2.Invoke(new Action<string>(AppendToDebugOutput), message);
                }
            else
                {
                textBox2.AppendText($"{(message.StartsWith("DEBUG:") ? "" : "DEBUG: ")}{message}{Environment.NewLine}");
                textBox2.ScrollToCaret();
                }
            }

        private void UpdateButtonStates()
            {
            if (this.InvokeRequired)
                {
                this.Invoke(new Action(UpdateButtonStates));
                return;
                }
            button1.Text = isFormRecordingState ? "Stop Recording" : "Start Recording";
            button2.Enabled = !isFormRecordingState; // Calibrate
            button3.Enabled = !isFormRecordingState; // Model
            button4.Enabled = !isFormRecordingState; // Mic
            }

        private void UpdateUIFromServiceSettings()
            {
            if (this.InvokeRequired)
                {
                this.Invoke(new Action(UpdateUIFromServiceSettings));
                return;
                }
            // Update labels or status strips if you have them
            // Example: lblModelName.Text = Path.GetFileName(transcriptionService.Settings.ModelFilePath);
            // lblThreshold.Text = $"{transcriptionService.Settings.CalibratedEnergySilenceThreshold:F4}";
            textBox2.Visible = transcriptionService.Settings.ShowDebugMessages;
            }

        private void PopulateMicrophoneList() // For a ComboBox or ListBox later
            {
            availableMicrophones = transcriptionService.GetAvailableMicrophones();
            if (availableMicrophones.Count == 0)
                {
                MessageBox.Show("No microphones found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                button1.Enabled = false; // Disable record button
                button2.Enabled = false; // Disable calibrate
                button4.Enabled = false; // Disable mic select
                }
            else
                {
                button1.Enabled = true;
                button2.Enabled = true;
                button4.Enabled = true;
                // If you add a ComboBox:
                // comboBoxMicrophones.DataSource = availableMicrophones;
                // comboBoxMicrophones.DisplayMember = "Name";
                // comboBoxMicrophones.ValueMember = "Index";
                // comboBoxMicrophones.SelectedValue = transcriptionService.Settings.SelectedMicrophoneDevice;
                AppendToDebugOutput($"Populated mics. Current in settings: {transcriptionService.Settings.SelectedMicrophoneDevice}");
                }
            }

        // --- Button Click Handlers ---
        private async void button1_Click(object sender, EventArgs e) // Record/Stop
            {
            if (!isFormRecordingState)
                {
                if (availableMicrophones.Count == 0)
                    {
                    MessageBox.Show("No microphone to record from.", "Error");
                    return;
                    }

                textBox1.Clear(); // Clear previous full transcription
                AppendToDebugOutput("Start recording button clicked.");
                bool success = await transcriptionService.StartRecordingAsync(transcriptionService.Settings.SelectedMicrophoneDevice);
                if (!success)
                    {
                    MessageBox.Show("Failed to start recording. Check debug log.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                // RecordingStateChanged event will update isFormRecordingState and button text
                }
            else
                {
                AppendToDebugOutput("Stop recording button clicked.");
                transcriptionService.StopRecording();
                // RecordingStateChanged event will update isFormRecordingState and button text
                // Full transcription will be raised by FullTranscriptionReady event
                }
            }

        // Form1.cs

        // (Make sure you have a Label named lblCalibrationStatus on your form)

        private async void button2_Click(object sender, EventArgs e) // btnCalibrate_Click
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

            button1.Enabled = false; // Disable Record/Stop
            button2.Enabled = false; // Disable Calibrate
            button3.Enabled = false; // Disable Model
            button4.Enabled = false; // Disable Mic
            lblCalibrationStatus.Text = "Calibration starting..."; // Update status label
            lblCalibrationStatus.Visible = true;

            // Define the callback that updates the UI label
            Action<string> uiUpdateAction = (message) =>
            {
                if (lblCalibrationStatus.InvokeRequired)
                    {
                    lblCalibrationStatus.Invoke(() => {
                        lblCalibrationStatus.Text = message;
                        // Application.DoEvents(); // Use sparingly, can cause issues, but might help UI update during tight loops
                    });
                    }
                else
                    {
                    lblCalibrationStatus.Text = message;
                    // Application.DoEvents(); 
                    }
                AppendToDebugOutput($"CALIBRATION_UI: {message}"); // Also send to debug log
            };

            try
                {
                await transcriptionService.CalibrateThresholdsAsync(transcriptionService.Settings.SelectedMicrophoneDevice, uiUpdateAction);
                // Final message after completion
                string finalThresholdMessage = $"Calibration complete. New threshold: {transcriptionService.Settings.CalibratedEnergySilenceThreshold:F4}";
                lblCalibrationStatus.Text = finalThresholdMessage;
                MessageBox.Show(finalThresholdMessage, "Calibration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            catch (Exception ex)
                {
                string errorMsg = $"Calibration failed: {ex.Message}";
                lblCalibrationStatus.Text = errorMsg;
                MessageBox.Show(errorMsg, "Calibration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            finally
                {
                // lblCalibrationStatus.Visible = false; // Optionally hide after a delay or keep visible
                button1.Enabled = true; // Re-enable Record/Stop
                button2.Enabled = true; // Re-enable Calibrate
                button3.Enabled = true; // Re-enable Model
                button4.Enabled = true; // Re-enable Mic
                UpdateUIFromServiceSettings(); // Refresh main UI elements with potentially new threshold
                }
            }
        private async void button3_Click(object sender, EventArgs e) // Change Model
            {
            if (isFormRecordingState)
                {
                MessageBox.Show("Stop recording before changing model.", "Info");
                return;
                }
            using (OpenFileDialog ofd = new OpenFileDialog())
                {
                ofd.Filter = "Whisper Model Files (*.bin)|*.bin|All files (*.*)|*.*";
                ofd.Title = "Select Whisper Model File";
                try
                    {
                    ofd.InitialDirectory = !string.IsNullOrWhiteSpace(transcriptionService.Settings.ModelFilePath) && Directory.Exists(Path.GetDirectoryName(transcriptionService.Settings.ModelFilePath)) ?
                                           Path.GetDirectoryName(transcriptionService.Settings.ModelFilePath) :
                                           Path.GetDirectoryName(Application.ExecutablePath);
                    if (!string.IsNullOrWhiteSpace(transcriptionService.Settings.ModelFilePath))
                        ofd.FileName = Path.GetFileName(transcriptionService.Settings.ModelFilePath);
                    }
                catch { ofd.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath); }


                if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                    await transcriptionService.ChangeModelPathAsync(ofd.FileName);
                    // SettingsUpdated event will trigger UpdateUIFromServiceSettings
                    }
                }
            }

        // Form1.cs

        private void button4_Click(object sender, EventArgs e) // btnSelectMic_Click
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
        }
    }
