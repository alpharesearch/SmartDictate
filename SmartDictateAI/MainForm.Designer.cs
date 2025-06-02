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
            textBoxOutput = new TextBox();
            textBoxDebug = new TextBox();
            btnCalibration = new Button();
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
            SuspendLayout();
            // 
            // textBoxOutput
            // 
            textBoxOutput.Location = new Point(7, 51);
            textBoxOutput.Margin = new Padding(1, 1, 1, 1);
            textBoxOutput.Multiline = true;
            textBoxOutput.Name = "textBoxOutput";
            textBoxOutput.ScrollBars = ScrollBars.Vertical;
            textBoxOutput.Size = new Size(380, 354);
            textBoxOutput.TabIndex = 1;
            // 
            // textBoxDebug
            // 
            textBoxDebug.Location = new Point(394, 51);
            textBoxDebug.Margin = new Padding(1, 1, 1, 1);
            textBoxDebug.Multiline = true;
            textBoxDebug.Name = "textBoxDebug";
            textBoxDebug.ScrollBars = ScrollBars.Vertical;
            textBoxDebug.Size = new Size(380, 354);
            textBoxDebug.TabIndex = 2;
            // 
            // btnCalibration
            // 
            btnCalibration.Location = new Point(66, 7);
            btnCalibration.Margin = new Padding(1, 1, 1, 1);
            btnCalibration.Name = "btnCalibration";
            btnCalibration.Size = new Size(56, 24);
            btnCalibration.TabIndex = 3;
            btnCalibration.Text = "Calibration";
            btnCalibration.UseVisualStyleBackColor = true;
            btnCalibration.Click += btnCalibration_Click;
            // 
            // btnModelSettings
            // 
            btnModelSettings.Location = new Point(125, 7);
            btnModelSettings.Margin = new Padding(1, 1, 1, 1);
            btnModelSettings.Name = "btnModelSettings";
            btnModelSettings.Size = new Size(56, 24);
            btnModelSettings.TabIndex = 4;
            btnModelSettings.Text = "Model";
            btnModelSettings.UseVisualStyleBackColor = true;
            btnModelSettings.Click += btnModelSettings_Click;
            // 
            // btnMicInput
            // 
            btnMicInput.Location = new Point(183, 7);
            btnMicInput.Margin = new Padding(1, 1, 1, 1);
            btnMicInput.Name = "btnMicInput";
            btnMicInput.Size = new Size(56, 24);
            btnMicInput.TabIndex = 5;
            btnMicInput.Text = "Mic input";
            btnMicInput.UseVisualStyleBackColor = true;
            btnMicInput.Click += btnMicInput_Click;
            // 
            // btnStartStop
            // 
            btnStartStop.Location = new Point(7, 7);
            btnStartStop.Margin = new Padding(1, 1, 1, 1);
            btnStartStop.Name = "btnStartStop";
            btnStartStop.Size = new Size(56, 24);
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
            lblCalibrationIndicator.Size = new Size(15, 15);
            lblCalibrationIndicator.TabIndex = 7;
            lblCalibrationIndicator.Text = "C";
            // 
            // lblStatusIndicator
            // 
            lblStatusIndicator.AutoSize = true;
            lblStatusIndicator.Location = new Point(7, 33);
            lblStatusIndicator.Margin = new Padding(1, 0, 1, 0);
            lblStatusIndicator.Name = "lblStatusIndicator";
            lblStatusIndicator.Size = new Size(26, 15);
            lblStatusIndicator.TabIndex = 8;
            lblStatusIndicator.Text = "Idle";
            // 
            // btnCopyRawText
            // 
            btnCopyRawText.Location = new Point(10, 407);
            btnCopyRawText.Margin = new Padding(1, 1, 1, 1);
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
            btnCopyLLMText.Margin = new Padding(1, 1, 1, 1);
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
            chkDebug.Margin = new Padding(1, 1, 1, 1);
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
            chkLLM.Margin = new Padding(1, 1, 1, 1);
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
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(784, 441);
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
            Controls.Add(btnCalibration);
            Controls.Add(textBoxDebug);
            Controls.Add(textBoxOutput);
            Margin = new Padding(1, 1, 1, 1);
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
        private Button btnCalibration;
        private Button btnModelSettings;
        private Button btnMicInput;
        private Label lblCalibrationIndicator;
        private Label lblStatusIndicator;
        private Button btnCopyRawText;
        private Button btnCopyLLMText;
        private CheckBox chkDebug;
        private CheckBox chkLLM;
        private Label lblDictateInstruction;
        }
    }
