﻿// Form1.cs
using CommunityToolkit.HighPerformance;
using System;
using System.IO; // For Path
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;
// using WhisperNetConsoleDemo; // If AppSettings and TranscriptionService are in this namespace

namespace WhisperNetConsoleDemo
{
    public partial class MainForm : Form
    {
        private TranscriptionService transcriptionService;
        private GlobalHotkeyService globalHotkeyService;

        // ADDED: second hotkey ID and a simple “busy” guard
        private const int HOTKEY_ID_DICTATION = 9000;   // Matches your existing service default
        private const int HOTKEY_ID_PROOFREAD = 9001;   // New: proofread clipboard
        private bool _isProofreadingClipboard = false;  // Prevent double-triggering

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

        private System.Windows.Forms.Timer? _vramTimer;
        private List<PerformanceCounter> _vramCounters = new List<PerformanceCounter>();

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

            // Wire up VAD sensitivity combobox (assume it exists in designer as cmbVadSensitivity)
            cmbVadSensitivity.SelectedIndexChanged += cmbVadSensitivity_SelectedIndexChanged;

            globalHotkeyService = new GlobalHotkeyService(this.Handle);
            globalHotkeyService.HotKeyPressed += OnGlobalHotKeyPressed;

            InitializeHotkeyService();
            btnCopyRawText.Enabled = false;
            btnCopyLLMText.Enabled = false;

            InitializeVramMonitor();
        }

        private void InitializeVramMonitor()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Adapter Memory");
                var instances = category.GetInstanceNames();
                foreach (var instance in instances)
                {
                    _vramCounters.Add(new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", instance));
                }
            }
            catch (Exception ex)
            {
                AppendToDebugOutput($"Failed to initialize VRAM performance counter: {ex.Message}. Ensure 'System.Diagnostics.PerformanceCounter' NuGet package is installed.");
            }

