using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AutoDecoder.Models;

// Abstract base class for all log line types
public abstract class LogLine
{
    // Backing fields (encapsulation)
    private readonly string _raw;
    private readonly int _lineNumber;

    // Common identity
    public int LineNumber => _lineNumber;
    public string Raw => _raw;

    // Derived classes must report their type
    public abstract LineType Type { get; }

    // Decoding output
    public string Summary { get; protected set; } = string.Empty;
    public string Details { get; protected set; } = string.Empty;
    public double Confidence { get; protected set; } = 0.0;

    // ✅ FIX: Timestamp should be a nullable DateTime (because other code uses .HasValue/.Value)
    public DateTime? Timestamp { get; set; }

    // ✅ Optional: a UI-friendly timestamp string (DataGridView can bind to this instead)
    public string TimestampText => Timestamp.HasValue
        ? Timestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture)
        : string.Empty;

    // CAN arbitration ID + friendly node label for UI
    public int? CanId { get; protected set; }
    public string? CanNode { get; protected set; }

    public ushort? Did { get; set; }

    public ushort? UdsDid { get; set; }
    public byte? UdsSid { get; set; }
    public byte? UdsNrc { get; set; }

    protected LogLine(int lineNumber, string raw)
    {
        _lineNumber = lineNumber;
        _raw = raw ?? string.Empty;
    }

    // Derived classes parse their format and fill Summary/Details/Confidence/etc.
    public abstract void ParseAndDecode();

    // Helper: extract bytes inside [...]
    public byte[]? ExtractHexBytes()
    {
        if (string.IsNullOrWhiteSpace(Raw))
            return null;

        int start = Raw.IndexOf('[');
        int end = Raw.IndexOf(']');

        if (start < 0 || end < 0 || end <= start)
            return null;

        string inside = Raw.Substring(start + 1, end - start - 1);

        var parts = inside
            .Split(',')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        var bytes = new List<byte>(parts.Length);

        foreach (var part in parts)
        {
            if (byte.TryParse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
                bytes.Add(b);
        }

        return bytes.Count > 0 ? bytes.ToArray() : null;
    }

    // ✅ Helper: safely parse common timestamp prefixes used in your logs
    protected static bool TryParseTimestamp(string rawLine, out DateTime timestamp)
    {
        timestamp = default;

        if (string.IsNullOrWhiteSpace(rawLine))
            return false;

        // Try the first token (before a space) as an ISO-ish timestamp.
        string firstToken = rawLine.Split(' ', '\t').FirstOrDefault() ?? "";
        if (firstToken.Length < 10)
            return false;

        // Common formats: adjust/add if your logs vary.
        string[] formats =
        {
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.ff",
            "yyyy-MM-ddTHH:mm:ss.f",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss"
        };

        return DateTime.TryParseExact(
            firstToken,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out timestamp
        );
    }
}