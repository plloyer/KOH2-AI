using System;

namespace AIOverhaul
{
    /// <summary>
    /// Helper methods for working with districts
    /// </summary>
    public static class DistrictHelper
    {
        public static bool IsReligiousSettlement(string id)
        {
            return id == SettlementNames.Monastery ||
                id == SettlementNames.Mosque ||
                id == SettlementNames.Shrine;
        }
    }
}
