using System;
using System.Collections.Generic;

namespace AIOverhaul
{
    public static class GoodsHelper
    {
        // Cache to avoid iterating defs every frame
        public static List<Logic.Building.Def> ProductiveBuildings { get; set; }

        public static void EnsureCache(Logic.Game game)
        {
            if (ProductiveBuildings != null) return;
            ProductiveBuildings = new List<Logic.Building.Def>();

            if (game?.defs == null) return;

            var allBuildings = game.defs.GetDefs<Logic.Building.Def>();
            if (allBuildings == null) return;

            foreach (var def in allBuildings)
            {
                if (def == null) continue;
                bool producesGoods = false;

                // Check basics
                if (HasTradeGood(def.produces, game)) producesGoods = true;
                else if (HasTradeGood(def.produces_completed, game)) producesGoods = true;

                if (producesGoods)
                {
                    ProductiveBuildings.Add(def);
                }
            }
        }

        public static bool HasTradeGood(List<Logic.Building.Def.ProducedResource> list, Logic.Game game)
        {
            if (list == null) return false;
            foreach (var p in list)
            {
                if (IsTradeGood(p.resource, game)) return true;
            }
            return false;
        }

        public static bool IsTradeGood(string resourceName, Logic.Game game)
        {
            if (string.IsNullOrEmpty(resourceName) || game == null) return false;

            // NATIVE DETECT: Check if a Resource.Def exists and has a Name.
            // Base resources (Gold, Food, etc.) do not have a loaded Resource.Def with a Name.
            var def = game.defs.Find<Logic.Resource.Def>(resourceName);
            if (def != null && !string.IsNullOrEmpty(def.Name))
            {
                return true;
            }
            return false;
        }

