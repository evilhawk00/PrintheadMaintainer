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
using PrintheadMaintainerUI.Printing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PrintheadMaintainerUI.ViewModels
{
    public sealed class ChoosePrinterViewModel : ViewModelBase
    {
        private bool _loading;
        private int _loadVersion;
        private IReadOnlyList<string> _printers = Array.Empty<string>();
        private string _selectedPrinter;
        private string _message = string.Empty;

        public ChoosePrinterViewModel(INavigator navigator)
        {
            BackCommand = new RelayCommand(() => navigator.ReturnToSettings(null));
            SelectCommand = new RelayCommand(() => navigator.ReturnToSettings(SelectedPrinter), () => SelectedPrinter != null);
            RefreshCommand = new RelayCommand(() => Load(SelectedPrinter), () => !_loading);
        }

        public ICommand BackCommand { get; }

        public ICommand SelectCommand { get; }

        public ICommand RefreshCommand { get; }

        public IReadOnlyList<string> Printers
        {
            get => _printers;
            private set => SetProperty(ref _printers, value);
        }

        public string SelectedPrinter
        {
            get => _selectedPrinter;
            set => SetProperty(ref _selectedPrinter, value);
        }

        public string Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        public async void Load(string printerToSelect)
        {
            int version = ++_loadVersion;
            _loading = true;
            Message = "Looking for printers...";
            CommandManager.InvalidateRequerySuggested();

            IReadOnlyList<string> printers;
            string error = null;
            try
            {
                printers = await Task.Run(() => PrinterCatalog.GetInstalledPrinters());
            }
            catch (Win32Exception e)
            {
                printers = Array.Empty<string>();
                error = "The printers could not be listed. " + e.Message;
            }
            if (version != _loadVersion)
            {
                return; // a newer request replaced this one
            }

            _loading = false;
            Printers = printers;
            SelectedPrinter = printers.Contains(printerToSelect) ? printerToSelect : null;
            Message = error ?? (printers.Count == 0 ? "No printers are installed." : string.Empty);
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
