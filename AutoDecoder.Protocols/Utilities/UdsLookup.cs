#nullable enable
using System;

namespace AutoDecoder.Protocols.Utilities
{
    /// <summary>
    /// Single source-of-truth lookup wrapper around UdsTables.
    /// Keeps Iso15765Line clean and prevents "guessing".
    /// </summary>
    public static class UdsLookup
    {
        public static string GetServiceName(byte sid)
            => UdsTables.ServiceName.TryGetValue(sid, out var name) ? name : "UnknownService";

        public static string GetNrcMeaning(byte nrc)
            => UdsTables.NrcMeaning.TryGetValue(nrc, out var meaning) ? meaning : "UnknownNRC";

        public static string GetDidName(ushort did)
            => UdsTables.DescribeDid(did); // returns specific DID if known, else range heuristic
    }
}