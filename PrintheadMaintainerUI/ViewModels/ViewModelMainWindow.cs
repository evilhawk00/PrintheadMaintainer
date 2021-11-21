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
using Microsoft.Win32;
using PrintheadMaintainerUI.Enums;
using PrintheadMaintainerUI.GlobalConstants;
using PrintheadMaintainerUI.Interfaces;
using PrintheadMaintainerUI.Mediators;
using PrintheadMaintainerUI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

namespace PrintheadMaintainerUI.ViewModels
{
    public class ViewModelMainWindow : ViewModelBase
    {
        
        
        private IPageViewModel _currentPageViewModel;
        private List<IPageViewModel> _pageViewModels;

        private ServiceState _serviceStateForConverter;

        private string _strFooter;
        private BitmapFrame _bfWindowIcon;

        public List<IPageViewModel> PageViewModels
        {
            get
            {
                if (_pageViewModels == null)
                {
                    _pageViewModels = new List<IPageViewModel>();
                }

                return _pageViewModels;
            }
        }

        public IPageViewModel CurrentPageViewModel
        {
            get
            {
                return _currentPageViewModel;
            }
            set
            {
                _currentPageViewModel = value;
                OnPropertyChanged("CurrentPageViewModel");
            }
        }

        public ServiceState ServiceStateForConverter
        {
            get
            {
                return _serviceStateForConverter;
            }
            set
            {
                _serviceStateForConverter = value;
                OnPropertyChanged(nameof(ServiceStateForConverter));

            }
        }

        public string StrFooter
        {
            get
            {
                return _strFooter;//format example : Next scheduled printing: 2022/02/24 | Printing Interval: 7 Days
            }
            set
            {
                _strFooter = value;
                OnPropertyChanged(nameof(StrFooter));
            }

        }
        public BitmapFrame BfWindowIcon
        {
            get
            {
                return _bfWindowIcon;
            }
            set
            {
                _bfWindowIcon = value;
                OnPropertyChanged(nameof(BfWindowIcon));
            }
        }

        private void ChangeViewModel(IPageViewModel viewModel)
        {
            if (!PageViewModels.Contains(viewModel))
            {
                PageViewModels.Add(viewModel);
            }

            CurrentPageViewModel = PageViewModels.FirstOrDefault(vm => vm == viewModel);
        }

        public ViewModelMainWindow()
        {
            // Add available pages and set page
            PageViewModels.Add(new HomeViewModel());
            PageViewModels.Add(new PrintNowViewModel());
            PageViewModels.Add(new SettingsViewModel());
            PageViewModels.Add(new LogsViewModel());
            PageViewModels.Add(new AboutViewModel());
            PageViewModels.Add(new ChoosePrinterViewModel());

            CurrentPageViewModel = PageViewModels[0];

            Mediator.Subscribe("SwitchToHome", Callback_SwitchToHomeView);
            Mediator.Subscribe("SwitchToPrintNow", Callback_SwitchToPrintNowView);
            Mediator.Subscribe("SwitchToSettings", Callback_SwitchToSettingsView);
            Mediator.Subscribe("BackToSettings", Callback_SwitchToSettingsView_NoRefresh);
            Mediator.Subscribe("SwitchToLogs", Callback_SwitchToLogsView);
            Mediator.Subscribe("SwitchToAbout", Callback_SwitchToAboutView);
            Mediator.Subscribe("SwitchToChoosePrinter", Callback_SwitchToChoosePrinterView);

            Mediator.Subscribe("UpdateFooter", Callback_UpdateFooterText);

            Mediator.Subscribe("WinOK", Callback_ChangeWindowIcon_OK);
            Mediator.Subscribe("WinWarning", Callback_ChangeWindowIcon_Warning);
            Mediator.Subscribe("WinError", Callback_ChangeWindowIcon_Error);
            Mediator.Subscribe("WinDefault", Callback_ChangeWindowIcon_Default);

            VoidUpdateUI_FooterText();
        }



        private void Callback_SwitchToHomeView(object obj)
        {
            ChangeViewModel(PageViewModels[0]);
            ((HomeViewModel)PageViewModels[0]).VoidRefreshView();
        }

        private void Callback_SwitchToPrintNowView(object obj)
        {
            ChangeViewModel(PageViewModels[1]);
            ((PrintNowViewModel)PageViewModels[1]).VoidRefreshView();
        }

        private void Callback_SwitchToSettingsView(object obj)
        {
            ChangeViewModel(PageViewModels[2]);
            ((SettingsViewModel)PageViewModels[2]).VoidRefreshView();
        }

