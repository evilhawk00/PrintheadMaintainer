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
#include "FailureReason.h"

FailureReason FailureReasonFromValue(DWORD value)
{
    if (value > static_cast<DWORD>(FailureReason::JobNotCompleted))
    {
        return FailureReason::PrinterError;
    }
    return static_cast<FailureReason>(value);
}

const wchar_t* FailureReasonName(FailureReason reason)
{
    switch (reason)
    {
    case FailureReason::None: return L"None";
    case FailureReason::PrinterNotFound: return L"PrinterNotFound";
    case FailureReason::PrinterOffline: return L"PrinterOffline";
    case FailureReason::PaperJam: return L"PaperJam";
    case FailureReason::PaperOut: return L"PaperOut";
    case FailureReason::PaperProblem: return L"PaperProblem";
    case FailureReason::DoorOpen: return L"DoorOpen";
    case FailureReason::OutOfInk: return L"OutOfInk";
    case FailureReason::OutputBinFull: return L"OutputBinFull";
    case FailureReason::PrinterError: return L"PrinterError";
    case FailureReason::ImageUnavailable: return L"ImageUnavailable";
    case FailureReason::SpoolerError: return L"SpoolerError";
    case FailureReason::JobNotCompleted: return L"JobNotCompleted";
    }
    return L"PrinterError";
}

const wchar_t* FailureReasonDescription(FailureReason reason)
{
    switch (reason)
    {
    case FailureReason::None: return L"no error";
    case FailureReason::PrinterNotFound: return L"the printer was not found";
    case FailureReason::PrinterOffline: return L"the printer is offline";
    case FailureReason::PaperJam: return L"paper jam";
    case FailureReason::PaperOut: return L"out of paper";
    case FailureReason::PaperProblem: return L"paper problem";
    case FailureReason::DoorOpen: return L"a printer door is open";
    case FailureReason::OutOfInk: return L"out of ink or toner";
    case FailureReason::OutputBinFull: return L"the output bin is full";
    case FailureReason::PrinterError: return L"the printer reported an error";
    case FailureReason::ImageUnavailable: return L"the image could not be loaded";
    case FailureReason::SpoolerError: return L"the print spooler reported an error";
    case FailureReason::JobNotCompleted: return L"the print job did not complete";
    }
    return L"the printer reported an error";
}
