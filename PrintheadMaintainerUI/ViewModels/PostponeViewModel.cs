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
using PrintheadMaintainerUI.Commands;
using PrintheadMaintainerUI.Interfaces;
using PrintheadMaintainerUI.Presentation;
using PrintheadMaintainerUI.Status;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PrintheadMaintainerUI.ViewModels
{
    /// <summary>Postpones the next scheduled print by a few days, past the next one, or to a chosen day.</summary>
    public sealed class PostponeViewModel : ViewModelBase
    {
        private readonly ScheduleActions _actions;
        private readonly StatusMonitor _monitor;
        private bool _sending;
        private bool _hasNextPrint;
        private string _nextPrint = string.Empty;
        private DateTime _firstDay = DateTime.Today.AddDays(1);
        private DateTime _lastDay;
        private DateTime? _selectedDay;
        private string _message = string.Empty;

        public PostponeViewModel(INavigator navigator, ScheduleActions actions, StatusMonitor monitor)
        {
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _lastDay = actions.LastPostponementDay;
            BackCommand = new RelayCommand(navigator.ShowHome);
            PostponeOneDayCommand = new RelayCommand(() => Run(() => _actions.PostponeAsync(TimeSpan.FromDays(1))), CanPostpone);
            PostponeThreeDaysCommand = new RelayCommand(() => Run(() => _actions.PostponeAsync(TimeSpan.FromDays(3))), CanPostpone);
            SkipNextPrintCommand = new RelayCommand(() => Run(_actions.SkipNextPrintAsync), CanPostpone);
            PostponeToDayCommand = new RelayCommand(PostponeToDay, () => CanPostpone() && _selectedDay.HasValue);
            monitor.Updated += (sender, e) => Update();
        }

        public ICommand BackCommand { get; }

        public ICommand PostponeOneDayCommand { get; }

        public ICommand PostponeThreeDaysCommand { get; }

        public ICommand SkipNextPrintCommand { get; }

        public ICommand PostponeToDayCommand { get; }

        /// <summary>Scheduled printing is on and a printer is selected, so there is a print to postpone.</summary>
        public bool HasNextPrint
        {
            get => _hasNextPrint;
            private set => SetProperty(ref _hasNextPrint, value);
        }

        public string NextPrint
        {
            get => _nextPrint;
            private set => SetProperty(ref _nextPrint, value);
        }

        /// <summary>The first day the calendar offers: the day after the next print.</summary>
        public DateTime FirstDay
        {
            get => _firstDay;
            private set => SetProperty(ref _firstDay, value);
        }

        public DateTime LastDay
        {
            get => _lastDay;
            private set => SetProperty(ref _lastDay, value);
        }

        public DateTime? SelectedDay
        {
            get => _selectedDay;
            set
            {
                if (SetProperty(ref _selectedDay, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        public void Load()
        {
            Message = string.Empty;
            SelectedDay = null;
            Update();
        }

        private bool CanPostpone()
        {
            return !_sending && _actions.HasNextPrint;
        }

        private void PostponeToDay()
        {
            DateTime? day = _selectedDay;
            if (day.HasValue)
            {
                Run(() => _actions.PostponeToDayAsync(day.Value));
            }
        }

        private async void Run(Func<Task<ScheduleChange>> postpone)
        {
            if (!CanPostpone())
            {
                return;
            }

            _sending = true;
            Message = string.Empty;
            CommandManager.InvalidateRequerySuggested();

            ScheduleChange change = await postpone();
            _sending = false;
            Message = change.Message;
            if (change.Succeeded)
            {
                SelectedDay = null;
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void Update()
        {
            DateTime? next = _actions.NextPrintUtc;
            HasNextPrint = next.HasValue;
            if (next.HasValue)
            {
                NextPrint = StatusSummary.Create(_monitor.Connection, _monitor.Status, DateTime.UtcNow).NextScheduledPrint;
                DateTime firstDay = next.Value.ToLocalTime().Date.AddDays(1);
                DateTime lastDay = _actions.LastPostponementDay;
                if (lastDay < firstDay)
                {
                    lastDay = firstDay;
                }

                // The calendar would widen its range to keep the selected day, so clear the day first.
                if (_selectedDay.HasValue && (_selectedDay.Value < firstDay || _selectedDay.Value > lastDay))
                {
                    SelectedDay = null;
                }
                FirstDay = firstDay;
                LastDay = lastDay;
            }
            else
            {
                NextPrint = _monitor.Status == null ? "Not connected to the service" : "No scheduled print";
            }
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
