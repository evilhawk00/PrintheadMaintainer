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
using PrintheadMaintainerUI.Interfaces;
using PrintheadMaintainerUI.Models;
using PrintheadMaintainerUI.NamedPipeClient;
using PrintheadMaintainerUI.Printing;
using PrintheadMaintainerUI.Status;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PrintheadMaintainerUI.ViewModels
{
    /// <summary>
    /// Edits the service's settings. Changes are kept here until the user applies them, and then
    /// sent in one request, which the service applies completely or not at all.
    /// </summary>
    public sealed class SettingsViewModel : ViewModelBase
    {
        // The same limits as the service (Settings/ServiceSettings.h), which checks them again.
        private const int MinIntervalDays = 1;
        private const int MaxIntervalDays = 365;
        private const int DefaultIntervalDays = 7;

        private const string NotAvailable = "--";
        private const string DefaultImageName = "Default image";

        private readonly INavigator _navigator;
        private readonly ServiceClient _client;
        private readonly StatusMonitor _monitor;

        // The settings as the service has them; null while they could not be loaded.
        private ServiceStatus _current;

        // Unsaved changes; null means unchanged.
        private string _newPrinterName;
        private ImageSelection _newImage;

        private bool _busy;
        private bool _enabled;

        // The selected paper source. The list only shows it, so the value is right even while
        // the list of paper sources is still being loaded.
        private int _paperSource;
        private int _paperSourcesVersion;
        private string _intervalDays = string.Empty;
        private IReadOnlyList<PaperSourceOption> _paperSources = Array.Empty<PaperSourceOption>();
        private PaperSourceOption _selectedPaperSource;
        private string _message = string.Empty;

        public SettingsViewModel(INavigator navigator, ServiceClient client, StatusMonitor monitor)
        {
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

            BackCommand = new RelayCommand(navigator.ShowHome);
            SelectPrinterCommand = new RelayCommand(() => _navigator.ChoosePrinter(PrinterNameToUse), CanEdit);
            RefreshPaperSourcesCommand = new RelayCommand(LoadPaperSources, CanEdit);
            SelectImageCommand = new RelayCommand(SelectImage, CanEdit);
            UseDefaultImageCommand = new RelayCommand(UseDefaultImage, () => CanEdit() && (_newImage != null ? _newImage != ImageSelection.Default : _current.CustomImage));
            ApplyCommand = new RelayCommand(Apply, () => CanEdit() && HasChanges && IsIntervalValid);
            DiscardCommand = new RelayCommand(() => ShowSettings(_current), () => CanEdit() && HasChanges);
        }

        public ICommand BackCommand { get; }

        public ICommand SelectPrinterCommand { get; }

        public ICommand RefreshPaperSourcesCommand { get; }

        public ICommand SelectImageCommand { get; }

        public ICommand UseDefaultImageCommand { get; }

        public ICommand ApplyCommand { get; }

        public ICommand DiscardCommand { get; }

        /// <summary>False while the settings are loaded or saved, or when they could not be loaded.</summary>
        public bool IsEditable => CanEdit();

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (SetProperty(ref _enabled, value))
                {
                    OnEdited();
                }
            }
        }

        public string PrinterName
        {
            get
            {
                if (_current == null)
                {
                    return NotAvailable;
                }
                string name = PrinterNameToUse;
                return name.Length > 0 ? name : "(Not set)";
            }
        }

        /// <summary>The printing interval in days, as typed.</summary>
        public string IntervalDays
        {
            get => _intervalDays;
            set
            {
                if (SetProperty(ref _intervalDays, value ?? string.Empty))
                {
                    OnEdited();
                }
            }
        }

        public IReadOnlyList<PaperSourceOption> PaperSources
        {
            get => _paperSources;
            private set => SetProperty(ref _paperSources, value);
        }

        public PaperSourceOption SelectedPaperSource
        {
            get => _selectedPaperSource;
            set
            {
                // The list box clears the selection while its items are replaced; keep ours.
                if (value != null && SetProperty(ref _selectedPaperSource, value))
                {
                    _paperSource = value.RawKind;
                    OnEdited();
                }
            }
        }

        public string ImageName
        {
            get
            {
                if (_newImage != null)
                {
                    return _newImage.CustomImage?.SourceName ?? DefaultImageName;
                }
                if (_current == null)
                {
                    return NotAvailable;
                }
                return _current.CustomImage ? _current.ImageSourceName : DefaultImageName;
            }
        }

        public string Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        private string PrinterNameToUse => _newPrinterName ?? _current?.PrinterName ?? string.Empty;

        private bool IsIntervalValid => ParseInterval().HasValue;

        private bool HasChanges =>
            _current != null &&
            (_enabled != _current.Enabled || ParseInterval() != _current.IntervalDays || _newPrinterName != null ||
             _paperSource != _current.PaperSource || _newImage != null);

        /// <summary>Shows the settings as the service has them now, dropping unsaved changes.</summary>
        public async void Load()
        {
            SetBusy(true);
            ShowSettings(null);
            Message = "Loading the settings...";

            ServiceStatus status = await _client.GetStatusAsync();
            SetBusy(false);
            ShowSettings(status);
        }

        /// <summary>Uses the printer the user chose on the printer page.</summary>
        public void SelectPrinter(string printerName)
        {
            if (_current == null)
            {
                return;
            }

            _newPrinterName = printerName == _current.PrinterName ? null : printerName;
            OnPropertyChanged(nameof(PrinterName));

            // Paper sources differ between printers, so a new printer starts with its default.
            _paperSource = _newPrinterName == null ? _current.PaperSource : PrinterCatalog.DefaultPaperSource;
            LoadPaperSources();
            OnEdited();
        }

        private bool CanEdit()
        {
            return !_busy && _current != null;
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            OnPropertyChanged(nameof(IsEditable));
            CommandManager.InvalidateRequerySuggested();
        }

        private void ShowSettings(ServiceStatus status)
        {
            _current = status;
            _newPrinterName = null;
            _newImage = null;
            _enabled = status?.Enabled ?? false;
            _intervalDays = (status?.IntervalDays ?? DefaultIntervalDays).ToString(CultureInfo.InvariantCulture);
            _paperSource = status?.PaperSource ?? PrinterCatalog.DefaultPaperSource;
            OnPropertyChanged(nameof(Enabled));
            OnPropertyChanged(nameof(IntervalDays));
            OnPropertyChanged(nameof(PrinterName));
            OnPropertyChanged(nameof(ImageName));
            OnPropertyChanged(nameof(IsEditable));
            LoadPaperSources();

            Message = status == null && !_busy ? "Cannot connect to the Printhead Maintainer service." : string.Empty;
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnEdited()
        {
            Message = IsIntervalValid
                ? string.Empty
                : "The printing interval must be " + MinIntervalDays + " to " + MaxIntervalDays + " days.";
            CommandManager.InvalidateRequerySuggested();
        }

        private int? ParseInterval()
        {
            bool valid = int.TryParse(_intervalDays.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int days) &&
                days >= MinIntervalDays && days <= MaxIntervalDays;
            return valid ? days : (int?)null;
        }

        // Asks the printer driver for its paper sources in the background, since that can be slow.
        private async void LoadPaperSources()
        {
            int version = ++_paperSourcesVersion;
            ShowPaperSources(new[] { new PaperSourceOption("Loading...", _paperSource) });

            string printerName = _current != null ? PrinterNameToUse : string.Empty;
            IReadOnlyList<PaperSourceOption> sources = await Task.Run(() => PrinterCatalog.GetPaperSources(printerName));
            if (version != _paperSourcesVersion)
            {
                return; // a newer request replaced this one
            }

            // Keep a stored source the driver no longer reports, so that it is not changed by accident.
            if (sources.All(source => source.RawKind != _paperSource))
            {
                sources = sources.Concat(new[] { new PaperSourceOption("Paper source " + _paperSource, _paperSource) }).ToList();
            }
            ShowPaperSources(sources);
        }

        private void ShowPaperSources(IReadOnlyList<PaperSourceOption> sources)
        {
            PaperSources = sources;
            _selectedPaperSource = sources.First(source => source.RawKind == _paperSource);
            OnPropertyChanged(nameof(SelectedPaperSource));
        }

        private async void SelectImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "BMP images (*.bmp)|*.bmp",
                DefaultExt = ".bmp",
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string path = dialog.FileName;
            SetBusy(true);
            Message = "Reading the image...";
            try
            {
                PrintImage image = await Task.Run(() => PrintImageLoader.Load(path));
                _newImage = ImageSelection.Custom(image);
                OnPropertyChanged(nameof(ImageName));
                OnEdited();
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is InvalidDataException)
            {
                Message = "The image could not be read. " + e.Message;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void UseDefaultImage()
        {
            _newImage = _current.CustomImage ? ImageSelection.Default : null;
            OnPropertyChanged(nameof(ImageName));
            OnEdited();
        }

        private async void Apply()
        {
            int intervalDays = ParseInterval().GetValueOrDefault(DefaultIntervalDays);
            var update = new SettingsUpdate
            {
                Enabled = _enabled != _current.Enabled ? _enabled : (bool?)null,
                IntervalDays = intervalDays != _current.IntervalDays ? intervalDays : (int?)null,
                PrinterName = _newPrinterName,
                PaperSource = _paperSource != _current.PaperSource ? _paperSource : (int?)null,
                Image = _newImage,
            };

            SetBusy(true);
            Message = "Saving the settings...";

            ApplySettingsResult result = await _client.ApplySettingsAsync(update);
            if (result != ApplySettingsResult.Applied)
            {
                SetBusy(false);
                Message = Describe(result);
                return;
            }

            // Show what the service stored, and let the rest of the UI catch up.
            _monitor.RefreshNow();
            ServiceStatus status = await _client.GetStatusAsync();
            SetBusy(false);
            ShowSettings(status);
            Message = status != null
                ? "The settings were saved."
                : "The settings were saved, but the service did not answer afterwards.";
        }

        private static string Describe(ApplySettingsResult result)
        {
            switch (result)
            {
                case ApplySettingsResult.InvalidValue:
                    return "The service did not accept one of the values. Nothing was changed.";
                case ApplySettingsResult.PrinterUnavailable:
                    return "The service cannot find this printer. Printers added only for your user account " +
                        "cannot be used by the service; add the printer for all users. Nothing was changed.";
                case ApplySettingsResult.StorageError:
                    return "The service could not save the settings.";
                case ApplySettingsResult.NotConnected:
                    return "Cannot connect to the Printhead Maintainer service. Nothing was changed.";
                default:
                    return "The service could not apply the settings.";
            }
        }
    }
}
