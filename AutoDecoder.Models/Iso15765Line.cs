#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoDecoder.Models;




public sealed class Iso15765Line : LogLine
{
    private string _direction = "UNK";
    private byte[]? _allBytes;

    // Builders depend on these:
    public string Direction => string.IsNullOrWhiteSpace(_direction) ? "UNK" : _direction;
    public byte[] PayloadBytes => _allBytes ?? Array.Empty<byte>();
    public byte[] IdBytes => (_allBytes != null && _allBytes.Length >= 4) ? _allBytes.Take(4).ToArray() : Array.Empty<byte>();
    public byte[] UdsBytes => (_allBytes != null && _allBytes.Length > 4) ? _allBytes.Skip(4).ToArray() : Array.Empty<byte>();

    public override LineType Type => LineType.Iso15765;

    public Iso15765Line(int lineNumber, string raw) : base(lineNumber, raw) { }

    public override void ParseAndDecode()
    {
        Summary = "ISO15765 line";
        Details = $"Raw: {Raw}";
        Confidence = 0.5;

        DetectDirection();

        if (!TryExtractBracketBytes(Raw, out _allBytes))
        {
            Summary = $"ISO15765 {Direction} (metadata only)";
            Details = $"Direction: {Direction}\nNo payload brackets found.";
            Confidence = 0.4;
            return;
        }

        // ✅ FIX: CanId is int? (not int?), and CanNode is set here
        CanId = TryExtractCanId(_allBytes);

        if (CanId.HasValue)
            CanNode = ModuleAddressBook.Format(CanId.Value);
        else
            CanNode = null;

        if (_allBytes.Length <= 4)
        {
            string idHex = BitConverter.ToString(_allBytes).Replace("-", " ");
            Summary = $"ISO15765 {Direction} (ID only, no UDS payload)";
            Details =
                $"Direction: {Direction}\n" +
                $"ID/Bytes: [{idHex}]\n" +
                $"CAN ID: {FormatCanId(CanId)}\n" +
                $"Node: {CanNode ?? "(unknown)"}\n" +
                "Not enough bytes for UDS payload (need >4 bytes)";
            Confidence = 0.5;
            return;
        }

        DecodeWithIdHeader();
    }

