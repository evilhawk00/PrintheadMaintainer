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
#include "PipeServer.h"

#include <sddl.h>
#include <exception>
#include <optional>
#include <system_error>
#include <utility>

#include "Message.h"
#include "RequestHandler.h"
#include "../Common/WinHandle.h"
#include "../Logging/ServiceLog.h"

namespace
{
    constexpr wchar_t kPipeName[] = L"\\\\.\\pipe\\Printhead Maintainer";

    // SYSTEM and administrators: full control. Interactively logged-on users: 0x12019b, which is
    // FILE_GENERIC_READ | FILE_GENERIC_WRITE without FILE_CREATE_PIPE_INSTANCE, so they can talk
    // to the service but cannot create pipe instances to impersonate it.
    constexpr wchar_t kPipeSecurity[] = L"D:P(A;;GA;;;SY)(A;;GA;;;BA)(A;;0x12019b;;;IU)";

    constexpr DWORD kPipeBufferSize = 64 * 1024;
    constexpr size_t kReadChunkSize = 1024 * 1024;
    constexpr DWORD kIoTimeoutMs = 10 * 1000;    // per read or write; large requests may take several
    constexpr DWORD kCloseTimeoutMs = 5 * 1000;  // for the client to close its end after the response
    constexpr DWORD kRetryDelayMs = 5 * 1000;    // before trying to create the pipe again

    UniqueLocalMemory CreatePipeSecurityDescriptor()
    {
        PSECURITY_DESCRIPTOR descriptor = nullptr;
        if (!::ConvertStringSecurityDescriptorToSecurityDescriptorW(kPipeSecurity, SDDL_REVISION_1, &descriptor,
                nullptr))
        {
            return UniqueLocalMemory();
        }
        return UniqueLocalMemory(descriptor);
    }

    UniqueFileHandle CreatePipeInstance(PSECURITY_DESCRIPTOR descriptor)
    {
        SECURITY_ATTRIBUTES attributes{};
        attributes.nLength = sizeof(attributes);
        attributes.lpSecurityDescriptor = descriptor;
        attributes.bInheritHandle = FALSE;

        // FILE_FLAG_FIRST_PIPE_INSTANCE fails if another process already created the pipe.
        return UniqueFileHandle(::CreateNamedPipeW(kPipeName,
            PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED | FILE_FLAG_FIRST_PIPE_INSTANCE,
            PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT | PIPE_REJECT_REMOTE_CLIENTS,
            1, kPipeBufferSize, kPipeBufferSize, 0, &attributes));
    }

    // Starts an overlapped operation and waits until it completes, the service stops or the
    // timeout passes. Returns the operation's result (ERROR_SUCCESS, ERROR_MORE_DATA, ...),
    // ERROR_OPERATION_ABORTED when stopping or ERROR_TIMEOUT. A pending operation is cancelled
    // and finished before returning, so the OVERLAPPED structure may go out of scope.
    template <typename StartOperation>
    DWORD RunOverlapped(HANDLE pipe, HANDLE stopEvent, DWORD timeoutMs, DWORD& transferred, StartOperation start)
    {
        transferred = 0;
        const UniqueKernelHandle completed(::CreateEventW(nullptr, TRUE, FALSE, nullptr));
        if (!completed)
        {
            return ::GetLastError();
        }

        OVERLAPPED overlapped{};
        overlapped.hEvent = completed.Get();
        if (!start(&overlapped))
        {
            const DWORD error = ::GetLastError();
            if (error != ERROR_IO_PENDING && error != ERROR_MORE_DATA)
            {
                return error;
            }
            if (error == ERROR_IO_PENDING)
            {
                const HANDLE events[] = { completed.Get(), stopEvent };
                const DWORD waited = ::WaitForMultipleObjects(2, events, FALSE, timeoutMs);
                if (waited != WAIT_OBJECT_0)
                {
                    ::CancelIoEx(pipe, &overlapped);
                    ::GetOverlappedResult(pipe, &overlapped, &transferred, TRUE);
                    return waited == WAIT_TIMEOUT ? ERROR_TIMEOUT : ERROR_OPERATION_ABORTED;
                }
            }
        }

        if (!::GetOverlappedResult(pipe, &overlapped, &transferred, FALSE))
        {
            return ::GetLastError();
        }
        return ERROR_SUCCESS;
    }

    std::wstring AccountName(HANDLE token)
    {
        DWORD size = 0;
        ::GetTokenInformation(token, TokenUser, nullptr, 0, &size);
        std::vector<BYTE> buffer(size);
        if (size == 0 || !::GetTokenInformation(token, TokenUser, buffer.data(), size, &size))
        {
            return std::wstring();
        }
        const PSID sid = reinterpret_cast<const TOKEN_USER*>(buffer.data())->User.Sid;

        wchar_t name[256] = {};
        wchar_t domain[256] = {};
        DWORD nameLength = ARRAYSIZE(name);
        DWORD domainLength = ARRAYSIZE(domain);
        SID_NAME_USE use{};
        if (::LookupAccountSidW(nullptr, sid, name, &nameLength, domain, &domainLength, &use))
        {
            return std::wstring(domain) + L"\\" + name;
        }

        wchar_t* sidText = nullptr;
        if (::ConvertSidToStringSidW(sid, &sidText))
        {
            const UniqueLocalMemory owned(sidText);
            return sidText;
        }
        return std::wstring();
    }

