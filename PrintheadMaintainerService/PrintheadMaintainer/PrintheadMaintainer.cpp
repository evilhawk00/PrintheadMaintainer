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
#include <iostream>
#include <Windows.h>
#include <process.h>

#include "SoftwareSettings/SoftwareSettings.h"
#include "PrintingEntryPoint/PrintingEntryPoint.h"
#include "NamedPipeServer/IPCServer.h"
#include "Logging/Logging.h"
#include "Utils/PrinterUtils.h"
#include "Utils/RegistryUtils.h"
#include "Utils/DateTimeUtils.h"

#define SVCNAME L"PrintheadMaintenanceSvc"

SERVICE_STATUS          gSvcStatus;
SERVICE_STATUS_HANDLE   gSvcStatusHandle;
HANDLE                  ghSvcStopEvent = NULL;

VOID WINAPI SvcCtrlHandler(DWORD);
VOID WINAPI SvcMain(DWORD, LPTSTR*);

VOID ReportSvcStatus(DWORD, DWORD, DWORD);
VOID SvcInit(DWORD, LPTSTR*);
VOID SvcReportEvent(LPTSTR);

unsigned __stdcall ServiceWorkerThread(void* pArguments);
unsigned __stdcall NamedPipeListenerThread(void* pArguments);

int main()
{
    wchar_t svc_buf[] = SVCNAME;

    SERVICE_TABLE_ENTRY DispatchTable[] =
    {
        { svc_buf, (LPSERVICE_MAIN_FUNCTION)SvcMain },
        { NULL, NULL }
    };

    if (!StartServiceCtrlDispatcher(DispatchTable))
    {
        return -1;
    }

    return 0;
}

VOID WINAPI SvcMain(DWORD dwArgc, LPTSTR* lpszArgv)
{
    // Register the handler function for the service

    gSvcStatusHandle = RegisterServiceCtrlHandler(SVCNAME,SvcCtrlHandler);

    if (gSvcStatusHandle == 0)
    {
        //register service control handler failed, abort
        return;
    }

    // These SERVICE_STATUS members remain as set here

    gSvcStatus.dwServiceType = SERVICE_WIN32_OWN_PROCESS;
    gSvcStatus.dwServiceSpecificExitCode = 0;

    // Report initial status to the SCM

    ReportSvcStatus(SERVICE_START_PENDING, NO_ERROR, 8000);

    // Perform service-specific initialization and work.

    SvcInit(dwArgc, lpszArgv);
}

VOID SvcInit(DWORD dwArgc, LPTSTR* lpszArgv)
{
    ghSvcStopEvent = CreateEvent(
        NULL,    // default security attributes
        TRUE,    // manual reset event
        FALSE,   // not signaled
        NULL);   // no name

    if (ghSvcStopEvent == NULL)
    {
        ReportSvcStatus(SERVICE_STOPPED, GetLastError(), 0);
        return;
    }

    // Report running status when initialization is complete.

    ReportSvcStatus(SERVICE_RUNNING, NO_ERROR, 0);

    // TO_DO: Perform work until service stops.
    std::wstring wsDummy1;
    Logging::intWriteLogToLogFile_ThreadSafe(DEF_intLogType_ServiceStart, wsDummy1);

    HANDLE hThread = (HANDLE)_beginthreadex(NULL, 0, &ServiceWorkerThread, NULL, 0, NULL);
    HANDLE hThread2 = (HANDLE)_beginthreadex(NULL, 0, &NamedPipeListenerThread, NULL, 0, NULL);

    

    // Check whether to stop the service.
    WaitForSingleObject(hThread, INFINITE);

    CancelSynchronousIo(hThread2);
    //WaitForSingleObject(hThread2, INFINITE);

    CloseHandle(ghSvcStopEvent);

    std::wstring wsDummy2;
    Logging::intWriteLogToLogFile_ThreadSafe(DEF_intLogType_ServiceStop, wsDummy2);

    ReportSvcStatus(SERVICE_STOPPED, NO_ERROR, 0);
    return;
    
}

unsigned __stdcall ServiceWorkerThread(void* pArguments)
{

    int intWaitCounter = 0; //Execute MainExecCode Every 15 minutes

    //first start wait 15 sec
    Sleep(15000);
    //first try printing
    intStartPrintingJob(true, true);

    //now go to waiting loop
    while (WaitForSingleObject(ghSvcStopEvent, 0) != WAIT_OBJECT_0)
    {
        if (intWaitCounter <= 180) {
            Sleep(5000);
            intWaitCounter++;
            //Always wait 15 minute, we try to print every 15 minutes
        }
        else {
        
            intWaitCounter = 0; //reset counter
            intStartPrintingJob(true,true);
        }
        

        
    }

    //_endthreadex(0);
    return 0;
}

unsigned __stdcall NamedPipeListenerThread(void* pArguments) {

    NamedPipeListener();
    return 0;
}

VOID ReportSvcStatus(DWORD dwCurrentState, DWORD dwWin32ExitCode, DWORD dwWaitHint)
{
    static DWORD dwCheckPoint = 1;

    gSvcStatus.dwCurrentState = dwCurrentState;
    gSvcStatus.dwWin32ExitCode = dwWin32ExitCode;
    gSvcStatus.dwWaitHint = dwWaitHint;

    if (dwCurrentState == SERVICE_START_PENDING) {
        gSvcStatus.dwControlsAccepted = 0;
    }
    else {
        gSvcStatus.dwControlsAccepted = SERVICE_ACCEPT_STOP;
    }

    if ((dwCurrentState == SERVICE_RUNNING) || (dwCurrentState == SERVICE_STOPPED)) {
        gSvcStatus.dwCheckPoint = 0;
    }
    else {
        gSvcStatus.dwCheckPoint = dwCheckPoint++;
    }
    

    // Report the status of the service to the SCM.
    SetServiceStatus(gSvcStatusHandle, &gSvcStatus);
}

VOID WINAPI SvcCtrlHandler(DWORD dwCtrl)
{
    // Handle the requested control code. 

    switch (dwCtrl)
    {
    case SERVICE_CONTROL_STOP:
        ReportSvcStatus(SERVICE_STOP_PENDING, NO_ERROR, 0);

        // Signal the service to stop.

        SetEvent(ghSvcStopEvent);
        ReportSvcStatus(gSvcStatus.dwCurrentState, NO_ERROR, 0);

        return;

    case SERVICE_CONTROL_INTERROGATE:
        break;

    default:
        break;
    }

}

