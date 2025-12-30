namespace AIOverhaul.Helpers
{
    /// <summary>
    /// Helper methods for working with districts
    /// </summary>
    public static class DistrictHelper
    {
        /// <summary>
        /// Get a district definition by name
        /// </summary>
        public static Logic.District.Def GetDistrict(Logic.Game game, string districtName)
        {
            return game?.defs?.Get<Logic.District.Def>(districtName);
        }

        /// <summary>
        /// Check if a castle has a specific district
        /// </summary>
        public static bool HasDistrict(Logic.Castle castle, string districtName)
        {
            var district = GetDistrict(castle?.game, districtName);
            return district != null && castle.HasDistrict(district);
        }
    }
}
