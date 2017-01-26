namespace MySql.Data.MySqlClient
{
    public enum RowsDataExportMode
    {
        Insert = 1,
        InsertIgnore = 2,
        Replace = 3,
        OnDuplicateKeyUpdate = 4,
        Update = 5
    }
}