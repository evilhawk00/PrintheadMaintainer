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

#include <string_view>

// The service log is an append-only UTF-8 text file in Paths::LogDirectory(), one entry per
// line in the form "YYYY/MM/DD HH:MM:SS  message", oldest entry first. When the file grows past
// its size limit it is renamed to the backup name (replacing the previous backup) and a new file
// is started. The UI reads both files and shows the newest entries first.
namespace ServiceLog
{
    constexpr wchar_t kFileName[] = L"PrintheadMaintainer.log";
    constexpr wchar_t kBackupFileName[] = L"PrintheadMaintainer.1.log";

    // Appends an entry. Control characters in the message are replaced so that every entry
    // stays on one line. Thread-safe. Failures are ignored; logging never stops the service.
    void Write(std::wstring_view message);
}
