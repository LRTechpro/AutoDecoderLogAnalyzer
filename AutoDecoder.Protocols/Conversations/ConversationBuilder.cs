#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AutoDecoder.Models;

namespace AutoDecoder.Protocols.Conversations
{
    public static class ConversationBuilder
    {
        public static List<UdsTransaction> Build(IEnumerable<LogLine> lines)
        {
            var isoLines = lines
                .Where(l => l.Type == LineType.Iso15765)
                .OrderBy(l => l.LineNumber)
                .ToList();

            var transactions = new List<UdsTransaction>();

            for (int i = 0; i < isoLines.Count; i++)
            {
                var line = isoLines[i];

                if (!TryExtractService(line, out byte sid))
                    continue;

                // Only treat TX as request (simple rule)
                if (!line.Raw.Contains("TX")) continue;

                var tx = new UdsTransaction
                {
                    RequestLine = line.LineNumber,
                    RequestCanId = ExtractCanId(line), // now int
                    ServiceId = sid,
                    RequestTime = line.Timestamp
                };

                // Look forward for response
                for (int j = i + 1; j < isoLines.Count; j++)
                {
                    var candidate = isoLines[j];

                    if (!TryExtractService(candidate, out byte respSid))
                        continue;

                    // positive response SID = request + 0x40
                    if (respSid == (byte)(sid + 0x40))
                    {
                        tx.ResponseLine = candidate.LineNumber;
                        tx.PositiveSid = respSid;
                        tx.ResponseTime = candidate.Timestamp;
                        tx.ResponseCanId = ExtractCanId(candidate); // now int
                        tx.LatencyMs = CalculateLatency(tx);
                        break;
                    }

                    // negative response 7F
                    if (respSid == 0x7F && TryExtractNrc(candidate, out byte nrc))
                    {
                        tx.ResponseLine = candidate.LineNumber;
                        tx.Nrc = nrc;
                        tx.ResponseTime = candidate.Timestamp;
                        tx.ResponseCanId = ExtractCanId(candidate); // now int
                        tx.LatencyMs = CalculateLatency(tx);
                        break;
                    }
                }

                transactions.Add(tx);
            }

            return transactions;
        }

        private static double? CalculateLatency(UdsTransaction tx)
        {
            if (tx.RequestTime.HasValue && tx.ResponseTime.HasValue)
                return (tx.ResponseTime.Value - tx.RequestTime.Value).TotalMilliseconds;

            return null;
        }

        private static bool TryExtractService(LogLine line, out byte sid)
        {
            sid = 0;

            var bytes = line.ExtractHexBytes();
            if (bytes == null || bytes.Length < 1) return false;

            sid = bytes[0];
            return true;
        }

        private static bool TryExtractNrc(LogLine line, out byte nrc)
        {
            nrc = 0;

            var bytes = line.ExtractHexBytes();
            if (bytes == null || bytes.Length < 3) return false;

            if (bytes[0] == 0x7F)
            {
                nrc = bytes[2];
                return true;
            }

            return false;
        }

        // ✅ FIX: int matches LogLine.CanId (int?)
        private static int ExtractCanId(LogLine line)
            => line.CanId ?? 0;
    }
}