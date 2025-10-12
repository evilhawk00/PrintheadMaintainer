using System;

namespace PrintheadMaintainerUI.Models
{
    public sealed class PaperSourceOption
    {
        public PaperSourceOption(string displayName, int rawKind)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            RawKind = rawKind;
        }

        public string DisplayName { get; }

        public int RawKind { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
