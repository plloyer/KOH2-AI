using System.Collections.Generic;
using AIOverhaul.Constants;

namespace AIOverhaul.Helpers
{
    /// <summary>
    /// Helper for mapping Province Features to Buildings
    /// </summary>
    public static class FeatureHelper
    {
        private static readonly Dictionary<string, string> FeatureToBuildingMap = new Dictionary<string, string>
        {
            // Animals
            { FeatureNames.Cattle, BuildingNames.CattleFarming },
            { FeatureNames.Sheep, BuildingNames.SheepFarming },
            { FeatureNames.Horses, BuildingNames.HorseBreeding },
            { FeatureNames.Camels, BuildingNames.CamelsTrade },
            { FeatureNames.RareGame, BuildingNames.FurTrade }, // RareGame -> FursTrade

            // Flora/Terrain
            { FeatureNames.DeepForests, BuildingNames.Woodworking },
            { FeatureNames.FlaxFields, BuildingNames.FlaxGrowing },
            { FeatureNames.Herbage, BuildingNames.HerbGardening },
            { FeatureNames.Vines, BuildingNames.Viticulture },
            { FeatureNames.Rivers, BuildingNames.RiverTrade },

            // Resources
            { FeatureNames.IronOre, BuildingNames.Metalworking },
            { FeatureNames.GoldOre, BuildingNames.GoldSmelting },
            { FeatureNames.SilverOre, BuildingNames.SilverSmelting },
            { FeatureNames.MineralsDeposit, BuildingNames.MineralsTrade },
            { FeatureNames.MarbleDeposit, BuildingNames.Stoneworking },
            { FeatureNames.LimestoneDeposit, BuildingNames.LimeTrade },
            { FeatureNames.LodestoneDeposits, BuildingNames.LodestoneTrade },
            { FeatureNames.SulfurDeposits, BuildingNames.SulfurTrade },
            { FeatureNames.SaltpeterDeposits, BuildingNames.SaltpeterTrade },
            { FeatureNames.SaltDeposit, BuildingNames.SaltTrade },
            { FeatureNames.AmberDeposits, BuildingNames.AmberTrade },
            
            // Note: Coastal maps to multiple, but typically Harbor is the primary one
            { FeatureNames.Coastal, BuildingNames.Harbor }
        };

        private static readonly Dictionary<string, string> BuildingToFeatureMap = new Dictionary<string, string>();

        static FeatureHelper()
        {
            // Reverse map
            foreach (var kvp in FeatureToBuildingMap)
            {
                if (!BuildingToFeatureMap.ContainsKey(kvp.Value))
                {
                    BuildingToFeatureMap[kvp.Value] = kvp.Key;
                }
            }
            
            // Manual overrides or additions for reverse map if needed
            // e.g. Coastal buildings
            BuildingToFeatureMap[BuildingNames.Shipyard] = FeatureNames.Coastal;
            BuildingToFeatureMap[BuildingNames.Admiralty] = FeatureNames.Coastal;
            BuildingToFeatureMap[BuildingNames.TradePort] = FeatureNames.Coastal;
        }

        /// <summary>
        /// Get the primary building enabled by a province feature.
        /// </summary>
        public static string GetRelatedBuilding(string featureName)
        {
            return FeatureToBuildingMap.TryGetValue(featureName, out var building) ? building : null;
        }

        /// <summary>
        /// Get the province feature required for a building.
        /// </summary>
        public static string GetRequiredFeature(string buildingName)
        {
             return BuildingToFeatureMap.TryGetValue(buildingName, out var feature) ? feature : null;
        }
    }
}
