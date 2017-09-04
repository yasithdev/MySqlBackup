using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Timers;

namespace MySql.Data.MySqlClient
{
    public class MySqlBackup : IDisposable
    {
        public delegate void ExportComplete(object sender, ExportCompleteArgs e);

        public delegate void ExportProgressChange(object sender, ExportProgressArgs e);

        public delegate void GetTotalRowsProgressChange(object sender, GetTotalRowsArgs e);

        public delegate void ImportComplete(object sender, ImportCompleteArgs e);

        public delegate void ImportProgressChange(object sender, ImportProgressArgs e);

        public enum ProcessEndType
        {
            UnknownStatus,
            Complete,
            Cancelled,
            Error
        }

        public const string Version = "2.0.9.2";

        public readonly ExportInformations ExportInfo = new ExportInformations();
        public readonly ImportInformations ImportInfo = new ImportInformations();

        private long _currentBytes;
        private ProcessType _currentProcess;
        private long _currentRowIndexInAllTable;
        private long _currentRowIndexInCurrentTable;
        private int _currentTableIndex;

        private string _currentTableName = "";

        private string _delimiter = "";
        private bool _isNewDatabase;
        private MySqlScript _mySqlScript;
        private bool _nameIsSet;
        private ProcessEndType _processCompletionType;
        private StringBuilder _sbImport;

        private string _sha512HashedPassword = "";
        private bool _stopProcess;
        private TextReader _textReader;
        private TextWriter _textWriter;
        private DateTime _timeEnd;
        private Timer _timerReport;
        private DateTime _timeStart;
        private long _totalBytes;
        private long _totalRowsInAllTables;
        private long _totalRowsInCurrentTable;
        private int _totalTables;

        private Encoding _utf8WithoutBom;

        public MySqlBackup()
        {
            InitializeComponents();
        }

        public MySqlBackup(MySqlCommand cmd)
        {
            Command = cmd;
            InitializeComponents();
        }

        public Exception LastError { get; private set; }

        /// <summary>
        ///     Gets the information about the connected database.
        /// </summary>
        private MySqlDatabase Database { get; } = new MySqlDatabase();

        /// <summary>
        ///     Gets the information about the connected MySQL server.
        /// </summary>
        private MySqlServer Server { get; set; } = new MySqlServer();

        /// <summary>
        ///     Gets or Sets the instance of MySqlCommand.
        /// </summary>
        public MySqlCommand Command { get; set; }

        public void Dispose()
        {
            try
            {
                Database.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                Server = null;
            }
            catch
            {
                // ignored
            }

            try
            {
                _mySqlScript = null;
            }
            catch
            {
                // ignored
            }
        }

        public event ExportProgressChange ExportProgressChanged;
        public event ExportComplete ExportCompleted;
        public event ImportProgressChange ImportProgressChanged;
        public event ImportComplete ImportCompleted;
        public event GetTotalRowsProgressChange GetTotalRowsProgressChanged;

        private void InitializeComponents()
        {
            Database.GetTotalRowsProgressChanged += _database_GetTotalRowsProgressChanged;

            _timerReport = new Timer();
            _timerReport.Elapsed += timerReport_Elapsed;
            _utf8WithoutBom = new UTF8Encoding(false);
        }

        private void _database_GetTotalRowsProgressChanged(object sender, GetTotalRowsArgs e)
        {
            GetTotalRowsProgressChanged?.Invoke(this, e);
        }

