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
using PrintheadMaintainerUI.Interfaces;
using PrintheadMaintainerUI.NamedPipeClient;
using PrintheadMaintainerUI.Presentation;
using PrintheadMaintainerUI.Status;
using System;

namespace PrintheadMaintainerUI.ViewModels
{
    /// <summary>The main window: the page it shows, the footer and the state the icons show.</summary>
    public sealed class MainViewModel : ViewModelBase, INavigator
    {
        private readonly StatusMonitor _monitor;
        private readonly HomeViewModel _home;
        private readonly PrintNowViewModel _printNow;
        private readonly SettingsViewModel _settings;
        private readonly ChoosePrinterViewModel _choosePrinter;
        private readonly LogsViewModel _logs;
        private readonly AboutViewModel _about;
        private ViewModelBase _currentPage;
        private ServiceState _state = ServiceState.Unknown;
        private string _statusTitle = string.Empty;
        private string _footer = string.Empty;

        public MainViewModel(ServiceClient client, StatusMonitor monitor)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _home = new HomeViewModel(this, monitor);
            _printNow = new PrintNowViewModel(this, client, monitor);
            _settings = new SettingsViewModel(this, client, monitor);
            _choosePrinter = new ChoosePrinterViewModel(this);
            _logs = new LogsViewModel(this);
            _about = new AboutViewModel(this);
            _currentPage = _home;

            monitor.Updated += (sender, e) => Update();
            Update();
        }

        public ViewModelBase CurrentPage
        {
            get => _currentPage;
            private set => SetProperty(ref _currentPage, value);
        }

        /// <summary>Shown by the window and tray icons.</summary>
        public ServiceState State
        {
            get => _state;
            private set => SetProperty(ref _state, value);
        }

        /// <summary>A few words about the state, for the tray icon tooltip.</summary>
        public string StatusTitle
        {
            get => _statusTitle;
            private set => SetProperty(ref _statusTitle, value);
        }

        public string Footer
        {
            get => _footer;
            private set => SetProperty(ref _footer, value);
        }

        public void ShowHome()
        {
            Navigate(_home);
        }

        public void ShowPrintNow()
        {
            Navigate(_printNow);
            _printNow.Load();
        }

        public void ShowSettings()
        {
            Navigate(_settings);
            _settings.Load();
        }

        public void ShowLogs()
        {
            Navigate(_logs);
            _logs.Load();
        }

        public void ShowAbout()
        {
            Navigate(_about);
        }

        public void ChoosePrinter(string selectedPrinter)
        {
            Navigate(_choosePrinter);
            _choosePrinter.Load(selectedPrinter);
        }

        public void ReturnToSettings(string chosenPrinter)
        {
            Navigate(_settings);
            if (chosenPrinter != null)
            {
                _settings.SelectPrinter(chosenPrinter);
            }
        }

        private void Navigate(ViewModelBase page)
        {
            if (_currentPage == _printNow && page != _printNow)
            {
                _printNow.Unload();
            }
            CurrentPage = page;
        }

        private void Update()
        {
            StatusSummary summary = StatusSummary.Create(_monitor.Connection, _monitor.Status, DateTime.UtcNow);
            State = summary.State;
            StatusTitle = summary.Title;
            Footer = "Next scheduled printing: " + summary.NextScheduledPrint + " | Printing interval: " + summary.Interval;
        }
    }
}
