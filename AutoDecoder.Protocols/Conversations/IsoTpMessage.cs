#nullable enable
using System;

namespace AutoDecoder.Protocols.Conversations
{
    public sealed class IsoTpMessage
    {
        public int StartLine { get; init; }
        public int EndLine { get; init; }

        public DateTime? StartTime { get; init; }
        public DateTime? EndTime { get; init; }

        public string Direction { get; init; } = ""; // "TX" or "RX"
        public int CanId { get; init; }

        public byte[] Payload { get; init; } = Array.Empty<byte>(); // Complete UDS payload

        public bool IsComplete { get; init; }
        public string? Error { get; init; }
    }
}