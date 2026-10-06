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
using PrintheadMaintainerUI.NamedPipeClient;
using PrintheadMaintainerUI.Notifications;
using PrintheadMaintainerUI.Status;
using PrintheadMaintainerUI.ViewModels;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace PrintheadMaintainerUI
{
    public partial class App : Application
    {
        // One instance per user session. Starting the program again shows the running instance.
        private const string InstanceMutexName = "PrintheadMaintainer";
        private const string ShowWindowEventName = "{2C4150D2-F22B-4F1D-97FC-7B68EE70FEA6}";

        // Passed by the autostart entry: start in the notification area without showing the window.
        private const string SilentArgument = "/silent";

        private Mutex _instanceMutex;
        private EventWaitHandle _showWindowEvent;
        private RegisteredWaitHandle _showWindowWait;
        private StatusNotifier _notifier;
        private MainWindow _window;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _instanceMutex = new Mutex(false, InstanceMutexName, out bool firstInstance);
            if (!firstInstance)
            {
                ShowRunningInstance();
                Shutdown();
                return;
            }

            // Created right away, so that a second start while this one is still starting is not
            // lost: the event stays set until the wait below is registered.
            _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowWindowEventName);

            // The window only hides when closed; the program ends from the tray icon menu.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var client = new ServiceClient();
            var monitor = new StatusMonitor(client);
            var actions = new ScheduleActions(client, monitor);
            _window = new MainWindow(new MainViewModel(client, monitor, actions));
            MainWindow = _window;
            _notifier = new StatusNotifier(monitor);
            _notifier.OpenRequested += (sender, args) => _window.ShowFromTray();

            _showWindowWait = ThreadPool.RegisterWaitForSingleObject(_showWindowEvent,
                (state, timedOut) => Dispatcher.BeginInvoke(new Action(() => _window.ShowFromTray())),
                null, Timeout.Infinite, false);

            if (!e.Args.Contains(SilentArgument))
            {
                _window.ShowFromTray();
            }
            monitor.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _showWindowWait?.Unregister(null);
            _showWindowEvent?.Dispose();
            _notifier?.Dispose();
            _window?.RemoveTrayIcon();
            _instanceMutex?.Dispose();
            base.OnExit(e);
        }

        private static void ShowRunningInstance()
        {
            if (EventWaitHandle.TryOpenExisting(ShowWindowEventName, out EventWaitHandle showWindowEvent))
            {
                using (showWindowEvent)
                {
                    // This process was started by the user, so it may hand the foreground to the
                    // running instance; otherwise its window could not come to the front.
                    AllowSetForegroundWindow(AnyProcess);
                    showWindowEvent.Set();
                }
            }
        }

        private const int AnyProcess = -1;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllowSetForegroundWindow(int processId);
    }
}
