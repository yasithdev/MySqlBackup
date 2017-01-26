using System;
using System.Linq;

namespace MySql.Data.MySqlClient
{
    public class MySqlColumn
    {
        public enum DataWrapper
        {
            None,
            Sql
        }

        private readonly int _timeFractionLength;

        public MySqlColumn(string name, Type type, string mySqlDataType,
            string collation, bool allowNull, string key, string defaultValue,
            string extra, string privileges, string comment)
        {
            Name = name;
            DataType = type;
            MySqlDataType = mySqlDataType.ToLower();
            Collation = collation;
            AllowNull = allowNull;
            Key = key;
            DefaultValue = defaultValue;
            Extra = extra;
            Privileges = privileges;
            Comment = comment;

            if (key.ToLower() == "pri")
                IsPrimaryKey = true;

            if (DataType != typeof(DateTime) || MySqlDataType.Length <= 8) return;

            var fractionLength = MySqlDataType.Where(char.IsNumber)
                .Aggregate("", (current, _dL) => current + Convert.ToString(_dL));

            if (fractionLength.Length <= 0) return;
            _timeFractionLength = 0;
            int.TryParse(fractionLength, out _timeFractionLength);
        }

        public string Name { get; }
        public Type DataType { get; }
        public string MySqlDataType { get; }
        public string Collation { get; }
        public bool AllowNull { get; }
        public string Key { get; }
        public string DefaultValue { get; }
        public string Extra { get; }
        public string Privileges { get; }
        public string Comment { get; }
        public bool IsPrimaryKey { get; }
        public int TimeFractionLength => _timeFractionLength;
        public bool IsGenerated => Extra.Contains("GENERATED");
    }
}