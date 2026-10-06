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
using Microsoft.Toolkit.Uwp.Notifications;
using PrintheadMaintainerUI.Enums;
using PrintheadMaintainerUI.Models;
using PrintheadMaintainerUI.Presentation;
using PrintheadMaintainerUI.Status;
using System;
using System.Windows.Threading;

namespace PrintheadMaintainerUI.Notifications
{
    /// <summary>
    /// Shows Windows notifications for the service's status. The computer may be left alone right
    /// after it starts, so a failed scheduled print must still be noticed when the user comes back:
    /// - A failed scheduled print is shown as a reminder that stays on screen until the user closes
    ///   it, and again when the service reports a different problem. While it is not resolved, it
    ///   is shown again every 15 minutes, when the UI starts and when the user returns.
    /// - Each kind of notification replaces the previous one of its kind, so the notification
    ///   center holds only the latest one, and it is removed once a later print succeeds.
    /// Create it on the UI thread.
    /// </summary>
    public sealed class StatusNotifier : IDisposable
    {
        private const string Group = "Status";
        private const string ScheduledFailureTag = "ScheduledFailure";
        private const string ManualFailureTag = "ManualFailure";
        private const string PrintStartingTag = "PrintStarting";

        private static readonly TimeSpan ReminderInterval = TimeSpan.FromMinutes(15);

        private readonly StatusMonitor _monitor;
        private readonly UserReturnDetector _userReturn = new UserReturnDetector();
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private ServiceStatus _previous;
        private bool _showUnresolvedFailure = true;
        private DateTime _lastReminderUtc = DateTime.MinValue;

        // Notifications from an earlier run may still be in the notification center.
        private bool _scheduledFailureMayBeShown = true;
        private bool _manualFailureMayBeShown = true;
        private bool _printStartingMayBeShown = true;

        public StatusNotifier(StatusMonitor monitor)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _monitor.Updated += OnStatusUpdated;
            _userReturn.UserReturned += OnUserReturned;
            TryNotificationCall(() => ToastNotificationManagerCompat.OnActivated += OnToastActivated);
        }

        /// <summary>The user clicked a notification; raised on the UI thread.</summary>
        public event EventHandler OpenRequested;

        public void Dispose()
        {
            _monitor.Updated -= OnStatusUpdated;
            _userReturn.Dispose();
            TryNotificationCall(() => ToastNotificationManagerCompat.OnActivated -= OnToastActivated);
        }

        private void OnStatusUpdated(object sender, EventArgs e)
        {
            ServiceStatus status = _monitor.Status;
            if (status == null)
            {
                return; // nothing is known while the service cannot be reached; leave everything as it is
            }

            ServiceStatus previous = _previous;
            _previous = status;
            UpdateScheduledFailure(status, previous);
            UpdateManualFailure(status, previous);
            UpdatePrintStarting(status, previous);
        }

        private void OnUserReturned(object sender, EventArgs e)
        {
            _showUnresolvedFailure = true;
            _monitor.RefreshNow();
        }

        private void UpdateScheduledFailure(ServiceStatus status, ServiceStatus previous)
        {
            // Once scheduled printing is turned off nothing will be retried, so stop reminding.
            bool unresolved = status.HasUnresolvedScheduledFailure && status.Enabled && status.IsPrinterSelected;
            _userReturn.IsEnabled = unresolved;
            DateTime now = DateTime.UtcNow;
            bool reminderDue = now - _lastReminderUtc >= ReminderInterval || now < _lastReminderUtc; // or the clock was turned back
            if (unresolved && (_showUnresolvedFailure || IsNewer(status.LastScheduledFailure, previous?.LastScheduledFailure) || reminderDue))
            {
                PrintFailure failure = status.LastScheduledFailure;
                Show(new ToastContentBuilder()
                        .SetToastScenario(ToastScenario.Reminder)
                        .AddText("Scheduled printing failed")
                        .AddText(DisplayText.Describe(failure.Reason) + ".")
                        .AddText("Since " + DisplayText.FormatTime(failure.TimeUtc) + ". It prints automatically once this is fixed.")
                        .AddButton(new ToastButton().SetContent("Open").AddArgument("action", "open"))
                        .AddButton(new ToastButtonDismiss()),
                    ScheduledFailureTag);
                _scheduledFailureMayBeShown = true;
                _lastReminderUtc = now;
            }
            else if (!unresolved && _scheduledFailureMayBeShown)
            {
                Remove(ScheduledFailureTag);
                _scheduledFailureMayBeShown = false;
            }
            _showUnresolvedFailure = false;
        }

        private void UpdateManualFailure(ServiceStatus status, ServiceStatus previous)
        {
            // The user is waiting for a manual print, so only failures seen while the UI runs are shown.
            bool unresolved = status.HasUnresolvedManualFailure;
            if (unresolved && previous != null && IsNewer(status.LastManualFailure, previous.LastManualFailure))
            {
                PrintFailure failure = status.LastManualFailure;
                Show(new ToastContentBuilder()
                        .AddText("Printing failed")
                        .AddText(DisplayText.Describe(failure.Reason) + ".")
                        .AddText("Manual printing at " + DisplayText.FormatTime(failure.TimeUtc) + "."),
                    ManualFailureTag);
                _manualFailureMayBeShown = true;
            }
            else if (!unresolved && _manualFailureMayBeShown)
            {
                Remove(ManualFailureTag);
                _manualFailureMayBeShown = false;
            }
        }

        private void UpdatePrintStarting(ServiceStatus status, ServiceStatus previous)
        {
            if (status.PrintState == PrintState.Countdown && previous?.PrintState != PrintState.Countdown)
            {
                Show(new ToastContentBuilder()
                        .AddText("Preparing to print...")
                        .AddText("Printhead Maintainer will print a maintenance page in about a minute."),
                    PrintStartingTag);
                _printStartingMayBeShown = true;
            }
            else if (status.PrintState == PrintState.Idle && _printStartingMayBeShown)
            {
                Remove(PrintStartingTag);
                _printStartingMayBeShown = false;
            }
        }

        private static bool IsNewer(PrintFailure failure, PrintFailure previous)
        {
            return failure != null && (previous == null || failure.TimeUtc > previous.TimeUtc);
        }

        // Raised on a background thread.
        private void OnToastActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            _dispatcher.BeginInvoke(new Action(() => OpenRequested?.Invoke(this, EventArgs.Empty)));
        }

        private static void Show(ToastContentBuilder content, string tag)
        {
            content.AddArgument("action", "open");
            TryNotificationCall(() => content.Show(toast =>
            {
                toast.Tag = tag;
                toast.Group = Group;
            }));
        }

        private static void Remove(string tag)
        {
            TryNotificationCall(() => ToastNotificationManagerCompat.History.Remove(tag, Group));
        }

        // Notifications are best effort: Windows can refuse them in many ways (turned off, not
        // supported, the notification platform failing). The home page and the tray icon still
        // show the state, so a failure here must never take the UI down.
        private static void TryNotificationCall(Action call)
        {
            try
            {
                call();
            }
            catch (Exception)
            {
            }
        }
    }
}
