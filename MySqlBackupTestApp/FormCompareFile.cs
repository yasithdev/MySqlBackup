using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace MySqlBackupTestApp
{
    public partial class FormCompareFile : Form
    {
        private string file1 = "";
        private bool file1Opened;
        private string file2 = "";
        private bool file2Opened;
        private string hash1 = "";
        private string hash2 = "";

        public FormCompareFile()
        {
            InitializeComponent();
        }

        private void button_OpenFile1_Click(object sender, EventArgs e)
        {
            file1Opened = GetHash(ref file1, ref hash1);
            lbFilePath1.Text = "File: " + file1;
            lbSHA1.Text = "SHA256 Checksum: " + hash1;
            CompareFile();
        }

        private void button_OpenFile2_Click(object sender, EventArgs e)
        {
            file2Opened = GetHash(ref file2, ref hash2);
            lbFilePath2.Text = "File: " + file2;
            lbSHA2.Text = "SHA256 Checksum: " + hash2;
            CompareFile();
        }

        private bool GetHash(ref string file, ref string hash)
        {
            try
            {
                var f = new OpenFileDialog();
                if (DialogResult.OK == f.ShowDialog())
                {
                    file = f.FileName;
                    var ba = File.ReadAllBytes(f.FileName);
                    hash = CryptoExpress.Sha256Hash(ba);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("File not valid.\r\n\r\n" + ex);
                return false;
            }
        }

        private void CompareFile()
        {
            if (file1Opened && file2Opened)
                if (hash1 == hash2)
                {
                    lbResult.Text = "Match. 100% same content.";
                    lbResult.ForeColor = Color.DarkGreen;
                }
                else
                {
                    lbResult.Text = "Not match. Both files are not same.";
                    lbResult.ForeColor = Color.Red;
                }
            else
                lbResult.Text = "";
        }

        private void btInfo_Click(object sender, EventArgs e)
        {
            var a =
                @"This function can be used to find out both EXPORT and IMPORT are working as expected or not by comparing the results.

Instructions:

1. Build the database and fill some data.
2. Export into first dump file.
3. Drop the database.
4. Import from first dump file.
5. Export again into second dump file.
6. Compare the first and second dump by using this SHA256 checksum.
7. If both checksums are match, this will prove that both EXPORT and IMPORT are working good.";
            MessageBox.Show(a, "Info");
        }
    }
}