#nullable enable
using System;

namespace AutoDecoder.Protocols.Conversations;

public sealed class UdsTransaction
{
    public int RequestLine { get; set; }
    public int? ResponseLine { get; set; }

    public DateTime? RequestTime { get; set; }
    public DateTime? ResponseTime { get; set; }

    public int? RequestCanId { get; set; }
    public int? ResponseCanId { get; set; }

    public byte ServiceId { get; set; }
    public byte? PositiveSid { get; set; }
    public byte? Nrc { get; set; }

    public ushort? Did { get; set; }

    public double? LatencyMs { get; set; }

    public bool IsNegative => Nrc.HasValue;
    public bool IsComplete => ResponseLine.HasValue;

    public string? RequestNode { get; set; }
    public string? ResponseNode { get; set; }
}