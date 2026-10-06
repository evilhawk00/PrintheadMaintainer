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
using PrintheadMaintainerUI.Models;
using PrintheadMaintainerUI.Status;
using System;

namespace PrintheadMaintainerUI.Presentation
{
    /// <summary>
    /// What the home page, the window icon, the tray icon and the footer show for a status. All
    /// of them use this one summary so that they always agree.
    /// </summary>
    public sealed class StatusSummary
    {
        private const string Unknown = "--";

        // The last print is shown in red when it is this much later than the interval.
        private static readonly TimeSpan OverdueMargin = TimeSpan.FromDays(3);

        private StatusSummary()
        {
        }

        public ServiceState State { get; private set; }

        public string Title { get; private set; }

        public string Problem { get; private set; }

        public ValueState ProblemState { get; private set; }

        public string ScheduledPrinting { get; private set; }

        public ValueState ScheduledPrintingState { get; private set; }

        public string LastPrint { get; private set; }

        public ValueState LastPrintState { get; private set; }

        public string NextScheduledPrint { get; private set; }

        public string Interval { get; private set; }

        public static StatusSummary Create(ServiceConnection connection, ServiceStatus status, DateTime nowUtc)
        {
            if (status == null)
            {
                bool connecting = connection == ServiceConnection.Connecting;
                return new StatusSummary
                {
                    State = connecting ? ServiceState.Unknown : ServiceState.Error,
                    Title = connecting ? "Connecting..." : "Service unavailable",
                    Problem = connecting ? "Checking..." : "Cannot connect to the Printhead Maintainer service",
                    ProblemState = connecting ? ValueState.Warning : ValueState.Error,
                    ScheduledPrinting = Unknown,
                    ScheduledPrintingState = ValueState.Warning,
                    LastPrint = Unknown,
                    LastPrintState = ValueState.Warning,
                    NextScheduledPrint = Unknown,
                    Interval = Unknown,
                };
            }

            var summary = new StatusSummary
            {
                ScheduledPrinting = status.Enabled ? "ON" : "OFF",
                ScheduledPrintingState = status.Enabled ? ValueState.OK : ValueState.Error,
                Interval = DisplayText.Count(status.IntervalDays, "day"),
            };

            if (!status.NextScheduledPrintUtc.HasValue)
            {
                summary.NextScheduledPrint = Unknown;
            }
            else
            {
                summary.NextScheduledPrint = status.NextScheduledPrintUtc.Value <= nowUtc
                    ? "Due now"
                    : DisplayText.FormatTime(status.NextScheduledPrintUtc.Value);
            }

            if (!status.LastPrintUtc.HasValue)
            {
                summary.LastPrint = "Never";
                summary.LastPrintState = ValueState.Warning;
            }
            else
            {
                DateTime lastPrint = status.LastPrintUtc.Value;
                summary.LastPrint = DisplayText.FormatTimeAgo(lastPrint, nowUtc);
                bool overdue = nowUtc - lastPrint > TimeSpan.FromDays(status.IntervalDays) + OverdueMargin;
                summary.LastPrintState = overdue ? ValueState.Error : ValueState.OK;
            }

            string settingsProblem = FindSettingsProblem(status);
            if (settingsProblem != null)
            {
                summary.State = ServiceState.Error;
                summary.Title = "Not functioning";
                summary.Problem = settingsProblem;
                summary.ProblemState = ValueState.Error;
            }
            else if (status.HasUnresolvedScheduledFailure)
            {
                PrintFailure failure = status.LastScheduledFailure;
                summary.State = ServiceState.Warning;
                summary.Title = "Attention required";
                summary.Problem = "Scheduled printing failed at " + DisplayText.FormatTime(failure.TimeUtc) + ". " +
                    DisplayText.Describe(failure.Reason) + ".";
                summary.ProblemState = ValueState.Warning;
            }
            else
            {
                summary.State = ServiceState.OK;
                summary.Title = "In good shape";
                summary.Problem = "None";
                summary.ProblemState = ValueState.OK;
            }
            return summary;
        }

        private static string FindSettingsProblem(ServiceStatus status)
        {
            if (!status.Enabled)
            {
                return "Scheduled printing is disabled";
            }
            if (!status.IsPrinterSelected)
            {
                return "No printer has been selected";
            }
            if (!status.ImageAvailable)
            {
                return "The image for printing is missing";
            }
            return null;
        }
    }
}
