using System;
using System.Data;

namespace MySql.Data.MySqlClient
{
    public class MySqlDatabase : IDisposable
    {
        public delegate void GetTotalRowsProgressChange(object sender, GetTotalRowsArgs e);

        public string Name { get; private set; } = "";
        public string DefaultCharacterSet { get; private set; } = "";
        public string CreateDatabaseSql { get; private set; } = "";
        public string DropDatabaseSql { get; private set; } = "";

        public MySqlTableList Tables { get; private set; } = new MySqlTableList();
        public MySqlProcedureList Procedures { get; private set; } = new MySqlProcedureList();
        public MySqlEventList Events { get; private set; } = new MySqlEventList();
        public MySqlViewList Views { get; private set; } = new MySqlViewList();
        public MySqlFunctionList Functions { get; private set; } = new MySqlFunctionList();
        public MySqlTriggerList Triggers { get; private set; } = new MySqlTriggerList();

        public long TotalRows
        {
            get
            {
                long t = 0;

                foreach (var t1 in Tables)
                    t = t + t1.TotalRows;

                return t;
            }
        }

        public void Dispose()
        {
            Tables.Dispose();
            Procedures.Dispose();
            Functions.Dispose();
            Events.Dispose();
            Triggers.Dispose();
            Views.Dispose();
        }

        public event GetTotalRowsProgressChange GetTotalRowsProgressChanged;

        public void GetDatabaseInfo(MySqlCommand cmd, bool getTotalRowsForEachTable)
        {
            Name = QueryExpress.ExecuteScalarStr(cmd, "SELECT DATABASE();");
            DefaultCharacterSet =
                QueryExpress.ExecuteScalarStr(cmd, "SHOW VARIABLES LIKE 'character_set_database';", 1);
            CreateDatabaseSql =
                QueryExpress.ExecuteScalarStr(cmd, $"SHOW CREATE DATABASE `{Name}`;", 1)
                    .Replace("CREATE DATABASE", "CREATE DATABASE IF NOT EXISTS") + ";";
            DropDatabaseSql = $"DROP DATABASE IF EXISTS `{Name}`;";

            Tables = new MySqlTableList(cmd);
            Procedures = new MySqlProcedureList(cmd);
            Functions = new MySqlFunctionList(cmd);
            Triggers = new MySqlTriggerList(cmd);
            Events = new MySqlEventList(cmd);
            Views = new MySqlViewList(cmd);

            if (getTotalRowsForEachTable)
                GetTotalRows(cmd);
        }

        public void GetTotalRows(MySqlCommand cmd)
        {
            var dtTotalRows = QueryExpress.GetTable(cmd,
                $"SELECT TABLE_NAME, TABLE_ROWS FROM `information_schema`.`tables` WHERE `table_schema` = '{Name}';");

            var tableCountTotalRow = 0;

            foreach (DataRow dr in dtTotalRows.Rows)
            {
                var thisTableName = dr["TABLE_NAME"] + "";

                var totalRowsThisTable = 0L;

                try
                {
                    long.TryParse(dr["TABLE_ROWS"] + "", out totalRowsThisTable);
                }
                catch
                {
                    // ignored
                }

                foreach (var t in Tables)
                {
                    if (t.Name != thisTableName)
                        continue;

                    tableCountTotalRow = tableCountTotalRow + 1;

                    t.SetTotalRows(totalRowsThisTable);

                    GetTotalRowsProgressChanged?.Invoke(this, new GetTotalRowsArgs(Tables.Count, tableCountTotalRow));

                    break;
                }
            }


            //for (int i = 0; i < _listTable.Count; i++)
            //{
            //    _listTable[i].GetTotalRows(cmd);

            //    if (GetTotalRowsProgressChanged != null)
            //    {
            //        GetTotalRowsProgressChanged(this, new GetTotalRowsArgs(_listTable.Count, i + 1));
            //    }
            //}
        }
    }
}