namespace AutoDecoder.Models;

// Derived class representing unrecognized or unparseable lines
public sealed class UnknownLine : LogLine
{
    // Private field to store the reason why the line is unknown
    private string _reason = string.Empty;

    // Override the Type property to return Unknown
    public override LineType Type => LineType.Unknown;

    // Constructor calls the base class constructor
    public UnknownLine(int lineNumber, string raw) : base(lineNumber, raw)
    {
    }

    // Constructor overload that accepts a reason
    public UnknownLine(int lineNumber, string raw, string reason) : base(lineNumber, raw)
    {
        // Store the reason
        _reason = reason;
    }

    // Parse and decode the unknown line (minimal processing)
    public override void ParseAndDecode()
    {
        // Build summary indicating unknown type
        Summary = "Unknown line type";

        // Build detailed information
        Details = $"Type: Unknown\n";

        // Check if a reason was provided
        if (!string.IsNullOrEmpty(_reason))
        {
            // Add the reason to details
            Details += $"Reason: {_reason}\n\n";
        }
        else
        {
            // Add generic reason
            Details += "Reason: Could not match any known line format.\n\n";
        }

        // Add raw line content
        Details += $"Raw: {Raw}";

        // Low confidence for unknown lines
        Confidence = 0.1;
    }
}
