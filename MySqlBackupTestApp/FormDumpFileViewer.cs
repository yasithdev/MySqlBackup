﻿using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MySqlBackupTestApp
{
    public partial class FormDumpFileViewer : Form
    {
        public FormDumpFileViewer()
        {
            InitializeComponent();
            textBox1.Text = "";
            tsFile.Text = "";
            tsStatus.Text = "(No file loaded)";
        }

        public void OpenTargetFile()
        {
            OpenFile(Program.TargetFile);
        }

        private void OpenFile(string file)
        {
            if (file == "")
            {
                textBox1.Text = "";
                tsFile.Text = "";
                tsStatus.Text = "(No file loaded)";
                return;
            }

            if (!File.Exists(file))
            {
                tsFile.Text = "";
                tsStatus.Text = "(File not exists)";
                MessageBox.Show("File not exists:\r\n" + file, "Open", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                tsStatus.Text = "(Please wait... File is loading...)";
                Refresh();
                SuspendLayout();
                textBox1.Text = File.ReadAllText(file);
                tsFile.Text = file;
                tsStatus.Text = "(File Loaded)";
                ResumeLayout(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            tsStatus.Text = "(Editing)";
        }

        private void tsOpen_Click(object sender, EventArgs e)
        {
            var of = new OpenFileDialog();
            if (Program.DefaultFolder != "")
                of.InitialDirectory = Program.DefaultFolder;
            if (of.ShowDialog() == DialogResult.OK)
                OpenFile(of.FileName);
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            if (tsFile.Text == "")
            {
                MessageBox.Show("No file to save.");
                return;
            }

            File.WriteAllText(tsFile.Text, textBox1.Text, new UTF8Encoding(false));
            tsStatus.Text = "(Saved)";
        }

        private void tsSaveAs_Click(object sender, EventArgs e)
        {
            var sf = new SaveFileDialog();
            if (sf.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sf.FileName, textBox1.Text, new UTF8Encoding(false));
                tsStatus.Text = "(Saved)";
                tsFile.Text = sf.FileName;
                Program.DefaultFolder = Path.GetDirectoryName(tsFile.Text);
            }
        }

        private void tsClose_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            tsStatus.Text = "(No file loaded)";
            tsFile.Text = "";
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                textBox1.SelectAll();
                e.SuppressKeyPress = true;
            }
        }
    }
}