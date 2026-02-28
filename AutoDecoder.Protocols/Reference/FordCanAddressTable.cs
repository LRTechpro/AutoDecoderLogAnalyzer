#nullable enable
namespace AutoDecoder.Protocols.Reference
{
    public static class FordCanAddressTable
    {
        public static bool IsObdFunctional(int canId) => canId == 0x7DF;

        public static bool IsTypicalPhysicalRequest(int canId) => canId >= 0x7E0 && canId <= 0x7E7;

        public static bool IsTypicalPhysicalResponse(int canId) => canId >= 0x7E8 && canId <= 0x7EF;

        public static string Classify(int canId)
        {
            if (IsObdFunctional(canId)) return "Functional request (0x7DF)";
            if (IsTypicalPhysicalRequest(canId)) return "Physical request (0x7E0–0x7E7)";
            if (IsTypicalPhysicalResponse(canId)) return "Physical response (0x7E8–0x7EF)";
            return "Other / OEM-specific";
        }
    }
}