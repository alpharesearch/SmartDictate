using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WhisperNetConsoleDemo
    {
    public partial class ModelSelectionForm : Form
        {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // Add this attribute
        public string SelectedWhisperModelPath
            {
            get; private set;
            }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // Add this attribute
        public string SelectedLLMModelPath
            {
            get; private set;
            }

        public ModelSelectionForm(string currentWhisperPath, string currentLLMPath)
            {
            InitializeComponent();

            SelectedWhisperModelPath = currentWhisperPath;
            SelectedLLMModelPath = currentLLMPath;

            txtWhisperModelPath.Text = currentWhisperPath;
            txtLLMModelPath.Text = currentLLMPath;
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
                ofd.Filter = "LLM Model Files (*.gguf)|*.gguf|All files (*.*)|*.*"; // GGUF is common for local LLMs
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
                else if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath)) // If it's just a directory
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

        private void btnOK_Click(object sender, EventArgs e)
            {
            // Validate paths before accepting
            string whisperPath = txtWhisperModelPath.Text.Trim();
            string llmPath = txtLLMModelPath.Text.Trim();
            bool whisperValid = true;
            bool llmValid = true;

            if (string.IsNullOrWhiteSpace(whisperPath) || !File.Exists(whisperPath))
                {
                MessageBox.Show("Whisper model path is invalid or file does not exist.", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtWhisperModelPath.Focus();
                whisperValid = false;
                }

            // LLM path can be empty if user doesn't want to use an LLM
            if (!string.IsNullOrWhiteSpace(llmPath) && !File.Exists(llmPath))
                {
                MessageBox.Show("LLM model path is invalid or file does not exist (leave empty to not use LLM).", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtLLMModelPath.Focus();
                llmValid = false;
                }

            if (whisperValid && llmValid)
                {
                SelectedWhisperModelPath = whisperPath;
                SelectedLLMModelPath = string.IsNullOrWhiteSpace(llmPath) ? string.Empty : llmPath; // Store empty if not set
                this.DialogResult = DialogResult.OK;
                this.Close();
                }
            // If not valid, DialogResult is not set to OK, so form stays open or btnCancel handles it
            }

        private void btnCancel_Click(object sender, EventArgs e)
            {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
            }
        }
    }


