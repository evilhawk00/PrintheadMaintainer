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
using PrintheadMaintainerUI.NamedPipeClient;
using PrintheadMaintainerUI.Presentation;
using System;
using System.Threading.Tasks;

namespace PrintheadMaintainerUI.Status
{
    /// <summary>
    /// Changes when the next scheduled print happens, for the home page, the postpone page, the
    /// tray menu and the notifications. Every change is worked out from the service's status at
    /// that moment, so that changes made in quick succession add up, and the status the UI shows
    /// is refreshed after it.
    /// </summary>
    public sealed class ScheduleActions
    {
        // The service accepts postponements up to 730 days ahead (Settings/ServiceSettings.h).
        private static readonly TimeSpan MaxPostponement = TimeSpan.FromDays(730);

        private readonly ServiceClient _client;
        private readonly StatusMonitor _monitor;

        public ScheduleActions(ServiceClient client, StatusMonitor monitor)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        /// <summary>Scheduled printing is on and a printer is selected, so there is a next print.</summary>
        public bool HasNextPrint => _monitor.Status?.NextScheduledPrintUtc != null;

        /// <summary>The next print waits for a postponement that has not ended yet.</summary>
        public bool IsPostponed => _monitor.Status?.IsPostponed(DateTime.UtcNow) == true;

        /// <summary>The next scheduled print as shown, or now if it is due; null if there is none.</summary>
        public DateTime? NextPrintUtc => NextPrint(_monitor.Status);

        /// <summary>The last day the next print can be postponed to.</summary>
        public DateTime LastPostponementDay => DateTime.Now.Add(MaxPostponement).Date.AddDays(-1);

        /// <summary>Moves the next print later by the given time.</summary>
        public async Task<ScheduleChange> PostponeAsync(TimeSpan delay)
        {
            ServiceStatus status = await _client.GetStatusAsync();
            return CannotMoveNextPrint(status) ??
                await PostponeUntilAsync(NextPrint(status).Value + delay, "The next print is postponed to {0}.");
        }

        /// <summary>Leaves out the next print: the one after it follows one interval later.</summary>
        public async Task<ScheduleChange> SkipNextPrintAsync()
        {
            ServiceStatus status = await _client.GetStatusAsync();
            return CannotMoveNextPrint(status) ??
                await PostponeUntilAsync(NextPrint(status).Value.AddDays(status.IntervalDays),
                    "The next print is skipped. The one after it is on {0}.");
        }

        /// <summary>Moves the next print to the given day, at the time of day it has now.</summary>
        public async Task<ScheduleChange> PostponeToDayAsync(DateTime localDay)
        {
            ServiceStatus status = await _client.GetStatusAsync();
            ScheduleChange cannotMove = CannotMoveNextPrint(status);
            if (cannotMove != null)
            {
                return cannotMove;
            }
            DateTime local = localDay.Date + NextPrint(status).Value.ToLocalTime().TimeOfDay;
            return await PostponeUntilAsync(DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime(),
                "The next print is postponed to {0}.");
        }

        public async Task<ScheduleChange> ResumeScheduleAsync()
        {
            ScheduleChangeResult result = await _client.PostponeAsync(null);
            return Finish(result, "The postponement was removed.");
        }

        /// <summary>Counts the printer as printed now, for example after something else was printed on it.</summary>
        public async Task<ScheduleChange> MarkPrintedAsync()
        {
            ServiceStatus status = await _client.GetStatusAsync();
            if (status == null)
            {
                return NotConnected();
            }

            // A postponement that ends later still holds.
            DateTime next = DateTime.UtcNow.AddDays(status.IntervalDays);
            if (status.IsNextScheduledPrintPostponed && status.NextScheduledPrintUtc > next)
            {
                next = status.NextScheduledPrintUtc.Value;
            }
            ScheduleChangeResult result = await _client.MarkPrintedAsync(false);
            return Finish(result, status.NextScheduledPrintUtc.HasValue
                ? "Marked as printed. The next print is on " + DisplayText.FormatTime(next) + "."
                : "Marked as printed.");
        }

        public async Task<ScheduleChange> UndoMarkPrintedAsync()
        {
            ScheduleChangeResult result = await _client.MarkPrintedAsync(true);
            return Finish(result, "The mark as printed was removed.");
        }

        private async Task<ScheduleChange> PostponeUntilAsync(DateTime untilUtc, string messageFormat)
        {
            DateTime now = DateTime.UtcNow;
            if (untilUtc <= now || untilUtc - now > MaxPostponement)
            {
                return new ScheduleChange(false, "A print can be postponed by up to two years.");
            }
            ScheduleChangeResult result = await _client.PostponeAsync(untilUtc);
            return Finish(result, string.Format(messageFormat, DisplayText.FormatTime(untilUtc)));
        }

        private ScheduleChange Finish(ScheduleChangeResult result, string success)
        {
            switch (result)
            {
                case ScheduleChangeResult.Changed:
                    _monitor.RefreshNow();
                    return new ScheduleChange(true, success);
                case ScheduleChangeResult.NotConnected:
                    return NotConnected();
                case ScheduleChangeResult.InvalidValue:
                    return new ScheduleChange(false, "The service did not accept that time.");
                case ScheduleChangeResult.StorageError:
                    return new ScheduleChange(false, "The service could not save the change.");
                default:
                    return new ScheduleChange(false, "The service could not change the schedule.");
            }
        }

        // The next scheduled print, or now if it is due; null if there is none.
        private static DateTime? NextPrint(ServiceStatus status)
        {
            DateTime? next = status?.NextScheduledPrintUtc;
            DateTime now = DateTime.UtcNow;
            return next.HasValue && next.Value < now ? now : next;
        }

        // Why the next print cannot be moved; null if it can.
        private static ScheduleChange CannotMoveNextPrint(ServiceStatus status)
        {
            if (status == null)
            {
                return NotConnected();
            }
            if (!status.NextScheduledPrintUtc.HasValue)
            {
                return new ScheduleChange(false, "Scheduled printing is turned off or no printer is selected.");
            }
            if (status.PrintState == PrintState.Printing && status.IsScheduledPrintDue(DateTime.UtcNow))
            {
                return new ScheduleChange(false, "The scheduled page is being printed already.");
            }
            return null;
        }

        private static ScheduleChange NotConnected()
        {
            return new ScheduleChange(false, "Cannot connect to the Printhead Maintainer service.");
        }
    }
}
