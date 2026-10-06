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
using PrintheadMaintainerUI.Enums;
using System;

namespace PrintheadMaintainerUI.Models
{
    /// <summary>
    /// The service's settings and print history at one point in time. The service owns all of
    /// this state; the UI only shows it and asks the service to change it. Times are UTC.
    /// </summary>
    public sealed class ServiceStatus
    {
        public PrintState PrintState { get; set; }

        /// <summary>A manual print was requested and has not finished yet.</summary>
        public bool ManualPrintPending { get; set; }

        public bool Enabled { get; set; }

        public int IntervalDays { get; set; }

        /// <summary>Empty until a printer is selected.</summary>
        public string PrinterName { get; set; } = string.Empty;

        /// <summary>The paper source (DEVMODE dmDefaultSource); 0 uses the printer default.</summary>
        public int PaperSource { get; set; }

        /// <summary>False when the image installed with the program is printed.</summary>
        public bool CustomImage { get; set; }

        /// <summary>The name of the file the custom image was made from, for display only.</summary>
        public string ImageSourceName { get; set; } = string.Empty;

        /// <summary>The file the service prints.</summary>
        public string ImagePath { get; set; } = string.Empty;

        public bool ImageAvailable { get; set; }

        public DateTime? LastPrintUtc { get; set; }

        /// <summary>When the user last marked the printer as printed; null if never.</summary>
        public DateTime? LastMarkedPrintUtc { get; set; }

        public PrintFailure LastScheduledFailure { get; set; }

        public PrintFailure LastManualFailure { get; set; }

        /// <summary>Null when scheduled printing is disabled or no printer is selected.</summary>
        public DateTime? NextScheduledPrintUtc { get; set; }

        /// <summary>The next scheduled print waits for the end of a postponement.</summary>
        public bool IsNextScheduledPrintPostponed { get; set; }

        public bool IsPrinterSelected => PrinterName.Length > 0;

        /// <summary>
        /// What the schedule counts from: the last print, or a later mark as printed. As in the
        /// service, a time in the future (the clock was turned back) is left out.
        /// </summary>
        public DateTime? LastMaintenanceUtc
        {
            get
            {
                DateTime now = DateTime.UtcNow;
                DateTime? last = null;
                foreach (DateTime? time in new[] { LastPrintUtc, LastMarkedPrintUtc })
                {
                    if (time.HasValue && time.Value <= now && (!last.HasValue || time.Value > last.Value))
                    {
                        last = time;
                    }
                }
                return last;
            }
        }

        /// <summary>The schedule counts from a mark as printed rather than from a print.</summary>
        public bool IsMarkedAsPrinted => LastMaintenanceUtc.HasValue && LastMaintenanceUtc == LastMarkedPrintUtc;

        public bool IsScheduledPrintDue(DateTime nowUtc)
        {
            return NextScheduledPrintUtc.HasValue && NextScheduledPrintUtc.Value <= nowUtc;
        }

        /// <summary>The next scheduled print waits for a postponement that has not ended yet.</summary>
        public bool IsPostponed(DateTime nowUtc)
        {
            return IsNextScheduledPrintPostponed && NextScheduledPrintUtc.HasValue && NextScheduledPrintUtc.Value > nowUtc;
        }

        public bool IsBusy => PrintState != PrintState.Idle || ManualPrintPending;

        /// <summary>The last scheduled print failed and nothing has been printed (or marked) since.</summary>
        public bool HasUnresolvedScheduledFailure => IsUnresolved(LastScheduledFailure);

        /// <summary>The last manual print failed and nothing has been printed (or marked) since.</summary>
        public bool HasUnresolvedManualFailure => IsUnresolved(LastManualFailure);

        private bool IsUnresolved(PrintFailure failure)
        {
            DateTime? lastMaintenance = LastMaintenanceUtc;
            return failure != null && (!lastMaintenance.HasValue || failure.TimeUtc > lastMaintenance.Value);
        }
    }
}
