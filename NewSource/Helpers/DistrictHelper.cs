using AIOverhaul.Constants;

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
        public static Logic.District.Def GetDistrictDefinition(Logic.Game game, string districtName)
        {
            return game?.defs?.Get<Logic.District.Def>(districtName);
        }

        /// <summary>
        /// Check if a castle has a specific district
        /// </summary>
        public static bool HasDistrict(Logic.Castle castle, string districtName)
        {
            var district = GetDistrictDefinition(castle?.game, districtName);
            return district != null && castle.HasDistrict(district);
        }

        /// <summary>
        /// Check if the realm has a Religious Settlement (Monastery, Mosque, or Shrine)
        /// </summary>
        public static bool HasReligiousSettlement(Logic.Realm realm)
        {
            if (realm == null) return false;
            return GetReligiousCount(realm) > 0;
        }

        /// <summary>
        /// Count of Keeps (forts) in the realm
        /// </summary>
        public static int GetKeepCount(Logic.Realm realm)
        {
            if (realm?.settlements == null) return 0;
            int count = 0;
            foreach (var s in realm.settlements)
            {
                if (s?.def?.id == SettlementNames.Keep)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Count of Villages in the realm
        /// </summary>
        public static int GetVillageCount(Logic.Realm realm)
        {
            if (realm?.settlements == null) return 0;
            int count = 0;
            foreach (var s in realm.settlements)
            {
                if (s?.def?.id == SettlementNames.Village)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Count of Religious settlements (Monastery, Mosque, Shrine) in the realm
        /// </summary>
        public static int GetReligiousCount(Logic.Realm realm)
        {
            if (realm?.settlements == null) return 0;
            int count = 0;
            foreach (var s in realm.settlements)
            {
                if (s?.def != null && IsReligiousSettlement(s.def.id))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Count of Farms in the realm
        /// </summary>
        public static int GetFarmCount(Logic.Realm realm)
        {
            if (realm?.settlements == null) return 0;
            int count = 0;
            foreach (var s in realm.settlements)
            {
                if (s?.def?.id == SettlementNames.Farm)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Count of Coastal settlements in the realm
        /// Excludes castle, includes ruined settlements (they rebuild over time)
        /// </summary>
        public static int GetCoastalCount(Logic.Realm realm)
        {
            if (realm?.settlements == null) return 0;
            int count = 0;
            foreach (var s in realm.settlements)
            {
                // Exclude castle, include ruined (they rebuild), check coastal flag
                if (s != null && s != realm.castle && s.coastal)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Check if a settlement ID corresponds to a Religious Settlement (Monastery, Mosque, Shrine)
        /// </summary>
        private static bool IsReligiousSettlement(string id)
        {
            return id == SettlementNames.Monastery ||
                   id == SettlementNames.Mosque ||
                   id == SettlementNames.Shrine;
        }
    }
}
