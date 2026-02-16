namespace AutoDecoder.Models;

// Derived class representing ISO 15765 protocol lines (CAN diagnostic protocol with ID header handling)
public sealed class Iso15765Line : LogLine
{
    // Private field to store the direction of communication (TX or RX)
    private string _direction = string.Empty;
    // Private field to store all extracted bytes (including ID header)
    private byte[]? _allBytes;

    // Override the Type property to return Iso15765
    public override LineType Type => LineType.Iso15765;

    // Constructor calls the base class constructor
    public Iso15765Line(int lineNumber, string raw) : base(lineNumber, raw)
    {
    }

    // Parse and decode the ISO 15765 line format (handles 4-byte ID header + UDS payload)
    public override void ParseAndDecode()
    {
        // Set default summary in case parsing fails
        Summary = "ISO15765 line";
        // Set default details
        Details = $"Raw: {Raw}";
        // Set initial confidence
        Confidence = 0.5;

        // Try to detect the direction (TX or RX, case-insensitive)
        if (Raw.IndexOf(" TX ", StringComparison.OrdinalIgnoreCase) >= 0 || 
            Raw.IndexOf("->", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Detected transmit direction
            _direction = "TX";
        }
        else if (Raw.IndexOf(" RX ", StringComparison.OrdinalIgnoreCase) >= 0 || 
                 Raw.IndexOf("<-", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Detected receive direction
            _direction = "RX";
        }

        // Try to extract the bracket payload using the Raw property
        int bracketStart = Raw.IndexOf('[');
        // Find the first closing bracket after opening bracket
        int bracketEnd = bracketStart >= 0 ? Raw.IndexOf(']', bracketStart + 1) : -1;

        // Check if both brackets were found
        if (bracketStart >= 0 && bracketEnd > bracketStart)
        {
            // Extract the content between brackets
            string bracketContent = Raw.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
            // Split by comma to get individual hex values
            string[] hexParts = bracketContent.Split(',');
            // Create a list to hold the parsed bytes
            List<byte> bytesList = new();

            // Parse each hex string into a byte
            foreach (string hexPart in hexParts)
            {
                // Trim whitespace from the hex string
                string trimmed = hexPart.Trim();
                // Skip empty parts
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    // Skip empty entries
                    continue;
                }
                // Try to parse the hex string
                if (byte.TryParse(trimmed, System.Globalization.NumberStyles.HexNumber, null, out byte b))
                {
                    // Add the parsed byte to the list
                    bytesList.Add(b);
                }
            }

            // Store all parsed bytes (ID header + UDS payload)
            _allBytes = bytesList.ToArray();

            // Check if we have enough bytes (need more than just ID header)
            if (_allBytes.Length > 4)
            {
                // Attempt to decode with ID header separation
                DecodeWithIdHeader();
            }
            else if (_allBytes.Length > 0)
            {
                // Only ID bytes or less, no UDS payload
                string idHex = BitConverter.ToString(_allBytes).Replace("-", " ");
                // Set summary
                Summary = $"ISO15765 {_direction} (ID only, no UDS payload)";
                // Set details
                Details = $"Direction: {_direction}\nID/Bytes: [{idHex}]\nNot enough bytes for UDS payload (need >4 bytes)";
                // Lower confidence
                Confidence = 0.5;
            }
            else
            {
                // No payload bytes extracted
                Summary = $"ISO15765 {_direction} (empty payload)";
                // Set details
                Details = $"Direction: {_direction}\nNo payload bytes extracted.";
                // Lower confidence
                Confidence = 0.4;
            }
        }
        else
        {
            // No brackets found, metadata only
            Summary = $"ISO15765 {_direction} (metadata only)";
            // Set details
            Details = $"Direction: {_direction}\nNo payload brackets found.";
            // Lower confidence
            Confidence = 0.4;
        }
    }

    // Private method to decode with ID header separation (first 4 bytes = ID, rest = UDS payload)
    private void DecodeWithIdHeader()
    {
        // Check if we have enough bytes
        if (_allBytes == null || _allBytes.Length <= 4)
        {
            // Exit early if not enough bytes
            return;
        }

        // Extract ID bytes (first 4 bytes)
        byte[] idBytes = _allBytes.Take(4).ToArray();
        // Extract UDS payload bytes (everything after first 4 bytes)
        byte[] udsPayload = _allBytes.Skip(4).ToArray();

        // Parse UDS payload into structured frame
        var frame = ParseUdsPayload(udsPayload, idBytes);

        // Use new formatting helpers to build Summary and Details
        Summary = BuildReportSummary(frame);
        Details = BuildTechnicalBreakdown(frame);
        Confidence = frame.IsRequest ? 0.9 : (frame.IsNegativeResponse ? 1.0 : 0.9);
    }

    // Parse UDS payload into structured ParsedUdsFrame
    private ParsedUdsFrame ParseUdsPayload(byte[] udsPayload, byte[] idBytes)
    {
        var frame = new ParsedUdsFrame
        {
            Direction = _direction,
            IdBytes = idBytes,
            UdsPayload = udsPayload
        };

        // Check for UDS Request: ReadDataByIdentifier (0x22)
        if (udsPayload.Length >= 3 && udsPayload[0] == 0x22)
        {
            frame.IsRequest = true;
            frame.ServiceId = 0x22;
            frame.ServiceName = "ReadDataByIdentifier";

            // Extract DID
            byte didHi = udsPayload[1];
            byte didLo = udsPayload[2];
            frame.Did = (ushort)((didHi << 8) | didLo);
            frame.DidName = GetDidName(frame.Did.Value);
        }
        // Check for UDS Negative Response (0x7F)
        else if (udsPayload.Length >= 3 && udsPayload[0] == 0x7F)
        {
            frame.IsNegativeResponse = true;
            frame.ServiceId = udsPayload[1]; // Original service ID
            frame.ServiceName = GetUdsServiceName(frame.ServiceId);
            frame.Nrc = udsPayload[2];
            frame.NrcName = GetUdsNrcMeaning(frame.Nrc.Value);

            // Try to extract DID if this was a ReadDID request
            if (frame.ServiceId == 0x22 && udsPayload.Length >= 5)
            {
                byte didHi = udsPayload[3];
                byte didLo = udsPayload[4];
                frame.Did = (ushort)((didHi << 8) | didLo);
                frame.DidName = GetDidName(frame.Did.Value);
            }
        }
        // Check for UDS Positive Response to ReadDataByIdentifier (0x62)
        else if (udsPayload.Length >= 3 && udsPayload[0] == 0x62)
        {
            frame.IsPositiveResponse = true;
            frame.ServiceId = 0x62;
            frame.ServiceName = "ReadDataByIdentifier";

            // Extract DID
            byte didHi = udsPayload[1];
            byte didLo = udsPayload[2];
            frame.Did = (ushort)((didHi << 8) | didLo);
            frame.DidName = GetDidName(frame.Did.Value);

            // Extract data bytes after DID
            frame.DataBytes = udsPayload.Length > 3 ? udsPayload[3..] : Array.Empty<byte>();
        }

        return frame;
    }

    // Build clean Report Summary for documentation
    private string BuildReportSummary(ParsedUdsFrame frame)
    {
        // Check for UDS Request
        if (frame.IsRequest && frame.Did.HasValue)
        {
            return $"UDS ReadDataByIdentifier (0x22) → DID 0x{frame.Did.Value:X4}";
        }

        // Check for UDS Negative Response
        if (frame.IsNegativeResponse && frame.Nrc.HasValue)
        {
            if (frame.Did.HasValue)
            {
                return $"UDS {frame.ServiceName} (0x{frame.ServiceId:X2}) → DID 0x{frame.Did.Value:X4} — NRC 0x{frame.Nrc.Value:X2} ({frame.NrcName})";
            }
            else
            {
                return $"UDS {frame.ServiceName} (0x{frame.ServiceId:X2}) — NRC 0x{frame.Nrc.Value:X2} ({frame.NrcName})";
            }
        }

        // Check for UDS Positive Response
        if (frame.IsPositiveResponse && frame.Did.HasValue)
        {
            int dataLength = frame.DataBytes?.Length ?? 0;
            return $"UDS ReadDataByIdentifier (0x22) → DID 0x{frame.Did.Value:X4} — Positive Response ({dataLength} bytes)";
        }

        // Default fallback
        return $"ISO15765 {_direction} - UDS Service 0x{frame.ServiceId:X2}";
    }

    // Build structured Technical Breakdown for engineering analysis
    private string BuildTechnicalBreakdown(ParsedUdsFrame frame)
    {
        var breakdown = new System.Text.StringBuilder();

        // Add Direction
        breakdown.AppendLine($"Direction: {frame.Direction}");

        // Add CAN ID
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

        // Add ASCII Preview if data bytes present
        if (frame.DataBytes != null && frame.DataBytes.Length > 0)
        {
            byte[] previewBytes = frame.DataBytes.Length > 64 ? frame.DataBytes[0..64] : frame.DataBytes;
            string asciiPreview = CreateAsciiPreview(previewBytes);
            breakdown.AppendLine($"ASCII Preview: {asciiPreview}");

            if (frame.DataBytes.Length > 64)
            {
                breakdown.AppendLine($"(showing first 64 of {frame.DataBytes.Length} bytes)");
            }
        }

        return breakdown.ToString().TrimEnd();
    }

    // Helper method to get UDS service name from service ID (deterministic, no guessing)
    private static string GetUdsServiceName(byte sid)
    {
        // Return service name based on service ID
        return sid switch
        {
            // DiagnosticSessionControl
            0x10 => "DiagnosticSessionControl",
            // ECUReset
            0x11 => "ECUReset",
            // ReadDataByIdentifier
            0x22 => "ReadDataByIdentifier",
            // SecurityAccess
            0x27 => "SecurityAccess",
            // WriteDataByIdentifier
            0x2E => "WriteDataByIdentifier",
            // RoutineControl
            0x31 => "RoutineControl",
            // TesterPresent
            0x3E => "TesterPresent",
            // NegativeResponse
            0x7F => "NegativeResponse",
            // Unknown service (deterministic label)
            _ => "Unknown"
        };
    }

    // Helper method to get UDS NRC (Negative Response Code) meaning (deterministic, no guessing)
    private static string GetUdsNrcMeaning(byte nrc)
    {
        // Return NRC meaning based on code
        return nrc switch
        {
            // GeneralReject
            0x10 => "GeneralReject",
            // ServiceNotSupported
            0x11 => "ServiceNotSupported",
            // SubFunctionNotSupported
            0x12 => "SubFunctionNotSupported",
            // IncorrectMessageLengthOrInvalidFormat
            0x13 => "IncorrectMessageLengthOrInvalidFormat",
            // ConditionsNotCorrect
            0x22 => "ConditionsNotCorrect",
            // RequestOutOfRange
            0x31 => "RequestOutOfRange",
            // SecurityAccessDenied
            0x33 => "SecurityAccessDenied",
            // InvalidKey
            0x35 => "InvalidKey",
            // ExceededNumberOfAttempts
            0x36 => "ExceededNumberOfAttempts",
            // RequiredTimeDelayNotExpired
            0x37 => "RequiredTimeDelayNotExpired",
            // ResponsePending
            0x78 => "ResponsePending",
            // Unknown NRC (deterministic label)
            _ => "Unknown"
        };
    }

    // Helper method to get DID name from DID value (deterministic, no guessing)
    private static string GetDidName(ushort did)
    {
        // Return DID name based on value
        return did switch
        {
            // Strategy
            0xF188 => "Strategy",
            // PartII_Spec
            0xF110 => "PartII_Spec",
            // CoreAssembly
            0xF111 => "CoreAssembly",
            // Assembly
            0xF113 => "Assembly",
            // Calibration
            0xF124 => "Calibration",
            // DirectConfiguration
            0xDE00 => "DirectConfiguration",
            // Unknown DID (deterministic label)
            _ => "Unknown"
        };
    }

    // Helper method to create ASCII preview from bytes (printable chars or '.')
    private static string CreateAsciiPreview(byte[] bytes)
    {
        // Check if bytes array is empty
        if (bytes.Length == 0)
        {
            // Return placeholder for empty
            return "(empty)";
        }

        // Create character array for preview
        char[] chars = new char[bytes.Length];
        // Iterate through each byte
        for (int i = 0; i < bytes.Length; i++)
        {
            // Get the current byte
            byte b = bytes[i];
            // Check if byte is printable ASCII (32-126)
            if (b >= 32 && b <= 126)
            {
                // Use the ASCII character
                chars[i] = (char)b;
            }
            else
            {
                // Use dot for non-printable
                chars[i] = '.';
            }
        }

        // Convert char array to string and return
        return new string(chars);
    }
}
