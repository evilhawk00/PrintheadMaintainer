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
#include <atomic>
#include <thread>

#include "../Common/WinHandle.h"
#include "../Settings/ServiceSettings.h"

enum class PrintState
{
    Idle,
    Countdown, // a scheduled print starts in about a minute
    Printing,
};

enum class ManualPrintRequest
{
    Accepted,
    Busy,          // a manual print is already queued or running
    NotConfigured, // no printer has been selected
};

// Runs every print, scheduled or manual, on one thread so that two jobs never overlap.
// It checks whether a scheduled print is due every 15 minutes, every minute while a due print
// waits for the printer to become ready or for the problem that made it fail to be fixed, and
// immediately when a manual print is requested. All waits end as soon as the service stop
// event is signaled.
class PrintWorker
{
public:
    explicit PrintWorker(HANDLE stopEvent);
    ~PrintWorker();

    PrintWorker(const PrintWorker&) = delete;
    PrintWorker& operator=(const PrintWorker&) = delete;

    bool Start();

    // Waits for the thread to finish; call after signaling the stop event.
    void Join();

    // Thread-safe.
    ManualPrintRequest RequestManualPrint();
    PrintState State() const { return m_state.load(); }
    bool IsManualPrintPending() const { return m_manualPrintPending.load(); }

private:
    struct JobOutcome
    {
        bool interrupted = false; // the service is stopping
        FailureReason failure = FailureReason::None;
        bool skipped = false;     // the settings changed during the countdown; nothing was printed
        bool notReady = false;    // the printer was not ready, so the print was not started at all
        bool idleReport = false;  // with notReady: the printer reported it itself, see Print
        bool waiting = false;     // a scheduled print failed recently and is not tried again yet
    };

    void Run();
    JobOutcome RunPrint(PrintKind kind);
    JobOutcome Print(PrintKind kind);
    JobOutcome WaitForJob(const std::wstring& printerName, DWORD jobId);

    // Returns false if the service started stopping during the wait.
    bool Wait(DWORD milliseconds) const;

    HANDLE m_stopEvent;
    UniqueKernelHandle m_manualPrintRequested;
    std::atomic<PrintState> m_state{ PrintState::Idle };
    std::atomic<bool> m_manualPrintPending{ false };

    // Used by the worker thread only.
    ULONGLONG m_retryTick = 0;                         // after a failed scheduled print, it waits until then
    FailureReason m_lastProblem = FailureReason::None; // what the last scheduled check found, if anything
    std::thread m_thread;
};
