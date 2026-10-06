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
using PrintheadMaintainerUI.Enums;
using PrintheadMaintainerUI.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Forms = System.Windows.Forms;

namespace PrintheadMaintainerUI
{
    public partial class MainWindow : Window
    {
        // Windows does not accept longer tooltips for notification area icons.
        private const int MaxTrayTextLength = 63;

        private readonly MainViewModel _viewModel;
        private readonly Forms.NotifyIcon _trayIcon;
        private bool _exiting;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = viewModel;
            titleBar.MouseLeftButtonDown += (sender, e) => DragMove();

            _trayIcon = CreateTrayIcon();
            UpdateTrayIcon();
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        /// <summary>
        /// Shows the window and brings it to the front. A hidden window already shows the home page;
        /// a visible one keeps its page, so that unsaved settings are not lost.
        /// </summary>
        public void ShowFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        /// <summary>Removes the notification area icon; called when the program ends.</summary>
        public void RemoveTrayIcon()
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Closing the window, for example from the taskbar, only hides it.
            if (!_exiting)
            {
                e.Cancel = true;
                HideToTray();
            }
            base.OnClosing(e);
        }

        private Forms.NotifyIcon CreateTrayIcon()
        {
            var menu = new Forms.ContextMenu(new[]
            {
                new Forms.MenuItem("S&how Window", (sender, e) => ShowFromTray()),
                new Forms.MenuItem("E&xit", (sender, e) => Exit()),
            });
            var trayIcon = new Forms.NotifyIcon { ContextMenu = menu, Visible = true };
            trayIcon.DoubleClick += (sender, e) => ShowFromTray();
            return trayIcon;
        }

        private void Exit()
        {
            _exiting = true;
            Application.Current.Shutdown();
        }

        private void HideToTray()
        {
            Hide();
            _viewModel.ShowHome(); // leaving the print page also frees its preview image
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.State) || e.PropertyName == nameof(MainViewModel.StatusTitle))
            {
                UpdateTrayIcon();
            }
        }

        private void UpdateTrayIcon()
        {
            System.Drawing.Icon previous = _trayIcon.Icon;
            switch (_viewModel.State)
            {
                case ServiceState.OK:
                    _trayIcon.Icon = Properties.Resources.Icon_Green;
                    break;
                case ServiceState.Warning:
                    _trayIcon.Icon = Properties.Resources.Icon_Orange;
                    break;
                case ServiceState.Error:
                    _trayIcon.Icon = Properties.Resources.Icon_Red;
                    break;
                default:
                    _trayIcon.Icon = Properties.Resources.Icon_Blue;
                    break;
            }
            previous?.Dispose(); // every read of a resource creates a new icon

            string text = "Printhead Maintainer - " + _viewModel.StatusTitle;
            _trayIcon.Text = text.Length > MaxTrayTextLength ? text.Substring(0, MaxTrayTextLength) : text;
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindowGrid.Focus();
        }

        private void HideToTray_Click(object sender, RoutedEventArgs e)
        {
            HideToTray();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
