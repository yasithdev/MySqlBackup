using System;
using System.Collections.Generic;
using System.Data;

namespace MySql.Data.MySqlClient
{
    public class MySqlEventList : IDisposable
    {
        private List<MySqlEvent> _lst = new List<MySqlEvent>();

        public MySqlEventList()
        {
        }

        public MySqlEventList(MySqlCommand cmd)
        {
            try
            {
                var dbname = QueryExpress.ExecuteScalarStr(cmd, "SELECT DATABASE();");
                SqlShowEvent = $"SHOW EVENTS WHERE UPPER(TRIM(Db))=UPPER(TRIM('{dbname}'));";
                var dt = QueryExpress.GetTable(cmd, SqlShowEvent);

                foreach (DataRow dr in dt.Rows)
                    _lst.Add(new MySqlEvent(cmd, dr["Name"] + "", dr["Definer"] + ""));
            }
            catch (MySqlException myEx)
            {
                if (myEx.Message.ToLower().Contains("access denied"))
                    AllowAccess = false;
            }
        }

        public bool AllowAccess { get; } = true;

        public string SqlShowEvent { get; } = "";

        public MySqlEvent this[int eventIndex] => _lst[eventIndex];

        public MySqlEvent this[string eventName]
        {
            get
            {
                foreach (var t in _lst)
                    if (t.Name == eventName)
                        return t;
                throw new Exception("Event \"" + eventName + "\" is not existed.");
            }
        }

        public int Count => _lst.Count;

        public void Dispose()
        {
            for (var i = 0; i < _lst.Count; i++)
                _lst[i] = null;
            _lst = null;
        }

        public bool Contains(string eventName)
        {
            return this[eventName] != null;
        }

        public IEnumerator<MySqlEvent> GetEnumerator()
        {
            return _lst.GetEnumerator();
        }
    }
}