    private void DetectDirection()
    {
        if (Raw.IndexOf(" TX ", StringComparison.OrdinalIgnoreCase) >= 0 ||
            Raw.IndexOf("->", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _direction = "TX";
        }
        else if (Raw.IndexOf(" RX ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 Raw.IndexOf("<-", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _direction = "RX";
        }
        else
        {
            _direction = "UNK";
        }
    }

    private static bool TryExtractBracketBytes(string raw, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();

        int bracketStart = raw.IndexOf('[');
        int bracketEnd = bracketStart >= 0 ? raw.IndexOf(']', bracketStart + 1) : -1;
        if (bracketStart < 0 || bracketEnd <= bracketStart) return false;

        string bracketContent = raw.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
        string[] parts = bracketContent.Split(',');

        var list = new List<byte>(parts.Length);
        foreach (var p in parts)
        {
            var t = p.Trim();
            if (t.Length == 0) continue;

            if (byte.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                list.Add(b);
        }

        bytes = list.ToArray();
        return bytes.Length > 0;
    }

    private void DecodeWithIdHeader()
    {
        if (_allBytes == null || _allBytes.Length <= 4) return;

        byte[] idBytes = _allBytes.Take(4).ToArray();
        byte[] udsPayload = _allBytes.Skip(4).ToArray();

        var frame = ParseUdsPayload(udsPayload, idBytes);

        Summary = BuildReportSummary(frame);
        Details = BuildTechnicalBreakdown(frame);
        Confidence = frame.IsNegativeResponse ? 1.0 : 0.9;
    }

    private ParsedUdsFrame ParseUdsPayload(byte[] udsPayload, byte[] idBytes)
    {
        var frame = new ParsedUdsFrame
        {
            Direction = Direction,
            IdBytes = idBytes,
            UdsPayload = udsPayload
        };

        if (udsPayload.Length < 1)
            return frame;

        byte sid = udsPayload[0];

        // Request: 0x22 <DIDhi> <DIDlo>
        if (sid == 0x22 && udsPayload.Length >= 3)
        {
            frame.IsRequest = true;
            frame.ServiceId = 0x22;
            frame.ServiceName = UdsLookup.GetServiceName(0x22);

            frame.Did = (ushort)((udsPayload[1] << 8) | udsPayload[2]);
            frame.DidName = UdsLookup.GetDidName(frame.Did.Value);
            return frame;
        }

        // Negative response: 0x7F <origSID> <NRC> [optional context bytes...]
        if (sid == 0x7F && udsPayload.Length >= 3)
        {
            frame.IsNegativeResponse = true;

            byte originalSid = udsPayload[1];
            byte nrc = udsPayload[2];

            frame.ServiceId = originalSid;
            frame.ServiceName = UdsLookup.GetServiceName(originalSid);

            frame.Nrc = nrc;
            frame.NrcName = UdsLookup.GetNrcMeaning(nrc);

            // [0x7F, 0x22, NRC, DIDhi, DIDlo]
            if (originalSid == 0x22 && udsPayload.Length >= 5)
            {
                frame.Did = (ushort)((udsPayload[3] << 8) | udsPayload[4]);
                frame.DidName = UdsLookup.GetDidName(frame.Did.Value);
            }

            return frame;
        }

        // Positive response to 0x22 is 0x62
        if (sid == 0x62 && udsPayload.Length >= 3)
        {
            frame.IsPositiveResponse = true;

            frame.ServiceId = 0x22;
            frame.ServiceName = UdsLookup.GetServiceName(0x22);

            frame.Did = (ushort)((udsPayload[1] << 8) | udsPayload[2]);
            frame.DidName = UdsLookup.GetDidName(frame.Did.Value);

            frame.DataBytes = udsPayload.Length > 3 ? udsPayload[3..] : Array.Empty<byte>();
            return frame;
        }

        // Unknown/other service
        frame.ServiceId = sid;
        frame.ServiceName = UdsLookup.GetServiceName(sid);
        return frame;
    }

    private string BuildReportSummary(ParsedUdsFrame frame)
    {
        // Include Node if available
        string nodePart = CanNode != null ? $" | {CanNode}" : "";

        if (frame.IsRequest && frame.Did.HasValue)
            return $"UDS {frame.ServiceName} (0x{frame.ServiceId:X2}) → DID 0x{frame.Did.Value:X4}{nodePart}";

        if (frame.IsNegativeResponse && frame.Nrc.HasValue)
        {
            if (frame.Did.HasValue)
                return $"UDS {frame.ServiceName} (0x{frame.ServiceId:X2}) → DID 0x{frame.Did.Value:X4} — NRC 0x{frame.Nrc.Value:X2} ({frame.NrcName}){nodePart}";

            return $"UDS {frame.ServiceName} (0x{frame.ServiceId:X2}) — NRC 0x{frame.Nrc.Value:X2} ({frame.NrcName}){nodePart}";
        }

        if (frame.IsPositiveResponse && frame.Did.HasValue)
        {
            int len = frame.DataBytes?.Length ?? 0;
            return $"UDS {frame.ServiceName} (0x{frame.ServiceId:X2}) → DID 0x{frame.Did.Value:X4} — Positive Response ({len} bytes){nodePart}";
        }

        return $"ISO15765 {Direction} — Service 0x{frame.ServiceId:X2} ({frame.ServiceName}){nodePart}";
    }

    private string BuildTechnicalBreakdown(ParsedUdsFrame frame)
    {
        var b = new System.Text.StringBuilder();

        b.AppendLine($"Direction: {frame.Direction}");

        // ✅ Prefer showing computed CAN ID + Node (what you need for UI)
        b.AppendLine($"CAN ID: {FormatCanId(CanId)}");
        if (!string.IsNullOrWhiteSpace(CanNode))
            b.AppendLine($"Node: {CanNode}");

        // Keep the raw 4 ID bytes too (useful for verification)
        if (frame.IdBytes is { Length: > 0 })
        {
            string idHex = BitConverter.ToString(frame.IdBytes).Replace("-", " ");
            b.AppendLine($"ID Bytes: [{idHex}]");
        }

        if (frame.IsRequest)
            b.AppendLine($"Service: {frame.ServiceName} (0x{frame.ServiceId:X2}) [Request]");
        else if (frame.IsNegativeResponse)
            b.AppendLine($"Service: Negative Response (0x7F) to {frame.ServiceName} (0x{frame.ServiceId:X2})");
        else if (frame.IsPositiveResponse)
            b.AppendLine($"Service: {frame.ServiceName} (0x{frame.ServiceId:X2}) [Positive Response]");
        else
            b.AppendLine($"Service: {frame.ServiceName} (0x{frame.ServiceId:X2})");

        if (frame.Did.HasValue)
            b.AppendLine($"DID: 0x{frame.Did.Value:X4} ({frame.DidName})");

        if (frame.DataBytes is { Length: > 0 })
            b.AppendLine($"Data Length: {frame.DataBytes.Length} bytes");

        if (frame.Nrc.HasValue)
            b.AppendLine($"NRC: 0x{frame.Nrc.Value:X2} ({frame.NrcName})");

        if (frame.UdsPayload is { Length: > 0 })
        {
            string payloadHex = BitConverter.ToString(frame.UdsPayload).Replace("-", " ");
            b.AppendLine($"Raw Bytes: [{payloadHex}]");
        }

        return b.ToString().TrimEnd();
    }

    // ✅ FIX: return int? (not int?) to match LogLine.CanId
    private static int? TryExtractCanId(byte[]? bytes)
    {
        if (bytes == null || bytes.Length < 4) return null;

        // Common case in your logs: 00 00 07 D0 => 0x07D0 (11-bit style stored in low 2 bytes)
        if (bytes[0] == 0x00 && bytes[1] == 0x00)
            return (bytes[2] << 8) | bytes[3];

        // Full 32-bit value for other formats (still stored as int)
        int v = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
        return v;
    }

    private static string FormatCanId(int? canId)
    {
        if (!canId.HasValue) return "(none)";
        int v = canId.Value;
        return (v > 0x7FF) ? $"0x{v:X8}" : $"0x{v:X3}";
    }

    // ---- Timestamp helpers (use ONLY the one that matches LogLine.Timestamp type) ----
    private static string? TryParseTimestampPrefixString(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        int space = raw.IndexOf(' ');
        if (space <= 0) return null;

        string token = raw.Substring(0, space).Trim();
        return DateTime.TryParse(token, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _)
            ? token
            : null;
    }

    private static DateTime? TryParseTimestampPrefixDateTime(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        int space = raw.IndexOf(' ');
        if (space <= 0) return null;

        string token = raw.Substring(0, space).Trim();
        return DateTime.TryParse(token, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)
            ? dt
            : null;
    }
}