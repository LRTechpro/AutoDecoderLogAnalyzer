namespace AutoDecoder.Decoders;

// Static class for decoding ISO 15765 protocol lines with proper ID header handling
public static class Iso15765Decoder
{
    // Decode an ISO 15765 line and return the result (splits ID header from UDS payload)
    public static DecodeResult DecodeIso15765Line(string rawLine)
    {
        // Detect the direction of communication (case-insensitive)
        string direction = "Unknown";
        // Check for transmit direction (improved to handle variations)
        if (rawLine.IndexOf(" TX ", StringComparison.OrdinalIgnoreCase) >= 0 || 
            rawLine.IndexOf("->", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Set direction to TX
            direction = "TX";
        }
        // Check for receive direction (improved to handle variations)
        else if (rawLine.IndexOf(" RX ", StringComparison.OrdinalIgnoreCase) >= 0 || 
                 rawLine.IndexOf("<-", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Set direction to RX
            direction = "RX";
        }

        // Try to extract all bracket bytes (includes ID header + UDS payload)
        bool hasBracketPayload = HexTools.TryParseBracketHexBytes(rawLine, out byte[] allBytes);

        // Check if we successfully extracted bytes
        if (!hasBracketPayload || allBytes.Length == 0)
        {
            // No payload found, return metadata-only result
            return new DecodeResult
            {
                // Summary with direction
                Summary = $"ISO15765 {direction} (metadata only)",
                // Details
                Details = $"Direction: {direction}\nNo payload bytes extracted.\n\nRaw: {rawLine}",
                // Low confidence
                Confidence = 0.4
            };
        }

        // Check if we have enough bytes (need more than just ID header for UDS decoding)
        if (allBytes.Length <= 4)
        {
            // Only ID header present (first 4 bytes), no UDS payload available
            string idOnlyHex = BitConverter.ToString(allBytes).Replace("-", " ");
            // Return frame header only result with better explanation
            return new DecodeResult
            {
                // Summary indicating frame header only
                Summary = $"ISO15765 {direction} - Frame header only (no UDS payload)",
                // Details with explanation of ISO15765 structure
                Details = $"Direction: {direction}\n" +
                         $"ID/Header Bytes: [{idOnlyHex}]\n\n" +
                         $"Explanation: ISO15765 frames contain a 4-byte address/ID header followed by the UDS payload.\n" +
                         $"This frame contains only the header ({allBytes.Length} bytes total), so no UDS data can be decoded.\n\n" +
                         $"Raw: {rawLine}",
                // Lower confidence for frame header only
                Confidence = 0.4
            };
        }

        // Split into ID header (first 4 bytes) and UDS payload (remaining bytes)
        byte[] idBytes = allBytes.Take(4).ToArray();
        // Extract UDS payload bytes (everything after first 4 bytes)
        byte[] udsPayload = allBytes.Skip(4).ToArray();

        // Convert ID bytes to hex string for display
        string idHex = BitConverter.ToString(idBytes).Replace("-", " ");
        // Convert UDS payload to hex string for display
        string udsPayloadHex = BitConverter.ToString(udsPayload).Replace("-", " ");

        // Try to decode UDS content from the payload (excluding ID header)
        DecodeResult? udsResult = UdsDecoder.TryDecodeFromPayload(udsPayload);

        // Check if UDS decoding was successful
        if (udsResult != null && udsResult.Confidence > 0.0)
        {
            // UDS decode successful, build combined summary
            string summary = $"ISO15765 {direction} - {udsResult.Summary}";

            // Build detailed information combining ISO 15765 and UDS info
            string details = $"Direction: {direction}\n";
            // Add ID bytes
            details += $"ID Bytes: [{idHex}]\n";
            // Add UDS payload bytes
            details += $"UDS Payload: [{udsPayloadHex}]\n";
            // Add payload length
            details += $"UDS Payload Length: {udsPayload.Length} bytes\n";
            // Add separator
            details += "\n--- UDS Decode ---\n";
            // Add UDS decoded details
            details += udsResult.Details;
            // Add ASCII preview of UDS payload
            details += $"\n\nUDS Payload ASCII: {HexTools.ToAsciiPreview(udsPayload)}";

            // Return combined result
            return new DecodeResult
            {
                // Set summary
                Summary = summary,
                // Set details
                Details = details,
                // Use UDS confidence
                Confidence = udsResult.Confidence
            };
        }

        // No UDS decode, return generic ISO 15765 result
        string fallbackSummary = $"ISO15765 {direction} - payload (no UDS decode)";

        // Build detailed information
        string fallbackDetails = $"Direction: {direction}\n";
        // Add ID bytes
        fallbackDetails += $"ID Bytes: [{idHex}]\n";
        // Add UDS payload bytes
        fallbackDetails += $"UDS Payload: [{udsPayloadHex}]\n";
        // Add payload length
        fallbackDetails += $"UDS Payload Length: {udsPayload.Length} bytes\n";
        // Add ASCII preview
        fallbackDetails += $"ASCII Preview: {HexTools.ToAsciiPreview(udsPayload)}\n\n";
        // Add raw line
        fallbackDetails += $"Raw: {rawLine}";

        // Return result with medium confidence
        return new DecodeResult
        {
            // Set summary
            Summary = fallbackSummary,
            // Set details
            Details = fallbackDetails,
            // Medium confidence
            Confidence = 0.6
        };
    }
}
