#nullable enable
using System;

namespace AutoDecoder.Models;

public static class UdsLookup
{
    public static string GetServiceName(byte sid) => sid switch
    {
        0x10 => "DiagnosticSessionControl",
        0x11 => "ECUReset",
        0x22 => "ReadDataByIdentifier",
        0x27 => "SecurityAccess",
        0x2E => "WriteDataByIdentifier",
        0x31 => "RoutineControl",
        0x3E => "TesterPresent",
        0x7F => "NegativeResponse",
        _ => "Unknown"
    };

    public static string GetNrcMeaning(byte nrc) => nrc switch
    {
        0x10 => "GeneralReject",
        0x11 => "ServiceNotSupported",
        0x12 => "SubFunctionNotSupported",
        0x13 => "IncorrectMessageLengthOrInvalidFormat",
        0x22 => "ConditionsNotCorrect",
        0x31 => "RequestOutOfRange",
        0x33 => "SecurityAccessDenied",
        0x35 => "InvalidKey",
        0x36 => "ExceededNumberOfAttempts",
        0x37 => "RequiredTimeDelayNotExpired",
        0x78 => "ResponsePending",
        _ => "Unknown"
    };

    public static string GetDidName(ushort did) => did switch
    {
        0xF188 => "Strategy",
        0xF110 => "PartII_Spec",
        0xF111 => "CoreAssembly",
        0xF113 => "Assembly",
        0xF124 => "Calibration",
        0xDE00 => "DirectConfiguration",
        _ => "Unknown"
    };
}