#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AutoDecoder.Models;

namespace AutoDecoder.Protocols.Conversations;

public static class IsoTpReassembler
{
    private sealed class State
    {
        public int StartLine;
        public int LastLine;
        public DateTime? StartTime;
        public DateTime? LastTime;

        public string Direction = "UNK";
        public int CanId;

        public int ExpectedLength;
        public int NextSeq;
        public List<byte> Data = new();
    }

    /// <summary>
    /// Builds complete PDUs from ISO15765 lines.
    /// If a line doesn't look like ISO-TP (no PCI), it becomes a single-line PDU (A-path).
    /// If it does, we reassemble across lines (B-path).
    /// </summary>
    public static List<IsoTpPdu> Build(IEnumerable<LogLine> lines)
    {
        var result = new List<IsoTpPdu>();

        // keyed by (Direction, CanId)
        var states = new Dictionary<(string dir, int canId), State>();

        foreach (var ll in lines)
        {
            if (ll is not Iso15765Line iso) continue;

            var dir = iso.Direction ?? "UNK";
            var canId = iso.CanId ?? 0;
            var payload = iso.PayloadBytes; // after 4-byte header
            if (payload == null || payload.Length == 0)
                continue;

            // If it doesn't look like ISO-TP PCI, treat it as already-assembled payload (A)
            if (!LooksLikeIsoTpPci(payload[0]))
            {
                result.Add(new IsoTpPdu
                {
                    StartLine = iso.LineNumber,
                    EndLine = iso.LineNumber,
                    StartTime = iso.Timestamp,
                    EndTime = iso.Timestamp,
                    Direction = dir,
                    CanId = (int)canId,
                    Payload = payload.ToArray()
                });
                continue;
            }

            var key = (dir, (int)canId);

            // Basic "reset conditions" to avoid runaway state
            if (states.TryGetValue(key, out var existing))
            {
                // If line numbers jump a lot, or time jumps a lot, flush state
                if (iso.LineNumber <= existing.LastLine || (iso.Timestamp.HasValue && existing.LastTime.HasValue
                    && (iso.Timestamp.Value - existing.LastTime.Value).TotalSeconds > 3))
                {
                    // drop incomplete state
                    states.Remove(key);
                }
            }

            var pci = payload[0];
            int type = (pci >> 4) & 0xF;

            switch (type)
            {
                case 0x0: // SF
                    {
                        int len = pci & 0xF;
                        var data = payload.Skip(1).Take(len).ToArray();

                        result.Add(new IsoTpPdu
                        {
                            StartLine = iso.LineNumber,
                            EndLine = iso.LineNumber,
                            StartTime = iso.Timestamp,
                            EndTime = iso.Timestamp,
                            Direction = dir,
                            CanId = (int)canId,
                            Payload = data
                        });

                        // SF completes immediately, clear any old state
                        states.Remove(key);
                        break;
                    }

                case 0x1: // FF
                    {
                        if (payload.Length < 2) break;
                        int len = ((pci & 0xF) << 8) | payload[1];

                        var st = new State
                        {
                            StartLine = iso.LineNumber,
                            LastLine = iso.LineNumber,
                            StartTime = iso.Timestamp,
                            LastTime = iso.Timestamp,
                            Direction = dir,
                            CanId = (int)canId,
                            ExpectedLength = len,
                            NextSeq = 1
                        };

                        // First Frame data begins at byte 2
                        st.Data.AddRange(payload.Skip(2));

                        states[key] = st;
                        break;
                    }

                case 0x2: // CF
                    {
                        if (!states.TryGetValue(key, out var st)) break;

                        int seq = pci & 0xF;
                        // If sequence mismatch, drop the state
                        if (seq != (st.NextSeq & 0xF))
                        {
                            states.Remove(key);
                            break;
                        }

                        st.NextSeq++;
                        st.LastLine = iso.LineNumber;
                        st.LastTime = iso.Timestamp;

                        st.Data.AddRange(payload.Skip(1));

                        // If we have enough, finalize
                        if (st.Data.Count >= st.ExpectedLength)
                        {
                            var full = st.Data.Take(st.ExpectedLength).ToArray();
                            result.Add(new IsoTpPdu
                            {
                                StartLine = st.StartLine,
                                EndLine = st.LastLine,
                                StartTime = st.StartTime,
                                EndTime = st.LastTime,
                                Direction = st.Direction,
                                CanId = st.CanId,
                                Payload = full
                            });
                            states.Remove(key);
                        }

                        break;
                    }

                case 0x3: // FC (Flow Control)
                    {
                        // For now, ignore FC frames for payload building.
                        // Some logs include them; they don't carry UDS data.
                        break;
                    }

                default:
                    // Unknown PCI type; drop any existing state to be safe
                    states.Remove(key);
                    break;
            }
        }

        return result;
    }

    private static bool LooksLikeIsoTpPci(byte b)
    {
        int type = (b >> 4) & 0xF;
        return type is 0x0 or 0x1 or 0x2 or 0x3;
    }
}