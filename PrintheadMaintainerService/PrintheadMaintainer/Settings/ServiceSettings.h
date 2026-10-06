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
#include <optional>
#include <string>
#include <string_view>

#include "../Printing/FailureReason.h"

constexpr DWORD kMinIntervalDays = 1;
constexpr DWORD kMaxIntervalDays = 365;
constexpr DWORD kDefaultIntervalDays = 7;
constexpr DWORD kMaxPaperSource = 32767;          // DEVMODE dmDefaultSource is a short
constexpr size_t kMaxPrinterNameLength = 256;
constexpr size_t kMaxImageSourceNameLength = 255; // a file name, as NTFS allows

struct ServiceSettings
{
    bool enabled = false;
    DWORD intervalDays = kDefaultIntervalDays;
    std::wstring printerName;     // empty until the user selects a printer
    short paperSource = 0;        // DEVMODE dmDefaultSource; 0 uses the printer default
    bool customImage = false;     // false prints the image installed with the program
    std::wstring imageSourceName; // the file the custom image came from, for display only
};

enum class PrintKind
{
    Scheduled,
    Manual,
};

struct FailureRecord
{
    uint64_t timeUtc = 0; // FILETIME ticks; 0 when no failure was recorded
    FailureReason reason = FailureReason::None;
};

struct PrintHistory
{
    uint64_t lastPrintUtc = 0; // FILETIME ticks; 0 when nothing was printed yet
    FailureRecord lastScheduledFailure;
    FailureRecord lastManualFailure;
};

bool IsValidIntervalDays(uint64_t days);
bool IsValidPaperSource(uint64_t paperSource);
bool IsValidPrinterName(std::wstring_view name);
bool IsValidImageSourceName(std::wstring_view name);

// When the next scheduled print is due, or nothing if scheduled printing is disabled or no
// printer is configured. A last print time in the future (the clock was turned back) is
// ignored so that printing resumes instead of waiting until that time.
std::optional<uint64_t> NextScheduledPrintUtc(const ServiceSettings& settings, const PrintHistory& history,
    uint64_t nowUtc);
