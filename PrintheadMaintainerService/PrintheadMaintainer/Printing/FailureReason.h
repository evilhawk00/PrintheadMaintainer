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

// Why a print attempt failed. The numeric values are stored in the registry and the
// names are sent to the UI, so never renumber or rename an existing value.
enum class FailureReason : DWORD
{
    None = 0,
    PrinterNotFound = 1,   // the printer does not exist or is not visible to the service
    PrinterOffline = 2,
    PaperJam = 3,
    PaperOut = 4,
    PaperProblem = 5,
    DoorOpen = 6,
    OutOfInk = 7,
    OutputBinFull = 8,
    PrinterError = 9,      // any other error reported by the printer or its print job
    ImageUnavailable = 10, // the image to print is missing or cannot be loaded
    SpoolerError = 11,     // a print spooler or GDI call failed
    JobNotCompleted = 12,  // the job did not leave the queue in time or was cancelled
};

// Converts a value read from storage; unknown values become PrinterError.
FailureReason FailureReasonFromValue(DWORD value);

// Stable identifier used by the IPC protocol, for example L"PaperJam".
const wchar_t* FailureReasonName(FailureReason reason);

// Short English description used in the service log, for example L"paper jam".
const wchar_t* FailureReasonDescription(FailureReason reason);
