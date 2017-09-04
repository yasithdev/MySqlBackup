using System;
using System.IO;
using System.Windows.Forms;

namespace MySqlBackupTestApp
{
    internal static class Program
    {
        private static string _connectionString = "";
        public static string DefaultFolder = "";
        public static string TargetFile = "";

        public static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                    throw new Exception("Connection string is empty.");
                return _connectionString;
            }
            set => _connectionString = value;
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormMain());
        }

        public static bool TargetDirectoryIsValid()
        {
            try
            {
                var dir = Path.GetDirectoryName(TargetFile);

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Specify path is not valid. Press [Export As] to specify a valid file path." + Environment.NewLine +
                    Environment.NewLine + ex.Message, "Invalid Directory", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }
        }

        public static bool SourceFileExists()
        {
            if (!File.Exists(TargetFile))
            {
                MessageBox.Show(
                    "File is not exists. Press [Select File] to choose a SQL Dump file." + Environment.NewLine +
                    Environment.NewLine + TargetFile, "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }
    }
}