        private void ReportEndProcess()
        {
            _timeEnd = DateTime.Now;

            StopAllProcess();

            switch (_currentProcess)
            {
                case ProcessType.Export:
                {
                    ReportProgress();
                    if (ExportCompleted == null) return;
                    var arg = new ExportCompleteArgs(_timeStart, _timeEnd, _processCompletionType, LastError);
                    ExportCompleted(this, arg);
                    break;
                }
                case ProcessType.Import:
                {
                    _currentBytes = _totalBytes;

                    ReportProgress();
                    if (ImportCompleted == null) return;
                    var arg = new ImportCompleteArgs();
                    ImportCompleted(this, arg);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void timerReport_Elapsed(object sender, ElapsedEventArgs e)
        {
            ReportProgress();
        }

        private void ReportProgress()
        {
            switch (_currentProcess)
            {
                case ProcessType.Export:
                {
                    if (ExportProgressChanged == null) return;
                    var arg = new ExportProgressArgs(_currentTableName, _totalRowsInCurrentTable, _totalRowsInAllTables,
                        _currentRowIndexInCurrentTable, _currentRowIndexInAllTable, _totalTables, _currentTableIndex);
                    ExportProgressChanged(this, arg);
                }
                    break;
                case ProcessType.Import:
                {
                    if (ImportProgressChanged == null) return;
                    var arg = new ImportProgressArgs(_currentBytes, _totalBytes);
                    ImportProgressChanged(this, arg);
                }
                    break;
            }
        }

        public void StopAllProcess()
        {
            _stopProcess = true;
            _timerReport.Stop();
        }

        private enum ProcessType
        {
            Export,
            Import
        }

        private enum NextImportAction
        {
            Ignore,
            SetNames,
            CreateNewDatabase,
            AppendLine,
            ChangeDelimiter,
            AppendLineAndExecute
        }

        #region Export

        public string ExportToString()
        {
            using (var ms = new MemoryStream())
            {
                ExportToMemoryStream(ms);
                ms.Position = 0L;
                using (var thisReader = new StreamReader(ms))
                {
                    return thisReader.ReadToEnd();
                }
            }
        }

        public void ExportToFile(string filePath)
        {
            using (_textWriter = new StreamWriter(filePath, false, _utf8WithoutBom))
            {
                ExportStart();
                _textWriter.Close();
            }
        }

        public void ExportToTextWriter(TextWriter tw)
        {
            _textWriter = tw;
            ExportStart();
        }

        public void ExportToMemoryStream(MemoryStream ms, bool resetMemoryStreamPosition = true)
        {
            if (resetMemoryStreamPosition)
            {
                if (ms == null)
                    ms = new MemoryStream();
                if (ms.Length > 0)
                    ms = new MemoryStream();
                ms.Position = 0L;
            }

            _textWriter = new StreamWriter(ms, _utf8WithoutBom);
            ExportStart();
        }

        private void ExportStart()
        {
            try
            {
                Export_InitializeVariables();

                var stage = 1;

                while (stage < 11)
                {
                    if (_stopProcess) break;

                    switch (stage)
                    {
                        case 1:
                            Export_BasicInfo();
                            break;
                        case 2:
                            Export_CreateDatabase();
                            break;
                        case 3:
                            Export_DocumentHeader();
                            break;
                        case 4:
                            Export_TableRows();
                            break;
                        case 5:
                            Export_Functions();
                            break;
                        case 6:
                            Export_Procedures();
                            break;
                        case 7:
                            Export_Events();
                            break;
                        case 8:
                            Export_Views();
                            break;
                        case 9:
                            Export_Triggers();
                            break;
                        case 10:
                            Export_DocumentFooter();
                            break;
                    }

                    _textWriter.Flush();

                    stage = stage + 1;
                }

                _processCompletionType = _stopProcess ? ProcessEndType.Cancelled : ProcessEndType.Complete;
            }
            catch (Exception ex)
            {
                LastError = ex;
                StopAllProcess();
                throw;
            }

            ReportEndProcess();
        }

        private void Export_InitializeVariables()
        {
            if (Command == null)
                throw new Exception("MySqlCommand is not initialized. Object not set to an instance of an object.");

            if (Command.Connection == null)
                throw new Exception(
                    "MySqlCommand.Connection is not initialized. Object not set to an instance of an object.");

            if (Command.Connection.State != ConnectionState.Open)
                throw new Exception("MySqlCommand.Connection is not opened.");

            _timeStart = DateTime.Now;

            _stopProcess = false;
            _processCompletionType = ProcessEndType.UnknownStatus;
            _currentProcess = ProcessType.Export;
            LastError = null;
            _timerReport.Interval = ExportInfo.IntervalForProgressReport;
            GetSha512HashFromPassword(ExportInfo.EncryptionPassword);

            Database.GetDatabaseInfo(Command, ExportInfo.GetTotalRowsBeforeExport);
            Server.GetServerInfo(Command);
            _currentTableName = "";
            _totalRowsInCurrentTable = 0;
            _totalRowsInAllTables = Database.TotalRows;
            _currentRowIndexInCurrentTable = 0;
            _currentRowIndexInAllTable = 0;
            _totalTables = 0;
            _currentTableIndex = 0;
        }

        private void Export_BasicInfo()
        {
            Export_WriteComment($"MySqlBackup.NET {Version}");
            Export_WriteComment(ExportInfo.RecordDumpTime ? $"Dump Time: {_timeStart:yyyy-MM-dd HH:mm:ss}" : "");
            Export_WriteComment("--------------------------------------");
            Export_WriteComment($"Server version {Server.Version}");
            _textWriter.WriteLine();
        }

        private void Export_CreateDatabase()
        {
            if (!ExportInfo.AddCreateDatabase) return;

            Export_WriteComment("");
            Export_WriteComment($"Create schema {Database.Name}");
            Export_WriteComment("");
            _textWriter.WriteLine();
            Export_WriteLine(Database.CreateDatabaseSql);
            Export_WriteLine($"Use `{Database.Name}`;");
            _textWriter.WriteLine();
            _textWriter.WriteLine();
        }

        private void Export_DocumentHeader()
        {
            _textWriter.WriteLine();

            var lstHeaders = ExportInfo.GetDocumentHeaders(Command);
            if (lstHeaders.Count <= 0) return;

            foreach (var s in lstHeaders)
                Export_WriteLine(s);

            _textWriter.WriteLine();
            _textWriter.WriteLine();
        }

        private void Export_TableRows()
        {
            var dicTables = Export_GetTablesToBeExported();

            _totalTables = dicTables.Count;

            if (!ExportInfo.ExportTableStructure && !ExportInfo.ExportRows) return;
            if (ExportProgressChanged != null)
                _timerReport.Start();

            foreach (var kvTable in dicTables)
            {
                if (_stopProcess)
                    return;

                var tableName = kvTable.Key;
                var selectSql = kvTable.Value;

                var exclude = Export_ThisTableIsExcluded(tableName);
                if (exclude)
                    continue;

                _currentTableName = tableName;
                _currentTableIndex = _currentTableIndex + 1;
                _totalRowsInCurrentTable = Database.Tables[tableName].TotalRows;

                if (ExportInfo.ExportTableStructure)
                    Export_TableStructure(tableName);

                if (ExportInfo.ExportRows)
                    Export_Rows(tableName, selectSql);
            }
        }

        private bool Export_ThisTableIsExcluded(string tableName)
        {
            var tableNameLower = tableName.ToLower();

            foreach (var blacklistedTable in ExportInfo.ExcludeTables)
                if (blacklistedTable.ToLower() == tableNameLower)
                    return true;

            return false;
        }

        private void Export_TableStructure(string tableName)
        {
            if (_stopProcess)
                return;

            Export_WriteComment("");
            Export_WriteComment($"Definition of {tableName}");
            Export_WriteComment("");

            _textWriter.WriteLine();

            Export_WriteLine($"DROP TABLE IF EXISTS `{tableName}`;");

            Export_WriteLine(ExportInfo.ResetAutoIncrement
                ? Database.Tables[tableName].CreateTableSqlWithoutAutoIncrement
                : Database.Tables[tableName].CreateTableSql);

            _textWriter.WriteLine();

            _textWriter.Flush();
        }

        private Dictionary<string, string> Export_GetTablesToBeExported()
        {
            var dic = new Dictionary<string, string>();

            if (ExportInfo.TablesToBeExportedDic == null || ExportInfo.TablesToBeExportedDic.Count == 0)
                foreach (var table in Database.Tables)
                {
                    var columnstr = "";
                    foreach (var tableColumn in table.Columns)
                        if (!tableColumn.IsGenerated) columnstr += $"`{tableColumn.Name}`,";
                    columnstr = columnstr.TrimEnd(',');
                    dic[table.Name] = $"SELECT {columnstr} FROM `{table.Name}`;";
                }
            else
                foreach (var kv in ExportInfo.TablesToBeExportedDic)
                    dic[kv.Key] = kv.Value;

            return dic;
        }

        private void Export_Rows(string tableName, string selectSql)
        {
            Export_WriteComment("");
            Export_WriteComment($"Dumping data for table {tableName}");
            Export_WriteComment("");
            _textWriter.WriteLine();
            Export_WriteLine($"/*!40000 ALTER TABLE `{tableName}` DISABLE KEYS */;");

            if (ExportInfo.WrapWithinTransaction)
                Export_WriteLine("START TRANSACTION;");

            Export_RowsData(tableName, selectSql);

            if (ExportInfo.WrapWithinTransaction)
                Export_WriteLine("COMMIT;");

            Export_WriteLine($"/*!40000 ALTER TABLE `{tableName}` ENABLE KEYS */;");
            _textWriter.WriteLine();
            _textWriter.Flush();
        }

        private void Export_RowsData(string tableName, string selectSql)
        {
            _currentRowIndexInCurrentTable = 0L;

            if (ExportInfo.RowsExportMode == RowsDataExportMode.Insert ||
                ExportInfo.RowsExportMode == RowsDataExportMode.InsertIgnore ||
                ExportInfo.RowsExportMode == RowsDataExportMode.Replace)
                Export_RowsData_Insert_Ignore_Replace(tableName, selectSql);
            else if (ExportInfo.RowsExportMode == RowsDataExportMode.OnDuplicateKeyUpdate)
                Export_RowsData_OnDuplicateKeyUpdate(tableName, selectSql);
            else if (ExportInfo.RowsExportMode == RowsDataExportMode.Update)
                Export_RowsData_Update(tableName, selectSql);
        }

        private void Export_RowsData_Insert_Ignore_Replace(string tableName, string selectSql)
        {
            var table = Database.Tables[tableName];

            Command.CommandText = selectSql;
            var rdr = Command.ExecuteReader();

            string insertStatementHeader = null;

            var sb = new StringBuilder(ExportInfo.MaxSqlLength);

            while (rdr.Read())
            {
                if (_stopProcess)
                    return;

                _currentRowIndexInAllTable = _currentRowIndexInAllTable + 1;
                _currentRowIndexInCurrentTable = _currentRowIndexInCurrentTable + 1;

                if (insertStatementHeader == null)
                    insertStatementHeader = Export_GetInsertStatementHeader(ExportInfo.RowsExportMode, tableName, rdr);

                var sqlDataRow = Export_GetValueString(rdr, table);

                if (sb.Length == 0)
                {
                    sb.AppendLine(insertStatementHeader);
                    sb.Append(sqlDataRow);
                }
                else if (sb.Length + (long) sqlDataRow.Length < ExportInfo.MaxSqlLength)
                {
                    sb.AppendLine(",");
                    sb.Append(sqlDataRow);
                }
                else
                {
                    sb.AppendFormat(";");

                    Export_WriteLine(sb.ToString());
                    _textWriter.Flush();

                    sb = new StringBuilder(ExportInfo.MaxSqlLength);
                    sb.AppendLine(insertStatementHeader);
                    sb.Append(sqlDataRow);
                }
            }

            rdr.Close();

            if (sb.Length > 0)
                sb.Append(";");

            Export_WriteLine(sb.ToString());
            _textWriter.Flush();
        }

        private void Export_RowsData_OnDuplicateKeyUpdate(string tableName, string selectSql)
        {
            var table = Database.Tables[tableName];

            var allPrimaryField = true;
            foreach (var col in table.Columns)
                if (!col.IsPrimaryKey)
                {
                    allPrimaryField = false;
                    break;
                }

            Command.CommandText = selectSql;
            var rdr = Command.ExecuteReader();

            while (rdr.Read())
            {
                if (_stopProcess)
                    return;

                var sb = new StringBuilder();

                if (allPrimaryField)
                {
                    sb.Append(Export_GetInsertStatementHeader(RowsDataExportMode.InsertIgnore, tableName, rdr));
                    sb.Append(Export_GetValueString(rdr, table));
                }
                else
                {
                    sb.Append(Export_GetInsertStatementHeader(RowsDataExportMode.Insert, tableName, rdr));
                    sb.Append(Export_GetValueString(rdr, table));
                    sb.Append(" ON DUPLICATE KEY UPDATE ");
                    Export_GetUpdateString(rdr, table, sb);
                }

                sb.Append(";");

                Export_WriteLine(sb.ToString());
                _textWriter.Flush();
            }

            rdr.Close();
        }

        private void Export_RowsData_Update(string tableName, string selectSql)
        {
            var table = Database.Tables[tableName];

            var allPrimaryField = true;
            foreach (var col in table.Columns)
                if (!col.IsPrimaryKey)
                {
                    allPrimaryField = false;
                    break;
                }

            if (allPrimaryField)
                return;

            var allNonPrimaryField = true;
            foreach (var col in table.Columns)
                if (col.IsPrimaryKey)
                {
                    allNonPrimaryField = false;
                    break;
                }

            if (allNonPrimaryField)
                return;

            Command.CommandText = selectSql;
            var rdr = Command.ExecuteReader();

            while (rdr.Read())
            {
                if (_stopProcess)
                    return;

                var sb = new StringBuilder();
                sb.Append("UPDATE `");
                sb.Append(tableName);
                sb.Append("` SET ");

                Export_GetUpdateString(rdr, table, sb);

                sb.Append(" WHERE ");

                Export_GetConditionString(rdr, table, sb);

                sb.Append(";");

                Export_WriteLine(sb.ToString());

                _textWriter.Flush();
            }

            rdr.Close();
        }

        private static string Export_GetInsertStatementHeader(RowsDataExportMode rowsExportMode, string tableName,
            MySqlDataReader rdr)
        {
            if (rdr == null) throw new ArgumentNullException(nameof(rdr));
            var sb = new StringBuilder();

            switch (rowsExportMode)
            {
                case RowsDataExportMode.Insert:
                    sb.Append("INSERT INTO `");
                    break;
                case RowsDataExportMode.InsertIgnore:
                    sb.Append("INSERT IGNORE INTO `");
                    break;
                case RowsDataExportMode.Replace:
                    sb.Append("REPLACE INTO `");
                    break;
            }

            sb.Append(tableName);
            sb.Append("`(");

            for (var i = 0; i < rdr.FieldCount; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append("`");
                sb.Append(rdr.GetName(i));
                sb.Append("`");
            }

            sb.Append(") VALUES");
            return sb.ToString();
        }

        private static string Export_GetValueString(MySqlDataReader rdr, MySqlTable table)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < rdr.FieldCount; i++)
            {
                sb.AppendFormat(sb.Length == 0 ? "(" : ",");

                var columnName = rdr.GetName(i);
                var col = table.Columns[columnName];

                sb.Append(QueryExpress.ConvertToSqlFormat(rdr, i, true, true, col));
            }

            sb.AppendFormat(")");
            return sb.ToString();
        }

        private static void Export_GetUpdateString(MySqlDataReader rdr, MySqlTable table, StringBuilder sb)
        {
            var isFirst = true;

            for (var i = 0; i < rdr.FieldCount; i++)
            {
                var colName = rdr.GetName(i);

                var col = table.Columns[colName];

                if (!col.IsPrimaryKey)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        sb.Append(",");

                    sb.Append("`");
                    sb.Append(colName);
                    sb.Append("`=");
                    sb.Append(QueryExpress.ConvertToSqlFormat(rdr, i, true, true, col));
                }
            }
        }