        private void Callback_SwitchToSettingsView_NoRefresh(object obj)
        {
            ChangeViewModel(PageViewModels[2]);
        }

        private void Callback_SwitchToLogsView(object obj)
        {
            ChangeViewModel(PageViewModels[3]);
            ((LogsViewModel)PageViewModels[3]).VoidRefreshView();
        }

        private void Callback_SwitchToAboutView(object obj)
        {
            ChangeViewModel(PageViewModels[4]);
        }
        private void Callback_SwitchToChoosePrinterView(object obj)
        {
            ChangeViewModel(PageViewModels[5]);
            ((ChoosePrinterViewModel)PageViewModels[5]).VoidRefreshView();
        }

        private void Callback_UpdateFooterText(object obj)
        {
            VoidUpdateUI_FooterText();
        }

        private void Callback_ChangeWindowIcon_OK(object obj)
        {
            ServiceStateForConverter = ServiceState.OK;
        }

        private void Callback_ChangeWindowIcon_Warning(object obj)
        {
            ServiceStateForConverter = ServiceState.Warning;
        }

        private void Callback_ChangeWindowIcon_Error(object obj)
        {
            ServiceStateForConverter = ServiceState.Error;
        }
        private void Callback_ChangeWindowIcon_Default(object obj)
        {
            ServiceStateForConverter = ServiceState.Transforming;
        }

        private void VoidUpdateUI_FooterText() {

            string strStart_PartNextScheduled = "Next scheduled printing: ";
            string strMidPart_PrintingInterval = " | Printing Interval: ";
            string strEndPart_Days = " Days";

            string strValue_interval;
            string strValue_NextScheduledDate;

            RegistryKey rkHandle = RegistryUtils.RkOpenRegistyHandle(false);
            if (rkHandle == null)
            {
                ////at this time may be the first start so the registry is still not created
                strValue_interval = "--";
                strValue_NextScheduledDate = "--";
                StrFooter = strStart_PartNextScheduled + strValue_NextScheduledDate + strMidPart_PrintingInterval + strValue_interval + strEndPart_Days;
                return;
            }

            int intResultInterval = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Interval);
            if (intResultInterval > 0)
            {
                strValue_interval = intResultInterval.ToString();
            }
            else
            {
                //setting not available or corrupted
                strValue_interval = "--";
            }


            int intResultEnabled = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Enabled);
            if (intResultEnabled == 1)
            {
                //Regular Printing: ON
                //get current date
                DateTime dtNow = DateTime.Now;

                //get lastprint date
                int intLastPrintYear = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_LastPrintTime_Year);
                int intLastPrintMonth = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_LastPrintTime_Month);
                int intLastPrintDay = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_LastPrintTime_Day);
                int intLastPrintHour = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_LastPrintTime_Hour);
                int intLastPrintMinute = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_LastPrintTime_Minute);
                int intLastPrintSecond = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_LastPrintTime_Second);

                DateTime dtLastPrintTime;

                if (intLastPrintYear > 0 && intLastPrintMonth > 0 && intLastPrintDay > 0 && intLastPrintHour >= 0 && intLastPrintMinute >= 0 && intLastPrintSecond >= 0) {

                    try
                    {
                        //last print time is valid, now caculate
                        dtLastPrintTime = new DateTime(intLastPrintYear, intLastPrintMonth, intLastPrintDay, intLastPrintHour, intLastPrintMinute, intLastPrintSecond);

                        DateTime dtScheduledPrintIsValidFrom = dtLastPrintTime.AddDays(intResultInterval).AddMinutes(1);

                        int intCompare = DateTime.Compare(dtScheduledPrintIsValidFrom, dtNow);

                        if (intCompare < 0)
                        {
                            //dtScheduledPrintIsValidFrom is eariler than current date
                            strValue_NextScheduledDate = "Today";
                        }
                        else {
                            strValue_NextScheduledDate = dtScheduledPrintIsValidFrom.ToString("yyyy/MM/dd HH:mm");
                        }
                    }
                    catch (Exception) {
                        //no valid last print time, means scheduled print is today
                        strValue_NextScheduledDate = "Today";
                    }
                }
                else {
                    //no valid last print time, means scheduled print is today
                    strValue_NextScheduledDate = "Today";

                }

            }
            else
            {
                //Regular Printing: OFF
                strValue_NextScheduledDate = "--";
            }

            StrFooter = strStart_PartNextScheduled + strValue_NextScheduledDate + strMidPart_PrintingInterval + strValue_interval + strEndPart_Days;

        }


        


        

        

    }
}
