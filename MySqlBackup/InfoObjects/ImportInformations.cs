namespace MySql.Data.MySqlClient
{
    public class ImportInformations
    {
        private string _databaseDefaultCharSet = "";
        private int _interval = 100;
        private string _targetDatabase = "";

        /// <summary>
        ///     Gets or Sets a value indicates whether the Imported Dump File is encrypted.
        /// </summary>
        public bool EnableEncryption = false;

        /// <summary>
        ///     Sets the password used to decrypt the exported dump file.
        /// </summary>
        public string EncryptionPassword = "";

        /// <summary>
        ///     Gets or Sets the file path used to log error messages.
        /// </summary>
        public string ErrorLogFile = "";

        /// <summary>
        ///     Gets or Sets a value indicates whether SQL errors occurs in import process should be ignored.
        /// </summary>
        public bool IgnoreSqlError = false;

        /// <summary>
        ///     Gets or Sets a value indicates the interval of time (in miliseconds) to raise the event of ExportProgressChanged.
        /// </summary>
        public int IntervalForProgressReport
        {
            get
            {
                return _interval == 0 ? 100 : _interval;
            }
            set { _interval = value; }
        }

        /// <summary>
        ///     Gets or Sets the name of target database.
        /// </summary>
        public string TargetDatabase
        {
            get { return (_targetDatabase + "").Trim(); }
            set { _targetDatabase = value; }
        }

        /// <summary>
        ///     Gets or Sets the default character set of the target database. This will only take effect when targetting new
        ///     non-existed database.
        /// </summary>
        public string DatabaseDefaultCharSet
        {
            get { return (_databaseDefaultCharSet + "").Trim(); }
            set { _databaseDefaultCharSet = value; }
        }
    }
}