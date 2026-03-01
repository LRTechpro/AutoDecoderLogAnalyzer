#nullable enable
using System;
using System.Collections.Generic;

namespace AutoDecoder.Protocols.Utilities
{
    public readonly record struct ModuleInfo(string Abbrev, string Name)
    {
        public override string ToString() => $"{Abbrev} — {Name}";
    }

    public static class ModuleAddressBook
    {
        // NOTE: Fill this dictionary with your full sheet over time.
        // Keys are 11-bit CAN IDs expressed as integers (hex).
        private static readonly Dictionary<int, ModuleInfo> _byCanId = new()
        {
            // --- Examples from what is visible in your screenshots ---
            [0x7D0] = new ModuleInfo("APIM", "Accessory Protocol Interface Module"),
            [0x726] = new ModuleInfo("BCM", "Body Control Module"),
            [0x7E0] = new ModuleInfo("PCM", "Powertrain Control Module"),
            [0x730] = new ModuleInfo("PSCM", "Power Steering Control Module"),
            [0x737] = new ModuleInfo("RCM", "Restraints Control Module"),
            [0x716] = new ModuleInfo("GWM", "Gateway Module A"),
            [0x720] = new ModuleInfo("IPC", "Instrument Panel Cluster"),
            [0x754] = new ModuleInfo("TCU", "Telematic Control Unit Module"),
            [0x792] = new ModuleInfo("ATCM", "All Terrain Control Module"),
            [0x703] = new ModuleInfo("AWD", "All Wheel Drive"),
            [0x7C1] = new ModuleInfo("CMR", "Camera Module Rear (Driver Status Monitor Camera Module)"),
            [0x734] = new ModuleInfo("HCM", "Headlamp Control Module"),
            [0x7B2] = new ModuleInfo("HUD", "Heads Up Display Module"),
            [0x733] = new ModuleInfo("HVAC", "Heating, Ventilation, and Air Conditioning Module"),
            [0x706] = new ModuleInfo("IPMA", "Image Processing Module A"),
            [0x7B1] = new ModuleInfo("IPMB", "Image Processing Module B"),
            [0x6F6] = new ModuleInfo("LDCMA", "Lighting Driver Control Module A"),
            [0x6F7] = new ModuleInfo("LDCMB", "Lighting Driver Control Module B"),
            [0x6F5] = new ModuleInfo("OBCC", "Off-Board Charger Controller"),
            [0x765] = new ModuleInfo("OCS", "Occupant Classification System Module"),
            [0x750] = new ModuleInfo("PACM", "Pedestrian Alert Control Module"),
            [0x736] = new ModuleInfo("PAM", "Parking Assist Control Module"),
            [0x741] = new ModuleInfo("PDM", "Passenger Door Module"),
            [0x774] = new ModuleInfo("RACM", "Rear Audio Control Module"),
            [0x766] = new ModuleInfo("RBM", "Running Board Control Module"),
            [0x731] = new ModuleInfo("RFA", "Remote Function Actuator"),
            [0x775] = new ModuleInfo("RGTM", "Rear Gate Trunk Module"),
            [0x751] = new ModuleInfo("RTM", "Radio Transceiver Module"),
            [0x797] = new ModuleInfo("SASM", "Steering Angle Sensor Module"),
            [0x724] = new ModuleInfo("SCCM", "Steering Column Control Module"),
            [0x7A3] = new ModuleInfo("SCMB", "Passenger Front Seat Module"),
            [0x702] = new ModuleInfo("SCMC", "Seat Control Module C"),
            [0x763] = new ModuleInfo("SCMD", "Seat Control Module D"),
            [0x776] = new ModuleInfo("SCME", "Front Seat Climate Control Module"),
            [0x777] = new ModuleInfo("SCMF", "Rear Seat Climate Control Module"),
            [0x712] = new ModuleInfo("SCMG", "Driver Multi-Contour Seat Module"),
            [0x713] = new ModuleInfo("SCMH", "Passenger Multi-Contour Seat Module"),
            [0x787] = new ModuleInfo("SCMJ", "Seat Control Module J"),
            [0x7C5] = new ModuleInfo("SECM", "Steering Effort Control Module"),
            [0x7E2] = new ModuleInfo("SOBDM", "Secondary On-Board Diagnostic Control Module A"),
            [0x7E7] = new ModuleInfo("SOBDMB", "Secondary On-Board Diagnostic Control Module B"),
            [0x7E6] = new ModuleInfo("SOBDMC", "Secondary On-Board Diagnostic Control Module C"),
            [0x6F2] = new ModuleInfo("SODCMC", "Side Obstacle Detection Control Module C"),
            [0x6F3] = new ModuleInfo("SODCMD", "Side Obstacle Detection Control Module D"),
            [0x7C4] = new ModuleInfo("SODL", "Side Obstacle Detection Control Module LH"),
            [0x7C6] = new ModuleInfo("SODR", "Side Obstacle Detection Control Module RH"),
            [0x761] = new ModuleInfo("TCCM", "Transfer Case Control Module"),
            [0x7E9] = new ModuleInfo("TCM", "Transmission Control Module"),
            [0x791] = new ModuleInfo("TRM/TBM", "Trailer Relay Module / Trailer Brake Control Module"),
            [0x721] = new ModuleInfo("VDM", "Vehicle Dynamics Control Module"),
            [0x725] = new ModuleInfo("WACM", "Wireless Accessory Charging Module"),
        };

        public static bool TryGet(int canId, out ModuleInfo info)
            => _byCanId.TryGetValue(canId, out info);

        public static string Format(int canId)
        {
            bool isExtended = canId > 0x7FF;
            string hex = isExtended ? $"0x{canId:X8}" : $"0x{canId:X3}";

            if (TryGet(canId, out var info))
                return $"{info.Abbrev} ({hex}) — {info.Name}";

            return hex;
        }
        public static void AddOrUpdate(int canId, string abbrev, string name)
        {
            _byCanId[canId] = new ModuleInfo(abbrev, name);
        }
        public static int Count => _byCanId.Count;
    }
}