            _vramTimer = new System.Windows.Forms.Timer();
            _vramTimer.Interval = 1000; // Update every 1 second
            _vramTimer.Tick += (s, e) => UpdateVramUsage();
            _vramTimer.Start();
        }

        private void UpdateVramUsage()
        {
            if (_vramCounters.Count > 0)
            {
                try
                {
                    float totalVramBytes = 0;
                    foreach (var counter in _vramCounters)
                    {
                        totalVramBytes += counter.NextValue();
                    }
                    label_vram.Text = $"VRAM: {totalVramBytes / (1024 * 1024 * 1024):F2} GB";
                }
                catch
                {
                    label_vram.Text = "VRAM: Error";
                }
            }
            else
            {
                label_vram.Text = "VRAM: N/A";
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY)
            {
                globalHotkeyService.ProcessHotKeyMessage(m.WParam.ToInt32());
            }

            base.WndProc(ref m);
        }

        private void InitializeHotkeyService()
        {
            // Example: Register Ctrl+Alt+D
            if (!globalHotkeyService.Register(GlobalHotkeyService.FsModifiers.Control | GlobalHotkeyService.FsModifiers.Alt, Keys.D))
            {
                AppendToDebugOutput("Failed to register global hotkey Ctrl+Alt+D!");
            }
            else
            {
                AppendToDebugOutput("Global hotkey Ctrl+Alt+D registered for dictation mode.");
            }
            // Example: Register Ctrl+Alt+G for clipboard proofreading
            if (!globalHotkeyService.Register(
                    HOTKEY_ID_PROOFREAD,
                    GlobalHotkeyService.FsModifiers.Control | GlobalHotkeyService.FsModifiers.Alt,
                    Keys.P,
                    () => _ = ProofreadClipboardAsyncSafe()))
            {
                AppendToDebugOutput("Failed to register global hotkey Ctrl+Alt+P for clipboard proofreading!");
            }
            else
            {
                AppendToDebugOutput("Global hotkey Ctrl+Alt+P registered for clipboard proofreading.");
            }
        }

        private DateTime _lastHotkeyTime = DateTime.MinValue;
        private async void OnGlobalHotKeyPressed()
        {
            // Debounce: ignore triggers within 400ms
            if ((DateTime.UtcNow - _lastHotkeyTime).TotalMilliseconds < 400)
                return;
            _lastHotkeyTime = DateTime.UtcNow;

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

        // ADDED: Proofread clipboard using your existing LLM pipeline
        private async Task ProofreadClipboardAsyncSafe()
        {
            if (_isProofreadingClipboard)
                return;

            _isProofreadingClipboard = true;
            IDataObject? backupData = null;
            string? backupText = null;
            bool restored = false;

            try
            {
                // Backup entire clipboard (so we can restore non-text formats too)
                try
                {
                    backupData = Clipboard.GetDataObject();
                    if (clipboardContainsTextSafe())
                        backupText = Clipboard.GetText();
                }
                catch (Exception ex)
                {
                    AppendToDebugOutput($"Warning: could not read existing clipboard: {ex.Message}");
                    backupData = null;
                    backupText = null;
                }

                // Try copying the current selection. Retry & wait until clipboard changes or timeout.
                string clip = string.Empty;
                bool copySucceeded = false;
                const int maxAttempts = 5;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    // Use low-level SendInput rather than SendKeys
                    KeyboardSimulator.SendCtrlC();

                    // Progressive wait: first attempt short, later attempts longer
                    int waitMs = 120 + (attempt * 120);
                    await Task.Delay(waitMs);

                    try
                    {
                        clip = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
                    }
                    catch (Exception ex)
                    {
                        AppendToDebugOutput($"Clipboard read error after copy attempt {attempt + 1}: {ex.Message}");
                        clip = string.Empty;
                    }

                    // If we have non-empty text and it's different from previous text (if known), accept it
                    if (!string.IsNullOrWhiteSpace(clip) && (backupText == null || clip != backupText))
                    {
                        copySucceeded = true;
                        break;
                    }
                }

                if (!copySucceeded)
                {
                    AppendToDebugOutput("Clipboard proofreading: nothing selected or copy did not change clipboard.");
                    return;
                }

                AppendToDebugOutput("Clipboard proofreading selection: " + clip);

                if (isInDictationModeCurrently)
                {
                    AppendToDebugOutput("Clipboard proofreading skipped: dictation active.");
                    return;
                }

                AppendToDebugOutput("Clipboard proofreading: sending text to LLM...");
                var refined = await transcriptionService.ProcessTextWithLLMAsync(clip);

                if (!string.IsNullOrWhiteSpace(refined))
                {
                    try
                    {
                        Clipboard.SetText(refined);
                        await Task.Delay(80); // allow clipboard to propagate
                        // Use low-level paste
                        KeyboardSimulator.SendCtrlV();
                        await Task.Delay(150); // Give target app more time to process the paste command
                    }
                    catch (Exception ex)
                    {
                        AppendToDebugOutput("Clipboard paste error: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendToDebugOutput("Clipboard proofreading error: " + ex.Message);
            }
            finally
            {
                // Restore original clipboard state if we backed it up
                if (backupData != null)
                {
                    try
                    {
                        Clipboard.SetDataObject(backupData, true);
                        restored = true;
                    }
                    catch (Exception ex)
                    {
                        AppendToDebugOutput("Warning: could not restore original clipboard: " + ex.Message);
                    }
                }

                if (restored)
                    AppendToDebugOutput("Clipboard proofreading: original clipboard restored.");
                _isProofreadingClipboard = false;
            }

            // Local helper to safely check Clipboard.ContainsText without throwing on some clipboard states
            bool clipboardContainsTextSafe()
            {
                try
                {
                    return Clipboard.ContainsText();
                }
                catch
                {
                    return false;
                }
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
                if (!string.IsNullOrWhiteSpace(rawTextFilter)) AppendToTranscriptionOutput(rawTextFilter, false);
                AppendToDebugOutput("INFO: " + timestampedText + "\n");
            }

            if (isInDictationModeCurrently && !string.IsNullOrWhiteSpace(rawTextFilter))
            {
                AppendToDebugOutput($"Dictation output: {rawTextFilter}");
                await Task.Delay(50);
                Action<string> loggerAction = (logMsg) => AppendToDebugOutput($"SIMULATOR: {logMsg}");
                KeyboardSimulator.SendText(rawTextFilter + " ", false, loggerAction); // Add a space after each segment, disable simulator filtering
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
            UpdateStatusIndicator(isFormRecordingState ? AppStatus.Listening : AppStatus.Idle);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateUIFromServiceSettings();
            PopulateMicrophoneList();
            UpdateButtonStates();
            UpdateStatusIndicator(AppStatus.Idle);
            textBoxDebug.Visible = transcriptionService.Settings.ShowDebugMessages;

            // Populate VAD sensitivity combobox
            try
            {
                cmbVadSensitivity.Items.Clear();
                cmbVadSensitivity.Items.AddRange(new string[] { "Low (0)", "Medium (1)", "High (2)", "Max (3)" });
                int idx = transcriptionService.Settings.VadMode;
                if (idx < 0 || idx > 3) idx = 3;
                cmbVadSensitivity.SelectedIndex = idx;
            }
            catch (Exception ex)
            {
                AppendToDebugOutput("Error populating VAD combo: " + ex.Message);
            }
        }

        private bool _isClosing = false;
        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isClosing && isFormRecordingState && transcriptionService != null)
            {
                e.Cancel = true; // Prevent closing immediately while recording
                AppendToDebugOutput("Form closing during recording, stopping service...");
                btnStartStop.Enabled = false; // Prevent further interaction
                await transcriptionService.StopRecording();
                _isClosing = true;
                this.Close(); // Call Close again after we finish stopping
                return;
            }

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
            if (globalHotkeyService != null)
            {
                globalHotkeyService.HotKeyPressed -= OnGlobalHotKeyPressed; // Unsubscribe
                globalHotkeyService.Dispose(); // This calls UnregisterHotKey
            }

            if (_vramTimer != null)
            {
                _vramTimer.Stop();
                _vramTimer.Dispose();
            }
            foreach (var counter in _vramCounters)
            {
                counter.Dispose();
            }

            transcriptionService?.Dispose();
            Thread.Sleep(100);
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
            btnStartStop.Enabled = true;
            btnStartStop.Text = isFormRecordingState ? "Stop Recording" : "Start Recording";
            // Replace calibration button enable/disable with VAD sensitivity combobox
            cmbVadSensitivity.Enabled = !isFormRecordingState;
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

            // Keep combobox in sync if present
            try
            {
                int idx = transcriptionService.Settings.VadMode;
                if (idx < 0 || idx > 3) idx = 3;
                if (cmbVadSensitivity.SelectedIndex != idx)
                    cmbVadSensitivity.SelectedIndex = idx;
            }
            catch { }
        }

        private void PopulateMicrophoneList() // For a ComboBox or ListBox later
        {
            availableMicrophones = TranscriptionService.GetAvailableMicrophones();
            if (availableMicrophones.Count == 0)
            {
                MessageBox.Show("No microphones found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnStartStop.Enabled = false; // Disable record button
                // Replace calibration button references with VAD combobox
                cmbVadSensitivity.Enabled = false;
                btnMicInput.Enabled = false; // Disable mic select
            }
            else
            {
                btnStartStop.Enabled = true;
                // Enable VAD combobox
                cmbVadSensitivity.Enabled = true;
                btnMicInput.Enabled = true;
                AppendToDebugOutput($"Populated mics. Current in settings: {transcriptionService.Settings.SelectedMicrophoneDevice}");
            }
        }

        // --- Button Click Handlers ---
        private async void btnStart_Stop_Click(object sender, EventArgs e) // Record/Stop
        {
            btnStartStop.Enabled = false; // Prevent double clicks
            if (!isFormRecordingState)
            {
                if (availableMicrophones.Count == 0)
                {
                    MessageBox.Show("No microphone to record from.", "Error");
                    btnStartStop.Enabled = true;
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
                    btnStartStop.Enabled = true;
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

        // Removed btnCalibration_Click and lblCalibrationIndicator related logic per request.
        // (Calibration UI and logic replaced by WebRtcVadSharp and VAD sensitivity combo.)

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

        private async void btnLLMcb_Click(object sender, EventArgs e)
        {
            var LLM = await transcriptionService.ProcessTextWithLLMAsync(transcriptionService.LastRawFilteredText);
            textBoxOutput.Text += Environment.NewLine;
            textBoxOutput.Text += transcriptionService.LastRawFilteredText;
            textBoxOutput.Text += Environment.NewLine;
            textBoxOutput.Text += LLM;
            transcriptionService.LastLLMProcessedText = LLM;
        }

        private void cmbVadSensitivity_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int idx = cmbVadSensitivity.SelectedIndex;
            if (idx < 0 || idx > 3) idx = 3;
            transcriptionService.SetVadMode(idx);
            AppendToDebugOutput($"VAD sensitivity set to {idx}.");
        }
    }
}
