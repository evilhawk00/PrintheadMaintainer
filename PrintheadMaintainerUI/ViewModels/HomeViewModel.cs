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
using PrintheadMaintainerUI.Presentation;
using PrintheadMaintainerUI.Status;
using System;
using System.Windows.Input;

namespace PrintheadMaintainerUI.ViewModels
{
    public sealed class HomeViewModel : ViewModelBase
    {
        private readonly StatusMonitor _monitor;
        private ServiceState _state = ServiceState.Transforming;
        private string _title = string.Empty;
        private string _scheduledPrinting = string.Empty;
        private ValueState _scheduledPrintingState;
        private string _problem = string.Empty;
        private ValueState _problemState;
        private string _lastPrint = string.Empty;
        private ValueState _lastPrintState;

        public HomeViewModel(INavigator navigator, StatusMonitor monitor)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            ShowPrintNowCommand = new RelayCommand(navigator.ShowPrintNow);
            ShowSettingsCommand = new RelayCommand(navigator.ShowSettings);
            ShowLogsCommand = new RelayCommand(navigator.ShowLogs);
            ShowAboutCommand = new RelayCommand(navigator.ShowAbout);

            monitor.Updated += (sender, e) => Update();
            Update();
        }

        public ICommand ShowPrintNowCommand { get; }

        public ICommand ShowSettingsCommand { get; }

        public ICommand ShowLogsCommand { get; }

        public ICommand ShowAboutCommand { get; }

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

        private void Update()
        {
            StatusSummary summary = StatusSummary.Create(_monitor.Connection, _monitor.Status, DateTime.UtcNow);
            State = summary.State;
            Title = summary.Title;
            ScheduledPrinting = summary.ScheduledPrinting;
            ScheduledPrintingState = summary.ScheduledPrintingState;
            Problem = summary.Problem;
            ProblemState = summary.ProblemState;
            LastPrint = summary.LastPrint;
            LastPrintState = summary.LastPrintState;
        }
    }
}
