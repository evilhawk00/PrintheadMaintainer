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
#include "Printer.h"

#include <winspool.h>
#include <cstddef>
#include <optional>
#include <vector>

#include "../Common/WinHandle.h"

namespace
{
    struct PrinterHandleTraits
    {
        using Handle = HANDLE;
        static Handle Invalid() noexcept { return nullptr; }
        static void Close(Handle handle) noexcept { ::ClosePrinter(handle); }
    };

    struct DcTraits
    {
        using Handle = HDC;
        static Handle Invalid() noexcept { return nullptr; }
        static void Close(Handle handle) noexcept { ::DeleteDC(handle); }
    };

    struct BitmapTraits
    {
        using Handle = HBITMAP;
        static Handle Invalid() noexcept { return nullptr; }
        static void Close(Handle handle) noexcept { ::DeleteObject(handle); }
    };

    using UniquePrinter = UniqueHandle<PrinterHandleTraits>;
    using UniqueDc = UniqueHandle<DcTraits>;
    using UniqueBitmap = UniqueHandle<BitmapTraits>;

    struct StatusMapping
    {
        DWORD bits;
        FailureReason reason;
    };

    // Printer status bits that prevent printing, most specific first.
    constexpr StatusMapping kPrinterStatusErrors[] = {
        { PRINTER_STATUS_PAPER_JAM, FailureReason::PaperJam },
        { PRINTER_STATUS_PAPER_OUT, FailureReason::PaperOut },
        { PRINTER_STATUS_DOOR_OPEN, FailureReason::DoorOpen },
        { PRINTER_STATUS_NO_TONER, FailureReason::OutOfInk },
        { PRINTER_STATUS_PAPER_PROBLEM, FailureReason::PaperProblem },
        { PRINTER_STATUS_OUTPUT_BIN_FULL, FailureReason::OutputBinFull },
        { PRINTER_STATUS_OFFLINE | PRINTER_STATUS_NOT_AVAILABLE, FailureReason::PrinterOffline },
        { PRINTER_STATUS_ERROR | PRINTER_STATUS_OUT_OF_MEMORY, FailureReason::PrinterError },
    };

    // Job status bits that mean the job cannot be printed, most specific first.
    constexpr StatusMapping kJobStatusErrors[] = {
        { JOB_STATUS_PAPEROUT, FailureReason::PaperOut },
        { JOB_STATUS_OFFLINE, FailureReason::PrinterOffline },
        { JOB_STATUS_ERROR | JOB_STATUS_BLOCKED_DEVQ, FailureReason::PrinterError },
    };

    // Upper bound for the number of jobs inspected on one printer.
    constexpr DWORD kMaxJobsToInspect = 1024;

    template <size_t N>
    FailureReason MapStatus(DWORD status, const StatusMapping (&table)[N])
    {
        for (const StatusMapping& entry : table)
        {
            if (status & entry.bits)
            {
                return entry.reason;
            }
        }
        return FailureReason::None;
    }

    UniquePrinter OpenPrinterByName(const std::wstring& printerName)
    {
        std::wstring name = printerName; // OpenPrinterW takes a non-const string
        HANDLE handle = nullptr;
        if (!::OpenPrinterW(name.data(), &handle, nullptr))
        {
            return UniquePrinter();
        }
        return UniquePrinter(handle);
    }

    // Calls a spooler query that reports the required buffer size, growing the buffer and
    // retrying when the data changed in between (for example a job was added).
    // Returns ERROR_SUCCESS or the error of the last attempt.
    template <typename Query>
    DWORD QueryIntoBuffer(std::vector<BYTE>& buffer, Query query)
    {
        for (int attempt = 0; attempt < 3; ++attempt)
        {
            DWORD needed = 0;
            if (query(buffer.empty() ? nullptr : buffer.data(), static_cast<DWORD>(buffer.size()), &needed))
            {
                return ERROR_SUCCESS;
            }
            const DWORD error = ::GetLastError();
            if (error != ERROR_INSUFFICIENT_BUFFER || needed <= buffer.size())
            {
                return error;
            }
            buffer.resize(needed);
        }
        return ERROR_INSUFFICIENT_BUFFER;
    }

    // Lists the jobs in the printer's queue (JOB_INFO_1). Returns false if the queue cannot be read.
    bool EnumerateJobs(HANDLE printer, std::vector<BYTE>& buffer, DWORD& count)
    {
        count = 0;
        return QueryIntoBuffer(buffer, [&](BYTE* data, DWORD size, DWORD* needed) {
            return ::EnumJobsW(printer, 0, kMaxJobsToInspect, 1, data, size, needed, &count);
        }) == ERROR_SUCCESS;
    }

