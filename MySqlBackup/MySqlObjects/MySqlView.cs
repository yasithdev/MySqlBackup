namespace MySql.Data.MySqlClient
{
    public class MySqlView
    {
        public MySqlView(MySqlCommand cmd, string viewName)
        {
            Name = viewName;

            var sqlShowCreate = string.Format("SHOW CREATE VIEW `{0}`;", viewName);

            var dtView = QueryExpress.GetTable(cmd, sqlShowCreate);

            CreateViewSQL = dtView.Rows[0]["Create View"] + ";";

            CreateViewSQL = CreateViewSQL.Replace("\r\n", "^~~~~~~~~~~~~~~^");
            CreateViewSQL = CreateViewSQL.Replace("\n", "^~~~~~~~~~~~~~~^");
            CreateViewSQL = CreateViewSQL.Replace("\r", "^~~~~~~~~~~~~~~^");
            CreateViewSQL = CreateViewSQL.Replace("^~~~~~~~~~~~~~~^", "\r\n");

            CreateViewSQLWithoutDefiner = QueryExpress.EraseDefiner(CreateViewSQL);
        }

        public string Name { get; } = "";
        public string CreateViewSQL { get; } = "";
        public string CreateViewSQLWithoutDefiner { get; } = "";
    }
}