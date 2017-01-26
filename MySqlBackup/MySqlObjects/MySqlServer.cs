namespace MySql.Data.MySqlClient
{
    public class MySqlServer
    {
        private decimal _majorVersionNumber;

        public string Version => $"{VersionNumber} {Edition}";

        public string VersionNumber { get; private set; }

        public decimal MajorVersionNumber => _majorVersionNumber;

        public string Edition { get; private set; }
        public string CharacterSetServer { get; private set; } = "";
        public string CharacterSetSystem { get; private set; } = "";
        public string CharacterSetConnection { get; private set; } = "";
        public string CharacterSetDatabase { get; private set; } = "";
        public string CurrentUser { get; private set; } = "";
        public string CurrentUserClientHost { get; private set; } = "";
        public string CurrentClientHost { get; private set; } = "";

        public void GetServerInfo(MySqlCommand cmd)
        {
            Edition = QueryExpress.ExecuteScalarStr(cmd, "SHOW variables LIKE 'version_comment';", 1);
            VersionNumber = QueryExpress.ExecuteScalarStr(cmd, "SHOW variables LIKE 'version';", 1);
            CharacterSetServer = QueryExpress.ExecuteScalarStr(cmd, "SHOW variables LIKE 'character_set_server';", 1);
            CharacterSetSystem = QueryExpress.ExecuteScalarStr(cmd, "SHOW variables LIKE 'character_set_system';", 1);
            CharacterSetConnection = QueryExpress.ExecuteScalarStr(cmd,
                "SHOW variables LIKE 'character_set_connection';", 1);
            CharacterSetDatabase = QueryExpress.ExecuteScalarStr(cmd, "SHOW variables LIKE 'character_set_database';", 1);

            CurrentUserClientHost = QueryExpress.ExecuteScalarStr(cmd, "SELECT current_user;");

            var ca = CurrentUserClientHost.Split('@');

            CurrentUser = ca[0];
            CurrentClientHost = ca[1];

            GetMajorVersionNumber();
        }

        private void GetMajorVersionNumber()
        {
            var vsa = VersionNumber.Split('.');
            string v;
            if (vsa.Length > 1)
                v = vsa[0] + "." + vsa[1];
            else
                v = vsa[0];
            decimal.TryParse(v, out _majorVersionNumber);
        }
    }
}