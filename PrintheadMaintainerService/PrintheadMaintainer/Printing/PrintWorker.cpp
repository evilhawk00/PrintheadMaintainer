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
    constexpr DWORD kNotReadyIntervalMs = 60 * 1000;   // the same while a due print waits for the printer
    constexpr DWORD kCountdownMs = 2 * 60 * 1000;      // the UI warns the user during this period
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
        const std::optional<ScheduledPrint> next = NextScheduledPrint(settings, SettingsStore::LoadHistory(), now);
        return next && next->timeUtc <= now;
    }

    std::wstring ImagePath(const ServiceSettings& settings)
    {
        return settings.customImage ? Paths::CustomImagePath() : Paths::DefaultImagePath();
    }

    // Whether a scheduled failure has been recorded since the last print (or mark as printed)
    // and since a postponement ended.
    bool IsRecordedFailure(const ServiceSettings& settings, const PrintHistory& history, uint64_t nowUtc)
    {
        const FailureRecord& last = history.lastScheduledFailure;
        return last.timeUtc != 0 && last.timeUtc > LastMaintenanceUtc(history, nowUtc) &&
            last.timeUtc >= settings.postponedUntilUtc && last.timeUtc <= nowUtc;
    }

    // Finds what would prevent printing with these settings before anything is sent to the printer.
    Printing::PrinterStatus CheckReadiness(const ServiceSettings& settings)
    {
        if (settings.printerName.empty())
        {
            return { FailureReason::PrinterNotFound, false };
        }
        if (::GetFileAttributesW(ImagePath(settings).c_str()) == INVALID_FILE_ATTRIBUTES)
        {
            return { FailureReason::ImageUnavailable, false };
        }
        return Printing::CheckPrinter(settings.printerName);
    }
}

PrintWorker::PrintWorker(HANDLE stopEvent)
    : m_stopEvent(stopEvent),
      m_manualPrintRequested(::CreateEventW(nullptr, FALSE, FALSE, nullptr)),
      m_scheduleChanged(::CreateEventW(nullptr, FALSE, FALSE, nullptr))
{
}

PrintWorker::~PrintWorker()
{
    Join();
}

bool PrintWorker::Start()
{
    if (!m_manualPrintRequested || !m_scheduleChanged)
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

    // A scheduled print that is counting down gives way to the manual one; one that is already
    // printing finishes first.
    ::SetEvent(m_manualPrintRequested.Get());
    return ManualPrintRequest::Accepted;
}

