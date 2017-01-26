using System;

namespace MySql.Data.MySqlClient
{
    public class ExportProgressArgs : EventArgs
    {
        public ExportProgressArgs(string currentTableName,
            long totalRowsInCurrentTable,
            long totalRowsInAllTables,
            long currentRowIndexInCurrentTable,
            long currentRowIndexInAllTable,
            int totalTables,
            int currentTableIndex)
        {
            CurrentTableName = currentTableName;
            TotalRowsInCurrentTable = totalRowsInCurrentTable;
            TotalRowsInAllTables = totalRowsInAllTables;
            CurrentRowIndexInCurrentTable = currentRowIndexInCurrentTable;
            CurrentRowIndexInAllTables = currentRowIndexInAllTable;
            TotalTables = totalTables;
            CurrentTableIndex = currentTableIndex;
        }

        public string CurrentTableName { get; }
        public long TotalRowsInCurrentTable { get; }
        public long TotalRowsInAllTables { get; }
        public long CurrentRowIndexInCurrentTable { get; }
        public long CurrentRowIndexInAllTables { get; }
        public int TotalTables { get; }
        public int CurrentTableIndex { get; }
    }
}