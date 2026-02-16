using AutoDecoder.Models;

namespace AutoDecoder.Decoders;

// Class responsible for classifying raw log lines into appropriate LogLine types
public static class LineClassifier
{
    // Classify a raw line and return the appropriate LogLine object
    public static LogLine Classify(int lineNumber, string rawLine)
    {
        // Check if rawLine is null or empty
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            // Return UnknownLine for empty input
            return new UnknownLine(lineNumber, rawLine ?? string.Empty, "Empty or null line");
        }

        // Priority 1: Check for XML format (contains "<" and "didValue=" or "<ns")
        if ((rawLine.Contains("<") && rawLine.Contains("didValue=")) || rawLine.Contains("<ns"))
        {
            // Return XmlLine for XML content
            return new XmlLine(lineNumber, rawLine);
        }

        // Priority 2: Check for ISO 15765 format (case-insensitive search for "ISO15765")
        if (rawLine.IndexOf("ISO15765", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Return Iso15765Line for ISO 15765 protocol (handles timestamp prefixes)
            return new Iso15765Line(lineNumber, rawLine);
        }

        // Priority 3: Check for hex format with bracket notation (improved bracket parser)
        if (HexTools.TryParseBracketHexBytes(rawLine, out _))
        {
            // Return HexLine for bracket hex notation
            return new HexLine(lineNumber, rawLine);
        }

        // Priority 4: Check for long hex string format
        if (HexTools.TryParseLongHexString(rawLine, out _))
        {
            // Return HexLine for long hex string
            return new HexLine(lineNumber, rawLine);
        }

        // Priority 5: Check for ASCII format (mostly printable characters)
        int printableCount = rawLine.Count(c => c >= 32 && c <= 126);
        // Calculate percentage of printable characters
        double printablePercent = rawLine.Length > 0 ? (double)printableCount / rawLine.Length * 100 : 0;

        // Check if line is mostly ASCII (80% or more printable) and no brackets
        if (printablePercent >= 80 && !rawLine.Contains('['))
        {
            // Return AsciiLine for ASCII text (excluding lines with brackets)
            return new AsciiLine(lineNumber, rawLine);
        }

        // Priority 6: No match found, return UnknownLine
        return new UnknownLine(lineNumber, rawLine, "No matching pattern found");
    }
}
