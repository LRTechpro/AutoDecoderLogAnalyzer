using AutoDecoder.Models;

namespace AutoDecoder.Decoders;

// Static class for decoding UDS (Unified Diagnostic Services) payloads
public static class UdsDecoder
{
    // Try to decode a UDS payload from raw bytes (handles Request, Negative Response, Positive Response)
    public static DecodeResult? TryDecodeFromPayload(byte[] payload)
    {
        // Check if payload is null or empty
        if (payload == null || payload.Length == 0)
        {
            // Cannot decode empty payload
            return null;
        }

        // Check for UDS Request: ReadDataByIdentifier (0x22)
        if (payload.Length >= 3 && payload[0] == 0x22)
        {
            // Decode ReadDID request
            return DecodeReadDidRequest(payload);
        }

        // Check for UDS Negative Response (0x7F)
        if (payload.Length >= 3 && payload[0] == 0x7F)
        {
            // Decode negative response
            return DecodeNegativeResponse(payload);
        }

        // Check for UDS Positive Response to ReadDataByIdentifier (0x62)
        if (payload.Length >= 3 && payload[0] == 0x62)
        {
            // Decode positive response
            return DecodeReadDidPositiveResponse(payload);
        }

        // Unknown UDS format, return low-confidence result
        return new DecodeResult
        {
            // Generic summary
            Summary = "UDS: Not decoded",
            // Generic details
            Details = $"Payload: {BitConverter.ToString(payload).Replace("-", " ")}",
            // Zero confidence
            Confidence = 0.0
        };
    }

    // Decode a UDS Request: ReadDataByIdentifier (0x22)
    public static DecodeResult DecodeReadDidRequest(byte[] payload)
    {
        // Check minimum length for ReadDID request
        if (payload.Length < 3)
        {
            // Invalid request length
            return new DecodeResult
            {
                // Error summary
                Summary = "Invalid ReadDID Request",
                // Error details
                Details = "Payload too short for ReadDataByIdentifier request.",
                // Low confidence
                Confidence = 0.2
            };
        }

        // Extract DID high byte
        byte didHi = payload[1];
        // Extract DID low byte
        byte didLo = payload[2];
        // Combine to form 16-bit DID
        ushort did = (ushort)((didHi << 8) | didLo);

        // Try to get the DID name from the lookup table (deterministic, no guessing)
        string didName = DecodeTables.KnownDids.TryGetValue(did, out string? knownName)
            ? knownName
            : "Unknown";

        // Build the summary
        string summary = $"UDS Request: ReadDID 0x{did:X4} ({didName})";

        // Build the detailed information
        string details = $"Request: ReadDataByIdentifier (0x22)\n";
        // Add DID
        details += $"DID: 0x{did:X4} ({didName})\n";
        // Add payload
        details += $"Full Payload: {BitConverter.ToString(payload).Replace("-", " ")}";

        // Return the decode result with high confidence
        return new DecodeResult
        {
            // Set summary
            Summary = summary,
            // Set details
            Details = details,
            // High confidence for ReadDID request
            Confidence = 0.9
        };
    }

    // Decode a UDS Negative Response payload
    public static DecodeResult DecodeNegativeResponse(byte[] payload)
    {
        // Check minimum length for negative response
        if (payload.Length < 3)
        {
            // Invalid negative response length
            return new DecodeResult
            {
                // Error summary
                Summary = "Invalid Negative Response",
                // Error details
                Details = "Payload too short for negative response.",
                // Low confidence
                Confidence = 0.2
            };
        }

        // Extract the original service ID that was requested
        byte originalSid = payload[1];
        // Extract the negative response code (NRC)
        byte nrc = payload[2];

        // Try to get the service name from the lookup table (deterministic, no guessing)
        string serviceName = DecodeTables.UdsServiceNames.TryGetValue(originalSid, out string? svcName)
            ? svcName
            : $"Unknown";

        // Try to get the NRC meaning from the lookup table (deterministic, no guessing)
        string nrcMeaning = DecodeTables.UdsNrcNames.TryGetValue(nrc, out string? nrcName)
            ? nrcName
            : $"Unknown";

        // Build the summary
        string summary = $"UDS Negative Response: {serviceName} (0x{originalSid:X2}) NRC {nrcMeaning} (0x{nrc:X2})";

        // Build the detailed information
        string details = $"Negative Response (0x7F)\n";
        // Add original service ID
        details += $"Original Service: 0x{originalSid:X2} ({serviceName})\n";
        // Add NRC code
        details += $"NRC: 0x{nrc:X2} ({nrcMeaning})\n";
        // Add payload
        details += $"Full Payload: {BitConverter.ToString(payload).Replace("-", " ")}";

        // Return the decode result with high confidence
        return new DecodeResult
        {
            // Set summary
            Summary = summary,
            // Set details
            Details = details,
            // High confidence for negative response
            Confidence = 1.0
        };
    }

