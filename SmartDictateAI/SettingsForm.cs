using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SmartDictateAI
{
    public partial class SettingsForm : Form
        {
        private AppSettings _clonedSettings;
        private bool _loadingForm = true;

        // Advanced LLM Engine Controls (Tab 1)
        private GroupBox grpLlmAdvanced = default!;
        private Label lblLlmContextSize = default!;
        private NumericUpDown numLlmContextSize = default!;
        private Label lblLlmSeed = default!;
        private NumericUpDown numLlmSeed = default!;
        private Label lblLlmTemperature = default!;
        private NumericUpDown numLlmTemperature = default!;
        private Label lblLlmMaxTokens = default!;
        private NumericUpDown numLlmMaxTokens = default!;
        private CheckBox chkMaintainContext = default!;

        // Advanced Audio & VAD Controls (Tab 2)
        private GroupBox grpAudioChunking = default!;
        private Label lblVadGain = default!;
        private NumericUpDown numVadGain = default!;
        private GroupBox grpNormalMode = default!;
        private Label lblNormalMaxDuration = default!;
        private NumericUpDown numNormalMaxDuration = default!;
        private Label lblNormalSilence = default!;
        private NumericUpDown numNormalSilence = default!;
        private GroupBox grpDictationMode = default!;
        private Label lblDictationMaxDuration = default!;
        private NumericUpDown numDictationMaxDuration = default!;
        private Label lblDictationSilence = default!;
        private NumericUpDown numDictationSilence = default!;

        // Tab 4 (General / Hotkeys)
        private CheckBox chkShowRealtime = default!;
        private GroupBox grpHotkeys = default!;
        private Label lblDictationMods = default!;
        private TextBox txtDictationModifiers = default!;
        private Label lblDictationKey = default!;
        private TextBox txtDictationKey = default!;
        private Label lblProofreadMods = default!;
        private TextBox txtProofreadModifiers = default!;
        private Label lblProofreadKey = default!;
        private TextBox txtProofreadKey = default!;
        private Label lblHotkeyNote = default!;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AppSettings? UpdatedSettings
            {
            get; private set;
            }

        private class MicItem
            {
            public int Index
                {
                get; set;
                }
            public string Name { get; set; } = string.Empty;
            public override string ToString() => $"[{Index}] {Name}";
            }

        private class VadModeItem
            {
            public int ModeValue
                {
                get; set;
                }
            public string DisplayName { get; set; } = string.Empty;
            public override string ToString() => DisplayName;
            }

        public SettingsForm(AppSettings currentSettings, List<(int Index, string Name)> availableMics)
            {
            InitializeComponent();

            // Perform deep copy/clone of the settings to support atomic Cancel/OK transitions
            _clonedSettings = CloneSettings(currentSettings);

            // 1. Models Tab
            txtWhisperModelPath.Text = _clonedSettings.ModelFilePath;
            txtLLMModelPath.Text = _clonedSettings.LocalLLMModelPath;
            chkUseGpu.Checked = _clonedSettings.UseGpu;

            // 2. Audio & VAD Tab
            PopulateMicrophones(availableMics);
            PopulateVadModes();

            // 3. LLM & Prompts Tab
            chkProcessWithLlm.Checked = _clonedSettings.ProcessWithLLM;
            PopulatePromptProfiles();
            txtSystemPrompt.Text = _clonedSettings.LLMSystemPrompt;
            txtUserPrompt.Text = _clonedSettings.LLMUserPrompt;

            // Toggle LLM input controls based on ProcessWithLLM state
            ToggleLlmControls(chkProcessWithLlm.Checked);

            // 4. General Tab
            chkShowDebug.Checked = _clonedSettings.ShowDebugMessages;

            // 5. Initialize advanced controls programmatically
            InitializeAdvancedControls();

            // Load values to advanced controls (with clamping to guarantee no overflow out of bounds)
            numLlmContextSize.Value = Math.Max(512, Math.Min(131072, _clonedSettings.LLMContextSize));
            numLlmSeed.Value = Math.Max(0, Math.Min(int.MaxValue, _clonedSettings.LLMSeed));
            numLlmTemperature.Value = Math.Max(0.0M, Math.Min(2.0M, (decimal)_clonedSettings.LLMTemperature));
            numLlmMaxTokens.Value = Math.Max(-1, Math.Min(32768, _clonedSettings.LLMMaxOutputTokens));
            chkMaintainContext.Checked = _clonedSettings.MaintainContextAcrossChunks;

            numVadGain.Value = Math.Max(0.1M, Math.Min(10.0M, (decimal)_clonedSettings.VadGainMultiplier));
            numNormalMaxDuration.Value = Math.Max(1.0M, Math.Min(60.0M, (decimal)_clonedSettings.NormalMaxChunkDurationSeconds));
            numNormalSilence.Value = Math.Max(0.1M, Math.Min(10.0M, (decimal)_clonedSettings.NormalSilenceThresholdSeconds));
            numDictationMaxDuration.Value = Math.Max(1.0M, Math.Min(60.0M, (decimal)_clonedSettings.DictationMaxChunkDurationSeconds));
            numDictationSilence.Value = Math.Max(0.1M, Math.Min(10.0M, (decimal)_clonedSettings.DictationSilenceThresholdSeconds));

            chkShowRealtime.Checked = _clonedSettings.ShowRealtimeTranscription;
            txtDictationModifiers.Text = _clonedSettings.DictationHotkeyModifiers;
            txtDictationKey.Text = _clonedSettings.DictationHotkeyKey;
            txtProofreadModifiers.Text = _clonedSettings.ProofreadHotkeyModifiers;
            txtProofreadKey.Text = _clonedSettings.ProofreadHotkeyKey;

            _loadingForm = false;
            }

        private AppSettings CloneSettings(AppSettings source)
            {
            return new AppSettings
                {
                SelectedMicrophoneDevice = source.SelectedMicrophoneDevice,
                ModelFilePath = source.ModelFilePath,
                VadMode = source.VadMode,
                ShowRealtimeTranscription = source.ShowRealtimeTranscription,
                ShowDebugMessages = source.ShowDebugMessages,
                ProcessWithLLM = source.ProcessWithLLM,
                LocalLLMModelPath = source.LocalLLMModelPath,
                LLMContextSize = source.LLMContextSize,
                LLMSeed = source.LLMSeed,
                LLMTemperature = source.LLMTemperature,
                LLMMaxOutputTokens = source.LLMMaxOutputTokens,
                LLMAntiPrompts = source.LLMAntiPrompts != null ? new List<string>(source.LLMAntiPrompts) : new List<string>(),
                LLMPromptTemplate = source.LLMPromptTemplate,
                LLMSystemPrompt = source.LLMSystemPrompt,
                LLMUserPrompt = source.LLMUserPrompt,
                UseGpu = source.UseGpu,
                NormalMaxChunkDurationSeconds = source.NormalMaxChunkDurationSeconds,
                NormalSilenceThresholdSeconds = source.NormalSilenceThresholdSeconds,
                DictationMaxChunkDurationSeconds = source.DictationMaxChunkDurationSeconds,
                DictationSilenceThresholdSeconds = source.DictationSilenceThresholdSeconds,
                VadGainMultiplier = source.VadGainMultiplier,
                MaintainContextAcrossChunks = source.MaintainContextAcrossChunks,
                DictationHotkeyModifiers = source.DictationHotkeyModifiers,
                DictationHotkeyKey = source.DictationHotkeyKey,
                ProofreadHotkeyModifiers = source.ProofreadHotkeyModifiers,
                ProofreadHotkeyKey = source.ProofreadHotkeyKey,
                ActivePromptProfileName = source.ActivePromptProfileName,
                PromptProfiles = source.PromptProfiles != null
                    ? source.PromptProfiles.Select(p => new PromptProfile
                        {
                        Name = p.Name,
                        SystemPrompt = p.SystemPrompt,
                        UserPrompt = p.UserPrompt
                        }).ToList()
                    : new List<PromptProfile>()
                };
            }

        private void PopulateMicrophones(List<(int Index, string Name)> availableMics)
            {
            cmbMicrophone.Items.Clear();
            if (availableMics == null || availableMics.Count == 0)
                return;

            int selectIndex = 0;
            for (int i = 0; i < availableMics.Count; i++)
                {
                var mic = availableMics[i];
                var item = new MicItem { Index = mic.Index, Name = mic.Name };
                cmbMicrophone.Items.Add(item);

                if (mic.Index == _clonedSettings.SelectedMicrophoneDevice)
                    {
                    selectIndex = i;
                    }
                }

            if (cmbMicrophone.Items.Count > 0)
                {
                cmbMicrophone.SelectedIndex = selectIndex;
                }
            }

        private void PopulateVadModes()
            {
            cmbVadMode.Items.Clear();
            var modes = new[]
            {
                new VadModeItem { ModeValue = 0, DisplayName = "Low (0)" },
                new VadModeItem { ModeValue = 1, DisplayName = "Medium (1)" },
                new VadModeItem { ModeValue = 2, DisplayName = "High (2)" },
                new VadModeItem { ModeValue = 3, DisplayName = "Max (3)" }
            };

            int selectIndex = 3; // Default to Max
            for (int i = 0; i < modes.Length; i++)
                {
                cmbVadMode.Items.Add(modes[i]);
                if (modes[i].ModeValue == _clonedSettings.VadMode)
                    {
                    selectIndex = i;
                    }
                }

            cmbVadMode.SelectedIndex = selectIndex;
            }

        private void PopulatePromptProfiles()
            {
            cmbPromptProfile.Items.Clear();
            if (_clonedSettings.PromptProfiles == null || _clonedSettings.PromptProfiles.Count == 0)
                return;

            int selectIndex = 0;
            for (int i = 0; i < _clonedSettings.PromptProfiles.Count; i++)
                {
                var profile = _clonedSettings.PromptProfiles[i];
                cmbPromptProfile.Items.Add(profile);

                if (profile.Name == _clonedSettings.ActivePromptProfileName)
                    {
                    selectIndex = i;
                    }
                }

            if (cmbPromptProfile.Items.Count > 0)
                {
                cmbPromptProfile.SelectedIndex = selectIndex;
                }
            }

        private void ToggleLlmControls(bool enabled)
            {
            lblPromptProfile.Enabled = enabled;
            cmbPromptProfile.Enabled = enabled;
            lblSystemPrompt.Enabled = enabled;
            txtSystemPrompt.Enabled = enabled;
            lblUserPrompt.Enabled = enabled;
            txtUserPrompt.Enabled = enabled;
            }

        private void btnBrowseWhisperModel_Click(object sender, EventArgs e)
            {
            using (OpenFileDialog ofd = new OpenFileDialog())
                {
                ofd.Filter = "Whisper Model Files (*.bin;*.gguf)|*.bin;*.gguf|All files (*.*)|*.*";
                ofd.Title = "Select Whisper Model File";
                SetInitialDirectoryAndFile(ofd, txtWhisperModelPath.Text);

                if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                    txtWhisperModelPath.Text = ofd.FileName;
                    }
                }
            }

        private void btnBrowseLLMModel_Click(object sender, EventArgs e)
            {
            using (OpenFileDialog ofd = new OpenFileDialog())
                {
                ofd.Filter = "LLM Model Files (*.gguf)|*.gguf|All files (*.*)|*.*";
                ofd.Title = "Select LLM Model File";
                SetInitialDirectoryAndFile(ofd, txtLLMModelPath.Text);

                if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                    txtLLMModelPath.Text = ofd.FileName;
                    }
                }
            }

        private void SetInitialDirectoryAndFile(OpenFileDialog ofd, string currentPath)
            {
            try
                {
                if (!string.IsNullOrWhiteSpace(currentPath) && File.Exists(currentPath))
                    {
                    ofd.InitialDirectory = Path.GetDirectoryName(currentPath);
                    ofd.FileName = Path.GetFileName(currentPath);
                    }
                else if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath))
                    {
                    ofd.InitialDirectory = currentPath;
                    }
                else
                    {
                    ofd.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath);
                    }
                }
            catch
                {
                ofd.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath);
                }
            }

        private void chkProcessWithLlm_CheckedChanged(object sender, EventArgs e)
            {
            bool isChecked = chkProcessWithLlm.Checked;
            ToggleLlmControls(isChecked);
            if (!_loadingForm)
                {
                _clonedSettings.ProcessWithLLM = isChecked;
                }
            }

        private void cmbPromptProfile_SelectedIndexChanged(object sender, EventArgs e)
            {
            if (_loadingForm)
                return;

            if (cmbPromptProfile.SelectedItem is PromptProfile selectedProfile)
                {
                _loadingForm = true; // Temporary disable events to avoid circular triggers
                txtSystemPrompt.Text = selectedProfile.SystemPrompt;
                txtUserPrompt.Text = selectedProfile.UserPrompt;
                _clonedSettings.ActivePromptProfileName = selectedProfile.Name;
                _clonedSettings.LLMSystemPrompt = selectedProfile.SystemPrompt;
                _clonedSettings.LLMUserPrompt = selectedProfile.UserPrompt;
                _loadingForm = false;
                }
            }

        private void txtSystemPrompt_TextChanged(object sender, EventArgs e)
            {
            if (_loadingForm)
                return;

            _clonedSettings.LLMSystemPrompt = txtSystemPrompt.Text;

            // Save the customized prompt back to the currently active profile inside cloned settings
            var activeProfile = _clonedSettings.PromptProfiles.FirstOrDefault(p => p.Name == _clonedSettings.ActivePromptProfileName);
            if (activeProfile != null)
                {
                activeProfile.SystemPrompt = txtSystemPrompt.Text;
                }
            }

        private void txtUserPrompt_TextChanged(object sender, EventArgs e)
            {
            if (_loadingForm)
                return;

            _clonedSettings.LLMUserPrompt = txtUserPrompt.Text;

            // Save the customized prompt back to the currently active profile inside cloned settings
            var activeProfile = _clonedSettings.PromptProfiles.FirstOrDefault(p => p.Name == _clonedSettings.ActivePromptProfileName);
            if (activeProfile != null)
                {
                activeProfile.UserPrompt = txtUserPrompt.Text;
                }
            }

        private void btnOK_Click(object sender, EventArgs e)
            {
            // Validate paths before accepting
            string whisperPath = txtWhisperModelPath.Text.Trim();
            string llmPath = txtLLMModelPath.Text.Trim();

            if (string.IsNullOrWhiteSpace(whisperPath) || !File.Exists(whisperPath))
                {
                MessageBox.Show("Whisper model path is invalid or file does not exist.", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tabSettings.SelectedTab = tabModels;
                txtWhisperModelPath.Focus();
                return;
                }

            // LLM path is required only if ProcessWithLLM is checked
            if (chkProcessWithLlm.Checked && (string.IsNullOrWhiteSpace(llmPath) || !File.Exists(llmPath)))
                {
                MessageBox.Show("LLM model path is invalid or file does not exist. (Uncheck 'Process with local LLM' if you do not want to use an LLM).", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tabSettings.SelectedTab = tabModels;
                txtLLMModelPath.Focus();
                return;
                }

            // Validate hotkeys
            string dictationMods = txtDictationModifiers.Text.Trim();
            string dictationKey = txtDictationKey.Text.Trim();
            string proofreadMods = txtProofreadModifiers.Text.Trim();
            string proofreadKey = txtProofreadKey.Text.Trim();

            if (string.IsNullOrWhiteSpace(dictationMods) || string.IsNullOrWhiteSpace(dictationKey))
                {
                MessageBox.Show("Dictation hotkey modifiers and key cannot be empty.", "Invalid Hotkey", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tabSettings.SelectedTab = tabGeneral;
                txtDictationModifiers.Focus();
                return;
                }

            if (string.IsNullOrWhiteSpace(proofreadMods) || string.IsNullOrWhiteSpace(proofreadKey))
                {
                MessageBox.Show("Proofread hotkey modifiers and key cannot be empty.", "Invalid Hotkey", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tabSettings.SelectedTab = tabGeneral;
                txtProofreadModifiers.Focus();
                return;
                }

            // Apply all changes to our cloned settings
            _clonedSettings.ModelFilePath = whisperPath;
            _clonedSettings.LocalLLMModelPath = llmPath;
            _clonedSettings.UseGpu = chkUseGpu.Checked;

            if (cmbMicrophone.SelectedItem is MicItem selectedMic)
                {
                _clonedSettings.SelectedMicrophoneDevice = selectedMic.Index;
                }

            if (cmbVadMode.SelectedItem is VadModeItem selectedVad)
                {
                _clonedSettings.VadMode = selectedVad.ModeValue;
                }

            _clonedSettings.ShowDebugMessages = chkShowDebug.Checked;

            // Apply advanced settings
            _clonedSettings.LLMContextSize = (int)numLlmContextSize.Value;
            _clonedSettings.LLMSeed = (int)numLlmSeed.Value;
            _clonedSettings.LLMTemperature = (float)numLlmTemperature.Value;
            _clonedSettings.LLMMaxOutputTokens = (int)numLlmMaxTokens.Value;
            _clonedSettings.MaintainContextAcrossChunks = chkMaintainContext.Checked;

            _clonedSettings.VadGainMultiplier = (float)numVadGain.Value;
            _clonedSettings.NormalMaxChunkDurationSeconds = (double)numNormalMaxDuration.Value;
            _clonedSettings.NormalSilenceThresholdSeconds = (double)numNormalSilence.Value;
            _clonedSettings.DictationMaxChunkDurationSeconds = (double)numDictationMaxDuration.Value;
            _clonedSettings.DictationSilenceThresholdSeconds = (double)numDictationSilence.Value;

            _clonedSettings.ShowRealtimeTranscription = chkShowRealtime.Checked;
            _clonedSettings.DictationHotkeyModifiers = dictationMods;
            _clonedSettings.DictationHotkeyKey = dictationKey;
            _clonedSettings.ProofreadHotkeyModifiers = proofreadMods;
            _clonedSettings.ProofreadHotkeyKey = proofreadKey;

            // Set final result and close
            UpdatedSettings = _clonedSettings;
            this.DialogResult = DialogResult.OK;
            this.Close();
            }

        private void btnCancel_Click(object sender, EventArgs e)
            {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
            }

        private void chkUseGpu_CheckedChanged(object sender, EventArgs e)
            {

            }

        private void InitializeAdvancedControls()
            {
            // 1. Clear prompt anchors to avoid layout calculation conflicts
            txtSystemPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            txtUserPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            // 2. Resize Form and TabControl
            this.ClientSize = new Size(610, 560);
            tabSettings.Size = new Size(586, 486);

            // Set up grpLlmAdvanced (Tab 1: Models)
            grpLlmAdvanced = new GroupBox();
            grpLlmAdvanced.Text = "Advanced LLM Parameters";
            grpLlmAdvanced.Location = new Point(15, 200);
            grpLlmAdvanced.Size = new Size(550, 240);
            tabModels.Controls.Add(grpLlmAdvanced);

            lblLlmContextSize = new Label();
            lblLlmContextSize.Text = "Context Size (Tokens):";
            lblLlmContextSize.Location = new Point(15, 25);
            lblLlmContextSize.AutoSize = true;
            grpLlmAdvanced.Controls.Add(lblLlmContextSize);

            numLlmContextSize = new NumericUpDown();
            numLlmContextSize.Location = new Point(15, 45);
            numLlmContextSize.Size = new Size(160, 23);
            numLlmContextSize.Minimum = 512;
            numLlmContextSize.Maximum = 131072;
            numLlmContextSize.Increment = 1024;
            numLlmContextSize.Value = 16384;
            grpLlmAdvanced.Controls.Add(numLlmContextSize);

            lblLlmSeed = new Label();
            lblLlmSeed.Text = "Seed (0 for random):";
            lblLlmSeed.Location = new Point(200, 25);
            lblLlmSeed.AutoSize = true;
            grpLlmAdvanced.Controls.Add(lblLlmSeed);

            numLlmSeed = new NumericUpDown();
            numLlmSeed.Location = new Point(200, 45);
            numLlmSeed.Size = new Size(160, 23);
            numLlmSeed.Minimum = 0;
            numLlmSeed.Maximum = int.MaxValue;
            numLlmSeed.Increment = 1;
            numLlmSeed.Value = 0;
            grpLlmAdvanced.Controls.Add(numLlmSeed);

            lblLlmTemperature = new Label();
            lblLlmTemperature.Text = "Temperature:";
            lblLlmTemperature.Location = new Point(15, 85);
            lblLlmTemperature.AutoSize = true;
            grpLlmAdvanced.Controls.Add(lblLlmTemperature);

            numLlmTemperature = new NumericUpDown();
            numLlmTemperature.Location = new Point(15, 105);
            numLlmTemperature.Size = new Size(160, 23);
            numLlmTemperature.Minimum = 0.0M;
            numLlmTemperature.Maximum = 2.0M;
            numLlmTemperature.Increment = 0.05M;
            numLlmTemperature.DecimalPlaces = 2;
            numLlmTemperature.Value = 0.60M;
            grpLlmAdvanced.Controls.Add(numLlmTemperature);

            lblLlmMaxTokens = new Label();
            lblLlmMaxTokens.Text = "Max Output Tokens:";
            lblLlmMaxTokens.Location = new Point(200, 85);
            lblLlmMaxTokens.AutoSize = true;
            grpLlmAdvanced.Controls.Add(lblLlmMaxTokens);

            numLlmMaxTokens = new NumericUpDown();
            numLlmMaxTokens.Location = new Point(200, 105);
            numLlmMaxTokens.Size = new Size(160, 23);
            numLlmMaxTokens.Minimum = -1;
            numLlmMaxTokens.Maximum = 32768;
            numLlmMaxTokens.Increment = 128;
            numLlmMaxTokens.Value = -1;
            grpLlmAdvanced.Controls.Add(numLlmMaxTokens);

            chkMaintainContext = new CheckBox();
            chkMaintainContext.Text = "Maintain LLM Context Across Audio Chunks";
            chkMaintainContext.Location = new Point(15, 155);
            chkMaintainContext.Size = new Size(400, 19);
            chkMaintainContext.AutoSize = true;
            chkMaintainContext.Checked = true;
            grpLlmAdvanced.Controls.Add(chkMaintainContext);

            // Set up grpAudioChunking (Tab 2: Audio & VAD)
            grpAudioChunking = new GroupBox();
            grpAudioChunking.Text = "Advanced Audio & VAD Settings";
            grpAudioChunking.Location = new Point(15, 150);
            grpAudioChunking.Size = new Size(550, 290);
            tabAudioVad.Controls.Add(grpAudioChunking);

            lblVadGain = new Label();
            lblVadGain.Text = "VAD / Microphone Software Gain Multiplier:";
            lblVadGain.Location = new Point(15, 25);
            lblVadGain.AutoSize = true;
            grpAudioChunking.Controls.Add(lblVadGain);

            numVadGain = new NumericUpDown();
            numVadGain.Location = new Point(320, 22);
            numVadGain.Size = new Size(100, 23);
            numVadGain.Minimum = 0.1M;
            numVadGain.Maximum = 10.0M;
            numVadGain.Increment = 0.1M;
            numVadGain.DecimalPlaces = 1;
            numVadGain.Value = 1.0M;
            grpAudioChunking.Controls.Add(numVadGain);

            grpNormalMode = new GroupBox();
            grpNormalMode.Text = "Normal & Proofreading Mode Chunking";
            grpNormalMode.Location = new Point(15, 60);
            grpNormalMode.Size = new Size(250, 130);
            grpAudioChunking.Controls.Add(grpNormalMode);

            lblNormalMaxDuration = new Label();
            lblNormalMaxDuration.Text = "Max Duration (sec):";
            lblNormalMaxDuration.Location = new Point(15, 25);
            lblNormalMaxDuration.AutoSize = true;
            grpNormalMode.Controls.Add(lblNormalMaxDuration);

            numNormalMaxDuration = new NumericUpDown();
            numNormalMaxDuration.Location = new Point(15, 45);
            numNormalMaxDuration.Size = new Size(120, 23);
            numNormalMaxDuration.Minimum = 1.0M;
            numNormalMaxDuration.Maximum = 60.0M;
            numNormalMaxDuration.Increment = 0.5M;
            numNormalMaxDuration.DecimalPlaces = 1;
            numNormalMaxDuration.Value = 6.0M;
            grpNormalMode.Controls.Add(numNormalMaxDuration);

            lblNormalSilence = new Label();
            lblNormalSilence.Text = "Silence Threshold (sec):";
            lblNormalSilence.Location = new Point(15, 75);
            lblNormalSilence.AutoSize = true;
            grpNormalMode.Controls.Add(lblNormalSilence);

            numNormalSilence = new NumericUpDown();
            numNormalSilence.Location = new Point(15, 95);
            numNormalSilence.Size = new Size(120, 23);
            numNormalSilence.Minimum = 0.1M;
            numNormalSilence.Maximum = 10.0M;
            numNormalSilence.Increment = 0.1M;
            numNormalSilence.DecimalPlaces = 2;
            numNormalSilence.Value = 1.5M;
            grpNormalMode.Controls.Add(numNormalSilence);

            grpDictationMode = new GroupBox();
            grpDictationMode.Text = "Global Dictation Mode Chunking";
            grpDictationMode.Location = new Point(285, 60);
            grpDictationMode.Size = new Size(250, 130);
            grpAudioChunking.Controls.Add(grpDictationMode);

            lblDictationMaxDuration = new Label();
            lblDictationMaxDuration.Text = "Max Duration (sec):";
            lblDictationMaxDuration.Location = new Point(15, 25);
            lblDictationMaxDuration.AutoSize = true;
            grpDictationMode.Controls.Add(lblDictationMaxDuration);

            numDictationMaxDuration = new NumericUpDown();
            numDictationMaxDuration.Location = new Point(15, 45);
            numDictationMaxDuration.Size = new Size(120, 23);
            numDictationMaxDuration.Minimum = 1.0M;
            numDictationMaxDuration.Maximum = 60.0M;
            numDictationMaxDuration.Increment = 0.5M;
            numDictationMaxDuration.DecimalPlaces = 1;
            numDictationMaxDuration.Value = 3.0M;
            grpDictationMode.Controls.Add(numDictationMaxDuration);

            lblDictationSilence = new Label();
            lblDictationSilence.Text = "Silence Threshold (sec):";
            lblDictationSilence.Location = new Point(15, 75);
            lblDictationSilence.AutoSize = true;
            grpDictationMode.Controls.Add(lblDictationSilence);

            numDictationSilence = new NumericUpDown();
            numDictationSilence.Location = new Point(15, 95);
            numDictationSilence.Size = new Size(120, 23);
            numDictationSilence.Minimum = 0.1M;
            numDictationSilence.Maximum = 10.0M;
            numDictationSilence.Increment = 0.1M;
            numDictationSilence.DecimalPlaces = 2;
            numDictationSilence.Value = 0.75M;
            grpDictationMode.Controls.Add(numDictationSilence);

            // Re-parent VAD explanation text label into the new GroupBox for visual cohesion
            tabAudioVad.Controls.Remove(lblVadExplanation);
            grpAudioChunking.Controls.Add(lblVadExplanation);
            lblVadExplanation.Location = new Point(15, 205);
            lblVadExplanation.Size = new Size(520, 75);

            // Tab 3: Enlarge and reposition System / User prompts multiline textboxes
            txtSystemPrompt.Location = new Point(15, 95);
            txtSystemPrompt.Size = new Size(520, 140);

            lblUserPrompt.Location = new Point(15, 245);

            txtUserPrompt.Location = new Point(15, 265);
            txtUserPrompt.Size = new Size(520, 170);

            // Restore proper anchors for prompt textboxes after resizing
            txtSystemPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtUserPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Tab 4: General tab
            chkShowRealtime = new CheckBox();
            chkShowRealtime.Text = "Show Real-time Word/Segment Transcription (Whisper)";
            chkShowRealtime.Location = new Point(15, 50);
            chkShowRealtime.Size = new Size(400, 19);
            chkShowRealtime.AutoSize = true;
            chkShowRealtime.Checked = true;
            tabGeneral.Controls.Add(chkShowRealtime);

            grpHotkeys = new GroupBox();
            grpHotkeys.Text = "Global Keyboard Hotkey Overrides";
            grpHotkeys.Location = new Point(15, 90);
            grpHotkeys.Size = new Size(550, 240);
            tabGeneral.Controls.Add(grpHotkeys);

            lblDictationMods = new Label();
            lblDictationMods.Text = "Dictation Modifiers (e.g., Control, Alt):";
            lblDictationMods.Location = new Point(15, 25);
            lblDictationMods.AutoSize = true;
            grpHotkeys.Controls.Add(lblDictationMods);

            txtDictationModifiers = new TextBox();
            txtDictationModifiers.Location = new Point(15, 45);
            txtDictationModifiers.Size = new Size(200, 23);
            grpHotkeys.Controls.Add(txtDictationModifiers);

            lblDictationKey = new Label();
            lblDictationKey.Text = "Key (e.g., D, Space):";
            lblDictationKey.Location = new Point(240, 25);
            lblDictationKey.AutoSize = true;
            grpHotkeys.Controls.Add(lblDictationKey);

            txtDictationKey = new TextBox();
            txtDictationKey.Location = new Point(240, 45);
            txtDictationKey.Size = new Size(100, 23);
            grpHotkeys.Controls.Add(txtDictationKey);

            lblProofreadMods = new Label();
            lblProofreadMods.Text = "Proofread Modifiers (e.g., Control, Alt):";
            lblProofreadMods.Location = new Point(15, 85);
            lblProofreadMods.AutoSize = true;
            grpHotkeys.Controls.Add(lblProofreadMods);

            txtProofreadModifiers = new TextBox();
            txtProofreadModifiers.Location = new Point(15, 105);
            txtProofreadModifiers.Size = new Size(200, 23);
            grpHotkeys.Controls.Add(txtProofreadModifiers);

            lblProofreadKey = new Label();
            lblProofreadKey.Text = "Key (e.g., P, Space):";
            lblProofreadKey.Location = new Point(240, 85);
            lblProofreadKey.AutoSize = true;
            grpHotkeys.Controls.Add(lblProofreadKey);

            txtProofreadKey = new TextBox();
            txtProofreadKey.Location = new Point(240, 105);
            txtProofreadKey.Size = new Size(100, 23);
            grpHotkeys.Controls.Add(txtProofreadKey);

            lblHotkeyNote = new Label();
            lblHotkeyNote.Text = "⚠️ Modifiers must be comma-separated combinations of Control, Alt, Shift, or Windows. Key is a letter, number, or function key. Changes require an application restart to take effect on system hooks.";
            lblHotkeyNote.ForeColor = Color.DarkRed;
            lblHotkeyNote.Font = new Font(this.Font, FontStyle.Italic);
            lblHotkeyNote.Location = new Point(15, 145);
            lblHotkeyNote.Size = new Size(520, 80);
            grpHotkeys.Controls.Add(lblHotkeyNote);
            }
        }
    }
