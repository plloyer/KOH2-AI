using System.Collections.Generic;
using AIOverhaul.Constants;

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

        /// <summary>
        /// Returns the count of unique trade goods potentially produced by a fully upgraded building.
        /// </summary>
        public static int GetPotentialGoodsCount(string buildingId)
        {
            var goods = GetPotentialGoods(buildingId);
            return goods != null ? goods.Count : 0;
        }

        /// <summary>
        /// Returns a list of trade goods potentially produced by a fully upgraded building.
        /// </summary>
        public static List<string> GetPotentialGoods(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return new List<string>();

            switch (buildingId)
            {
                // Farming
                case BuildingNames.FlaxGrowing:
                    return new List<string> { GoodsNames.Oils, GoodsNames.Linen, GoodsNames.Canvas, GoodsNames.Ropes };
                case BuildingNames.HerbGardening:
                    return new List<string> { GoodsNames.Herbs, GoodsNames.Wax, GoodsNames.Spices, GoodsNames.Candles, GoodsNames.Dyes };
                case BuildingNames.Viticulture:
                    return new List<string> { GoodsNames.Raisins, GoodsNames.Wine, GoodsNames.Spirits };
                case BuildingNames.SheepFarming:
                    return new List<string> { GoodsNames.Meat, GoodsNames.Wool, GoodsNames.Leather, GoodsNames.Parchment };
                case BuildingNames.CattleFarming:
                    return new List<string> { GoodsNames.Meat, GoodsNames.FineCheese, GoodsNames.Leather, GoodsNames.Sausages, GoodsNames.DraftOxen };
                case BuildingNames.HorseBreeding:
                    return new List<string> { GoodsNames.TrainedHorses, GoodsNames.WarHorses };

                // Industry
                case BuildingNames.Woodworking:
                    return new List<string> { GoodsNames.Timber, GoodsNames.Tar, GoodsNames.Charcoal, GoodsNames.Barrels, GoodsNames.Glass, GoodsNames.Furniture };
                case BuildingNames.Stoneworking:
                    return new List<string> { GoodsNames.Marble, GoodsNames.Sculptures, GoodsNames.Masons };
                case BuildingNames.Metalworking:
                    return new List<string> { GoodsNames.Iron };
                
                // Resources
                case BuildingNames.LimeTrade: return new List<string> { GoodsNames.Lime };
                case BuildingNames.SilverSmelting: return new List<string> { GoodsNames.Silver };
                case BuildingNames.GoldSmelting: return new List<string> { GoodsNames.Gold };
                case BuildingNames.LodestoneTrade: return new List<string> { GoodsNames.Lodestone };
                case BuildingNames.MineralsTrade: return new List<string> { GoodsNames.Minerals };
                case BuildingNames.SulfurTrade: return new List<string> { GoodsNames.Sulfur };
                case BuildingNames.SaltTrade: return new List<string> { GoodsNames.Salt, GoodsNames.Barrels }; // Salt produces Barrels too? No, checking logic... SaltTrade: produces Salt, Barrels (Yes in visual line 56)
                case BuildingNames.SaltpeterTrade: return new List<string> { GoodsNames.Saltpeter };
                case BuildingNames.AmberTrade: return new List<string> { GoodsNames.Amber };
                case BuildingNames.FurTrade: return new List<string> { GoodsNames.Furs };
                case BuildingNames.CamelsTrade: return new List<string> { GoodsNames.TrainedCamels };

                // Coastal
                case BuildingNames.Harbor:
                    return new List<string> { GoodsNames.SaltedFish };
                case BuildingNames.Admiralty:
                    return new List<string> { GoodsNames.NavigationMaps };
                case BuildingNames.TradePort:
                    // Expeditions produces Explorers, but strictly goods? Let's exclude Explorers for now if debatable, but keeping in logic if requested.
                    // User said "major goods". Explorers are a mechanic. I'll omit unique mechanic resources if unnecessary, but NavigationMaps is a good.
                    // Let's stick to tradeable goods.
                    return new List<string>(); // TradePort mainly enables trade, doesn't produce "goods" like Wine.
                case BuildingNames.Shipyard:
                    return new List<string> { GoodsNames.Sails };

                // City
                case BuildingNames.Artisans:
                    // TailorsShop -> FineClothing
                    // ShoemakersShop -> FineBoots
                    // RugsWeaver -> Carpets
                    // JewellersShop -> Jewellery
                    return new List<string> { GoodsNames.FineClothing, GoodsNames.FineBoots, GoodsNames.Carpets, GoodsNames.Jewellery };
                case BuildingNames.MerchantsGuild:
                    // TradeFair -> Merchants (Resource)
                    // Caravanserai -> Merchants
                    // Bazaar -> FineClothing, Jewellery, Spices (Does it produce them? Line 477: "FineClothing\nJewellery\nSpices" in Requires column).
                    // So Bazaar REQUIRES them.
                    // Only "Merchants" produced.
                    return new List<string>(); 

                // Church
                case BuildingNames.Church:
                    return new List<string> { GoodsNames.Ink }; // InkMaker
                case BuildingNames.Cathedral:
                     // ChristianCeremonies -> Clergy
                    return new List<string>(); 
                case BuildingNames.University:
                    // AlchemistLab -> Gunpowder
                    // Observatory -> Compasses
                    // GreatLibrary -> IllustratedBooks
                    // TranslatorsChambers -> Scholars
                    return new List<string> { GoodsNames.Gunpowder, GoodsNames.Compasses, GoodsNames.IllustratedBooks };
                
                // City Food
                case BuildingNames.FoodMarket:
                    // Bakery -> Pastries
                    // Alehouse -> Ale
                    return new List<string> { GoodsNames.Pastries, GoodsNames.Ale };

                default:
                    return new List<string>();
            }
        }
    }
}