        private static void Export_GetConditionString(MySqlDataReader rdr, MySqlTable table, StringBuilder sb)
        {
            var isFirst = true;

            for (var i = 0; i < rdr.FieldCount; i++)
            {
                var colName = rdr.GetName(i);

                var col = table.Columns[colName];

                if (col.IsPrimaryKey)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        sb.Append(" and ");

                    sb.Append("`");
                    sb.Append(colName);
                    sb.Append("`=");
                    sb.Append(QueryExpress.ConvertToSqlFormat(rdr, i, true, true, col));
                }
            }
        }

        private void Export_Procedures()
        {
            if (!ExportInfo.ExportProcedures || Database.Procedures.Count == 0)
                return;

            Export_WriteComment("");
            Export_WriteComment("Dumping procedures");
            Export_WriteComment("");
            _textWriter.WriteLine();

            foreach (var procedure in Database.Procedures)
            {
                if (_stopProcess)
                    return;

                Export_WriteLine(string.Format("DROP PROCEDURE IF EXISTS `{0}`;", procedure.Name));
                Export_WriteLine("DELIMITER " + ExportInformations.ScriptsDelimiter);

                if (ExportInfo.ExportRoutinesWithoutDefiner)
                    Export_WriteLine(procedure.CreateProcedureSqlWithoutDefiner + " " +
                                     ExportInformations.ScriptsDelimiter);
                else
                    Export_WriteLine(procedure.CreateProcedureSql + " " + ExportInformations.ScriptsDelimiter);

                Export_WriteLine("DELIMITER ;");
                _textWriter.WriteLine();
            }
            _textWriter.Flush();
        }

