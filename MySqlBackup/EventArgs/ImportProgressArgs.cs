using System;

namespace MySql.Data.MySqlClient
{
    public class ImportProgressArgs : EventArgs
    {
        public ImportProgressArgs(long currentBytes, long totalBytes)
        {
            CurrentBytes = currentBytes;
            TotalBytes = totalBytes;
        }

        /// <summary>
        ///     Number of processed bytes in current import process.
        /// </summary>
        public long CurrentBytes { get; }

        /// <summary>
        ///     Total bytes to be processed.
        /// </summary>
        public long TotalBytes { get; }

        /// <summary>
        ///     Percentage of completeness.
        /// </summary>
        public int PercentageCompleted => (int) (CurrentBytes * 100L / TotalBytes);
    }
}