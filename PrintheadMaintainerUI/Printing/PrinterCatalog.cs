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
using PrintheadMaintainerUI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Linq;

namespace PrintheadMaintainerUI.Printing
{
    /// <summary>
    /// Looks up the printers installed on this computer and their paper sources. Both ask the
    /// print spooler and printer drivers, which can be slow, so call them from a background thread.
    /// </summary>
    public static class PrinterCatalog
    {
        /// <summary>The paper source value that lets the printer choose.</summary>
        public const int DefaultPaperSource = 0;

        // The service stores the paper source as a DEVMODE dmDefaultSource, which is a short.
        private const int MaxPaperSource = short.MaxValue;

        /// <summary>Printer names sorted alphabetically.</summary>
        /// <exception cref="Win32Exception">The print spooler is not available.</exception>
        public static IReadOnlyList<string> GetInstalledPrinters()
        {
            return PrinterSettings.InstalledPrinters.Cast<string>()
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The paper sources of the printer, starting with the printer default. Sources the driver
        /// cannot report are left out; an unknown or unavailable printer has only the default.
        /// </summary>
        public static IReadOnlyList<PaperSourceOption> GetPaperSources(string printerName)
        {
            var sources = new List<PaperSourceOption>();
            if (!string.IsNullOrEmpty(printerName))
            {
                try
                {
                    var settings = new PrinterSettings { PrinterName = printerName };
                    if (settings.IsValid)
                    {
                        foreach (PaperSource source in settings.PaperSources)
                        {
                            int rawKind = source.RawKind;
                            if (rawKind > DefaultPaperSource && rawKind <= MaxPaperSource &&
                                sources.All(existing => existing.RawKind != rawKind))
                            {
                                string name = string.IsNullOrWhiteSpace(source.SourceName)
                                    ? source.Kind.ToString()
                                    : source.SourceName;
                                sources.Add(new PaperSourceOption(name, rawKind));
                            }
                        }
                    }
                }
                catch (Exception e) when (e is Win32Exception || e is InvalidPrinterException)
                {
                    // Keep whatever was found; the printer default is always available.
                }
            }

            sources.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.CurrentCultureIgnoreCase));
            sources.Insert(0, new PaperSourceOption("(Use printer default)", DefaultPaperSource));
            return sources;
        }
    }
}
