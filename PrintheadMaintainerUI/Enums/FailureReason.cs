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
namespace PrintheadMaintainerUI.Enums
{
    /// <summary>
    /// Why a print attempt failed. The names match the ones the service sends
    /// (Printing/FailureReason.h in the service).
    /// </summary>
    public enum FailureReason
    {
        PrinterError, // any other error reported by the printer, also used for unknown names
        PrinterNotFound,
        PrinterOffline,
        PaperJam,
        PaperOut,
        PaperProblem,
        DoorOpen,
        OutOfInk,
        OutputBinFull,
        ImageUnavailable,
        SpoolerError,
        JobNotCompleted,
    }
}
