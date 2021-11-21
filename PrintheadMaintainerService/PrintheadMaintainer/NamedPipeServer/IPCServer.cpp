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
#include <string>
#include <iostream>
#include <windows.h> 
#include <stdio.h> 
#include <tchar.h>
#include <strsafe.h>

#include "IPCProcessingJob.h"

#define BUFSIZE 512

#define DEF_NAMEDPIPE_NAME L"\\\\.\\pipe\\Printhead Maintainer"

DWORD WINAPI InstanceThread(LPVOID);
VOID GetAnswerToRequest(wchar_t* pchRequest, LPTSTR, LPDWORD);

int NamedPipeListener() {

    BOOL   fConnected = FALSE;
    DWORD  dwThreadId = 0;
    HANDLE hPipe = INVALID_HANDLE_VALUE, hThread = NULL;
    LPCTSTR lpszPipename = DEF_NAMEDPIPE_NAME;

    //security
    SECURITY_DESCRIPTOR sd;
    InitializeSecurityDescriptor(&sd, SECURITY_DESCRIPTOR_REVISION);
    SetSecurityDescriptorDacl(&sd, TRUE, NULL, FALSE);

    SECURITY_ATTRIBUTES sa = { 0 };
    sa.nLength = sizeof(SECURITY_ATTRIBUTES);
    sa.lpSecurityDescriptor = &sd;
    sa.bInheritHandle = FALSE;


    while (true)
    {

        hPipe = CreateNamedPipe(
            lpszPipename,             // pipe name 
            PIPE_ACCESS_DUPLEX,       // read/write access 
            PIPE_TYPE_MESSAGE |       // message type pipe 
            PIPE_READMODE_MESSAGE |   // message-read mode 
            PIPE_WAIT,                // blocking mode 
            PIPE_UNLIMITED_INSTANCES, // max. instances  
            BUFSIZE,                  // output buffer size 
            BUFSIZE,                  // input buffer size 
            0,                        // client time-out 
            &sa);                    // default security attribute 

        if (hPipe == INVALID_HANDLE_VALUE)
        {
            
            return -1;
        }

        int intResultConnect = ConnectNamedPipe(hPipe, NULL);

        if (intResultConnect != 0) {
            fConnected = true;
        }
        else {

            if (GetLastError() == ERROR_PIPE_CONNECTED) {
                fConnected = true;
            }
            else {
                fConnected = false;
            }

        }



        if (fConnected)
        {
            // Create a thread for this client. 
            hThread = CreateThread(
                NULL,              // no security attribute 
                0,                 // default stack size 
                InstanceThread,    // thread proc
                (LPVOID)hPipe,    // thread parameter 
                0,                 // not suspended 
                &dwThreadId);      // returns thread ID 

            if (hThread == NULL)
            {
                
                return -1;
            }
            else CloseHandle(hThread);
        }
        else {
            // The client could not connect, so close the pipe. 
            CloseHandle(hPipe);

        }

    }

    return 0;
}

DWORD WINAPI InstanceThread(LPVOID lpvParam)
{
    HANDLE hHeap = GetProcessHeap();
    wchar_t* pchRequest = (wchar_t*)HeapAlloc(hHeap, 0, BUFSIZE * sizeof(wchar_t));
    wchar_t* pchReply = (wchar_t*)HeapAlloc(hHeap, 0, BUFSIZE * sizeof(wchar_t));

    DWORD cbBytesRead = 0, cbReplyBytes = 0, cbWritten = 0;
    BOOL fSuccess = FALSE;
    HANDLE hPipe = NULL;

    if (lpvParam == NULL)
    {
        if (pchReply != NULL) HeapFree(hHeap, 0, pchReply);
        if (pchRequest != NULL) HeapFree(hHeap, 0, pchRequest);
        return (DWORD)-1;
    }

    if (pchRequest == NULL)
    {
        if (pchReply != NULL) HeapFree(hHeap, 0, pchReply);
        return (DWORD)-1;
    }

    if (pchReply == NULL)
    {
        if (pchRequest != NULL) HeapFree(hHeap, 0, pchRequest);
        return (DWORD)-1;
    }

    //zero memory so there's no remains of the previous request
    memset(pchRequest, 0, BUFSIZE * sizeof(wchar_t));
    memset(pchReply, 0, BUFSIZE * sizeof(wchar_t));

    hPipe = (HANDLE)lpvParam;

    // Loop until done reading
    while (1)
    {
        fSuccess = ReadFile(
            hPipe,        // handle to pipe 
            pchRequest,    // buffer to receive data 
            BUFSIZE * sizeof(wchar_t), // size of buffer 
            &cbBytesRead, // number of bytes read 
            NULL);        // not overlapped I/O 

        if (!fSuccess || cbBytesRead == 0)
        {
            if (GetLastError() == ERROR_BROKEN_PIPE)
            {
                //client disconnected
            }
            else
            {
                //InstanceThread ReadFile failed
            }
            break;
        }

        // Process the incoming message.
        GetAnswerToRequest(pchRequest, pchReply, &cbReplyBytes);

        // Write the reply to the pipe. 
        fSuccess = WriteFile(
            hPipe,        // handle to pipe 
            pchReply,     // buffer to write from 
            cbReplyBytes, // number of bytes to write 
            &cbWritten,   // number of bytes written 
            NULL);        // not overlapped I/O 

        if (!fSuccess || cbReplyBytes != cbWritten)
        {
            //InstanceThread WriteFile failed
            break;
        }
    }

    FlushFileBuffers(hPipe);
    DisconnectNamedPipe(hPipe);
    CloseHandle(hPipe);

    HeapFree(hHeap, 0, pchRequest);
    HeapFree(hHeap, 0, pchReply);

    return 1;
}

VOID GetAnswerToRequest(wchar_t* pchRequest, LPTSTR pchReply, LPDWORD pchBytes)
{
    std::wstring wsRequestMessage(pchRequest);
    
    std::wstring wsBufferRequestID;
    int intResultProcessing = intProcessClientRequestByMsg(wsRequestMessage, wsBufferRequestID);

    std::wstring wsBufferResponseMsg;
    int intResultResponse = intGenerateResponseMsg(wsBufferRequestID, intResultProcessing, wsBufferResponseMsg);

    HRESULT hr = StringCchCopyW(pchReply, BUFSIZE, wsBufferResponseMsg.c_str());
    if (FAILED(hr)) {
        *pchBytes = 0;
        pchReply[0] = 0;
        //StringCchCopy failed, no outgoing message
        return;

    }

    *pchBytes = (wcslen(pchReply) + 1) * sizeof(wchar_t);
}

