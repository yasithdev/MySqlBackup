using System;
using System.Collections.Generic;
using System.Data;

namespace MySql.Data.MySqlClient
{
    public class MySqlFunctionList : IDisposable
    {
        private List<MySqlFunction> _lst = new List<MySqlFunction>();

        public MySqlFunctionList()
        {
        }

        public MySqlFunctionList(MySqlCommand cmd)
        {
            try
            {
                var dbname = QueryExpress.ExecuteScalarStr(cmd, "SELECT DATABASE();");
                SqlShowFunctions = $"SHOW FUNCTION STATUS WHERE UPPER(TRIM(Db))= UPPER(TRIM('{dbname}'));";
                var dt = QueryExpress.GetTable(cmd, SqlShowFunctions);

                foreach (DataRow dr in dt.Rows)
                    _lst.Add(new MySqlFunction(cmd, dr["Name"] + "", dr["Definer"] + ""));
            }
            catch (MySqlException myEx)
            {
                if (myEx.Message.ToLower().Contains("access denied"))
                    AllowAccess = false;
            }
        }

        public bool AllowAccess { get; } = true;

        public string SqlShowFunctions { get; } = "";

        public MySqlFunction this[int functionIndex] => _lst[functionIndex];

        public MySqlFunction this[string functionName]
        {
            get
            {
                foreach (MySqlFunction t in _lst)
                    if (t.Name == functionName)
                        return t;
                throw new Exception("Function \"" + functionName + "\" is not existed.");
            }
        }

        public int Count => _lst.Count;

        public void Dispose()
        {
            for (var i = 0; i < _lst.Count; i++)
                _lst[i] = null;
            _lst = null;
        }

        public bool Contains(string functionName)=> this[functionName] != null;
        public IEnumerator<MySqlFunction> GetEnumerator() => _lst.GetEnumerator();
    }
}