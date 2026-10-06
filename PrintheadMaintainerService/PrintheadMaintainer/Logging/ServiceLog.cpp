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
#include "ServiceLog.h"

#include <Windows.h>
#include <mutex>
#include <string>

#include "../Common/Clock.h"
#include "../Common/Paths.h"
#include "../Common/Text.h"
#include "../Common/WinHandle.h"

namespace
{
    // Roughly 3,000 entries per file; with the backup about 6,000 entries are kept.
    constexpr LONGLONG kRotateSize = 256 * 1024;

    std::mutex g_mutex;

    UniqueFileHandle OpenForAppend(const std::wstring& path)
    {
        // FILE_APPEND_DATA without FILE_WRITE_DATA: every write goes to the end of the file.
        // Readers may keep the file open, and may rename it away, while we write.
        return UniqueFileHandle(::CreateFileW(path.c_str(), FILE_APPEND_DATA, FILE_SHARE_READ | FILE_SHARE_DELETE,
            nullptr, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr));
    }

    LONGLONG FileSize(HANDLE file)
    {
        LARGE_INTEGER size{};
        return ::GetFileSizeEx(file, &size) ? size.QuadPart : 0;
    }
}

namespace ServiceLog
{
    void Write(std::wstring_view message)
    {
        const std::wstring entry = FormatCurrentLocalTime() + L"  " + ReplaceControlCharacters(message) + L"\r\n";

        std::lock_guard<std::mutex> lock(g_mutex);

        const std::wstring directory = Paths::LogDirectory();
        ::CreateDirectoryW(directory.c_str(), nullptr); // inherits the data directory's permissions
        const std::wstring path = directory + kFileName;

        UniqueFileHandle file = OpenForAppend(path);
        if (!file)
        {
            return;
        }

        LONGLONG size = FileSize(file.Get());
        if (size >= kRotateSize)
        {
            file.Reset();
            const std::wstring backupPath = directory + kBackupFileName;
            // If the rename fails (another program holds the file without sharing delete access),
            // keep appending and try again with the next entry.
            ::MoveFileExW(path.c_str(), backupPath.c_str(), MOVEFILE_REPLACE_EXISTING);
            file = OpenForAppend(path);
            if (!file)
            {
                return;
            }
            size = FileSize(file.Get());
        }

        std::string bytes;
        if (size == 0)
        {
            bytes = "\xEF\xBB\xBF"; // UTF-8 byte order mark for new files
        }
        bytes += ToUtf8(entry);

        DWORD written = 0;
        ::WriteFile(file.Get(), bytes.data(), static_cast<DWORD>(bytes.size()), &written, nullptr);
    }
}
