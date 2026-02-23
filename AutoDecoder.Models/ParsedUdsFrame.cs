#nullable enable
using System;

namespace AutoDecoder.Models;

// Structure to hold parsed UDS frame information for formatting
public sealed class ParsedUdsFrame
{
    public string Direction { get; set; } = "UNK";

    // First 4 bytes from the bracket payload (your "ID header" bytes)
    public byte[] IdBytes { get; set; } = Array.Empty<byte>();

    // Bytes after the first 4 bytes (UDS / ISO-TP payload region)
    public byte[]? UdsPayload { get; set; }

    public bool IsRequest { get; set; }
    public bool IsNegativeResponse { get; set; }
    public bool IsPositiveResponse { get; set; }

    public byte ServiceId { get; set; }
    public string ServiceName { get; set; } = "Unknown";

    public ushort? Did { get; set; }
    public string DidName { get; set; } = "Unknown";

    public byte? Nrc { get; set; }
    public string NrcName { get; set; } = "Unknown";

    public byte[]? DataBytes { get; set; }
}