    // Decode a UDS Positive Response to ReadDataByIdentifier (improved for large payloads)
    public static DecodeResult DecodeReadDidPositiveResponse(byte[] payload)
    {
        // Check minimum length for positive response
        if (payload.Length < 3)
        {
            // Invalid positive response length
            return new DecodeResult
            {
                // Error summary
                Summary = "Invalid Positive Response",
                // Error details
                Details = "Payload too short for positive response.",
                // Low confidence
                Confidence = 0.2
            };
        }

        // Extract DID high byte
        byte didHi = payload[1];
        // Extract DID low byte
        byte didLo = payload[2];
        // Combine to form 16-bit DID
        ushort did = (ushort)((didHi << 8) | didLo);

        // Try to get the DID name from the lookup table (deterministic, no guessing)
        string didName = DecodeTables.KnownDids.TryGetValue(did, out string? knownName)
            ? knownName
            : "Unknown";

        // Extract data bytes after the DID (start at index 3 of udsPayload)
        byte[] dataBytes = payload.Length > 3 ? payload[3..] : Array.Empty<byte>();

        // Format data hex with truncation for large payloads (improves readability)
        string dataHex = FormatHexPreview(dataBytes, 64, 8);

        // Create ASCII preview of only the first 64 bytes (for large payloads)
        byte[] asciiPreviewBytes = dataBytes.Length > 64 ? dataBytes[0..64] : dataBytes;
        // Generate ASCII preview using HexTools
        string asciiPreview = HexTools.ToAsciiPreview(asciiPreviewBytes);

        // Build the summary
        string summary = $"UDS Positive Response (ReadDID): DID 0x{did:X4} ({didName})";

        // Build the detailed information
        string details = $"Positive Response to ReadDataByIdentifier (0x62)\n";
        // Add DID
        details += $"DID: 0x{did:X4} ({didName})\n";
        // Add data length (always show total length)
        details += $"Data Length: {dataBytes.Length} bytes\n";
        // Add formatted data hex (with truncation if needed)
        details += $"Data (hex): {dataHex}\n";
        // Add ASCII preview (first 64 bytes only for large payloads)
        details += $"Data (ASCII): {asciiPreview}\n";
        // Add full payload
        details += $"Full Payload: {BitConverter.ToString(payload).Replace("-", " ")}";

        // Return the decode result with high confidence
        return new DecodeResult
        {
            // Set summary
            Summary = summary,
            // Set details
            Details = details,
            // High confidence for positive response
            Confidence = 0.9
        };
    }

    // Helper method to format hex data with truncation for large payloads
    private static string FormatHexPreview(byte[] data, int headBytes = 64, int tailBytes = 8)
    {
        // Check if data is empty
        if (data.Length == 0)
        {
            // Return placeholder for empty data
            return "(none)";
        }

        // Check if data is small enough to show in full (threshold is headBytes + tailBytes)
        if (data.Length <= headBytes + tailBytes)
        {
            // Show all bytes as hex (space-separated)
            return BitConverter.ToString(data).Replace("-", " ");
        }

        // Data is large, show head + "..." + tail for readability
        byte[] headData = data[0..headBytes];
        // Extract tail bytes from end of data
        byte[] tailData = data[^tailBytes..];

        // Format head bytes as hex
        string headHex = BitConverter.ToString(headData).Replace("-", " ");
        // Format tail bytes as hex
        string tailHex = BitConverter.ToString(tailData).Replace("-", " ");

        // Build truncated format: "head ... (truncated) ... tail"
        return $"{headHex}\n... ({data.Length - headBytes - tailBytes} bytes truncated) ...\n{tailHex}";
    }

    // ============================================================
    // NEW: Report Summary and Technical Breakdown Helpers
    // ============================================================

