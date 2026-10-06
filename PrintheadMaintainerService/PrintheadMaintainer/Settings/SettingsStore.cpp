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
#include "SettingsStore.h"

#include <cwchar>
#include <mutex>
#include <optional>
#include <string>
#include <utility>

#include "../Common/WinHandle.h"

namespace
{
    constexpr wchar_t kKeyPath[] = L"SOFTWARE\\evilhawk00\\Printhead Maintainer";

    constexpr wchar_t kEnabled[] = L"Enabled";
    constexpr wchar_t kIntervalDays[] = L"IntervalDays";
    constexpr wchar_t kPrinterName[] = L"PrinterName";
    constexpr wchar_t kPaperSource[] = L"PaperSource";
    constexpr wchar_t kCustomImage[] = L"CustomImage";
    constexpr wchar_t kImageSourceName[] = L"ImageSourceName";
    constexpr wchar_t kPostponedUntil[] = L"PostponedUntil";
    constexpr wchar_t kLastPrintTime[] = L"LastPrintTime";
    constexpr wchar_t kLastMarkedPrintTime[] = L"LastMarkedPrintTime";
    constexpr wchar_t kLastScheduledFailureTime[] = L"LastScheduledFailureTime";
    constexpr wchar_t kLastScheduledFailureReason[] = L"LastScheduledFailureReason";
    constexpr wchar_t kLastManualFailureTime[] = L"LastManualFailureTime";
    constexpr wchar_t kLastManualFailureReason[] = L"LastManualFailureReason";

    std::mutex g_mutex;

    UniqueRegKey OpenKey(bool forWriting)
    {
        HKEY key = nullptr;
        LSTATUS status;
        if (forWriting)
        {
            status = ::RegCreateKeyExW(HKEY_LOCAL_MACHINE, kKeyPath, 0, nullptr, REG_OPTION_NON_VOLATILE,
                KEY_QUERY_VALUE | KEY_SET_VALUE | KEY_WOW64_64KEY, nullptr, &key, nullptr);
        }
        else
        {
            status = ::RegOpenKeyExW(HKEY_LOCAL_MACHINE, kKeyPath, 0, KEY_QUERY_VALUE | KEY_WOW64_64KEY, &key);
        }
        return status == ERROR_SUCCESS ? UniqueRegKey(key) : UniqueRegKey();
    }

    std::optional<DWORD> ReadDword(HKEY key, const wchar_t* name)
    {
        DWORD value = 0;
        DWORD size = sizeof(value);
        if (::RegGetValueW(key, nullptr, name, RRF_RT_REG_DWORD, nullptr, &value, &size) != ERROR_SUCCESS)
        {
            return std::nullopt;
        }
        return value;
    }

    std::optional<uint64_t> ReadQword(HKEY key, const wchar_t* name)
    {
        uint64_t value = 0;
        DWORD size = sizeof(value);
        if (::RegGetValueW(key, nullptr, name, RRF_RT_REG_QWORD, nullptr, &value, &size) != ERROR_SUCCESS)
        {
            return std::nullopt;
        }
        return value;
    }

    // RegGetValueW checks the type and always returns a null-terminated string.
    std::optional<std::wstring> ReadString(HKEY key, const wchar_t* name)
    {
        for (int attempt = 0; attempt < 3; ++attempt)
        {
            DWORD size = 0;
            if (::RegGetValueW(key, nullptr, name, RRF_RT_REG_SZ, nullptr, nullptr, &size) != ERROR_SUCCESS)
            {
                return std::nullopt;
            }

            std::wstring value(size / sizeof(wchar_t) + 1, L'\0');
            size = static_cast<DWORD>(value.size() * sizeof(wchar_t));
            const LSTATUS status = ::RegGetValueW(key, nullptr, name, RRF_RT_REG_SZ, nullptr, value.data(), &size);
            if (status == ERROR_MORE_DATA)
            {
                continue; // the value grew in between; ask for the size again
            }
            if (status != ERROR_SUCCESS)
            {
                return std::nullopt;
            }
            value.resize(std::wcslen(value.c_str()));
            return value;
        }
        return std::nullopt;
    }

    bool WriteDword(HKEY key, const wchar_t* name, DWORD value)
    {
        return ::RegSetValueExW(key, name, 0, REG_DWORD, reinterpret_cast<const BYTE*>(&value), sizeof(value)) ==
            ERROR_SUCCESS;
    }

    bool WriteQword(HKEY key, const wchar_t* name, uint64_t value)
    {
        return ::RegSetValueExW(key, name, 0, REG_QWORD, reinterpret_cast<const BYTE*>(&value), sizeof(value)) ==
            ERROR_SUCCESS;
    }

    bool WriteString(HKEY key, const wchar_t* name, const std::wstring& value)
    {
        const DWORD size = static_cast<DWORD>((value.size() + 1) * sizeof(wchar_t));
        return ::RegSetValueExW(key, name, 0, REG_SZ, reinterpret_cast<const BYTE*>(value.c_str()), size) ==
            ERROR_SUCCESS;
    }