        private void Export_Functions()
        {
            if (!ExportInfo.ExportFunctions || Database.Functions.Count == 0)
                return;

            Export_WriteComment("");
            Export_WriteComment("Dumping functions");
            Export_WriteComment("");
            _textWriter.WriteLine();

            foreach (var function in Database.Functions)
            {
                if (_stopProcess)
                    return;

                Export_WriteLine(string.Format("DROP FUNCTION IF EXISTS `{0}`;", function.Name));
                Export_WriteLine("DELIMITER " + ExportInformations.ScriptsDelimiter);

                if (ExportInfo.ExportRoutinesWithoutDefiner)
                    Export_WriteLine(function.CreateFunctionSqlWithoutDefiner + " " +
                                     ExportInformations.ScriptsDelimiter);
                else
                    Export_WriteLine(function.CreateFunctionSql + " " + ExportInformations.ScriptsDelimiter);

                Export_WriteLine("DELIMITER ;");
                _textWriter.WriteLine();
            }

            _textWriter.Flush();
        }

        private void Export_Views()
        {
            if (!ExportInfo.ExportViews || Database.Views.Count == 0)
                return;

            Export_WriteComment("");
            Export_WriteComment("Dumping views");
            Export_WriteComment("");
            _textWriter.WriteLine();

            foreach (var view in Database.Views)
            {
                if (_stopProcess)
                    return;

                Export_WriteLine(string.Format("DROP TABLE IF EXISTS `{0}`;", view.Name));
                Export_WriteLine(string.Format("DROP VIEW IF EXISTS `{0}`;", view.Name));

                if (ExportInfo.ExportRoutinesWithoutDefiner)
                    Export_WriteLine(view.CreateViewSQLWithoutDefiner);
                else
                    Export_WriteLine(view.CreateViewSQL);

                _textWriter.WriteLine();
            }

            _textWriter.WriteLine();
            _textWriter.Flush();
        }

