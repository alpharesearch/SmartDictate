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
            label_vram = new Label();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            comboBox1 = new ComboBox();
            groupBox3 = new GroupBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
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
            btnStartStop.Size = new Size(143, 27);
            btnStartStop.TabIndex = 6;
            btnStartStop.Text = "Start";
            btnStartStop.UseVisualStyleBackColor = true;
            btnStartStop.Click += btnStart_Stop_Click;
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
            lblStatusIndicator.Location = new Point(7, 60);
            lblStatusIndicator.Margin = new Padding(1, 0, 1, 0);
            lblStatusIndicator.Name = "lblStatusIndicator";
            lblStatusIndicator.Size = new Size(26, 15);
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
            // label_vram
            // 
            label_vram.AutoSize = true;
            label_vram.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label_vram.Location = new Point(4, 19);
            label_vram.Margin = new Padding(1, 0, 1, 0);
            label_vram.Name = "label_vram";
            label_vram.Size = new Size(40, 15);
            label_vram.TabIndex = 16;
            label_vram.Text = "VRAM";
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(btnStartStop);
            groupBox1.Controls.Add(cmbVadSensitivity);
            groupBox1.Controls.Add(btnModelSettings);
            groupBox1.Controls.Add(btnMicInput);
            groupBox1.Controls.Add(lblCalibrationIndicator);
            groupBox1.Controls.Add(chkLLM);
            groupBox1.Controls.Add(chkDebug);
            groupBox1.Location = new Point(7, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(777, 57);
            groupBox1.TabIndex = 17;
            groupBox1.TabStop = false;
            groupBox1.Text = "Main";
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Controls.Add(comboBox1);
            groupBox2.Controls.Add(btnCopyRawText);
            groupBox2.Controls.Add(btnCopyLLMText);
            groupBox2.Controls.Add(lblDictateInstruction);
            groupBox2.Controls.Add(btnLLMcb);
            groupBox2.Controls.Add(lblProofreadInstruction);
            groupBox2.Location = new Point(7, 286);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(777, 55);
            groupBox2.TabIndex = 18;
            groupBox2.TabStop = false;
            groupBox2.Text = "Control";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(566, 22);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(121, 23);
            comboBox1.TabIndex = 16;
            comboBox1.SelectedValueChanged += comboBox1_SelectedValueChanged;
            // 
            // groupBox3
            // 
            groupBox3.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox3.Controls.Add(label_vram);
            groupBox3.Controls.Add(textBoxDebug);
            groupBox3.Location = new Point(7, 347);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(777, 253);
            groupBox3.TabIndex = 19;
            groupBox3.TabStop = false;
            groupBox3.Text = "Debug";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(798, 606);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(textBoxOutput);
            Controls.Add(lblStatusIndicator);
            Controls.Add(groupBox1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(1);
            Name = "MainForm";
            Text = "SmartDictateAI";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
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
        private Label label_vram;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private ComboBox comboBox1;
    }
    }
