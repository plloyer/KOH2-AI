using System;
using Logic;

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

        /// <summary>
        /// Count how many religion building slots exist in a district definition
        /// </summary>
        public static int CountReligionSlots(this District.Def religionDistrict)
        {
            if (religionDistrict?.buildings == null) return 0;

            // Count how many religion building slots exist in this district definition
            return religionDistrict.buildings.Count;
        }
    }
}
