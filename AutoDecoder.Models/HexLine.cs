namespace AutoDecoder.Models;

// Derived class representing raw hexadecimal data lines
public sealed class HexLine : LogLine
{
    // Private field to store extracted hex bytes
    private byte[]? _bytes;

    // Override the Type property to return Hex
    public override LineType Type => LineType.Hex;

    // Constructor calls the base class constructor
    public HexLine(int lineNumber, string raw) : base(lineNumber, raw)
    {
    }

    // Parse and decode the hex line format
    public override void ParseAndDecode()
    {
        // Set default summary
        Summary = "Hex data";
        // Set default details
        Details = $"Raw: {Raw}";
        // Set initial confidence
        Confidence = 0.7;

        // Try to parse hex string (with or without spaces/separators)
        List<byte> bytesList = new();
        // Create a working copy of the raw string
        string working = Raw.Trim();

        // Remove common separators to make parsing easier
        working = working.Replace(" ", "");
        // Remove dashes
        working = working.Replace("-", "");
        // Remove commas
        working = working.Replace(",", "");
        // Remove colons
        working = working.Replace(":", "");

        // Try to parse pairs of hex characters
        for (int i = 0; i + 1 < working.Length; i += 2)
        {
            // Extract two characters
            string hexPair = working.Substring(i, 2);
            // Try to parse as hex byte
            if (byte.TryParse(hexPair, System.Globalization.NumberStyles.HexNumber, null, out byte b))
            {
                // Add parsed byte to list
                bytesList.Add(b);
            }
            else
            {
                // Not a valid hex pair, stop parsing
                break;
            }
        }

        // Store the parsed bytes
        _bytes = bytesList.ToArray();

        // Build summary and details based on parsed bytes
        if (_bytes.Length > 0)
        {
            // Build summary with byte count
            Summary = $"Hex data ({_bytes.Length} bytes)";

            // Convert bytes to hex string for display
            string hexString = BitConverter.ToString(_bytes).Replace("-", " ");
            // Create ASCII preview
            string asciiPreview = CreateAsciiPreview(_bytes);

            // Build detailed information
            Details = $"Byte Count: {_bytes.Length}\n";
            // Add hex representation
            Details += $"Hex: {hexString}\n";
            // Add ASCII preview
            Details += $"ASCII: {asciiPreview}\n\n";
            // Add raw line
            Details += $"Raw: {Raw}";

            // High confidence if we parsed many bytes
            Confidence = _bytes.Length >= 4 ? 0.9 : 0.7;
        }
        else
        {
            // No bytes parsed
            Summary = "Hex line (no valid bytes)";
            // Set details
            Details = $"Could not parse valid hex bytes.\n\nRaw: {Raw}";
            // Low confidence
            Confidence = 0.3;
        }
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
