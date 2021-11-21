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
using PrintheadMaintainerUI.Commands;
using PrintheadMaintainerUI.Enums;
using PrintheadMaintainerUI.GlobalConstants;
using PrintheadMaintainerUI.Interfaces;
using PrintheadMaintainerUI.Mediators;
using PrintheadMaintainerUI.Utils;
using System;
using System.Timers;
using System.Windows.Input;

namespace PrintheadMaintainerUI.ViewModels
{
    public class HomeViewModel : ViewModelBase, IPageViewModel
    {

        private ICommand _switchToPrintNowView;
        private ICommand _switchToSettingsView;
        private ICommand _switchToLogsView;
        private ICommand _switchToAboutView;

        private ServiceState _serviceStateForConverter;

        private string _strSummary_SvcStatus;

        private string _strSummary_ScheduledPrinting;
        private string _strSummary_LastPrint;

        private ValueState _vsValueStateEnabled;
        private ValueState _vsValueStateLastPrint;

        private string _strSummary_Problems_ExcludeServiceStopped;
        

        private ValueState _vsValueStateProblems_ExcludeServiceStopped;
        

        //Setting Error Type
        private const int intErrorType_NO_ERROR = 0;
        private const int intErrorType_SCHEDULED_PRINTING_DISABLED = -1;
        private const int intErrorType_PRINTERNAME_INVALID = -2;
        private const int intErrorType_INTERVAL_INVALID = -3;
        private const int intErrorType_TARGET_IMAGE_INVALID = -4;
        //Setting Error Type

        private bool bServiceStopped = false;

        //service error flag
        private bool bIfSettingIsEnabledAndValid = true;
        private int intReasonOfInvalidSetting = intErrorType_NO_ERROR;
        
        private bool bIfLastScheduledFailIsOld = true; // If Last scheduled fail is not old(later than last print) , it means a new print failure and we raise warning

        public ICommand SwitchToPrintNowView
        {
            get
            {
                return _switchToPrintNowView ?? (_switchToPrintNowView = new RelayCommand(x =>
                {
                    Mediator.Notify("SwitchToPrintNow", "");
                }));
            }
        }

        public ICommand SwitchToSettingsView
        {
            get
            {
                return _switchToSettingsView ?? (_switchToSettingsView = new RelayCommand(x =>
                {
                    Mediator.Notify("SwitchToSettings", "");
                }));
            }
        }

        public ICommand SwitchToLogsView
        {
            get
            {
                return _switchToLogsView ?? (_switchToLogsView = new RelayCommand(x =>
                {
                    Mediator.Notify("SwitchToLogs", "");
                }));
            }
        }

