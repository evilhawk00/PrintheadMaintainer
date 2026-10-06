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
#include "RequestHandler.h"

#include <Windows.h>
#include <optional>
#include <string_view>

#include "../Common/Clock.h"
#include "../Common/Paths.h"
#include "../Common/Text.h"
#include "../Logging/ServiceLog.h"
#include "../Printing/PrintImage.h"
#include "../Printing/PrintWorker.h"
#include "../Printing/Printer.h"
#include "../Settings/SettingsStore.h"

namespace
{
    constexpr wchar_t kGetStatus[] = L"GetStatus";
    constexpr wchar_t kApplySettings[] = L"ApplySettings";
    constexpr wchar_t kPrintNow[] = L"PrintNow";

    constexpr wchar_t kOk[] = L"OK";
    constexpr wchar_t kUnknownCommand[] = L"UnknownCommand";
    constexpr wchar_t kInvalidValue[] = L"InvalidValue";
    constexpr wchar_t kPrinterUnavailable[] = L"PrinterUnavailable";
    constexpr wchar_t kStorageError[] = L"StorageError";
    constexpr wchar_t kBusy[] = L"Busy";
    constexpr wchar_t kNotConfigured[] = L"NotConfigured";

    constexpr wchar_t kEnabled[] = L"Enabled";
    constexpr wchar_t kIntervalDays[] = L"IntervalDays";
    constexpr wchar_t kPrinterName[] = L"PrinterName";
    constexpr wchar_t kPaperSource[] = L"PaperSource";
    constexpr wchar_t kImage[] = L"Image";
    constexpr wchar_t kImageWidth[] = L"ImageWidth";
    constexpr wchar_t kImageHeight[] = L"ImageHeight";
    constexpr wchar_t kImageSourceName[] = L"ImageSourceName";

    constexpr wchar_t kImageDefault[] = L"Default";
    constexpr wchar_t kImageCustom[] = L"Custom";

    std::wstring Flag(bool value)
    {
        return value ? L"1" : L"0";
    }

    const wchar_t* PrintStateName(PrintState state)
    {
        switch (state)
        {
        case PrintState::Countdown: return L"Countdown";
        case PrintState::Printing: return L"Printing";
        case PrintState::Idle: break;
        }
        return L"Idle";
    }

    bool FileExists(const std::wstring& path)
    {
        const DWORD attributes = ::GetFileAttributesW(path.c_str());
        return attributes != INVALID_FILE_ATTRIBUTES && (attributes & FILE_ATTRIBUTE_DIRECTORY) == 0;
    }

    std::optional<bool> ParseFlag(const std::wstring& text)
    {
        if (text == L"1")
        {
            return true;
        }
        if (text == L"0")
        {
            return false;
        }
        return std::nullopt;
    }

    Ipc::Message InvalidValue(const wchar_t* field)
    {
        Ipc::Message response(kInvalidValue);
        response.Add(L"Field", field);
        return response;
    }

    void AppendChange(std::wstring& summary, const std::wstring& change)
    {
        if (!summary.empty())
        {
            summary += L", ";
        }
        summary += change;
    }

    Ipc::Message GetStatus(const PrintWorker& worker)
    {
        const ServiceSettings settings = SettingsStore::LoadSettings();
        const PrintHistory history = SettingsStore::LoadHistory();
        const std::wstring imagePath = settings.customImage ? Paths::CustomImagePath() : Paths::DefaultImagePath();

        Ipc::Message response(kOk);
        response.Add(L"PrintState", PrintStateName(worker.State()));
        response.Add(L"ManualPrintPending", Flag(worker.IsManualPrintPending()));
        response.Add(kEnabled, Flag(settings.enabled));
        response.Add(kIntervalDays, std::to_wstring(settings.intervalDays));
        response.Add(kPrinterName, settings.printerName);
        response.Add(kPaperSource, std::to_wstring(settings.paperSource));
        response.Add(L"CustomImage", Flag(settings.customImage));
        response.Add(kImageSourceName, settings.imageSourceName);
        response.Add(L"ImagePath", imagePath);
        response.Add(L"ImageAvailable", Flag(FileExists(imagePath)));
        response.Add(L"LastPrint", std::to_wstring(history.lastPrintUtc));
        response.Add(L"LastScheduledFailure", std::to_wstring(history.lastScheduledFailure.timeUtc));
        response.Add(L"LastScheduledFailureReason", FailureReasonName(history.lastScheduledFailure.reason));
        response.Add(L"LastManualFailure", std::to_wstring(history.lastManualFailure.timeUtc));
        response.Add(L"LastManualFailureReason", FailureReasonName(history.lastManualFailure.reason));
        response.Add(L"NextScheduledPrint",
            std::to_wstring(NextScheduledPrintUtc(settings, history, CurrentUtcTicks()).value_or(0)));
        return response;
    }

