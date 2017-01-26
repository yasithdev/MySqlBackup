using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace MySqlBackupTestApp
{
    public partial class FormDatabaseInfo : Form
    {
        private readonly BackgroundWorker _bw;
        private readonly Timer _timer1;
        private MySqlCommand _cmd;
        private MySqlDatabase _myDatabase;
        private MySqlServer _myServer;
        private StringBuilder _sb;

        public FormDatabaseInfo()
        {
            _timer1 = new Timer {Interval = 100};
            _timer1.Tick += timer1_Tick;
            _bw = new BackgroundWorker();
            _bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            _bw.DoWork += bw_DoWork;
            InitializeComponent();
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Start();
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
            }
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            webBrowser1.DocumentText = _sb.ToString();
        }

        private void Start()
        {
            _sb = new StringBuilder();
            _sb.AppendLine(
                "<html><head><style>h1 { line-height:160%; font-size: 20pt; } h2 { line-height:160%; font-size: 14pt; } body { font-family: \"Segoe UI\", Arial; line-height: 150%; } table { border: 1px solid #5C5C5C; border-collapse: collapse; } td { font-size: 10pt; padding: 4px; border: 1px solid #5C5C5C; } .code { font-family: \"Courier New\"; font-size: 10pt; line-height:110%; } </style></head>");
            _sb.AppendLine("<body>");

            using (var conn = new MySqlConnection(Program.ConnectionString))
            {
                try
                {
                    conn.Open();


                    _cmd = new MySqlCommand();
                    _cmd.Connection = conn;

                    _myDatabase = new MySqlDatabase();
                    _myDatabase.GetDatabaseInfo(_cmd, true);
                    _myServer = new MySqlServer();
                    _myServer.GetServerInfo(_cmd);

                    var stage = 1;

                    while (stage < 13)
                    {
                        try
                        {
                            switch (stage)
                            {
                                case 1:
                                    LoadDatabase();
                                    break;
                                case 2:
                                    LoadUser();
                                    break;
                                case 3:
                                    LoadGlobalPrivilege();
                                    break;
                                case 4:
                                    LoadViewPrivilege();
                                    break;
                                case 5:
                                    LoadFunctionPrivilege();
                                    break;
                                case 6:
                                    LoadVariables();
                                    break;
                                case 7:
                                    LoadTables();
                                    break;
                                case 8:
                                    LoadFunctions();
                                    break;
                                case 9:
                                    LoadProcedures();
                                    break;
                                case 10:
                                    LoadTriggers();
                                    break;
                                case 11:
                                    LoadViews();
                                    break;
                                case 12:
                                    LoadEvents();
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(ex.Message);
                        }

                        stage += 1;
                    }

                    conn.Close();
                }
                catch (Exception exCon)
                {
                    WriteError(exCon.Message);
                }
            }

            _sb.Append("</body>");
            _sb.Append("</html>");
        }

        private void LoadDatabase()
        {
            WriteHead1("Database");
            WriteCodeBlock(_myDatabase.CreateDatabaseSql);
        }

        private void LoadUser()
        {
            WriteHead1("User");

            var sqlSelectCurrentUser = "SELECT current_user;";
            WriteCodeBlock(sqlSelectCurrentUser);
            WriteCodeBlock(_myServer.CurrentUserClientHost);
        }

        private void LoadGlobalPrivilege()
        {
            WriteHead2("Global Privileges");

            var curUser = "";
            if (_myServer.CurrentUser != "root")
                curUser = _myServer.CurrentUser;
            else
                WriteText("Current user is \"root\". All privileges are granted by default.");

            var sqlShowUserPrivilege = "SELECT * FROM mysql.db WHERE `user` = '" + curUser + "';";

            var dt = QueryExpress.GetTable(_cmd, sqlShowUserPrivilege);


            WriteCodeBlock(sqlShowUserPrivilege);
            WriteTable(dt);
        }

        private void LoadViewPrivilege()
        {
            WriteHead2("Privileges of View");

            var sqlViewPrivilege =
                @"SELECT  mv.host `Host`,  mv.user `User`,
CONCAT(mv.Db, '.', mv.Table_name) `Views`,
REPLACE(mv.Table_priv, ',', ', ') AS `Privileges`
FROM  mysql.tables_priv mv
WHERE mv.Db = '" + _myDatabase.Name + @"' 
and mv.Table_name IN  
(SELECT  DISTINCT v.table_name `views` FROM information_schema.views AS v) 
ORDER BY  mv.Host,  mv.User,  mv.Db,  mv.Table_name;";

            var dtViewPrivilege = QueryExpress.GetTable(_cmd, sqlViewPrivilege);

            WriteCodeBlock(sqlViewPrivilege);
            WriteTable(dtViewPrivilege);
        }

        private void LoadProcedurePrivilege()
        {
            WriteHead2("Privileges of Procedure");

            var sqlProcedurePrivilege =
                @"SELECT  mp.host `Host`,  mp.user `User`,
CONCAT(mp.Db, '.', mp.Routine_name) `Procedures`,
REPLACE(mp.Proc_priv, ',', ', ') AS `Privileges`
FROM  mysql.procs_priv mp
WHERE mp.Db = '" + _myDatabase.Name + @"' 
and mp.Routine_type = 'PROCEDURE' 
ORDER BY  mp.Host,  mp.User,  mp.Db,  mp.Routine_name;";

            var dt = QueryExpress.GetTable(_cmd, sqlProcedurePrivilege);

            WriteCodeBlock(sqlProcedurePrivilege);
            WriteTable(dt);
        }

        private void LoadFunctionPrivilege()
        {
            WriteHead2("Privileges of Function");

            var sqlPrivilegeFunction =
                @"SELECT  mf.host `Host`,  mf.user `User`,
CONCAT(mf.Db, '.', mf.Routine_name) `Procedures`,
REPLACE(mf.Proc_priv, ',', ', ') AS `Privileges`
FROM  mysql.procs_priv mf WHERE mf.Db = '" + _myDatabase.Name + @"'
and mf.Routine_type = 'FUNCTION' 
ORDER BY  mf.Host,  mf.User,  mf.Db,  mf.Routine_name;";

            var dtPrivilegeFunction = QueryExpress.GetTable(_cmd, sqlPrivilegeFunction);

            WriteCodeBlock(sqlPrivilegeFunction);
            WriteTable(dtPrivilegeFunction);
        }

        private void LoadVariables()
        {
            WriteHead1("System Variables");

            var sqlShowVariables = "SHOW variables;";

            var dtVariables = QueryExpress.GetTable(_cmd, sqlShowVariables);

            WriteCodeBlock(sqlShowVariables);
            WriteTable(dtVariables);
        }

        private void LoadTables()
        {
            WriteHead1("Tables");

            WriteText(
                "Note: Value of \"Rows\" shown below is not accurate. It is a cache value, it is not up to date. For accurate total rows count, please see the following next table.");

            var sqlShowTableStatus = "SHOW TABLE STATUS;";

            var dtTableStatus = QueryExpress.GetTable(_cmd, sqlShowTableStatus);

            WriteCodeBlock(sqlShowTableStatus);
            WriteTable(dtTableStatus);

            WriteHead2("Actual Total Rows For Each Table");

            var dtTotalRows = new DataTable();
            dtTotalRows.Columns.Add("Table");
            dtTotalRows.Columns.Add("Total Rows");

            foreach (var table in _myDatabase.Tables)
                dtTotalRows.Rows.Add(table.Name, table.TotalRows);

            WriteTable(dtTotalRows);

            foreach (var table in _myDatabase.Tables)
            {
                WriteHead2(table.Name);
                WriteCodeBlock(table.Columns.SqlShowFullColumns);
                var dtColumns = QueryExpress.GetTable(_cmd, table.Columns.SqlShowFullColumns);
                WriteTable(dtColumns);

                WriteText("Data Type in .NET Framework");

                var dtDataType = new DataTable();
                dtDataType.Columns.Add("Column Name");
                dtDataType.Columns.Add("MySQL Data Type");
                dtDataType.Columns.Add(".NET Data Type");

                foreach (var myCol in table.Columns)
                    dtDataType.Rows.Add(myCol.Name, myCol.MySqlDataType, myCol.DataType.ToString());

                WriteTable(dtDataType);

                WriteCodeBlock("SHOW CREATE TABLE `" + table.Name + "`;");
                WriteCodeBlock(table.CreateTableSqlWithoutAutoIncrement);
            }
        }

        private void LoadFunctions()
        {
            WriteHead1("Functions");

            WriteCodeBlock(_myDatabase.Functions.SqlShowFunctions);
            var dtFunctionList = QueryExpress.GetTable(_cmd, _myDatabase.Functions.SqlShowFunctions);
            WriteTable(dtFunctionList);

            WriteCodeBlock("SHOW CREATE FUNCTION `<name>`;");

            if (!_myDatabase.Functions.AllowAccess)
                WriteAccessDeniedErrMsg();

            foreach (var func in _myDatabase.Functions)
            {
                WriteHead2(func.Name);
                WriteCodeBlock(func.CreateFunctionSqlWithoutDefiner);
            }
        }

        private void LoadProcedures()
        {
            WriteHead1("Procedures");

            WriteCodeBlock(_myDatabase.Procedures.SqlShowProcedures);
            var dtProcedureList = QueryExpress.GetTable(_cmd, _myDatabase.Procedures.SqlShowProcedures);
            WriteTable(dtProcedureList);

            WriteCodeBlock("SHOW CREATE PROCEDURE `<name>`;");

            if (!_myDatabase.Procedures.AllowAccess)
                WriteAccessDeniedErrMsg();

            foreach (var proc in _myDatabase.Procedures)
            {
                WriteHead2(proc.Name);
                WriteCodeBlock(proc.CreateProcedureSqlWithoutDefiner);
            }
        }

        private void LoadTriggers()
        {
            WriteHead1("Triggers");

            WriteCodeBlock(_myDatabase.Triggers.SqlShowTriggers);
            var dtTriggerList = QueryExpress.GetTable(_cmd, _myDatabase.Triggers.SqlShowTriggers);
            WriteTable(dtTriggerList);

            WriteCodeBlock("SHOW CREATE TRIGGER `<name>`;");

            if (!_myDatabase.Triggers.AllowAccess)
                WriteAccessDeniedErrMsg();

            foreach (var trigger in _myDatabase.Triggers)
            {
                WriteHead2(trigger.Name);
                WriteCodeBlock(trigger.CreateTriggerSql);
            }
        }

        private void LoadViews()
        {
            WriteHead1("Views");

            WriteCodeBlock(_myDatabase.Views.SqlShowViewList);
            var dtViewList = QueryExpress.GetTable(_cmd, _myDatabase.Views.SqlShowViewList);
            WriteTable(dtViewList);

            WriteCodeBlock("SHOW CREATE VIEW `<name>`;");

            if (!_myDatabase.Views.AllowAccess)
                WriteAccessDeniedErrMsg();

            foreach (var myview in _myDatabase.Views)
            {
                WriteHead2(myview.Name);
                WriteCodeBlock(myview.CreateViewSQL);
            }
        }

        private void LoadEvents()
        {
            WriteHead1("Events");

            WriteCodeBlock(_myDatabase.Events.SqlShowEvent);
            var dtEventList = QueryExpress.GetTable(_cmd, _myDatabase.Events.SqlShowEvent);
            WriteTable(dtEventList);

            WriteCodeBlock("SHOW CREATE EVENT `<name>`;");

            if (!_myDatabase.Events.AllowAccess)
                WriteAccessDeniedErrMsg();

            foreach (var myevent in _myDatabase.Events)
            {
                WriteHead2(myevent.Name);
                WriteCodeBlock(myevent.CreateEventSql);
            }
        }

        private void WriteHead1(string text)
        {
            _sb.Append("<h1>");
            _sb.Append(GetHtmlString(text.Trim()));
            _sb.AppendLine("</h1>");
            _sb.AppendLine("<hr />");
        }

        private void WriteHead2(string text)
        {
            _sb.Append("<h2>");
            _sb.Append(GetHtmlString(text.Trim()));
            _sb.AppendLine("</h2>");
        }

        private void WriteText(string text)
        {
            _sb.AppendLine("<p>");
            _sb.AppendLine(GetHtmlString(text.Trim()));
            _sb.AppendLine("</p>");
        }

        private void WriteCodeBlock(string text)
        {
            _sb.AppendLine("<span class=\"code\">");
            _sb.AppendLine(GetHtmlString(text.Trim()));
            _sb.AppendLine("</span>");
            _sb.AppendLine("<br /><br />");
        }

        private void WriteTable(DataTable dt)
        {
            _sb.AppendFormat(HtmlExpress.ConvertDataTableToHtmlTable(dt));
            _sb.AppendLine("<br />");
        }

        private void WriteAccessDeniedErrMsg()
        {
            WriteError("Access denied for user " + _myServer.CurrentUserClientHost);
        }

        private void WriteError(string errMsg)
        {
            _sb.AppendLine("<br />");
            _sb.AppendLine("<div style=\"background-color: #FFE8E8; padding: 5px; border: 1px solid #FF0000;\">");
            _sb.AppendLine("Error or Exception occured. Error message:<br />");
            _sb.AppendLine(GetHtmlString(errMsg));
            _sb.AppendLine("</div>");
            _sb.AppendLine("<br />");
        }

        private string GetHtmlString(string input)
        {
            input = input.Replace("\r\n", "^||||^").Replace("\n", "^||||^").Replace("\r", "^||||^");
            var sb2 = new StringBuilder();
            foreach (var c in input)
                switch (c)
                {
                    case '&':
                        sb2.AppendFormat("&amp;");
                        break;
                    case '"':
                        sb2.AppendFormat("&quot;");
                        break;
                    case '\'':
                        sb2.AppendFormat("&#39;");
                        break;
                    case '<':
                        sb2.AppendFormat("&lt;");
                        break;
                    case '>':
                        sb2.AppendFormat("&gt;");
                        break;
                    default:
                        sb2.Append(c);
                        break;
                }
            return sb2.ToString().Replace("^||||^", "<br />");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _timer1.Stop();
            webBrowser1.DocumentText = "<h1>Database info is loading...<br />Please wait...</h1>";
            _bw.RunWorkerAsync();
        }

        private void FormDatabaseInfo_Load(object sender, EventArgs e)
        {
            _timer1.Start();
        }

        private void btExport_Click(object sender, EventArgs e)
        {
            var sf = new SaveFileDialog
            {
                Filter = @"HTML|*.html",
                FileName = _myDatabase.Name + ".html"
            };
            if (DialogResult.OK == sf.ShowDialog())
                File.WriteAllText(sf.FileName, webBrowser1.DocumentText);
        }

        private void btRefresh_Click(object sender, EventArgs e)
        {
            _timer1.Start();
        }

        private void btPrint_Click(object sender, EventArgs e)
        {
            webBrowser1.ShowPrintPreviewDialog();
        }
    }
}