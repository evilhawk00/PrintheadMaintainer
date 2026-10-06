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
using PrintheadMaintainerUI.Status;
using PrintheadMaintainerUI.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Forms = System.Windows.Forms;

namespace PrintheadMaintainerUI.Notifications
{
    /// <summary>
    /// The icon in the notification area. It shows the state, opens the window when double-clicked,
    /// and its menu offers the main actions. Changes to the schedule made from the menu do not open
    /// the window; a notification tells how they went. Create and use it on the UI thread.
    /// </summary>
    public sealed class TrayIcon : IDisposable
    {
        // Windows does not accept longer tooltips for notification area icons.
        private const int MaxTextLength = 63;

        private readonly MainWindow _window;
        private readonly MainViewModel _viewModel;
        private readonly ScheduleActions _actions;
        private readonly StatusNotifier _notifier;
        private readonly Forms.NotifyIcon _icon;
        private readonly Forms.MenuItem _printNow;
        private readonly Forms.MenuItem _markPrinted;
        private readonly Forms.MenuItem _postpone;
        private readonly Forms.MenuItem _resumeSchedule;

        public TrayIcon(MainWindow window, MainViewModel viewModel, ScheduleActions actions, StatusNotifier notifier)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));

            _printNow = new Forms.MenuItem("&Print Now", (sender, e) => _window.PrintNow());
            _markPrinted = new Forms.MenuItem("I &Already Printed", (sender, e) => Change(_actions.MarkPrintedAsync));
            _postpone = new Forms.MenuItem("P&ostpone", new[]
            {
                new Forms.MenuItem("&1 Day Later", (sender, e) => Change(() => _actions.PostponeAsync(TimeSpan.FromDays(1)))),
                new Forms.MenuItem("&3 Days Later", (sender, e) => Change(() => _actions.PostponeAsync(TimeSpan.FromDays(3)))),
                new Forms.MenuItem("&Skip the Next Print", (sender, e) => Change(_actions.SkipNextPrintAsync)),
                new Forms.MenuItem("&Choose a Day...", (sender, e) => _window.ShowPostpone()),
            });
            _resumeSchedule = new Forms.MenuItem("&Resume Schedule", (sender, e) => Change(_actions.ResumeScheduleAsync));
            var menu = new Forms.ContextMenu(new[]
            {
                new Forms.MenuItem("S&how Window", (sender, e) => _window.ShowFromTray()),
                new Forms.MenuItem("-"),
                _printNow,
                _markPrinted,
                _postpone,
                _resumeSchedule,
                new Forms.MenuItem("-"),
                new Forms.MenuItem("E&xit", (sender, e) => _window.Exit()),
            });
            menu.Popup += (sender, e) => UpdateMenu();

            _icon = new Forms.NotifyIcon { ContextMenu = menu, Visible = true };
            _icon.DoubleClick += (sender, e) => _window.ShowFromTray();
            UpdateIcon();
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public void Dispose()
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _icon.Visible = false;
            _icon.Dispose();
        }

        private void UpdateMenu()
        {
            _printNow.Enabled = _viewModel.CanPrintNow;
            _markPrinted.Enabled = _actions.HasNextPrint;
            _postpone.Enabled = _actions.HasNextPrint;
            _resumeSchedule.Visible = _actions.IsPostponed;
        }

        // async void: the menu does not wait; the outcome is shown in a notification.
        private async void Change(Func<Task<ScheduleChange>> change)
        {
            _notifier.ShowScheduleChange(await change());
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.State) || e.PropertyName == nameof(MainViewModel.StatusTitle))
            {
                UpdateIcon();
            }
        }

        private void UpdateIcon()
        {
            System.Drawing.Icon previous = _icon.Icon;
            switch (_viewModel.State)
            {
                case ServiceState.OK:
                    _icon.Icon = Properties.Resources.Icon_Green;
                    break;
                case ServiceState.Warning:
                    _icon.Icon = Properties.Resources.Icon_Orange;
                    break;
                case ServiceState.Error:
                    _icon.Icon = Properties.Resources.Icon_Red;
                    break;
                default:
                    _icon.Icon = Properties.Resources.Icon_Blue;
                    break;
            }
            previous?.Dispose(); // every read of a resource creates a new icon

            string text = "Printhead Maintainer - " + _viewModel.StatusTitle;
            _icon.Text = text.Length > MaxTextLength ? text.Substring(0, MaxTextLength) : text;
        }
    }
}
