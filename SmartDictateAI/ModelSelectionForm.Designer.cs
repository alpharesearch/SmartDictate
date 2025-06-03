namespace WhisperNetConsoleDemo
    {
    partial class ModelSelectionForm
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
            labelWhisper = new Label();
            txtWhisperModelPath = new TextBox();
            btnBrowseWhisperModel = new Button();
            labelLLM = new Label();
            txtLLMModelPath = new TextBox();
            btnBrowseLLMModel = new Button();
            btnOK = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // labelWhisper
            // 
            labelWhisper.AutoSize = true;
            labelWhisper.Location = new Point(13, 9);
            labelWhisper.Name = "labelWhisper";
            labelWhisper.Size = new Size(117, 15);
            labelWhisper.TabIndex = 7;
            labelWhisper.Text = "Whisper Model Path:";
            // 
            // txtWhisperModelPath
            // 
            txtWhisperModelPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtWhisperModelPath.Location = new Point(13, 37);
            txtWhisperModelPath.Name = "txtWhisperModelPath";
            txtWhisperModelPath.Size = new Size(620, 23);
            txtWhisperModelPath.TabIndex = 6;
            // 
            // btnBrowseWhisperModel
            // 
            btnBrowseWhisperModel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseWhisperModel.Location = new Point(639, 29);
            btnBrowseWhisperModel.Name = "btnBrowseWhisperModel";
            btnBrowseWhisperModel.Size = new Size(104, 36);
            btnBrowseWhisperModel.TabIndex = 5;
            btnBrowseWhisperModel.Text = "Browse...";
            btnBrowseWhisperModel.Click += btnBrowseWhisperModel_Click;
            // 
            // labelLLM
            // 
            labelLLM.AutoSize = true;
            labelLLM.Location = new Point(13, 87);
            labelLLM.Name = "labelLLM";
            labelLLM.Size = new Size(97, 15);
            labelLLM.TabIndex = 4;
            labelLLM.Text = "LLM Model Path:";
            // 
            // txtLLMModelPath
            // 
            txtLLMModelPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtLLMModelPath.Location = new Point(12, 115);
            txtLLMModelPath.Name = "txtLLMModelPath";
            txtLLMModelPath.Size = new Size(621, 23);
            txtLLMModelPath.TabIndex = 3;
            // 
            // btnBrowseLLMModel
            // 
            btnBrowseLLMModel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseLLMModel.Location = new Point(639, 107);
            btnBrowseLLMModel.Name = "btnBrowseLLMModel";
            btnBrowseLLMModel.Size = new Size(104, 36);
            btnBrowseLLMModel.TabIndex = 2;
            btnBrowseLLMModel.Text = "Browse...";
            btnBrowseLLMModel.Click += btnBrowseLLMModel_Click;
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.Location = new Point(590, 172);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 40);
            btnOK.TabIndex = 1;
            btnOK.Text = "OK";
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(671, 172);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 40);
            btnCancel.TabIndex = 0;
            btnCancel.Text = "Cancel";
            btnCancel.Click += btnCancel_Click;
            // 
            // ModelSelectionForm
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(753, 224);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(btnBrowseLLMModel);
            Controls.Add(txtLLMModelPath);
            Controls.Add(labelLLM);
            Controls.Add(btnBrowseWhisperModel);
            Controls.Add(txtWhisperModelPath);
            Controls.Add(labelWhisper);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ModelSelectionForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Select Models";
            ResumeLayout(false);
            PerformLayout();
            }

        #endregion

        private Label labelWhisper;
        private TextBox txtWhisperModelPath;
        private Button btnBrowseWhisperModel;
        private Label labelLLM;
        private TextBox txtLLMModelPath;
        private Button btnBrowseLLMModel;
        private Button btnOK;
        private Button btnCancel;
        }
    }
