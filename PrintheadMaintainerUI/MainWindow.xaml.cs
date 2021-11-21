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
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;
using PrintheadMaintainerUI.Threads;
using PrintheadMaintainerUI.Mediators;

namespace PrintheadMaintainerUI
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {

        private bool bCloseApplication = false;

        private readonly NotifyIcon niNotifyIcon = new NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();

            bool bStartMinimized = (System.Windows.Application.Current as App).bStartupMinimized;
            WindowState = WindowState.Normal;
            if (bStartMinimized)
            {
                Hide();
            }
            else {
                Show();
            }


            titleBar.MouseLeftButtonDown += (o, e) => DragMove();

            VoidCreateTrayIcon();

            Mediator.Subscribe("TrayOK", Callback_ChangeTrayIcon_OK);
            Mediator.Subscribe("TrayWarning", Callback_ChangeTrayIcon_Warning);
            Mediator.Subscribe("TrayError", Callback_ChangeTrayIcon_Error);
            Mediator.Subscribe("TrayDefault", Callback_ChangeTrayIcon_Default);

            //start service query thread
            _ = ThreadPool.QueueUserWorkItem(delegate
            {
                RealtimeStatusUpdater.VoidSyncLoopRealtimeStatusUpdater();
            }, null);


        }

        private void Callback_ChangeTrayIcon_OK(object obj)
        {
            niNotifyIcon.Icon = Properties.Resources.Icon_Green;
        }

        private void Callback_ChangeTrayIcon_Warning(object obj)
        {
            niNotifyIcon.Icon = Properties.Resources.Icon_Orange;
        }

        private void Callback_ChangeTrayIcon_Error(object obj)
        {
            niNotifyIcon.Icon = Properties.Resources.Icon_Red;
        }
        private void Callback_ChangeTrayIcon_Default(object obj)
        {
            niNotifyIcon.Icon = Properties.Resources.Icon_Blue;
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            //If set cancel to true will cancel the close request, handle here to prevent user close from taskbar
            if (!bCloseApplication)
            {
                e.Cancel = true;
                Hide();

            }
            else {
                
                e.Cancel = false;
            }

            base.OnClosing(e);
        }

        private void VoidCreateTrayIcon()
        {

            ContextMenu contextMenu1;
            MenuItem menuItem1;

            contextMenu1 = new ContextMenu();
            menuItem1 = new MenuItem();

            MenuItem menuItem2 = new MenuItem
            {
                Index = 0,
                Text = "S&how Window"
            };
            menuItem2.Click += new EventHandler(VoidRestoreWindow);

            // Initialize contextMenu1
            contextMenu1.MenuItems.AddRange(new MenuItem[] { menuItem1, menuItem2 });

            // Initialize menuItem1
            menuItem1.Index = 1;
            menuItem1.Text = "E&xit";
            menuItem1.Click += new EventHandler(VoidCloseApplication);
            niNotifyIcon.ContextMenu = contextMenu1;

            niNotifyIcon.Icon = Properties.Resources.Icon_Blue;

            niNotifyIcon.Text = "Printhead Maintainer";


            niNotifyIcon.Visible = true;
            niNotifyIcon.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    //switch back to home first
                    Mediator.Notify("SwitchToHome", "");

                    Show();
                    WindowState = WindowState.Normal;

                };

        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _ = MainWindowGrid.Focus();
        }

        private void VoidCloseApplication(object Sender, EventArgs e)
        {

            bCloseApplication = true;
            //closes the application.
            if (niNotifyIcon != null)
            {

                niNotifyIcon.Icon = null;
                niNotifyIcon.Visible = false;
                niNotifyIcon.Dispose();
            }
            Close();
        }

        private void VoidRestoreWindow(object Sender, EventArgs e)
        {
            //switch back to home first
            Mediator.Notify("SwitchToHome", "");

            Show();
            WindowState = WindowState.Normal;
        }

        private void VoidMinimizeWindowToTray(object sender, RoutedEventArgs e) {

            Hide();
            WindowState = WindowState.Minimized;
            //free preview image view in PrintNowViewModel
            Mediator.Notify("FreeImgPreview", "");
            

        }

        private void VoidMinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;

        }

    }

}
