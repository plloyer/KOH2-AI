namespace AIOverhaul
{
    /// <summary>
    /// Constants for settlement names/types defined in settlements.def
    /// </summary>
    public static class SettlementNames
    {
        // Special logic category
        public const string ReligiousSettlement = "ReligiousSettlement";

        // Concrete Settlement IDs
        public const string Village = "Village";
        public const string Farm = "Farm";
        public const string Keep = "Keep"; // Displayed as "Castle" or "Town" in-game / Fort

        // Religious
        public const string Monastery = "Monastery";
        public const string Mosque = "Mosque";
        public const string Shrine = "Shrine";

        // Resources / Special
        public const string Mine = "Mine";
        public const string Quarry = "Quarry";
        public const string LoggingCamp = "LoggingCamp";

        // Farms (Crop Farms)
        public const string CattleFarm = "CattleFarm";
        public const string SheepFarm = "SheepFarm";
        public const string HorseFarm = "HorseFarm";
        public const string Vineyard = "Vineyard";
        public const string HerbsGardens = "HerbsGardens";
        public const string FlaxField = "FlaxField";
        public const string CropFarm = "CropFarm";

        // Coastal
        public const string CoastalSettlement = "CoastalSettlement";
        public const string CoastalVillage = "CoastalVillage";
        public const string CoastalTown = "CoastalTown";
        public const string FishingVillage = "FishingVillage";

        // Empty/System
        public const string Empty = "Empty";
    }
}
