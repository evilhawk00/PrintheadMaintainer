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
#include "PrintWorker.h"

#include <exception>
#include <optional>
#include <string>
#include <system_error>

#include "Printer.h"
#include "../Common/Clock.h"
#include "../Common/Paths.h"
#include "../Logging/ServiceLog.h"
#include "../Settings/SettingsStore.h"

namespace
{
    constexpr DWORD kStartupDelayMs = 15 * 1000;       // let the spooler settle after boot
    constexpr DWORD kCheckIntervalMs = 15 * 60 * 1000; // how often a scheduled print is checked
    constexpr DWORD kCountdownMs = 60 * 1000;          // the UI warns the user during this period
    constexpr DWORD kFirstJobCheckDelayMs = 60 * 1000; // time the printer gets before the first check
    constexpr int kJobPollCount = 12;                  // then poll for up to two more minutes
    constexpr DWORD kJobPollIntervalMs = 10 * 1000;
    constexpr DWORD kSettleAfterJobMs = 30 * 1000;     // time to report a problem with the printed page

    constexpr wchar_t kDocumentName[] = L"Printhead Maintainer maintenance page";

    std::wstring KindLabel(PrintKind kind)
    {
        return kind == PrintKind::Scheduled ? L"Scheduled" : L"Manual";
    }

    bool IsScheduledPrintDue(const ServiceSettings& settings)
    {
        const uint64_t now = CurrentUtcTicks();
        const std::optional<uint64_t> next = NextScheduledPrintUtc(settings, SettingsStore::LoadHistory(), now);
        return next && *next <= now;
    }

    std::wstring ImagePath(const ServiceSettings& settings)
    {
        return settings.customImage ? Paths::CustomImagePath() : Paths::DefaultImagePath();
    }

    // Finds what would prevent printing with these settings before anything is sent to the printer.
    FailureReason CheckReadiness(const ServiceSettings& settings)
    {
        if (settings.printerName.empty())
        {
            return FailureReason::PrinterNotFound;
        }
        if (::GetFileAttributesW(ImagePath(settings).c_str()) == INVALID_FILE_ATTRIBUTES)
        {
            return FailureReason::ImageUnavailable;
        }
        return Printing::CheckPrinter(settings.printerName);
    }
}

PrintWorker::PrintWorker(HANDLE stopEvent)
    : m_stopEvent(stopEvent),
      m_manualPrintRequested(::CreateEventW(nullptr, FALSE, FALSE, nullptr))
{
}

PrintWorker::~PrintWorker()
{
    Join();
}

bool PrintWorker::Start()
{
    if (!m_manualPrintRequested)
    {
        return false;
    }

    try
    {
        m_thread = std::thread(&PrintWorker::Run, this);
    }
    catch (const std::system_error&)
    {
        return false;
    }
    return true;
}

void PrintWorker::Join()
{
    if (m_thread.joinable())
    {
        m_thread.join();
    }
}

ManualPrintRequest PrintWorker::RequestManualPrint()
{
    if (SettingsStore::LoadSettings().printerName.empty())
    {
        return ManualPrintRequest::NotConfigured;
    }

    bool alreadyPending = false;
    if (!m_manualPrintPending.compare_exchange_strong(alreadyPending, true))
    {
        return ManualPrintRequest::Busy;
    }

    // If a scheduled print is running, the request is handled as soon as it finishes.
    ::SetEvent(m_manualPrintRequested.Get());
    return ManualPrintRequest::Accepted;
}

bool PrintWorker::Wait(DWORD milliseconds) const
{
    return ::WaitForSingleObject(m_stopEvent, milliseconds) == WAIT_TIMEOUT;
}

void PrintWorker::Run()
{
    if (!Wait(kStartupDelayMs))
    {
        return;
    }

    const HANDLE wakeEvents[] = { m_stopEvent, m_manualPrintRequested.Get() };
    for (;;)
    {
        const bool manual = m_manualPrintPending.load();
        try
        {
            if (manual)
            {
                RunPrint(PrintKind::Manual);
            }
            else if (IsScheduledPrintDue(SettingsStore::LoadSettings()))
            {
                RunPrint(PrintKind::Scheduled);
            }
        }
        catch (const std::exception&)
        {
            m_state.store(PrintState::Idle);
            ServiceLog::Write(L"Printing was aborted because of an unexpected error");
        }

        // A manual print requested while a scheduled one ran is still pending and runs next.
        if (manual)
        {
            m_manualPrintPending.store(false);
        }

        const DWORD woken = ::WaitForMultipleObjects(2, wakeEvents, FALSE, kCheckIntervalMs);
        if (woken != WAIT_OBJECT_0 + 1 && woken != WAIT_TIMEOUT)
        {
            return; // stop requested (or the wait failed)
        }
    }
}

