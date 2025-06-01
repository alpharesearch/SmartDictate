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
            textBox1 = new TextBox();
            textBox2 = new TextBox();
            button2 = new Button();
            button3 = new Button();
            button4 = new Button();
            btnStartStop = new Button();
            lblCalibrationStatus = new Label();
            lblStatusIndicator = new Label();
            btnCopyRawText = new Button();
            btnCopyLLMText = new Button();
            checkBox_debug = new CheckBox();
            bxLLM = new CheckBox();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Location = new Point(12, 77);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(621, 408);
            textBox1.TabIndex = 1;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(639, 77);
            textBox2.Multiline = true;
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(631, 408);
            textBox2.TabIndex = 2;
            // 
            // button2
            // 
            button2.Location = new Point(130, 12);
            button2.Name = "button2";
            button2.Size = new Size(112, 34);
            button2.TabIndex = 3;
            button2.Text = "Calibration";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.Location = new Point(248, 12);
            button3.Name = "button3";
            button3.Size = new Size(112, 34);
            button3.TabIndex = 4;
            button3.Text = "Model";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // button4
            // 
            button4.Location = new Point(366, 12);
            button4.Name = "button4";
            button4.Size = new Size(112, 34);
            button4.TabIndex = 5;
            button4.Text = "Mic input";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // btnStartStop
            // 
            btnStartStop.Location = new Point(12, 12);
            btnStartStop.Name = "btnStartStop";
            btnStartStop.Size = new Size(112, 34);
            btnStartStop.TabIndex = 6;
            btnStartStop.Text = "Start";
            btnStartStop.UseVisualStyleBackColor = true;
            btnStartStop.Click += btnStart_Stop_Click;
            // 
            // lblCalibrationStatus
            // 
            lblCalibrationStatus.AutoSize = true;
            lblCalibrationStatus.Location = new Point(130, 49);
            lblCalibrationStatus.Name = "lblCalibrationStatus";
            lblCalibrationStatus.Size = new Size(0, 25);
            lblCalibrationStatus.TabIndex = 7;
            // 
            // lblStatusIndicator
            // 
            lblStatusIndicator.AutoSize = true;
            lblStatusIndicator.Location = new Point(12, 49);
            lblStatusIndicator.Name = "lblStatusIndicator";
            lblStatusIndicator.Size = new Size(41, 25);
            lblStatusIndicator.TabIndex = 8;
            lblStatusIndicator.Text = "Idle";
            // 
            // btnCopyRawText
            // 
            btnCopyRawText.Location = new Point(12, 491);
            btnCopyRawText.Name = "btnCopyRawText";
            btnCopyRawText.Size = new Size(112, 34);
            btnCopyRawText.TabIndex = 9;
            btnCopyRawText.Text = "Copy Raw";
            btnCopyRawText.UseVisualStyleBackColor = true;
            btnCopyRawText.Click += btnCopyRawText_Click;
            // 
            // btnCopyLLMText
            // 
            btnCopyLLMText.Location = new Point(130, 491);
            btnCopyLLMText.Name = "btnCopyLLMText";
            btnCopyLLMText.Size = new Size(112, 34);
            btnCopyLLMText.TabIndex = 10;
            btnCopyLLMText.Text = "Copy LLM";
            btnCopyLLMText.UseVisualStyleBackColor = true;
            btnCopyLLMText.Click += btnCopyLLMText_Click;
            // 
            // checkBox_debug
            // 
            checkBox_debug.AutoSize = true;
            checkBox_debug.Location = new Point(1178, 17);
            checkBox_debug.Name = "checkBox_debug";
            checkBox_debug.Size = new Size(92, 29);
            checkBox_debug.TabIndex = 11;
            checkBox_debug.Text = "Debug";
            checkBox_debug.UseVisualStyleBackColor = true;
            checkBox_debug.CheckedChanged += debug_checkBox1_CheckedChanged;
            // 
            // bxLLM
            // 
            bxLLM.AutoSize = true;
            bxLLM.Location = new Point(1102, 17);
            bxLLM.Name = "bxLLM";
            bxLLM.Size = new Size(70, 29);
            bxLLM.TabIndex = 12;
            bxLLM.Text = "LLM";
            bxLLM.UseVisualStyleBackColor = true;
            bxLLM.CheckedChanged += bxLLM_CheckedChanged;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1276, 559);
            Controls.Add(bxLLM);
            Controls.Add(checkBox_debug);
            Controls.Add(btnCopyLLMText);
            Controls.Add(btnCopyRawText);
            Controls.Add(lblStatusIndicator);
            Controls.Add(lblCalibrationStatus);
            Controls.Add(btnStartStop);
            Controls.Add(button4);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(textBox2);
            Controls.Add(textBox1);
            Name = "MainForm";
            Text = "SmartDictateAI";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
            }

        #endregion

        private Button btnStartStop;
        private TextBox textBox1;
        private TextBox textBox2;
        private Button button2;
        private Button button3;
        private Button button4;
        private Label lblCalibrationStatus;
        private Label lblStatusIndicator;
        private Button btnCopyRawText;
        private Button btnCopyLLMText;
        private CheckBox checkBox_debug;
        private CheckBox bxLLM;
        }
    }
