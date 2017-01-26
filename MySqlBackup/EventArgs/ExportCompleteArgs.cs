using System;

namespace MySql.Data.MySqlClient
{
    public class ExportCompleteArgs
    {
        public ExportCompleteArgs(DateTime timeStart, DateTime timeEnd, MySqlBackup.ProcessEndType endType,
            Exception exception)
        {
            CompletionType = endType;
            TimeStart = timeStart;
            TimeEnd = timeEnd;
            TimeUsed = timeStart - timeEnd;
            LastError = exception;
        }

        /// <summary>
        ///     The Starting time of export process.
        /// </summary>
        public DateTime TimeStart { get; }

        /// <summary>
        ///     The Ending time of export process.
        /// </summary>
        public DateTime TimeEnd { get; }

        /// <summary>
        ///     Total time used in current export process.
        /// </summary>
        public TimeSpan TimeUsed { get; }

        public MySqlBackup.ProcessEndType CompletionType { get; }

        public Exception LastError { get; }

        public bool HasError => LastError != null;
    }
}