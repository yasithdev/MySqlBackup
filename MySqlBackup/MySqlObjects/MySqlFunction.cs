namespace MySql.Data.MySqlClient
{
    public class MySqlFunction
    {
        public MySqlFunction(MySqlCommand cmd, string functionName, string definer)
        {
            Name = functionName;

            var sql = $"SHOW CREATE FUNCTION `{functionName}`;";

            CreateFunctionSql = QueryExpress.ExecuteScalarStr(cmd, sql, 2);

            CreateFunctionSql = CreateFunctionSql.Replace("\r\n", "^~~~~~~~~~~~~~~^");
            CreateFunctionSql = CreateFunctionSql.Replace("\n", "^~~~~~~~~~~~~~~^");
            CreateFunctionSql = CreateFunctionSql.Replace("\r", "^~~~~~~~~~~~~~~^");
            CreateFunctionSql = CreateFunctionSql.Replace("^~~~~~~~~~~~~~~^", "\r\n");

            var sa = definer.Split('@');
            definer = $" DEFINER=`{sa[0]}`@`{sa[1]}`";

            CreateFunctionSqlWithoutDefiner = CreateFunctionSql.Replace(definer, string.Empty);
        }

        public string Name { get; }
        public string CreateFunctionSql { get; }
        public string CreateFunctionSqlWithoutDefiner { get; }
    }
}