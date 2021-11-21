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
using PrintheadMaintainerUI.Mediators;
using System;
using System.IO;
using System.Text;
using System.Windows.Input;
using static System.Environment;

namespace PrintheadMaintainerUI.ViewModels
{
    public class LogsViewModel : ViewModelBase, IPageViewModel
    {

        private ICommand _switchToHomeView;
        private string _strLogFileText;

        public ICommand SwitchToHomeView
        {
            get
            {
                return _switchToHomeView ?? (_switchToHomeView = new RelayCommand(x =>
                {
                    Mediator.Notify("SwitchToHome", "");
                }));
            }
        }

        public ICommand CmdRefreshLog
        {

            get
            {
                return new RelayCommand(x =>
                {
                    VoidLoadLogFile();
                });
            }

        }

        public string StrLogFileText {
            get {
                return _strLogFileText;
            }
            set
            {
                _strLogFileText = value;
                OnPropertyChanged(nameof(StrLogFileText));
            }
        }

        public void VoidRefreshView()
        {

            VoidLoadLogFile();

        }


        public LogsViewModel() {

            VoidLoadLogFile();

            


        }

        private void VoidLoadLogFile() {

            try
            {
                StrLogFileText = File.ReadAllText(GetFolderPath(SpecialFolder.CommonApplicationData) + "\\Printhead Maintainer\\Printhead Maintainer.log", Encoding.UTF8);

            }
            catch (Exception)
            {
                StrLogFileText = "Loading logs failed...please try again later.";
            }
            
        }
    }
}
