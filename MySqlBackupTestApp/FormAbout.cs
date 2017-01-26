using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace MySqlBackupTestApp
{
    public partial class FormAbout : Form
    {
        public FormAbout()
        {
            InitializeComponent();

            lbVersion.Text += "\r\n\r\nCurrent Loaded MySqlBackup.DLL Version: " + MySqlBackup.Version;
        }
    }
}