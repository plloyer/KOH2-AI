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
        const int k_MinVillagesForMilitia = 2;
        const string k_LogPrefix = "[AddBuildOptions]";

        static void Postfix(Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;
            
            ApplySwordsmithLogic(__instance);
            ApplyFletcherLogic(__instance);
            ApplyReligiousSettlementConstraint(__instance);
            ApplyVillageMilitiaLogic(__instance);
            ApplyFoodSecurityLogic(__instance);
            ApplyBarracksLogic(__instance);
        }

        static void ApplyReligiousSettlementConstraint(Castle castle)
        {
            var realm = castle.GetRealm();
            // Check if Province has Monastery/Mosque/Shrine
            if (!realm.HasReligiousSettlement())
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
                // Has religious settlements — boost religious buildings based on count
                int religiousCount = realm.GetReligiousCount();
                float multiplier = 1 + religiousCount * GameBalance.ReligionBuildingBoostPerSlot;

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
            if (kingdom == null) return;

            float food = KingdomHelper.GetFood(kingdom);

            if (food <= 0)
            {
                var realm = castle.GetRealm();
                int farmCount = realm.GetFarmCount();
                int coastalCount = realm.GetCoastalCount();

                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Food CRITICAL ({food}), boosting food production in {castle.name}. Farms: {farmCount}, Coastal: {coastalCount}", LogCategory.Spending, kingdom);

                float farmEval = k_HighPriorityEval * (1 + farmCount) * 2;
                float coastalEval = k_HighPriorityEval * (1 + coastalCount);

                // Buildings
                EnsureBuildOption(castle, BuildingNames.CropFarming, farmEval, KingdomAI.Expense.Priority.Urgent);
                EnsureBuildOption(castle, BuildingNames.Harbor, coastalEval, KingdomAI.Expense.Priority.Urgent);

                // Upgrades (inject if vanilla didn't generate them)
                EnsureUpgradeOption(castle, BuildingUpgradeNames.CropsRotation, BuildingNames.CropFarming, farmEval, KingdomAI.Expense.Priority.Urgent);
                EnsureUpgradeOption(castle, BuildingUpgradeNames.Docks_Harbor, BuildingNames.Harbor, coastalEval, KingdomAI.Expense.Priority.Urgent);
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
                if (opt.def == def)
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

            // Not in list — inject it
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
        static bool EnsureUpgradeOption(Castle castle, string upgradeId, string parentBuildingId, float eval, KingdomAI.Expense.Priority priority)
        {
            var def = castle.game.defs.Find<Logic.Building.Def>(upgradeId);
            if (def == null) return false;
            if (castle.HasBuilding(def)) return false;

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

            // Not in list — inject it
            var option = new Castle.BuildOption
            {
                castle = castle, def = def, eval = eval, priority = priority
            };
            Castle.upgrade_options.Add(option);
            Castle.upgrade_options_sum += eval;
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} INJECTED {upgradeId} upgrade in {castle.name}: eval={eval}", LogCategory.Spending, castle.GetKingdom());
            return true;
        }

        static void ApplyVillageMilitiaLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return;

            // If we already have Village Militia, boost TrainingGrounds upgrade
            if (kingdom.HasBuilding(BuildingNames.VillageMilitia))
            {
                if (!kingdom.HasBuildingUpgrade(BuildingUpgradeNames.TrainingGrounds))
                {
                    MultiplyUpgradeOption(BuildingUpgradeNames.TrainingGrounds, GameBalance.HighPriorityBuildingMultiplier);
                }
                return;
            }

            // Find the best castle for Village Militia (Most villages, >= 2)
            Castle bestCastle = null;
            int maxVillages = -1;

            if (kingdom.realms != null)
            {
                foreach (var r in kingdom.realms)
                {
                    if (r == null || r.castle == null) continue;

                    int vCount = r.GetVillageCount();
                    if (vCount >= k_MinVillagesForMilitia)
                    {
                        if (vCount > maxVillages)
                        {
                            maxVillages = vCount;
                            bestCastle = r.castle;
                        }
                    }
                }
            }

            // If no suitable castle found, do nothing
            if (bestCastle == null) return;

            // If THIS is the best castle, boost the option
            if (castle == bestCastle)
            {
                for (int i = 0; i < Castle.build_options.Count; i++)
                {
                    var option = Castle.build_options[i];
                    if (option.castle == castle && option.def != null && option.def.id == BuildingNames.VillageMilitia)
                    {
                        option.eval = k_HighPriorityEval;
                        option.priority = KingdomAI.Expense.Priority.Urgent;
                        Castle.build_options[i] = option;

                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} BOOSTING VillageMilitia in {castle.name} (Best location with {maxVillages} villages)", LogCategory.Spending, kingdom);
                        return;
                    }
                }
            }
        }

        static void ApplySwordsmithLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return;

            if (!kingdom.HasBuildingUpgrade(BuildingUpgradeNames.Swordsmith))
                MultiplyUpgradeOption(BuildingUpgradeNames.Swordsmith, GameBalance.HighPriorityBuildingMultiplier);
        }

        static void ApplyFletcherLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return;

            bool hasSwordsmith = kingdom.HasBuildingUpgrade(BuildingUpgradeNames.Swordsmith);
            bool hasFletcher = kingdom.HasBuildingUpgrade(BuildingUpgradeNames.Fletcher_Barracks);

            if (hasSwordsmith && !hasFletcher)
                MultiplyUpgradeOption(BuildingUpgradeNames.Fletcher, GameBalance.HighPriorityBuildingMultiplier);
        }

        static void ApplyBarracksLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return;

            var keep = castle.GetRealm().GetKeepCount();
            MultiplyBuildOption(BuildingNames.Barracks, 1 + (keep * GameBalance.BarracksSlotBoostPerSlot));
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
    }
}
