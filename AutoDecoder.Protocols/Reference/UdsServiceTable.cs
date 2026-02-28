#nullable enable
using System.Collections.Generic;

namespace AutoDecoder.Protocols.Reference
{
    public static class UdsServiceTable
    {
        // ISO 14229-1 UDS Service IDs (common/public)
        public static readonly Dictionary<byte, string> RequestSidToName = new()
        {
            [0x10] = "DiagnosticSessionControl",
            [0x11] = "ECUReset",
            [0x14] = "ClearDiagnosticInformation",
            [0x19] = "ReadDTCInformation",
            [0x22] = "ReadDataByIdentifier",
            [0x23] = "ReadMemoryByAddress",
            [0x24] = "ReadScalingDataByIdentifier",
            [0x27] = "SecurityAccess",
            [0x28] = "CommunicationControl",
            [0x29] = "Authentication",
            [0x2A] = "ReadDataByPeriodicIdentifier",
            [0x2C] = "DynamicallyDefineDataIdentifier",
            [0x2E] = "WriteDataByIdentifier",
            [0x2F] = "InputOutputControlByIdentifier",
            [0x31] = "RoutineControl",
            [0x34] = "RequestDownload",
            [0x35] = "RequestUpload",
            [0x36] = "TransferData",
            [0x37] = "RequestTransferExit",
            [0x38] = "RequestFileTransfer",
            [0x3D] = "WriteMemoryByAddress",
            [0x3E] = "TesterPresent",
            [0x83] = "AccessTimingParameter",
            [0x84] = "SecuredDataTransmission",
            [0x85] = "ControlDTCSetting",
            [0x86] = "ResponseOnEvent",
            [0x87] = "LinkControl"
        };

        public static byte PositiveResponseSid(byte requestSid) => (byte)(requestSid + 0x40);

        public static string NameOrUnknown(byte requestSid)
            => RequestSidToName.TryGetValue(requestSid, out var name) ? name : $"UnknownSID(0x{requestSid:X2})";
    }
}