void PrintWorker::RunPrint(PrintKind kind)
{
    const JobOutcome outcome = Print(kind);
    m_state.store(PrintState::Idle);

    if (outcome.interrupted)
    {
        ServiceLog::Write(KindLabel(kind) + L" printing was cancelled because the service is stopping");
        return;
    }
    if (outcome.skipped)
    {
        ServiceLog::Write(L"Scheduled printing was skipped because the settings were changed during the countdown");
        return;
    }

    const uint64_t now = CurrentUtcTicks();
    if (outcome.failure == FailureReason::None)
    {
        SettingsStore::RecordSuccess(now);
        ServiceLog::Write(KindLabel(kind) + L" printing succeeded");
    }
    else
    {
        SettingsStore::RecordFailure(kind, now, outcome.failure);
        ServiceLog::Write(KindLabel(kind) + L" printing failed: " + FailureReasonDescription(outcome.failure));
    }
}

PrintWorker::JobOutcome PrintWorker::Print(PrintKind kind)
{
    // Check before announcing a scheduled print, so that an offline or jammed printer is
    // reported right away instead of after a countdown that cannot print a page.
    ServiceSettings settings = SettingsStore::LoadSettings();
    FailureReason failure = CheckReadiness(settings);
    if (failure != FailureReason::None)
    {
        return { false, failure };
    }

    if (kind == PrintKind::Scheduled)
    {
        m_state.store(PrintState::Countdown);
        if (!Wait(kCountdownMs))
        {
            return { true, FailureReason::None };
        }

        // The user may have changed the settings meanwhile, for example turned scheduled
        // printing off or chosen another printer or image.
        settings = SettingsStore::LoadSettings();
        if (!IsScheduledPrintDue(settings))
        {
            JobOutcome skipped;
            skipped.skipped = true;
            return skipped;
        }
        failure = CheckReadiness(settings);
        if (failure != FailureReason::None)
        {
            return { false, failure };
        }
    }

    m_state.store(PrintState::Printing);
    const Printing::SubmitResult submitted =
        Printing::SubmitBitmapJob(settings.printerName, settings.paperSource, ImagePath(settings), kDocumentName);
    if (submitted.failure != FailureReason::None)
    {
        return { false, submitted.failure };
    }
    return WaitForJob(settings.printerName, submitted.jobId);
}

PrintWorker::JobOutcome PrintWorker::WaitForJob(const std::wstring& printerName, DWORD jobId)
{
    // A job that does not finish is removed from the queue so that it cannot print later.
    const auto cancel = [&](JobOutcome outcome) {
        Printing::CancelJob(printerName, jobId);
        return outcome;
    };

    if (!Wait(kFirstJobCheckDelayMs))
    {
        return cancel({ true, FailureReason::None });
    }

    FailureReason failure = Printing::CheckPrinter(printerName);
    if (failure != FailureReason::None)
    {
        return cancel({ false, failure });
    }

    Printing::JobStatus job = Printing::QueryJob(printerName, jobId);
    if (job.state == Printing::JobState::Completed)
    {
        return {}; // printed within the first minute
    }

    for (int poll = 0; poll < kJobPollCount; ++poll)
    {
        if (job.state == Printing::JobState::Failed)
        {
            return cancel({ false, job.failure });
        }
        if (!Wait(kJobPollIntervalMs))
        {
            return cancel({ true, FailureReason::None });
        }

        job = Printing::QueryJob(printerName, jobId);
        if (job.state == Printing::JobState::Completed)
        {
            break;
        }

        failure = Printing::CheckPrinter(printerName);
        if (failure != FailureReason::None)
        {
            return cancel({ false, failure });
        }
    }

    if (job.state != Printing::JobState::Completed)
    {
        const FailureReason reason =
            job.state == Printing::JobState::Failed ? job.failure : FailureReason::JobNotCompleted;
        return cancel({ false, reason });
    }

    // The job has left the queue. Give the printer time to report a problem with the page.
    // If the service stops meanwhile, count the page as printed rather than printing it twice.
    if (!Wait(kSettleAfterJobMs))
    {
        return {};
    }
    return { false, Printing::CheckPrinter(printerName) };
}
