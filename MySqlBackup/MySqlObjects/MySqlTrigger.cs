namespace MySql.Data.MySqlClient
{
    public class MySqlTrigger
    {
        public MySqlTrigger(MySqlCommand cmd, string triggerName, string definer)
        {
            Name = triggerName;

            CreateTriggerSql = QueryExpress.ExecuteScalarStr(cmd,
                $"SHOW CREATE TRIGGER `{triggerName}`;", 2);

            CreateTriggerSql = CreateTriggerSql.Replace("\r\n", "^~~~~~~~~~~~~~~^");
            CreateTriggerSql = CreateTriggerSql.Replace("\n", "^~~~~~~~~~~~~~~^");
            CreateTriggerSql = CreateTriggerSql.Replace("\r", "^~~~~~~~~~~~~~~^");
            CreateTriggerSql = CreateTriggerSql.Replace("^~~~~~~~~~~~~~~^", "\r\n");

            var sa = definer.Split('@');
            definer = $" DEFINER=`{sa[0]}`@`{sa[1]}`";

            CreateTriggerSqlWithoutDefiner = CreateTriggerSql.Replace(definer, string.Empty);
        }

        public string Name { get; } = "";
        public string CreateTriggerSql { get; } = "";
        public string CreateTriggerSqlWithoutDefiner { get; } = "";
    }
}