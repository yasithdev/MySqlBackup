using System;

namespace MySql.Data.MySqlClient
{
    public class ImportCompleteArgs
    {
        /// <summary>
        ///     Enum of completion type
        /// </summary>
        public enum CompleteType
        {
            Completed,
            Cancelled,
            Error
        }

        /// <summary>
        ///     The last error (exception) occur in import process.
        /// </summary>
        public readonly Exception LastError = null;

        /// <summary>
        ///     The completion type of current import processs.
        /// </summary>
        public CompleteType CompletedType = CompleteType.Completed;

        /// <summary>
        ///     The ending time of import process.
        /// </summary>
        public DateTime TimeEnd;

        /// <summary>
        ///     The starting time of import process.
        /// </summary>
        public DateTime TimeStart;

        /// <summary>
        ///     Indicates whether the import process has error(s).
        /// </summary>
        public bool HasErrors => LastError != null;

        /// <summary>
        ///     Total time used in current import process.
        /// </summary>
        public TimeSpan TimeUsed => TimeEnd - TimeStart;
    }
}