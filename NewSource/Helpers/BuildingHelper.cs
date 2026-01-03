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
        /// Returns the count of unique trade goods CURRENTLY produced by a castle.
        /// </summary>
        public static int GetCurrentGoodsCount(Logic.Castle castle)
        {
            var goods = GetCurrentGoods(castle);
            return goods != null ? goods.Count : 0;
        }

        /// <summary>
        /// Returns a list of unique trade goods CURRENTLY produced by a castle.
        /// </summary>
        public static List<string> GetCurrentGoods(Logic.Castle castle)
        {
            if (castle == null || castle.buildings == null) return new List<string>();

            var activeGoods = new HashSet<string>();

            foreach (var building in castle.buildings)
            {
                if (building == null || building.def == null) continue;

                var goods = GetGoodsProducedByDef(building.def.id);
                foreach (var good in goods)
                {
                    activeGoods.Add(good);
                }
            }

            return new List<string>(activeGoods);
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
        /// (Aggregates all possible upgrades for the given base building ID)
        /// </summary>
        public static List<string> GetPotentialGoods(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return new List<string>();

            // This function returns the "Potential" of the tree.
            // We can reuse the granular mapping if we know the upgrade IDs, 
            // OR we can keep the manual aggregation for "Potential" queries if precise upgrade paths are hard to traverse here.
            // For now, I will keep the manual aggregation logic but updated with the accurate CSV data I found.
            
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
                
                // Resources (Base buildings that produce goods directly)
                case BuildingNames.LimeTrade: return new List<string> { GoodsNames.Lime };
                case BuildingNames.SilverSmelting: return new List<string> { GoodsNames.Silver };
                case BuildingNames.GoldSmelting: return new List<string> { GoodsNames.Gold };
                case BuildingNames.LodestoneTrade: return new List<string> { GoodsNames.Lodestone };
                case BuildingNames.MineralsTrade: return new List<string> { GoodsNames.Minerals };
                case BuildingNames.SulfurTrade: return new List<string> { GoodsNames.Sulfur };
                case BuildingNames.SaltTrade: return new List<string> { GoodsNames.Salt };
                case BuildingNames.SaltpeterTrade: return new List<string> { GoodsNames.Saltpeter };
                case BuildingNames.AmberTrade: return new List<string> { GoodsNames.Amber };
                case BuildingNames.FurTrade: return new List<string> { GoodsNames.Furs };
                case BuildingNames.CamelsTrade: return new List<string> { GoodsNames.TrainedCamels };

                // Coastal
                case BuildingNames.Harbor:
                    return new List<string> { GoodsNames.SaltedFish }; // From FishMarket
                case BuildingNames.Admiralty:
                    return new List<string> { GoodsNames.NavigationMaps }; // From Cartographer
                case BuildingNames.Shipyard:
                    return new List<string> { GoodsNames.Sails }; // From SailMaker

                // City
                case BuildingNames.Artisans:
                    return new List<string> { GoodsNames.FineClothing, GoodsNames.FineBoots, GoodsNames.Carpets, GoodsNames.Jewellery };
                    
                // Church/Education
                case BuildingNames.Church:
                    return new List<string> { GoodsNames.Ink }; // InkMaker
                case BuildingNames.University:
                    return new List<string> { GoodsNames.Gunpowder, GoodsNames.Compasses, GoodsNames.IllustratedBooks };
                
                // City Food
                case BuildingNames.FoodMarket:
                    return new List<string> { GoodsNames.Pastries, GoodsNames.Ale };

                default:
                    return new List<string>();
            }
        }

        /// <summary>
        /// Helper to get the goods produced by a SPECIFIC building or upgrade definition ID.
        /// </summary>
        private static List<string> GetGoodsProducedByDef(string defId)
        {
             switch (defId)
             {
                // Base Resources
                case BuildingNames.AmberTrade: return new List<string> { GoodsNames.Amber };
                case BuildingNames.CamelsTrade: return new List<string> { GoodsNames.TrainedCamels };
                case BuildingNames.FurTrade: return new List<string> { GoodsNames.Furs };
                case BuildingNames.MineralsTrade: return new List<string> { GoodsNames.Minerals };
                case BuildingNames.SaltTrade: return new List<string> { GoodsNames.Salt }; // Assuming Barrels are required, not produced
                case BuildingNames.SaltpeterTrade: return new List<string> { GoodsNames.Saltpeter };
                case BuildingNames.SulfurTrade: return new List<string> { GoodsNames.Sulfur };
                case BuildingNames.LimeTrade: return new List<string> { GoodsNames.Lime };
                case BuildingNames.SilverSmelting: return new List<string> { GoodsNames.Silver };
                case BuildingNames.GoldSmelting: return new List<string> { GoodsNames.Gold };
                case BuildingNames.LodestoneTrade: return new List<string> { GoodsNames.Lodestone };

                // Upgrades - Farming
                case "Butcher": // Generic Butcher upgrade name? Or specific per type?
                case "Butcher_Sheep": 
                case "Butcher_Cattle":
                    return new List<string> { GoodsNames.Meat };
                
                case "Tannery":
                case "Tannery_Sheep":
                case "Tannery_Cattle":
                    return new List<string> { GoodsNames.Leather };

                case "Apiary": return new List<string> { GoodsNames.Wax };
                case "SpiceShop": return new List<string> { GoodsNames.Spices };
                case "CandleMaker": return new List<string> { GoodsNames.Candles };
                case "DyeWorkshop": return new List<string> { GoodsNames.Dyes };
                case "OilPress": return new List<string> { GoodsNames.Oils };
                case "FlaxWeaver": return new List<string> { GoodsNames.Linen };
                case "CanvasMaker": return new List<string> { GoodsNames.Canvas };
                case "Ropewalk": return new List<string> { GoodsNames.Ropes };
                case "HerbalistShacks": return new List<string> { GoodsNames.Herbs };
                
                case "Winery": return new List<string> { GoodsNames.Wine };
                case "Distillery": return new List<string> { GoodsNames.Spirits };
                case "SunDryingGrapes": return new List<string> { GoodsNames.Raisins };

                case "WoolWeaver": return new List<string> { GoodsNames.Wool };
                case "ParchmentMaker": return new List<string> { GoodsNames.Parchment };
                
                case "CattleMarket": return new List<string> { GoodsNames.DraftOxen };
                case "DairyShop": return new List<string> { GoodsNames.FineCheese };
                case "SausageMaker": return new List<string> { GoodsNames.Sausages };

                case "HorseMarket": return new List<string> { GoodsNames.TrainedHorses };
                case "WarhorseBreed": return new List<string> { GoodsNames.WarHorses };

                // Upgrades - Industry
                case "Sawmill": return new List<string> { GoodsNames.Timber };
                case "TarPit": return new List<string> { GoodsNames.Tar };
                case "ColliersKiln": return new List<string> { GoodsNames.Charcoal };
                case "CoopersShop": return new List<string> { GoodsNames.Barrels };
                case "Glassworks": return new List<string> { GoodsNames.Glass };
                case "MasterJoiner": return new List<string> { GoodsNames.Furniture };
                
                case "StoneCutters": return new List<string> { GoodsNames.Marble };
                case "SculpturesShop": return new List<string> { GoodsNames.Sculptures };
                case "MasonsGuild": return new List<string> { GoodsNames.Masons };

                case "BlastFurnace": return new List<string> { GoodsNames.Iron };

                // Upgrades - City/Misc
                case "ShoemakersShop": return new List<string> { GoodsNames.FineBoots };
                case "JewellersShop": return new List<string> { GoodsNames.Jewellery };
                case "TailorsShop": return new List<string> { GoodsNames.FineClothing };
                case "RugsWeaver": return new List<string> { GoodsNames.Carpets };

                case "Alehouse": return new List<string> { GoodsNames.Ale };
                case "Bakery": return new List<string> { GoodsNames.Pastries };
                
                case "FishMarket": return new List<string> { GoodsNames.SaltedFish };
                case "Cartographer": return new List<string> { GoodsNames.NavigationMaps };
                case "SailMaker": return new List<string> { GoodsNames.Sails };
                
                case "InkMaker": // InkMaker appears in multiple contexts (Church, etc)
                case "InkMaker_Christian":
                case "InkMaker_Muslim":
                case "InkMaker_Pagan":
                    return new List<string> { GoodsNames.Ink };

                case "Apothecary":
                case "Apothecary_Christian":
                case "Apothecary_Muslim":
                case "Apothecary_Pagan":
                    return new List<string> { GoodsNames.Medicine };

                case "AlchemistLab": 
                case "AlchemistLab_Christian":
                case "AlchemistLab_Muslim":
                    return new List<string> { GoodsNames.Gunpowder };

                case "Observatory":
                case "Observatory_Christian":
                case "Observatory_Muslim":
                    return new List<string> { GoodsNames.Compasses };
                
                case "GreatLibrary":
                case "GreatLibrary_Christian":
                case "GreatLibrary_Muslim":
                    return new List<string> { GoodsNames.IllustratedBooks };

                case "TranslatorsChambers":
                case "TranslatorsChambers_Christian":
                case "TranslatorsChambers_Muslim":
                    // produces Scholars - not in GoodsNames
                    return new List<string>(); 

                case "ArtsSchool":
                case "ArtsSchool_Christian":
                case "ArtsSchool_Muslim":
                    return new List<string> { GoodsNames.FineArt }; // and Artists

                default:
                    return new List<string>();
             }
        }
    }
}
