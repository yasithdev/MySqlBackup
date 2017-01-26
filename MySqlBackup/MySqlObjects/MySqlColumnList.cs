using System;
using System.Collections.Generic;

namespace MySql.Data.MySqlClient
{
    public class MySqlColumnList : IDisposable
    {
        private readonly string _tableName;
        private List<MySqlColumn> _lst = new List<MySqlColumn>();

        public MySqlColumnList()
        {
        }

        public MySqlColumnList(MySqlCommand cmd, string tableName)
        {
            _tableName = tableName;
            var dtDataType = QueryExpress.GetTable(cmd, $"SELECT * FROM  `{_tableName}` LIMIT 0;");

            SqlShowFullColumns = $"SHOW FULL COLUMNS FROM `{_tableName}`;";
            var dtColInfo = QueryExpress.GetTable(cmd, SqlShowFullColumns);

            for (var i = 0; i < dtDataType.Columns.Count; i++)
            {
                var isNullStr = (dtColInfo.Rows[i]["Null"] + "").ToLower();
                var isNull = isNullStr == "yes";

                _lst.Add(new MySqlColumn(
                    dtDataType.Columns[i].ColumnName,
                    dtDataType.Columns[i].DataType,
                    dtColInfo.Rows[i]["Type"] + "",
                    dtColInfo.Rows[i]["Collation"] + "",
                    isNull,
                    dtColInfo.Rows[i]["Key"] + "",
                    dtColInfo.Rows[i]["Default"] + "",
                    dtColInfo.Rows[i]["Extra"] + "",
                    dtColInfo.Rows[i]["Privileges"] + "",
                    dtColInfo.Rows[i]["Comment"] + ""));
            }
        }

        public string SqlShowFullColumns { get; } = "";

        public MySqlColumn this[int columnIndex] => _lst[columnIndex];

        public MySqlColumn this[string columnName]
        {
            get
            {
                foreach (var t in _lst)
                    if (t.Name == columnName)
                        return t;
                throw new Exception("Column \"" + columnName + "\" is not existed in table \"" + _tableName + "\".");
            }
        }

        public int Count => _lst.Count;

        public void Dispose()
        {
            for (var i = 0; i < _lst.Count; i++)
                _lst[i] = null;
            _lst = null;
        }

        public bool Contains(string columnName)
        {
            return this[columnName] != null;
        }

        public IEnumerator<MySqlColumn> GetEnumerator()
        {
            return _lst.GetEnumerator();
        }
    }
}