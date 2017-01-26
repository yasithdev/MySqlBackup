using System;

namespace MySql.Data.MySqlClient
{
    public class GetTotalRowsArgs : EventArgs
    {
        private int _curTable;
        private int _totalTables;

        public GetTotalRowsArgs(int totalTables, int curTable)
        {
            _totalTables = totalTables;
            _curTable = curTable;
        }
    }
}