namespace MySql.Data.MySqlClient
{
    public class MySqlEvent
    {
        public MySqlEvent(MySqlCommand cmd, string eventName, string definer)
        {
            Name = eventName;

            CreateEventSql = QueryExpress.ExecuteScalarStr(cmd, $"SHOW CREATE EVENT `{Name}`;",
                "Create Event");

            CreateEventSql = CreateEventSql.Replace("\r\n", "^~~~~~~~~~~~~~~^");
            CreateEventSql = CreateEventSql.Replace("\n", "^~~~~~~~~~~~~~~^");
            CreateEventSql = CreateEventSql.Replace("\r", "^~~~~~~~~~~~~~~^");
            CreateEventSql = CreateEventSql.Replace("^~~~~~~~~~~~~~~^", "\r\n");

            var sa = definer.Split('@');
            definer = $" DEFINER=`{sa[0]}`@`{sa[1]}`";

            CreateEventSqlWithoutDefiner = CreateEventSql.Replace(definer, string.Empty);
        }

        public string Name { get; }
        public string CreateEventSql { get; }
        public string CreateEventSqlWithoutDefiner { get; }
    }
}