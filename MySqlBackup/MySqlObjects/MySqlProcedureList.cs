using System;
using System.Collections.Generic;
using System.Data;

namespace MySql.Data.MySqlClient
{
    public class MySqlProcedureList : IDisposable
    {
        private List<MySqlProcedure> _lst = new List<MySqlProcedure>();

        public MySqlProcedureList()
        {
        }

        public MySqlProcedureList(MySqlCommand cmd)
        {
            try
            {
                var dbname = QueryExpress.ExecuteScalarStr(cmd, "SELECT DATABASE();");
                SqlShowProcedures = $"SHOW PROCEDURE STATUS WHERE UPPER(TRIM(Db))= UPPER(TRIM('{dbname}'));";
                var dt = QueryExpress.GetTable(cmd, SqlShowProcedures);

                foreach (DataRow dr in dt.Rows)
                    _lst.Add(new MySqlProcedure(cmd, dr["Name"] + "", dr["Definer"] + ""));
            }
            catch (MySqlException myEx)
            {
                if (myEx.Message.ToLower().Contains("access denied"))
                    AllowAccess = false;
            }
        }

        public bool AllowAccess { get; } = true;

        public string SqlShowProcedures { get; } = "";

        public MySqlProcedure this[int indexProcedure] => _lst[indexProcedure];

        public MySqlProcedure this[string procedureName]
        {
            get
            {
                foreach (var t in _lst)
                    if (t.Name == procedureName)
                        return t;
                throw new Exception("Store procedure \"" + procedureName + "\" is not existed.");
            }
        }

        public int Count => _lst.Count;

        public void Dispose()
        {
            for (var i = 0; i < _lst.Count; i++)
                _lst[i] = null;
            _lst = null;
        }

        public bool Contains(string procedureName) => this[procedureName] != null;
        public IEnumerator<MySqlProcedure> GetEnumerator() => _lst.GetEnumerator();
    }
}