    // Whether the job is still in the printer's queue, or nothing if the queue cannot be read.
    std::optional<bool> IsJobQueued(HANDLE printer, DWORD jobId)
    {
        std::vector<BYTE> jobBuffer;
        DWORD jobCount = 0;
        if (EnumerateJobs(printer, jobBuffer, jobCount))
        {
            const auto* jobs = reinterpret_cast<const JOB_INFO_1W*>(jobBuffer.data());
            for (DWORD i = 0; i < jobCount; ++i)
            {
                if (jobs[i].JobId == jobId)
                {
                    return true;
                }
            }
            return false;
        }

        // Some print providers cannot list jobs; an empty queue still shows the job is gone.
        std::vector<BYTE> printerInfoBuffer;
        if (QueryIntoBuffer(printerInfoBuffer, [&](BYTE* buffer, DWORD size, DWORD* needed) {
                return ::GetPrinterW(printer, 2, buffer, size, needed);
            }) == ERROR_SUCCESS &&
            reinterpret_cast<const PRINTER_INFO_2W*>(printerInfoBuffer.data())->cJobs == 0)
        {
            return false;
        }
        return std::nullopt;
    }

    // Builds a DEVMODE that selects the requested paper source, merged and validated by the
    // printer driver. Returns an empty buffer to use the printer defaults.
    std::vector<BYTE> PrepareDevMode(HANDLE printer, std::wstring& printerName, short paperSource)
    {
        if (paperSource <= 0)
        {
            return {};
        }

        const LONG size = ::DocumentPropertiesW(nullptr, printer, printerName.data(), nullptr, nullptr, 0);
        if (size <= 0)
        {
            return {};
        }

        std::vector<BYTE> current(static_cast<size_t>(size));
        auto* devMode = reinterpret_cast<DEVMODEW*>(current.data());
        if (::DocumentPropertiesW(nullptr, printer, printerName.data(), devMode, nullptr, DM_OUT_BUFFER) != IDOK)
        {
            return {};
        }
        devMode->dmFields |= DM_DEFAULTSOURCE;
        devMode->dmDefaultSource = paperSource;

        std::vector<BYTE> merged(static_cast<size_t>(size));
        if (::DocumentPropertiesW(nullptr, printer, printerName.data(), reinterpret_cast<DEVMODEW*>(merged.data()),
                devMode, DM_IN_BUFFER | DM_OUT_BUFFER) == IDOK)
        {
            return merged;
        }
        return current;
    }
}

namespace Printing
{
    FailureReason CheckPrinter(const std::wstring& printerName)
    {
        UniquePrinter printer = OpenPrinterByName(printerName);
        if (!printer)
        {
            return FailureReason::PrinterNotFound;
        }

        std::vector<BYTE> printerInfoBuffer;
        DWORD error = QueryIntoBuffer(printerInfoBuffer, [&](BYTE* buffer, DWORD size, DWORD* needed) {
            return ::GetPrinterW(printer.Get(), 2, buffer, size, needed);
        });
        if (error != ERROR_SUCCESS)
        {
            return FailureReason::SpoolerError;
        }

        const auto* printerInfo = reinterpret_cast<const PRINTER_INFO_2W*>(printerInfoBuffer.data());
        FailureReason reason = MapStatus(printerInfo->Status, kPrinterStatusErrors);
        if (reason != FailureReason::None || printerInfo->cJobs == 0)
        {
            return reason;
        }

        // Many printers report problems only on the job they are printing. This check is an
        // addition to the printer status, so a print provider that cannot list jobs is not an error.
        std::vector<BYTE> jobBuffer;
        DWORD jobCount = 0;
        if (!EnumerateJobs(printer.Get(), jobBuffer, jobCount))
        {
            return FailureReason::None;
        }

        const auto* jobs = reinterpret_cast<const JOB_INFO_1W*>(jobBuffer.data());
        for (DWORD i = 0; i < jobCount; ++i)
        {
            if (jobs[i].Status & JOB_STATUS_PRINTING)
            {
                reason = MapStatus(jobs[i].Status, kJobStatusErrors);
                if (reason != FailureReason::None)
                {
                    return reason;
                }
            }
        }
        return FailureReason::None;
    }

