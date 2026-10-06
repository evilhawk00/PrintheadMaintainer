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
using PrintheadMaintainerUI.Interfaces;
using PrintheadMaintainerUI.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PrintheadMaintainerUI.ViewModels
{
    public sealed class LogsViewModel : ViewModelBase
    {
        private int _loadVersion;
        private string _logText = string.Empty;

        public LogsViewModel(INavigator navigator)
        {
            BackCommand = new RelayCommand(navigator.ShowHome);
            RefreshCommand = new RelayCommand(Load);
        }

        public ICommand BackCommand { get; }

        public ICommand RefreshCommand { get; }

        /// <summary>The service log, newest entry first.</summary>
        public string LogText
        {
            get => _logText;
            private set => SetProperty(ref _logText, value);
        }

        public async void Load()
        {
            int version = ++_loadVersion;
            string text;
            try
            {
                IReadOnlyList<string> entries = await Task.Run(() => ServiceLogReader.ReadNewestFirst());
                text = entries.Count > 0 ? string.Join(Environment.NewLine, entries) : "Nothing has been logged yet.";
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                text = "The log could not be read. " + e.Message;
            }
            if (version == _loadVersion)
            {
                LogText = text;
            }
        }
    }
}
