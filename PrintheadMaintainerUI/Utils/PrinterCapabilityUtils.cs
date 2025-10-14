using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using PrintheadMaintainerUI.Models;

namespace PrintheadMaintainerUI.Utils
{
    public static class PrinterCapabilityUtils
    {
        public static IReadOnlyList<PaperSourceOption> GetPaperSources(string printerName)
        {
            var results = new List<PaperSourceOption>();

            if (string.IsNullOrWhiteSpace(printerName))
            {
                return results;
            }

            try
            {
                var printerSettings = new PrinterSettings { PrinterName = printerName };
                if (!printerSettings.IsValid)
                {
                    return results;
                }

                var seen = new HashSet<int>();
                foreach (PaperSource source in printerSettings.PaperSources)
                {
                    if (source == null)
                    {
                        continue;
                    }

                    int rawKind = source.RawKind;
                    if (rawKind <= 0 || !seen.Add(rawKind))
                    {
                        continue;
                    }

                    string displayName = string.IsNullOrWhiteSpace(source.SourceName)
                        ? source.Kind.ToString()
                        : source.SourceName;

                    results.Add(new PaperSourceOption(displayName, rawKind));
                }
            }
            catch (Exception)
            {
                // Swallow exceptions and return whatever we managed to collect.
            }

            results.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.CurrentCultureIgnoreCase));
            return results;
        }
    }
}
