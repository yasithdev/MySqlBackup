using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace MySqlBackupTestApp
{
    public partial class FormTestSimple : Form
    {
        public FormTestSimple()
        {
            InitializeComponent();
        }

        private void btImport_Click(object sender, EventArgs e)
        {
            if (!Program.SourceFileExists())
                return;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(Program.ConnectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        using (var mb = new MySqlBackup(cmd))
                        {
                            cmd.Connection = conn;
                            conn.Open();

                            mb.ImportFromFile(Program.TargetFile);

                            conn.Close();
                        }
                    }
                }

                MessageBox.Show("Done.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btExport_Click(object sender, EventArgs e)
        {
            if (!Program.TargetDirectoryIsValid())
                return;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(Program.ConnectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        using (var mb = new MySqlBackup(cmd))
                        {
                            cmd.Connection = conn;
                            conn.Open();

                            mb.ExportToFile(Program.TargetFile);

                            conn.Close();
                        }
                    }
                }

                MessageBox.Show("Done.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}