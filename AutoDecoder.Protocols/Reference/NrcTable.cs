#nullable enable
using System.Collections.Generic;

namespace AutoDecoder.Protocols.Reference
{
    public static class NrcTable
    {
        // ISO 14229-1 Negative Response Codes (commonly used set)
        public static readonly Dictionary<byte, string> CodeToMeaning = new()
        {
            [0x00] = "positiveResponse (not used in NRC context)",
            [0x10] = "generalReject",
            [0x11] = "serviceNotSupported",
            [0x12] = "subFunctionNotSupported",
            [0x13] = "incorrectMessageLengthOrInvalidFormat",
            [0x14] = "responseTooLong",
            [0x21] = "busyRepeatRequest",
            [0x22] = "conditionsNotCorrect",
            [0x24] = "requestSequenceError",
            [0x25] = "noResponseFromSubnetComponent",
            [0x26] = "failurePreventsExecutionOfRequestedAction",
            [0x31] = "requestOutOfRange",
            [0x33] = "securityAccessDenied",
            [0x35] = "invalidKey",
            [0x36] = "exceededNumberOfAttempts",
            [0x37] = "requiredTimeDelayNotExpired",
            [0x70] = "uploadDownloadNotAccepted",
            [0x71] = "transferDataSuspended",
            [0x72] = "generalProgrammingFailure",
            [0x73] = "wrongBlockSequenceCounter",
            [0x78] = "requestCorrectlyReceivedResponsePending",
            [0x7E] = "subFunctionNotSupportedInActiveSession",
            [0x7F] = "serviceNotSupportedInActiveSession",
            [0x81] = "rpmTooHigh",
            [0x82] = "rpmTooLow",
            [0x83] = "engineIsRunning",
            [0x84] = "engineIsNotRunning",
            [0x85] = "engineRunTimeTooLow",
            [0x86] = "temperatureTooHigh",
            [0x87] = "temperatureTooLow",
            [0x88] = "vehicleSpeedTooHigh",
            [0x89] = "vehicleSpeedTooLow",
            [0x8A] = "throttlePedalTooHigh",
            [0x8B] = "throttlePedalTooLow",
            [0x8C] = "transmissionRangeNotInNeutral",
            [0x8D] = "transmissionRangeNotInGear",
            [0x8F] = "brakeSwitchNotClosed",
            [0x90] = "shifterLeverNotInPark",
            [0x91] = "torqueConverterClutchLocked",
            [0x92] = "voltageTooHigh",
            [0x93] = "voltageTooLow",
            [0xF1] = "VendorSpecific (Ford often uses for security/session constraints; confirm in context)"
        };

        public static string MeaningOrUnknown(byte nrc)
        {
            if (CodeToMeaning.TryGetValue(nrc, out var meaning))
                return meaning;

            // Many OEMs use 0xF0-0xFF space for vendor-specific meanings
            if (nrc >= 0xF0)
                return $"VendorSpecific(0x{nrc:X2})";

            return $"UnknownNRC(0x{nrc:X2})";
        }
    }
}