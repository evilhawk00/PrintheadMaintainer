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
#pragma once

#include <Windows.h>
#include <cstdint>
#include <string>
#include <thread>
#include <vector>

class PrintWorker;

// Serves the UI on \\.\pipe\Printhead Maintainer.
//
// - Only SYSTEM, administrators and interactively logged-on users may connect, remote clients
//   are rejected, and nobody else may create instances of the pipe.
// - A single pipe instance is created at startup and reused for every client, so the pipe name
//   never becomes free for another process to take over. Clients are served one at a time.
// - Every step (reading the request, writing the response) has a time limit and ends when the
//   service stops, so a client that stalls cannot block the service.
class PipeServer
{
public:
    PipeServer(HANDLE stopEvent, PrintWorker& worker);
    ~PipeServer();

    PipeServer(const PipeServer&) = delete;
    PipeServer& operator=(const PipeServer&) = delete;

    bool Start();

    // Waits for the thread to finish; call after signaling the stop event.
    void Join();

private:
    void Run();
    void ServeClients(HANDLE pipe);
    void ServeClient(HANDLE pipe);
    bool ReadMessage(HANDLE pipe, std::vector<uint8_t>& message) const;
    bool IsStopping() const;

    HANDLE m_stopEvent;
    PrintWorker& m_worker;
    std::thread m_thread;
};
