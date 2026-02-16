namespace AutoDecoder.Models;

// Structure to hold parsed UDS frame information for formatting
public class ParsedUdsFrame
{
    public string Direction { get; set; } = string.Empty;
    public byte[] IdBytes { get; set; } = Array.Empty<byte>();
    public byte[]? UdsPayload { get; set; }
    public byte ServiceId { get; set; }
    public ushort? Did { get; set; }
    public byte? Nrc { get; set; }
    public byte[]? DataBytes { get; set; }
    public string ServiceName { get; set; } = "Unknown";
    public string DidName { get; set; } = "Unknown";
    public string NrcName { get; set; } = "Unknown";
    public bool IsRequest { get; set; }
    public bool IsNegativeResponse { get; set; }
    public bool IsPositiveResponse { get; set; }
}
