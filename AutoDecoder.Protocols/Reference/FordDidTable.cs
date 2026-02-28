#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AutoDecoder.Protocols.Reference
{
    public static class FordDidTable
    {
        private static readonly Dictionary<ushort, string> _didToMeaning = new();

        public static void LoadFromCsv(string csvPath)
        {
            _didToMeaning.Clear();

            if (!File.Exists(csvPath))
                throw new FileNotFoundException("DID CSV not found.", csvPath);

            // Expected CSV columns:
            // DID_HEX,Type,NameOrCategory,Description
            // Example:
            // F110,PartNumber,,Part number (data record)

            foreach (var rawLine in File.ReadLines(csvPath))
            {
                var line = rawLine.Trim();

                // skip blanks and comments
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("#")) continue;

                var parts = line.Split(',');
                if (parts.Length < 1) continue;

                var didHex = parts[0].Trim().Replace("0x", "", StringComparison.OrdinalIgnoreCase);

                if (!ushort.TryParse(didHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var did))
                    continue;

                // Build a friendly meaning string from remaining columns
                var type = parts.Length > 1 ? parts[1].Trim() : "";
                var name = parts.Length > 2 ? parts[2].Trim() : "";
                var desc = parts.Length > 3 ? parts[3].Trim() : "";

                string meaning = string.Join(" — ", new[] { type, name, desc }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (string.IsNullOrWhiteSpace(meaning))
                    meaning = "Ford DID";

                _didToMeaning[did] = meaning;
            }
        }

        public static string MeaningOrUnknown(ushort did)
            => _didToMeaning.TryGetValue(did, out var meaning) ? meaning : $"UnknownDID(0x{did:X4})";
    }
}