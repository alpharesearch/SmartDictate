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
            grpLlmAdvanced = new GroupBox();
            numLlmContextSize = new NumericUpDown();
            lblLlmContextSize = new Label();
            numLlmSeed = new NumericUpDown();
            lblLlmSeed = new Label();
            numLlmTemperature = new NumericUpDown();
            lblLlmTemperature = new Label();
            numLlmMaxTokens = new NumericUpDown();
            lblLlmMaxTokens = new Label();
            chkMaintainContext = new CheckBox();
            tabAudioVad = new TabPage();
            cmbVadMode = new ComboBox();
            lblVadMode = new Label();
            cmbMicrophone = new ComboBox();
            lblMicrophone = new Label();
            grpAudioChunking = new GroupBox();
            numVadGain = new NumericUpDown();
            lblVadGain = new Label();
            grpNormalMode = new GroupBox();
            numNormalMaxDuration = new NumericUpDown();
            lblNormalMaxDuration = new Label();
            numNormalSilence = new NumericUpDown();
            lblNormalSilence = new Label();
            grpDictationMode = new GroupBox();
            numDictationMaxDuration = new NumericUpDown();
            lblDictationMaxDuration = new Label();
            numDictationSilence = new NumericUpDown();
            lblDictationSilence = new Label();
            lblVadExplanation = new Label();
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
            chkShowRealtime = new CheckBox();
            grpHotkeys = new GroupBox();
            txtDictationModifiers = new TextBox();
            lblDictationMods = new Label();
            txtDictationKey = new TextBox();
            lblDictationKey = new Label();
            txtProofreadModifiers = new TextBox();
            lblProofreadMods = new Label();
            txtProofreadKey = new TextBox();
            lblProofreadKey = new Label();
            lblHotkeyNote = new Label();
            btnOK = new Button();
            btnCancel = new Button();
            llWhisper = new LinkLabel();
            llLLM = new LinkLabel();
            tabSettings.SuspendLayout();
            tabModels.SuspendLayout();
            grpLlmAdvanced.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numLlmContextSize).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numLlmSeed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numLlmTemperature).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numLlmMaxTokens).BeginInit();
            tabAudioVad.SuspendLayout();
            grpAudioChunking.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numVadGain).BeginInit();
            grpNormalMode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numNormalMaxDuration).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numNormalSilence).BeginInit();
            grpDictationMode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numDictationMaxDuration).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDictationSilence).BeginInit();
            tabLlmPrompts.SuspendLayout();
            tabGeneral.SuspendLayout();
            grpHotkeys.SuspendLayout();
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
            tabSettings.Size = new Size(586, 486);
            tabSettings.TabIndex = 0;
            // 
            // tabModels
            // 
            tabModels.Controls.Add(llLLM);
            tabModels.Controls.Add(llWhisper);
            tabModels.Controls.Add(chkUseGpu);
            tabModels.Controls.Add(btnBrowseLLMModel);
            tabModels.Controls.Add(txtLLMModelPath);
            tabModels.Controls.Add(lblLlmPath);
            tabModels.Controls.Add(btnBrowseWhisperModel);
            tabModels.Controls.Add(txtWhisperModelPath);
            tabModels.Controls.Add(lblWhisperPath);
            tabModels.Controls.Add(grpLlmAdvanced);
            tabModels.Location = new Point(4, 24);
            tabModels.Name = "tabModels";
            tabModels.Padding = new Padding(3);
            tabModels.Size = new Size(578, 458);
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
            btnBrowseLLMModel.Location = new Point(471, 108);
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
            txtLLMModelPath.Size = new Size(446, 23);
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
            btnBrowseWhisperModel.Location = new Point(471, 38);
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
            txtWhisperModelPath.Size = new Size(446, 23);
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
            // grpLlmAdvanced
            // 
            grpLlmAdvanced.Controls.Add(numLlmContextSize);
            grpLlmAdvanced.Controls.Add(lblLlmContextSize);
            grpLlmAdvanced.Controls.Add(numLlmSeed);
            grpLlmAdvanced.Controls.Add(lblLlmSeed);
            grpLlmAdvanced.Controls.Add(numLlmTemperature);
            grpLlmAdvanced.Controls.Add(lblLlmTemperature);
            grpLlmAdvanced.Controls.Add(numLlmMaxTokens);
            grpLlmAdvanced.Controls.Add(lblLlmMaxTokens);
            grpLlmAdvanced.Controls.Add(chkMaintainContext);
            grpLlmAdvanced.Location = new Point(15, 200);
            grpLlmAdvanced.Name = "grpLlmAdvanced";
            grpLlmAdvanced.Size = new Size(550, 240);
            grpLlmAdvanced.TabIndex = 7;
            grpLlmAdvanced.TabStop = false;
            grpLlmAdvanced.Text = "Advanced LLM Parameters";
            // 
            // numLlmContextSize
            // 
            numLlmContextSize.Increment = new decimal(new int[] { 1024, 0, 0, 0 });
            numLlmContextSize.Location = new Point(15, 45);
            numLlmContextSize.Maximum = new decimal(new int[] { 131072, 0, 0, 0 });
            numLlmContextSize.Minimum = new decimal(new int[] { 512, 0, 0, 0 });
            numLlmContextSize.Name = "numLlmContextSize";
            numLlmContextSize.Size = new Size(160, 23);
            numLlmContextSize.TabIndex = 1;
            numLlmContextSize.Value = new decimal(new int[] { 16384, 0, 0, 0 });
            // 
            // lblLlmContextSize
            // 
            lblLlmContextSize.AutoSize = true;
            lblLlmContextSize.Location = new Point(15, 25);
            lblLlmContextSize.Name = "lblLlmContextSize";
            lblLlmContextSize.Size = new Size(122, 15);
            lblLlmContextSize.TabIndex = 0;
            lblLlmContextSize.Text = "Context Size (Tokens):";
            // 
            // numLlmSeed
            // 
            numLlmSeed.Location = new Point(200, 45);
            numLlmSeed.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numLlmSeed.Name = "numLlmSeed";
            numLlmSeed.Size = new Size(160, 23);
            numLlmSeed.TabIndex = 3;
            // 
            // lblLlmSeed
            // 
            lblLlmSeed.AutoSize = true;
            lblLlmSeed.Location = new Point(200, 25);
            lblLlmSeed.Name = "lblLlmSeed";
            lblLlmSeed.Size = new Size(115, 15);
            lblLlmSeed.TabIndex = 2;
            lblLlmSeed.Text = "Seed (0 for random):";
            // 
            // numLlmTemperature
            // 
            numLlmTemperature.DecimalPlaces = 2;
            numLlmTemperature.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            numLlmTemperature.Location = new Point(15, 105);
            numLlmTemperature.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            numLlmTemperature.Name = "numLlmTemperature";
            numLlmTemperature.Size = new Size(160, 23);
            numLlmTemperature.TabIndex = 5;
            numLlmTemperature.Value = new decimal(new int[] { 60, 0, 0, 131072 });
            // 
            // lblLlmTemperature
            // 
            lblLlmTemperature.AutoSize = true;
            lblLlmTemperature.Location = new Point(15, 85);
            lblLlmTemperature.Name = "lblLlmTemperature";
            lblLlmTemperature.Size = new Size(77, 15);
            lblLlmTemperature.TabIndex = 4;
            lblLlmTemperature.Text = "Temperature:";
            // 
            // numLlmMaxTokens
            // 
            numLlmMaxTokens.Increment = new decimal(new int[] { 128, 0, 0, 0 });
            numLlmMaxTokens.Location = new Point(200, 105);
            numLlmMaxTokens.Maximum = new decimal(new int[] { 32768, 0, 0, 0 });
            numLlmMaxTokens.Minimum = new decimal(new int[] { 1, 0, 0, int.MinValue });
            numLlmMaxTokens.Name = "numLlmMaxTokens";
            numLlmMaxTokens.Size = new Size(160, 23);
            numLlmMaxTokens.TabIndex = 7;
            numLlmMaxTokens.Value = new decimal(new int[] { 1, 0, 0, int.MinValue });
            // 
            // lblLlmMaxTokens
            // 
            lblLlmMaxTokens.AutoSize = true;
            lblLlmMaxTokens.Location = new Point(200, 85);
            lblLlmMaxTokens.Name = "lblLlmMaxTokens";
            lblLlmMaxTokens.Size = new Size(113, 15);
            lblLlmMaxTokens.TabIndex = 6;
            lblLlmMaxTokens.Text = "Max Output Tokens:";
            // 
            // chkMaintainContext
            // 
            chkMaintainContext.AutoSize = true;
            chkMaintainContext.Checked = true;
            chkMaintainContext.CheckState = CheckState.Checked;
            chkMaintainContext.Location = new Point(15, 155);
            chkMaintainContext.Name = "chkMaintainContext";
            chkMaintainContext.Size = new Size(259, 19);
            chkMaintainContext.TabIndex = 8;
            chkMaintainContext.Text = "Maintain LLM Context Across Audio Chunks";
            chkMaintainContext.UseVisualStyleBackColor = true;
            // 
            // tabAudioVad
            // 
            tabAudioVad.Controls.Add(cmbVadMode);
            tabAudioVad.Controls.Add(lblVadMode);
            tabAudioVad.Controls.Add(cmbMicrophone);
            tabAudioVad.Controls.Add(lblMicrophone);
            tabAudioVad.Controls.Add(grpAudioChunking);
            tabAudioVad.Location = new Point(4, 24);
            tabAudioVad.Name = "tabAudioVad";
            tabAudioVad.Padding = new Padding(3);
            tabAudioVad.Size = new Size(578, 458);
            tabAudioVad.TabIndex = 1;
            tabAudioVad.Text = "🎤 Audio & VAD";
            tabAudioVad.UseVisualStyleBackColor = true;
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
            cmbMicrophone.Size = new Size(546, 23);
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
            // grpAudioChunking
            // 
            grpAudioChunking.Controls.Add(numVadGain);
            grpAudioChunking.Controls.Add(lblVadGain);
            grpAudioChunking.Controls.Add(grpNormalMode);
            grpAudioChunking.Controls.Add(grpDictationMode);
            grpAudioChunking.Controls.Add(lblVadExplanation);
            grpAudioChunking.Location = new Point(15, 150);
            grpAudioChunking.Name = "grpAudioChunking";
            grpAudioChunking.Size = new Size(550, 290);
            grpAudioChunking.TabIndex = 4;
            grpAudioChunking.TabStop = false;
            grpAudioChunking.Text = "Advanced Audio & VAD Settings";
            // 
            // numVadGain
            // 
            numVadGain.DecimalPlaces = 1;
            numVadGain.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numVadGain.Location = new Point(320, 22);
            numVadGain.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numVadGain.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            numVadGain.Name = "numVadGain";
            numVadGain.Size = new Size(100, 23);
            numVadGain.TabIndex = 1;
            numVadGain.Value = new decimal(new int[] { 10, 0, 0, 65536 });
            // 
            // lblVadGain
            // 
            lblVadGain.AutoSize = true;
            lblVadGain.Location = new Point(15, 25);
            lblVadGain.Name = "lblVadGain";
            lblVadGain.Size = new Size(238, 15);
            lblVadGain.TabIndex = 0;
            lblVadGain.Text = "VAD / Microphone Software Gain Multiplier:";
            // 
            // grpNormalMode
            // 
            grpNormalMode.Controls.Add(numNormalMaxDuration);
            grpNormalMode.Controls.Add(lblNormalMaxDuration);
            grpNormalMode.Controls.Add(numNormalSilence);
            grpNormalMode.Controls.Add(lblNormalSilence);
            grpNormalMode.Location = new Point(15, 60);
            grpNormalMode.Name = "grpNormalMode";
            grpNormalMode.Size = new Size(250, 130);
            grpNormalMode.TabIndex = 2;
            grpNormalMode.TabStop = false;
            grpNormalMode.Text = "Normal & Proofreading Mode Chunking";
            // 
            // numNormalMaxDuration
            // 
            numNormalMaxDuration.DecimalPlaces = 1;
            numNormalMaxDuration.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            numNormalMaxDuration.Location = new Point(15, 45);
            numNormalMaxDuration.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            numNormalMaxDuration.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numNormalMaxDuration.Name = "numNormalMaxDuration";
            numNormalMaxDuration.Size = new Size(120, 23);
            numNormalMaxDuration.TabIndex = 1;
            numNormalMaxDuration.Value = new decimal(new int[] { 60, 0, 0, 65536 });
            // 
            // lblNormalMaxDuration
            // 
            lblNormalMaxDuration.AutoSize = true;
            lblNormalMaxDuration.Location = new Point(15, 25);
            lblNormalMaxDuration.Name = "lblNormalMaxDuration";
            lblNormalMaxDuration.Size = new Size(109, 15);
            lblNormalMaxDuration.TabIndex = 0;
            lblNormalMaxDuration.Text = "Max Duration (sec):";
            // 
            // numNormalSilence
            // 
            numNormalSilence.DecimalPlaces = 2;
            numNormalSilence.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numNormalSilence.Location = new Point(15, 95);
            numNormalSilence.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numNormalSilence.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            numNormalSilence.Name = "numNormalSilence";
            numNormalSilence.Size = new Size(120, 23);
            numNormalSilence.TabIndex = 3;
            numNormalSilence.Value = new decimal(new int[] { 15, 0, 0, 65536 });
            // 
            // lblNormalSilence
            // 
            lblNormalSilence.AutoSize = true;
            lblNormalSilence.Location = new Point(15, 75);
            lblNormalSilence.Name = "lblNormalSilence";
            lblNormalSilence.Size = new Size(131, 15);
            lblNormalSilence.TabIndex = 2;
            lblNormalSilence.Text = "Silence Threshold (sec):";
            // 
            // grpDictationMode
            // 
            grpDictationMode.Controls.Add(numDictationMaxDuration);
            grpDictationMode.Controls.Add(lblDictationMaxDuration);
            grpDictationMode.Controls.Add(numDictationSilence);
            grpDictationMode.Controls.Add(lblDictationSilence);
            grpDictationMode.Location = new Point(285, 60);
            grpDictationMode.Name = "grpDictationMode";
            grpDictationMode.Size = new Size(250, 130);
            grpDictationMode.TabIndex = 3;
            grpDictationMode.TabStop = false;
            grpDictationMode.Text = "Global Dictation Mode Chunking";
            // 
            // numDictationMaxDuration
            // 
            numDictationMaxDuration.DecimalPlaces = 1;
            numDictationMaxDuration.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            numDictationMaxDuration.Location = new Point(15, 45);
            numDictationMaxDuration.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            numDictationMaxDuration.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numDictationMaxDuration.Name = "numDictationMaxDuration";
            numDictationMaxDuration.Size = new Size(120, 23);
            numDictationMaxDuration.TabIndex = 1;
            numDictationMaxDuration.Value = new decimal(new int[] { 30, 0, 0, 65536 });
            // 
            // lblDictationMaxDuration
            // 
            lblDictationMaxDuration.AutoSize = true;
            lblDictationMaxDuration.Location = new Point(15, 25);
            lblDictationMaxDuration.Name = "lblDictationMaxDuration";
            lblDictationMaxDuration.Size = new Size(109, 15);
            lblDictationMaxDuration.TabIndex = 0;
            lblDictationMaxDuration.Text = "Max Duration (sec):";
            // 
            // numDictationSilence
            // 
            numDictationSilence.DecimalPlaces = 2;
            numDictationSilence.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numDictationSilence.Location = new Point(15, 95);
            numDictationSilence.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numDictationSilence.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            numDictationSilence.Name = "numDictationSilence";
            numDictationSilence.Size = new Size(120, 23);
            numDictationSilence.TabIndex = 3;
            numDictationSilence.Value = new decimal(new int[] { 75, 0, 0, 131072 });
            // 
            // lblDictationSilence
            // 
            lblDictationSilence.AutoSize = true;
            lblDictationSilence.Location = new Point(15, 75);
            lblDictationSilence.Name = "lblDictationSilence";
            lblDictationSilence.Size = new Size(131, 15);
            lblDictationSilence.TabIndex = 2;
            lblDictationSilence.Text = "Silence Threshold (sec):";
            // 
            // lblVadExplanation
            // 
            lblVadExplanation.ForeColor = Color.DimGray;
            lblVadExplanation.Location = new Point(15, 205);
            lblVadExplanation.Name = "lblVadExplanation";
            lblVadExplanation.Size = new Size(520, 75);
            lblVadExplanation.TabIndex = 4;
            lblVadExplanation.Text = resources.GetString("lblVadExplanation.Text");
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
            tabLlmPrompts.Size = new Size(578, 458);
            tabLlmPrompts.TabIndex = 2;
            tabLlmPrompts.Text = "\U0001f9e0 LLM & Prompts";
            tabLlmPrompts.UseVisualStyleBackColor = true;
            // 
            // txtUserPrompt
            // 
            txtUserPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtUserPrompt.Location = new Point(15, 265);
            txtUserPrompt.Multiline = true;
            txtUserPrompt.Name = "txtUserPrompt";
            txtUserPrompt.ScrollBars = ScrollBars.Vertical;
            txtUserPrompt.Size = new Size(546, 170);
            txtUserPrompt.TabIndex = 6;
            txtUserPrompt.TextChanged += txtUserPrompt_TextChanged;
            // 
            // lblUserPrompt
            // 
            lblUserPrompt.AutoSize = true;
            lblUserPrompt.Location = new Point(15, 245);
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
            txtSystemPrompt.Size = new Size(546, 140);
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
            cmbPromptProfile.Size = new Size(441, 23);
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
            tabGeneral.Controls.Add(chkShowRealtime);
            tabGeneral.Controls.Add(grpHotkeys);
            tabGeneral.Location = new Point(4, 24);
            tabGeneral.Name = "tabGeneral";
            tabGeneral.Padding = new Padding(3);
            tabGeneral.Size = new Size(578, 458);
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
            // chkShowRealtime
            // 
            chkShowRealtime.AutoSize = true;
            chkShowRealtime.Checked = true;
            chkShowRealtime.CheckState = CheckState.Checked;
            chkShowRealtime.Location = new Point(15, 50);
            chkShowRealtime.Name = "chkShowRealtime";
            chkShowRealtime.Size = new Size(319, 19);
            chkShowRealtime.TabIndex = 1;
            chkShowRealtime.Text = "Show Real-time Word/Segment Transcription (Whisper)";
            chkShowRealtime.UseVisualStyleBackColor = true;
            // 
            // grpHotkeys
            // 
            grpHotkeys.Controls.Add(txtDictationModifiers);
            grpHotkeys.Controls.Add(lblDictationMods);
            grpHotkeys.Controls.Add(txtDictationKey);
            grpHotkeys.Controls.Add(lblDictationKey);
            grpHotkeys.Controls.Add(txtProofreadModifiers);
            grpHotkeys.Controls.Add(lblProofreadMods);
            grpHotkeys.Controls.Add(txtProofreadKey);
            grpHotkeys.Controls.Add(lblProofreadKey);
            grpHotkeys.Controls.Add(lblHotkeyNote);
            grpHotkeys.Location = new Point(15, 90);
            grpHotkeys.Name = "grpHotkeys";
            grpHotkeys.Size = new Size(550, 240);
            grpHotkeys.TabIndex = 2;
            grpHotkeys.TabStop = false;
            grpHotkeys.Text = "Global Keyboard Hotkey Overrides";
            // 
            // txtDictationModifiers
            // 
            txtDictationModifiers.Location = new Point(15, 45);
            txtDictationModifiers.Name = "txtDictationModifiers";
            txtDictationModifiers.Size = new Size(200, 23);
            txtDictationModifiers.TabIndex = 1;
            // 
            // lblDictationMods
            // 
            lblDictationMods.AutoSize = true;
            lblDictationMods.Location = new Point(15, 25);
            lblDictationMods.Name = "lblDictationMods";
            lblDictationMods.Size = new Size(208, 15);
            lblDictationMods.TabIndex = 0;
            lblDictationMods.Text = "Dictation Modifiers (e.g., Control, Alt):";
            // 
            // txtDictationKey
            // 
            txtDictationKey.Location = new Point(240, 45);
            txtDictationKey.Name = "txtDictationKey";
            txtDictationKey.Size = new Size(100, 23);
            txtDictationKey.TabIndex = 3;
            // 
            // lblDictationKey
            // 
            lblDictationKey.AutoSize = true;
            lblDictationKey.Location = new Point(240, 25);
            lblDictationKey.Name = "lblDictationKey";
            lblDictationKey.Size = new Size(110, 15);
            lblDictationKey.TabIndex = 2;
            lblDictationKey.Text = "Key (e.g., D, Space):";
            // 
            // txtProofreadModifiers
            // 
            txtProofreadModifiers.Location = new Point(15, 105);
            txtProofreadModifiers.Name = "txtProofreadModifiers";
            txtProofreadModifiers.Size = new Size(200, 23);
            txtProofreadModifiers.TabIndex = 5;
            // 
            // lblProofreadMods
            // 
            lblProofreadMods.AutoSize = true;
            lblProofreadMods.Location = new Point(15, 85);
            lblProofreadMods.Name = "lblProofreadMods";
            lblProofreadMods.Size = new Size(212, 15);
            lblProofreadMods.TabIndex = 4;
            lblProofreadMods.Text = "Proofread Modifiers (e.g., Control, Alt):";
            // 
            // txtProofreadKey
            // 
            txtProofreadKey.Location = new Point(240, 105);
            txtProofreadKey.Name = "txtProofreadKey";
            txtProofreadKey.Size = new Size(100, 23);
            txtProofreadKey.TabIndex = 7;
            // 
            // lblProofreadKey
            // 
            lblProofreadKey.AutoSize = true;
            lblProofreadKey.Location = new Point(240, 85);
            lblProofreadKey.Name = "lblProofreadKey";
            lblProofreadKey.Size = new Size(109, 15);
            lblProofreadKey.TabIndex = 6;
            lblProofreadKey.Text = "Key (e.g., P, Space):";
            // 
            // lblHotkeyNote
            // 
            lblHotkeyNote.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblHotkeyNote.ForeColor = Color.DarkRed;
            lblHotkeyNote.Location = new Point(15, 145);
            lblHotkeyNote.Name = "lblHotkeyNote";
            lblHotkeyNote.Size = new Size(520, 80);
            lblHotkeyNote.TabIndex = 8;
            lblHotkeyNote.Text = "⚠️ Modifiers must be comma-separated combinations of Control, Alt, Shift, or Windows. Key is a letter, number, or function key. Changes require an application restart to take effect on system hooks.";
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.Location = new Point(410, 510);
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
            btnCancel.Location = new Point(508, 510);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 32);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // llWhisper
            // 
            llWhisper.AutoSize = true;
            llWhisper.Location = new Point(15, 66);
            llWhisper.Name = "llWhisper";
            llWhisper.Size = new Size(56, 15);
            llWhisper.TabIndex = 8;
            llWhisper.TabStop = true;
            llWhisper.Text = "llWhisper";
            // 
            // llLLM
            // 
            llLLM.AutoSize = true;
            llLLM.Location = new Point(15, 136);
            llLLM.Name = "llLLM";
            llLLM.Size = new Size(36, 15);
            llLLM.TabIndex = 9;
            llLLM.TabStop = true;
            llLLM.Text = "llLLM";
            // 
            // SettingsForm
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(610, 560);
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
            grpLlmAdvanced.ResumeLayout(false);
            grpLlmAdvanced.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numLlmContextSize).EndInit();
            ((System.ComponentModel.ISupportInitialize)numLlmSeed).EndInit();
            ((System.ComponentModel.ISupportInitialize)numLlmTemperature).EndInit();
            ((System.ComponentModel.ISupportInitialize)numLlmMaxTokens).EndInit();
            tabAudioVad.ResumeLayout(false);
            tabAudioVad.PerformLayout();
            grpAudioChunking.ResumeLayout(false);
            grpAudioChunking.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numVadGain).EndInit();
            grpNormalMode.ResumeLayout(false);
            grpNormalMode.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numNormalMaxDuration).EndInit();
            ((System.ComponentModel.ISupportInitialize)numNormalSilence).EndInit();
            grpDictationMode.ResumeLayout(false);
            grpDictationMode.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numDictationMaxDuration).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDictationSilence).EndInit();
            tabLlmPrompts.ResumeLayout(false);
            tabLlmPrompts.PerformLayout();
            tabGeneral.ResumeLayout(false);
            tabGeneral.PerformLayout();
            grpHotkeys.ResumeLayout(false);
            grpHotkeys.PerformLayout();
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

        // Advanced LLM Engine Controls (Tab 1)
        private System.Windows.Forms.GroupBox grpLlmAdvanced;
        private System.Windows.Forms.Label lblLlmContextSize;
        private System.Windows.Forms.NumericUpDown numLlmContextSize;
        private System.Windows.Forms.Label lblLlmSeed;
        private System.Windows.Forms.NumericUpDown numLlmSeed;
        private System.Windows.Forms.Label lblLlmTemperature;
        private System.Windows.Forms.NumericUpDown numLlmTemperature;
        private System.Windows.Forms.Label lblLlmMaxTokens;
        private System.Windows.Forms.NumericUpDown numLlmMaxTokens;
        private System.Windows.Forms.CheckBox chkMaintainContext;

        // Advanced Audio & VAD Controls (Tab 2)
        private System.Windows.Forms.GroupBox grpAudioChunking;
        private System.Windows.Forms.Label lblVadGain;
        private System.Windows.Forms.NumericUpDown numVadGain;
        private System.Windows.Forms.GroupBox grpNormalMode;
        private System.Windows.Forms.Label lblNormalMaxDuration;
        private System.Windows.Forms.NumericUpDown numNormalMaxDuration;
        private System.Windows.Forms.Label lblNormalSilence;
        private System.Windows.Forms.NumericUpDown numNormalSilence;
        private System.Windows.Forms.GroupBox grpDictationMode;
        private System.Windows.Forms.Label lblDictationMaxDuration;
        private System.Windows.Forms.NumericUpDown numDictationMaxDuration;
        private System.Windows.Forms.Label lblDictationSilence;
        private System.Windows.Forms.NumericUpDown numDictationSilence;

        // Tab 4 (General / Hotkeys)
        private System.Windows.Forms.CheckBox chkShowRealtime;
        private System.Windows.Forms.GroupBox grpHotkeys;
        private System.Windows.Forms.Label lblDictationMods;
        private System.Windows.Forms.TextBox txtDictationModifiers;
        private System.Windows.Forms.Label lblDictationKey;
        private System.Windows.Forms.TextBox txtDictationKey;
        private System.Windows.Forms.Label lblProofreadMods;
        private System.Windows.Forms.TextBox txtProofreadModifiers;
        private System.Windows.Forms.Label lblProofreadKey;
        private System.Windows.Forms.TextBox txtProofreadKey;
        private System.Windows.Forms.Label lblHotkeyNote;
        private LinkLabel llLLM;
        private LinkLabel llWhisper;
        }
}
