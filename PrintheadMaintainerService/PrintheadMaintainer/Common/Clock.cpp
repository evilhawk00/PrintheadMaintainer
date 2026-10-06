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
#include "Clock.h"

#include <Windows.h>
#include <cwchar>

uint64_t CurrentUtcTicks()
{
    FILETIME now{};
    ::GetSystemTimeAsFileTime(&now);
    return (static_cast<uint64_t>(now.dwHighDateTime) << 32) | now.dwLowDateTime;
}

std::wstring FormatCurrentLocalTime()
{
    SYSTEMTIME now{};
    ::GetLocalTime(&now);

    wchar_t text[32] = {};
    std::swprintf(text, sizeof(text) / sizeof(text[0]), L"%04u/%02u/%02u %02u:%02u:%02u",
        now.wYear, now.wMonth, now.wDay, now.wHour, now.wMinute, now.wSecond);
    return text;
}

std::wstring FormatLocalTime(uint64_t utcTicks)
{
    FILETIME utc{};
    utc.dwLowDateTime = static_cast<DWORD>(utcTicks);
    utc.dwHighDateTime = static_cast<DWORD>(utcTicks >> 32);
    SYSTEMTIME utcTime{};
    SYSTEMTIME local{};
    if (!::FileTimeToSystemTime(&utc, &utcTime) || !::SystemTimeToTzSpecificLocalTime(nullptr, &utcTime, &local))
    {
        return L"?";
    }

    wchar_t text[32] = {};
    std::swprintf(text, sizeof(text) / sizeof(text[0]), L"%04u/%02u/%02u %02u:%02u",
        local.wYear, local.wMonth, local.wDay, local.wHour, local.wMinute);
    return text;
}
