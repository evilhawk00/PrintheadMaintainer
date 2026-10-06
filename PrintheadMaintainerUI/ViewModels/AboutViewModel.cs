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
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;

namespace PrintheadMaintainerUI.ViewModels
{
    public sealed class AboutViewModel : ViewModelBase
    {
        private const string ProjectPage = "https://github.com/evilhawk00/PrintheadMaintainer";

        public AboutViewModel(INavigator navigator)
        {
            BackCommand = new RelayCommand(navigator.ShowHome);
            OpenProjectPageCommand = new RelayCommand(OpenProjectPage);
        }

        public ICommand BackCommand { get; }

        public ICommand OpenProjectPageCommand { get; }

        public string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private static void OpenProjectPage()
        {
            try
            {
                using (Process.Start(ProjectPage))
                {
                }
            }
            catch (Exception e) when (e is Win32Exception || e is InvalidOperationException)
            {
                // No browser is set up; the address is shown on the page.
            }
        }
    }
}
