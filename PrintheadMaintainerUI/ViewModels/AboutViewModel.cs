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
using System.Diagnostics;
using System.Windows.Input;

namespace PrintheadMaintainerUI.ViewModels
{
    public class AboutViewModel : ViewModelBase, IPageViewModel
    {
        private ICommand _switchToHomeView;

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

        public ICommand CmdOpenGithubProjectPage
        {
            get
            {
                return new RelayCommand(x =>
                {
                    VoidOpenGithubProjectPage();
                });
            }
        }

        private void VoidOpenGithubProjectPage()
        {
            _ = Process.Start("https://github.com/evilhawk00/PrintheadMaintainer");
        }

    }
}
