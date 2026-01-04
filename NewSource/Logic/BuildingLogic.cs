using System;
using AIOverhaul.Constants;
using AIOverhaul.Helpers;
using HarmonyLib;
using Logic;
using UnityEngine;

namespace AIOverhaul
{
    // "EvalBuild" evaluates the priority/desirability of constructing a specific building definition.
    // Intent: BuildingPrioritizationPatch
    [HarmonyPatch(typeof(Castle), "EvalBuild")]
    public class Castle_EvalBuild
    {
        static void Postfix(Castle __instance, Logic.Building.Def def, Resource production_weights, ref float __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;
        }
    }
    
    // "AddBuildOptions" generates the list of available buildings and upgrades for a castle.
    // Intent: Consolidated AddBuildOptionsPatch
    // Merges logic for:
    // 1. Swordsmith Priority (Military)
    // 2. Barracks Placement (Military)
    // 3. Religion Building Logic (Economy)
    [HarmonyPatch(typeof(Castle), "AddBuildOptions", typeof(bool), typeof(Resource))]
    public class Castle_AddBuildOptions
    {
        static void Postfix(Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

            var kingdom = __instance.GetKingdom();

            // Block all construction if less than 2 merchants (Early Economy Setup)
            if (KingdomHelper.CountMerchants(kingdom) < 2)
            {
                Castle.build_options.Clear();
                Castle.upgrade_options.Clear();
                return;
            }

            // Block all construction if rushing tradition (save gold for Writing/Learning)
            if (TraditionHelper.ShouldRushTradition(kingdom))
            {
                Castle.build_options.Clear();
                Castle.upgrade_options.Clear();
                return;
            }

            ApplySwordsmithLogic(__instance);
            ApplyBarracksLogic(__instance);
            ApplyReligionLogic(__instance);
        }

        // --- Logic Blocks ---

        static void ApplySwordsmithLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return;

            bool hasSwordsmith = BuildingHelper.HasBuildingUpgrade(kingdom, BuildingUpgradeNames.Swordsmith);
            bool hasFletcher = BuildingHelper.HasBuildingUpgrade(kingdom, BuildingUpgradeNames.Fletcher_Barracks);

            // Boost Swordsmith evaluation, reduce Fletcher evaluation
            for (int i = 0; i < Castle.upgrade_options.Count; i++)
            {
                var option = Castle.upgrade_options[i];
                if (option.def == null) continue;

                if (option.def.id == BuildingUpgradeNames.Swordsmith)
                {
                    if (!hasSwordsmith)
                    {
                        // Very high priority for first Swordsmith in kingdom
                        option.eval *= GameBalance.SwordsmithBoost;
                        Castle.upgrade_options[i] = option;
                    }
                }
                else if (option.def.id == BuildingUpgradeNames.Fletcher_Barracks)
                {
                    if (hasSwordsmith && !hasFletcher)
                    {
                        // Very high priority for Fletcher after Swordsmith
                        option.eval *= GameBalance.FletcherBoost;
                        Castle.upgrade_options[i] = option;
                    }
                    else if (!hasSwordsmith)
                    {
                        // Block Fletcher until Swordsmith is built
                        option.eval *= GameBalance.StrongPenaltyMultiplier;
                        Castle.upgrade_options[i] = option;
                    }
                }
            }
        }

        static void ApplyBarracksLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return;

            // Get Castle district definition (Barracks goes in Castle district)
            District.Def castleDistrict = DistrictHelper.GetDistrict(castle.game, DistrictNames.Castle);
            if (castleDistrict == null) return;

            // Check if this castle HAS the Castle district
            bool hasCastleDistrict = castle.HasDistrict(castleDistrict);

            // Check if kingdom already has any barracks (across all provinces)
            bool kingdomHasBarracks = BuildingHelper.HasBuilding(kingdom, BuildingNames.Barracks);

            // Find Barracks in build options
            for (int i = Castle.build_options.Count - 1; i >= 0; i--)
            {
                var option = Castle.build_options[i];
                if (option.def == null || option.def.id != BuildingNames.Barracks) continue;

                if (!kingdomHasBarracks)
                {
                    // First barracks in kingdom - high priority, extra boost for Castle districts
                    float boost = GameBalance.BarracksBoost;

                    if (hasCastleDistrict)
                    {
                        // Additional boost based on Castle district slots
                        int slots = castleDistrict.buildings?.Count ?? 0;
                        boost *= (1.0f + (slots * GameBalance.BarracksSlotBoostPerSlot));
                        AIOverhaulPlugin.LogDiagnostic($"BOOSTING first Barracks in {castle.name} (Base: {GameBalance.BarracksBoost}, Slots: {slots})", LogCategory.Military, kingdom);
                    }

                    option.eval *= boost;
                    Castle.build_options[i] = option;
                }
                else
                {
                    // Kingdom already has barracks - ONLY allow in Castle districts OR IronOre provinces
                    bool hasIronOre = FeatureHelper.HasFeature(castle.GetRealm(), FeatureNames.IronOre);

                    if (!hasCastleDistrict && !hasIronOre)
                    {
                        Castle.build_options.RemoveAt(i);
                    }
                }
            }
        }

        static void ApplyReligionLogic(Castle castle)
        {
            // Get Religion district definition
            District.Def religionDistrict = DistrictHelper.GetDistrict(castle.game, DistrictNames.Religion);
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
                        // Boost priority for castles with religion district
                        // Further boost based on how many religion slots available
                        int religionSlots = BuildingHelper.CountReligionSlots(castle, religionDistrict);
                        float boost = 1.0f + (religionSlots * GameBalance.ReligionBuildingBoostPerSlot);
                        option.eval *= boost;
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
                        // Boost priority for castles with religion district
                        int religionSlots = BuildingHelper.CountReligionSlots(castle, religionDistrict);
                        float boost = 1.0f + (religionSlots * GameBalance.ReligionBuildingBoostPerSlot);
                        option.eval *= boost;
                        Castle.upgrade_options[i] = option;
                    }
                }
            }
        }
    }
}