    Ipc::Message ApplySettings(const Ipc::Message& request, const std::wstring& clientName)
    {
        static constexpr std::wstring_view kKnownFields[] = {
            kEnabled, kIntervalDays, kPrinterName, kPaperSource, kImage, kImageWidth, kImageHeight, kImageSourceName,
        };
        for (const auto& field : request.fields)
        {
            bool known = false;
            for (std::wstring_view name : kKnownFields)
            {
                known = known || field.first == name;
            }
            if (!known)
            {
                return Ipc::Message(Ipc::kResultInvalidRequest);
            }
        }

        const ServiceSettings current = SettingsStore::LoadSettings();
        ServiceSettings updated = current;

        if (const std::wstring* value = request.Find(kEnabled))
        {
            const std::optional<bool> enabled = ParseFlag(*value);
            if (!enabled)
            {
                return InvalidValue(kEnabled);
            }
            updated.enabled = *enabled;
        }

        if (const std::wstring* value = request.Find(kIntervalDays))
        {
            const std::optional<uint64_t> days = ParseDecimal(*value);
            if (!days || !IsValidIntervalDays(*days))
            {
                return InvalidValue(kIntervalDays);
            }
            updated.intervalDays = static_cast<DWORD>(*days);
        }

        if (const std::wstring* value = request.Find(kPrinterName))
        {
            if (!IsValidPrinterName(*value))
            {
                return InvalidValue(kPrinterName);
            }
            if (*value != current.printerName)
            {
                const std::optional<std::wstring> installed = Printing::FindInstalledPrinter(*value);
                if (!installed)
                {
                    return Ipc::Message(kPrinterUnavailable);
                }
                updated.printerName = *installed;
            }
        }

        if (const std::wstring* value = request.Find(kPaperSource))
        {
            const std::optional<uint64_t> paperSource = ParseDecimal(*value);
            if (!paperSource || !IsValidPaperSource(*paperSource))
            {
                return InvalidValue(kPaperSource);
            }
            updated.paperSource = static_cast<short>(*paperSource);
        }

        bool saveImage = false;
        uint32_t imageWidth = 0;
        uint32_t imageHeight = 0;
        if (const std::wstring* value = request.Find(kImage))
        {
            if (*value == kImageDefault)
            {
                updated.customImage = false;
                updated.imageSourceName.clear();
            }
            else if (*value == kImageCustom)
            {
                const std::wstring* width = request.Find(kImageWidth);
                const std::wstring* height = request.Find(kImageHeight);
                const std::optional<uint64_t> parsedWidth = width ? ParseDecimal(*width) : std::nullopt;
                const std::optional<uint64_t> parsedHeight = height ? ParseDecimal(*height) : std::nullopt;
                if (!parsedWidth || !parsedHeight || !PrintImage::IsValidSize(*parsedWidth, *parsedHeight))
                {
                    return InvalidValue(kImage);
                }
                imageWidth = static_cast<uint32_t>(*parsedWidth);
                imageHeight = static_cast<uint32_t>(*parsedHeight);
                if (request.data.size() != PrintImage::RowStride(imageWidth) * imageHeight)
                {
                    return InvalidValue(kImage);
                }

                const std::wstring* sourceName = request.Find(kImageSourceName);
                if (sourceName != nullptr && !IsValidImageSourceName(*sourceName))
                {
                    return InvalidValue(kImageSourceName);
                }
                updated.customImage = true;
                updated.imageSourceName = sourceName != nullptr ? *sourceName : std::wstring();
                saveImage = true;
            }
            else
            {
                return InvalidValue(kImage);
            }
        }
        else if (request.Find(kImageWidth) || request.Find(kImageHeight) || request.Find(kImageSourceName) ||
            !request.data.empty())
        {
            return Ipc::Message(Ipc::kResultInvalidRequest);
        }

        // Everything has been validated; store it.
        if (saveImage && !PrintImage::Save(Paths::CustomImagePath(), imageWidth, imageHeight, request.data))
        {
            return Ipc::Message(kStorageError);
        }
        if (!SettingsStore::SaveSettings(updated))
        {
            return Ipc::Message(kStorageError);
        }
        if (current.customImage && !updated.customImage)
        {
            ::DeleteFileW(Paths::CustomImagePath().c_str());
        }

        std::wstring summary;
        if (updated.enabled != current.enabled)
        {
            AppendChange(summary, updated.enabled ? L"scheduled printing enabled" : L"scheduled printing disabled");
        }
        if (updated.intervalDays != current.intervalDays)
        {
            AppendChange(summary, L"interval " + std::to_wstring(updated.intervalDays) + L" days");
        }
        if (updated.printerName != current.printerName)
        {
            AppendChange(summary, L"printer \"" + updated.printerName + L"\"");
        }
        if (updated.paperSource != current.paperSource)
        {
            AppendChange(summary, updated.paperSource == 0 ? std::wstring(L"paper source: printer default")
                                                          : L"paper source " + std::to_wstring(updated.paperSource));
        }
        if (saveImage)
        {
            AppendChange(summary, L"image \"" + updated.imageSourceName + L"\"");
        }
        else if (current.customImage && !updated.customImage)
        {
            AppendChange(summary, L"image: default");
        }
        if (!summary.empty())
        {
            ServiceLog::Write(L"Settings changed by " + clientName + L": " + summary);
        }
        return Ipc::Message(kOk);
    }

    Ipc::Message PrintNow(PrintWorker& worker, const std::wstring& clientName)
    {
        switch (worker.RequestManualPrint())
        {
        case ManualPrintRequest::Accepted:
            ServiceLog::Write(L"Manual printing requested by " + clientName);
            return Ipc::Message(kOk);
        case ManualPrintRequest::Busy:
            return Ipc::Message(kBusy);
        case ManualPrintRequest::NotConfigured:
            return Ipc::Message(kNotConfigured);
        }
        return Ipc::Message(Ipc::kResultInternalError);
    }
}

namespace Ipc
{
    Message HandleRequest(const Message& request, const std::wstring& clientName, PrintWorker& worker)
    {
        if (request.name == kApplySettings)
        {
            return ApplySettings(request, clientName);
        }
        if (request.name == kGetStatus || request.name == kPrintNow)
        {
            if (!request.fields.empty() || !request.data.empty())
            {
                return Message(kResultInvalidRequest);
            }
            return request.name == kGetStatus ? GetStatus(worker) : PrintNow(worker, clientName);
        }
        return Message(kUnknownCommand);
    }
}
