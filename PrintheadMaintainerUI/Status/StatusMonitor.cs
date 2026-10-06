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
using PrintheadMaintainerUI.Models;
using PrintheadMaintainerUI.NamedPipeClient;
using System;
using System.Threading;

namespace PrintheadMaintainerUI.Status
{
    /// <summary>
    /// Asks the service for its status every few seconds and raises Updated after every answer
    /// (or failure to get one). Start it on the UI thread: the polling loop resumes there after
    /// every request, so Updated is always raised on the UI thread and handlers can update
    /// bindings and the tray icon directly.
    /// </summary>
    public sealed class StatusMonitor
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

        // The UI starts at logon, possibly before the service has finished starting.
        private static readonly TimeSpan StartupGracePeriod = TimeSpan.FromSeconds(30);

        private readonly ServiceClient _client;
        private readonly SemaphoreSlim _refreshRequested = new SemaphoreSlim(0, 1);
        private DateTime _startedUtc;
        private bool _started;

        public StatusMonitor(ServiceClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>Raised on the UI thread after every poll.</summary>
        public event EventHandler Updated;

        public ServiceConnection Connection { get; private set; } = ServiceConnection.Connecting;

        /// <summary>The latest status, or null while the service cannot be reached.</summary>
        public ServiceStatus Status { get; private set; }

        public void Start()
        {
            if (_started)
            {
                return;
            }
            _started = true;
            _startedUtc = DateTime.UtcNow;
            Run();
        }

        /// <summary>Polls again right away, for example after the settings were changed.</summary>
        public void RefreshNow()
        {
            if (_refreshRequested.CurrentCount == 0)
            {
                _refreshRequested.Release();
            }
        }

        // async void on purpose: the loop never ends, and an exception from an Updated handler
        // should surface as an unhandled exception rather than silently stop all status updates.
        private async void Run()
        {
            for (;;)
            {
                ServiceStatus status = await _client.GetStatusAsync();
                Status = status;
                if (status != null)
                {
                    Connection = ServiceConnection.Connected;
                }
                else if (Connection == ServiceConnection.Connected || DateTime.UtcNow - _startedUtc > StartupGracePeriod)
                {
                    Connection = ServiceConnection.Unreachable;
                }
                Updated?.Invoke(this, EventArgs.Empty);

                await _refreshRequested.WaitAsync(PollInterval);
            }
        }
    }
}
