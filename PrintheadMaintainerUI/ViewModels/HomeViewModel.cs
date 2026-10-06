/*
* 
* Copyright (C) 2021  YAN-LIN, CHEN
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
using PrintheadMaintainerUI.Enums;
using PrintheadMaintainerUI.Interfaces;
using PrintheadMaintainerUI.Models;
using PrintheadMaintainerUI.Presentation;
using PrintheadMaintainerUI.Status;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PrintheadMaintainerUI.ViewModels
{
    public sealed class HomeViewModel : ViewModelBase
    {
        private readonly StatusMonitor _monitor;
        private readonly ScheduleActions _actions;
        private bool _sending;
        private ServiceState _state = ServiceState.Unknown;
        private string _title = string.Empty;
        private string _scheduledPrinting = string.Empty;
        private ValueState _scheduledPrintingState;
        private string _problem = string.Empty;
        private ValueState _problemState;
        private string _lastPrint = string.Empty;
        private ValueState _lastPrintState;
        private string _nextPrint = string.Empty;
        private ValueState _nextPrintState;
        private bool _isPrintNowVisible;
        private bool _isSettingsVisible;
        private bool _isResumeScheduleVisible;
        private bool _isUndoMarkVisible;
        private string _message = string.Empty;

        public HomeViewModel(INavigator navigator, StatusMonitor monitor, ScheduleActions actions)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            ShowPrintNowCommand = new RelayCommand(navigator.ShowPrintNow);
            ShowSettingsCommand = new RelayCommand(navigator.ShowSettings);
            ShowLogsCommand = new RelayCommand(navigator.ShowLogs);
            ShowAboutCommand = new RelayCommand(navigator.ShowAbout);
            PrintNowCommand = new RelayCommand(navigator.PrintNow, () => navigator.CanPrintNow);
            ShowPostponeCommand = new RelayCommand(navigator.ShowPostpone, () => _actions.HasNextPrint);
            MarkPrintedCommand = new RelayCommand(() => Run(_actions.MarkPrintedAsync), () => !_sending && _actions.HasNextPrint);
            ResumeScheduleCommand = new RelayCommand(() => Run(_actions.ResumeScheduleAsync), () => !_sending);
            UndoMarkPrintedCommand = new RelayCommand(() => Run(_actions.UndoMarkPrintedAsync), () => !_sending);

            monitor.Updated += (sender, e) => Update();
            Update();
        }

        public ICommand ShowPrintNowCommand { get; }

        public ICommand ShowSettingsCommand { get; }

        public ICommand ShowLogsCommand { get; }

        public ICommand ShowAboutCommand { get; }

        public ICommand PrintNowCommand { get; }

        public ICommand ShowPostponeCommand { get; }

        public ICommand MarkPrintedCommand { get; }

        public ICommand ResumeScheduleCommand { get; }

        public ICommand UndoMarkPrintedCommand { get; }

        public ServiceState State
        {
            get => _state;
            private set => SetProperty(ref _state, value);
        }

        public string Title
        {
            get => _title;
            private set => SetProperty(ref _title, value);
        }

        public string ScheduledPrinting
        {
            get => _scheduledPrinting;
            private set => SetProperty(ref _scheduledPrinting, value);
        }

        public ValueState ScheduledPrintingState
        {
            get => _scheduledPrintingState;
            private set => SetProperty(ref _scheduledPrintingState, value);
        }

        public string Problem
        {
            get => _problem;
            private set => SetProperty(ref _problem, value);
        }

        public ValueState ProblemState
        {
            get => _problemState;
            private set => SetProperty(ref _problemState, value);
        }

        public string LastPrint
        {
            get => _lastPrint;
            private set => SetProperty(ref _lastPrint, value);
        }

        public ValueState LastPrintState
        {
            get => _lastPrintState;
            private set => SetProperty(ref _lastPrintState, value);
        }

        public string NextPrint
        {
            get => _nextPrint;
            private set => SetProperty(ref _nextPrint, value);
        }

        public ValueState NextPrintState
        {
            get => _nextPrintState;
            private set => SetProperty(ref _nextPrintState, value);
        }

        /// <summary>
        /// While scheduled printing fails, so that a printer that has been fixed prints right away
        /// instead of when the service tries again.
        /// </summary>
        public bool IsPrintNowVisible
        {
            get => _isPrintNowVisible;
            private set => SetProperty(ref _isPrintNowVisible, value);
        }

        /// <summary>Instead of Print Now when the image could not be printed, which printing again does not fix.</summary>
        public bool IsSettingsVisible
        {
            get => _isSettingsVisible;
            private set => SetProperty(ref _isSettingsVisible, value);
        }

        /// <summary>While the next print is postponed.</summary>
        public bool IsResumeScheduleVisible
        {
            get => _isResumeScheduleVisible;
            private set => SetProperty(ref _isResumeScheduleVisible, value);
        }

        /// <summary>
        /// While the schedule counts from a mark as printed, but not while scheduled printing fails:
        /// it failed after the mark, so removing the mark changes nothing.
        /// </summary>
        public bool IsUndoMarkVisible
        {
            get => _isUndoMarkVisible;
            private set => SetProperty(ref _isUndoMarkVisible, value);
        }

        public string Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        public void Load()
        {
            Message = string.Empty;
        }

        // The page shows the changed schedule right away, so only a failure needs a message.
        private async void Run(Func<Task<ScheduleChange>> change)
        {
            if (_sending)
            {
                return;
            }

            _sending = true;
            Message = string.Empty;
            CommandManager.InvalidateRequerySuggested();

            ScheduleChange result = await change();
            _sending = false;
            Message = result.Succeeded ? string.Empty : result.Message;
            CommandManager.InvalidateRequerySuggested();
        }

        private void Update()
        {
            ServiceStatus status = _monitor.Status;
            DateTime now = DateTime.UtcNow;
            StatusSummary summary = StatusSummary.Create(_monitor.Connection, status, now);
            State = summary.State;
            Title = summary.Title;
            ScheduledPrinting = summary.ScheduledPrinting;
            ScheduledPrintingState = summary.ScheduledPrintingState;
            Problem = summary.Problem;
            ProblemState = summary.ProblemState;
            LastPrint = summary.LastPrint;
            LastPrintState = summary.LastPrintState;
            NextPrint = summary.NextScheduledPrint;
            NextPrintState = summary.NextScheduledPrintState;
            bool failing = summary.State == ServiceState.Warning;
            bool imageFailed = failing && status.LastScheduledFailure?.Reason == FailureReason.ImageUnavailable;
            IsPrintNowVisible = failing && !imageFailed;
            IsSettingsVisible = imageFailed;
            IsResumeScheduleVisible = status != null && status.IsPostponed(now);
            IsUndoMarkVisible = status != null && status.IsMarkedAsPrinted && !failing;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
