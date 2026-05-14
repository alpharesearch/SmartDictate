namespace WhisperNetConsoleDemo
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
            label1 = new Label();
            label_vram = new Label();
            SuspendLayout();
            // 
            // textBoxOutput
            // 
            textBoxOutput.Location = new Point(7, 34);
            textBoxOutput.Margin = new Padding(1);
            textBoxOutput.Multiline = true;
            textBoxOutput.Name = "textBoxOutput";
            textBoxOutput.ScrollBars = ScrollBars.Vertical;
            textBoxOutput.Size = new Size(767, 371);
            textBoxOutput.TabIndex = 1;
            // 
            // textBoxDebug
            // 
            textBoxDebug.Location = new Point(7, 433);
            textBoxDebug.Margin = new Padding(1);
            textBoxDebug.Multiline = true;
            textBoxDebug.Name = "textBoxDebug";
            textBoxDebug.ScrollBars = ScrollBars.Vertical;
            textBoxDebug.Size = new Size(767, 354);
            textBoxDebug.TabIndex = 2;
            // 
            // cmbVadSensitivity
            // 
            cmbVadSensitivity.Location = new Point(466, 7);
            cmbVadSensitivity.Margin = new Padding(1);
            cmbVadSensitivity.Name = "cmbVadSensitivity";
            cmbVadSensitivity.Size = new Size(73, 23);
            cmbVadSensitivity.TabIndex = 3;
            cmbVadSensitivity.Text = "VAD";
            // 
            // btnModelSettings
            // 
            btnModelSettings.Location = new Point(599, 7);
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
            btnMicInput.Location = new Point(541, 7);
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
            btnStartStop.Location = new Point(10, 5);
            btnStartStop.Margin = new Padding(1);
            btnStartStop.Name = "btnStartStop";
            btnStartStop.Size = new Size(143, 27);
            btnStartStop.TabIndex = 6;
            btnStartStop.Text = "Start";
            btnStartStop.UseVisualStyleBackColor = true;
            btnStartStop.Click += btnStart_Stop_Click;
            // 
            // lblCalibrationIndicator
            // 
            lblCalibrationIndicator.AutoSize = true;
            lblCalibrationIndicator.Location = new Point(66, 33);
            lblCalibrationIndicator.Margin = new Padding(1, 0, 1, 0);
            lblCalibrationIndicator.Name = "lblCalibrationIndicator";
            lblCalibrationIndicator.Size = new Size(0, 15);
            lblCalibrationIndicator.TabIndex = 7;
            // 
            // lblStatusIndicator
            // 
            lblStatusIndicator.AutoSize = true;
            lblStatusIndicator.Location = new Point(162, 9);
            lblStatusIndicator.Margin = new Padding(1, 0, 1, 0);
            lblStatusIndicator.Name = "lblStatusIndicator";
            lblStatusIndicator.Size = new Size(26, 15);
            lblStatusIndicator.TabIndex = 8;
            lblStatusIndicator.Text = "Idle";
            // 
            // btnCopyRawText
            // 
            btnCopyRawText.Location = new Point(10, 407);
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
            btnCopyLLMText.Location = new Point(100, 407);
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
            chkDebug.AutoSize = true;
            chkDebug.Location = new Point(713, 10);
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
            chkLLM.AutoSize = true;
            chkLLM.Location = new Point(662, 10);
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
            lblDictateInstruction.Location = new Point(190, 412);
            lblDictateInstruction.Margin = new Padding(1, 0, 1, 0);
            lblDictateInstruction.Name = "lblDictateInstruction";
            lblDictateInstruction.Size = new Size(183, 15);
            lblDictateInstruction.TabIndex = 13;
            lblDictateInstruction.Text = "CTRL + ALT + D to dictate cursor.";
            // 
            // btnLLMcb
            // 
            btnLLMcb.Location = new Point(699, 407);
            btnLLMcb.Name = "btnLLMcb";
            btnLLMcb.Size = new Size(75, 24);
            btnLLMcb.TabIndex = 14;
            btnLLMcb.Text = "Rerun LLM";
            btnLLMcb.UseVisualStyleBackColor = true;
            btnLLMcb.Click += btnLLMcb_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(401, 412);
            label1.Margin = new Padding(1, 0, 1, 0);
            label1.Name = "label1";
            label1.Size = new Size(254, 15);
            label1.TabIndex = 15;
            label1.Text = "CTRL + ALT + P to proofreads clipboard locally";
            // 
            // label_vram
            // 
            label_vram.AutoSize = true;
            label_vram.Font = new Font("Segoe UI", 7F);
            label_vram.Location = new Point(229, 11);
            label_vram.Margin = new Padding(1, 0, 1, 0);
            label_vram.Name = "label_vram";
            label_vram.Size = new Size(32, 12);
            label_vram.TabIndex = 16;
            label_vram.Text = "VRAM";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 441);
            Controls.Add(label_vram);
            Controls.Add(label1);
            Controls.Add(btnLLMcb);
            Controls.Add(lblDictateInstruction);
            Controls.Add(chkLLM);
            Controls.Add(chkDebug);
            Controls.Add(btnCopyLLMText);
            Controls.Add(btnCopyRawText);
            Controls.Add(lblStatusIndicator);
            Controls.Add(lblCalibrationIndicator);
            Controls.Add(btnStartStop);
            Controls.Add(btnMicInput);
            Controls.Add(btnModelSettings);
            Controls.Add(cmbVadSensitivity);
            Controls.Add(textBoxDebug);
            Controls.Add(textBoxOutput);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(1);
            Name = "MainForm";
            Text = "SmartDictateAI";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
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
        private Label label1;
        private Label label_vram;
        }
    }
