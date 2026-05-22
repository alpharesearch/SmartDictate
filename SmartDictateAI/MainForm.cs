// MainForm.cs
using CommunityToolkit.HighPerformance;
using System;
using System.IO; // For Path
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SmartDictateAI
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
        private bool isStoppingOrProcessingFinal = false; // UI flag to prevent overlap during stop processing
        private List<(int Index, string Name)> availableMicrophones = new List<(int, string)>();

        private enum AppStatus
            {
            Idle, Calibrating, Listening, Processing, Error
            }
        private Color idleColor = Color.FromArgb(240, 240, 240); // Sleek modern light gray
        private Color listeningColor = Color.FromArgb(235, 245, 255); // Pastel blue
        private Color processingColor = Color.FromArgb(255, 248, 230); // Pastel orange
        private Color errorColor = Color.FromArgb(255, 235, 235); // Pastel red
        private Color calibratingColor = Color.FromArgb(255, 255, 230); // Pastel yellow

        private System.Windows.Forms.Timer? _vramTimer;
        private List<PerformanceCounter> _vramCounters = new List<PerformanceCounter>();
        private Dictionary<string, PerformanceCounter> _processVramCounters = new Dictionary<string, PerformanceCounter>();
        private bool _loadingUi;

        public MainForm()
            {
            _loadingUi = true;
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
            transcriptionService.VisualStateChanged += TranscriptionService_VisualStateChanged;

            // Set initial UI state from settings
            cmbPromptSelect.DataSource = transcriptionService.Settings.PromptProfiles;
            cmbPromptSelect.DisplayMember = "Name";
            cmbPromptSelect.ValueMember = "Name";


            textBoxDebug.Visible = transcriptionService.Settings.ShowDebugMessages; // txtDebugOutput

            globalHotkeyService = new GlobalHotkeyService(this.Handle);
            globalHotkeyService.HotKeyPressed += OnGlobalHotKeyPressed;

            InitializeHotkeyService();
            btnCopyRawText.Enabled = false;
            btnCopyLLMText.Enabled = false;
            btnLLMcb.Enabled = false;
            SetupContextMenus();
            _loadingUi = false;
            AppendToDebugOutput($"[UI] Init comboBox1.SelectedValue: {transcriptionService.Settings.ActivePromptProfileName}");
            cmbPromptSelect.SelectedIndex = -1; // Force a selection change
            cmbPromptSelect.SelectedValue = transcriptionService.Settings.ActivePromptProfileName;
            }

        private void SetupContextMenus()
            {
            // Output TextBox Context Menu
            var outputMenu = new ContextMenuStrip();
            outputMenu.Items.Add("Cut", null, (s, e) => textBoxOutput.Cut());
            outputMenu.Items.Add("Copy", null, (s, e) => textBoxOutput.Copy());
            outputMenu.Items.Add("Paste", null, (s, e) => textBoxOutput.Paste());
            outputMenu.Items.Add("Delete", null, (s, e) => textBoxOutput.SelectedText = "");
            outputMenu.Items.Add(new ToolStripSeparator());
            outputMenu.Items.Add("Select All", null, (s, e) => textBoxOutput.SelectAll());
            outputMenu.Items.Add(new ToolStripSeparator());
            outputMenu.Items.Add("Clear All", null, (s, e) => textBoxOutput.Clear());

            outputMenu.Opening += (s, e) =>
            {
                outputMenu.Items[0].Enabled = textBoxOutput.SelectionLength > 0; // Cut
                outputMenu.Items[1].Enabled = textBoxOutput.SelectionLength > 0; // Copy
                outputMenu.Items[2].Enabled = Clipboard.ContainsText();          // Paste
                outputMenu.Items[3].Enabled = textBoxOutput.SelectionLength > 0; // Delete
            };

            textBoxOutput.ContextMenuStrip = outputMenu;

            // Debug TextBox Context Menu (often read-only, so mainly copy/select all/clear)
            var debugMenu = new ContextMenuStrip();
            debugMenu.Items.Add("Copy", null, (s, e) => textBoxDebug.Copy());
            debugMenu.Items.Add(new ToolStripSeparator());
            debugMenu.Items.Add("Select All", null, (s, e) => textBoxDebug.SelectAll());
            debugMenu.Items.Add(new ToolStripSeparator());
            debugMenu.Items.Add("Clear All", null, (s, e) => textBoxDebug.Clear());

            debugMenu.Opening += (s, e) =>
            {
                debugMenu.Items[0].Enabled = textBoxDebug.SelectionLength > 0; // Copy
            };

            textBoxDebug.ContextMenuStrip = debugMenu;
            }

        private void InitializeVramMonitor()
            {
            Task.Run(() =>
            {
                try
                    {
                    var category = new PerformanceCounterCategory("GPU Adapter Memory");
                    var instances = category.GetInstanceNames();
                    var newCounters = new List<PerformanceCounter>();
                    foreach (var instance in instances)
                        {
                        newCounters.Add(new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", instance));
                        }

                    try
                        {
                        this.BeginInvoke(new Action(() =>
                        {
                            if (this.IsDisposed)
                                return;
                            _vramCounters.AddRange(newCounters);
                            _vramTimer = new System.Windows.Forms.Timer();
                            _vramTimer.Interval = 1000; // Update every 1 second
                            _vramTimer.Tick += (s, e) => UpdateVramUsage();
                            _vramTimer.Start();
                        }));
                        }
                    catch (InvalidOperationException) { } // Handle already disposed
                    }
                catch (Exception ex)
                    {
                    AppendToDebugOutput($"[UI] Failed to initialize VRAM performance counter: {ex.Message}. Ensure 'System.Diagnostics.PerformanceCounter' NuGet package is installed.");
                    }
            });
            }

        private void UpdateVramUsage()
            {
            // Get standard CPU RAM (Working Set) usage
            long ramBytes = Process.GetCurrentProcess().WorkingSet64;
            string ramText = $"RAM: {ramBytes / (1024f * 1024f * 1024f):F2} GB";

            if (_vramCounters.Count > 0)
                {
                try
                    {
                    float totalVramBytes = 0;
                    foreach (var counter in _vramCounters)
                        {
                        totalVramBytes += counter.NextValue();
                        }

                    float processVramBytes = 0;
                    bool processVramSuccess = false;
                    try
                        {
                        string pidPrefix = $"pid_{Process.GetCurrentProcess().Id}_";
                        var processCategory = new PerformanceCounterCategory("GPU Process Memory");
                        var currentInstances = processCategory.GetInstanceNames();

                        foreach (var instance in currentInstances)
                            {
                            // Check for instances matching our current process ID and add them if we aren't tracking them yet
                            if (instance.StartsWith(pidPrefix, StringComparison.OrdinalIgnoreCase) && !_processVramCounters.ContainsKey(instance))
                                {
                                _processVramCounters[instance] = new PerformanceCounter("GPU Process Memory", "Dedicated Usage", instance, true);
                                }
                            }

                        // Remove counters for GPU engines/allocations that our process has closed
                        var toRemove = new List<string>();
                        foreach (var key in _processVramCounters.Keys)
                            {
                            if (Array.IndexOf(currentInstances, key) < 0)
                                {
                                toRemove.Add(key);
                                }
                            }
                        foreach (var key in toRemove)
                            {
                            _processVramCounters[key].Dispose();
                            _processVramCounters.Remove(key);
                            }

                        // Accumulate the memory specifically used by this application
                        foreach (var counter in _processVramCounters.Values)
                            {
                            processVramBytes += counter.NextValue();
                            }
                        processVramSuccess = true;
                        }
                    catch
                        {
                        // Process VRAM tracking might fail if the OS/Drivers don't support the specific performance category.
                        // It will gracefully fall back to just total VRAM below.
                        }

                    if (processVramSuccess)
                        {
                        lblVramUsage.Text = $"{ramText} | VRAM: {processVramBytes / (1024f * 1024f * 1024f):F2} GB (App) / {totalVramBytes / (1024f * 1024f * 1024f):F2} GB (Total)";
                        }
                    else
                        {
                        lblVramUsage.Text = $"{ramText} | VRAM: {totalVramBytes / (1024f * 1024f * 1024f):F2} GB";
                        }
                    }
                catch
                    {
                    lblVramUsage.Text = $"{ramText} | VRAM: Error";
                    }
                }
            else
                {
                lblVramUsage.Text = $"{ramText} | VRAM: N/A";
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
            GlobalHotkeyService.FsModifiers dictationMods = GlobalHotkeyService.FsModifiers.Control | GlobalHotkeyService.FsModifiers.Alt;
            Keys dictationKey = Keys.D;
            try
                {
                dictationMods = (GlobalHotkeyService.FsModifiers)Enum.Parse(typeof(GlobalHotkeyService.FsModifiers), transcriptionService.Settings.DictationHotkeyModifiers, true);
                dictationKey = (Keys)Enum.Parse(typeof(Keys), transcriptionService.Settings.DictationHotkeyKey, true);
                }
            catch (Exception ex)
                {
                AppendToDebugOutput($"[Hotkey] Failed to parse dictation hotkey settings: {ex.Message}. Using defaults.");
                }

            if (!globalHotkeyService.Register(dictationMods, dictationKey))
                {
                AppendToDebugOutput($"[Hotkey] Failed to register global hotkey {transcriptionService.Settings.DictationHotkeyModifiers}+{transcriptionService.Settings.DictationHotkeyKey} for dictation mode!");
                }
            else
                {
                AppendToDebugOutput($"[Hotkey] Global hotkey {transcriptionService.Settings.DictationHotkeyModifiers}+{transcriptionService.Settings.DictationHotkeyKey} registered for dictation mode.");
                }

            GlobalHotkeyService.FsModifiers proofreadMods = GlobalHotkeyService.FsModifiers.Control | GlobalHotkeyService.FsModifiers.Alt;
            Keys proofreadKey = Keys.P;
            try
                {
                proofreadMods = (GlobalHotkeyService.FsModifiers)Enum.Parse(typeof(GlobalHotkeyService.FsModifiers), transcriptionService.Settings.ProofreadHotkeyModifiers, true);
                proofreadKey = (Keys)Enum.Parse(typeof(Keys), transcriptionService.Settings.ProofreadHotkeyKey, true);
                }
            catch (Exception ex)
                {
                AppendToDebugOutput($"[Hotkey] Failed to parse proofread hotkey settings: {ex.Message}. Using defaults.");
                }

            if (!globalHotkeyService.Register(
                    HOTKEY_ID_PROOFREAD,
                    proofreadMods,
                    proofreadKey,
                    () => _ = ProofreadClipboardAsyncSafe()))
                {
                AppendToDebugOutput($"[Hotkey] Failed to register global hotkey {transcriptionService.Settings.ProofreadHotkeyModifiers}+{transcriptionService.Settings.ProofreadHotkeyKey} for clipboard proofreading!");
                }
            else
                {
                AppendToDebugOutput($"[Hotkey] Global hotkey {transcriptionService.Settings.ProofreadHotkeyModifiers}+{transcriptionService.Settings.ProofreadHotkeyKey} registered for clipboard proofreading.");
                }
            }

        private DateTime _lastHotkeyTime = DateTime.MinValue;
        private async void OnGlobalHotKeyPressed()
            {
            // Debounce: ignore triggers within 400ms
            if ((DateTime.UtcNow - _lastHotkeyTime).TotalMilliseconds < 400)
                return;
            _lastHotkeyTime = DateTime.UtcNow;

            AppendToDebugOutput("[Hotkey] Global Hotkey Pressed!");

            if (!isInDictationModeCurrently) // If not in dictation mode, start it
                {
                if (isFormRecordingState) // If normal recording is active
                    {
                    AppendToDebugOutput("[Dictation] Normal recording active. Stop it before starting dictation mode.");
                    MessageBox.Show("Please stop the current recording session before starting dictation mode.", "Info");
                    return;
                    }
                AppendToDebugOutput("[Dictation] Attempting to start dictation mode...");
                UpdateStatusIndicator(AppStatus.Processing, "Dictation Starting...");
                isInDictationModeCurrently = true; // Optimistic
                bool success = await transcriptionService.StartDictationModeAsync(transcriptionService.Settings.SelectedMicrophoneDevice);
                if (success)
                    {
                    AppendToDebugOutput("[Dictation] Dictation mode started.");
                    UpdateStatusIndicator(AppStatus.Listening, "Dictating...");
                    // Optionally minimize or hide your main form
                    // this.WindowState = FormWindowState.Minimized;
                    }
                else
                    {
                    AppendToDebugOutput("[Dictation] Failed to start dictation mode.");
                    UpdateStatusIndicator(AppStatus.Error, "Dictation start failed");
                    isInDictationModeCurrently = false; // Revert
                    }
                }
            else // If already in dictation mode, stop it
                {
                AppendToDebugOutput("[Dictation] Attempting to stop dictation mode...");
                UpdateStatusIndicator(AppStatus.Processing, "Dictation Stopping...");
                isStoppingOrProcessingFinal = true;
                UpdateButtonStates();
                await transcriptionService.StopDictationModeAsync();
                isStoppingOrProcessingFinal = false;
                isInDictationModeCurrently = false;
                UpdateStatusIndicator(AppStatus.Idle, "Dictation Ended");
                AppendToDebugOutput("[Dictation] Dictation mode stopped.");
                UpdateButtonStates();
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
                    AppendToDebugOutput($"[Clipboard] Warning: could not read existing clipboard: {ex.Message}");
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
                        AppendToDebugOutput($"[Clipboard] Clipboard read error after copy attempt {attempt + 1}: {ex.Message}");
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
                    AppendToDebugOutput("[Clipboard] Clipboard proofreading: nothing selected or copy did not change clipboard.");
                    return;
                    }

                AppendToDebugOutput("[Clipboard] Clipboard proofreading selection: " + clip);

                if (isInDictationModeCurrently)
                    {
                    AppendToDebugOutput("[Clipboard] Clipboard proofreading skipped: dictation active.");
                    return;
                    }

                AppendToDebugOutput("[Clipboard] Clipboard proofreading: sending text to LLM...");
                var refined = await transcriptionService.ProcessTextWithLLMAsync(clip);

                // Update transcription service state so the copy buttons work
                transcriptionService.LastRawFilteredText = clip;
                transcriptionService.LastLLMProcessedText = refined;
                transcriptionService.WasLastProcessingWithLLM = !string.IsNullOrWhiteSpace(refined);

                // Append output to the UI like Dictation does
                AppendToTranscriptionOutput("", true);
                AppendToTranscriptionOutput("--- Clipboard Proofreading ---", true);
                AppendToTranscriptionOutput("Original: " + clip, true);

                if (!string.IsNullOrWhiteSpace(refined))
                    {
                    AppendToTranscriptionOutput("LLM Refined: " + refined, true);
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
                        AppendToDebugOutput("[Clipboard] Clipboard paste error: " + ex.Message);
                        }
                    }
                else
                    {
                    AppendToTranscriptionOutput("LLM Refined: [No Output]", true);
                    }
                AppendToTranscriptionOutput("------------------------------", true);

                // Re-evaluate button states to enable Copy buttons
                UpdateButtonStates();
                }
            catch (Exception ex)
                {
                AppendToDebugOutput("[Clipboard] Clipboard proofreading error: " + ex.Message);
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
                        AppendToDebugOutput("[Clipboard] Warning: could not restore original clipboard: " + ex.Message);
                        }
                    }

                if (restored)
                    AppendToDebugOutput("[Clipboard] Clipboard proofreading: original clipboard restored.");
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
                if (!string.IsNullOrWhiteSpace(rawTextFilter))
                    AppendToTranscriptionOutput(rawTextFilter, false);
                AppendToDebugOutput("[UI] INFO: " + timestampedText + "\n");
                }

            if (isInDictationModeCurrently && !string.IsNullOrWhiteSpace(rawTextFilter))
                {
                AppendToDebugOutput($"[Dictation] Dictation output: {rawTextFilter}");
                await Task.Delay(50);
                Action<string> loggerAction = (logMsg) => AppendToDebugOutput($"[UI] SIMULATOR: {logMsg}");
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
            Color textColor = Color.DimGray;

            switch (status)
                {
                case AppStatus.Idle:
                    displayText = string.IsNullOrWhiteSpace(message) ? "Ready" : message;
                    displayColor = idleColor;
                    textColor = Color.DimGray;
                    break;
                case AppStatus.Listening:
                    displayText = string.IsNullOrWhiteSpace(message) ? "Listening..." : message;
                    displayColor = listeningColor;
                    textColor = Color.FromArgb(0, 102, 204);
                    break;
                case AppStatus.Processing:
                    displayText = string.IsNullOrWhiteSpace(message) ? "Processing..." : message;
                    displayColor = processingColor;
                    textColor = Color.FromArgb(230, 115, 0);
                    break;
                case AppStatus.Error:
                    displayText = string.IsNullOrWhiteSpace(message) ? "Error" : message;
                    displayColor = errorColor;
                    textColor = Color.FromArgb(204, 0, 0);
                    break;
                case AppStatus.Calibrating:
                    displayText = string.IsNullOrWhiteSpace(message) ? "Calibrating..." : message;
                    displayColor = calibratingColor;
                    textColor = Color.FromArgb(153, 115, 0);
                    break;
                }
            lblStatusIndicator.Text = displayText;
            lblStatusIndicator.BackColor = displayColor;
            lblStatusIndicator.ForeColor = textColor;
            }

        private void TranscriptionService_VisualStateChanged(DictationVisualState state)
            {
            if (this.InvokeRequired)
                {
                this.BeginInvoke(new Action(() => TranscriptionService_VisualStateChanged(state)));
                return;
                }

            switch (state)
                {
                case DictationVisualState.Idle:
                    lblStatusIndicator.Text = "⚪ Ready";
                    lblStatusIndicator.BackColor = Color.FromArgb(240, 240, 240);
                    lblStatusIndicator.ForeColor = Color.DimGray;
                    break;

                case DictationVisualState.ListeningSilent:
                    lblStatusIndicator.Text = "🎤 Listening...";
                    lblStatusIndicator.BackColor = Color.FromArgb(235, 245, 255);
                    lblStatusIndicator.ForeColor = Color.FromArgb(0, 102, 204);
                    break;

                case DictationVisualState.SpeechDetected:
                    lblStatusIndicator.Text = "🔥 Speaking";
                    lblStatusIndicator.BackColor = Color.FromArgb(235, 255, 240);
                    lblStatusIndicator.ForeColor = Color.FromArgb(0, 153, 51);
                    break;

                case DictationVisualState.Processing:
                    lblStatusIndicator.Text = "⏳ Processing Text...";
                    lblStatusIndicator.BackColor = Color.FromArgb(255, 248, 230);
                    lblStatusIndicator.ForeColor = Color.FromArgb(230, 115, 0);
                    break;
                }
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

        private void MainForm_Load(object sender, EventArgs e)
            {
            UpdateUIFromServiceSettings();
            PopulateMicrophoneList();
            UpdateButtonStates();
            UpdateStatusIndicator(AppStatus.Idle);
            textBoxDebug.Visible = transcriptionService.Settings.ShowDebugMessages;

            // Update UI Labels with configured hotkeys
            lblDictateInstruction.Text = $"{transcriptionService.Settings.DictationHotkeyModifiers} + {transcriptionService.Settings.DictationHotkeyKey} \r\nto dictate cursor.";
            lblProofreadInstruction.Text = $"{transcriptionService.Settings.ProofreadHotkeyModifiers} + {transcriptionService.Settings.ProofreadHotkeyKey} \r\nto proofreads clipboard locally.";



            InitializeVramMonitor();

            // Fire and forget model preloading in the background
            _ = transcriptionService.PreloadModelsAsync();
            }

        private bool _isClosing = false;
        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
            {
            AppendToDebugOutput($"[UI] Saving because form is closing");
            transcriptionService?.SaveAppSettings();
            if (!_isClosing && isFormRecordingState && transcriptionService != null)
                {
                e.Cancel = true; // Prevent closing immediately while recording
                AppendToDebugOutput("[Audio] Form closing during recording, stopping service...");
                btnStartStop.Enabled = false; // Prevent further interaction
                await transcriptionService.StopRecording();
                _isClosing = true;
                this.Close(); // Call Close again after we finish stopping
                return;
                }

            if (transcriptionService != null)
                {
                AppendToDebugOutput("[UI] MainForm_FormClosing: Unsubscribing from TranscriptionService events.");
                transcriptionService.DebugMessageGenerated -= OnDebugMessageReceived;
                transcriptionService.SegmentTranscribed -= OnServiceSegmentTranscribedForDictation;
                transcriptionService.FullTranscriptionReady -= OnFullTranscriptionCompleted;
                transcriptionService.RecordingStateChanged -= OnServiceRecordingStateChanged;
                transcriptionService.SettingsUpdated -= OnServiceSettingsUpdated;
                transcriptionService.ProcessingStarted -= OnServiceProcessingStarted;
                transcriptionService.ProcessingFinished -= OnServiceProcessingFinished;
                transcriptionService.VisualStateChanged -= TranscriptionService_VisualStateChanged;
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
            foreach (var counter in _processVramCounters.Values)
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
            if (this.InvokeRequired)
                {
                this.Invoke(new Action<bool>(OnServiceRecordingStateChanged), nowRecording);
                return;
                }

            isFormRecordingState = nowRecording; // Update UI state
            if (nowRecording)
                {
                UpdateStatusIndicator(AppStatus.Listening);
                btnCopyRawText.Enabled = false; // Disable during recording
                btnCopyLLMText.Enabled = false; // Disable during recording
                }
            else
                {
                if (isInDictationModeCurrently)
                    {
                    if (!isStoppingOrProcessingFinal)
                        {
                        isInDictationModeCurrently = false;
                        UpdateStatusIndicator(AppStatus.Idle, "Dictation Ended");
                        }
                    }
                else if (!activelyProcessingChunkInUI)
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

            if (isStoppingOrProcessingFinal)
                {
                btnStartStop.Enabled = false;
                btnStartStop.Text = "Processing...";
                }
            else
                {
                btnStartStop.Enabled = true;
                btnStartStop.Text = isFormRecordingState ? "Stop Recording" : "Start Recording";
                }

            // Enable/disable Settings button
            btnSettings.Enabled = !isFormRecordingState && !isStoppingOrProcessingFinal;

            // Copy and rerun LLM button states
            if (!isFormRecordingState && !isStoppingOrProcessingFinal) // Only enable copy and rerun LLM buttons when not recording and not stopping/processing
                {
                btnCopyRawText.Enabled = !string.IsNullOrEmpty(transcriptionService.LastRawFilteredText);
                btnCopyLLMText.Enabled = transcriptionService.WasLastProcessingWithLLM &&
                                         !string.IsNullOrEmpty(transcriptionService.LastLLMProcessedText);
                btnLLMcb.Enabled = !string.IsNullOrEmpty(transcriptionService.LastRawFilteredText);
                }
            else
                {
                btnCopyRawText.Enabled = false;
                btnCopyLLMText.Enabled = false;
                btnLLMcb.Enabled = false;
                }

            }

        private void UpdateUIFromServiceSettings()
            {
            if (this.InvokeRequired)
                {
                this.Invoke(new Action(UpdateUIFromServiceSettings));
                return;
                }

            bool showDebug = transcriptionService.Settings.ShowDebugMessages;
            textBoxDebug.Visible = showDebug;
            gbDebug.Visible = showDebug;

            // Detach bottom anchors temporarily so textBoxOutput doesn't stretch during programmatic resize
            textBoxOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbDebug.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Reset locations and sizes to default to discard manual vertical resizing
            if (this.WindowState == FormWindowState.Normal)
                {
                textBoxOutput.Height = 206;
                gbControl.Top = 286;
                gbDebug.Top = 347;
                }

            if (showDebug)
                {
                this.MinimumSize = new Size(500, 645);
                if (this.WindowState == FormWindowState.Normal)
                    this.Size = new Size(this.Width, 645);
                }
            else
                {
                this.MinimumSize = new Size(500, 386);
                if (this.WindowState == FormWindowState.Normal)
                    this.Size = new Size(this.Width, 386);
                }

            // Restore bottom anchors for vertical scaling
            textBoxOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gbControl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gbDebug.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            }

        private void PopulateMicrophoneList() // For a ComboBox or ListBox later
            {
            availableMicrophones = TranscriptionService.GetAvailableMicrophones();
            if (availableMicrophones.Count == 0)
                {
                MessageBox.Show("No microphones found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnStartStop.Enabled = false; // Disable record button
                btnSettings.Enabled = false; // Disable Settings button
                }
            else
                {
                btnStartStop.Enabled = true;
                btnSettings.Enabled = true;
                AppendToDebugOutput($"[Audio] Populated mics. Current in settings: {transcriptionService.Settings.SelectedMicrophoneDevice}");
                }
            }

        // --- Button Click Handlers ---
        private async void btnStartStop_Click(object sender, EventArgs e) // Record/Stop
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
                AppendToDebugOutput("[Audio] Start recording button clicked.");
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
                if (isInDictationModeCurrently)
                    {
                    AppendToDebugOutput("[Dictation] Stop dictation mode button clicked.");
                    UpdateStatusIndicator(AppStatus.Processing, "Dictation Stopping...");
                    isStoppingOrProcessingFinal = true;
                    UpdateButtonStates();
                    await transcriptionService.StopDictationModeAsync();
                    isStoppingOrProcessingFinal = false;
                    isInDictationModeCurrently = false;
                    UpdateStatusIndicator(AppStatus.Idle, "Dictation Ended");
                    AppendToDebugOutput("[Dictation] Dictation mode stopped.");
                    }
                else
                    {
                    AppendToDebugOutput("[Audio] Stop recording button clicked.");
                    UpdateStatusIndicator(AppStatus.Processing, "Stopping..."); // Indicate stopping is a form of processing
                    isStoppingOrProcessingFinal = true;
                    UpdateButtonStates();
                    Task stopTask = transcriptionService.StopRecording();
                    await stopTask;
                    isStoppingOrProcessingFinal = false;
                    }
                UpdateButtonStates();
                // RecordingStateChanged event will update isFormRecordingState and button text
                // Full transcription will be raised by FullTranscriptionReady event
                }
            }

        // Removed btnCalibration_Click and lblCalibrationIndicator related logic per request.
        // (Calibration UI and logic replaced by WebRtcVadSharp and VAD sensitivity combo.)

        // MainForm.cs

        private async void btnSettings_Click(object sender, EventArgs e)
            {
            if (isFormRecordingState)
                {
                MessageBox.Show("Stop recording before opening Settings.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
                }

            using (SettingsForm settingsForm = new SettingsForm(transcriptionService.Settings, availableMicrophones))
                {
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                    {
                    var newSettings = settingsForm.UpdatedSettings;
                    if (newSettings == null) return;

                    bool whisperChanged = transcriptionService.Settings.ModelFilePath != newSettings.ModelFilePath;
                    bool llmChanged = transcriptionService.Settings.LocalLLMModelPath != newSettings.LocalLLMModelPath;

                    // Select the microphone in the service
                    transcriptionService.SelectMicrophone(newSettings.SelectedMicrophoneDevice);

                    // Apply model changes asynchronously (must run before copying other fields to ensure correct change detection)
                    if (whisperChanged)
                        {
                        AppendToDebugOutput($"[Settings] New Whisper model selected: {newSettings.ModelFilePath}");
                        await transcriptionService.ChangeModelPathAsync(newSettings.ModelFilePath);
                        }

                    if (llmChanged)
                        {
                        AppendToDebugOutput($"[Settings] New LLM model selected: {newSettings.LocalLLMModelPath}");
                        await transcriptionService.ChangeLLMModelPathAsync(newSettings.LocalLLMModelPath);
                        }

                    // Copy all settings fields including newly exposed advanced options
                    transcriptionService.Settings.CopyFrom(newSettings);

                    // Save settings
                    transcriptionService.SaveAppSettings();

                    // Apply UI visibility updates
                    UpdateUIFromServiceSettings();
                    UpdateButtonStates();

                    // Keep combobox in sync for Prompt Selection on MainForm
                    _loadingUi = true;
                    cmbPromptSelect.DataSource = null;
                    cmbPromptSelect.DataSource = transcriptionService.Settings.PromptProfiles;
                    cmbPromptSelect.DisplayMember = "Name";
                    cmbPromptSelect.ValueMember = "Name";
                    cmbPromptSelect.SelectedValue = transcriptionService.Settings.ActivePromptProfileName;
                    _loadingUi = false;

                    AppendToDebugOutput("[Settings] Settings successfully updated and saved.");
                    }
                else
                    {
                    AppendToDebugOutput("[Settings] Changes discarded.");
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
                    AppendToDebugOutput("[Clipboard] Raw text copied to clipboard.");
                    }
                catch (Exception ex)
                    {
                    AppendToDebugOutput($"[Clipboard] Error copying raw text: {ex.Message}");
                    }
                }
            else
                {
                AppendToDebugOutput("[Clipboard] No raw text available to copy.");
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
                    AppendToDebugOutput("[Clipboard] LLM refined text copied to clipboard.");
                    }
                catch (Exception ex)
                    {
                    AppendToDebugOutput($"[Clipboard] Error copying LLM text: {ex.Message}");
                    }
                }
            else
                {
                AppendToDebugOutput("[Clipboard] No LLM refined text available to copy (LLM might be off or produced no output).");
                }
            }



        private async void btnLLMcb_Click(object sender, EventArgs e)
            {
            try
                {
                btnLLMcb.Enabled = false;
                var LLM = await transcriptionService.ProcessTextWithLLMAsync(transcriptionService.LastRawFilteredText);
                textBoxOutput.Text += Environment.NewLine;
                textBoxOutput.Text += transcriptionService.LastRawFilteredText;
                textBoxOutput.Text += Environment.NewLine;
                textBoxOutput.Text += LLM;
                transcriptionService.LastLLMProcessedText = LLM;
                transcriptionService.WasLastProcessingWithLLM = !string.IsNullOrWhiteSpace(LLM);
                }
            catch (Exception ex)
                {
                AppendToDebugOutput($"[LLM] Error rerunning LLM: {ex.Message}");
                }
            finally
                {
                UpdateButtonStates();
                }
            }



        private void cmbPromptSelect_SelectedValueChanged(object sender, EventArgs e)
            {
            if (_loadingUi)
                return;
            //transcriptionService.Settings.ActivePromptProfileName = comboBox1.SelectedValue?.ToString() ?? "";
            AppendToDebugOutput($"[UI] Prompt selection changed: {cmbPromptSelect.SelectedValue?.ToString() ?? ""}.");

            if (cmbPromptSelect.SelectedItem is PromptProfile profile)
                {
                transcriptionService.Settings.ActivePromptProfileName = profile.Name;

                // If you want to immediately apply prompts to your LLM settings:
                transcriptionService.Settings.LLMSystemPrompt = profile.SystemPrompt;
                transcriptionService.Settings.LLMUserPrompt = profile.UserPrompt;

                AppendToDebugOutput($"[UI] Selected profile: {profile.Name}");
                }
            transcriptionService.SaveAppSettings();
            }
        }
    }
