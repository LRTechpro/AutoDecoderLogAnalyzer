using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AutoDecoder.Models;
using AutoDecoder.Protocols.Utilities;

namespace AutoDecoder.Protocols.Classifiers;

// Class responsible for classifying raw log lines into appropriate LogLine types
public static class LineClassifier
{
    // Regex: ISO-8601 timestamp at start of line, then the rest of the message
    // Examples matched:
    // 2025-10-21T10:23:45.123 ISO15765 ...
    // 2025-10-21T10:23:45 ISO15765 ...
    private static readonly Regex _tsPrefix = new(
        @"^(?<ts>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?)\s+(?<rest>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Classify a raw line and return the appropriate LogLine object
    public static LogLine Classify(int lineNumber, string rawLine)
    {
        // Check if rawLine is null or empty
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            // Return UnknownLine for empty input
            return new UnknownLine(lineNumber, rawLine ?? string.Empty, "Empty or null line");
        }

        // 1) Extract timestamp prefix (if present) so it lands in the Timestamp column,
        //    and the Raw column shows the "rest" content (ISO15765..., DEBUG..., etc.).
        DateTime? ts = null;
        string content = rawLine;

        var m = _tsPrefix.Match(rawLine);
        if (m.Success)
        {
            var tsText = m.Groups["ts"].Value;
            var rest = m.Groups["rest"].Value;

            // Try parse as round-trip / ISO style
            if (DateTime.TryParse(
                    tsText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                    out var parsed))
            {
                ts = parsed;
                content = rest;
            }
        }

        // 2) Classify based on the content WITHOUT the timestamp prefix
        LogLine line;

        // Priority 1: Check for XML format (contains "<" and "didValue=" or "<ns")
        if ((content.Contains("<") && content.Contains("didValue=")) || content.Contains("<ns"))
        {
            // Return XmlLine for XML content
            line = new XmlLine(lineNumber, content);
        }
        // Priority 2: Check for ISO 15765 format (case-insensitive search for "ISO15765")
        else if (content.IndexOf("ISO15765", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Return Iso15765Line for ISO 15765 protocol
            line = new Iso15765Line(lineNumber, content);
        }
        // Priority 3: Check for hex format with bracket notation
        else if (HexTools.TryParseBracketHexBytes(content, out _))
        {
            // Return HexLine for bracket hex notation
            line = new HexLine(lineNumber, content);
        }
        // Priority 4: Check for long hex string format
        else if (HexTools.TryParseLongHexString(content, out _))
        {
            // Return HexLine for long hex string
            line = new HexLine(lineNumber, content);
        }
        else
        {
            // Priority 5: Check for ASCII format (mostly printable characters)
            int printableCount = content.Count(c => c >= 32 && c <= 126);
            double printablePercent = content.Length > 0 ? (double)printableCount / content.Length * 100.0 : 0.0;

            // Check if line is mostly ASCII (80% or more printable) and no brackets
            if (printablePercent >= 80 && !content.Contains('['))
            {
                // Return AsciiLine for ASCII text
                line = new AsciiLine(lineNumber, content);
            }
            else
            {
                // Priority 6: No match found
                line = new UnknownLine(lineNumber, content, "No matching pattern found");
            }
        }

        // 3) Apply extracted timestamp to the model so the grid "Timestamp" column populates.
        //    Assumes LogLine.Timestamp is DateTime? (or compatible nullable type).
        if (m.Success)
        {
            var tsText = m.Groups["ts"].Value;

            if (DateTime.TryParse(
                    tsText,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeLocal,
                    out var parsedTs))
            {
                line.Timestamp = parsedTs;
            }
        }

        return line;
    }
}