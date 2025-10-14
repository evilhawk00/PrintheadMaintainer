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
using PrintheadMaintainerUI.GlobalConstants;
using PrintheadMaintainerUI.Interfaces;
using PrintheadMaintainerUI.Mediators;
using PrintheadMaintainerUI.Models;
using PrintheadMaintainerUI.NamedPipeClient;
using PrintheadMaintainerUI.Singletons;
using PrintheadMaintainerUI.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PrintheadMaintainerUI.ViewModels
{
    public class SettingsViewModel : ViewModelBase, IPageViewModel
    {
        private ICommand _switchToHomeView;
        private ICommand _switchToChoosePrinterView;
        private ICommand _refreshPaperSources;

        private bool _bSettingsEnabled;
        private bool _bSettingsEnabled_Original;

        private string _strUnsaved_New_PrinterName;
        private string _strPrinterName;

        private string _strSettingsImagePath;
        private string _strUnsaved_New_SettingsImagePath;

        private int _intSettingsInterval;
        private int _intSettingsInterval_Original;
        private bool bIfSettingsIntervalUsesDefaultValue = false;

        private string _strTextApplyResult;

        private ObservableCollection<PaperSourceOption> _paperSourceOptions = new ObservableCollection<PaperSourceOption>();
        private PaperSourceOption _selectedPaperSourceOption;
        private int _currentPaperSourceRawKind;
        private int? _pendingPaperSourceRawKind;

        //LOCKS
        private bool bApplyEnabledSettings_LOCK = false;
        private bool bApplyPrinterNameSettings_LOCK = false;
        private bool bApplyIntervalSettings_LOCK = false;
        private bool bApplyBmpPathSettings_LOCK = false;
        private bool bApplyPaperSourceSettings_LOCK = false;

        //Apply Result Flag
        private bool bApplyResultOK_Enabled = true;
        private bool bApplyResultOK_PrinterName = true;
        private bool bApplyResultOK_Interval = true;
        private bool bApplyResultOK_BmpPath = true;
        private bool bApplyResultOK_PaperSource = true;

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

        public ICommand SwitchToChoosePrinterView
        {
            get
            {
                return _switchToChoosePrinterView ?? (_switchToChoosePrinterView = new RelayCommand(x =>
                {
                    Mediator.Notify("SwitchToChoosePrinter", "");
                }));
            }
        }

        public ICommand CmdSelectBmp
        {

            get
            {
                //send value to server
                return new RelayCommand(x =>
                {
                    VoidOnClickSelectBmpBtn();
                });
            }

        }

        public ICommand CmdRefreshPaperSources

        {

            get

            {

                return _refreshPaperSources ?? (_refreshPaperSources = new RelayCommand(x =>

                {

                    VoidReloadPaperSourcesForCurrentPrinter();

                }));

            }

        }



        public ICommand CmdApplySettings
        {

            get
            {
                //send value to server
                return new RelayCommand(x =>
                {
                    VoidOnClickBtnApplyAllSettings();
                }, x => 
                { 
                    return BIsEnabled_BtnApplyOrDiscardChanges(); 
                });
            }

        }

        


        public ICommand CmdDiscardChanges{

            get
            {
                //send value to server
                return new RelayCommand(x =>
                {
                    VoidOnClickBtnDiscardChanges();
                }, x =>
                {
                    return BIsEnabled_BtnApplyOrDiscardChanges();
                });
            }

        }


        private bool BIsEnabled_BtnApplyOrDiscardChanges()
        {
            //first check all locks
            if (!bApplyEnabledSettings_LOCK && !bApplyPrinterNameSettings_LOCK && !bApplyIntervalSettings_LOCK && !bApplyBmpPathSettings_LOCK && !bApplyPaperSourceSettings_LOCK)
            {
                // all locks are not locked

                if (_bSettingsEnabled != _bSettingsEnabled_Original)
                {
                    return true;
                }
                else if (_strUnsaved_New_PrinterName != null && !_strUnsaved_New_PrinterName.Equals(""))
                {
                    return true;
                }
                else if (_pendingPaperSourceRawKind.HasValue)
                {
                    return true;
                }
                else if (_intSettingsInterval != _intSettingsInterval_Original || bIfSettingsIntervalUsesDefaultValue)
                {
                    return true;
                }
                else if (_strUnsaved_New_SettingsImagePath != null && !_strUnsaved_New_SettingsImagePath.Equals(""))
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            else
            {
                return false;
            }
        }

        public bool BSettingsEnabled {
            get
            {
                return _bSettingsEnabled;
            }
            set
            {
                if (value != _bSettingsEnabled) {
                    _bSettingsEnabled = value;
                    OnPropertyChanged(nameof(BSettingsEnabled));
                } 
            }
        }
        public string StrSettingsPrinterName {

            get {

                if (VoidGetPrinterNameFromSingleton())
                {
                    return _strUnsaved_New_PrinterName;
                }
                else {
                    return _strPrinterName;
                }

            }

        }
        public int IntSettingsInterval {

            get {

                return _intSettingsInterval;

            }
            set {
                if (value != _intSettingsInterval) {
                    _intSettingsInterval = value;
                    OnPropertyChanged(nameof(_intSettingsInterval));
                }
                
            }


        }
        public string StrSettingsImagePath {

            get {
                if (_strUnsaved_New_SettingsImagePath != null && !_strUnsaved_New_SettingsImagePath.Equals(""))
                {
                    return _strUnsaved_New_SettingsImagePath;
                }
                else {
                    return _strSettingsImagePath;
                }

            }

        }

        public ObservableCollection<PaperSourceOption> PaperSourceOptions
        {
            get
            {
                return _paperSourceOptions;
            }
        }

        public PaperSourceOption SelectedPaperSourceOption
        {
            get
            {
                return _selectedPaperSourceOption;
            }
            set
            {
                if (_selectedPaperSourceOption != value)
                {
                    _selectedPaperSourceOption = value;

                    if (value == null)
                    {
                        _pendingPaperSourceRawKind = null;
                    }
                    else if (value.RawKind != _currentPaperSourceRawKind)
                    {
                        _pendingPaperSourceRawKind = value.RawKind;
                    }
                    else
                    {
                        _pendingPaperSourceRawKind = null;
                    }

                    OnPropertyChanged(nameof(SelectedPaperSourceOption));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string StrTextApplyResult
        {
            get {
                return _strTextApplyResult;
            }
            set {
                _strTextApplyResult = value;
                OnPropertyChanged(nameof(StrTextApplyResult));
            }
        
        }

        public void VoidRefreshView() {

            VoidUpdateUISettings();


        }

        public SettingsViewModel() {

            VoidUpdateUISettings();

        }

        private void VoidUpdateUISettings() {

            //reset old value
            VoidResetPrinterNameSingleton();
            _strUnsaved_New_SettingsImagePath = null;

            RegistryKey rkHandle = RegistryUtils.RkOpenRegistyHandle(false);
            if (rkHandle == null)
            {
                //at this time may be the first start so the registry is still not created
                _strPrinterName = "(Not set)";
                //setting not available or corrupted, set to default 7
                _intSettingsInterval_Original = 7;
                //initialize int variable
                _intSettingsInterval = _intSettingsInterval_Original;
                bIfSettingsIntervalUsesDefaultValue = true;
                _strSettingsImagePath = "(Not set)";

                _currentPaperSourceRawKind = 0;
                _pendingPaperSourceRawKind = null;
                VoidReloadPaperSourcesForPrinter(null, _currentPaperSourceRawKind, false);

                return;
            }

            int intResultEnabled = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Enabled);
            //default is off, if setting corrupted, just set to 0
            if (intResultEnabled == 1)
            {
                _bSettingsEnabled_Original = true;
            }
            else {
                _bSettingsEnabled_Original = false;
            }

            //initialize bool variable
            _bSettingsEnabled = _bSettingsEnabled_Original;

            string strResultPrinterName = RegistryUtils.StrRegistryReadSingleLineString(rkHandle, RegistryConstants.strRegistryValue_PrinterName);
            if (strResultPrinterName != null && !strResultPrinterName.Equals(""))
            {
                _strPrinterName = strResultPrinterName;
            }
            else {
                _strPrinterName = "(Not set)";
            }

            int intResultPaperSource = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_PrinterPaperSource);
            if (intResultPaperSource >= 0)
            {
                _currentPaperSourceRawKind = intResultPaperSource;
            }
            else
            {
                _currentPaperSourceRawKind = 0;
            }

            _pendingPaperSourceRawKind = null;

            string printerNameForSources = (!string.IsNullOrEmpty(_strPrinterName) && !_strPrinterName.Equals("(Not set)", StringComparison.Ordinal)) ? _strPrinterName : null;
            VoidReloadPaperSourcesForPrinter(printerNameForSources, _currentPaperSourceRawKind, false);

            int intResultInterval = RegistryUtils.IntReadUnsignedInteger(rkHandle, RegistryConstants.strRegistryValue_Interval);
            if (intResultInterval > 0)
            {
                _intSettingsInterval_Original = intResultInterval;
            }
            else {
                //setting not available or corrupted, set to default 7
                _intSettingsInterval_Original = 7;
                bIfSettingsIntervalUsesDefaultValue = true;
            }

            //initialize int variable
            _intSettingsInterval = _intSettingsInterval_Original;

            string strResultBmpPath = RegistryUtils.StrRegistryReadSingleLineString(rkHandle, RegistryConstants.strRegistryValue_BmpPath);
            if (strResultBmpPath != null && !strResultBmpPath.Equals(""))
            {
                _strSettingsImagePath = strResultBmpPath;
            }
            else {
                _strSettingsImagePath = "(Not set)";
            }



        }

        private void VoidOnClickSelectBmpBtn() {

            string strChosenImage = FileSelectorUtils.StrBrowseBmpFile();
            if (strChosenImage != null && !strChosenImage.Equals("")) {
                _strUnsaved_New_SettingsImagePath = strChosenImage;
                OnPropertyChanged(nameof(StrSettingsImagePath));
            }
            
            

        }

        private void VoidApplyEnabledSettings()
        {
            bApplyEnabledSettings_LOCK = true;
            bApplyResultOK_Enabled = false;

            int intValue = Convert.ToInt32(_bSettingsEnabled);
            string strValueData = intValue.ToString();

            //encode msg
            string strRequestID = RandomUtils.StrGenerateRandomHexadecimalString(8);
            string strFinalMsg = IPCClient.StrMessageEncoder(strRequestID, NamedPipeConstants.JobFlag_WriteRegistryEnabled, strValueData);
            _ = IPCClient.AsyncSendMsgToServerAndDecodeResponse(strRequestID, strFinalMsg, VoidApplyEnabledSettings_Callback);

        }

        private void VoidApplyEnabledSettings_Callback(int intResult)
        {

            if (intResult == NamedPipeConstants.intServerResult_SUCCESS)
            {
                _bSettingsEnabled_Original = _bSettingsEnabled;
                bApplyResultOK_Enabled = true;
            }
            else {
                //operation failed, revert to original
                _bSettingsEnabled = _bSettingsEnabled_Original;
            }
            OnPropertyChanged(nameof(BSettingsEnabled));

            bApplyEnabledSettings_LOCK = false;
            VoidUpdateText_ApplyResult();
        }

        private void VoidApplyPrinterNameSettings()
        {
            bApplyPrinterNameSettings_LOCK = true;
            bApplyResultOK_PrinterName = false;

            //encode msg
            string strRequestID = RandomUtils.StrGenerateRandomHexadecimalString(8);
            string strFinalMsg = IPCClient.StrMessageEncoder(strRequestID, NamedPipeConstants.JobFlag_WriteRegistryPrinterName, _strUnsaved_New_PrinterName);
            _ = IPCClient.AsyncSendMsgToServerAndDecodeResponse(strRequestID, strFinalMsg, VoidApplyPrinterNameSettings_Callback);

        }


        private void VoidApplyPrinterNameSettings_Callback(int intResult)
        {

            if (intResult == NamedPipeConstants.intServerResult_SUCCESS)
            {
                _strPrinterName = _strUnsaved_New_PrinterName;
               
                bApplyResultOK_PrinterName = true;
            }

            //no matter success or fail, both need to reset setting
            VoidResetPrinterNameSingleton();
            _strUnsaved_New_PrinterName = null;

            VoidReloadPaperSourcesForCurrentPrinter();

            OnPropertyChanged(nameof(StrSettingsPrinterName));

            bApplyPrinterNameSettings_LOCK = false;
            VoidUpdateText_ApplyResult();
            CommandManager.InvalidateRequerySuggested();
            return;
        }

        private void VoidApplyIntervalSettings()
        {
            bApplyIntervalSettings_LOCK = true;
            bApplyResultOK_Interval = false;

            //encode msg
            string strRequestID = RandomUtils.StrGenerateRandomHexadecimalString(8);
            string strFinalMsg = IPCClient.StrMessageEncoder(strRequestID, NamedPipeConstants.JobFlag_WriteRegistryInterval, _intSettingsInterval.ToString());
            _ = IPCClient.AsyncSendMsgToServerAndDecodeResponse(strRequestID, strFinalMsg, VoidApplyIntervalSettings_Callback);

        }


        private void VoidApplyIntervalSettings_Callback(int intResult)
        {

            if (intResult == NamedPipeConstants.intServerResult_SUCCESS)
            {
                _intSettingsInterval_Original = _intSettingsInterval;
                bApplyResultOK_Interval = true;
                //if use default value, reset here
                bIfSettingsIntervalUsesDefaultValue = false;
            }
            else {
                //operation failed,revert back to original setting
                _intSettingsInterval = _intSettingsInterval_Original;
            
            }
            OnPropertyChanged(nameof(IntSettingsInterval));

            bApplyIntervalSettings_LOCK = false;
            VoidUpdateText_ApplyResult();
            return;
        }


        private void VoidApplyBmpPathSettings() {

            bApplyBmpPathSettings_LOCK = true;
            bApplyResultOK_BmpPath = false;

            //encode msg
            string strRequestID = RandomUtils.StrGenerateRandomHexadecimalString(8);
            string strFinalMsg = IPCClient.StrMessageEncoder(strRequestID,NamedPipeConstants.JobFlag_WriteRegistryBmpPath, _strUnsaved_New_SettingsImagePath );
            _ = IPCClient.AsyncSendMsgToServerAndDecodeResponse(strRequestID, strFinalMsg, VoidApplyBmpPathSettings_Callback);

        }


        private void VoidApplyBmpPathSettings_Callback(int intResult) {

            if (intResult == NamedPipeConstants.intServerResult_SUCCESS) {
                _strSettingsImagePath = _strUnsaved_New_SettingsImagePath;
                
                bApplyResultOK_BmpPath = true;
            }

            //no matter success or not, all need to reset setting
            _strUnsaved_New_SettingsImagePath = null;

            OnPropertyChanged(nameof(StrSettingsImagePath));

            bApplyBmpPathSettings_LOCK = false;
            VoidUpdateText_ApplyResult();

            return;
        }

        private bool VoidGetPrinterNameFromSingleton() {

            PrinterNameSingletons pnsInstance = PrinterNameSingletons.Instance;
            string strResult = pnsInstance.strPrinterName;

            if (!string.IsNullOrEmpty(strResult))
            {
                if (!string.Equals(_strUnsaved_New_PrinterName, strResult, StringComparison.Ordinal))
                {
                    _strUnsaved_New_PrinterName = strResult;
                    _pendingPaperSourceRawKind = null;
                    VoidReloadPaperSourcesForPrinter(_strUnsaved_New_PrinterName, 0, true);
                }

                return true;
            }

            return false;

        }

        private void VoidResetPrinterNameSingleton() {

            PrinterNameSingletons pnsInstance = PrinterNameSingletons.Instance;
            pnsInstance.strPrinterName = null;
        }

        public void VoidOnClickBtnApplyAllSettings() {

            //notify if service is not running
            ServiceUtils.ServiceStatus ssServiceStatus = ServiceUtils.SsGetServiceStatus(ServiceConstants.strServiceName);
            if (ssServiceStatus != ServiceUtils.ServiceStatus.Running) {
                //service is not running, alert and abort
                StrTextApplyResult = "Can not apply settings when service is not running. Please start the service first.";

                //make this text only display for 5 sec
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(5000);
                    StrTextApplyResult = "";

                });
                return;
            }

            if (_bSettingsEnabled != _bSettingsEnabled_Original) {

                VoidApplyEnabledSettings();
                VoidUpdateText_ApplyResult();
            }

            if (_strUnsaved_New_PrinterName != null && !_strUnsaved_New_PrinterName.Equals("")) {

                VoidApplyPrinterNameSettings();
                VoidUpdateText_ApplyResult();
            }

            if (_pendingPaperSourceRawKind.HasValue) {

                VoidApplyPaperSourceSettings();
                VoidUpdateText_ApplyResult();
            }

            if (_intSettingsInterval != _intSettingsInterval_Original || bIfSettingsIntervalUsesDefaultValue) {

                VoidApplyIntervalSettings();
                
                VoidUpdateText_ApplyResult();
            }

            if (_strUnsaved_New_SettingsImagePath != null && !_strUnsaved_New_SettingsImagePath.Equals("")) {

                VoidApplyBmpPathSettings();
                VoidUpdateText_ApplyResult();
            }
        }

        private void VoidOnClickBtnDiscardChanges() {

            _bSettingsEnabled = _bSettingsEnabled_Original;
            VoidResetPrinterNameSingleton();
            _strUnsaved_New_PrinterName = null;
            _intSettingsInterval = _intSettingsInterval_Original;
            _strUnsaved_New_SettingsImagePath = null;
            _pendingPaperSourceRawKind = null;

            string printerNameForSources = (!string.IsNullOrEmpty(_strPrinterName) && !_strPrinterName.Equals("(Not set)", StringComparison.Ordinal)) ? _strPrinterName : null;
            VoidReloadPaperSourcesForPrinter(printerNameForSources, _currentPaperSourceRawKind, false);

            OnPropertyChanged(nameof(BSettingsEnabled));
            OnPropertyChanged(nameof(StrSettingsPrinterName));
            OnPropertyChanged(nameof(IntSettingsInterval));
            OnPropertyChanged(nameof(StrSettingsImagePath));

            CommandManager.InvalidateRequerySuggested();

        }

        private void VoidReloadPaperSourcesForCurrentPrinter() {

            string printerName = null;
            int? rawKindToSelect = null;
            bool markAsPending = false;

            if (!string.IsNullOrEmpty(_strUnsaved_New_PrinterName))
            {
                printerName = _strUnsaved_New_PrinterName;
                rawKindToSelect = _pendingPaperSourceRawKind ?? (_selectedPaperSourceOption?.RawKind ?? 0);
                markAsPending = _pendingPaperSourceRawKind.HasValue;
            }
            else if (!string.IsNullOrEmpty(_strPrinterName) && !_strPrinterName.Equals("(Not set)", StringComparison.Ordinal))
            {
                printerName = _strPrinterName;
                rawKindToSelect = _pendingPaperSourceRawKind ?? _currentPaperSourceRawKind;
                markAsPending = _pendingPaperSourceRawKind.HasValue;
            }

            VoidReloadPaperSourcesForPrinter(printerName, rawKindToSelect, markAsPending);
        }

        private void VoidReloadPaperSourcesForPrinter(string printerName, int? rawKindToSelect, bool markAsPendingChange) {

            var options = new ObservableCollection<PaperSourceOption>()
            {
                new PaperSourceOption("(Use printer default)", 0)
            };

            if (!string.IsNullOrWhiteSpace(printerName))
            {
                foreach (var option in PrinterCapabilityUtils.GetPaperSources(printerName))
                {
                    if (!options.Any(existing => existing.RawKind == option.RawKind))
                    {
                        options.Add(option);
                    }
                }
            }

            PaperSourceOption optionToSelect = null;

            if (rawKindToSelect.HasValue)
            {
                optionToSelect = options.FirstOrDefault(o => o.RawKind == rawKindToSelect.Value);
            }

            if (optionToSelect == null && options.Count > 0)
            {
                optionToSelect = options[0];
            }

            _paperSourceOptions = options;
            _selectedPaperSourceOption = optionToSelect;

            if (markAsPendingChange && optionToSelect != null)
            {
                _pendingPaperSourceRawKind = optionToSelect.RawKind;
            }
            else if (_pendingPaperSourceRawKind.HasValue && optionToSelect != null && optionToSelect.RawKind == _currentPaperSourceRawKind)
            {
                _pendingPaperSourceRawKind = null;
            }
            else if (!markAsPendingChange && optionToSelect != null && optionToSelect.RawKind != _currentPaperSourceRawKind)
            {
                _pendingPaperSourceRawKind = optionToSelect.RawKind;
            }

            OnPropertyChanged(nameof(PaperSourceOptions));
            OnPropertyChanged(nameof(SelectedPaperSourceOption));
            CommandManager.InvalidateRequerySuggested();
        }

        private void VoidApplyPaperSourceSettings() {

            if (!_pendingPaperSourceRawKind.HasValue)
            {
                return;
            }

            bApplyPaperSourceSettings_LOCK = true;
            bApplyResultOK_PaperSource = false;

            string strRequestID = RandomUtils.StrGenerateRandomHexadecimalString(8);
            string strFinalMsg = IPCClient.StrMessageEncoder(strRequestID, NamedPipeConstants.JobFlag_WriteRegistryPaperSource, _pendingPaperSourceRawKind.Value.ToString());
            _ = IPCClient.AsyncSendMsgToServerAndDecodeResponse(strRequestID, strFinalMsg, VoidApplyPaperSourceSettings_Callback);
        }

        private void VoidApplyPaperSourceSettings_Callback(int intResult) {

            if (intResult == NamedPipeConstants.intServerResult_SUCCESS && _selectedPaperSourceOption != null)
            {
                _currentPaperSourceRawKind = _selectedPaperSourceOption.RawKind;
                _pendingPaperSourceRawKind = null;
                bApplyResultOK_PaperSource = true;
            }

            bApplyPaperSourceSettings_LOCK = false;
            VoidUpdateText_ApplyResult();
            CommandManager.InvalidateRequerySuggested();
        }

        private void VoidUpdateText_ApplyResult() {

            if (bApplyEnabledSettings_LOCK || bApplyPrinterNameSettings_LOCK || bApplyIntervalSettings_LOCK || bApplyBmpPathSettings_LOCK || bApplyPaperSourceSettings_LOCK)
            {

                StrTextApplyResult = "Sending setting value to the service, please wait...";
            }
            else {
                //all action finished, now check result

                if (bApplyResultOK_Enabled && bApplyResultOK_PrinterName && bApplyResultOK_Interval && bApplyResultOK_BmpPath && bApplyResultOK_PaperSource)
                {
                    //all action success
                    StrTextApplyResult = "All action finished sucessfully.";
                }
                else {
                    //some action went wrong
                    StrTextApplyResult = "Some settings can not be changed.";
                }

                
                Mediator.Notify("UpdateFooter", "");
                //make this text only display for 5 sec
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(5000);
                    StrTextApplyResult = "";
                    
                });
            }
        
        
        
        }

    }   
}
