using System;
using System.Collections.Generic;
using System.Data;

namespace MySql.Data.MySqlClient
{
    public class MySqlViewList : IDisposable
    {
        private List<MySqlView> _lst = new List<MySqlView>();

        public MySqlViewList()
        {
        }

        public MySqlViewList(MySqlCommand cmd)
        {
            try
            {
                var dbname = QueryExpress.ExecuteScalarStr(cmd, "SELECT DATABASE();");
                SqlShowViewList = string.Format("SHOW FULL TABLES FROM `{0}` WHERE Table_type = 'VIEW';", dbname);
                var dt = QueryExpress.GetTable(cmd, SqlShowViewList);

                foreach (DataRow dr in dt.Rows)
                    _lst.Add(new MySqlView(cmd, dr[0] + ""));
            }
            catch (MySqlException myEx)
            {
                if (myEx.Message.ToLower().Contains("access denied"))
                    AllowAccess = false;
            }
            catch
            {
                throw;
            }
        }

        public bool AllowAccess { get; } = true;

        public string SqlShowViewList { get; } = "";

        public MySqlView this[int viewIndex]
        {
            get { return _lst[viewIndex]; }
        }

        public MySqlView this[string viewName]
        {
            get
            {
                for (var i = 0; i < _lst.Count; i++)
                    if (_lst[i].Name == viewName)
                        return _lst[i];
                throw new Exception("View \"" + viewName + "\" is not existed.");
            }
        }

        public int Count
        {
            get { return _lst.Count; }
        }

        public void Dispose()
        {
            for (var i = 0; i < _lst.Count; i++)
                _lst[i] = null;
            _lst = null;
        }

        public bool Contains(string viewName)
        {
            if (this[viewName] == null)
                return false;
            return true;
        }

        public IEnumerator<MySqlView> GetEnumerator()
        {
            return _lst.GetEnumerator();
        }
    }
}