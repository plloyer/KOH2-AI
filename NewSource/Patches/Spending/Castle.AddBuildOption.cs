using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "AddBuildOptions" generates the list of available buildings and upgrades for a castle.
    // Intent: Consolidated AddBuildOptionsPatch
    // For build option a HIGH eval value means high priority.
    [HarmonyPatch(typeof(Castle), "AddBuildOptions", typeof(bool), typeof(Resource))]
    public class Castle_AddBuildOptions
    {
        const float HighPriorityEval = 100000f;
        const int MinVillagesForMilitia = 2;

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
                        //AIOverhaulPlugin.LogDebug($"Removed {option.def.id} from {castle.name} (No Religious Settlement)", LogCategory.Spending, castle.GetKingdom());
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

                AIOverhaulPlugin.LogDebug($"Food CRITICAL ({food}), boosting food production in {castle.name}. Farms: {farmCount}, Coastal: {coastalCount}", LogCategory.Spending, kingdom);

                // Boost building options
                for (int i = 0; i < Castle.build_options.Count; i++)
                {
                    var option = Castle.build_options[i];
                    if (option.def == null) continue;

                    if (option.def.id == BuildingNames.CropFarming)
                    {
                        // Boost based on Farm count
                        option.eval = HighPriorityEval * (1 + farmCount) * 2; // Prioritize farm over harbor
                        option.priority = KingdomAI.Expense.Priority.Urgent;
                        Castle.build_options[i] = option;
                        AIOverhaulPlugin.LogDebug($"BOOSTING CropFarming in {castle.name}: eval={option.eval}", LogCategory.Spending, kingdom);
                    }
                    else if (option.def.id == BuildingNames.Harbor)
                    {
                        // Boost based on Coastal count
                        option.eval = HighPriorityEval * (1 + coastalCount);
                        option.priority = KingdomAI.Expense.Priority.Urgent;
                        Castle.build_options[i] = option;
                        AIOverhaulPlugin.LogDebug($"BOOSTING Harbor in {castle.name}: eval={option.eval}", LogCategory.Spending, kingdom);
                    }
                }

                // Boost upgrade options (CropsRotation for farms, Docks for harbors)
                for (int i = 0; i < Castle.upgrade_options.Count; i++)
                {
                    var option = Castle.upgrade_options[i];
                    if (option.def == null) continue;

                    if (option.def.id == BuildingUpgradeNames.CropsRotation)
                    {
                        option.eval = HighPriorityEval * (1 + farmCount) * 2;
                        option.priority = KingdomAI.Expense.Priority.Urgent;
                        Castle.upgrade_options[i] = option;
                        AIOverhaulPlugin.LogDebug($"BOOSTING CropsRotation upgrade in {castle.name}: eval={option.eval}", LogCategory.Spending, kingdom);
                    }
                    else if (option.def.id == BuildingUpgradeNames.Docks_Harbor)
                    {
                        option.eval = HighPriorityEval * (1 + coastalCount);
                        option.priority = KingdomAI.Expense.Priority.Urgent;
                        Castle.upgrade_options[i] = option;
                        AIOverhaulPlugin.LogDebug($"BOOSTING Docks upgrade in {castle.name}: eval={option.eval}", LogCategory.Spending, kingdom);
                    }
                }
            }
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

                    int vCount = DistrictHelper.GetVillageCount(r);
                    if (vCount >= MinVillagesForMilitia)
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
                    if (option.def != null && option.def.id == BuildingNames.VillageMilitia)
                    {
                        option.eval = HighPriorityEval;
                        option.priority = KingdomAI.Expense.Priority.Urgent;
                        Castle.build_options[i] = option;

                        AIOverhaulPlugin.LogDebug($"BOOSTING VillageMilitia in {castle.name} (Best location with {maxVillages} villages)", LogCategory.Spending, kingdom);
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
