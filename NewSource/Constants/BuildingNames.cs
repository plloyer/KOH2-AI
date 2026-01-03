// Total buildings: 47
// Source: C:\Program Files (x86)\Steam\steamapps\common\Knights of Honor II\Defs\buildings.def
// Organized by building type

namespace AIOverhaul
{
    /// <summary>
    /// Constants for all building names in Knights of Honor II
    /// </summary>
    public static class BuildingNames
    {
        // Military
        public const string Barracks = "Barracks";
        public const string RoyalArmory = "RoyalArmory";
        public const string LordsCastles = "LordsCastles";
        public const string RoyalPalace = "RoyalPalace";

        // Population
        public const string MarketSquare = "MarketSquare";
        public const string Housings = "Housings";
        public const string VillageMilitia = "VillageMilitia";
        public const string Artisans = "Artisans";
        public const string MerchantsGuild = "MerchantsGuild";

        // Religion
        public const string Church = "Church";
        public const string Cathedral = "Cathedral";
        public const string University = "University";
        public const string Masjid = "Masjid";
        public const string GreatMosque = "GreatMosque";
        public const string Madrasah = "Madrasah";
        public const string Temple = "Temple";

        // Farming
        public const string CropFarming = "CropFarming";
        public const string Windmill = "Windmill";
        public const string FoodMarket = "FoodMarket";

        // Sea Shore
        public const string Harbor = "Harbor";
        public const string Admiralty = "Admiralty";
        public const string Shipyard = "Shipyard";
        public const string TradePort = "TradePort";

        // Others
        public const string Irrigation = "Irrigation";
        public const string SheepFarming = "SheepFarming";
        public const string CattleFarming = "CattleFarming";
        public const string HillFort = "HillFort";
        public const string FlaxGrowing = "FlaxGrowing";
        public const string HerbGardening = "HerbGardening";
        public const string Viticulture = "Viticulture";
        public const string HorseBreeding = "HorseBreeding";
        public const string Woodworking = "Woodworking";
        public const string AmberTrade = "AmberTrade";
        public const string CamelsTrade = "CamelsTrade";
        public const string FurTrade = "FurTrade";
        public const string MineralsTrade = "MineralsTrade";
        public const string SaltTrade = "SaltTrade";
        public const string SaltpeterTrade = "SaltpeterTrade";
        public const string SulfurTrade = "SulfurTrade";
        public const string RiverTrade = "RiverTrade";
        public const string Stoneworking = "Stoneworking";
        public const string LimeTrade = "LimeTrade";
        public const string Metalworking = "Metalworking";
        public const string SilverSmelting = "SilverSmelting";
        public const string GoldSmelting = "GoldSmelting";
        public const string LodestoneTrade = "LodestoneTrade";
        public const string PirateHaven = "PirateHaven";

        /// <summary>
        /// Get all building names as an array
        /// </summary>
        public static readonly string[] All = new[]
        {
            Housings,
            MarketSquare,
            Church,
            Masjid,
            Temple,
            HillFort,
            FlaxGrowing,
            HerbGardening,
            Viticulture,
            SheepFarming,
            CattleFarming,
            HorseBreeding,
            Woodworking,
            AmberTrade,
            CamelsTrade,
            FurTrade,
            MineralsTrade,
            SaltTrade,
            SaltpeterTrade,
            SulfurTrade,
            Harbor,
            PirateHaven,
            Admiralty,
            Shipyard,
            TradePort,
            Barracks,
            RoyalArmory,
            LordsCastles,
            RoyalPalace,
            VillageMilitia,
            RiverTrade,
            Artisans,
            MerchantsGuild,
            Cathedral,
            GreatMosque,
            University,
            Madrasah,
            CropFarming,
            Irrigation,
            Windmill,
            FoodMarket,
            Stoneworking,
            LimeTrade,
            Metalworking,
            SilverSmelting,
            GoldSmelting,
            LodestoneTrade
        };
    }
}