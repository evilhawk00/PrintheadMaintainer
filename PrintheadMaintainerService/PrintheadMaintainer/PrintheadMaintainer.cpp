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
#include <Windows.h>
#include <mutex>
#include <string>

#include "Common/WinHandle.h"
#include "Ipc/PipeServer.h"
#include "Logging/ServiceLog.h"
#include "Printing/PrintWorker.h"

namespace
{
    constexpr wchar_t kServiceName[] = L"PrintheadMaintenanceSvc";
    constexpr DWORD kStartWaitHintMs = 5 * 1000;
    constexpr DWORD kStopWaitHintMs = 30 * 1000;

    SERVICE_STATUS_HANDLE g_statusHandle = nullptr;
    std::mutex g_statusMutex;
    DWORD g_checkPoint = 1;

    // Lives until the process exits, so the control handler can always signal it.
    UniqueKernelHandle g_stopEvent;

    // Called from the service thread and from the control handler thread.
    void ReportStatus(DWORD state, DWORD exitCode = NO_ERROR, DWORD waitHint = 0)
    {
        std::lock_guard<std::mutex> lock(g_statusMutex);

        SERVICE_STATUS status{};
        status.dwServiceType = SERVICE_WIN32_OWN_PROCESS;
        status.dwCurrentState = state;
        status.dwControlsAccepted = state == SERVICE_RUNNING ? SERVICE_ACCEPT_STOP | SERVICE_ACCEPT_SHUTDOWN : 0;
        status.dwWin32ExitCode = exitCode;
        status.dwWaitHint = waitHint;
        status.dwCheckPoint = state == SERVICE_RUNNING || state == SERVICE_STOPPED ? 0 : g_checkPoint++;
        ::SetServiceStatus(g_statusHandle, &status);
    }

    DWORD WINAPI ControlHandler(DWORD control, DWORD, LPVOID, LPVOID)
    {
        switch (control)
        {
        case SERVICE_CONTROL_STOP:
        case SERVICE_CONTROL_SHUTDOWN:
            ReportStatus(SERVICE_STOP_PENDING, NO_ERROR, kStopWaitHintMs);
            ::SetEvent(g_stopEvent.Get());
            return NO_ERROR;
        case SERVICE_CONTROL_INTERROGATE:
            return NO_ERROR;
        default:
            return ERROR_CALL_NOT_IMPLEMENTED;
        }
    }

    void WINAPI ServiceMain(DWORD, LPWSTR*)
    {
        g_statusHandle = ::RegisterServiceCtrlHandlerExW(kServiceName, ControlHandler, nullptr);
        if (g_statusHandle == nullptr)
        {
            return;
        }
        ReportStatus(SERVICE_START_PENDING, NO_ERROR, kStartWaitHintMs);

        g_stopEvent.Reset(::CreateEventW(nullptr, TRUE, FALSE, nullptr));
        if (!g_stopEvent)
        {
            ReportStatus(SERVICE_STOPPED, ::GetLastError());
            return;
        }

        ServiceLog::Write(L"Service started, process " + std::to_wstring(::GetCurrentProcessId()));

        DWORD exitCode = NO_ERROR;
        {
            PrintWorker worker(g_stopEvent.Get());
            PipeServer pipeServer(g_stopEvent.Get(), worker);
            if (worker.Start() && pipeServer.Start())
            {
                ReportStatus(SERVICE_RUNNING);
            }
            else
            {
                exitCode = ERROR_SERVICE_NO_THREAD;
                ServiceLog::Write(L"Service could not start its threads");
                ::SetEvent(g_stopEvent.Get());
            }

            ::WaitForSingleObject(g_stopEvent.Get(), INFINITE);

            // Both threads end as soon as they notice the stop event; a print in progress is
            // cancelled and its job removed from the queue.
            pipeServer.Join();
            worker.Join();
        }

        ServiceLog::Write(L"Service stopped");
        ReportStatus(SERVICE_STOPPED, exitCode);
    }
}

int main()
{
    wchar_t serviceName[] = L"PrintheadMaintenanceSvc";
    const SERVICE_TABLE_ENTRYW dispatchTable[] = {
        { serviceName, ServiceMain },
        { nullptr, nullptr },
    };
    return ::StartServiceCtrlDispatcherW(dispatchTable) ? 0 : static_cast<int>(::GetLastError());
}
