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
}
