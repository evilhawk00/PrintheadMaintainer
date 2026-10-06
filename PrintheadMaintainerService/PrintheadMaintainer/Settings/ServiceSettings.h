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
// Enough to skip a print that is a year away.
constexpr DWORD kMaxPostponementDays = 2 * kMaxIntervalDays;
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

    // No scheduled print before this time (FILETIME ticks); 0 if it was never postponed. A
    // postponement that has ended keeps its end, which lies in the past.
    uint64_t postponedUntilUtc = 0;
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
    uint64_t lastPrintUtc = 0;       // FILETIME ticks; 0 when nothing was printed yet
    uint64_t lastMarkedPrintUtc = 0; // when a user said the printer was used otherwise; 0 when never
    FailureRecord lastScheduledFailure;
    FailureRecord lastManualFailure;
};

bool IsValidIntervalDays(uint64_t days);
bool IsValidPaperSource(uint64_t paperSource);
bool IsValidPrinterName(std::wstring_view name);
bool IsValidImageSourceName(std::wstring_view name);

// 0 removes the postponement; any other time must lie ahead, at most kMaxPostponementDays.
bool IsValidPostponement(uint64_t untilUtc, uint64_t nowUtc);

// What the schedule counts from: the later of the last print and the time a user marked the
// printer as printed, or 0 if there is neither. Times in the future (the clock was turned back)
// are ignored so that printing resumes instead of waiting until then.
uint64_t LastMaintenanceUtc(const PrintHistory& history, uint64_t nowUtc);

struct ScheduledPrint
{
    uint64_t timeUtc = 0;   // FILETIME ticks; at or before now when the print is due
    bool postponed = false; // the time is the end of a postponement
};

// When the next scheduled print is due, or nothing if scheduled printing is disabled or no
// printer is configured.
std::optional<ScheduledPrint> NextScheduledPrint(const ServiceSettings& settings, const PrintHistory& history,
    uint64_t nowUtc);
