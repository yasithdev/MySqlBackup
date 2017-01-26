using System;
using System.Text;

namespace MySql.Data.MySqlClient
{
    public class MySqlTable : IDisposable
    {
        public MySqlTable(MySqlCommand cmd, string name)
        {
            Name = name;
            var sql = $"SHOW CREATE TABLE `{name}`;";
            CreateTableSql =
                QueryExpress.ExecuteScalarStr(cmd, sql, 1)
                    .Replace(Environment.NewLine, "^~~~~~~^")
                    .Replace("\r", "^~~~~~~^")
                    .Replace("\n", "^~~~~~~^")
                    .Replace("^~~~~~~^", Environment.NewLine)
                    .Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ") + ";";
            CreateTableSqlWithoutAutoIncrement = RemoveAutoIncrement(CreateTableSql);
            Columns = new MySqlColumnList(cmd, name);
            GetInsertStatementHeaders();
        }

        public string Name { get; }
        public long TotalRows { get; private set; }
        public string CreateTableSql { get; }
        public string CreateTableSqlWithoutAutoIncrement { get; }
        public MySqlColumnList Columns { get; private set; }
        public string InsertStatementHeaderWithoutColumns { get; private set; } = "";
        public string InsertStatementHeader { get; private set; } = "";

        public void Dispose()
        {
            Columns.Dispose();
            Columns = null;
        }

        private void GetInsertStatementHeaders()
        {
            InsertStatementHeaderWithoutColumns = $"INSERT INTO `{Name}` VALUES";

            var sb = new StringBuilder();
            sb.Append("INSERT INTO `");
            sb.Append(Name);
            sb.Append("` (");
            for (var i = 0; i < Columns.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");

                sb.Append("`");
                sb.Append(Columns[i].Name);
                sb.Append("`");
            }
            sb.Append(") VALUES");

            InsertStatementHeader = sb.ToString();
        }

        //public void GetTotalRows(MySqlCommand cmd)
        //{
        //    string sql = string.Format("SELECT COUNT(*) FROM `{0}`;", _name);
        //    _totalRows = QueryExpress.ExecuteScalarLong(cmd, sql);
        //}
        public void SetTotalRows(long trows)
        {
            TotalRows = trows;
        }

        private string RemoveAutoIncrement(string sql)
        {
            const string a = "AUTO_INCREMENT=";

            if (!sql.Contains(a)) return sql;
            var i = sql.LastIndexOf(a, StringComparison.Ordinal);

            var b = i + a.Length;

            var d = "";

            var count = 0;

            while (char.IsDigit(sql[b + count]))
            {
                var cc = sql[b + count];

                d = d + cc;

                count = count + 1;
            }

            sql = sql.Replace(a + d, string.Empty);

            return sql;
        }
    }
}