        private void Export_Events()
        {
            if (!ExportInfo.ExportEvents || Database.Events.Count == 0)
                return;

            Export_WriteComment("");
            Export_WriteComment("Dumping events");
            Export_WriteComment("");
            _textWriter.WriteLine();

            foreach (var e in Database.Events)
            {
                if (_stopProcess)
                    return;

                Export_WriteLine(string.Format("DROP EVENT IF EXISTS `{0}`;", e.Name));
                Export_WriteLine("DELIMITER " + ExportInformations.ScriptsDelimiter);

                if (ExportInfo.ExportRoutinesWithoutDefiner)
                    Export_WriteLine(e.CreateEventSqlWithoutDefiner + " " + ExportInformations.ScriptsDelimiter);
                else
                    Export_WriteLine(e.CreateEventSql + " " + ExportInformations.ScriptsDelimiter);

                Export_WriteLine("DELIMITER ;");
                _textWriter.WriteLine();
            }

            _textWriter.Flush();
        }

        private void Export_Triggers()
        {
            if (!ExportInfo.ExportTriggers || Database.Triggers.Count == 0)
                return;

            Export_WriteComment("");
            Export_WriteComment("Dumping triggers");
            Export_WriteComment("");
            _textWriter.WriteLine();

            foreach (var trigger in Database.Triggers)
            {
                if (_stopProcess)
                    return;

                Export_WriteLine(string.Format("DROP TRIGGER /*!50030 IF EXISTS */ `{0}`;", trigger.Name));
                Export_WriteLine("DELIMITER " + ExportInformations.ScriptsDelimiter);

                if (ExportInfo.ExportRoutinesWithoutDefiner)
                    Export_WriteLine(trigger.CreateTriggerSqlWithoutDefiner + " " +
                                     ExportInformations.ScriptsDelimiter);
                else
                    Export_WriteLine(trigger.CreateTriggerSql + " " + ExportInformations.ScriptsDelimiter);

                Export_WriteLine("DELIMITER ;");
                _textWriter.WriteLine();
            }

            _textWriter.Flush();
        }