        public static void AddGoodsFromList(List<Logic.Building.Def.ProducedResource> list, HashSet<string> set, Logic.Game game)
        {
            if (list == null) return;
            foreach (var p in list)
            {
                if (IsTradeGood(p.resource, game))
                {
                    set.Add(p.resource);
                }
            }
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
        public static List<string> GetGoodsProducedByDef(string defId)
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
                case BuildingUpgradeNames.Butcher:
                case BuildingUpgradeNames.Butcher_Sheep:
                case BuildingUpgradeNames.Butcher_Cattle:
                    return new List<string> { GoodsNames.Meat };

                case BuildingUpgradeNames.Tannery:
                case BuildingUpgradeNames.Tannery_Sheep:
                case BuildingUpgradeNames.Tannery_Cattle:
                    return new List<string> { GoodsNames.Leather };

                case BuildingUpgradeNames.Apiary: return new List<string> { GoodsNames.Wax };
                case BuildingUpgradeNames.SpiceShop: return new List<string> { GoodsNames.Spices };
                case BuildingUpgradeNames.CandleMaker: return new List<string> { GoodsNames.Candles };
                case BuildingUpgradeNames.DyeWorkshop: return new List<string> { GoodsNames.Dyes };
                case BuildingUpgradeNames.OilPress: return new List<string> { GoodsNames.Oils };
                case BuildingUpgradeNames.FlaxWeaver: return new List<string> { GoodsNames.Linen };
                case BuildingUpgradeNames.CanvasMaker: return new List<string> { GoodsNames.Canvas };
                case BuildingUpgradeNames.Ropewalk: return new List<string> { GoodsNames.Ropes };
                case BuildingUpgradeNames.HerbalistShacks: return new List<string> { GoodsNames.Herbs };

                case BuildingUpgradeNames.Winery: return new List<string> { GoodsNames.Wine };
                case BuildingUpgradeNames.Distillery: return new List<string> { GoodsNames.Spirits };
                case BuildingUpgradeNames.SunDryingGrapes: return new List<string> { GoodsNames.Raisins };

                case BuildingUpgradeNames.WoolWeaver: return new List<string> { GoodsNames.Wool };
                case BuildingUpgradeNames.ParchmentMaker: return new List<string> { GoodsNames.Parchment };

                case BuildingUpgradeNames.CattleMarket: return new List<string> { GoodsNames.DraftOxen };
                case BuildingUpgradeNames.DairyShop: return new List<string> { GoodsNames.FineCheese };
                case BuildingUpgradeNames.SausageMaker: return new List<string> { GoodsNames.Sausages };

                case BuildingUpgradeNames.HorseMarket: return new List<string> { GoodsNames.TrainedHorses };
                case BuildingUpgradeNames.WarhorseBreed: return new List<string> { GoodsNames.WarHorses };

                // Upgrades - Industry
                case BuildingUpgradeNames.Sawmill: return new List<string> { GoodsNames.Timber };
                case BuildingUpgradeNames.TarPit: return new List<string> { GoodsNames.Tar };
                case BuildingUpgradeNames.ColliersKiln: return new List<string> { GoodsNames.Charcoal };
                case BuildingUpgradeNames.CoopersShop: return new List<string> { GoodsNames.Barrels };
                case BuildingUpgradeNames.Glassworks: return new List<string> { GoodsNames.Glass };
                case BuildingUpgradeNames.MasterJoiner: return new List<string> { GoodsNames.Furniture };

                case BuildingUpgradeNames.StoneCutters: return new List<string> { GoodsNames.Marble };
                case BuildingUpgradeNames.SculpturesShop: return new List<string> { GoodsNames.Sculptures };
                case BuildingUpgradeNames.MasonsGuild: return new List<string> { GoodsNames.Masons };

                case BuildingUpgradeNames.BlastFurnace: return new List<string> { GoodsNames.Iron };

                // Upgrades - City/Misc
                case BuildingUpgradeNames.ShoemakersShop: return new List<string> { GoodsNames.FineBoots };
                case BuildingUpgradeNames.JewellersShop: return new List<string> { GoodsNames.Jewellery };
                case BuildingUpgradeNames.TailorsShop: return new List<string> { GoodsNames.FineClothing };
                case BuildingUpgradeNames.RugsWeaver: return new List<string> { GoodsNames.Carpets };

                case BuildingUpgradeNames.Alehouse: return new List<string> { GoodsNames.Ale };
                case BuildingUpgradeNames.Bakery: return new List<string> { GoodsNames.Pastries };

                case BuildingUpgradeNames.FishMarket: return new List<string> { GoodsNames.SaltedFish };
                case BuildingUpgradeNames.Cartographer: return new List<string> { GoodsNames.NavigationMaps };
                case BuildingUpgradeNames.SailMaker: return new List<string> { GoodsNames.Sails };

                case BuildingUpgradeNames.InkMaker:
                case BuildingUpgradeNames.InkMaker_Christian:
                case BuildingUpgradeNames.InkMaker_Muslim:
                case BuildingUpgradeNames.InkMaker_Pagan:
                    return new List<string> { GoodsNames.Ink };

                case BuildingUpgradeNames.Apothecary:
                case BuildingUpgradeNames.Apothecary_Christian:
                case BuildingUpgradeNames.Apothecary_Muslim:
                case BuildingUpgradeNames.Apothecary_Pagan:
                    return new List<string> { GoodsNames.Medicine };

                case BuildingUpgradeNames.AlchemistLab:
                case BuildingUpgradeNames.AlchemistLab_Christian:
                case BuildingUpgradeNames.AlchemistLab_Muslim:
                    return new List<string> { GoodsNames.Gunpowder };

                case BuildingUpgradeNames.Observatory:
                case BuildingUpgradeNames.Observatory_Christian:
                case BuildingUpgradeNames.Observatory_Muslim:
                    return new List<string> { GoodsNames.Compasses };

                case BuildingUpgradeNames.GreatLibrary:
                case BuildingUpgradeNames.GreatLibrary_Christian:
                case BuildingUpgradeNames.GreatLibrary_Muslim:
                    return new List<string> { GoodsNames.IllustratedBooks };

                case BuildingUpgradeNames.TranslatorsChambers:
                case BuildingUpgradeNames.TranslatorsChambers_Christian:
                case BuildingUpgradeNames.TranslatorsChambers_Muslim:
                    // produces Scholars - not in GoodsNames
                    return new List<string>();

                case BuildingUpgradeNames.ArtsSchool:
                case BuildingUpgradeNames.ArtsSchool_Christian:
                case BuildingUpgradeNames.ArtsSchool_Muslim:
                    return new List<string> { GoodsNames.FineArt }; // and Artists

                default:
                    return new List<string>();
            }
        }
    }
}
