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
#include <optional>
#include <string>

#include "FailureReason.h"

namespace Printing
{
    // Looks the name up among the printers the service can use: local printers and printer
    // connections installed for all users (connections a user added only for their own account
    // are not visible to the service). Returns the name as the spooler spells it, or nothing.
    // Only names found here are accepted from the UI, so that a user cannot make the service open
    // a printer on another computer (\\host\printer), which would sign in to that host.
    std::optional<std::wstring> FindInstalledPrinter(const std::wstring& printerName);

    // Returns the first problem reported by the printer itself or by a job it is printing,
    // or FailureReason::None when nothing prevents printing.
    FailureReason CheckPrinter(const std::wstring& printerName);

    struct SubmitResult
    {
        DWORD jobId = 0;
        FailureReason failure = FailureReason::None;
    };

    // Prints the bitmap file stretched over the printable area of one page.
    // paperSource is a DEVMODE dmDefaultSource value; 0 keeps the printer default.
    // On success the result carries the spooler job id assigned by StartDoc.
    SubmitResult SubmitBitmapJob(const std::wstring& printerName, short paperSource,
        const std::wstring& bitmapPath, const std::wstring& documentName);

    enum class JobState
    {
        Pending,   // still queued or printing
        Completed, // printed, or no longer in the queue
        Failed,    // the job reports an error or was cancelled
        Unknown,   // the queue could not be queried
    };

    struct JobStatus
    {
        JobState state = JobState::Unknown;
        FailureReason failure = FailureReason::None;
    };

    JobStatus QueryJob(const std::wstring& printerName, DWORD jobId);

    // Removes the job from the queue if it is still there.
    void CancelJob(const std::wstring& printerName, DWORD jobId);
}
