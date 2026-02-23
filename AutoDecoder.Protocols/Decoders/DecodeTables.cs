

namespace AutoDecoder.Protocols.Decoders;

// Static class containing lookup tables for UDS (Unified Diagnostic Services) decoding
public static class DecodeTables
{
    // Dictionary mapping UDS service IDs to service names
    public static readonly Dictionary<byte, string> UdsServiceNames = new()
    {
        // DiagnosticSessionControl service
        { 0x10, "DiagnosticSessionControl" },
        // ECUReset service
        { 0x11, "ECUReset" },
        // ReadDataByIdentifier service
        { 0x22, "ReadDataByIdentifier" },
        // SecurityAccess service
        { 0x27, "SecurityAccess" },
        // WriteDataByIdentifier service
        { 0x2E, "WriteDataByIdentifier" },
        // RoutineControl service
        { 0x31, "RoutineControl" },
        // TesterPresent service
        { 0x3E, "TesterPresent" },
        // NegativeResponse service
        { 0x7F, "NegativeResponse" }
    };

    // Dictionary mapping UDS Negative Response Codes (NRC) to meanings
    public static readonly Dictionary<byte, string> UdsNrcNames = new()
    {
        // GeneralReject NRC
        { 0x10, "GeneralReject" },
        // ServiceNotSupported NRC
        { 0x11, "ServiceNotSupported" },
        // SubFunctionNotSupported NRC
        { 0x12, "SubFunctionNotSupported" },
        // IncorrectMessageLengthOrInvalidFormat NRC
        { 0x13, "IncorrectMessageLengthOrInvalidFormat" },
        // ConditionsNotCorrect NRC
        { 0x22, "ConditionsNotCorrect" },
        // RequestOutOfRange NRC
        { 0x31, "RequestOutOfRange" },
        // SecurityAccessDenied NRC
        { 0x33, "SecurityAccessDenied" },
        // InvalidKey NRC
        { 0x35, "InvalidKey" },
        // ExceededNumberOfAttempts NRC
        { 0x36, "ExceededNumberOfAttempts" },
        // RequiredTimeDelayNotExpired NRC
        { 0x37, "RequiredTimeDelayNotExpired" },
        // ResponsePending NRC (most common in logs)
        { 0x78, "ResponsePending" }
    };

    // Dictionary mapping known DID (Data Identifier) values to names
    public static readonly Dictionary<ushort, string> KnownDids = new()
    {
        // Strategy DID
        { 0xF188, "Strategy" },
        // PartII_Spec DID
        { 0xF110, "PartII_Spec" },
        // CoreAssembly DID
        { 0xF111, "CoreAssembly" },
        // Assembly DID
        { 0xF113, "Assembly" },
        // Calibration DID
        { 0xF124, "Calibration" },
        // DirectConfiguration DID
        { 0xDE00, "DirectConfiguration" }
    };
}