void PrintWorker::OnScheduleChanged()
{
    ::SetEvent(m_scheduleChanged.Get());
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

    const HANDLE wakeEvents[] = { m_stopEvent, m_manualPrintRequested.Get(), m_scheduleChanged.Get() };
    for (;;)
    {
        const bool manual = m_manualPrintPending.load();
        DWORD nextCheckMs = kCheckIntervalMs;
        try
        {
            if (manual)
            {
                RunPrint(PrintKind::Manual);

                // When the manual print failed, a scheduled print that is due is checked again soon too.
                if (IsScheduledPrintDue(SettingsStore::LoadSettings()))
                {
                    nextCheckMs = kNotReadyIntervalMs;
                }
            }
            else if (IsScheduledPrintDue(SettingsStore::LoadSettings()))
            {
                // A printer that is turned off or out of paper, or that a print failed on, is
                // checked again soon, so that the page prints shortly after the problem is fixed.
                const JobOutcome outcome = RunPrint(PrintKind::Scheduled);
                if (outcome.waiting || outcome.failure != FailureReason::None)
                {
                    nextCheckMs = kNotReadyIntervalMs;
                }
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

        const DWORD woken = ::WaitForMultipleObjects(3, wakeEvents, FALSE, nextCheckMs);
        if (woken == WAIT_OBJECT_0 + 2)
        {
            // The user may have fixed the problem by changing the settings, for example the paper
            // source, so a print that failed is tried again right away.
            m_retryTick = 0;
        }
        else if (woken != WAIT_OBJECT_0 + 1 && woken != WAIT_TIMEOUT)
        {
            return; // stop requested (or the wait failed)
        }
    }
}

PrintWorker::JobOutcome PrintWorker::RunPrint(PrintKind kind)
{
    const JobOutcome outcome = Print(kind);
    m_state.store(PrintState::Idle);

    // What a scheduled print found decides when it is tried again (see Print) and whether another
    // problem is news (see below).
    const FailureReason previousProblem = m_lastProblem;
    if (kind == PrintKind::Scheduled)
    {
        m_lastProblem = outcome.notReady ? outcome.failure : FailureReason::None;
        if (outcome.notReady && outcome.idleReport)
        {
            m_retryTick = 0; // the printer itself reports the problem, so it prints once that is fixed
        }
        else if (!outcome.notReady && outcome.failure != FailureReason::None)
        {
            m_retryTick = ::GetTickCount64() + kCheckIntervalMs;
        }
    }

    if (outcome.yielded)
    {
        return outcome; // the manual print, which runs next, prints the page
    }
    if (outcome.interrupted)
    {
        ServiceLog::Write(KindLabel(kind) + L" printing was cancelled because the service is stopping");
        return outcome;
    }
    if (outcome.skipped)
    {
        ServiceLog::Write(L"Scheduled printing was skipped because it is no longer due");
        return outcome;
    }
    if (outcome.waiting)
    {
        return outcome;
    }

    const uint64_t now = CurrentUtcTicks();
    if (outcome.failure == FailureReason::None)
    {
        SettingsStore::RecordSuccess(now);
        ServiceLog::Write(KindLabel(kind) + L" printing succeeded");
        return outcome;
    }

    // A scheduled failure is recorded, which also shows it in the UI, and logged when it is news:
    // the first failure since the last print (or mark as printed) or the end of a postponement,
    // or a different problem from the recorded one. The printer is checked every minute while a
    // due print waits, and what it reports can change briefly, for example while paper is loaded,
    // so another problem found by such a check is news only when the previous check found it too.
    // The same problem stays recorded with the time it was first found, and is logged again only
    // when a print was actually attempted.
    const PrintHistory history = SettingsStore::LoadHistory();
    const bool recorded =
        kind == PrintKind::Scheduled && IsRecordedFailure(SettingsStore::LoadSettings(), history, now);
    const bool changed = outcome.failure != history.lastScheduledFailure.reason &&
        (!outcome.notReady || outcome.failure == previousProblem);
    if (!recorded || changed)
    {
        SettingsStore::RecordFailure(kind, now, outcome.failure);
    }
    if (!recorded || changed || !outcome.notReady)
    {
        ServiceLog::Write(KindLabel(kind) + L" printing failed: " + FailureReasonDescription(outcome.failure));
    }
    return outcome;
}

PrintWorker::JobOutcome PrintWorker::Print(PrintKind kind)
{
    // The printer is checked before a scheduled print is announced, so that an offline or jammed
    // printer is reported right away instead of after a countdown that cannot print a page, and
    // again after the countdown. Checking a network printer can take a while, and the user may
    // postpone the print or mark the printer as printed meanwhile; then the print is skipped.
    const auto checkPrinter = [kind](const ServiceSettings& settings) -> std::optional<JobOutcome> {
        const Printing::PrinterStatus status = CheckReadiness(settings);
        if (status.failure == FailureReason::None)
        {
            return std::nullopt;
        }
        JobOutcome notReady;
        if (kind == PrintKind::Scheduled && !IsScheduledPrintDue(SettingsStore::LoadSettings()))
        {
            notReady.skipped = true;
            return notReady;
        }
        notReady.failure = status.failure;
        notReady.notReady = true;
        notReady.idleReport = status.idleReport;
        return notReady;
    };

    ServiceSettings settings = SettingsStore::LoadSettings();
    if (const std::optional<JobOutcome> notReady = checkPrinter(settings))
    {
        return *notReady;
    }

    if (kind == PrintKind::Scheduled)
    {
        // Some printers report a problem, such as being out of paper, only while they print a
        // page, and seem ready otherwise. So after a scheduled print failed, the page is printed
        // again after the regular 15 minutes, not every minute, or as soon as the printer is ready
        // again after it reported a problem itself while no document was waiting to print, which
        // means that the problem was fixed. A waiting document, such as the failed one, which was
        // cancelled but can stay queued for a while, can make the printer report a problem that
        // ends when the document leaves, fixed or not. A change of the settings or the schedule
        // ends the wait too (see Run).
        if (::GetTickCount64() < m_retryTick)
        {
            JobOutcome waiting;
            waiting.waiting = true;
            return waiting;
        }

        m_state.store(PrintState::Countdown);
        const Countdown countdown = WaitForCountdown();
        if (countdown != Countdown::Elapsed)
        {
            JobOutcome cancelled;
            cancelled.interrupted = countdown == Countdown::Stopping;
            cancelled.skipped = countdown == Countdown::NoLongerDue;
            cancelled.yielded = countdown == Countdown::ManualPrint;
            return cancelled;
        }

        // The user may have chosen another printer or image meanwhile.
        settings = SettingsStore::LoadSettings();
        if (const std::optional<JobOutcome> notReady = checkPrinter(settings))
        {
            return *notReady;
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

PrintWorker::Countdown PrintWorker::WaitForCountdown()
{
    const HANDLE events[] = { m_stopEvent, m_scheduleChanged.Get(), m_manualPrintRequested.Get() };
    const ULONGLONG deadline = ::GetTickCount64() + kCountdownMs;
    for (;;)
    {
        const ULONGLONG now = ::GetTickCount64();
        const DWORD remainingMs = now < deadline ? static_cast<DWORD>(deadline - now) : 0;
        const DWORD woken = ::WaitForMultipleObjects(3, events, FALSE, remainingMs);
        if (woken == WAIT_OBJECT_0 + 2)
        {
            // Print Now during the countdown prints one page, right away, rather than two. The
            // request is left for the Run loop, which then runs the manual print.
            ::SetEvent(m_manualPrintRequested.Get());
            return Countdown::ManualPrint;
        }
        if (woken != WAIT_TIMEOUT && woken != WAIT_OBJECT_0 + 1)
        {
            return Countdown::Stopping; // stop requested (or the wait failed)
        }

        // The user may have postponed the print, marked the printer as printed or turned
        // scheduled printing off, during the countdown or right at its end.
        if (!IsScheduledPrintDue(SettingsStore::LoadSettings()))
        {
            return Countdown::NoLongerDue;
        }
        if (woken == WAIT_TIMEOUT)
        {
            return Countdown::Elapsed;
        }
    }
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

    FailureReason failure = Printing::CheckPrinter(printerName).failure;
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

        failure = Printing::CheckPrinter(printerName).failure;
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
    return { false, Printing::CheckPrinter(printerName).failure };
}
