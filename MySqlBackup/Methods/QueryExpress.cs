using System;
using System.Data;
using System.Globalization;
using System.Text;
using MySql.Data.Types;

namespace MySql.Data.MySqlClient
{
    public static class QueryExpress
    {
        public static NumberFormatInfo MySqlNumberFormat { get; } = new NumberFormatInfo
        {
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = string.Empty
        };

        public static DateTimeFormatInfo MySqlDateTimeFormat { get; } = new DateTimeFormatInfo
        {
            DateSeparator = "-",
            TimeSeparator = ":"
        };

        public static DataTable GetTable(MySqlCommand cmd, string sql)
        {
            var dt = new DataTable();
            cmd.CommandText = sql;
            using (var da = new MySqlDataAdapter(cmd))
            {
                da.Fill(dt);
            }
            return dt;
        }

        public static string ExecuteScalarStr(MySqlCommand cmd, string sql)
        {
            cmd.CommandText = sql;
            var ob = cmd.ExecuteScalar();
            var bytes = ob as byte[];
            if (bytes != null)
                return Encoding.UTF8.GetString(bytes);
            return ob + "";
        }

        public static string ExecuteScalarStr(MySqlCommand cmd, string sql, int columnIndex)
        {
            var dt = GetTable(cmd, sql);

            var bytes = dt.Rows[0][columnIndex] as byte[];
            if (bytes != null)
                return Encoding.UTF8.GetString(bytes);
            return dt.Rows[0][columnIndex] + "";
        }

        public static string ExecuteScalarStr(MySqlCommand cmd, string sql, string columnName)
        {
            var dt = GetTable(cmd, sql);

            var bytes = dt.Rows[0][columnName] as byte[];
            if (bytes != null)
                return Encoding.UTF8.GetString(bytes);
            return dt.Rows[0][columnName] + "";
        }

        public static long ExecuteScalarLong(MySqlCommand cmd, string sql)
        {
            long l;
            cmd.CommandText = sql;
            long.TryParse(cmd.ExecuteScalar() + "", out l);
            return l;
        }

        public static string EscapeStringSequence(string data)
        {
            var builder = new StringBuilder();
            foreach (var ch in data)
                switch (ch)
                {
                    case '\\': // Backslash
                        builder.AppendFormat("\\\\");
                        break;
                    case '\r': // Carriage return
                        builder.AppendFormat("\\r");
                        break;
                    case '\n': // New Line
                        builder.AppendFormat("\\n");
                        break;
                    //case '\a': // Vertical tab
                    //    builder.AppendFormat("\\a");
                    //    break;
                    case '\b': // Backspace
                        builder.AppendFormat("\\b");
                        break;
                    //case '\f': // Formfeed
                    //    builder.AppendFormat("\\f");
                    //    break;
                    case '\t': // Horizontal tab
                        builder.AppendFormat("\\t");
                        break;
                    //case '\v': // Vertical tab
                    //    builder.AppendFormat("\\v");
                    //    break;
                    case '\"': // Double quotation mark
                        builder.AppendFormat("\\\"");
                        break;
                    case '\'': // Single quotation mark
                        builder.AppendFormat("''");
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }

            return builder.ToString();
        }

        public static string EraseDefiner(string input)
        {
            var sb = new StringBuilder();
            var definer = " DEFINER=";
            var dIndex = input.IndexOf(definer, StringComparison.Ordinal);

            sb.AppendFormat(definer);

            var pointAliasReached = false;
            var point3RdQuoteReached = false;

            for (var i = dIndex + definer.Length; i < input.Length; i++)
            {
                if (!pointAliasReached)
                {
                    if (input[i] == '@')
                        pointAliasReached = true;

                    sb.Append(input[i]);
                    continue;
                }

                if (!point3RdQuoteReached)
                {
                    if (input[i] == '`')
                        point3RdQuoteReached = true;

                    sb.Append(input[i]);
                    continue;
                }

                if (input[i] != '`')
                {
                    sb.Append(input[i]);
                }
                else
                {
                    sb.Append(input[i]);
                    break;
                }
            }

            return input.Replace(sb.ToString(), string.Empty);
        }

