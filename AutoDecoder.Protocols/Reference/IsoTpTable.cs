#nullable enable
namespace AutoDecoder.Protocols.Reference
{
    public enum IsoTpFrameType
    {
        Unknown,
        SingleFrame,      // 0x0
        FirstFrame,       // 0x1
        ConsecutiveFrame, // 0x2
        FlowControl       // 0x3
    }

    public enum FlowStatus
    {
        ContinueToSend = 0x00,
        Wait = 0x01,
        OverflowAbort = 0x02,
        Unknown = 0xFF
    }

    public static class IsoTpTable
    {
        public static IsoTpFrameType DetectFrameType(byte firstByte)
        {
            int nibble = (firstByte & 0xF0) >> 4;
            return nibble switch
            {
                0x0 => IsoTpFrameType.SingleFrame,
                0x1 => IsoTpFrameType.FirstFrame,
                0x2 => IsoTpFrameType.ConsecutiveFrame,
                0x3 => IsoTpFrameType.FlowControl,
                _ => IsoTpFrameType.Unknown
            };
        }

        public static FlowStatus ParseFlowStatus(byte fcByte1LowNibble)
        {
            // For FlowControl, PCI byte low nibble encodes FS: 0=CTS, 1=WT, 2=OVFLW
            return fcByte1LowNibble switch
            {
                0x0 => FlowStatus.ContinueToSend,
                0x1 => FlowStatus.Wait,
                0x2 => FlowStatus.OverflowAbort,
                _ => FlowStatus.Unknown
            };
        }

        public static string FlowStatusMeaning(FlowStatus fs) => fs switch
        {
            FlowStatus.ContinueToSend => "ContinueToSend (CTS)",
            FlowStatus.Wait => "Wait (WT)",
            FlowStatus.OverflowAbort => "Overflow/Abort (OVFLW)",
            _ => "Unknown FlowStatus"
        };
    }
}