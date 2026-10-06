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
using PrintheadMaintainerUI.Enums;
using System;
using System.Globalization;

namespace PrintheadMaintainerUI.Presentation
{
    /// <summary>Formats times, counts and failure reasons the same way everywhere in the UI.</summary>
    public static class DisplayText
    {
        public static string Describe(FailureReason reason)
        {
            switch (reason)
            {
                case FailureReason.PrinterNotFound:
                    return "The printer was not found";
                case FailureReason.PrinterOffline:
                    return "The printer is offline";
                case FailureReason.PaperJam:
                    return "The paper is jammed";
                case FailureReason.PaperOut:
                    return "The printer is out of paper";
                case FailureReason.PaperProblem:
                    return "There is a problem with the paper";
                case FailureReason.DoorOpen:
                    return "A printer door is open";
                case FailureReason.OutOfInk:
                    return "The printer is out of ink or toner";
                case FailureReason.OutputBinFull:
                    return "The output bin is full";
                case FailureReason.ImageUnavailable:
                    return "The image for printing could not be loaded";
                case FailureReason.SpoolerError:
                    return "The print spooler reported an error";
                case FailureReason.JobNotCompleted:
                    return "The print job did not complete";
                default:
                    return "The printer reported an error";
            }
        }

        /// <summary>A UTC time as local time, for example "2026/10/06 14:30".</summary>
        public static string FormatTime(DateTime utc)
        {
            return utc.ToLocalTime().ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>For example "3 days ago"; times in the future (the clock was turned back) count as now.</summary>
        public static string FormatTimeAgo(DateTime utc, DateTime nowUtc)
        {
            TimeSpan elapsed = nowUtc - utc;
            if (elapsed.TotalDays >= 1)
            {
                return Count((int)elapsed.TotalDays, "day") + " ago";
            }
            if (elapsed.TotalHours >= 1)
            {
                return Count((int)elapsed.TotalHours, "hour") + " ago";
            }
            if (elapsed.TotalMinutes >= 1)
            {
                return Count((int)elapsed.TotalMinutes, "minute") + " ago";
            }
            return "Just now";
        }

        /// <summary>For example "1 day" or "7 days".</summary>
        public static string Count(int count, string unit)
        {
            return count.ToString(CultureInfo.InvariantCulture) + " " + (count == 1 ? unit : unit + "s");
        }
    }
}
