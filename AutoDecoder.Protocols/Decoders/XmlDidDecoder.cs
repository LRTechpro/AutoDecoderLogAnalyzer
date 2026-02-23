using AutoDecoder.Protocols.Decoders;
using AutoDecoder.Protocols.Utilities;


namespace AutoDecoder.Protocols.Decoders;

// Static class for decoding XML lines containing DID information
public static class XmlDidDecoder
{
    // Decode an XML line and return the result
    public static DecodeResult DecodeXmlLine(string rawLine)
    {
        // Try to extract DID value from XML attribute didValue="XXXX"
        ushort did = 0;
        // Find the didValue attribute
        int didIndex = rawLine.IndexOf("didValue=\"");

        // Check if didValue attribute was found
        if (didIndex >= 0)
        {
            // Move past the attribute name and opening quote
            int didStart = didIndex + "didValue=\"".Length;
            // Find the closing quote
            int didEnd = rawLine.IndexOf("\"", didStart);

            // Check if closing quote was found
            if (didEnd > didStart)
            {
                // Extract the DID hex string
                string didHex = rawLine.Substring(didStart, didEnd - didStart);
                // Try to parse the DID as a hex number
                ushort.TryParse(didHex, System.Globalization.NumberStyles.HexNumber, null, out did);
            }
        }

        // Try to extract response value from XML element
        string responseValue = string.Empty;
        // Look for <ns3:Response> tag
        int responseStart = rawLine.IndexOf("<ns3:Response>");

        // Check if opening tag was found
        if (responseStart >= 0)
        {
            // Move past the opening tag
            responseStart += "<ns3:Response>".Length;
            // Find the closing tag
            int responseEnd = rawLine.IndexOf("</ns3:Response>", responseStart);

            // Check if closing tag was found
            if (responseEnd > responseStart)
            {
                // Extract the response value
                responseValue = rawLine.Substring(responseStart, responseEnd - responseStart);
            }
        }
        // Alternative: try to extract from <Response> tag (without namespace)
        else
        {
            // Look for opening tag without namespace
            responseStart = rawLine.IndexOf("<Response>");

            // Check if opening tag was found
            if (responseStart >= 0)
            {
                // Move past the opening tag
                responseStart += "<Response>".Length;
                // Find the closing tag
                int responseEnd = rawLine.IndexOf("</Response>", responseStart);

                // Check if closing tag was found
                if (responseEnd > responseStart)
                {
                    // Extract the response value
                    responseValue = rawLine.Substring(responseStart, responseEnd - responseStart);
                }
            }
        }

        // Build the result based on what was extracted
        if (did != 0)
        {
            // DID found, build result with DID information
            string didName = DecodeTables.KnownDids.TryGetValue(did, out string? knownName)
                ? knownName
                : "Unknown";

            // Build summary
            string summary = $"XML DID 0x{did:X4} ({didName})";

            // Build detailed information
            string details = $"DID: 0x{did:X4} ({didName})\n";

            // Check if we have a response value
            if (!string.IsNullOrEmpty(responseValue))
            {
                // Add response to details
                details += $"Response: {responseValue}\n";
                // Check if response looks like hex (all hex digits)
                bool looksLikeHex = responseValue.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
                // Add response format indication
                details += $"Response Format: {(looksLikeHex ? "Hexadecimal" : "Text/Mixed")}\n";
            }
            else
            {
                // No response found
                details += "Response: (not found)\n";
            }

            // Add raw line for reference
            details += $"\nRaw: {rawLine}";

            // Return result with high confidence
            return new DecodeResult
            {
                // Set summary
                Summary = summary,
                // Set details
                Details = details,
                // High confidence when DID is found
                Confidence = 0.8
            };
        }
        else
        {
            // DID not found, return generic XML result
            return new DecodeResult
            {
                // Generic summary
                Summary = "XML line (no DID found)",
                // Details
                Details = $"XML content detected but no didValue attribute found.\n\nRaw: {rawLine}",
                // Medium confidence
                Confidence = 0.6
            };
        }
    }
}
