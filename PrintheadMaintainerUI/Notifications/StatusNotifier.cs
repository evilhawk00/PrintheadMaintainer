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
    /// Shows Windows notifications for the service's status, and for changes to the schedule made
    /// without the window. The computer may be left alone right after it starts, so a failed
    /// scheduled print must still be noticed when the user comes back:
    /// - A failed scheduled print is shown as a reminder that stays on screen until the user closes
    ///   it, and again when the service reports a different problem. While it is not resolved and
    ///   the print is due (not postponed), it is shown again every 15 minutes, when the UI starts
    ///   and when the user returns.
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
        private const string ScheduleChangeTag = "ScheduleChange";

        // What a click on a notification or on one of its buttons asks for.
        private const string ActionArgument = "action";
        private const string OpenAction = "open";
        private const string PrintNowAction = "printNow";
        private const string SettingsAction = "settings";
        private const string SkipPrintAction = "skipPrint";
        private const string PostponeOneDayAction = "postponeOneDay";

        private static readonly TimeSpan ReminderInterval = TimeSpan.FromMinutes(15);

        // As long as the service counts down before a scheduled print (Printing/PrintWorker.cpp);
        // the buttons of the notification no longer apply after it.
        private static readonly TimeSpan CountdownLength = TimeSpan.FromMinutes(2);

        // How a change went is of no use later, so it does not stay in the notification center.
        private static readonly TimeSpan ScheduleChangeLifetime = TimeSpan.FromMinutes(5);

        private readonly StatusMonitor _monitor;
        private readonly ScheduleActions _actions;
        private readonly UserReturnDetector _userReturn = new UserReturnDetector();
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private readonly bool _startedByNotification;
        private ServiceStatus _previous;
        private bool _showUnresolvedFailure = true;
        private DateTime _lastReminderUtc = DateTime.MinValue;

        // A click on a notification made before the service's status is known, such as the click
        // that started the program; it is carried out once the status is known.
        private string _pendingAction;

        // Notifications from an earlier run may still be in the notification center.
        private bool _scheduledFailureMayBeShown = true;
        private bool _manualFailureMayBeShown = true;
        private bool _printStartingMayBeShown = true;

        public StatusNotifier(StatusMonitor monitor, ScheduleActions actions)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));

            // The user has just seen the notification that started the program.
            _startedByNotification = WasStartedByNotification();
            if (_startedByNotification)
            {
                _showUnresolvedFailure = false;
                _lastReminderUtc = DateTime.UtcNow;
            }

            _monitor.Updated += OnStatusUpdated;
            _userReturn.UserReturned += OnUserReturned;
            TryNotificationCall(() => ToastNotificationManagerCompat.OnActivated += OnToastActivated);
        }

        /// <summary>The user clicked a notification; raised on the UI thread.</summary>
        public event EventHandler OpenRequested;

        /// <summary>The user clicked Print Now on a notification; raised on the UI thread.</summary>
        public event EventHandler PrintNowRequested;

        /// <summary>The user clicked Settings on a notification; raised on the UI thread.</summary>
        public event EventHandler SettingsRequested;

        /// <summary>The program was started by a click on one of its notifications, which decides what to show.</summary>
        public static bool WasStartedByNotification()
        {
            bool started = false;
            TryNotificationCall(() => started = ToastNotificationManagerCompat.WasCurrentProcessToastActivated());
            return started;
        }

        /// <summary>Tells how a change to the schedule went that was made without the window.</summary>
        public void ShowScheduleChange(ScheduleChange change)
        {
            Show(new ToastContentBuilder()
                    .AddText(change.Succeeded ? "Schedule changed" : "The schedule was not changed")
                    .AddText(change.Message),
                ScheduleChangeTag, DateTimeOffset.Now + ScheduleChangeLifetime);
        }

        public void Dispose()
        {
            _monitor.Updated -= OnStatusUpdated;
            _userReturn.Dispose();
            TryNotificationCall(() => ToastNotificationManagerCompat.OnActivated -= OnToastActivated);
        }

        private void OnStatusUpdated(object sender, EventArgs e)
        {
            ServiceStatus status = _monitor.Status;
            bool clickHandled = false;
            if (_pendingAction != null && _monitor.Connection != ServiceConnection.Connecting)
            {
                string action = _pendingAction;
                _pendingAction = null;
                RunAction(action);
                clickHandled = true;
            }
            if (status == null)
            {
                return; // nothing is known while the service cannot be reached; leave everything as it is
            }

            ServiceStatus previous = _previous;
            _previous = status;

            // What a clicked notification showed is not shown again: the status its click acts on,
            // and the first status after the click started the program, which may come before it.
            if (clickHandled || (previous == null && _startedByNotification))
            {
                return;
            }
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
            // Remind only while the print is due: not once scheduled printing is turned off, since
            // nothing will be retried, nor while it is postponed. When a postponement ends, a failure
            // from before it waits until the print has been tried again.
            DateTime now = DateTime.UtcNow;
            bool beforePostponement = status.IsNextScheduledPrintPostponed &&
                status.LastScheduledFailure?.TimeUtc < status.NextScheduledPrintUtc;
            bool unresolved = status.HasUnresolvedScheduledFailure && status.IsScheduledPrintDue(now) && !beforePostponement;
            _userReturn.IsEnabled = unresolved;
            bool reminderDue = now - _lastReminderUtc >= ReminderInterval || now < _lastReminderUtc; // or the clock was turned back
            bool newFailure = previous != null && IsNewer(status.LastScheduledFailure, previous.LastScheduledFailure);
            if (unresolved && (_showUnresolvedFailure || newFailure || reminderDue))
            {
                PrintFailure failure = status.LastScheduledFailure;

                // Printing again does not help when the image could not be printed; another one has to be chosen.
                ToastButton fix = failure.Reason == FailureReason.ImageUnavailable
                    ? Button("Settings", SettingsAction)
                    : Button("Print Now", PrintNowAction);
                Show(new ToastContentBuilder()
                        .SetToastScenario(ToastScenario.Reminder)
                        .AddText("Scheduled printing failed")
                        .AddText(DisplayText.Describe(failure.Reason) + ".")
                        .AddText("Since " + DisplayText.FormatTime(failure.TimeUtc) + ". It prints automatically once this is fixed.")
                        .AddButton(fix)
                        .AddButton(Button("Postpone 1 Day", PostponeOneDayAction).SetBackgroundActivation())
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
                        .AddText("Printhead Maintainer will print a maintenance page in about two minutes.")
                        .AddButton(Button("Skip This Print", SkipPrintAction).SetBackgroundActivation())
                        .AddButton(Button("Postpone 1 Day", PostponeOneDayAction).SetBackgroundActivation()),
                    PrintStartingTag, DateTimeOffset.Now + CountdownLength);
                _printStartingMayBeShown = true;
            }
            else if (status.PrintState != PrintState.Countdown && _printStartingMayBeShown)
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
            string action = ToastArguments.Parse(e.Argument).TryGetValue(ActionArgument, out string value) ? value : OpenAction;
            _dispatcher.BeginInvoke(new Action(() => OnActionRequested(action)));
        }

        private void OnActionRequested(string action)
        {
            // The user has just seen the notification, so no reminder is due right away.
            _showUnresolvedFailure = false;
            _lastReminderUtc = DateTime.UtcNow;

            // A click that started the program waits until the service's status is known.
            if (action != OpenAction && _monitor.Connection == ServiceConnection.Connecting)
            {
                _pendingAction = action;
                return;
            }
            RunAction(action);
        }

        private async void RunAction(string action)
        {
            switch (action)
            {
                case PrintNowAction:
                    PrintNowRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case SettingsAction:
                    SettingsRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case SkipPrintAction:
                    ShowScheduleChange(await _actions.SkipNextPrintAsync());
                    break;
                case PostponeOneDayAction:
                    ShowScheduleChange(await _actions.PostponeAsync(TimeSpan.FromDays(1)));
                    break;
                default:
                    OpenRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        private static ToastButton Button(string content, string action)
        {
            return new ToastButton().SetContent(content).AddArgument(ActionArgument, action);
        }

        private static void Show(ToastContentBuilder content, string tag, DateTimeOffset? expiration = null)
        {
            content.AddArgument(ActionArgument, OpenAction); // a button with an action of its own keeps it
            TryNotificationCall(() => content.Show(toast =>
            {
                toast.Tag = tag;
                toast.Group = Group;
                if (expiration.HasValue)
                {
                    toast.ExpirationTime = expiration;
                }
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
