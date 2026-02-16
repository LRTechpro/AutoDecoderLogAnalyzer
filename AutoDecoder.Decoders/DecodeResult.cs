namespace AutoDecoder.Decoders;

// Sealed class to encapsulate the result of a decode operation
public sealed class DecodeResult
{
    // Short summary of the decoded information
    public string Summary { get; init; } = string.Empty;

    // Detailed decoded information
    public string Details { get; init; } = string.Empty;

    // Confidence level of the decode (0.0 to 1.0)
    public double Confidence { get; init; } = 0.0;
}
