namespace AutoDecoder.Models;

// Derived class representing XML formatted lines containing DID information
public sealed class XmlLine : LogLine
{
    // Private field to store extracted DID value
    private ushort _did;
    // Private field to store extracted response value
    private string _responseValue = string.Empty;

    // Override the Type property to return Xml
    public override LineType Type => LineType.Xml;

    // Constructor calls the base class constructor
    public XmlLine(int lineNumber, string raw) : base(lineNumber, raw)
    {
    }

    // Parse and decode the XML line format
    public override void ParseAndDecode()
    {
        // Set default summary
        Summary = "XML line";
        // Set default details
        Details = $"Raw: {Raw}";
        // Set initial confidence
        Confidence = 0.6;

        // Try to extract DID value from XML attribute didValue="XXXX"
        int didIndex = Raw.IndexOf("didValue=\"");
        // Check if didValue attribute was found
        if (didIndex >= 0)
        {
            // Move past the attribute name and opening quote
            int didStart = didIndex + "didValue=\"".Length;
            // Find the closing quote
            int didEnd = Raw.IndexOf("\"", didStart);

            // Check if closing quote was found
            if (didEnd > didStart)
            {
                // Extract the DID hex string
                string didHex = Raw.Substring(didStart, didEnd - didStart);
                // Try to parse the DID as a hex number
                if (ushort.TryParse(didHex, System.Globalization.NumberStyles.HexNumber, null, out ushort parsedDid))
                {
                    // Store the parsed DID
                    _did = parsedDid;
                    // Increase confidence since we found a valid DID
                    Confidence = 0.8;
                }
            }
        }

        // Try to extract response value from XML element <ns3:Response>VALUE</ns3:Response>
        int responseStart = Raw.IndexOf("<ns3:Response>");
        // Check if opening tag was found
        if (responseStart >= 0)
        {
            // Move past the opening tag
            responseStart += "<ns3:Response>".Length;
            // Find the closing tag
            int responseEnd = Raw.IndexOf("</ns3:Response>", responseStart);

            // Check if closing tag was found
            if (responseEnd > responseStart)
            {
                // Extract the response value
                _responseValue = Raw.Substring(responseStart, responseEnd - responseStart);
            }
        }
        // Alternative: try to extract from <Response> tag (without namespace)
        else
        {
            // Look for opening tag without namespace
            responseStart = Raw.IndexOf("<Response>");
            // Check if opening tag was found
            if (responseStart >= 0)
            {
                // Move past the opening tag
                responseStart += "<Response>".Length;
                // Find the closing tag
                int responseEnd = Raw.IndexOf("</Response>", responseStart);

                // Check if closing tag was found
                if (responseEnd > responseStart)
                {
                    // Extract the response value
                    _responseValue = Raw.Substring(responseStart, responseEnd - responseStart);
                }
            }
        }

        // Build the summary and details based on what was extracted
        if (_did != 0)
        {
            // Format DID as hex
            string didHex = $"0x{_did:X4}";
            // Get DID name if known
            string didName = GetDidName(_did);

            // Build summary with DID information
            Summary = $"XML DID {didHex} ({didName})";

            // Build detailed information
            Details = $"DID: {didHex} ({didName})\n";

            // Check if we have a response value
            if (!string.IsNullOrEmpty(_responseValue))
            {
                // Add response to details
                Details += $"Response: {_responseValue}\n";
                // Check if response looks like hex
                bool looksLikeHex = _responseValue.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
                // Add response format indication
                Details += $"Response Format: {(looksLikeHex ? "Hexadecimal" : "Text/Mixed")}\n";
            }
            else
            {
                // No response found
                Details += "Response: (not found)\n";
            }

            // Add raw line for reference
            Details += $"\nRaw: {Raw}";
        }
        else
        {
            // DID not found, generic XML line
            Summary = "XML line (no DID found)";
            // Set details
            Details = $"XML content detected but no didValue attribute found.\n\nRaw: {Raw}";
            // Lower confidence
            Confidence = 0.5;
        }
    }

    // Helper method to get DID name from DID value
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
            // Unknown DID
            _ => "Unknown"
        };
    }
}