    SubmitResult SubmitBitmapJob(const std::wstring& printerName, short paperSource,
        const std::wstring& bitmapPath, const std::wstring& documentName)
    {
        UniquePrinter printer = OpenPrinterByName(printerName);
        if (!printer)
        {
            return { 0, FailureReason::PrinterNotFound };
        }

        // Load the image before starting the document so that a missing image never
        // leaves an unfinished job in the queue. As a DIB section the image keeps its own
        // pixel format instead of being converted to the screen's.
        UniqueBitmap bitmap(static_cast<HBITMAP>(
            ::LoadImageW(nullptr, bitmapPath.c_str(), IMAGE_BITMAP, 0, 0, LR_LOADFROMFILE | LR_CREATEDIBSECTION)));
        BITMAP bitmapInfo{};
        if (!bitmap || ::GetObjectW(bitmap.Get(), sizeof(bitmapInfo), &bitmapInfo) == 0)
        {
            return { 0, FailureReason::ImageUnavailable };
        }

        std::wstring name = printerName;
        const std::vector<BYTE> devMode = PrepareDevMode(printer.Get(), name, paperSource);
        UniqueDc printerDc(::CreateDCW(L"WINSPOOL", name.c_str(), nullptr,
            devMode.empty() ? nullptr : reinterpret_cast<const DEVMODEW*>(devMode.data())));
        UniqueDc memoryDc(::CreateCompatibleDC(nullptr));
        if (!printerDc || !memoryDc)
        {
            return { 0, FailureReason::SpoolerError };
        }

        DOCINFOW docInfo{};
        docInfo.cbSize = sizeof(docInfo);
        docInfo.lpszDocName = documentName.c_str();

        // StartDoc returns the spooler job id, which identifies our job unambiguously.
        const int jobId = ::StartDocW(printerDc.Get(), &docInfo);
        if (jobId <= 0)
        {
            return { 0, FailureReason::SpoolerError };
        }

        bool pagePrinted = false;
        if (::StartPage(printerDc.Get()) > 0)
        {
            // Halftone averages the pixels when the image has to be shrunk, instead of dropping
            // rows and columns; it needs the brush origin to be set.
            ::SetStretchBltMode(printerDc.Get(), HALFTONE);
            ::SetBrushOrgEx(printerDc.Get(), 0, 0, nullptr);

            const HGDIOBJ previous = ::SelectObject(memoryDc.Get(), bitmap.Get());
            if (previous != nullptr)
            {
                const BOOL stretched = ::StretchBlt(printerDc.Get(), 0, 0,
                    ::GetDeviceCaps(printerDc.Get(), HORZRES), ::GetDeviceCaps(printerDc.Get(), VERTRES),
                    memoryDc.Get(), 0, 0, bitmapInfo.bmWidth, bitmapInfo.bmHeight, SRCCOPY);
                ::SelectObject(memoryDc.Get(), previous);
                pagePrinted = stretched != FALSE;
            }
            pagePrinted = ::EndPage(printerDc.Get()) > 0 && pagePrinted;
        }

        if (!pagePrinted)
        {
            ::AbortDoc(printerDc.Get());
            return { 0, FailureReason::SpoolerError };
        }
        if (::EndDoc(printerDc.Get()) <= 0)
        {
            CancelJob(printerName, static_cast<DWORD>(jobId));
            return { 0, FailureReason::SpoolerError };
        }
        return { static_cast<DWORD>(jobId), FailureReason::None };
    }

    JobStatus QueryJob(const std::wstring& printerName, DWORD jobId)
    {
        UniquePrinter printer = OpenPrinterByName(printerName);
        if (!printer)
        {
            return { JobState::Unknown, FailureReason::PrinterNotFound };
        }

        std::vector<BYTE> jobBuffer;
        const DWORD error = QueryIntoBuffer(jobBuffer, [&](BYTE* buffer, DWORD size, DWORD* needed) {
            return ::GetJobW(printer.Get(), jobId, 1, buffer, size, needed);
        });
        if (error != ERROR_SUCCESS)
        {
            // The Windows spooler reports ERROR_INVALID_PARAMETER once the job has left the queue,
            // but other print providers differ, so check the queue itself.
            if (IsJobQueued(printer.Get(), jobId) == false)
            {
                return { JobState::Completed, FailureReason::None };
            }
            return { JobState::Unknown, FailureReason::SpoolerError };
        }

        const DWORD status = reinterpret_cast<const JOB_INFO_1W*>(jobBuffer.data())->Status;
        if (status & (JOB_STATUS_PRINTED | JOB_STATUS_COMPLETE))
        {
            // Also covers printers that keep printed documents in the queue.
            return { JobState::Completed, FailureReason::None };
        }

        const FailureReason failure = MapStatus(status, kJobStatusErrors);
        if (failure != FailureReason::None)
        {
            return { JobState::Failed, failure };
        }
        if (status & (JOB_STATUS_DELETING | JOB_STATUS_DELETED))
        {
            return { JobState::Failed, FailureReason::JobNotCompleted };
        }
        return { JobState::Pending, FailureReason::None };
    }

    void CancelJob(const std::wstring& printerName, DWORD jobId)
    {
        UniquePrinter printer = OpenPrinterByName(printerName);
        if (printer)
        {
            ::SetJobW(printer.Get(), jobId, 0, nullptr, JOB_CONTROL_DELETE);
        }
    }
}
