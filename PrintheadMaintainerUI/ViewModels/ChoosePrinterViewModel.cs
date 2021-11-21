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
using PrintheadMaintainerUI.Singletons;
using System.Collections.ObjectModel;
using System.Management;
using System.Windows.Input;

namespace PrintheadMaintainerUI.ViewModels
{
    public class ChoosePrinterViewModel : ViewModelBase, IPageViewModel
    {
        private ICommand _switchToSettingsView;

        private string _strSelectedPrinterName;

        public ObservableCollection<string> OcStrPrinterList { get; set; }

        public string StrSelectedPrinterName {

            get {
                return _strSelectedPrinterName;
            }
            set {
                _strSelectedPrinterName = value;
            }
        
        }

        public ICommand SwitchToSettingsView
        {
            get
            {
                return _switchToSettingsView ?? (_switchToSettingsView = new RelayCommand(x =>
                {
                    Mediator.Notify("BackToSettings", "");
                }));
            }
        }


        public ICommand CmdSelectPrinter
        {

            get
            {
                //send value to server
                return new RelayCommand(x =>
                {
                    VoidPassPrinterNameToSingleton();
                    Mediator.Notify("BackToSettings", "");
                });
            }

        }


        public ICommand CmdRefreshPrinter
        {

            get
            {
                //send value to server
                return new RelayCommand(x =>
                {
                    VoidUpdatePrinterList();
                    
                });
            }

        }

        public void VoidRefreshView() {

            VoidUpdatePrinterList();

        }

        public ChoosePrinterViewModel() {


            VoidUpdatePrinterList();

        }

        private void VoidUpdatePrinterList() {

            OcStrPrinterList = new ObservableCollection<string>();

            ManagementScope objScope = new ManagementScope(ManagementPath.DefaultPath); 
            objScope.Connect();

            SelectQuery selectQuery = new SelectQuery();
            selectQuery.QueryString = "Select * from win32_Printer";
            ManagementObjectSearcher MOS = new ManagementObjectSearcher(objScope, selectQuery);
            ManagementObjectCollection MOC = MOS.Get();
            foreach (ManagementObject mo in MOC)
            {
                OcStrPrinterList.Add(mo["Name"].ToString());
            }
            OnPropertyChanged(nameof(OcStrPrinterList));
        }

        private void VoidPassPrinterNameToSingleton() {

            if (_strSelectedPrinterName != null && !_strSelectedPrinterName.Equals("")) {

                PrinterNameSingletons pnsInstance = PrinterNameSingletons.Instance;
                pnsInstance.strPrinterName = _strSelectedPrinterName;

            }

        }


    }
}
