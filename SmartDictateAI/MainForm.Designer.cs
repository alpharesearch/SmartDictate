namespace SmartDictateAI
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            textBoxOutput = new TextBox();
            textBoxDebug = new TextBox();
            cmbVadSensitivity = new ComboBox();
            btnModelSettings = new Button();
            btnMicInput = new Button();
            btnStartStop = new Button();
            lblCalibrationIndicator = new Label();
            lblStatusIndicator = new Label();
            btnCopyRawText = new Button();
            btnCopyLLMText = new Button();
            chkDebug = new CheckBox();
            chkLLM = new CheckBox();
            lblDictateInstruction = new Label();
            btnLLMcb = new Button();
            lblProofreadInstruction = new Label();
            lblVramUsage = new Label();
            gbMain = new GroupBox();
            gbControl = new GroupBox();
            cmbPromptSelect = new ComboBox();
            gbDebug = new GroupBox();
            gbMain.SuspendLayout();
            gbControl.SuspendLayout();
            gbDebug.SuspendLayout();
            SuspendLayout();
            // 
            // textBoxOutput
            // 
            textBoxOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxOutput.Location = new Point(7, 76);
            textBoxOutput.Margin = new Padding(1);
            textBoxOutput.Multiline = true;
            textBoxOutput.Name = "textBoxOutput";
            textBoxOutput.ScrollBars = ScrollBars.Vertical;
            textBoxOutput.Size = new Size(777, 206);
            textBoxOutput.TabIndex = 1;
            // 
            // textBoxDebug
            // 
            textBoxDebug.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxDebug.Location = new Point(0, 45);
            textBoxDebug.Margin = new Padding(1);
            textBoxDebug.Multiline = true;
            textBoxDebug.Name = "textBoxDebug";
            textBoxDebug.ScrollBars = ScrollBars.Vertical;
            textBoxDebug.Size = new Size(777, 207);
            textBoxDebug.TabIndex = 2;
            // 
            // cmbVadSensitivity
            // 
            cmbVadSensitivity.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmbVadSensitivity.Location = new Point(460, 22);
            cmbVadSensitivity.Margin = new Padding(1);
            cmbVadSensitivity.Name = "cmbVadSensitivity";
            cmbVadSensitivity.Size = new Size(73, 23);
            cmbVadSensitivity.TabIndex = 3;
            cmbVadSensitivity.Text = "VAD";
            // 
            // btnModelSettings
            // 
            btnModelSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnModelSettings.Location = new Point(593, 22);
            btnModelSettings.Margin = new Padding(1);
            btnModelSettings.Name = "btnModelSettings";
            btnModelSettings.Size = new Size(56, 24);
            btnModelSettings.TabIndex = 4;
            btnModelSettings.Text = "Model";
            btnModelSettings.UseVisualStyleBackColor = true;
            btnModelSettings.Click += btnModelSettings_Click;
            // 
            // btnMicInput
            // 
            btnMicInput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMicInput.Location = new Point(535, 22);
            btnMicInput.Margin = new Padding(1);
            btnMicInput.Name = "btnMicInput";
            btnMicInput.Size = new Size(56, 24);
            btnMicInput.TabIndex = 5;
            btnMicInput.Text = "Mic input";
            btnMicInput.UseVisualStyleBackColor = true;
            btnMicInput.Click += btnMicInput_Click;
            // 
            // btnStartStop
            // 
            btnStartStop.Location = new Point(4, 20);
            btnStartStop.Margin = new Padding(1);
            btnStartStop.Name = "btnStartStop";
            btnStartStop.Size = new Size(112, 27);
            btnStartStop.TabIndex = 6;
            btnStartStop.Text = "Start";
            btnStartStop.UseVisualStyleBackColor = true;
            btnStartStop.Click += btnStartStop_Click;
            // 
            // lblCalibrationIndicator
            // 
            lblCalibrationIndicator.AutoSize = true;
            lblCalibrationIndicator.Location = new Point(60, 48);
            lblCalibrationIndicator.Margin = new Padding(1, 0, 1, 0);
            lblCalibrationIndicator.Name = "lblCalibrationIndicator";
            lblCalibrationIndicator.Size = new Size(0, 15);
            lblCalibrationIndicator.TabIndex = 7;
            // 
            // lblStatusIndicator
            // 
            lblStatusIndicator.AutoSize = true;
            lblStatusIndicator.Font = new Font("Segoe UI Emoji", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStatusIndicator.Location = new Point(118, 22);
            lblStatusIndicator.Margin = new Padding(1, 0, 1, 0);
            lblStatusIndicator.Name = "lblStatusIndicator";
            lblStatusIndicator.Size = new Size(31, 19);
            lblStatusIndicator.TabIndex = 8;
            lblStatusIndicator.Text = "Idle";
            // 
            // btnCopyRawText
            // 
            btnCopyRawText.Location = new Point(4, 20);
            btnCopyRawText.Margin = new Padding(1);
            btnCopyRawText.Name = "btnCopyRawText";
            btnCopyRawText.Size = new Size(88, 24);
            btnCopyRawText.TabIndex = 9;
            btnCopyRawText.Text = "Copy Raw";
            btnCopyRawText.UseVisualStyleBackColor = true;
            btnCopyRawText.Click += btnCopyRawText_Click;
            // 
            // btnCopyLLMText
            // 
            btnCopyLLMText.Location = new Point(94, 20);
            btnCopyLLMText.Margin = new Padding(1);
            btnCopyLLMText.Name = "btnCopyLLMText";
            btnCopyLLMText.Size = new Size(88, 24);
            btnCopyLLMText.TabIndex = 10;
            btnCopyLLMText.Text = "Copy LLM";
            btnCopyLLMText.UseVisualStyleBackColor = true;
            btnCopyLLMText.Click += btnCopyLLMText_Click;
            // 
            // chkDebug
            // 
            chkDebug.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkDebug.AutoSize = true;
            chkDebug.Location = new Point(707, 25);
            chkDebug.Margin = new Padding(1);
            chkDebug.Name = "chkDebug";
            chkDebug.Size = new Size(61, 19);
            chkDebug.TabIndex = 11;
            chkDebug.Text = "Debug";
            chkDebug.UseVisualStyleBackColor = true;
            chkDebug.CheckedChanged += chkDebug_CheckedChanged;
            // 
            // chkLLM
            // 
            chkLLM.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkLLM.AutoSize = true;
            chkLLM.Location = new Point(656, 25);
            chkLLM.Margin = new Padding(1);
            chkLLM.Name = "chkLLM";
            chkLLM.Size = new Size(49, 19);
            chkLLM.TabIndex = 12;
            chkLLM.Text = "LLM";
            chkLLM.UseVisualStyleBackColor = true;
            chkLLM.CheckedChanged += chkLLM_CheckedChanged;
            // 
            // lblDictateInstruction
            // 
            lblDictateInstruction.AutoSize = true;
            lblDictateInstruction.Location = new Point(184, 19);
            lblDictateInstruction.Margin = new Padding(1, 0, 1, 0);
            lblDictateInstruction.Name = "lblDictateInstruction";
            lblDictateInstruction.Size = new Size(96, 30);
            lblDictateInstruction.TabIndex = 13;
            lblDictateInstruction.Text = "CTRL + ALT + D\r\nto dictate cursor.";
            // 
            // btnLLMcb
            // 
            btnLLMcb.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLLMcb.Location = new Point(693, 20);
            btnLLMcb.Name = "btnLLMcb";
            btnLLMcb.Size = new Size(75, 24);
            btnLLMcb.TabIndex = 14;
            btnLLMcb.Text = "Rerun LLM";
            btnLLMcb.UseVisualStyleBackColor = true;
            btnLLMcb.Click += btnLLMcb_Click;
            // 
            // lblProofreadInstruction
            // 
            lblProofreadInstruction.AutoSize = true;
            lblProofreadInstruction.Location = new Point(282, 19);
            lblProofreadInstruction.Margin = new Padding(1, 0, 1, 0);
            lblProofreadInstruction.Name = "lblProofreadInstruction";
            lblProofreadInstruction.Size = new Size(168, 30);
            lblProofreadInstruction.TabIndex = 15;
            lblProofreadInstruction.Text = "CTRL + ALT + P\r\nto proofreads clipboard locally";
            // 
            // lblVramUsage
            // 
            lblVramUsage.AutoSize = true;
            lblVramUsage.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblVramUsage.Location = new Point(4, 19);
            lblVramUsage.Margin = new Padding(1, 0, 1, 0);
            lblVramUsage.Name = "lblVramUsage";
            lblVramUsage.Size = new Size(40, 15);
            lblVramUsage.TabIndex = 16;
            lblVramUsage.Text = "VRAM";
            // 
            // gbMain
            // 
            gbMain.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbMain.Controls.Add(lblStatusIndicator);
            gbMain.Controls.Add(btnStartStop);
            gbMain.Controls.Add(cmbVadSensitivity);
            gbMain.Controls.Add(btnModelSettings);
            gbMain.Controls.Add(btnMicInput);
            gbMain.Controls.Add(lblCalibrationIndicator);
            gbMain.Controls.Add(chkLLM);
            gbMain.Controls.Add(chkDebug);
            gbMain.Location = new Point(7, 12);
            gbMain.Name = "gbMain";
            gbMain.Size = new Size(777, 57);
            gbMain.TabIndex = 17;
            gbMain.TabStop = false;
            gbMain.Text = "Main";
            // 
            // gbControl
            // 
            gbControl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gbControl.Controls.Add(cmbPromptSelect);
            gbControl.Controls.Add(btnCopyRawText);
            gbControl.Controls.Add(btnCopyLLMText);
            gbControl.Controls.Add(lblDictateInstruction);
            gbControl.Controls.Add(btnLLMcb);
            gbControl.Controls.Add(lblProofreadInstruction);
            gbControl.Location = new Point(7, 286);
            gbControl.Name = "gbControl";
            gbControl.Size = new Size(777, 55);
            gbControl.TabIndex = 18;
            gbControl.TabStop = false;
            gbControl.Text = "Control";
            // 
            // cmbPromptSelect
            // 
            cmbPromptSelect.FormattingEnabled = true;
            cmbPromptSelect.Location = new Point(521, 22);
            cmbPromptSelect.Name = "cmbPromptSelect";
            cmbPromptSelect.Size = new Size(166, 23);
            cmbPromptSelect.TabIndex = 16;
            cmbPromptSelect.SelectedValueChanged += cmbPromptSelect_SelectedValueChanged;
            // 
            // gbDebug
            // 
            gbDebug.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gbDebug.Controls.Add(lblVramUsage);
            gbDebug.Controls.Add(textBoxDebug);
            gbDebug.Location = new Point(7, 347);
            gbDebug.Name = "gbDebug";
            gbDebug.Size = new Size(777, 253);
            gbDebug.TabIndex = 19;
            gbDebug.TabStop = false;
            gbDebug.Text = "Debug";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(798, 606);
            Controls.Add(textBoxOutput);
            Controls.Add(gbDebug);
            Controls.Add(gbControl);
            Controls.Add(gbMain);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(1);
            Name = "MainForm";
            Text = "SmartDictateAI";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            gbMain.ResumeLayout(false);
            gbMain.PerformLayout();
            gbControl.ResumeLayout(false);
            gbControl.PerformLayout();
            gbDebug.ResumeLayout(false);
            gbDebug.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
            }

        #endregion

        private Button btnStartStop;
        private TextBox textBoxOutput;
        private TextBox textBoxDebug;
        private ComboBox cmbVadSensitivity;
        private Button btnModelSettings;
        private Button btnMicInput;
        private Label lblCalibrationIndicator;
        private Label lblStatusIndicator;
        private Button btnCopyRawText;
        private Button btnCopyLLMText;
        private CheckBox chkDebug;
        private CheckBox chkLLM;
        private Label lblDictateInstruction;
        private Button btnLLMcb;
        private Label lblProofreadInstruction;
        private Label lblVramUsage;
        private GroupBox gbMain;
        private GroupBox gbControl;
        private GroupBox gbDebug;
        private ComboBox cmbPromptSelect;
    }
    }
