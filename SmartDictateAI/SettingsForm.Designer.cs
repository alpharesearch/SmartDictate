namespace SmartDictateAI
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
            {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            tabSettings = new TabControl();
            tabModels = new TabPage();
            chkUseGpu = new CheckBox();
            btnBrowseLLMModel = new Button();
            txtLLMModelPath = new TextBox();
            lblLlmPath = new Label();
            btnBrowseWhisperModel = new Button();
            txtWhisperModelPath = new TextBox();
            lblWhisperPath = new Label();
            tabAudioVad = new TabPage();
            lblVadExplanation = new Label();
            cmbVadMode = new ComboBox();
            lblVadMode = new Label();
            cmbMicrophone = new ComboBox();
            lblMicrophone = new Label();
            tabLlmPrompts = new TabPage();
            txtUserPrompt = new TextBox();
            lblUserPrompt = new Label();
            txtSystemPrompt = new TextBox();
            lblSystemPrompt = new Label();
            cmbPromptProfile = new ComboBox();
            lblPromptProfile = new Label();
            chkProcessWithLlm = new CheckBox();
            tabGeneral = new TabPage();
            chkShowDebug = new CheckBox();
            btnOK = new Button();
            btnCancel = new Button();
            tabSettings.SuspendLayout();
            tabModels.SuspendLayout();
            tabAudioVad.SuspendLayout();
            tabLlmPrompts.SuspendLayout();
            tabGeneral.SuspendLayout();
            SuspendLayout();
            // 
            // tabSettings
            // 
            tabSettings.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabSettings.Controls.Add(tabModels);
            tabSettings.Controls.Add(tabAudioVad);
            tabSettings.Controls.Add(tabLlmPrompts);
            tabSettings.Controls.Add(tabGeneral);
            tabSettings.Location = new Point(12, 12);
            tabSettings.Name = "tabSettings";
            tabSettings.SelectedIndex = 0;
            tabSettings.Size = new Size(560, 330);
            tabSettings.TabIndex = 0;
            // 
            // tabModels
            // 
            tabModels.Controls.Add(chkUseGpu);
            tabModels.Controls.Add(btnBrowseLLMModel);
            tabModels.Controls.Add(txtLLMModelPath);
            tabModels.Controls.Add(lblLlmPath);
            tabModels.Controls.Add(btnBrowseWhisperModel);
            tabModels.Controls.Add(txtWhisperModelPath);
            tabModels.Controls.Add(lblWhisperPath);
            tabModels.Location = new Point(4, 24);
            tabModels.Name = "tabModels";
            tabModels.Padding = new Padding(3);
            tabModels.Size = new Size(552, 302);
            tabModels.TabIndex = 0;
            tabModels.Text = "📁 Models";
            tabModels.UseVisualStyleBackColor = true;
            // 
            // chkUseGpu
            // 
            chkUseGpu.AutoSize = true;
            chkUseGpu.Location = new Point(15, 165);
            chkUseGpu.Name = "chkUseGpu";
            chkUseGpu.Size = new Size(183, 19);
            chkUseGpu.TabIndex = 6;
            chkUseGpu.Text = "Use GPU Acceleration (CUDA)";
            chkUseGpu.UseVisualStyleBackColor = true;
            chkUseGpu.CheckedChanged += chkUseGpu_CheckedChanged;
            // 
            // btnBrowseLLMModel
            // 
            btnBrowseLLMModel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseLLMModel.Location = new Point(445, 108);
            btnBrowseLLMModel.Name = "btnBrowseLLMModel";
            btnBrowseLLMModel.Size = new Size(90, 27);
            btnBrowseLLMModel.TabIndex = 5;
            btnBrowseLLMModel.Text = "Browse...";
            btnBrowseLLMModel.UseVisualStyleBackColor = true;
            btnBrowseLLMModel.Click += btnBrowseLLMModel_Click;
            // 
            // txtLLMModelPath
            // 
            txtLLMModelPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtLLMModelPath.Location = new Point(15, 110);
            txtLLMModelPath.Name = "txtLLMModelPath";
            txtLLMModelPath.Size = new Size(420, 23);
            txtLLMModelPath.TabIndex = 4;
            // 
            // lblLlmPath
            // 
            lblLlmPath.AutoSize = true;
            lblLlmPath.Location = new Point(15, 90);
            lblLlmPath.Name = "lblLlmPath";
            lblLlmPath.Size = new Size(142, 15);
            lblLlmPath.TabIndex = 3;
            lblLlmPath.Text = "Local LLM Model (GGUF):";
            // 
            // btnBrowseWhisperModel
            // 
            btnBrowseWhisperModel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseWhisperModel.Location = new Point(445, 38);
            btnBrowseWhisperModel.Name = "btnBrowseWhisperModel";
            btnBrowseWhisperModel.Size = new Size(90, 27);
            btnBrowseWhisperModel.TabIndex = 2;
            btnBrowseWhisperModel.Text = "Browse...";
            btnBrowseWhisperModel.UseVisualStyleBackColor = true;
            btnBrowseWhisperModel.Click += btnBrowseWhisperModel_Click;
            // 
            // txtWhisperModelPath
            // 
            txtWhisperModelPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtWhisperModelPath.Location = new Point(15, 40);
            txtWhisperModelPath.Name = "txtWhisperModelPath";
            txtWhisperModelPath.Size = new Size(420, 23);
            txtWhisperModelPath.TabIndex = 1;
            // 
            // lblWhisperPath
            // 
            lblWhisperPath.AutoSize = true;
            lblWhisperPath.Location = new Point(15, 20);
            lblWhisperPath.Name = "lblWhisperPath";
            lblWhisperPath.Size = new Size(161, 15);
            lblWhisperPath.TabIndex = 0;
            lblWhisperPath.Text = "Whisper Model Path (GGML):";
            // 
            // tabAudioVad
            // 
            tabAudioVad.Controls.Add(lblVadExplanation);
            tabAudioVad.Controls.Add(cmbVadMode);
            tabAudioVad.Controls.Add(lblVadMode);
            tabAudioVad.Controls.Add(cmbMicrophone);
            tabAudioVad.Controls.Add(lblMicrophone);
            tabAudioVad.Location = new Point(4, 24);
            tabAudioVad.Name = "tabAudioVad";
            tabAudioVad.Padding = new Padding(3);
            tabAudioVad.Size = new Size(552, 302);
            tabAudioVad.TabIndex = 1;
            tabAudioVad.Text = "🎤 Audio & VAD";
            tabAudioVad.UseVisualStyleBackColor = true;
            // 
            // lblVadExplanation
            // 
            lblVadExplanation.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblVadExplanation.ForeColor = Color.DimGray;
            lblVadExplanation.Location = new Point(15, 155);
            lblVadExplanation.Name = "lblVadExplanation";
            lblVadExplanation.Size = new Size(520, 60);
            lblVadExplanation.TabIndex = 4;
            lblVadExplanation.Text = resources.GetString("lblVadExplanation.Text");
            // 
            // cmbVadMode
            // 
            cmbVadMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVadMode.FormattingEnabled = true;
            cmbVadMode.Location = new Point(15, 110);
            cmbVadMode.Name = "cmbVadMode";
            cmbVadMode.Size = new Size(200, 23);
            cmbVadMode.TabIndex = 3;
            // 
            // lblVadMode
            // 
            lblVadMode.AutoSize = true;
            lblVadMode.Location = new Point(15, 90);
            lblVadMode.Name = "lblVadMode";
            lblVadMode.Size = new Size(122, 15);
            lblVadMode.TabIndex = 2;
            lblVadMode.Text = "VAD Sensitivity Mode:";
            // 
            // cmbMicrophone
            // 
            cmbMicrophone.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbMicrophone.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMicrophone.FormattingEnabled = true;
            cmbMicrophone.Location = new Point(15, 40);
            cmbMicrophone.Name = "cmbMicrophone";
            cmbMicrophone.Size = new Size(520, 23);
            cmbMicrophone.TabIndex = 1;
            // 
            // lblMicrophone
            // 
            lblMicrophone.AutoSize = true;
            lblMicrophone.Location = new Point(15, 20);
            lblMicrophone.Name = "lblMicrophone";
            lblMicrophone.Size = new Size(106, 15);
            lblMicrophone.TabIndex = 0;
            lblMicrophone.Text = "Input Microphone:";
            // 
            // tabLlmPrompts
            // 
            tabLlmPrompts.Controls.Add(txtUserPrompt);
            tabLlmPrompts.Controls.Add(lblUserPrompt);
            tabLlmPrompts.Controls.Add(txtSystemPrompt);
            tabLlmPrompts.Controls.Add(lblSystemPrompt);
            tabLlmPrompts.Controls.Add(cmbPromptProfile);
            tabLlmPrompts.Controls.Add(lblPromptProfile);
            tabLlmPrompts.Controls.Add(chkProcessWithLlm);
            tabLlmPrompts.Location = new Point(4, 24);
            tabLlmPrompts.Name = "tabLlmPrompts";
            tabLlmPrompts.Padding = new Padding(3);
            tabLlmPrompts.Size = new Size(552, 302);
            tabLlmPrompts.TabIndex = 2;
            tabLlmPrompts.Text = "\U0001f9e0 LLM & Prompts";
            tabLlmPrompts.UseVisualStyleBackColor = true;
            // 
            // txtUserPrompt
            // 
            txtUserPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtUserPrompt.Location = new Point(15, 205);
            txtUserPrompt.Multiline = true;
            txtUserPrompt.Name = "txtUserPrompt";
            txtUserPrompt.ScrollBars = ScrollBars.Vertical;
            txtUserPrompt.Size = new Size(520, 80);
            txtUserPrompt.TabIndex = 6;
            txtUserPrompt.TextChanged += txtUserPrompt_TextChanged;
            // 
            // lblUserPrompt
            // 
            lblUserPrompt.AutoSize = true;
            lblUserPrompt.Location = new Point(15, 185);
            lblUserPrompt.Name = "lblUserPrompt";
            lblUserPrompt.Size = new Size(76, 15);
            lblUserPrompt.TabIndex = 5;
            lblUserPrompt.Text = "User Prompt:";
            // 
            // txtSystemPrompt
            // 
            txtSystemPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSystemPrompt.Location = new Point(15, 95);
            txtSystemPrompt.Multiline = true;
            txtSystemPrompt.Name = "txtSystemPrompt";
            txtSystemPrompt.ScrollBars = ScrollBars.Vertical;
            txtSystemPrompt.Size = new Size(520, 80);
            txtSystemPrompt.TabIndex = 4;
            txtSystemPrompt.TextChanged += txtSystemPrompt_TextChanged;
            // 
            // lblSystemPrompt
            // 
            lblSystemPrompt.AutoSize = true;
            lblSystemPrompt.Location = new Point(15, 75);
            lblSystemPrompt.Name = "lblSystemPrompt";
            lblSystemPrompt.Size = new Size(91, 15);
            lblSystemPrompt.TabIndex = 3;
            lblSystemPrompt.Text = "System Prompt:";
            // 
            // cmbPromptProfile
            // 
            cmbPromptProfile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbPromptProfile.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPromptProfile.FormattingEnabled = true;
            cmbPromptProfile.Location = new Point(120, 42);
            cmbPromptProfile.Name = "cmbPromptProfile";
            cmbPromptProfile.Size = new Size(415, 23);
            cmbPromptProfile.TabIndex = 2;
            cmbPromptProfile.SelectedIndexChanged += cmbPromptProfile_SelectedIndexChanged;
            // 
            // lblPromptProfile
            // 
            lblPromptProfile.AutoSize = true;
            lblPromptProfile.Location = new Point(15, 45);
            lblPromptProfile.Name = "lblPromptProfile";
            lblPromptProfile.Size = new Size(87, 15);
            lblPromptProfile.TabIndex = 1;
            lblPromptProfile.Text = "Prompt Profile:";
            // 
            // chkProcessWithLlm
            // 
            chkProcessWithLlm.AutoSize = true;
            chkProcessWithLlm.Location = new Point(15, 15);
            chkProcessWithLlm.Name = "chkProcessWithLlm";
            chkProcessWithLlm.Size = new Size(197, 19);
            chkProcessWithLlm.TabIndex = 0;
            chkProcessWithLlm.Text = "Process Dictation with local LLM";
            chkProcessWithLlm.UseVisualStyleBackColor = true;
            chkProcessWithLlm.CheckedChanged += chkProcessWithLlm_CheckedChanged;
            // 
            // tabGeneral
            // 
            tabGeneral.Controls.Add(chkShowDebug);
            tabGeneral.Location = new Point(4, 24);
            tabGeneral.Name = "tabGeneral";
            tabGeneral.Padding = new Padding(3);
            tabGeneral.Size = new Size(552, 302);
            tabGeneral.TabIndex = 3;
            tabGeneral.Text = "⚙️ General";
            tabGeneral.UseVisualStyleBackColor = true;
            // 
            // chkShowDebug
            // 
            chkShowDebug.AutoSize = true;
            chkShowDebug.Location = new Point(15, 20);
            chkShowDebug.Name = "chkShowDebug";
            chkShowDebug.Size = new Size(210, 19);
            chkShowDebug.TabIndex = 0;
            chkShowDebug.Text = "Show Debug Terminal & System Info";
            chkShowDebug.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.Location = new Point(390, 360);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(90, 32);
            btnOK.TabIndex = 1;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(488, 360);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 32);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // SettingsForm
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(590, 404);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(tabSettings);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            tabSettings.ResumeLayout(false);
            tabModels.ResumeLayout(false);
            tabModels.PerformLayout();
            tabAudioVad.ResumeLayout(false);
            tabAudioVad.PerformLayout();
            tabLlmPrompts.ResumeLayout(false);
            tabLlmPrompts.PerformLayout();
            tabGeneral.ResumeLayout(false);
            tabGeneral.PerformLayout();
            ResumeLayout(false);
            }

        #endregion

        private System.Windows.Forms.TabControl tabSettings;
        private System.Windows.Forms.TabPage tabModels;
        private System.Windows.Forms.TabPage tabAudioVad;
        private System.Windows.Forms.TabPage tabLlmPrompts;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.Label lblWhisperPath;
        private System.Windows.Forms.TextBox txtWhisperModelPath;
        private System.Windows.Forms.Button btnBrowseWhisperModel;
        private System.Windows.Forms.Label lblLlmPath;
        private System.Windows.Forms.TextBox txtLLMModelPath;
        private System.Windows.Forms.Button btnBrowseLLMModel;
        private System.Windows.Forms.CheckBox chkUseGpu;
        private System.Windows.Forms.Label lblMicrophone;
        private System.Windows.Forms.ComboBox cmbMicrophone;
        private System.Windows.Forms.Label lblVadMode;
        private System.Windows.Forms.ComboBox cmbVadMode;
        private System.Windows.Forms.Label lblVadExplanation;
        private System.Windows.Forms.CheckBox chkProcessWithLlm;
        private System.Windows.Forms.Label lblPromptProfile;
        private System.Windows.Forms.ComboBox cmbPromptProfile;
        private System.Windows.Forms.Label lblSystemPrompt;
        private System.Windows.Forms.TextBox txtSystemPrompt;
        private System.Windows.Forms.Label lblUserPrompt;
        private System.Windows.Forms.TextBox txtUserPrompt;
        private System.Windows.Forms.CheckBox chkShowDebug;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}