    // Names the user and process on the other end of the pipe, for the service log. The UI
    // connects at identification level, so the service can read the identity but cannot act
    // as the user.
    std::wstring IdentifyClient(HANDLE pipe)
    {
        std::wstring user;
        if (::ImpersonateNamedPipeClient(pipe))
        {
            HANDLE token = nullptr;
            const BOOL opened = ::OpenThreadToken(::GetCurrentThread(), TOKEN_QUERY, TRUE, &token);
            if (!::RevertToSelf())
            {
                // Running on with the client's identity would be unsafe.
                std::terminate();
            }
            if (opened)
            {
                const UniqueKernelHandle owned(token);
                user = AccountName(owned.Get());
            }
        }

        ULONG processId = 0;
        ::GetNamedPipeClientProcessId(pipe, &processId);
        return (user.empty() ? std::wstring(L"an unknown user") : user) + L" (process " +
            std::to_wstring(processId) + L")";
    }
}

PipeServer::PipeServer(HANDLE stopEvent, PrintWorker& worker)
    : m_stopEvent(stopEvent), m_worker(worker)
{
}

PipeServer::~PipeServer()
{
    Join();
}

bool PipeServer::Start()
{
    try
    {
        m_thread = std::thread(&PipeServer::Run, this);
    }
    catch (const std::system_error&)
    {
        return false;
    }
    return true;
}

void PipeServer::Join()
{
    if (m_thread.joinable())
    {
        m_thread.join();
    }
}

bool PipeServer::IsStopping() const
{
    return ::WaitForSingleObject(m_stopEvent, 0) != WAIT_TIMEOUT;
}

void PipeServer::Run()
{
    const UniqueLocalMemory securityDescriptor = CreatePipeSecurityDescriptor();
    if (!securityDescriptor)
    {
        ServiceLog::Write(L"The service cannot accept requests: the pipe security could not be set up (error " +
            std::to_wstring(::GetLastError()) + L")");
        return;
    }

    bool creationFailureLogged = false;
    while (!IsStopping())
    {
        const UniqueFileHandle pipe = CreatePipeInstance(securityDescriptor.Get());
        if (!pipe)
        {
            // Another process may hold the pipe name; keep trying instead of giving up for good.
            if (!creationFailureLogged)
            {
                ServiceLog::Write(L"The pipe for the user interface could not be created (error " +
                    std::to_wstring(::GetLastError()) + L"); retrying");
                creationFailureLogged = true;
            }
            ::WaitForSingleObject(m_stopEvent, kRetryDelayMs);
            continue;
        }

        creationFailureLogged = false;
        ServeClients(pipe.Get());
    }
}

void PipeServer::ServeClients(HANDLE pipe)
{
    for (;;)
    {
        DWORD transferred = 0;
        DWORD result = RunOverlapped(pipe, m_stopEvent, INFINITE, transferred, [&](OVERLAPPED* overlapped) {
            return ::ConnectNamedPipe(pipe, overlapped);
        });
        if (result == ERROR_OPERATION_ABORTED)
        {
            return;
        }
        if (result == ERROR_SUCCESS || result == ERROR_PIPE_CONNECTED)
        {
            try
            {
                ServeClient(pipe);
            }
            catch (const std::exception&)
            {
                // Out of memory or similar; drop this client and keep serving.
            }
        }
        else if (result != ERROR_NO_DATA)
        {
            return; // the instance is unusable; let Run create a new one
        }

        if (!::DisconnectNamedPipe(pipe) || IsStopping())
        {
            return;
        }
    }
}

void PipeServer::ServeClient(HANDLE pipe)
{
    std::vector<uint8_t> requestBytes;
    if (!ReadMessage(pipe, requestBytes))
    {
        return;
    }

    const std::wstring clientName = IdentifyClient(pipe);
    std::optional<Ipc::Message> request = Ipc::Parse(std::move(requestBytes));
    std::vector<uint8_t> responseBytes;
    try
    {
        responseBytes = Ipc::Serialize(request ? Ipc::HandleRequest(*request, clientName, m_worker)
                                               : Ipc::Message(Ipc::kResultInvalidRequest));
    }
    catch (const std::exception&)
    {
        responseBytes = Ipc::Serialize(Ipc::Message(Ipc::kResultInternalError));
    }
    DWORD transferred = 0;
    if (RunOverlapped(pipe, m_stopEvent, kIoTimeoutMs, transferred, [&](OVERLAPPED* overlapped) {
            return ::WriteFile(pipe, responseBytes.data(), static_cast<DWORD>(responseBytes.size()), nullptr,
                overlapped);
        }) != ERROR_SUCCESS)
    {
        return;
    }

    // DisconnectNamedPipe throws away data the client has not read yet, so wait until the
    // client closes its end (or the time limit passes) before disconnecting.
    BYTE ignored[64];
    RunOverlapped(pipe, m_stopEvent, kCloseTimeoutMs, transferred, [&](OVERLAPPED* overlapped) {
        return ::ReadFile(pipe, ignored, sizeof(ignored), nullptr, overlapped);
    });
}

bool PipeServer::ReadMessage(HANDLE pipe, std::vector<uint8_t>& message) const
{
    message.clear();
    std::vector<uint8_t> chunk(kReadChunkSize);
    for (;;)
    {
        DWORD received = 0;
        const DWORD result = RunOverlapped(pipe, m_stopEvent, kIoTimeoutMs, received, [&](OVERLAPPED* overlapped) {
            return ::ReadFile(pipe, chunk.data(), static_cast<DWORD>(chunk.size()), nullptr, overlapped);
        });
        if (result != ERROR_SUCCESS && result != ERROR_MORE_DATA)
        {
            return false;
        }
        if (message.size() + received > Ipc::kMaxMessageBytes)
        {
            return false;
        }

        message.insert(message.end(), chunk.begin(), chunk.begin() + static_cast<std::ptrdiff_t>(received));
        if (result == ERROR_SUCCESS)
        {
            return true; // the whole message has been read
        }
    }
}
