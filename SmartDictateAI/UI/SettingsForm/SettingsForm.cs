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
            txtCustomVocabulary.Text = _clonedSettings.CustomVocabulary;



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

            chkEnableVocabPrompt1.Checked = _clonedSettings.EnableVocabPrompt1;
            chkEnableVocabPrompt2.Checked = _clonedSettings.EnableVocabPrompt2;
            txtVocabPrompt1.Text = _clonedSettings.VocabPrompt1Text;
            txtVocabPrompt2.Text = _clonedSettings.VocabPrompt2Text;
            PopulateReplacementsList();

            _loadingForm = false;
            }

        private AppSettings CloneSettings(AppSettings source)
            {
            var cloned = new AppSettings();
            cloned.CopyFrom(source);
            return cloned;
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
            _clonedSettings.CustomVocabulary = txtCustomVocabulary.Text.Trim();
            _clonedSettings.EnableVocabPrompt1 = chkEnableVocabPrompt1.Checked;
            _clonedSettings.EnableVocabPrompt2 = chkEnableVocabPrompt2.Checked;
            _clonedSettings.VocabPrompt1Text = txtVocabPrompt1.Text;
            _clonedSettings.VocabPrompt2Text = txtVocabPrompt2.Text;
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

        private void btnLoadDefaults_Click(object sender, EventArgs e)
            {
            if (MessageBox.Show(this, "Are you sure you want to load default settings? This will reset all your current configurations.", "Load Defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                var defaults = new AppSettings();
                defaults.EnsureDefaultPromptProfiles();
                defaults.EnsureDefaultLLMAntiPrompts();

                _clonedSettings = defaults;

                _loadingForm = true;

                // 1. Models Tab
                txtWhisperModelPath.Text = _clonedSettings.ModelFilePath;
                txtLLMModelPath.Text = _clonedSettings.LocalLLMModelPath;
                chkUseGpu.Checked = _clonedSettings.UseGpu;

                // 2. Audio & VAD Tab
                if (cmbMicrophone.Items.Count > 0)
                    {
                    int selectIndex = 0;
                    for (int i = 0; i < cmbMicrophone.Items.Count; i++)
                        {
                        if (cmbMicrophone.Items[i] is MicItem mic && mic.Index == _clonedSettings.SelectedMicrophoneDevice)
                            {
                            selectIndex = i;
                            break;
                            }
                        }
                    cmbMicrophone.SelectedIndex = selectIndex;
                    }

                int selectVadIndex = 3; // Default is Max (3)
                for (int i = 0; i < cmbVadMode.Items.Count; i++)
                    {
                    if (cmbVadMode.Items[i] is VadModeItem vad && vad.ModeValue == _clonedSettings.VadMode)
                        {
                        selectVadIndex = i;
                        break;
                        }
                    }
                cmbVadMode.SelectedIndex = selectVadIndex;

                // 3. LLM & Prompts Tab
                chkProcessWithLlm.Checked = _clonedSettings.ProcessWithLLM;
                PopulatePromptProfiles();
                txtSystemPrompt.Text = _clonedSettings.LLMSystemPrompt;
                txtUserPrompt.Text = _clonedSettings.LLMUserPrompt;

                ToggleLlmControls(chkProcessWithLlm.Checked);

                // 4. General Tab
                chkShowDebug.Checked = _clonedSettings.ShowDebugMessages;

                // Load values to advanced controls
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
                txtCustomVocabulary.Text = _clonedSettings.CustomVocabulary;
                chkEnableVocabPrompt1.Checked = _clonedSettings.EnableVocabPrompt1;
                chkEnableVocabPrompt2.Checked = _clonedSettings.EnableVocabPrompt2;
                txtVocabPrompt1.Text = _clonedSettings.VocabPrompt1Text;
                txtVocabPrompt2.Text = _clonedSettings.VocabPrompt2Text;
                PopulateReplacementsList();

                _loadingForm = false;
                }
            }

        private void llWhisper_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            {
            try
                {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                    FileName = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-turbo.bin?download=true",
                    UseShellExecute = true
                    });
                }
            catch (Exception ex)
                {
                MessageBox.Show($"Could not open the link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        private void llLLM_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            {
            try
                {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                    FileName = "https://huggingface.co/unsloth/gemma-4-E4B-it-GGUF/resolve/main/gemma-4-E4B-it-Q4_0.gguf?download=true",
                    UseShellExecute = true
                    });
                }
            catch (Exception ex)
                {
                MessageBox.Show($"Could not open the link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        private void PopulateReplacementsList()
        {
            lstReplacements.Items.Clear();
            if (_clonedSettings.VocabularyReplacements != null)
            {
                foreach (var rep in _clonedSettings.VocabularyReplacements)
                {
                    var item = new ListViewItem(rep.Target);
                    item.SubItems.Add(rep.Replacement);
                    item.Tag = rep;
                    lstReplacements.Items.Add(item);
                }
            }
            txtRepTarget.Clear();
            txtRepReplacement.Clear();
        }

        private void btnRepAdd_Click(object sender, EventArgs e)
        {
            string target = txtRepTarget.Text.Trim();
            string replacement = txtRepReplacement.Text.Trim();

            if (string.IsNullOrEmpty(target))
            {
                MessageBox.Show("Please enter a target phrase to replace.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_clonedSettings.VocabularyReplacements == null)
            {
                _clonedSettings.VocabularyReplacements = new List<VocabularyReplacement>();
            }

            // Check if it already exists to update it, or add new
            var existing = _clonedSettings.VocabularyReplacements.FirstOrDefault(r => r.Target.Equals(target, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Replacement = replacement;
            }
            else
            {
                _clonedSettings.VocabularyReplacements.Add(new VocabularyReplacement { Target = target, Replacement = replacement });
            }

            PopulateReplacementsList();
        }

        private void btnRepDelete_Click(object sender, EventArgs e)
        {
            if (lstReplacements.SelectedItems.Count > 0)
            {
                var selectedItem = lstReplacements.SelectedItems[0];
                if (selectedItem.Tag is VocabularyReplacement rep)
                {
                    _clonedSettings.VocabularyReplacements.Remove(rep);
                    PopulateReplacementsList();
                }
            }
            else
            {
                MessageBox.Show("Please select a replacement from the list to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void lstReplacements_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstReplacements.SelectedItems.Count > 0)
            {
                var selectedItem = lstReplacements.SelectedItems[0];
                if (selectedItem.Tag is VocabularyReplacement rep)
                {
                    txtRepTarget.Text = rep.Target;
                    txtRepReplacement.Text = rep.Replacement;
                }
            }
        }
        }
    }
