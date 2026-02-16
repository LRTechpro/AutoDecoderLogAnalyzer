namespace AutoDecoder.Models;

// Derived class representing plain ASCII text lines
public sealed class AsciiLine : LogLine
{
    // Override the Type property to return Ascii
    public override LineType Type => LineType.Ascii;

    // Constructor calls the base class constructor
    public AsciiLine(int lineNumber, string raw) : base(lineNumber, raw)
    {
    }

    // Parse and decode the ASCII line format
    public override void ParseAndDecode()
    {
        // Count the number of printable ASCII characters
        int printableCount = Raw.Count(c => c >= 32 && c <= 126);
        // Calculate the percentage of printable characters
        double printablePercent = Raw.Length > 0 ? (double)printableCount / Raw.Length * 100 : 0;

        // Build summary showing it's ASCII text
        Summary = $"ASCII text ({Raw.Length} chars)";

        // Build detailed information
        Details = $"Type: Plain ASCII Text\n";
        // Add character count
        Details += $"Length: {Raw.Length} characters\n";
        // Add printable percentage
        Details += $"Printable: {printablePercent:F1}%\n\n";
        // Add the actual text content
        Details += $"Content:\n{Raw}";

        // Set confidence based on printability
        Confidence = printablePercent >= 80 ? 0.8 : 0.6;
    }
}
