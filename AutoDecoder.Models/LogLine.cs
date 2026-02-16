namespace AutoDecoder.Models;

// Abstract base class for all log line types, demonstrating inheritance and encapsulation
public abstract class LogLine
{
    // Private backing field for the raw line text (encapsulation)
    private readonly string _raw;
    // Private backing field for the line number in the original file (encapsulation)
    private readonly int _lineNumber;

    // Public read-only property exposing the raw line text
    public string Raw => _raw;

    // Public read-only property exposing the line number
    public int LineNumber => _lineNumber;

    // Abstract property that derived classes must implement to specify their type
    public abstract LineType Type { get; }

    // Summary of the decoded line (short description)
    public string Summary { get; protected set; } = string.Empty;

    // Detailed decoded information about the line
    public string Details { get; protected set; } = string.Empty;

    // Confidence level of the decode (0.0 to 1.0, where 1.0 is certain)
    public double Confidence { get; protected set; } = 0.0;

    // Protected constructor to initialize common fields (called by derived classes)
    protected LogLine(int lineNumber, string raw)
    {
        // Store the line number
        _lineNumber = lineNumber;
        // Store the raw line text
        _raw = raw ?? string.Empty;
    }

    // Abstract method that derived classes must implement to parse and decode their specific format
    public abstract void ParseAndDecode();
}
