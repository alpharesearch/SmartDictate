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
            btnStartStop = new Button();
            lblStatusIndicator = new Label();
            btnCopyRawText = new Button();
            btnCopyLLMText = new Button();
            lblDictateInstruction = new Label();
            btnLLMcb = new Button();
            lblProofreadInstruction = new Label();
            lblVramUsage = new Label();
            gbMain = new GroupBox();
            btnSettings = new Button();
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
            textBoxOutput.Size = new Size(784, 206);
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
            textBoxDebug.Size = new Size(784, 207);
            textBoxDebug.TabIndex = 2;
            // 
            // btnStartStop
            // 
            btnStartStop.Location = new Point(4, 20);
            btnStartStop.Margin = new Padding(1);
            btnStartStop.Name = "btnStartStop";
            btnStartStop.Size = new Size(120, 27);
            btnStartStop.TabIndex = 6;
            btnStartStop.Text = "Start Recording";
            btnStartStop.UseVisualStyleBackColor = true;
            btnStartStop.Click += btnStartStop_Click;
            // 
            // lblStatusIndicator
            // 
            lblStatusIndicator.AutoSize = true;
            lblStatusIndicator.Font = new Font("Segoe UI Emoji", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStatusIndicator.Location = new Point(130, 23);
            lblStatusIndicator.Margin = new Padding(1, 0, 1, 0);
            lblStatusIndicator.Name = "lblStatusIndicator";
            lblStatusIndicator.Size = new Size(31, 19);
            lblStatusIndicator.TabIndex = 8;
            lblStatusIndicator.Text = "Idle";
            // 
            // btnCopyRawText
            // 
            btnCopyRawText.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCopyRawText.Location = new Point(511, 20);
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
            btnCopyLLMText.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCopyLLMText.Location = new Point(601, 20);
            btnCopyLLMText.Margin = new Padding(1);
            btnCopyLLMText.Name = "btnCopyLLMText";
            btnCopyLLMText.Size = new Size(88, 24);
            btnCopyLLMText.TabIndex = 10;
            btnCopyLLMText.Text = "Copy LLM";
            btnCopyLLMText.UseVisualStyleBackColor = true;
            btnCopyLLMText.Click += btnCopyLLMText_Click;
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
            btnLLMcb.Size = new Size(85, 24);
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
            gbMain.Controls.Add(btnSettings);
            gbMain.Controls.Add(lblStatusIndicator);
            gbMain.Controls.Add(btnStartStop);
            gbMain.Location = new Point(7, 12);
            gbMain.Name = "gbMain";
            gbMain.Size = new Size(784, 57);
            gbMain.TabIndex = 17;
            gbMain.TabStop = false;
            gbMain.Text = "Main";
            // 
            // btnSettings
            // 
            btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSettings.Location = new Point(678, 20);
            btnSettings.Margin = new Padding(1);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(100, 27);
            btnSettings.TabIndex = 4;
            btnSettings.Text = "⚙ Settings";
            btnSettings.UseVisualStyleBackColor = true;
            btnSettings.Click += btnSettings_Click;
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
            gbControl.Size = new Size(784, 55);
            gbControl.TabIndex = 18;
            gbControl.TabStop = false;
            gbControl.Text = "Control";
            // 
            // cmbPromptSelect
            // 
            cmbPromptSelect.FormattingEnabled = true;
            cmbPromptSelect.Location = new Point(6, 22);
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
            gbDebug.Size = new Size(784, 253);
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
        private Label lblStatusIndicator;
        private Button btnCopyRawText;
        private Button btnCopyLLMText;
        private Label lblDictateInstruction;
        private Button btnLLMcb;
        private Label lblProofreadInstruction;
        private Label lblVramUsage;
        private GroupBox gbMain;
        private GroupBox gbControl;
        private GroupBox gbDebug;
        private ComboBox cmbPromptSelect;
        private Button btnSettings;
    }
}