        private void Export_DocumentFooter()
        {
            _textWriter.WriteLine();

            var lstFooters = ExportInfo.GetDocumentFooters();
            if (lstFooters.Count > 0)
                foreach (var s in lstFooters)
                    Export_WriteLine(s);

            _timeEnd = DateTime.Now;

            if (ExportInfo.RecordDumpTime)
            {
                var ts = _timeEnd - _timeStart;

                _textWriter.WriteLine();
                _textWriter.WriteLine();
                Export_WriteComment(string.Format("Dump completed on {0}", _timeEnd.ToString("yyyy-MM-dd HH:mm:ss")));
                Export_WriteComment(string.Format("Total time: {0}:{1}:{2}:{3}:{4} (d:h:m:s:ms)", ts.Days, ts.Hours,
                    ts.Minutes, ts.Seconds, ts.Milliseconds));
            }

            _textWriter.Flush();
        }

        private void Export_WriteComment(string text)
        {
            Export_WriteLine(string.Format("-- {0}", text));
        }

        private void Export_WriteLine(string text)
        {
            _textWriter.WriteLine(ExportInfo.EnableEncryption ? Encrypt(text) : text);
        }

        #endregion

        #region Import

        public void ImportFromString(string sqldumptext)
        {
            using (var ms = new MemoryStream())
            {
                using (var thisWriter = new StreamWriter(ms))
                {
                    thisWriter.Write(sqldumptext);
                    thisWriter.Flush();

                    ms.Position = 0L;

                    ImportFromMemoryStream(ms);
                }
            }
        }

        public void ImportFromFile(string filePath)
        {
            var fi = new FileInfo(filePath);

            using (TextReader tr = new StreamReader(filePath))
            {
                ImportFromTextReaderStream(tr, fi);
            }
        }

