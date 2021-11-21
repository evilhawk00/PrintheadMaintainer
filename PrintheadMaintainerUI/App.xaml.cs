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
        private static Mutex _mutex = null;

        public bool bStartupMinimized = false;
        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "PrintheadMaintainer";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "/silent")
                {
                    bStartupMinimized = true;
                }
            }

            

            if (!createdNew)
            {
                //app is already running! Exiting the application  
                Current.Shutdown();
            }

            base.OnStartup(e);
        }

    }
}