        public ICommand SwitchToAboutView
        {
            get
            {
                return _switchToAboutView ?? (_switchToAboutView = new RelayCommand(x =>
                {
                    Mediator.Notify("SwitchToAbout", "");
                }));
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

        public ValueState ValueStateEnabled
        {
            get
            {
                return _vsValueStateEnabled;
            }
            set
            {
                _vsValueStateEnabled = value;
                OnPropertyChanged(nameof(ValueStateEnabled));

            }
        }

        public ValueState ValueStateLastPrint
        {
            get
            {
                return _vsValueStateLastPrint;
            }
            set
            {
                _vsValueStateLastPrint = value;
                OnPropertyChanged(nameof(ValueStateLastPrint));

            }
        }


        public string StrSummary_LastPrint 
        {
            get {
                return _strSummary_LastPrint;
            }
            set {
                _strSummary_LastPrint = value;
                OnPropertyChanged(nameof(StrSummary_LastPrint));
            }
        }

        public string StrSummary_SvcStatus {
            get
            {
                return _strSummary_SvcStatus;
            }
            set 
            {
                _strSummary_SvcStatus = value;
                OnPropertyChanged(nameof(StrSummary_SvcStatus));
            } 
        }

        public string StrSummary_Problems
        {
            get {
                if (bServiceStopped)
                {
                    return "Service is not running";
                }
                else {
                    return _strSummary_Problems_ExcludeServiceStopped;
                }
            
            }
        
        }
        
        public ValueState ValueStateProblems {
            get {
                if (bServiceStopped)
                {
                    return ValueState.Error;
                }
                else {
                    return _vsValueStateProblems_ExcludeServiceStopped;
                }
            }
        }

        public string StrSummary_ScheduledPrinting {
            get {
                return _strSummary_ScheduledPrinting;
            }
            set {
                _strSummary_ScheduledPrinting = value;
                OnPropertyChanged(nameof(StrSummary_ScheduledPrinting));
            }
        }


        public HomeViewModel()
        {

            VoidUpdateUI_ScheduledPrinting_AndValidateScheduledPrintingSetting();
            VoidUpdateUI_LastPrint_AndCheckLastPrintFailResultIsOld(true);
            VoidUpdateUI_Problems_ExcludeServiceStopped();

            Mediator.Subscribe("RefreshLastPrint", Callback_RefreshLastPrintAndProblems);

            VoidStartServiceStateUpdateLoop();
            VoidStartLastPrintUpdateLoop();

        }

        public void VoidRefreshView() {

            VoidUpdateUI_ScheduledPrinting_AndValidateScheduledPrintingSetting();
            VoidUpdateUI_LastPrint_AndCheckLastPrintFailResultIsOld(true);
            VoidUpdateUI_Problems_ExcludeServiceStopped();
        }

        private void Callback_RefreshLastPrintAndProblems(object obj)
        {
            VoidUpdateUI_LastPrint_AndCheckLastPrintFailResultIsOld(true);
            VoidUpdateUI_Problems_ExcludeServiceStopped();
        }

        private void VoidUpdateUI_ScheduledPrinting_AndValidateScheduledPrintingSetting()
        {
            //we update UI and check if setting is valid and it is enabled, we update bool flag "bIfSettingIsEnabledAndValid" and intErrorType

            RegistryKey rkHandle = RegistryUtils.RkOpenRegistyHandle(false);
            if (rkHandle == null)
            {
                //registry not exist, scheduled printing is off
                bIfSettingIsEnabledAndValid = false;
                intReasonOfInvalidSetting = intErrorType_SCHEDULED_PRINTING_DISABLED;

                StrSummary_ScheduledPrinting = "OFF";
                ValueStateEnabled = ValueState.Error;
                return;
            }

            //check "enabled" setting
            int intResultEnabled = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Enabled);
            if (intResultEnabled == 1)
            {
                StrSummary_ScheduledPrinting = "ON";
                ValueStateEnabled = ValueState.OK;
            }
            else
            {
                bIfSettingIsEnabledAndValid = false;
                intReasonOfInvalidSetting = intErrorType_SCHEDULED_PRINTING_DISABLED;

                StrSummary_ScheduledPrinting = "OFF";
                ValueStateEnabled = ValueState.Error;
                return;
            }

            string strResultPrinterName = RegistryUtils.StrRegistryReadSingleLineString(rkHandle, RegistryConstants.strRegistryValue_PrinterName);
            if (strResultPrinterName == null || strResultPrinterName.Equals(""))
            {
                bIfSettingIsEnabledAndValid = false;
                intReasonOfInvalidSetting = intErrorType_PRINTERNAME_INVALID;
                return;
            }

            int intResultInterval = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Interval);
            if (intResultInterval <= 0)
            {
                bIfSettingIsEnabledAndValid = false;
                intReasonOfInvalidSetting = intErrorType_INTERVAL_INVALID;
                return;
            }

            string strResultBmpPath = RegistryUtils.StrRegistryReadSingleLineString(rkHandle, RegistryConstants.strRegistryValue_BmpPath);
            if (strResultBmpPath == null || strResultBmpPath.Equals(""))
            {
                bIfSettingIsEnabledAndValid = false;
                intReasonOfInvalidSetting = intErrorType_TARGET_IMAGE_INVALID;
                return;
            }

            bIfSettingIsEnabledAndValid = true;
            intReasonOfInvalidSetting = intErrorType_NO_ERROR;

        }