        public void ImportFromTextReader(TextReader tr)
        {
            ImportFromTextReaderStream(tr, null);
        }

        public void ImportFromMemoryStream(MemoryStream ms)
        {
            ms.Position = 0;
            _totalBytes = ms.Length;
            _textReader = new StreamReader(ms);
            Import_Start();
        }

        private void ImportFromTextReaderStream(TextReader tr, FileInfo fileInfo)
        {
            if (fileInfo != null)
                _totalBytes = fileInfo.Length;
            else
                _totalBytes = 0L;

            _textReader = tr;

            Import_Start();
        }

        private void Import_Start()
        {
            Import_InitializeVariables();

            var line = "";

            while (line != null)
            {
                if (_stopProcess)
                {
                    _processCompletionType = ProcessEndType.Cancelled;
                    break;
                }

                try
                {
                    line = Import_GetLine();

                    Import_ProcessLine(line);
                }
                catch (Exception ex)
                {
                    LastError = ex;
                    if (ImportInfo.IgnoreSqlError)
                    {
                        if (!string.IsNullOrEmpty(ImportInfo.ErrorLogFile))
                            File.AppendAllText(ImportInfo.ErrorLogFile, Environment.NewLine + Environment.NewLine + ex);
                    }
                    else
                    {
                        StopAllProcess();
                        throw;
                    }
                }
            }

            ReportEndProcess();
        }

        private void Import_InitializeVariables()
        {
            if (Command == null)
                throw new Exception("MySqlCommand is not initialized. Object not set to an instance of an object.");

            if (Command.Connection == null)
                throw new Exception(
                    "MySqlCommand.Connection is not initialized. Object not set to an instance of an object.");

            if (Command.Connection.State != ConnectionState.Open)
                throw new Exception("MySqlCommand.Connection is not opened.");

            _stopProcess = false;
            GetSha512HashFromPassword(ImportInfo.EncryptionPassword);
            LastError = null;
            _timeStart = DateTime.Now;
            _currentBytes = 0L;
            _sbImport = new StringBuilder();
            _mySqlScript = new MySqlScript(Command.Connection);
            _currentProcess = ProcessType.Import;
            _processCompletionType = ProcessEndType.Complete;
            _delimiter = ";";

            if (ImportProgressChanged != null)
                _timerReport.Start();

            if (ImportInfo.TargetDatabase.Length > 0)
                Import_CreateNewDatabase();
        }

        private string Import_GetLine()
        {
            var line = _textReader.ReadLine();

            if (line == null)
                return null;

            if (ImportProgressChanged != null)
                _currentBytes = _currentBytes + line.Length;

            if (Import_IsEmptyLine(line))
                return string.Empty;

            line = line.Trim();

            if (!ImportInfo.EnableEncryption)
                return line;

            line = Decrypt(line);

            return line.Trim();
        }

        private void Import_ProcessLine(string line)
        {
            var nextAction = Import_AnalyseNextAction(line);

            switch (nextAction)
            {
                case NextImportAction.Ignore:
                    break;
                case NextImportAction.SetNames:
                    Import_SetNames();
                    break;
                case NextImportAction.AppendLine:
                    Import_AppendLine(line);
                    break;
                case NextImportAction.ChangeDelimiter:
                    Import_ChangeDelimiter(line);
                    break;
                case NextImportAction.AppendLineAndExecute:
                    Import_AppendLineAndExecute(line);
                    break;
                case NextImportAction.CreateNewDatabase:
                    break;
            }
        }

