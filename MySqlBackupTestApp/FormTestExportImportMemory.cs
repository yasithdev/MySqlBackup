using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace MySqlBackupTestApp
{
    public partial class FormTestExportImportMemory : Form
    {
        private byte[] _ba;

        public FormTestExportImportMemory()
        {
            InitializeComponent();
            ClearMemory();
        }

        private void btExport_Click(object sender, EventArgs e)
        {
            try
            {
                var ms = new MemoryStream();
                var conn = new MySqlConnection(Program.ConnectionString);
                var cmd = new MySqlCommand();
                var mb = new MySqlBackup(cmd);
                cmd.Connection = conn;
                conn.Open();
                mb.ExportToMemoryStream(ms);
                conn.Close();
                LoadIntoMemory(ms.ToArray());
                MessageBox.Show("Finished.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btImport_Click(object sender, EventArgs e)
        {
            try
            {
                if (_ba == null || _ba.Length == 0)
                {
                    MessageBox.Show("No content is loaded into memory, cannot perform Import/Restore task.");
                    return;
                }

                var ms = new MemoryStream(_ba);
                var conn = new MySqlConnection(Program.ConnectionString);
                var cmd = new MySqlCommand();
                var mb = new MySqlBackup(cmd);
                cmd.Connection = conn;
                conn.Open();
                mb.ImportFromMemoryStream(ms);
                conn.Close();
                MessageBox.Show("Finished.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void LoadIntoMemory(byte[] ba)
        {
            if (ba == null || ba.Length == 0)
            {
                ClearMemory();
            }
            else
            {
                _ba = ba;
                lbStatus.Text = "Loaded into memory.";
                lbStatus.ForeColor = Color.DarkGreen;
                btImport.Enabled = true;
            }
        }

        private void ClearMemory()
        {
            _ba = null;
            lbStatus.Text = "No dump content is loaded in memory.";
            lbStatus.ForeColor = Color.Black;
            btImport.Enabled = false;
        }

        private void btLoadFile_Click(object sender, EventArgs e)
        {
            if (!Program.SourceFileExists())
                return;

            var ba = File.ReadAllBytes(Program.TargetFile);

            LoadIntoMemory(ba);

            MessageBox.Show("Loaded into memory.");
        }

        private void btClear_Click(object sender, EventArgs e)
        {
            ClearMemory();
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (_ba == null || _ba.Length == 0)
                {
                    MessageBox.Show("No content is loaded into memory, nothing to save.");
                    return;
                }

                var f = new SaveFileDialog();
                f.Filter = "*.sql|*.sql|*.*|*.*";
                f.FileName = "MemoryDump.sql";
                if (f.ShowDialog() == DialogResult.OK)
                    File.WriteAllBytes(f.FileName, _ba);
                MessageBox.Show("Done.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}