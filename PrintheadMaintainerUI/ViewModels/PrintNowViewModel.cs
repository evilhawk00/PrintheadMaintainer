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
using PrintheadMaintainerUI.Models;
using PrintheadMaintainerUI.NamedPipeClient;
using PrintheadMaintainerUI.Presentation;
using PrintheadMaintainerUI.Status;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PrintheadMaintainerUI.ViewModels
{
    public sealed class PrintNowViewModel : ViewModelBase
    {
        private const int PreviewDecodeWidth = 300;

        private readonly ServiceClient _client;
        private readonly StatusMonitor _monitor;
        private bool _isShown;
        private bool _sending;
        private string _outcome = string.Empty;

        // The status from before a print request the service accepted, used to tell the request's
        // outcome; null when no request is waiting for its outcome.
        private ServiceStatus _statusBeforeRequest;

        private string _previewPath;
        private int _previewVersion;
        private BitmapSource _preview;
        private string _printerName = string.Empty;
        private string _imageName = string.Empty;
        private string _message = string.Empty;

        public PrintNowViewModel(INavigator navigator, ServiceClient client, StatusMonitor monitor)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            BackCommand = new RelayCommand(navigator.ShowHome);
            PrintNowCommand = new RelayCommand(PrintNow, CanPrintNow);
            monitor.Updated += (sender, e) => Update();
        }

        public ICommand BackCommand { get; }

        public ICommand PrintNowCommand { get; }

        public BitmapSource Preview
        {
            get => _preview;
            private set => SetProperty(ref _preview, value);
        }

        public string PrinterName
        {
            get => _printerName;
            private set => SetProperty(ref _printerName, value);
        }

        public string ImageName
        {
            get => _imageName;
            private set => SetProperty(ref _imageName, value);
        }

        public string Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        public void Load()
        {
            _isShown = true;
            if (_statusBeforeRequest == null)
            {
                _outcome = string.Empty;
            }
            _previewPath = null;
            Update();
        }

        /// <summary>Called when another page is shown; frees the preview image.</summary>
        public void Unload()
        {
            _isShown = false;
            _previewPath = null;
            _previewVersion++;
            Preview = null;
        }

        private bool CanPrintNow()
        {
            ServiceStatus status = _monitor.Status;
            return !_sending && _statusBeforeRequest == null && status != null && status.IsPrinterSelected &&
                status.ImageAvailable && !status.ManualPrintPending;
        }

        private async void PrintNow()
        {
            if (!CanPrintNow())
            {
                return;
            }

            ServiceStatus before = _monitor.Status;
            _sending = true;
            _outcome = string.Empty;
            Update();

            PrintNowResult result = await _client.PrintNowAsync();
            _sending = false;
            switch (result)
            {
                case PrintNowResult.Accepted:
                    _statusBeforeRequest = before;
                    _monitor.RefreshNow();
                    break;
                case PrintNowResult.Busy:
                    _outcome = "A print is already in progress.";
                    break;
                case PrintNowResult.NotConfigured:
                    _outcome = "Select a printer in Settings first.";
                    break;
                case PrintNowResult.NotConnected:
                    _outcome = "Cannot connect to the Printhead Maintainer service.";
                    break;
                default:
                    _outcome = "The service could not start printing.";
                    break;
            }
            Update();
        }

        private void Update()
        {
            ServiceStatus status = _monitor.Status;
            if (status == null)
            {
                PrinterName = "Not connected to the service";
                ImageName = string.Empty;
            }
            else
            {
                PrinterName = status.IsPrinterSelected ? status.PrinterName : "No printer selected";
                ImageName = status.CustomImage ? status.ImageSourceName : "Default image";

                if (_statusBeforeRequest != null && !status.ManualPrintPending)
                {
                    _outcome = DescribeOutcome(_statusBeforeRequest, status);
                    _statusBeforeRequest = null;
                }
                if (_isShown && _previewPath == null && status.ImageAvailable)
                {
                    LoadPreview(status.ImagePath);
                }
            }

            if (_sending)
            {
                Message = "Sending the print request...";
            }
            else if (_statusBeforeRequest != null)
            {
                Message = "Printing... This can take a few minutes.";
            }
            else
            {
                Message = _outcome;
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private static string DescribeOutcome(ServiceStatus before, ServiceStatus after)
        {
            if (after.LastPrintUtc.HasValue && after.LastPrintUtc != before.LastPrintUtc)
            {
                return "Printed successfully at " + DisplayText.FormatTime(after.LastPrintUtc.Value) + ".";
            }

            PrintFailure failure = after.LastManualFailure;
            if (failure != null && (before.LastManualFailure == null || failure.TimeUtc > before.LastManualFailure.TimeUtc))
            {
                return "Printing failed. " + DisplayText.Describe(failure.Reason) + ".";
            }
            return string.Empty;
        }

        private async void LoadPreview(string path)
        {
            _previewPath = path;
            int version = ++_previewVersion;
            BitmapSource preview = await Task.Run(() => DecodePreview(path));
            if (version == _previewVersion)
            {
                Preview = preview;
            }
        }

        private static BitmapSource DecodePreview(string path)
        {
            try
            {
                // The service may replace the file at any time, so it is opened with every sharing
                // mode and read completely right away.
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    image.DecodePixelWidth = PreviewDecodeWidth;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException ||
                e is FormatException || e is ArgumentException || e is OverflowException ||
                e is ExternalException)
            {
                return null; // no preview; printing does not depend on it
            }
        }
    }
}
