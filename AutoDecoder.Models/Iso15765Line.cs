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

        // Convert ID bytes to hex string for display
        string idHex = BitConverter.ToString(idBytes).Replace("-", " ");
        // Convert UDS payload to hex string for display
        string udsPayloadHex = BitConverter.ToString(udsPayload).Replace("-", " ");

        // Check for UDS Request: ReadDataByIdentifier (0x22)
        if (udsPayload.Length >= 3 && udsPayload[0] == 0x22)
        {
            // Extract DID high byte
            byte didHi = udsPayload[1];
            // Extract DID low byte
            byte didLo = udsPayload[2];
            // Combine to form 16-bit DID
            ushort did = (ushort)((didHi << 8) | didLo);

            // Format DID as hex
            string didHex = $"0x{did:X4}";
            // Get DID name if known
            string didName = GetDidName(did);

            // Build summary for ReadDID request
            Summary = $"ISO15765 {_direction} - UDS Request: ReadDID {didHex} ({didName})";
            // Build detailed information
            Details = $"Direction: {_direction}\n";
            // Add ID bytes
            Details += $"ID Bytes: [{idHex}]\n";
            // Add UDS payload
            Details += $"UDS Payload: [{udsPayloadHex}]\n";
            // Add decoded info
            Details += $"UDS Request: ReadDataByIdentifier (0x22)\n";
            // Add DID
            Details += $"DID: {didHex} ({didName})";
            // High confidence for request
            Confidence = 0.9;
        }
        // Check for UDS Negative Response (starts with 0x7F)
        else if (udsPayload.Length >= 3 && udsPayload[0] == 0x7F)
        {
            // Extract the original service ID that was requested
            byte originalSid = udsPayload[1];
            // Extract the negative response code (NRC)
            byte nrc = udsPayload[2];

            // Get service name (deterministic, no guessing)
            string serviceName = GetUdsServiceName(originalSid);
            // Get NRC meaning (deterministic, no guessing)
            string nrcMeaning = GetUdsNrcMeaning(nrc);

            // Build summary for negative response
            Summary = $"ISO15765 {_direction} - UDS Negative Response: {serviceName} (0x{originalSid:X2}) NRC {nrcMeaning} (0x{nrc:X2})";
            // Build detailed information
            Details = $"Direction: {_direction}\n";
            // Add ID bytes
            Details += $"ID Bytes: [{idHex}]\n";
            // Add UDS payload
            Details += $"UDS Payload: [{udsPayloadHex}]\n";
            // Add decoded info
            Details += $"UDS Negative Response (0x7F):\n";
            // Add original service
            Details += $"  Original Service: 0x{originalSid:X2} ({serviceName})\n";
            // Add NRC code
            Details += $"  NRC: 0x{nrc:X2} ({nrcMeaning})";
            // High confidence for negative response
            Confidence = 1.0;
        }
        // Check for UDS Positive Response to ReadDataByIdentifier (0x62)
        else if (udsPayload.Length >= 3 && udsPayload[0] == 0x62)
        {
            // Extract DID high byte
            byte didHi = udsPayload[1];
            // Extract DID low byte
            byte didLo = udsPayload[2];
            // Combine to form 16-bit DID
            ushort did = (ushort)((didHi << 8) | didLo);

            // Format DID as hex
            string didHex = $"0x{did:X4}";
            // Get DID name if known
            string didName = GetDidName(did);

            // Extract data bytes after the DID
            byte[] dataBytes = udsPayload.Length > 3 ? udsPayload[3..] : Array.Empty<byte>();
            // Convert data bytes to hex string
            string dataHex = dataBytes.Length > 0 ? BitConverter.ToString(dataBytes).Replace("-", " ") : "(none)";
            // Create ASCII preview of data bytes
            string asciiPreview = CreateAsciiPreview(dataBytes);

            // Build summary for positive response
            Summary = $"ISO15765 {_direction} - UDS Positive Response (ReadDID): DID {didHex} ({didName})";
            // Build detailed information
            Details = $"Direction: {_direction}\n";
            // Add ID bytes
            Details += $"ID Bytes: [{idHex}]\n";
            // Add UDS payload
            Details += $"UDS Payload: [{udsPayloadHex}]\n";
            // Add decoded info
            Details += $"UDS Positive Response to ReadDataByIdentifier (0x62):\n";
            // Add DID
            Details += $"  DID: {didHex} ({didName})\n";
            // Add data length
            Details += $"  Data Length: {dataBytes.Length} bytes\n";
            // Add data hex
            Details += $"  Data (hex): {dataHex}\n";
            // Add ASCII preview
            Details += $"  Data (ASCII): {asciiPreview}";
            // High confidence for positive response
            Confidence = 0.9;
        }
        else
        {
            // Generic ISO 15765 payload without UDS decoding
            Summary = $"ISO15765 {_direction} - payload (no UDS decode)";
            // Build detailed information
            Details = $"Direction: {_direction}\n";
            // Add ID bytes
            Details += $"ID Bytes: [{idHex}]\n";
            // Add UDS payload
            Details += $"UDS Payload: [{udsPayloadHex}]\n";
            // Add payload length
            Details += $"UDS Payload Length: {udsPayload.Length} bytes\n";
            // Add ASCII preview
            Details += $"ASCII Preview: {CreateAsciiPreview(udsPayload)}";
            // Medium confidence
            Confidence = 0.6;
        }
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
