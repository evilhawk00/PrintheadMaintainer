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
using PrintheadMaintainerUI.NamedPipeClient;
using PrintheadMaintainerUI.Utils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PrintheadMaintainerUI.ViewModels
{
    public class PrintNowViewModel : ViewModelBase, IPageViewModel
    {
        private ICommand _switchToHomeView;


        private string _strSettingsImagePath;
        private string _strPrinterName;

        private BitmapImage _bmPreviewBitmap;

        private string _strSendPrintCommandResult;
        private bool bPrinterValueAvailable = false;
        private bool bBmpValueAvailable = false;

        private Stream sStream;

        //LOCKS
        private bool bSendPrintCommand_LOCK = false;

        
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

        public ICommand CmdPrintNow
        {

            get
            {
                //send value to server
                return new RelayCommand(x =>
                {
                    VoidOnClickPrintNowBtn();
                }, x =>
                {
                    return BIsEnabled_BtnPrintNow();
                });
            }

        }

        private bool BIsEnabled_BtnPrintNow()
        {
            if (!bSendPrintCommand_LOCK)
            {
                //lock is not locked
                if (bPrinterValueAvailable && bBmpValueAvailable)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else {
                //locked
                return false;    
            }
        }

        public BitmapImage BmPreviewBitmap 
        {   
            get { return _bmPreviewBitmap; 
            } 
            set { 
                _bmPreviewBitmap = value; 
                OnPropertyChanged(nameof(BmPreviewBitmap)); 
            } 
        }

        public string StrPrinterName
        {
            get {
                return _strPrinterName;
            }
            set {
                _strPrinterName = value;
                OnPropertyChanged(nameof(StrPrinterName));
            }
            
        }

        public string StrBmpPath
        {
            get {
                return _strSettingsImagePath;
            }
            set {
                _strSettingsImagePath = value;
                OnPropertyChanged(nameof(StrBmpPath));
            }
        }

        public string StrSendPrintCommandResult
        {
            get
            {
                return _strSendPrintCommandResult;
            }
            set
            {
                _strSendPrintCommandResult = value;
                OnPropertyChanged(nameof(StrSendPrintCommandResult));
            }
        
        }

        public PrintNowViewModel() {

            VoidUpdateUI_PreviewAndPrinterAndImage();
            Mediator.Subscribe("FreeImgPreview", Callback_FreeImageView);

        }

        public void VoidRefreshView()
        {

            VoidUpdateUI_PreviewAndPrinterAndImage();

        }

        private void VoidUpdateUI_PreviewAndPrinterAndImage()
        {

            RegistryKey rkHandle = RegistryUtils.RkOpenRegistyHandle(false);
            if (rkHandle == null)
            {
                //at this time may be the first start so the registry is still not created
                StrBmpPath = "Target image hasn't been appointed";
                StrPrinterName = "Target Printer hasn't been appointed";
                return;
            }

            string strResultBmpPath = RegistryUtils.StrRegistryReadSingleLineString(rkHandle, RegistryConstants.strRegistryValue_BmpPath);
            if (strResultBmpPath != null && !strResultBmpPath.Equals(""))
            {
                StrBmpPath = strResultBmpPath;
                bBmpValueAvailable = true;

                try
                {
                    DisposeMediaStream();

                    var bitmap = new BitmapImage();
                    sStream = File.OpenRead(StrBmpPath);

                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.None;
                    bitmap.StreamSource = sStream;
                    bitmap.EndInit();

                    bitmap.Freeze();
                    BmPreviewBitmap = bitmap;

                }
                catch (Exception)
                {

                }
            }
            else
            {
                StrBmpPath = "Target image hasn't been appointed";
                
            }

            string strResultPrinterName = RegistryUtils.StrRegistryReadSingleLineString(rkHandle, RegistryConstants.strRegistryValue_PrinterName);
            if (strResultPrinterName != null && !strResultPrinterName.Equals(""))
            {
                StrPrinterName = strResultPrinterName;
                bPrinterValueAvailable = true;
                
            }
            else
            {
                StrPrinterName = "Target Printer hasn't been appointed";
                
            }
        }

        private void VoidOnClickPrintNowBtn() {

            bSendPrintCommand_LOCK = true;

            StrSendPrintCommandResult = "Sending printing command to the service, please wait...";
            //encode msg
            string strRequestID = RandomUtils.StrGenerateRandomHexadecimalString(8);
            string strFinalMsg = IPCClient.StrMessageEncoder(strRequestID, NamedPipeConstants.JobFlag_PrintNow, null);
            _ = IPCClient.AsyncSendMsgToServerAndDecodeResponse(strRequestID, strFinalMsg, VoidOnClickPrintNowBtn_Callback);
        }

        private void VoidOnClickPrintNowBtn_Callback(int intResult)
        {

            if (intResult == NamedPipeConstants.intServerResult_SUCCESS)
            {
                //UWPNotificationUtils.VoidShowUWPNotification("yesyesyes", "!!!");
                StrSendPrintCommandResult = "Printing command sent to printer successfully.";
            }
            else {
                //UWPNotificationUtils.VoidShowUWPNotification("nonono", "nonono");
                StrSendPrintCommandResult = "Failed to send printing command to printer";
            }
            
            bSendPrintCommand_LOCK = false;
            //now its unlocked, we re-evaluate canexecuted again
            CommandManager.InvalidateRequerySuggested();


            //make this text only display for 5 sec
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(5000);
                StrSendPrintCommandResult = "";

            });
            
            
        }

        private void DisposeMediaStream()
        {
            if (sStream != null)
            {
                sStream.Close();
                sStream.Dispose();
                sStream = null;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            }
        }

        private void Callback_FreeImageView(object obj)
        {
            BmPreviewBitmap = null;
            DisposeMediaStream();
        }


    }
}