        private NextImportAction Import_AnalyseNextAction(string line)
        {
            if (Import_IsEmptyLine(line))
                return NextImportAction.Ignore;

            if (_isNewDatabase)
            {
                if (line.StartsWith("CREATE DATABASE ", StringComparison.OrdinalIgnoreCase))
                    return NextImportAction.Ignore;
                if (line.StartsWith("USE ", StringComparison.OrdinalIgnoreCase))
                    return NextImportAction.Ignore;
            }

            if (_nameIsSet)
            {
                if (line.StartsWith("/*!40101 SET NAMES ", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("SET NAMES ", StringComparison.OrdinalIgnoreCase))
                    return NextImportAction.Ignore;
                if (line.StartsWith("/*!40101 SET CHARACTER_SET_CLIENT", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("SET CHARACTER_SET_CLIENT", StringComparison.OrdinalIgnoreCase))
                    return NextImportAction.Ignore;
            }
            else
            {
                if (line.StartsWith("/*!40101 SET NAMES ", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("SET NAMES ", StringComparison.OrdinalIgnoreCase))
                    return NextImportAction.SetNames;
            }

            if (line.EndsWith(_delimiter))
                return NextImportAction.AppendLineAndExecute;

            if (line.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
                return NextImportAction.ChangeDelimiter;

            return NextImportAction.AppendLine;
        }

        private void Import_CreateNewDatabase()
        {
            if (ImportInfo.DatabaseDefaultCharSet.Length == 0)
                Command.CommandText = string.Format("CREATE DATABASE IF NOT EXISTS `{0}`;", ImportInfo.TargetDatabase);
            else
                Command.CommandText =
                    string.Format("CREATE DATABASE IF NOT EXISTS `{0}` /*!40100 DEFAULT CHARACTER SET {1} */;",
                        ImportInfo.TargetDatabase, ImportInfo.DatabaseDefaultCharSet);

            Command.ExecuteNonQuery();

            Command.CommandText = string.Format("USE `{0}`;", ImportInfo.TargetDatabase);
            Command.ExecuteNonQuery();

            Import_SetNames();

            _isNewDatabase = true;
            _nameIsSet = true;
        }

        private void Import_SetNames()
        {
            var setname = QueryExpress.ExecuteScalarStr(Command, "SHOW VARIABLES LIKE 'character_set_database';", 1);
            Command.CommandText = string.Format("/*!40101 SET NAMES {0} */;", setname);
            Command.ExecuteNonQuery();
            _nameIsSet = true;
        }

        private void Import_AppendLine(string line)
        {
            _sbImport.AppendLine(line);
        }

        private void Import_ChangeDelimiter(string line)
        {
            var nextDelimiter = line.Substring(9);
            _delimiter = nextDelimiter.Replace(" ", string.Empty);
        }

        private void Import_AppendLineAndExecute(string line)
        {
            _sbImport.AppendLine(line);
            if (!line.EndsWith(_delimiter))
                return;

            _mySqlScript.Query = _sbImport.ToString();
            _mySqlScript.Delimiter = _delimiter;
            _mySqlScript.Execute();
            _sbImport = new StringBuilder();
        }

        private bool Import_IsEmptyLine(string line)
        {
            if (line == null)
                return true;
            if (line == string.Empty)
                return true;
            if (line.Trim().Length == 0)
                return true;
            if (line.StartsWith("--"))
                return true;
            if (line == Environment.NewLine)
                return true;
            if (line == "\r")
                return true;
            if (line == "\n")
                return true;

            return false;
        }

        #endregion

        #region Encryption

        private void GetSha512HashFromPassword(string password)
        {
            _sha512HashedPassword = CryptoExpress.Sha512Hash(password);
        }

        private string Encrypt(string text)
        {
            return CryptoExpress.AES_Encrypt(text, _sha512HashedPassword);
        }

        private string Decrypt(string text)
        {
            return CryptoExpress.AES_Decrypt(text, _sha512HashedPassword);
        }

        public void EncryptDumpFile(string sourceFile, string outputFile, string password)
        {
            using (TextReader trSource = new StreamReader(sourceFile))
            {
                using (TextWriter twOutput = new StreamWriter(outputFile, false, _utf8WithoutBom))
                {
                    EncryptDumpFile(trSource, twOutput, password);
                    twOutput.Close();
                }
                trSource.Close();
            }
        }

        public void EncryptDumpFile(TextReader trSource, TextWriter twOutput, string password)
        {
            GetSha512HashFromPassword(password);

            var line = "";

            while (line != null)
            {
                line = trSource.ReadLine();

                if (line == null)
                    break;

                line = Encrypt(line);

                twOutput.WriteLine(line);
                twOutput.Flush();
            }
        }

        public void DecryptDumpFile(string sourceFile, string outputFile, string password)
        {
            using (TextReader trSource = new StreamReader(sourceFile))
            {
                using (TextWriter twOutput = new StreamWriter(outputFile, false, _utf8WithoutBom))
                {
                    DecryptDumpFile(trSource, twOutput, password);
                    twOutput.Close();
                }
                trSource.Close();
            }
        }

        public void DecryptDumpFile(TextReader trSource, TextWriter twOutput, string password)
        {
            GetSha512HashFromPassword(password);

            var line = "";

            while (line != null)
            {
                line = trSource.ReadLine();

                if (line == null)
                    break;

                if (line.Trim().Length == 0)
                    twOutput.WriteLine();

                line = Decrypt(line);

                twOutput.WriteLine(line);
                twOutput.Flush();
            }
        }

        #endregion
    }
}