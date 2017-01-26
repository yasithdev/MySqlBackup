using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace MySqlBackupTestApp
{
    public partial class FormTestEncryptDecrypt : Form
    {
        public FormTestEncryptDecrypt()
        {
            InitializeComponent();
        }

        private void btSourceFile_Click(object sender, EventArgs e)
        {
            var f = new OpenFileDialog();
            if (DialogResult.OK == f.ShowDialog())
                txtSource.Text = f.FileName;
        }

        private void btOutputFile_Click(object sender, EventArgs e)
        {
            var f = new SaveFileDialog();
            if (DialogResult.OK == f.ShowDialog())
                txtOutput.Text = f.FileName;
        }

        private void btDecrypt_Click(object sender, EventArgs e)
        {
            try
            {
                using (var mb = new MySqlBackup())
                {
                    mb.DecryptDumpFile(txtSource.Text, txtOutput.Text, txtPwd.Text);
                }
                MessageBox.Show("Done");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btEncrypt_Click(object sender, EventArgs e)
        {
            try
            {
                using (var mb = new MySqlBackup())
                {
                    mb.EncryptDumpFile(txtSource.Text, txtOutput.Text, txtPwd.Text);
                }
                MessageBox.Show("Done");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btSwitch_Click(object sender, EventArgs e)
        {
            var f1 = txtSource.Text;
            var f2 = txtOutput.Text;
            txtSource.Text = f2;
            txtOutput.Text = f1;
        }
    }
}