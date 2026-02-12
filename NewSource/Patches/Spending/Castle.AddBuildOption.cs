using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "AddBuildOptions" generates the list of available buildings and upgrades for a castle.
    // For build option a HIGH eval value means high priority.
    [HarmonyPatch(typeof(Castle), "AddBuildOptions", typeof(bool), typeof(Resource))]
    public class Castle_AddBuildOptions
    {
        const float k_HighPriorityEval = 100000f;
        const string k_LogPrefix = "[AddBuildOptions]";

        static void Postfix(Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

            if (ApplyVillageMilitiaLogic(__instance)) return;
            if (ApplyBarracksLogic(__instance)) return;
            if (ApplySwordsmithLogic(__instance)) return;
            if (ApplyFletcherLogic(__instance)) return;
            ApplyFoodSecurityLogic(__instance);
            ApplyCropFarmingConstraint(__instance);
            ApplyReligiousSettlementConstraint(__instance);
            ApplyVillageMilitiaTrainingGroundLogic(__instance);
        }

        static void ApplyCropFarmingConstraint(Castle castle)
        {
            var realm = castle.GetRealm();
            if (realm.GetFarmCount() > 0) return;

            float food = KingdomHelper.GetFood(castle.GetKingdom());
            if (food <= 0) return;

            for (int i = Castle.build_options.Count - 1; i >= 0; i--)
            {
                var option = Castle.build_options[i];
                if (option.def != null && option.def.id == BuildingNames.CropFarming)
                    Castle.build_options.RemoveAt(i);
            }
        }

        static void ApplyReligiousSettlementConstraint(Castle castle)
        {
            var realm = castle.GetRealm();
            int religiousCount = realm.GetReligiousCount();
            if (religiousCount == 0)
            {
                // If not, remove Religious Buildings from options
                for (int i = Castle.build_options.Count - 1; i >= 0; i--)
                {
                    var option = Castle.build_options[i];
                    if (option.def != null && BuildingHelper.IsReligiousBuilding(option.def.id))
                    {
                        Castle.build_options.RemoveAt(i);
                    }
                }
            }
            else
            {
                float multiplier = 1 + (religiousCount - 1) * GameBalance.ReligionBuildingBoostPerSlot;

                for (int i = 0; i < Castle.build_options.Count; i++)
                {
                    var option = Castle.build_options[i];
                    if (option.def != null && BuildingHelper.IsReligiousBuilding(option.def.id))
                    {
                        option.eval *= multiplier;
                        Castle.build_options[i] = option;
                    }
                }
            }
        }

        static void ApplyFoodSecurityLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();

            float food = KingdomHelper.GetFood(kingdom);

            if (food <= 0)
            {
                var realm = castle.GetRealm();
                int farmCount = realm.GetFarmCount();
                int coastalCount = realm.GetCoastalCount();

                bool hasRareGame = realm.features != null && realm.features.Contains(FeatureNames.RareGame);
                bool hasRivers = realm.features != null && realm.features.Contains(FeatureNames.Rivers);
                bool hasVines = realm.features != null && realm.features.Contains(FeatureNames.Vines);

                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Food CRITICAL ({food}), boosting food production in {castle.name}.", LogCategory.Spending, kingdom);

                float farmEval = k_HighPriorityEval * (0.5f + farmCount);
                float coastalEval = k_HighPriorityEval * (0.5f + coastalCount);
                float sheepEval = k_HighPriorityEval * 5;
                float cattleEval = k_HighPriorityEval * 5;
                float irrigationEval = k_HighPriorityEval * (1 + farmCount * 3);
                float furTradeEval = k_HighPriorityEval * 3;
                float viticultureEval = k_HighPriorityEval * 3;

                // Buildings
                EnsureBuildOption(castle, BuildingNames.CropFarming, farmEval, KingdomAI.Expense.Priority.Urgent);
                EnsureBuildOption(castle, BuildingNames.Harbor, coastalEval, KingdomAI.Expense.Priority.Urgent);
                EnsureBuildOption(castle, BuildingNames.SheepFarming, sheepEval, KingdomAI.Expense.Priority.Urgent);
                EnsureBuildOption(castle, BuildingNames.CattleFarming, cattleEval, KingdomAI.Expense.Priority.Urgent);
                if (hasRivers)
                    EnsureBuildOption(castle, BuildingNames.Irrigation, irrigationEval, KingdomAI.Expense.Priority.Urgent);
                if (hasRareGame)
                    EnsureBuildOption(castle, BuildingNames.FurTrade, furTradeEval, KingdomAI.Expense.Priority.Urgent);
                if (hasVines)
                    EnsureBuildOption(castle, BuildingNames.Viticulture, viticultureEval, KingdomAI.Expense.Priority.Urgent);

                // Upgrades (inject if vanilla didn't generate them)
                EnsureUpgradeOption(kingdom, castle, BuildingUpgradeNames.CropsRotation, BuildingNames.CropFarming, farmEval, KingdomAI.Expense.Priority.Urgent);
                EnsureUpgradeOption(kingdom, castle, BuildingUpgradeNames.Docks_Harbor, BuildingNames.Harbor, coastalEval, KingdomAI.Expense.Priority.Urgent);
                EnsureUpgradeOption(kingdom, castle, BuildingUpgradeNames.Butcher_Sheep, BuildingNames.SheepFarming, sheepEval, KingdomAI.Expense.Priority.Urgent);
                EnsureUpgradeOption(kingdom, castle, BuildingUpgradeNames.Butcher_Cattle, BuildingNames.CattleFarming, cattleEval, KingdomAI.Expense.Priority.Urgent);
                if (hasVines)
                    EnsureUpgradeOption(kingdom, castle, BuildingUpgradeNames.SunDryingGrapes, BuildingNames.Viticulture, viticultureEval, KingdomAI.Expense.Priority.Urgent);
            }
        }
        
        static bool ApplyVillageMilitiaLogic(Castle castle)
        {
            var realm = castle.GetRealm();
            if (realm == null) return false;

            int villageCount = realm.GetVillageCount();
            if (villageCount < GameBalance.MinVillagesForMilitia) return false;

            if (EnsureBuildOption(castle, BuildingNames.VillageMilitia, k_HighPriorityEval, KingdomAI.Expense.Priority.Urgent))
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} BOOSTING VillageMilitia in {castle.name} ({villageCount} villages)", LogCategory.Spending, castle.GetKingdom());
                return true;
            }

            return false;
        }

        static void ApplyVillageMilitiaTrainingGroundLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return;
            
            // If we already have Village Militia, boost TrainingGrounds upgrade
            if (kingdom.HasBuilding(BuildingNames.VillageMilitia))
            {
                if (!kingdom.HasBuildingUpgrade(BuildingUpgradeNames.TrainingGrounds))
                    EnsureUpgradeOption(kingdom, castle, BuildingUpgradeNames.TrainingGrounds, BuildingNames.VillageMilitia, GameBalance.HighPriorityBuildingMultiplier, KingdomAI.Expense.Priority.Urgent);
                return;
            }
        }

        static bool ApplySwordsmithLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return false;

            if (!kingdom.HasBuildingUpgrade(BuildingUpgradeNames.Swordsmith))
            {
                MultiplyUpgradeOption(BuildingUpgradeNames.Swordsmith, GameBalance.HighPriorityBuildingMultiplier);
                return true;
            }

            return false;
        }

        static bool ApplyFletcherLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return false;

            bool hasSwordsmith = kingdom.HasBuildingUpgrade(BuildingUpgradeNames.Swordsmith);
            bool hasFletcher = kingdom.HasBuildingUpgrade(BuildingUpgradeNames.Fletcher_Barracks);

            if (hasSwordsmith && !hasFletcher)
            {
                MultiplyUpgradeOption(BuildingUpgradeNames.Fletcher, GameBalance.HighPriorityBuildingMultiplier);
                return true;
            }

            return false;
        }

        static bool ApplyBarracksLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return false;
            if (kingdom.GetBuildingCount(BuildingNames.Barracks) > 0) return false;

            var keep = castle.GetRealm().GetKeepCount();
            MultiplyBuildOption(BuildingNames.Barracks, 1 + (keep * GameBalance.BarracksSlotBoostPerSlot));
            return true;
        }

        static void MultiplyUpgradeOption(string upgradeName, float multiplier)
        {
            for (int i = 0; i < Castle.upgrade_options.Count; i++)
            {
                var option = Castle.upgrade_options[i];
                if (option.def?.id == upgradeName)
                {
                    option.eval *= multiplier;
                    Castle.upgrade_options[i] = option;
                }
            }
        }

        static void MultiplyBuildOption(string buildingName, float multiplier)
        {
            for (int i = 0; i < Castle.build_options.Count; i++)
            {
                var option = Castle.build_options[i];
                if (option.def?.id == buildingName)
                {
                    option.eval *= multiplier;
                    Castle.build_options[i] = option;
                }
            }
        }

        /// <summary>
        /// Ensures a building option exists in build_options with at least the given eval/priority.
        /// If missing and the building is available (not already built), injects it.
        /// </summary>
        static bool EnsureBuildOption(Castle castle, string buildingId, float eval, KingdomAI.Expense.Priority priority)
        {
            var def = castle.game.defs.Find<Logic.Building.Def>(buildingId);
            if (def == null) return false;
            if (castle.HasBuilding(def)) return false;

            // Try to boost existing option first
            for (int i = 0; i < Castle.build_options.Count; i++)
            {
                var opt = Castle.build_options[i];
                if (opt.def == def && opt.castle == castle)
                {
                    if (opt.eval < eval || opt.priority < priority)
                    {
                        Castle.build_options_sum += eval - opt.eval;
                        opt.eval = eval;
                        opt.priority = priority;
                        Castle.build_options[i] = opt;
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} BOOSTED {buildingId} in {castle.name}: eval={eval}", LogCategory.Spending, castle.GetKingdom());
                    }
                    return true;
                }
            }

            // Not in list — verify the province can actually build it before injecting
            if (castle.CanBuildBuilding(def, ignore_cost: true) != Castle.StructureBuildAvailability.Available)
                return false;

            var option = new Castle.BuildOption
            {
                castle = castle, def = def, eval = eval, priority = priority
            };
            Castle.build_options.Add(option);
            Castle.build_options_sum += eval;
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} INJECTED {buildingId} build option in {castle.name}: eval={eval}", LogCategory.Spending, castle.GetKingdom());
            return true;
        }

        /// <summary>
        /// Ensures an upgrade option exists in upgrade_options with at least the given eval/priority.
        /// If missing and parent building is built (and upgrade isn't), injects it.
        /// </summary>
        static bool EnsureUpgradeOption(Logic.Kingdom kingdom, Castle castle, string upgradeId, string parentBuildingId, float eval, KingdomAI.Expense.Priority priority)
        {
            var def = castle.game.defs.Find<Logic.Building.Def>(upgradeId);
            if (def == null) return false;
            if (!kingdom.CanBuildUpgrade(castle, upgradeId)) return false;

            // Check parent building exists
            var parentDef = castle.game.defs.Find<Logic.Building.Def>(parentBuildingId);
            if (parentDef == null || !castle.HasBuilding(parentDef)) return false;

            // Try to boost existing option first
            for (int i = 0; i < Castle.upgrade_options.Count; i++)
            {
                var opt = Castle.upgrade_options[i];
                if (opt.def == def)
                {
                    if (opt.eval < eval || opt.priority < priority)
                    {
                        Castle.upgrade_options_sum += eval - opt.eval;
                        opt.eval = eval;
                        opt.priority = priority;
                        Castle.upgrade_options[i] = opt;
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} BOOSTED {upgradeId} upgrade in {castle.name}: eval={eval}", LogCategory.Spending, castle.GetKingdom());
                    }
                    return true;
                }
            }

            var option = new Castle.BuildOption
            {
                castle = castle, def = def, eval = eval, priority = priority
            };
            Castle.upgrade_options.Add(option);
            Castle.upgrade_options_sum += eval;
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} INJECTED {upgradeId} upgrade in {castle.name}: eval={eval}", LogCategory.Spending, castle.GetKingdom());
            return true;
        }
    }
}
