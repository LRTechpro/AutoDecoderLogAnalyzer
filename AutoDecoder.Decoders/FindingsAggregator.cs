using AutoDecoder.Models;
using System.Text.RegularExpressions;

namespace AutoDecoder.Decoders;

// Summary of aggregated findings from decoded log lines
public sealed class FindingsSummary
{
    // Dictionary mapping NRC codes (byte) to occurrence counts
    public Dictionary<byte, int> NrcCounts { get; set; } = new();

    // Dictionary mapping DID values (ushort) to occurrence counts
    public Dictionary<ushort, int> DidCounts { get; set; } = new();

    // Total number of lines processed
    public int TotalLines { get; set; }

    // Number of ISO15765 lines found
    public int IsoLines { get; set; }

    // Number of lines containing UDS findings (requests or responses)
    public int UdsFindingLines { get; set; }

    // Number of unknown/unparseable lines
    public int UnknownLines { get; set; }
}

// Static utility class to aggregate findings from decoded log lines
public static class FindingsAggregator
{
    // Regex pattern to extract hex values like "0x7F", "0x22", "0xF188"
    private static readonly Regex HexPattern = new Regex(@"0x([0-9A-Fa-f]{1,4})", RegexOptions.Compiled);

    // Build a deterministic summary of findings from a collection of log lines
    public static FindingsSummary Build(IEnumerable<LogLine> lines)
    {
        // Initialize empty summary object
        FindingsSummary summary = new FindingsSummary();

        // Process each log line
        foreach (LogLine line in lines)
        {
            // Increment total line count
            summary.TotalLines++;

            // Check if this is an ISO15765 line
            if (line.Type == LineType.Iso15765)
            {
                // Increment ISO line count
                summary.IsoLines++;
            }

            // Check if this is an unknown line
            if (line.Type == LineType.Unknown)
            {
                // Increment unknown line count
                summary.UnknownLines++;
            }

            // Try to extract NRC codes from Details field (deterministic pattern matching)
            ExtractNrcCodes(line, summary);

            // Try to extract DID codes from Summary or Details fields (deterministic pattern matching)
            ExtractDidCodes(line, summary);
        }

        // Return the populated summary
        return summary;
    }

    // Extract and count NRC codes from a log line's Details field
    private static void ExtractNrcCodes(LogLine line, FindingsSummary summary)
    {
        // Check if Details field is null or empty
        if (string.IsNullOrEmpty(line.Details))
        {
            // No details to process
            return;
        }

        // Check if Details contains "NRC:" pattern (case-insensitive)
        if (!line.Details.Contains("NRC:", StringComparison.OrdinalIgnoreCase))
        {
            // No NRC pattern found
            return;
        }

        // This line contains a UDS finding (negative response)
        summary.UdsFindingLines++;

        // Find all hex patterns after "NRC:" in the Details field
        int nrcIndex = line.Details.IndexOf("NRC:", StringComparison.OrdinalIgnoreCase);
        // Get the substring starting from "NRC:"
        string nrcSection = line.Details.Substring(nrcIndex);

        // Match all hex patterns in the NRC section
        MatchCollection matches = HexPattern.Matches(nrcSection);

        // Process the first hex value found (should be the NRC code)
        if (matches.Count > 0)
        {
            // Get the first hex value
            string hexValue = matches[0].Groups[1].Value;

            // Try to parse as byte (NRC codes are single bytes)
            if (byte.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out byte nrcCode))
            {
                // Increment count for this NRC code
                if (summary.NrcCounts.ContainsKey(nrcCode))
                {
                    // NRC already exists, increment count
                    summary.NrcCounts[nrcCode]++;
                }
                else
                {
                    // First occurrence of this NRC, initialize count to 1
                    summary.NrcCounts[nrcCode] = 1;
                }
            }
        }
    }

    // Extract and count DID codes from a log line's Summary or Details fields
    private static void ExtractDidCodes(LogLine line, FindingsSummary summary)
    {
        // Check if this looks like a UDS Request (ReadDataByIdentifier)
        bool isUdsRequest = false;

        // Check Summary field for "UDS Request" pattern
        if (line.Summary?.Contains("UDS Request", StringComparison.OrdinalIgnoreCase) == true)
        {
            // This is a UDS request
            isUdsRequest = true;
            // Increment UDS finding count
            summary.UdsFindingLines++;
        }

        // Check Details field for "UDS Request" pattern
        if (line.Details?.Contains("UDS Request", StringComparison.OrdinalIgnoreCase) == true)
        {
            // This is a UDS request
            isUdsRequest = true;
        }

        // Also check for "UDS Positive Response" in Details
        if (line.Details?.Contains("UDS Positive Response", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Increment UDS finding count (if not already counted)
            if (!isUdsRequest)
            {
                summary.UdsFindingLines++;
            }
        }

        // Look for "DID:" pattern in both Summary and Details
        string textToSearch = (line.Summary ?? "") + " " + (line.Details ?? "");

        // Check if text contains "DID:" pattern
        if (!textToSearch.Contains("DID:", StringComparison.OrdinalIgnoreCase))
        {
            // No DID pattern found
            return;
        }

        // Find all hex patterns in the text
        MatchCollection matches = HexPattern.Matches(textToSearch);

        // Try to find DID values (2-byte hex values like 0xF188, 0x806A)
        foreach (Match match in matches)
        {
            // Get the hex value
            string hexValue = match.Groups[1].Value;

            // DIDs are typically 2 bytes (4 hex digits), but accept 1-4 hex digits
            if (hexValue.Length <= 4)
            {
                // Try to parse as ushort (DID codes are 2 bytes)
                if (ushort.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out ushort didCode))
                {
                    // Only count DIDs from UDS Requests (ReadDataByIdentifier 0x22)
                    if (isUdsRequest)
                    {
                        // Increment count for this DID code
                        if (summary.DidCounts.ContainsKey(didCode))
                        {
                            // DID already exists, increment count
                            summary.DidCounts[didCode]++;
                        }
                        else
                        {
                            // First occurrence of this DID, initialize count to 1
                            summary.DidCounts[didCode] = 1;
                        }
                    }
                }
            }
        }
    }
}
