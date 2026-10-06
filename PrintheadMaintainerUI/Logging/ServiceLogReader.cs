/*
*
* Copyright (C) 2026  YAN-LIN, CHEN
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <https://www.gnu.org/licenses/>.
*
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PrintheadMaintainerUI.Logging
{
    /// <summary>
    /// Reads the service's log (Logging/ServiceLog.h in the service). The service appends one line
    /// per entry to Data\Logs in the installation folder and moves the file to a backup when it
    /// grows too large.
    /// </summary>
    public static class ServiceLogReader
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Logs");
        private const string FileName = "PrintheadMaintainer.log";
        private const string BackupFileName = "PrintheadMaintainer.1.log";

        /// <summary>The entries, newest first; empty if nothing has been logged yet.</summary>
        /// <exception cref="IOException">A log file cannot be read.</exception>
        /// <exception cref="UnauthorizedAccessException">The user may not read the log.</exception>
        public static IReadOnlyList<string> ReadNewestFirst()
        {
            var entries = new List<string>();
            ReadEntries(Path.Combine(LogDirectory, BackupFileName), entries);
            ReadEntries(Path.Combine(LogDirectory, FileName), entries);
            entries.Reverse();
            return entries;
        }

        private static void ReadEntries(string path, List<string> entries)
        {
            try
            {
                // The service keeps writing to the file and may replace it while it is read.
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length > 0)
                        {
                            entries.Add(line);
                        }
                    }
                }
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                // Nothing has been logged to this file yet.
            }
        }
    }
}