        public static string ConvertToSqlFormat(object ob, bool wrapStringWithSingleQuote, bool escapeStringSequence,
            MySqlColumn col)
        {
            var sb = new StringBuilder();

            if (ob == null || ob is DBNull)
            {
                sb.AppendFormat("NULL");
            }
            else if (ob is string)
            {
                var str = (string) ob;

                if (escapeStringSequence)
                    str = EscapeStringSequence(str);

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.Append(str);

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is bool)
            {
                sb.AppendFormat(Convert.ToInt32(ob).ToString());
            }
            else if (ob is byte[])
            {
                if (((byte[]) ob).Length == 0)
                    if (wrapStringWithSingleQuote)
                        return "''";
                    else
                        return "";
                sb.AppendFormat(CryptoExpress.ConvertByteArrayToHexString((byte[]) ob));
            }
            else if (ob is short)
            {
                sb.AppendFormat(((short) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is int)
            {
                sb.AppendFormat(((int) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is long)
            {
                sb.AppendFormat(((long) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is ushort)
            {
                sb.AppendFormat(((ushort) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is uint)
            {
                sb.AppendFormat(((uint) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is ulong)
            {
                sb.AppendFormat(((ulong) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is double)
            {
                sb.AppendFormat(((double) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is decimal)
            {
                sb.AppendFormat(((decimal) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is float)
            {
                sb.AppendFormat(((float) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is byte)
            {
                sb.AppendFormat(((byte) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is sbyte)
            {
                sb.AppendFormat(((sbyte) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is TimeSpan)
            {
                var ts = (TimeSpan) ob;

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.AppendFormat(ts.Hours.ToString().PadLeft(2, '0'));
                sb.AppendFormat(":");
                sb.AppendFormat(ts.Minutes.ToString().PadLeft(2, '0'));
                sb.AppendFormat(":");
                sb.AppendFormat(ts.Seconds.ToString().PadLeft(2, '0'));

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is DateTime)
            {
                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.AppendFormat(((DateTime) ob).ToString("yyyy-MM-dd HH:mm:ss", MySqlDateTimeFormat));

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is MySqlDateTime)
            {
                var mdt = (MySqlDateTime) ob;

                if (mdt.IsNull)
                {
                    sb.AppendFormat("NULL");
                }
                else
                {
                    if (mdt.IsValidDateTime)
                    {
                        var dtime = mdt.Value;

                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");

                        if (col.MySqlDataType == "datetime")
                            sb.AppendFormat(dtime.ToString("yyyy-MM-dd HH:mm:ss", MySqlDateTimeFormat));
                        else if (col.MySqlDataType == "date")
                            sb.AppendFormat(dtime.ToString("yyyy-MM-dd", MySqlDateTimeFormat));
                        else if (col.MySqlDataType == "time")
                            sb.AppendFormat(dtime.ToString("HH:mm:ss", MySqlDateTimeFormat));
                        else
                            sb.AppendFormat(dtime.ToString("yyyy-MM-dd HH:mm:ss", MySqlDateTimeFormat));

                        if (dtime.Millisecond > 0)
                        {
                            sb.Append(".");
                            sb.Append(dtime.Millisecond);
                        }

                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");
                    }
                    else
                    {
                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");

                        if (col.MySqlDataType == "datetime")
                            sb.AppendFormat("0000-00-00 00:00:00");
                        else if (col.MySqlDataType == "date")
                            sb.AppendFormat("0000-00-00");
                        else if (col.MySqlDataType == "time")
                            sb.AppendFormat("00:00:00");
                        else
                            sb.AppendFormat("0000-00-00 00:00:00");

                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");
                    }
                }
            }
            else if (ob is Guid)
            {
                if (col.MySqlDataType == "binary(16)")
                {
                    sb.Append(CryptoExpress.ConvertByteArrayToHexString(((Guid) ob).ToByteArray()));
                }
                else if (col.MySqlDataType == "char(36)")
                {
                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("'");

                    sb.Append(ob);

                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("'");
                }
                else
                {
                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("'");

                    sb.Append(ob);

                    if (wrapStringWithSingleQuote)
                        sb.AppendFormat("'");
                }
            }
            else
            {
                throw new Exception("Unhandled data type. Current processing data type: " + ob.GetType() +
                                    ". Please report this bug with this message to the development team.");
            }
            return sb.ToString();
        }

        public static string ConvertToSqlFormat(MySqlDataReader rdr, int colIndex, bool wrapStringWithSingleQuote,
            bool escapeStringSequence, MySqlColumn col)
        {
            var ob = rdr[colIndex];

            var sb = new StringBuilder();

            if (ob == null || ob is DBNull)
            {
                sb.AppendFormat("NULL");
            }
            else if (ob is string)
            {
                var str = (string) ob;

                if (escapeStringSequence)
                    str = EscapeStringSequence(str);

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.Append(str);

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is bool)
            {
                sb.AppendFormat(Convert.ToInt32(ob).ToString());
            }
            else if (ob is byte[])
            {
                if (((byte[]) ob).Length == 0)
                    if (wrapStringWithSingleQuote)
                        return "''";
                    else
                        return "";
                sb.AppendFormat(CryptoExpress.ConvertByteArrayToHexString((byte[]) ob));
            }
            else if (ob is short)
            {
                sb.AppendFormat(((short) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is int)
            {
                sb.AppendFormat(((int) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is long)
            {
                sb.AppendFormat(((long) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is ushort)
            {
                sb.AppendFormat(((ushort) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is uint)
            {
                sb.AppendFormat(((uint) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is ulong)
            {
                sb.AppendFormat(((ulong) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is double)
            {
                sb.AppendFormat(((double) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is decimal)
            {
                sb.AppendFormat(((decimal) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is float)
            {
                sb.AppendFormat(((float) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is byte)
            {
                sb.AppendFormat(((byte) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is sbyte)
            {
                sb.AppendFormat(((sbyte) ob).ToString(MySqlNumberFormat));
            }
            else if (ob is TimeSpan)
            {
                var ts = (TimeSpan) ob;

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.AppendFormat(ts.Hours.ToString().PadLeft(2, '0'));
                sb.AppendFormat(":");
                sb.AppendFormat(ts.Minutes.ToString().PadLeft(2, '0'));
                sb.AppendFormat(":");
                sb.AppendFormat(ts.Seconds.ToString().PadLeft(2, '0'));

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is DateTime)
            {
                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");

                sb.AppendFormat(((DateTime) ob).ToString("yyyy-MM-dd HH:mm:ss", MySqlDateTimeFormat));

                if (col.TimeFractionLength > 0)
                {
                    sb.Append(".");
                    var microsecond = rdr.GetMySqlDateTime(colIndex).Microsecond.ToString();
                    if (microsecond.Length < col.TimeFractionLength)
                        microsecond = microsecond.PadLeft(col.TimeFractionLength, '0');
                    else if (microsecond.Length > col.TimeFractionLength)
                        microsecond = microsecond.Substring(0, col.TimeFractionLength);
                    sb.Append(microsecond.PadLeft(col.TimeFractionLength, '0'));
                }

                if (wrapStringWithSingleQuote)
                    sb.AppendFormat("'");
            }
            else if (ob is MySqlDateTime)
            {
                var mdt = (MySqlDateTime) ob;

                if (mdt.IsNull)
                {
                    sb.AppendFormat("NULL");
                }
                else
                {
                    if (mdt.IsValidDateTime)
                    {
                        var dtime = mdt.Value;

                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");

                        if (col.MySqlDataType == "datetime")
                            sb.AppendFormat(dtime.ToString("yyyy-MM-dd HH:mm:ss", MySqlDateTimeFormat));
                        else if (col.MySqlDataType == "date")
                            sb.AppendFormat(dtime.ToString("yyyy-MM-dd", MySqlDateTimeFormat));
                        else if (col.MySqlDataType == "time")
                            sb.AppendFormat(dtime.ToString("HH:mm:ss", MySqlDateTimeFormat));
                        else
                            sb.AppendFormat(dtime.ToString("yyyy-MM-dd HH:mm:ss", MySqlDateTimeFormat));

                        if (col.TimeFractionLength > 0)
                        {
                            sb.Append(".");
                            sb.Append(((MySqlDateTime) ob).Microsecond.ToString().PadLeft(col.TimeFractionLength, '0'));
                        }

                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");
                    }
                    else
                    {
                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");

                        if (col.MySqlDataType == "datetime")
                            sb.AppendFormat("0000-00-00 00:00:00");
                        else if (col.MySqlDataType == "date")
                            sb.AppendFormat("0000-00-00");
                        else if (col.MySqlDataType == "time")
                            sb.AppendFormat("00:00:00");
                        else
                            sb.AppendFormat("0000-00-00 00:00:00");

                        if (col.TimeFractionLength > 0)
                            sb.Append(".".PadRight(col.TimeFractionLength, '0'));

                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");
                    }
                }
            }
            else if (ob is Guid)
            {
                switch (col.MySqlDataType)
                {
                    case "binary(16)":
                        sb.Append(CryptoExpress.ConvertByteArrayToHexString(((Guid) ob).ToByteArray()));
                        break;
                    case "char(36)":
                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");

                        sb.Append(ob);

                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");
                        break;
                    default:
                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");

                        sb.Append(ob);

                        if (wrapStringWithSingleQuote)
                            sb.AppendFormat("'");
                        break;
                }
            }
            else
            {
                throw new Exception("Unhandled data type. Current processing data type: " + ob.GetType() +
                                    ". Please report this bug with this message to the development team.");
            }
            return sb.ToString();
        }
    }
}