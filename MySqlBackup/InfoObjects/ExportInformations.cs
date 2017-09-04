using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.Data.MySqlClient
{
    /// <summary>
    ///     Informations and Settings of MySQL Database Export Process
    /// </summary>
    public class ExportInformations
    {
        private const string Delimiter = "|";
        private List<string> _documentFooters;

        private List<string> _documentHeaders;
        private int _interval = 50;

        private List<string> _lstExcludeTables;

        /// <summary>
        ///     Gets or Sets a value indicates whether the SQL statement of "CREATE DATABASE" should added into dump file.
        /// </summary>
        public bool AddCreateDatabase = false;

        /// <summary>
        ///     Gets or Sets a value indicates whether the Exported Dump File should be encrypted. Enabling encryption will slow
        ///     down the whole process.
        /// </summary>
        public bool EnableEncryption = false;

        /// <summary>
        ///     Sets the password used to encrypt the exported dump file.
        /// </summary>
        public string EncryptionPassword = "";

        /// <summary>
        ///     Gets or Sets a value indicates whether the Stored Events should be exported.
        /// </summary>
        public bool ExportEvents = true;

        /// <summary>
        ///     Gets or Sets a value indicates whether the Stored Functions should be exported.
        /// </summary>
        public bool ExportFunctions = true;

        /// <summary>
        ///     Gets or Sets a value indicates whether the Stored Procedures should be exported.
        /// </summary>
        public bool ExportProcedures = true;

        /// <summary>
        ///     Gets or Sets a value indicates whether the exported Scripts (Procedure, Functions, Events, Triggers, Events) should
        ///     exclude DEFINER.
        /// </summary>
        public bool ExportRoutinesWithoutDefiner = true;

        /// <summary>
        ///     Gets or Sets a value indicates whether the Rows should be exported.
        /// </summary>
        public bool ExportRows = true;

        /// <summary>
        ///     Gets or Sets a value indicates whether the Table Structure (CREATE TABLE) should be exported.
        /// </summary>
        public bool ExportTableStructure = true;

        /// <summary>
        ///     Gets or Sets a value indicates whether the Stored Triggers should be exported.
        /// </summary>
        public bool ExportTriggers = true;

        /// <summary>
        ///     Gets or Sets a value indicates whether the Stored Views should be exported.
        /// </summary>
        public bool ExportViews = true;

        /// <summary>
        ///     Gets or Sets a value indicates whether the totals of rows should be counted before export process commence. The
        ///     value of total rows is used for progress reporting. Extra time is needed to get the total rows. Sets this value to
        ///     FALSE if not applying progress reporting.
        /// </summary>
        public bool GetTotalRowsBeforeExport = true;

        /// <summary>
        ///     Gets or Sets the maximum length for combining multiple INSERTs into single sql. Default value is 5MB. Only applies
        ///     if RowsExportMode = "INSERT" or "INSERTIGNORE" or "REPLACE". This value will be ignored if RowsExportMode =
        ///     ONDUPLICATEKEYUPDATE or UPDATE.
        /// </summary>
        public int MaxSqlLength = 5 * 1024 * 1024;

        /// <summary>
        ///     Gets or Sets a value indicates whether the Dump Time should recorded in dump file.
        /// </summary>
        public bool RecordDumpTime = true;

        /// <summary>
        ///     Gets or Sets a value indicates whether the value of auto-increment of each table should be reset to 1.
        /// </summary>
        public bool ResetAutoIncrement = false;

        /// <summary>
        ///     Gets or Sets a enum value indicates how the rows of each table should be exported. INSERT = The default option.
        ///     Recommended if exporting to a new database. If the primary key existed, the process will halt; INSERT IGNORE = If
        ///     the primary key existed, skip it; REPLACE = If the primary key existed, delete the row and insert new data;
        ///     OnDuplicateKeyUpdate = If the primary key existed, update the row. If all fields are primary keys, it will change
        ///     to INSERT IGNORE; UPDATE = If the primary key is not existed, skip it and if all the fields are primary key, no
        ///     rows will be exported.
        /// </summary>
        public RowsDataExportMode RowsExportMode = RowsDataExportMode.Insert;

        /// <summary>
        ///     Gets or Sets a value indicates whether the rows dump should be wrapped with transaction. Recommended to set this
        ///     value to FALSE if using RowsExportMode = "INSERT" or "INSERTIGNORE" or "REPLACE", else TRUE.
        /// </summary>
        public bool WrapWithinTransaction = false;

        /// <summary>
        ///     Gets or Sets the tables (black list) that will be excluded for export. The rows of the these tables will not be
        ///     exported too.
        /// </summary>
        public List<string> ExcludeTables
        {
            get => _lstExcludeTables ?? (_lstExcludeTables = new List<string>());
            set => _lstExcludeTables = value;
        }

        /// <summary>
        ///     Gets or Sets the list of tables that will be exported. If none, all tables will be exported.
        /// </summary>
        public List<string> TablesToBeExportedList
        {
            get { return TablesToBeExportedDic.Select(kv => kv.Key).ToList(); }
            set
            {
                throw new NotSupportedException("Setting this creates the generated columns problem");
                TablesToBeExportedDic.Clear();
                foreach (var s in value)
                    TablesToBeExportedDic[s] = $"SELECT * FROM `{s}`;";
            }
        }

        /// <summary>
        ///     Gets or Sets the tables that will be exported with custom SELECT defined. If none or empty, all tables and rows
        ///     will be exported. Key = Table's Name. Value = Custom SELECT Statement. Example 1: SELECT * FROM `product` WHERE
        ///     `category` = 1; Example 2: SELECT `name`,`description` FROM `product`;
        /// </summary>
        public Dictionary<string, string> TablesToBeExportedDic { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     Gets or Sets a value indicates the interval of time (in miliseconds) to raise the event of ExportProgressChanged.
        /// </summary>
        public int IntervalForProgressReport
        {
            get => _interval == 0 ? 100 : _interval;
            set => _interval = value;
        }

        /// <summary>
        ///     Gets or Sets the delimiter used for exporting Procedures, Functions, Events and Triggers. Default delimiter is "|".
        /// </summary>
        public static string ScriptsDelimiter => string.IsNullOrEmpty(Delimiter) ? "|" : Delimiter;

        /// <summary>
        ///     Gets the list of document headers.
        /// </summary>
        /// <param name="cmd">The MySqlCommand that will be used to retrieve the database default character set.</param>
        /// <returns>List of document headers.</returns>
        public List<string> GetDocumentHeaders(MySqlCommand cmd)
        {
            if (_documentHeaders != null) return _documentHeaders;
            var databaseCharSet = QueryExpress.ExecuteScalarStr(cmd, "SHOW variables LIKE 'character_set_database';",
                1);
            _documentHeaders = new List<string>
            {
                "/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;",
                "/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;",
                "/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;",
                $"/*!40101 SET NAMES {databaseCharSet} */;",
                "/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;",
                "/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;",
                "/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;",
                "/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;"
            };

            //_documentHeaders.Add("/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;");
            //_documentHeaders.Add("/*!40103 SET TIME_ZONE='+00:00' */;");

            return _documentHeaders;
        }

        /// <summary>
        ///     Sets the document headers.
        /// </summary>
        /// <param name="lstHeaders">List of document headers</param>
        public void SetDocumentHeaders(List<string> lstHeaders)
        {
            _documentHeaders = lstHeaders;
        }

        /// <summary>
        ///     Gets the document footers.
        /// </summary>
        /// <returns>List of document footers.</returns>
        public List<string> GetDocumentFooters()
        {
            return _documentFooters ?? (_documentFooters = new List<string>
            {
                "/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;",
                "/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;",
                "/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;",
                "/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;",
                "/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;",
                "/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;",
                "/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;"
            });
        }

        /// <summary>
        ///     Sets the document footers.
        /// </summary>
        /// <param name="lstFooters">List of document footers.</param>
        public void SetDocumentFooters(List<string> lstFooters)
        {
            _documentFooters = lstFooters;
        }
    }
}