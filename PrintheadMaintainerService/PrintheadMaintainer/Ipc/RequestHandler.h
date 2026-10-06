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

#include <string>

#include "Message.h"

class PrintWorker;

// Requests and their responses; fields are optional unless noted. The UI side is
// NamedPipeClient/ServiceClient.cs.
//
//   GetStatus              -> OK with PrintState (Idle|Countdown|Printing), ManualPrintPending,
//                             Enabled, IntervalDays, PrinterName, PaperSource, CustomImage,
//                             ImageSourceName, ImagePath, ImageAvailable, LastPrint,
//                             LastMarkedPrint, LastScheduledFailure, LastScheduledFailureReason,
//                             LastManualFailure, LastManualFailureReason, NextScheduledPrint and
//                             NextScheduledPrintPostponed (it is the end of a postponement).
//                             Times are FILETIME ticks in UTC; 0 means none. Flags are 0 or 1.
//
//   ApplySettings          -> OK, InvalidValue (with Field), PrinterUnavailable or StorageError.
//     Enabled, IntervalDays, PrinterName, PaperSource: the values to change.
//     Image=Default        print the image installed with the program again.
//     Image=Custom         with ImageWidth, ImageHeight, optional ImageSourceName and the pixels
//                          as binary data (see PrintImage.h).
//     Every value is validated before anything is stored.
//
//   PrintNow               -> OK (queued), Busy or NotConfigured.
//
//   MarkPrinted            -> OK, InvalidValue (with Field) or StorageError. The printer counts
//                             as printed now, for example because something else was printed
//                             on it, so the schedule counts from now.
//     Clear=1              removes the mark instead.
//
//   Postpone               -> OK, InvalidValue (with Field) or StorageError.
//     Until                (required) no scheduled print before this time, at most 730 days
//                          ahead; 0 ends the postponement now.
//
// Any request can also be answered with InvalidRequest, UnknownCommand or InternalError.
namespace Ipc
{
    constexpr wchar_t kResultInternalError[] = L"InternalError";
    constexpr wchar_t kResultInvalidRequest[] = L"InvalidRequest";

    // clientName identifies the caller in the service log.
    Message HandleRequest(const Message& request, const std::wstring& clientName, PrintWorker& worker);
}
