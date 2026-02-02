using System;
using System.Collections.Generic;

namespace AIOverhaul
{
    /// <summary>
    /// Helper for mapping Province Features to Buildings
    /// </summary>
    public static class FeatureHelper
    {
        static readonly Dictionary<string, string> s_FeatureToBuildingMap = new Dictionary<string, string>
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

        static readonly Dictionary<string, string> s_BuildingToFeatureMap = new Dictionary<string, string>();

        static FeatureHelper()
        {
            // Reverse map
            foreach (var kvp in s_FeatureToBuildingMap)
            {
                if (!s_BuildingToFeatureMap.ContainsKey(kvp.Value))
                {
                    s_BuildingToFeatureMap[kvp.Value] = kvp.Key;
                }
            }
            
            // Manual overrides or additions for reverse map if needed
            // e.g. Coastal buildings
            s_BuildingToFeatureMap[BuildingNames.Shipyard] = FeatureNames.Coastal;
            s_BuildingToFeatureMap[BuildingNames.Admiralty] = FeatureNames.Coastal;
            s_BuildingToFeatureMap[BuildingNames.TradePort] = FeatureNames.Coastal;
        }

        /// <summary>
        /// Get the primary building enabled by a province feature.
        /// </summary>
        public static string GetRelatedBuilding(string featureName)
        {
            return s_FeatureToBuildingMap.TryGetValue(featureName, out var building) ? building : null;
        }

        /// <summary>
        /// Get the province feature required for a building.
        /// </summary>
        public static string GetRequiredFeature(string buildingName)
        {
             return s_BuildingToFeatureMap.TryGetValue(buildingName, out var feature) ? feature : null;
        }

        /// <summary>
        /// Check if a realm has a specific feature.
        /// </summary>
        public static bool HasFeature(Logic.Realm realm, string featureName)
        {
            return realm?.features?.Contains(featureName) ?? false;
        }
    }
}
