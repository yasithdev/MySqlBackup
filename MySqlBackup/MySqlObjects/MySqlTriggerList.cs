using System;
using System.Collections.Generic;
using System.Data;

namespace MySql.Data.MySqlClient
{
    public class MySqlTriggerList : IDisposable
    {
        private List<MySqlTrigger> _lst = new List<MySqlTrigger>();

        public MySqlTriggerList()
        {
        }

        public MySqlTriggerList(MySqlCommand cmd)
        {
            SqlShowTriggers = "SHOW TRIGGERS;";
            try
            {
                var dt = QueryExpress.GetTable(cmd, SqlShowTriggers);

                foreach (DataRow dr in dt.Rows)
                    _lst.Add(new MySqlTrigger(cmd, dr["Trigger"] + "", dr["Definer"] + ""));
            }
            catch (MySqlException myEx)
            {
                if (myEx.Message.ToLower().Contains("access denied"))
                    AllowAccess = false;
            }
        }

        public bool AllowAccess { get; } = true;

        public string SqlShowTriggers { get; } = "";

        public MySqlTrigger this[int triggerIndex] => _lst[triggerIndex];

        public MySqlTrigger this[string triggerName]
        {
            get
            {
                foreach (var t in _lst)
                    if (t.Name == triggerName)
                        return t;
                throw new Exception("Trigger \"" + triggerName + "\" is not existed.");
            }
        }

        public int Count => _lst.Count;

        public void Dispose()
        {
            for (var i = 0; i < _lst.Count; i++)
                _lst[i] = null;
            _lst = null;
        }

        public bool Contains(string triggerName) => this[triggerName] != null;
        public IEnumerator<MySqlTrigger> GetEnumerator() => _lst.GetEnumerator();
    }
}