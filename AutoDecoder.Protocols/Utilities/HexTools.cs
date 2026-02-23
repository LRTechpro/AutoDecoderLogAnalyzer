namespace AutoDecoder.Protocols.Utilities;

// Static helper class for parsing and manipulating hexadecimal data
public static class HexTools
{
    // Try to parse hex bytes from bracket notation like "[7F,22,78]" even with surrounding text
    public static bool TryParseBracketHexBytes(string raw, out byte[] bytes)
    {
        // Initialize output parameter
        bytes = Array.Empty<byte>();

        // Find the first opening bracket (handles timestamp prefixes)
        int bracketStart = raw.IndexOf('[');
        // Find the first closing bracket after the opening bracket
        int bracketEnd = raw.IndexOf(']', bracketStart + 1);

        // Check if both brackets were found in valid order
        if (bracketStart < 0 || bracketEnd <= bracketStart)
        {
            // Brackets not found or invalid order
            return false;
        }

        // Extract the content between brackets (improved to handle surrounding text)
        string bracketContent = raw.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
        // Trim any whitespace from the bracket content
        bracketContent = bracketContent.Trim();

        // Check if bracket content is empty
        if (string.IsNullOrWhiteSpace(bracketContent))
        {
            // Empty brackets
            return false;
        }

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
            else
            {
                // Parsing failed for this part, return false
                return false;
            }
        }

        // Store the parsed bytes
        bytes = bytesList.ToArray();
        // Return success
        return bytes.Length > 0;
    }

    // Try to parse a long hex string (with or without spaces) into bytes
    public static bool TryParseLongHexString(string raw, out byte[] bytes)
    {
        // Initialize output parameter
        bytes = Array.Empty<byte>();

        // Create a working copy of the input string
        string working = raw.Trim();

        // Remove common separators to make parsing easier
        working = working.Replace(" ", "");
        // Remove dashes
        working = working.Replace("-", "");
        // Remove commas
        working = working.Replace(",", "");
        // Remove colons
        working = working.Replace(":", "");

        // Check if the length is even (required for hex pairs)
        if (working.Length % 2 != 0)
        {
            // Odd length, cannot parse hex pairs
            return false;
        }

        // Check if the string is long enough to be considered a "long" hex string
        if (working.Length < 8)
        {
            // Too short to be considered a long hex string
            return false;
        }

        // Create a list to hold the parsed bytes
        List<byte> bytesList = new();

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
                // Not a valid hex pair, return false
                return false;
            }
        }

        // Store the parsed bytes
        bytes = bytesList.ToArray();
        // Return success
        return bytes.Length > 0;
    }

    // Convert bytes to ASCII preview (printable chars or '.')
    public static string ToAsciiPreview(byte[] bytes)
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
