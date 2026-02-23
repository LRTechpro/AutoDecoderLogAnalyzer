#nullable enable
using System;

namespace AutoDecoder.Protocols.Conversations;

public sealed class IsoTpPdu
{
    public int StartLine { get; set; }
    public int EndLine { get; set; }

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public string Direction { get; set; } = "UNK"; // TX/RX
    public int CanId { get; set; }               // from your 4-byte header extraction

    // Full UDS payload after ISO-TP reassembly (no PCI bytes)
    public byte[] Payload { get; set; } = Array.Empty<byte>();

    public override string ToString()
        => $"{Direction} 0x{CanId:X3} Lines {StartLine}-{EndLine} Len={Payload.Length}";
}