        private void VoidUpdateUI_LastPrint_AndCheckLastPrintFailResultIsOld(bool bCheckLastPrintFailResultOrNot)
        {
            //we check if last scheduled printing failure record is later than Last printing, if yes, it means the newest scheduled printing fails
            //we update bool flag "bIfLastScheduledFailIsOld" for this

            //get last print
            RegistryKey rkHandle = RegistryUtils.RkOpenRegistyHandle(false);
            if (rkHandle == null)
            {
                //registry not exist, just use never, but dont raise last print fail warning
                StrSummary_LastPrint = "Never";
                ValueStateLastPrint = ValueState.Warning;

                bIfLastScheduledFailIsOld = true;
                return;
            }

            //get lastprint date
            int intLastPrintYear = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_LastPrintTime_Year);
            int intLastPrintMonth = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_LastPrintTime_Month);
            int intLastPrintDay = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_LastPrintTime_Day);
            int intLastPrintHour = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_LastPrintTime_Hour);
            int intLastPrintMinute = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_LastPrintTime_Minute);
            int intLastPrintSecond = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_LastPrintTime_Second);

            if (intLastPrintYear <= 0 || intLastPrintMonth <= 0 || intLastPrintDay <= 0 || intLastPrintHour < 0 || intLastPrintMinute < 0 || intLastPrintSecond < 0)
            {
                //data corrupted or not avaialible
                StrSummary_LastPrint = "Never";
                ValueStateLastPrint = ValueState.Warning;

                bIfLastScheduledFailIsOld = true;
                return;

            }

            DateTime dtNow = DateTime.Now;
            DateTime dtLastPrintTime;

            try
            {
                dtLastPrintTime = new DateTime(intLastPrintYear, intLastPrintMonth, intLastPrintDay, intLastPrintHour, intLastPrintMinute, intLastPrintSecond);
            }
            catch (Exception)
            {
                //Error occured
                StrSummary_LastPrint = "Never";
                ValueStateLastPrint = ValueState.Warning;

                bIfLastScheduledFailIsOld = true;
                return;
            }

            //Compare LastPrint and current date , and update last print parameter
            int intCompare = DateTime.Compare(dtLastPrintTime, dtNow);
            if (intCompare > 0) {

                //last print date is in future, return error
                StrSummary_LastPrint = "Never";
                ValueStateLastPrint = ValueState.Warning;

                bIfLastScheduledFailIsOld = true;
                return;
            }
            
            //last print date is eariler or the same as current date, Last Print Date is valid
            TimeSpan tsPeriod = dtNow - dtLastPrintTime;

            int intDays = tsPeriod.Days;
            int intHours = tsPeriod.Hours;
            int intMinutes = tsPeriod.Minutes;

            //if timespan bigger than a day, use day as unit, else use hours => minutes
            if (intDays > 0)
            {
                //check if over interval + 3 days to decide value color, if over interval+3day = red, else green
                int intResultInterval = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Interval);
                if (intResultInterval > 0)
                {
                    //interval valid, now check
                    DateTime dtLastPrintAddIntervakAndThreeDay = dtLastPrintTime.AddDays(intResultInterval + 3);
                    int intCompare2 = DateTime.Compare(dtLastPrintAddIntervakAndThreeDay, dtNow);
                    if (intCompare2 < 0)
                    {
                        //Current time is grater than LastPrint+ interval + 3days
                        ValueStateLastPrint = ValueState.Error;
                    }
                    else
                    {
                        ValueStateLastPrint = ValueState.OK;
                    }

                }
                else
                {
                    //interval not valid , dont cheeck
                    ValueStateLastPrint = ValueState.OK;
                }

                //return value
                if (intDays > 1)
                {
                    StrSummary_LastPrint = intDays.ToString() + " days ago";
                }
                else
                {
                    StrSummary_LastPrint = intDays.ToString() + " day ago";
                }
            }
            else
            {
                //check hours and minutes
                //last print less than a day, so it's always green
                ValueStateLastPrint = ValueState.OK;
                if (intHours > 0)
                {
                    if (intHours > 1)
                    {
                        StrSummary_LastPrint = intHours.ToString() + " hours ago";
                    }
                    else
                    {
                        StrSummary_LastPrint = intHours.ToString() + " hour ago";
                    }
                }
                else
                {
                    //check minutes
                    if (intMinutes > 0)
                    {
                        if (intMinutes > 1)
                        {
                            StrSummary_LastPrint = intMinutes.ToString() + " minutes ago";
                        }
                        else
                        {
                            StrSummary_LastPrint = intMinutes.ToString() + " minute ago";
                        }
                    }
                    else
                    {
                        //seconds ago, just return 1 mnute
                        StrSummary_LastPrint = "1 minute ago";
                    }
                }
            }

            if (!bCheckLastPrintFailResultOrNot) {
                return;
            }

            //now we check if last scheduled printing failure record is later than Last printing
            //get Last Scheduled Printing Failure
            int intLastScheduleFailYear = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Year);
            int intLastScheduleFailMonth = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Month);
            int intLastScheduleFailDay = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Day);
            int intLastScheduleFailHour = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Hour);
            int intLastScheduleFailMinute = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Minute);
            int intLastScheduleFailSecond = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Second);

            if (intLastScheduleFailYear <= 0 || intLastScheduleFailMonth <= 0 || intLastScheduleFailDay <= 0 || intLastScheduleFailHour < 0 || intLastScheduleFailMinute < 0 || intLastScheduleFailSecond < 0)
            {
                //data corrupted or Last printing failure record not avaialible
                bIfLastScheduledFailIsOld = true;
                return;
            }

            DateTime dtLastScheduledFail;

            try
            {
                dtLastScheduledFail = new DateTime(intLastScheduleFailYear, intLastScheduleFailMonth, intLastScheduleFailDay, intLastScheduleFailHour, intLastScheduleFailMinute, intLastScheduleFailSecond);
            }
            catch (Exception)
            {
                //Error occured
                bIfLastScheduledFailIsOld = true;
                return;
            }

            //to here , both last print and last scheduled print fail datetime is valid
            int intCompare3 = DateTime.Compare(dtLastScheduledFail, dtLastPrintTime);
            if (intCompare3 <= 0)
            {
                //dtLastScheduledFail is earlier than or the same as dtLastPrintTime, not expired
                bIfLastScheduledFailIsOld = true;
                return;
            }
            else
            {
                //dtLastScheduledFail is later than dtLastPrintTime, expired
                bIfLastScheduledFailIsOld = false;
                return;
            }
        }

        private void VoidUpdateUI_Problems_ExcludeServiceStopped() {
            //check two bools
            if (bIfSettingIsEnabledAndValid && bIfLastScheduledFailIsOld)
            {
                //no error exist
                _strSummary_Problems_ExcludeServiceStopped = "None";
                _vsValueStateProblems_ExcludeServiceStopped = ValueState.OK;

            }
            else {
                //some error exist

                if (!bIfSettingIsEnabledAndValid)
                {
                    //settings corrupted, determine why and write reason , raise error state
                    _vsValueStateProblems_ExcludeServiceStopped = ValueState.Error;
                    switch (intReasonOfInvalidSetting)
                    {
                        case intErrorType_SCHEDULED_PRINTING_DISABLED:
                            _strSummary_Problems_ExcludeServiceStopped = "Scheduled printing is disabled";
                            break;
                        case intErrorType_PRINTERNAME_INVALID:
                            _strSummary_Problems_ExcludeServiceStopped = "Printer name haven't been set in settings";
                            break;
                        case intErrorType_INTERVAL_INVALID:
                            _strSummary_Problems_ExcludeServiceStopped = "Printing interval haven't been set in settings";
                            break;
                        case intErrorType_TARGET_IMAGE_INVALID:
                            _strSummary_Problems_ExcludeServiceStopped = "Target Image file haven't been set in settings";
                            break;
                        default:
                            _strSummary_Problems_ExcludeServiceStopped = "Unknown error occured";
                            break;
                    }

                }
                else {
                    //Last printing failed, raise warning state
                    _vsValueStateProblems_ExcludeServiceStopped = ValueState.Warning;

                    string strFailureTimeString = StrGenerateLastScheduledFailureTimeStringFromRegistry();
                    if (strFailureTimeString != null)
                    {
                        _strSummary_Problems_ExcludeServiceStopped = "Scheduled printing failed at " + strFailureTimeString;
                    }
                    else {
                        _strSummary_Problems_ExcludeServiceStopped = "Scheduled printing failed";
                    }

                }
            }
        }

        private string StrGenerateLastScheduledFailureTimeStringFromRegistry() {
            //get last print
            RegistryKey rkHandle = RegistryUtils.RkOpenRegistyHandle(false);
            if (rkHandle == null)
            {
                return null;
            }

            //We coded in this format for better execution performance, though it is messy.
            int intResultYear = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Year);
            if (intResultYear < 0)
            {
                return null;
            }

            int intResultMonth = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Month);
            if (intResultMonth < 0)
            {
                return null;
            }

            int intResultDay = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Day);
            if (intResultDay < 0)
            {
                return null;
            }

            int intResultDayOfWeek = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_DayOfWeek);
            if (intResultDayOfWeek < 0)
            {
                return null;
            }

            int intResultHour = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Hour);
            if (intResultHour < 0)
            {
                return null;
            }

            int intResultMinute = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Scheduled_PrintFailRecord_Minute);
            if (intResultMinute < 0)
            {
                return null;
            }

            string strFixedMonth = intResultMonth.ToString();
            if (intResultMonth < 10)
            {
                strFixedMonth = "0" + strFixedMonth;
            }
            string strFixedDay = intResultDay.ToString();
            if (intResultDay < 10)
            {
                strFixedDay = "0" + strFixedDay;
            }
            string strFixedMinute = intResultMinute.ToString();
            if (intResultMinute < 10)
            {
                strFixedMinute = "0" + strFixedMinute;
            }

            string strDayOfWeek;
            switch (intResultDayOfWeek)
            {
                case 0:
                    strDayOfWeek = "Sun";
                    break;
                case 1:
                    strDayOfWeek = "Mon";
                    break;
                case 2:
                    strDayOfWeek = "Tue";
                    break;
                case 3:
                    strDayOfWeek = "Wed";
                    break;
                case 4:
                    strDayOfWeek = "Thu";
                    break;
                case 5:
                    strDayOfWeek = "Fri";
                    break;
                case 6:
                    strDayOfWeek = "Sat";
                    break;
                default:
                    strDayOfWeek = "";
                    break;

            }

            string strGeneratedOutput = intResultYear.ToString() + "/" + strFixedMonth + "/" + strFixedDay + " " + intResultHour.ToString() + ":" + strFixedMinute + " " + strDayOfWeek;
            return strGeneratedOutput;
        }

        private void VoidStartLastPrintUpdateLoop() {

            Timer timer = new Timer();
            timer.Interval = 300000; //every 5min
            timer.Elapsed += Timer_Elapsed_LastPrintChecker;
            timer.Start();

        }

        private void Timer_Elapsed_LastPrintChecker(object sender, ElapsedEventArgs e)
        {
            VoidUpdateUI_LastPrint_AndCheckLastPrintFailResultIsOld(false);
        }


        private void VoidStartServiceStateUpdateLoop()
        {

            Timer timer = new Timer();
            timer.Interval = 8000; //every 8 sec
            timer.Elapsed += Timer_Elapsed_ServiceStateChecker;
            timer.Start();

            VoidUpdateServiceState();

        }

        private void Timer_Elapsed_ServiceStateChecker(object sender, ElapsedEventArgs e)
        {
            VoidUpdateServiceState();
        }


        public void VoidUpdateServiceState()
        {

            ServiceUtils.ServiceStatus ssServiceStatus = ServiceUtils.SsGetServiceStatus(ServiceConstants.strServiceName);
            
            ServiceState ssServiceState;

            //first, service stopp is more important than everything, if service is stopped, no setting can be changed
            if (ssServiceStatus == ServiceUtils.ServiceStatus.Stopped)
            {
                //service stopped
                StrSummary_SvcStatus = "Service has stopped";
                ssServiceState = ServiceState.Error;

                bServiceStopped = true;
                //notify change
                OnPropertyChanged(nameof(StrSummary_Problems));
                OnPropertyChanged(nameof(ValueStateProblems));
            }
            else {
                //service is still running, now check other problems
                bServiceStopped = false;
                //notify change
                OnPropertyChanged(nameof(StrSummary_Problems));
                OnPropertyChanged(nameof(ValueStateProblems));

                //first determine via bool, second determine via rest of the servicestatus
                if (!bIfSettingIsEnabledAndValid)
                {
                    StrSummary_SvcStatus = "Not functioning";
                    ssServiceState = ServiceState.Error;
                }
                else if (!bIfLastScheduledFailIsOld)
                {
                    //Last scheduled fail is new, it is later than last print, so raise a warning
                    StrSummary_SvcStatus = "Attention required";
                    ssServiceState = ServiceState.Warning;
                }
                else
                {
                    //no setting or last scheduled printing fail error, now simply determine service state with service running status
                    switch (ssServiceStatus)
                    {
                        case ServiceUtils.ServiceStatus.Running:
                            StrSummary_SvcStatus = "In good shape";
                            ssServiceState = ServiceState.OK;
                            break;
                        case ServiceUtils.ServiceStatus.StartPending:
                            StrSummary_SvcStatus = "Service is starting...";
                            ssServiceState = ServiceState.Transforming;
                            break;
                        case ServiceUtils.ServiceStatus.StopPending:
                            StrSummary_SvcStatus = "Service is stopping...";
                            ssServiceState = ServiceState.Transforming;
                            break;
                        case ServiceUtils.ServiceStatus.UtilExceptionError:
                            StrSummary_SvcStatus = "Unable to connect to the service";
                            ssServiceState = ServiceState.Error;
                            break;
                        default:
                            StrSummary_SvcStatus = "Checking...";
                            ssServiceState = ServiceState.Transforming;
                            break;
                    }
                }
            }

            ServiceStateForConverter = ssServiceState;

            //now send signal to change Window Icon, Tray Icon
            switch (ssServiceState)
            {
                case ServiceState.OK:
                    {
                        Mediator.Notify("WinOK", "");
                        Mediator.Notify("TrayOK", "");
                        break;
                    }
                case ServiceState.Warning:
                    {
                        Mediator.Notify("WinWarning", "");
                        Mediator.Notify("TrayWarning", "");
                        break;
                    }
                case ServiceState.Error:
                    {
                        Mediator.Notify("WinError", "");
                        Mediator.Notify("TrayError", "");
                        break;
                    }
                case ServiceState.Transforming:
                    {
                        Mediator.Notify("WinDefault", "");
                        Mediator.Notify("TrayDefault", "");
                        break;
                    }

                default:
                    break;
            }

        }


    }
}
