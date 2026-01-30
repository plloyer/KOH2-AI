using System;
using AIOverhaul.Constants;
using AIOverhaul.Helpers;
using HarmonyLib;
using Logic;
using UnityEngine;

namespace AIOverhaul
{
    // "AddBuildOptions" generates the list of available buildings and upgrades for a castle.
    // Intent: Consolidated AddBuildOptionsPatch
    // For build option a HIGH eval value means high priority.
    [HarmonyPatch(typeof(Castle), "AddBuildOptions", typeof(bool), typeof(Resource))]
    public class Castle_AddBuildOptions
    {
        static void Postfix(Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

            var kingdom = __instance.GetKingdom();

            //AIOverhaulPlugin.LogDebug($"AddBuildOptions Called", LogCategory.Spending, kingdom);

            ApplySwordsmithLogic(__instance);
            ApplyFletcherLogic(__instance);
            ApplyReligiousSettlementConstraint(__instance);
            // ApplyBarracksLogic(__instance);
            // ApplyReligionLogic(__instance);
        }

        static void ApplyReligiousSettlementConstraint(Castle castle)
        {
            var realm = castle.GetRealm();
            // Check if Province has Monastery/Mosque/Shrine
            if (!DistrictHelper.HasReligiousSettlement(realm))
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

        static void ApplySwordsmithLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return;

            if (!BuildingHelper.HasBuildingUpgrade(kingdom, BuildingUpgradeNames.Swordsmith))
                MultiplyUpgradeOption(BuildingUpgradeNames.Swordsmith, GameBalance.SwordsmithPriorityMultiplier);
        }

        static void ApplyFletcherLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return;

            bool hasSwordsmith = BuildingHelper.HasBuildingUpgrade(kingdom, BuildingUpgradeNames.Swordsmith);
            bool hasFletcher = BuildingHelper.HasBuildingUpgrade(kingdom, BuildingUpgradeNames.Fletcher_Barracks);

            if (hasSwordsmith && !hasFletcher)
                MultiplyUpgradeOption(BuildingUpgradeNames.Fletcher, GameBalance.FletcherPriorityMultiplier);
        }

        static void ApplyBarracksLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return;

            AIOverhaulPlugin.LogDebug($"ApplyBarracksLogic", LogCategory.Spending, kingdom);

            // Check if kingdom already has any barracks (across all provinces)
            bool kingdomHasBarracks = BuildingHelper.HasBuilding(kingdom, BuildingNames.Barracks);
            if (kingdomHasBarracks)
            {
                AIOverhaulPlugin.LogDebug($"SKIP KingdomHasBarrack", LogCategory.Spending, kingdom);
                return;
            }

            // Get Castle district definition (Barracks goes in Castle district)
            District.Def castleDistrict = DistrictHelper.GetDistrictDefinition(castle.game, DistrictNames.Castle);
            if (castleDistrict == null) return;

            // Find the castle with the most Castle district slots across ALL kingdom realms
            Castle bestCastle = null;
            int maxSlots = -1;
            bool anyCastleHasCastleDistrict = false;

            if (kingdom.realms != null)
            {
                foreach (var realm in kingdom.realms)
                {
                    var realmCastle = realm?.castle;
                    if (realmCastle == null) continue;

                    if (realmCastle.HasDistrict(castleDistrict))
                    {
                        anyCastleHasCastleDistrict = true;
                        int slots = castleDistrict.buildings?.Count ?? 0;

                        if (slots > maxSlots)
                        {
                            maxSlots = slots;
                            bestCastle = realmCastle;
                        }
                    }
                }
            }

            // Determine if THIS castle should get the priority boost
            bool isBestCastle = (bestCastle == castle) || (!anyCastleHasCastleDistrict);

            AIOverhaulPlugin.LogDebug($"Best castle for Barracks: {bestCastle?.name ?? "None"} (Slots: {maxSlots}), Current: {castle.name}, IsBest: {isBestCastle}", LogCategory.Spending, kingdom);

            // Find Barracks in build options
            for (int i = Castle.build_options.Count - 1; i >= 0; i--)
            {
                var option = Castle.build_options[i];
                if (option.def == null || option.def.id != BuildingNames.Barracks) continue;

                if (isBestCastle)
                {
                    // This is the best castle (or no castle has Castle district)
                    // Set very high eval for weighted random selection (higher eval = more likely to be selected)
                    // Typical eval values are 100-3000, so 100000 ensures barracks is virtually always chosen
                    option.eval = 100000f;
                    option.priority = Logic.KingdomAI.Expense.Priority.Urgent;
                    AIOverhaulPlugin.LogDebug($"BOOSTING Barracks eval to {option.eval} in {castle.name} (Best castle with {maxSlots} slots)", LogCategory.Spending, kingdom);
                }
                else
                {
                    // Not the best castle - deprioritize (set very low eval)
                    option.eval = 1f;
                    AIOverhaulPlugin.LogDebug($"DEPRIORITIZING Barracks in {castle.name} (Not best castle)", LogCategory.Spending, kingdom);
                }

                Castle.build_options[i] = option;
            }
        }

        static void ApplyReligionLogic(Castle castle)
        {
            // Get Religion district definition
            District.Def religionDistrict = DistrictHelper.GetDistrictDefinition(castle.game, DistrictNames.Religion);
            if (religionDistrict == null) return;

            // Check if this castle has the Religion district
            bool hasReligionDistrict = castle.HasDistrict(religionDistrict);

            // Find all religious buildings in build options
            for (int i = Castle.build_options.Count - 1; i >= 0; i--)
            {
                var option = Castle.build_options[i];
                if (option.def == null) continue;

                bool isReligiousBuilding = BuildingHelper.IsReligiousBuilding(option.def.id);

                if (isReligiousBuilding)
                {
                    if (!hasReligionDistrict)
                    {
                        // Block building if no religion district
                        Castle.build_options.RemoveAt(i);
                    }
                    else
                    {
                        // Boost priority for castles with religion district (Higher eval = Higher priority)
                        // Further boost based on how many religion slots available
                        int religionSlots = BuildingHelper.CountReligionSlots(castle, religionDistrict);
                        float multiplier = 1.0f + (religionSlots * GameBalance.ReligionBuildingBoostPerSlot);
                        option.eval *= multiplier;
                        Castle.build_options[i] = option;
                    }
                }
            }

            // Do the same for upgrade options
            for (int i = Castle.upgrade_options.Count - 1; i >= 0; i--)
            {
                var option = Castle.upgrade_options[i];
                if (option.def == null) continue;

                bool isReligiousBuilding = BuildingHelper.IsReligiousBuilding(option.def.id);

                if (isReligiousBuilding)
                {
                    if (!hasReligionDistrict)
                    {
                        // Block building if no religion district
                        Castle.upgrade_options.RemoveAt(i);
                    }
                    else
                    {
                        // Boost priority for castles with religion district (Higher eval = Higher priority)
                        int religionSlots = BuildingHelper.CountReligionSlots(castle, religionDistrict);
                        float multiplier = 1.0f + (religionSlots * GameBalance.ReligionBuildingBoostPerSlot);
                        option.eval *= multiplier;
                        Castle.upgrade_options[i] = option;
                    }
                }
            }
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
    }
}
