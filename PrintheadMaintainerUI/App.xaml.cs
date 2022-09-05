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
using System.Threading;
using System.Windows;

namespace PrintheadMaintainerUI
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        #pragma warning disable IDE0052
        private static Mutex _mutex = null;
        #pragma warning restore IDE0052
        private EventWaitHandle _eventWaitHandle;

        public bool bStartupMinimized = false;
        protected override void OnStartup(StartupEventArgs e)
        {
            const string strAppName = "PrintheadMaintainer";
            const string strUniqueEventName = "{2C4150D2-F22B-4F1D-97FC-7B68EE70FEA6}";

            bool bCreatedNew;

            _mutex = new Mutex(true, strAppName, out bCreatedNew);
            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, strUniqueEventName);


            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "/silent")
                {
                    bStartupMinimized = true;
                }
            }

            if (bCreatedNew)
            {
                //create a thread to wait for the event
                Thread thread = new Thread(
                    () =>
                    {
                        while (_eventWaitHandle.WaitOne())
                        {
                            _ = Current.Dispatcher.BeginInvoke(
                                (System.Action)(() => ((MainWindow)Current.MainWindow).Void_Public_Restore_Window()));
                        }
                    });

                //Mark it as background otherwise it will prevent app from exiting.
                thread.IsBackground = true;

                thread.Start();
                return;
            }

            //Tell other instance to bring window to front.
            _ = _eventWaitHandle.Set();

            //Terminate this instance.
            Shutdown();

            base.OnStartup(e);
        }

    }
}
