#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoDecoder.Protocols.Conversations;

public static class UdsConversationBuilder
{
    public static List<UdsTransaction> Build(IEnumerable<IsoTpPdu> pdus)
    {
        // Track open requests by (direction, canId, sid, did?) - but direction varies by logs.
        // We'll match primarily by: request SID, time proximity, and response CAN ID heuristic (+0x8).
        var open = new List<UdsTransaction>();
        var done = new List<UdsTransaction>();

        foreach (var pdu in pdus.OrderBy(p => p.StartLine))
        {
            if (pdu.Payload.Length == 0) continue;

            // Identify request vs response
            if (IsNegativeResponse(pdu.Payload, out var origSid, out var nrc))
            {
                var match = FindMatch(open, pdu, origSid);
                if (match != null)
                {
                    match.ResponseLine = pdu.EndLine;
                    match.ResponseTime = pdu.EndTime;
                    match.ResponseCanId = pdu.CanId;
                    match.Nrc = nrc;
                    match.LatencyMs = ComputeLatency(match.RequestTime, match.ResponseTime);
                    done.Add(match);
                    open.Remove(match);
                }
                else
                {
                    // orphan response: still record as standalone
                    done.Add(new UdsTransaction
                    {
                        RequestLine = pdu.StartLine,
                        RequestTime = pdu.StartTime,
                        RequestCanId = pdu.CanId,
                        ServiceId = origSid,
                        Nrc = nrc
                    });
                }
            }
            else if (IsPositiveResponse(pdu.Payload, out var posSid))
            {
                byte reqSid = (byte)(posSid - 0x40);
                var match = FindMatch(open, pdu, reqSid);
                if (match != null)
                {
                    match.ResponseLine = pdu.EndLine;
                    match.ResponseTime = pdu.EndTime;
                    match.ResponseCanId = pdu.CanId;
                    match.PositiveSid = posSid;
                    match.LatencyMs = ComputeLatency(match.RequestTime, match.ResponseTime);
                    done.Add(match);
                    open.Remove(match);
                }
                else
                {
                    done.Add(new UdsTransaction
                    {
                        RequestLine = pdu.StartLine,
                        RequestTime = pdu.StartTime,
                        RequestCanId = pdu.CanId,
                        ServiceId = reqSid,
                        PositiveSid = posSid
                    });
                }
            }
            else
            {
                // Treat as request
                byte sid = pdu.Payload[0];
                ushort? did = TryReadDid(pdu.Payload);

                open.Add(new UdsTransaction
                {
                    RequestLine = pdu.StartLine,
                    RequestTime = pdu.StartTime,
                    RequestCanId = pdu.CanId,
                    ServiceId = sid,
                    Did = did
                });

                // Keep list bounded (avoid memory growth on weird logs)
                if (open.Count > 500)
                    open.RemoveRange(0, open.Count - 500);
            }
        }

        // Anything left open can be returned as incomplete
        done.AddRange(open);
        return done;
    }

    private static UdsTransaction? FindMatch(List<UdsTransaction> open, IsoTpPdu response, byte reqSid)
    {
        // Prefer most recent request with:
        // 1) matching SID
        // 2) likely CAN ID pair (+0x8 is common 0x7D0->0x7D8)
        // 3) within time window (<= 5s when timestamps exist)
        int expectedReq = response.CanId >= 0x8
     ? response.CanId - 0x8
     : response.CanId;

        var candidates = open
            .Where(r => r.ServiceId == reqSid)
            .OrderByDescending(r => r.RequestLine)
            .ToList();

        // First pass: CAN-ID heuristic + time window
        foreach (var r in candidates)
        {
            bool canOk = r.RequestCanId == expectedReq || r.RequestCanId == response.CanId;
            bool timeOk = WithinSeconds(r.RequestTime, response.StartTime, 5);

            if (canOk && timeOk)
                return r;
        }

        // Second pass: time only
        foreach (var r in candidates)
        {
            if (WithinSeconds(r.RequestTime, response.StartTime, 5))
                return r;
        }

        // Third pass: last matching SID (no timestamps)
        return candidates.FirstOrDefault();
    }

    private static bool IsNegativeResponse(byte[] payload, out byte origSid, out byte nrc)
    {
        origSid = 0;
        nrc = 0;
        if (payload.Length < 3) return false;
        if (payload[0] != 0x7F) return false;

        origSid = payload[1];
        nrc = payload[2];
        return true;
    }

    private static bool IsPositiveResponse(byte[] payload, out byte posSid)
    {
        posSid = 0;
        if (payload.Length < 1) return false;
        byte sid = payload[0];

        // UDS positive responses are typically requestSid + 0x40 (0x50,0x62,0x67,0x71,...)
        if (sid >= 0x40 && sid != 0x7F)
        {
            posSid = sid;
            return true;
        }
        return false;
    }

    private static ushort? TryReadDid(byte[] payload)
    {
        // Only safe extraction for ReadDID request/response
        // Request: 22 DIDhi DIDlo
        // Positive: 62 DIDhi DIDlo ...
        if (payload.Length < 3) return null;
        byte sid = payload[0];

        if (sid is 0x22 or 0x62)
            return (ushort)((payload[1] << 8) | payload[2]);

        // Negative response sometimes includes DID for 0x22 depending on log source; ignore here.
        return null;
    }

    private static bool WithinSeconds(DateTime? a, DateTime? b, int seconds)
    {
        if (!a.HasValue || !b.HasValue) return true;
        return Math.Abs((a.Value - b.Value).TotalSeconds) <= seconds;
    }

    private static double? ComputeLatency(DateTime? a, DateTime? b)
    {
        if (!a.HasValue || !b.HasValue) return null;
        return (b.Value - a.Value).TotalMilliseconds;
    }
}