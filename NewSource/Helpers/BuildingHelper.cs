namespace AIOverhaul.Helpers
{
    /// <summary>
    /// Helper methods for working with buildings
    /// </summary>
    public static class BuildingHelper
    {
        /// <summary>
        /// Check if a building is a religious building
        /// </summary>
        public static bool IsReligiousBuilding(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return false;

            // Religious buildings include Church, Masjid, Temple, Cathedral, GreatMosque
            return buildingId == BuildingNames.Church ||
                   buildingId == BuildingNames.Masjid ||
                   buildingId == BuildingNames.Temple ||
                   buildingId == BuildingNames.Cathedral ||
                   buildingId == BuildingNames.GreatMosque;
        }

        /// <summary>
        /// Count how many religion building slots exist in a district definition
        /// </summary>
        public static int CountReligionSlots(Logic.Castle castle, Logic.District.Def religionDistrict)
        {
            if (religionDistrict?.buildings == null) return 0;

            // Count how many religion building slots exist in this district definition
            return religionDistrict.buildings.Count;
        }
    }
}
