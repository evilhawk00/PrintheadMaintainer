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
using PrintheadMaintainerUI.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace PrintheadMaintainerUI
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private bool _exiting;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = viewModel;
            titleBar.MouseLeftButtonDown += (sender, e) => DragMove();
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

        /// <summary>
        /// Shows the window with the print page, printing. As when the settings page is left with
        /// Back, its unsaved changes are dropped.
        /// </summary>
        public void PrintNow()
        {
            _viewModel.PrintNow();
            ShowFromTray();
        }

        /// <summary>
        /// Shows the window with the page that postpones the next print. As when the settings page
        /// is left with Back, its unsaved changes are dropped.
        /// </summary>
        public void ShowPostpone()
        {
            _viewModel.ShowPostpone();
            ShowFromTray();
        }

        /// <summary>
        /// Shows the window with the settings page, which shows the settings as the service has
        /// them: unsaved changes are dropped.
        /// </summary>
        public void ShowSettings()
        {
            _viewModel.ShowSettings();
            ShowFromTray();
        }

        /// <summary>Ends the program.</summary>
        public void Exit()
        {
            _exiting = true;
            Application.Current.Shutdown();
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

        private void HideToTray()
        {
            Hide();
            _viewModel.ShowHome(); // leaving the print page also frees its preview image
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