    // Build a clean, single-line Report Summary suitable for documentation
    public static string BuildReportSummary(ParsedUdsFrame frame)
    {
        // Check for UDS Request
        if (frame.IsRequest && frame.Did.HasValue)
        {
            // Format: UDS ReadDataByIdentifier (0x22) → DID 0x806A
            return $"UDS ReadDataByIdentifier (0x{frame.ServiceId:X2}) → DID 0x{frame.Did.Value:X4}";
        }

        // Check for UDS Negative Response
        if (frame.IsNegativeResponse && frame.Nrc.HasValue)
        {
            // Get original service name
            string origService = frame.ServiceName;
            // Format: UDS ReadDataByIdentifier (0x22) → DID 0xF188 — NRC 0x78 (Response Pending)
            if (frame.Did.HasValue)
            {
                return $"UDS {origService} (0x{frame.ServiceId:X2}) → DID 0x{frame.Did.Value:X4} — NRC 0x{frame.Nrc.Value:X2} ({frame.NrcName})";
            }
            else
            {
                return $"UDS {origService} (0x{frame.ServiceId:X2}) — NRC 0x{frame.Nrc.Value:X2} ({frame.NrcName})";
            }
        }

        // Check for UDS Positive Response
        if (frame.IsPositiveResponse && frame.Did.HasValue)
        {
            // Get data length
            int dataLength = frame.DataBytes?.Length ?? 0;
            // Format: UDS ReadDataByIdentifier (0x22) → DID 0x806A — Positive Response (384 bytes)
            return $"UDS ReadDataByIdentifier (0x{frame.ServiceId:X2}) → DID 0x{frame.Did.Value:X4} — Positive Response ({dataLength} bytes)";
        }

        // Default fallback
        return $"UDS Service 0x{frame.ServiceId:X2}";
    }

    // Build a structured, multi-line Technical Breakdown with all technical details
    public static string BuildTechnicalBreakdown(ParsedUdsFrame frame)
    {
        // Use StringBuilder for efficient multi-line construction
        var breakdown = new System.Text.StringBuilder();

        // Add Direction
        breakdown.AppendLine($"Direction: {frame.Direction}");

        // Add CAN ID (from ID bytes)
        if (frame.IdBytes != null && frame.IdBytes.Length > 0)
        {
            string canId = BitConverter.ToString(frame.IdBytes).Replace("-", " ");
            breakdown.AppendLine($"CAN ID: [{canId}]");
        }

        // Add Service information
        if (frame.IsRequest)
        {
            breakdown.AppendLine($"Service: ReadDataByIdentifier (0x{frame.ServiceId:X2})");
        }
        else if (frame.IsNegativeResponse)
        {
            breakdown.AppendLine($"Service: Negative Response (0x7F) to {frame.ServiceName} (0x{frame.ServiceId:X2})");
        }
        else if (frame.IsPositiveResponse)
        {
            breakdown.AppendLine($"Service: Positive Response (0x{frame.ServiceId:X2})");
        }

        // Add DID if present
        if (frame.Did.HasValue)
        {
            breakdown.AppendLine($"DID: 0x{frame.Did.Value:X4} ({frame.DidName})");
        }

        // Add Data Length if data bytes present
        if (frame.DataBytes != null && frame.DataBytes.Length > 0)
        {
            breakdown.AppendLine($"Data Length: {frame.DataBytes.Length} bytes");
        }

        // Add NRC if present
        if (frame.Nrc.HasValue)
        {
            breakdown.AppendLine($"NRC: 0x{frame.Nrc.Value:X2} ({frame.NrcName})");
        }

        // Add Raw Bytes (UDS Payload)
        if (frame.UdsPayload != null && frame.UdsPayload.Length > 0)
        {
            string payloadHex = BitConverter.ToString(frame.UdsPayload).Replace("-", " ");
            breakdown.AppendLine($"Raw Bytes: [{payloadHex}]");
        }

        // Add ASCII Preview if data bytes present (for positive responses)
        if (frame.DataBytes != null && frame.DataBytes.Length > 0)
        {
            // Use first 64 bytes for ASCII preview
            byte[] previewBytes = frame.DataBytes.Length > 64 ? frame.DataBytes[0..64] : frame.DataBytes;
            string asciiPreview = HexTools.ToAsciiPreview(previewBytes);
            breakdown.AppendLine($"ASCII Preview: {asciiPreview}");

            // Show truncation note if data is large
            if (frame.DataBytes.Length > 64)
            {
                breakdown.AppendLine($"(showing first 64 of {frame.DataBytes.Length} bytes)");
            }
        }

        // Return the complete breakdown (trim final newline)
        return breakdown.ToString().TrimEnd();
    }
}
