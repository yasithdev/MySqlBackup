using System;
using System.Collections.Generic;
using System.Data;

namespace MySql.Data.MySqlClient
{
    public class MySqlTableList : IDisposable
    {
        private List<MySqlTable> _lst = new List<MySqlTable>();

        public MySqlTableList()
        {
        }

        public MySqlTableList(MySqlCommand cmd)
        {
            SqlShowFullTables = "SHOW FULL TABLES WHERE Table_type = 'BASE TABLE';";
            var dtTableList = QueryExpress.GetTable(cmd, SqlShowFullTables);

            foreach (DataRow dr in dtTableList.Rows)
                _lst.Add(new MySqlTable(cmd, (string) dr[0]));
        }

        public string SqlShowFullTables { get; } = "";

        public MySqlTable this[int tableIndex] => _lst[tableIndex];

        public MySqlTable this[string tableName]
        {
            get
            {
                foreach (var t in _lst)
                    if (t.Name == tableName)
                        return t;
                throw new Exception("Table \"" + tableName + "\" is not existed.");
            }
        }

        public int Count => _lst.Count;

        public void Dispose()
        {
            for (var i = 0; i < _lst.Count; i++)
            {
                _lst[i].Dispose();
                _lst[i] = null;
            }
            _lst = null;
        }

        public IEnumerator<MySqlTable> GetEnumerator()
        {
            return _lst.GetEnumerator();
        }
    }
}