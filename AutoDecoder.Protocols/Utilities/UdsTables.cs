#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoDecoder.Protocols.Utilities
{
    /// <summary>
    /// Option 1: Universal UDS arithmetic + negative response pattern
    /// Option 2: Core UDS services + ISO-TP + NRC + session/security subfunctions + DID heuristics
    /// </summary>
    public static class UdsTables
    {
        // ---------------------------
        // Option 1: Universal UDS Arithmetic
        // ---------------------------

        /// <summary>
        /// Positive response SID = request SID + 0x40 (example: 0x22 -> 0x62, 0x10 -> 0x50)
        /// </summary>
        public static byte PositiveResponseSid(byte requestSid) => (byte)(requestSid + 0x40);

        /// <summary>
        /// Negative response pattern: 0x7F, original SID, NRC
        /// </summary>
        public static bool TryParseNegativeResponse(ReadOnlySpan<byte> payload, out byte originalSid, out byte nrc)
        {
            originalSid = 0;
            nrc = 0;

            if (payload.Length < 3) return false;
            if (payload[0] != 0x7F) return false;

            originalSid = payload[1];
            nrc = payload[2];
            return true;
        }

        // ---------------------------
        // Option 2: Core UDS Service IDs (Know These Cold)
        // ---------------------------

        public static readonly IReadOnlyDictionary<byte, string> ServiceName = new Dictionary<byte, string>
        {
            [0x10] = "DiagnosticSessionControl",
            [0x11] = "ECUReset",
            [0x14] = "ClearDiagnosticInformation",
            [0x19] = "ReadDTCInformation",
            [0x22] = "ReadDataByIdentifier",
            [0x23] = "ReadMemoryByAddress",
            [0x27] = "SecurityAccess",
            [0x28] = "CommunicationControl",
            [0x2E] = "WriteDataByIdentifier",
            [0x31] = "RoutineControl",
            [0x34] = "RequestDownload",
            [0x35] = "RequestUpload",
            [0x36] = "TransferData",
            [0x37] = "RequestTransferExit",
            [0x3E] = "TesterPresent",
            [0x85] = "ControlDTCSetting",
            [0x86] = "ResponseOnEvent"
        };

        public static readonly IReadOnlyDictionary<byte, string> ServiceTriggerThought = new Dictionary<byte, string>
        {
            [0x10] = "Session shift (Default 01, Programming 02, Extended 03)",
            [0x11] = "Will cause restart / re-init chatter",
            [0x14] = "Clears DTC groups",
            [0x19] = "DTC queries (expect multi-frame)",
            [0x22] = "Most common in logs (DID reads)",
            [0x23] = "Raw memory peek (watch for security)",
            [0x27] = "Seed & key (subfunctions 0x01/0x02...)",
            [0x28] = "Mute/limit comms",
            [0x2E] = "Changing config/cal values",
            [0x31] = "Start/Stop/Results for routines",
            [0x34] = "Programming preamble (download)",
            [0x35] = "Less common in field (upload)",
            [0x36] = "Flash payload blocks",
            [0x37] = "Close download/upload",
            [0x3E] = "Keep session alive (subfunction 00)",
            [0x85] = "Enable/disable DTC recording",
            [0x86] = "Event triggered responses"
        };

        public static string DescribeService(byte sid)
        {
            var name = ServiceName.TryGetValue(sid, out var n) ? n : "UnknownService";
            var thought = ServiceTriggerThought.TryGetValue(sid, out var t) ? t : "—";
            return $"{name} (0x{sid:X2}) — {thought}";
        }

        // ---------------------------
        // Session subfunctions (Service 0x10)
        // ---------------------------

        public static readonly IReadOnlyDictionary<byte, string> SessionSubfunction = new Dictionary<byte, string>
        {
            [0x01] = "Default",
            [0x02] = "Programming",
            [0x03] = "Extended"
        };

        // ---------------------------
        // SecurityAccess (0x27) mental model
        // ---------------------------
        // Typical convention:
        //  0x01/0x02 => Level 1 (seed/key)
        //  0x03/0x04 => Level 2
        //  0x05/0x06 => Level 3
        // Odd = requestSeed, Even = sendKey (common pattern; OEMs can vary)

        public static string DescribeSecuritySubfunction(byte sub)
        {
            int level = (sub + 1) / 2; // 1..n
            bool isSeed = (sub % 2) == 1;

            string phase = isSeed ? "Req (Seed)" : "Resp (Key)";
            string levelName = level switch
            {
                1 => "Level 1 (basic)",
                2 => "Level 2 (programming)",
                3 => "Level 3 (engineering)",
                _ => $"Level {level}"
            };

            return $"0x{sub:X2} — {phase} — {levelName}";
        }

        // ---------------------------
        // High-value NRCs (third byte after 7F <SID>)
        // ---------------------------

        public static readonly IReadOnlyDictionary<byte, string> NrcMeaning = new Dictionary<byte, string>
        {
            [0x10] = "GeneralReject",
            [0x11] = "ServiceNotSupported",
            [0x12] = "SubFunctionNotSupported",
            [0x13] = "IncorrectMessageLengthOrInvalidFormat",
            [0x21] = "BusyRepeatRequest",
            [0x22] = "ConditionsNotCorrect",
            [0x24] = "RequestSequenceError",
            [0x31] = "RequestOutOfRange",
            [0x33] = "SecurityAccessDenied",
            [0x35] = "InvalidKey",
            [0x36] = "ExceededNumberOfAttempts",
            [0x37] = "RequiredTimeDelayNotExpired",
            [0x72] = "GeneralProgrammingFailure",
            [0x7E] = "SubFunctionNotSupportedInActiveSession",
            [0x7F] = "ServiceNotSupportedInActiveSession",
            [0x78] = "ResponsePending"
        };

        public static readonly IReadOnlyDictionary<byte, string> NrcAction = new Dictionary<byte, string>
        {
            [0x10] = "Re-check formatting / timing",
            [0x11] = "Wrong service for ECU",
            [0x12] = "Wrong subfunction",
            [0x13] = "Fix length/format",
            [0x21] = "Retry with backoff",
            [0x22] = "Check ignition / preconditions",
            [0x24] = "Order wrong (missing prior step)",
            [0x31] = "DID not supported / session issue",
            [0x33] = "Perform 0x27 unlock",
            [0x35] = "Wrong key; watch attempt counter",
            [0x36] = "Wait cooldown or power cycle",
            [0x37] = "Delay before retry",
            [0x72] = "Flash/programming problem",
            [0x78] = "Wait; ECU busy"
        };

        public static string DescribeNrc(byte nrc)
        {
            var meaning = NrcMeaning.TryGetValue(nrc, out var m) ? m : "UnknownNRC";
            var action = NrcAction.TryGetValue(nrc, out var a) ? a : "—";
            return $"NRC 0x{nrc:X2} — {meaning} — Action: {action}";
        }

        // ---------------------------
        // ISO-TP / Transport Layer Nibbles (first byte of frame if raw)
        // ---------------------------

        public enum IsoTpPciType : byte
        {
            SingleFrame = 0x0,
            FirstFrame = 0x1,
            ConsecutiveFrame = 0x2,
            FlowControl = 0x3
        }

        public enum IsoTpFlowStatus : byte
        {
            CTS = 0x0,      // Continue To Send
            WAIT = 0x1,
            OVERFLOW = 0x2  // Overflow/Abort
        }

        public static bool TryParseIsoTpPci(byte pci, out IsoTpPciType type, out byte lowNibble)
        {
            type = (IsoTpPciType)(pci >> 4);
            lowNibble = (byte)(pci & 0x0F);

            return type is IsoTpPciType.SingleFrame
                or IsoTpPciType.FirstFrame
                or IsoTpPciType.ConsecutiveFrame
                or IsoTpPciType.FlowControl;
        }

        // ---------------------------
        // Common CAN / UDS Arbitration IDs (11-bit typical)
        // ---------------------------

        public static string DescribeArbitrationId(int canId)
        {
            // Heuristics; OEMs vary.
            return canId switch
            {
                0x7DF => "Functional OBD-II request (broadcast)",
                >= 0x7E0 and <= 0x7E7 => "Tester -> ECU physical request (powertrain cluster typical)",
                >= 0x7E8 and <= 0x7EF => "ECU -> Tester response (typical pairing for 0x7E0-0x7E7)",
                0x724 or 0x72C => "Example request/response pair you’ve seen (often +0x8 rule, not universal)",
                _ => "—"
            };
        }

        // ---------------------------
        // Frequent DID ranges (treat as heuristics; OEMs vary)
        // ---------------------------

        public static string DescribeDid(ushort did)
        {
            // Specific common DIDs from your notes
            return did switch
            {
                0xF190 => "VIN (common)",
                0xF187 => "VIN (alt/common)",
                0xF18C => "Serial Number (often)",
                0xF188 => "Strategy / Part numbers (often)",
                0xF110 => "Strategy / Part numbers (often)",
                0xF163 => "Diagnostic specification level",
                0xF166 => "Build date / release code (sometimes BCD)",
                0xD03F => "Fingerprint / hash / security blob (example)",
                _ => DescribeDidRange(did)
            };
        }

        public static string DescribeDidRange(ushort did)
        {
            if (did >= 0xF100 && did <= 0xF1FF) return "Identification / calibration / serials (F1xx cluster)";
            if (did >= 0xD100 && did <= 0xD1FF) return "OEM custom / status (D1xx cluster)";
            return "—";
        }

        // ---------------------------
        // “Does this need Security or Session?” quick guess aid
        // ---------------------------

        public static string SecurityOrSessionGuess(byte sid, byte? sub = null)
        {
            // sid is the service ID
            return sid switch
            {
                0x2E => "Likely: Extended + Security (writes)",
                0x31 when sub == 0x01 => "Likely: Programming + Security (routine start for erase/flash)",
                0x34 => "Likely: Programming + Security (download)",
                0x23 => "Likely: Extended + Security (memory reads)",
                0x22 => "Often: Default OK for basic IDs; session/security for protected DIDs",
                _ => "—"
            };
        }

        // ---------------------------
        // Timing heuristic (good vs suspicious)
        // ---------------------------

        public static string TimingHeuristic(byte sid, bool isMultiFrame)
        {
            // From your sheet; keep as guideline only.
            if (sid == 0x22 && !isMultiFrame) return "Typical single DID: ~2–15 ms";
            if (isMultiFrame) return "Moderate multi-frame: ~15–80 ms (varies)";
            if (sid == 0x31) return "RoutineControl result: ~50–500 ms (or longer)";
            return "—";
        }
    }
}