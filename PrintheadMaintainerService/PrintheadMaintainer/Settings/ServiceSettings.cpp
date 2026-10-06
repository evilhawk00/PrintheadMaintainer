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
#include "ServiceSettings.h"

#include "../Common/Clock.h"
#include "../Common/Text.h"

bool IsValidIntervalDays(uint64_t days)
{
    return days >= kMinIntervalDays && days <= kMaxIntervalDays;
}

bool IsValidPaperSource(uint64_t paperSource)
{
    return paperSource <= kMaxPaperSource;
}

bool IsValidPrinterName(std::wstring_view name)
{
    return !name.empty() && name.size() <= kMaxPrinterNameLength && !ContainsControlCharacters(name);
}

bool IsValidImageSourceName(std::wstring_view name)
{
    return name.size() <= kMaxImageSourceNameLength && !ContainsControlCharacters(name);
}

std::optional<uint64_t> NextScheduledPrintUtc(const ServiceSettings& settings, const PrintHistory& history,
    uint64_t nowUtc)
{
    if (!settings.enabled || settings.printerName.empty())
    {
        return std::nullopt;
    }
    if (history.lastPrintUtc == 0 || history.lastPrintUtc > nowUtc)
    {
        return nowUtc;
    }
    return history.lastPrintUtc + settings.intervalDays * kTicksPerDay;
}