    FailureRecord ReadFailure(HKEY key, const wchar_t* timeName, const wchar_t* reasonName)
    {
        FailureRecord record;
        const std::optional<uint64_t> time = ReadQword(key, timeName);
        if (time && *time != 0)
        {
            record.timeUtc = *time;
            const std::optional<DWORD> reason = ReadDword(key, reasonName);
            record.reason = reason ? FailureReasonFromValue(*reason) : FailureReason::PrinterError;
        }
        return record;
    }
}

namespace SettingsStore
{
    ServiceSettings LoadSettings()
    {
        std::lock_guard<std::mutex> lock(g_mutex);

        ServiceSettings settings;
        const UniqueRegKey key = OpenKey(false);
        if (!key)
        {
            return settings;
        }

        if (const auto value = ReadDword(key.Get(), kEnabled))
        {
            settings.enabled = *value == 1;
        }
        if (const auto value = ReadDword(key.Get(), kIntervalDays); value && IsValidIntervalDays(*value))
        {
            settings.intervalDays = *value;
        }
        if (auto value = ReadString(key.Get(), kPrinterName); value && IsValidPrinterName(*value))
        {
            settings.printerName = std::move(*value);
        }
        if (const auto value = ReadDword(key.Get(), kPaperSource); value && IsValidPaperSource(*value))
        {
            settings.paperSource = static_cast<short>(*value);
        }
        if (const auto value = ReadDword(key.Get(), kCustomImage))
        {
            settings.customImage = *value == 1;
        }
        if (auto value = ReadString(key.Get(), kImageSourceName); value && IsValidImageSourceName(*value))
        {
            settings.imageSourceName = std::move(*value);
        }
        settings.postponedUntilUtc = ReadQword(key.Get(), kPostponedUntil).value_or(0);
        return settings;
    }

    bool SaveSettings(const ServiceSettings& settings)
    {
        std::lock_guard<std::mutex> lock(g_mutex);

        const UniqueRegKey key = OpenKey(true);
        if (!key)
        {
            return false;
        }

        bool written = WriteDword(key.Get(), kEnabled, settings.enabled ? 1 : 0);
        written = WriteDword(key.Get(), kIntervalDays, settings.intervalDays) && written;
        written = WriteString(key.Get(), kPrinterName, settings.printerName) && written;
        written = WriteDword(key.Get(), kPaperSource, static_cast<DWORD>(settings.paperSource)) && written;
        written = WriteDword(key.Get(), kCustomImage, settings.customImage ? 1 : 0) && written;
        written = WriteString(key.Get(), kImageSourceName, settings.imageSourceName) && written;
        written = WriteQword(key.Get(), kPostponedUntil, settings.postponedUntilUtc) && written;
        return written;
    }

    bool SavePostponement(uint64_t untilUtc)
    {
        std::lock_guard<std::mutex> lock(g_mutex);

        const UniqueRegKey key = OpenKey(true);
        return key && WriteQword(key.Get(), kPostponedUntil, untilUtc);
    }

    PrintHistory LoadHistory()
    {
        std::lock_guard<std::mutex> lock(g_mutex);

        PrintHistory history;
        const UniqueRegKey key = OpenKey(false);
        if (!key)
        {
            return history;
        }

        history.lastPrintUtc = ReadQword(key.Get(), kLastPrintTime).value_or(0);
        history.lastMarkedPrintUtc = ReadQword(key.Get(), kLastMarkedPrintTime).value_or(0);
        history.lastScheduledFailure = ReadFailure(key.Get(), kLastScheduledFailureTime, kLastScheduledFailureReason);
        history.lastManualFailure = ReadFailure(key.Get(), kLastManualFailureTime, kLastManualFailureReason);
        return history;
    }

    bool RecordSuccess(uint64_t timeUtc)
    {
        std::lock_guard<std::mutex> lock(g_mutex);

        const UniqueRegKey key = OpenKey(true);
        return key && WriteQword(key.Get(), kLastPrintTime, timeUtc);
    }

    bool RecordFailure(PrintKind kind, uint64_t timeUtc, FailureReason reason)
    {
        std::lock_guard<std::mutex> lock(g_mutex);

        const UniqueRegKey key = OpenKey(true);
        if (!key)
        {
            return false;
        }

        const bool scheduled = kind == PrintKind::Scheduled;
        const bool reasonWritten = WriteDword(key.Get(),
            scheduled ? kLastScheduledFailureReason : kLastManualFailureReason, static_cast<DWORD>(reason));
        const bool timeWritten = WriteQword(key.Get(),
            scheduled ? kLastScheduledFailureTime : kLastManualFailureTime, timeUtc);
        return reasonWritten && timeWritten;
    }

    bool RecordMarkedPrint(uint64_t timeUtc)
    {
        std::lock_guard<std::mutex> lock(g_mutex);

        const UniqueRegKey key = OpenKey(true);
        return key && WriteQword(key.Get(), kLastMarkedPrintTime, timeUtc);
    }
}
