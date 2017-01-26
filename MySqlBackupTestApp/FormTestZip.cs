using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace MySqlBackupTestApp
{
    public partial class FormTestZip : Form
    {
        public FormTestZip()
        {
            InitializeComponent();
        }

        private void btExportMemoryZip_Click(object sender, EventArgs e)
        {
            try
            {
                var f = new SaveFileDialog();
                f.Filter = "Zip|*.zip";
                f.FileName = "ZipTest " + DateTime.Now.ToString("yyyyMMdd HHmmss") + ".zip";
                if (f.ShowDialog() != DialogResult.OK)
                    return;

                var zipFilePath = f.FileName;
                var zipFileName = "SqlDump.sql";

                using (var ms = new MemoryStream())
                {
                    using (TextWriter tw = new StreamWriter(ms, new UTF8Encoding(false)))
                    {
                        using (MySqlConnection conn = new MySqlConnection(Program.ConnectionString))
                        {
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                using (var mb = new MySqlBackup(cmd))
                                {
                                    cmd.Connection = conn;
                                    conn.Open();

                                    mb.ExportToTextWriter(tw);
                                    conn.Close();

                                    using (var zip = ZipStorer.Create(zipFilePath, "MySQL Dump"))
                                    {
                                        ms.Position = 0;
                                        zip.AddStream(ZipStorer.Compression.Deflate, zipFileName, ms, DateTime.Now,
                                            "MySQL Dump");
                                    }
                                }
                            }
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

        private void btImportUnzipMemoryStream_Click(object sender, EventArgs e)
        {
            try
            {
                var f = new OpenFileDialog();
                f.Filter = "Zip|*.zip";
                if (f.ShowDialog() != DialogResult.OK)
                    return;

                var file = f.FileName;

                using (var ms = new MemoryStream())
                {
                    using (var zip = ZipStorer.Open(file, FileAccess.Read))
                    {
                        var dir = zip.ReadCentralDir();
                        zip.ExtractFile(dir[0], ms);

                        ms.Position = 0;
                        using (TextReader tr = new StreamReader(ms))
                        {
                            using (MySqlConnection conn = new MySqlConnection(Program.ConnectionString))
                            {
                                using (MySqlCommand cmd = new MySqlCommand())
                                {
                                    using (var mb = new MySqlBackup(cmd))
                                    {
                                        cmd.Connection = conn;
                                        conn.Open();

                                        mb.ImportFromTextReader(tr);

                                        conn.Close();
                                    }
                                }
                            }
                        }
                    }
                }
                MessageBox.Show("Finished.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btImportUnzipFile_Click(object sender, EventArgs e)
        {
            try
            {
                var of = new OpenFileDialog();
                of.Filter = "Zip|*.zip";
                of.Title = "Select the Zip file";
                of.Multiselect = false;
                if (of.ShowDialog() != DialogResult.OK)
                    return;

                var zipfile = of.FileName;

                var f = new FolderBrowserDialog();
                f.Description = "Extract the dump file to which folder?";
                if (f.ShowDialog() != DialogResult.OK)
                    return;

                var folder = f.SelectedPath;
                var dumpFile = "";

                using (var zip = ZipStorer.Open(zipfile, FileAccess.Read))
                {
                    var dir = zip.ReadCentralDir();
                    dumpFile = folder + "\\" + dir[0].FilenameInZip;
                    zip.ExtractFile(dir[0], dumpFile);
                }

                using (MySqlConnection conn = new MySqlConnection(Program.ConnectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        using (var mb = new MySqlBackup(cmd))
                        {
                            cmd.Connection = conn;
                            conn.Open();

                            mb.ImportFromFile(dumpFile);

                            conn.Close();
                        }
                    }
                }

                MessageBox.Show("Finished.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btExportFileZip_Click(object sender, EventArgs e)
        {
            try
            {
                var f = new FolderBrowserDialog();
                f.Description = "Select a folder to save the dump file and zip file.";
                if (f.ShowDialog() != DialogResult.OK)
                    return;

                var timenow = DateTime.Now.ToString("yyyyMMddHHmmss");
                var folder = f.SelectedPath;
                var filename = "dump" + timenow + ".sql";
                var fileDump = f.SelectedPath + "\\" + filename;
                var fileZip = f.SelectedPath + "\\dumpzip" + timenow + ".zip";

                using (MySqlConnection conn = new MySqlConnection(Program.ConnectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        using (var mb = new MySqlBackup(cmd))
                        {
                            cmd.Connection = conn;
                            conn.Open();

                            mb.ExportToFile(fileDump);

                            conn.Close();
                        }
                    }
                }

                using (var zip = ZipStorer.Create(fileZip, "MySQL Dump"))
                {
                    zip.AddFile(ZipStorer.Compression.Deflate, fileDump, filename, "MySQL Dump");
                }

                MessageBox.Show("Finished.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}