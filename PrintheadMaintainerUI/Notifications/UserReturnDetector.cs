/*
*
* Copyright (C) 2026  YAN-LIN, CHEN
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
using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace PrintheadMaintainerUI.Notifications
{
    /// <summary>
    /// Raises UserReturned when someone starts using the computer again: when the session is
    /// unlocked or connected to, or on the first input after several minutes without any. It only
    /// watches while enabled. Create and use it on the UI thread; events are raised there.
    /// </summary>
    public sealed class UserReturnDetector : IDisposable
    {
        private static readonly TimeSpan AwayAfter = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(2);

        // Unlocking also counts as input; report one return, not two.
        private static readonly TimeSpan MinimumTimeBetweenReturns = TimeSpan.FromSeconds(30);

        private readonly DispatcherTimer _idleTimer;
        private bool _enabled;
        private bool _away;
        private DateTime _lastReturnUtc = DateTime.MinValue;

        public UserReturnDetector()
        {
            _idleTimer = new DispatcherTimer { Interval = CheckInterval };
            _idleTimer.Tick += OnIdleTimerTick;
            SystemEvents.SessionSwitch += OnSessionSwitch;
        }

        public event EventHandler UserReturned;

        public bool IsEnabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    _away = false;
                    _idleTimer.IsEnabled = value;
                }
            }
        }

        public void Dispose()
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            _idleTimer.Stop();
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionUnlock:
                case SessionSwitchReason.SessionLogon:
                case SessionSwitchReason.ConsoleConnect:
                case SessionSwitchReason.RemoteConnect:
                    OnReturned();
                    break;
            }
        }

        private void OnIdleTimerTick(object sender, EventArgs e)
        {
            var info = new LastInputInfo { Size = (uint)Marshal.SizeOf(typeof(LastInputInfo)) };
            if (!GetLastInputInfo(ref info))
            {
                return;
            }

            // Both are GetTickCount values, so the difference is right even after they wrap around.
            uint idleMilliseconds = unchecked((uint)Environment.TickCount - info.Time);
            if (idleMilliseconds >= AwayAfter.TotalMilliseconds)
            {
                _away = true;
            }
            else if (_away)
            {
                OnReturned();
            }
        }

        private void OnReturned()
        {
            _away = false;
            DateTime now = DateTime.UtcNow;
            if (!_enabled || now - _lastReturnUtc < MinimumTimeBetweenReturns)
            {
                return;
            }
            _lastReturnUtc = now;
            UserReturned?.Invoke(this, EventArgs.Empty);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            public uint Size;
            public uint Time;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetLastInputInfo(ref LastInputInfo info);
    }
}
