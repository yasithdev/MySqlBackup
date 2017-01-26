using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace MySqlBackupTestApp
{
    public partial class FormTestExportImportString : Form
    {
        public FormTestExportImportString()
        {
            InitializeComponent();
        }

        private void btImport_Click(object sender, EventArgs e)
        {
            try
            {
                using (var conn = new MySqlConnection(Program.ConnectionString))
                {
                    using (var cmd = new MySqlCommand())
                    {
                        using (var mb = new MySqlBackup(cmd))
                        {
                            cmd.Connection = conn;
                            conn.Open();

                            mb.ImportFromString(textBox1.Text);

                            conn.Close();
                        }
                    }
                }
                MessageBox.Show("Import completed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btExport_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            textBox1.Refresh();
            Refresh();
            try
            {
                using (var conn = new MySqlConnection(Program.ConnectionString))
                {
                    using (var cmd = new MySqlCommand())
                    {
                        using (var mb = new MySqlBackup(cmd))
                        {
                            cmd.Connection = conn;
                            conn.Open();

                            textBox1.Text = mb.ExportToString();

                            conn.Close();
                        }
                    }
                }
                MessageBox.Show("Export completed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}