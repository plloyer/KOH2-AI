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
    // Priority order:
    // 1. First Barracks (required for Swordsmith/Fletcher upgrades)
    // 2. Swordsmith upgrade (better melee units, required before Fletcher)
    // 3. Fletcher upgrade (better ranged units, requires Swordsmith first)
    // 4. Religion buildings (only in provinces with Religion district)
    [HarmonyPatch(typeof(Castle), "AddBuildOptions", typeof(bool), typeof(Resource))]
    public class Castle_AddBuildOptions
    {
        static void Postfix(Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

            var kingdom = __instance.GetKingdom();

            // Block non-essential construction if less than 2 merchants (Early Economy Setup)
            if (KingdomHelper.CountMerchants(kingdom) < 2)
            {
                // ALLOW essential foundation buildings even without merchants
                for (int i = Castle.build_options.Count - 1; i >= 0; i--)
                {
                    var opt = Castle.build_options[i];
                    if (opt.def == null) continue;

                    bool isEssential = opt.def.id == BuildingNames.Barracks ||
                                     opt.def.id == BuildingNames.MarketSquare ||
                                     opt.def.id == BuildingNames.Housings;

                    if (!isEssential)
                    {
                        Castle.build_options.RemoveAt(i);
                    }
                }

                // Apply Barracks priority boost BEFORE early return
                ApplyBarracksLogic(__instance);

                // Block all upgrades until we have merchants (Keep it simple)
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
            ApplyFletcherLogic(__instance);
            ApplyBarracksLogic(__instance);
            ApplyReligionLogic(__instance);
        }

        // --- Logic Blocks ---

        static void ApplySwordsmithLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return;

            bool hasSwordsmith = BuildingHelper.HasBuildingUpgrade(kingdom, BuildingUpgradeNames.Swordsmith);

            // Boost Swordsmith evaluation
            for (int i = 0; i < Castle.upgrade_options.Count; i++)
            {
                var option = Castle.upgrade_options[i];
                if (option.def == null) continue;

                if (option.def.id == BuildingUpgradeNames.Swordsmith)
                {
                    if (!hasSwordsmith)
                    {
                        // Very high priority for first Swordsmith in kingdom (lower eval = higher priority)
                        option.eval *= GameBalance.SwordsmithPriorityMultiplier;
                        Castle.upgrade_options[i] = option;
                    }
                }
            }
        }

        static void ApplyFletcherLogic(Castle castle)
        {
            var kingdom = castle.GetKingdom();
            if (kingdom == null) return;

            bool hasSwordsmith = BuildingHelper.HasBuildingUpgrade(kingdom, BuildingUpgradeNames.Swordsmith);
            bool hasFletcher = BuildingHelper.HasBuildingUpgrade(kingdom, BuildingUpgradeNames.Fletcher_Barracks);

            // After Swordsmith is built, Fletcher becomes critical priority
            if (hasSwordsmith && !hasFletcher)
            {
                // 1. Boost Fletcher upgrade if it appears in options
                bool fletcherOptionFound = false;
                for (int i = 0; i < Castle.upgrade_options.Count; i++)
                {
                    var option = Castle.upgrade_options[i];
                    if (option.def == null) continue;

                    if (option.def.id == BuildingUpgradeNames.Fletcher_Barracks)
                    {
                        // VERY high priority for Fletcher after Swordsmith (lower eval = higher priority)
                        option.eval *= GameBalance.FletcherPriorityMultiplier;
                        Castle.upgrade_options[i] = option;
                        fletcherOptionFound = true;
                        AIOverhaulPlugin.LogDebug($"BOOSTING Fletcher upgrade in {castle.name} (eval: {option.eval:F2})", LogCategory.Military, kingdom);
                    }
                }

                // 2. If Fletcher not found in options, forcibly inject it
                if (!fletcherOptionFound)
                {
                    // Find Barracks in this castle
                    Logic.Building barracks = null;
                    if (castle.buildings != null)
                    {
                        foreach (var building in castle.buildings)
                        {
                            if (building?.def?.id == BuildingNames.Barracks)
                            {
                                barracks = building;
                                break;
                            }
                        }
                    }

                    // If we have a Barracks, find Fletcher in its upgrades and inject it
                    if (barracks != null && barracks.def.upgrades?.buildings != null)
                    {
                        Logic.Building.Def fletcherDef = null;
                        foreach (var upgradeInfo in barracks.def.upgrades.buildings)
                        {
                            if (upgradeInfo.def?.id == BuildingUpgradeNames.Fletcher_Barracks)
                            {
                                fletcherDef = upgradeInfo.def;
                                break;
                            }
                        }

                        // Inject Fletcher into upgrade_options with VERY high priority
                        if (fletcherDef != null)
                        {
                            var fletcherOption = new Castle.BuildOption
                            {
                                castle = castle,
                                def = fletcherDef,
                                eval = 1000f * GameBalance.FletcherPriorityMultiplier, // Base eval * multiplier (lower = higher priority)
                                priority = Logic.KingdomAI.Expense.Priority.Urgent
                            };

                            Castle.upgrade_options.Add(fletcherOption);
                            Castle.upgrade_options_sum += fletcherOption.eval;

                            AIOverhaulPlugin.LogDebug($"INJECTED Fletcher upgrade into {castle.name} (eval: {fletcherOption.eval:F2})", LogCategory.Military, kingdom);
                        }
                    }
                }
            }
            else if (!hasSwordsmith)
            {
                // Block Fletcher until Swordsmith is built
                for (int i = 0; i < Castle.upgrade_options.Count; i++)
                {
                    var option = Castle.upgrade_options[i];
                    if (option.def == null) continue;

                    if (option.def.id == BuildingUpgradeNames.Fletcher_Barracks)
                    {
                        // Block Fletcher until Swordsmith (higher eval = lower priority)
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
                    // First barracks in kingdom - high priority, extra multiplier for Castle districts (lower eval = higher priority)
                    float multiplier = GameBalance.BarracksPriorityMultiplier;

                    if (hasCastleDistrict)
                    {
                        // Additional boost based on Castle district slots (lower multiplier = even lower eval)
                        int slots = castleDistrict.buildings?.Count ?? 0;
                        multiplier /= (1.0f + (slots * GameBalance.BarracksSlotBoostPerSlot));
                        AIOverhaulPlugin.LogDebug($"BOOSTING first Barracks in {castle.name} (Multiplier: {multiplier:F4}, Slots: {slots})", LogCategory.Military, kingdom);
                    }

                    option.eval *= multiplier;

                    // Force URGENT priority for first barracks to bypass budget checks
                    option.priority = Logic.KingdomAI.Expense.Priority.Urgent;

                    // Cap eval at 1.0 (affordable in 1 turn or less) to ensure it's picked quickly
                    if (option.eval > 1.0f) option.eval = 1.0f;

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
                        // Boost priority for castles with religion district (lower eval = higher priority)
                        // Further boost based on how many religion slots available
                        int religionSlots = BuildingHelper.CountReligionSlots(castle, religionDistrict);
                        float divisor = 1.0f + (religionSlots * GameBalance.ReligionBuildingBoostPerSlot);
                        option.eval /= divisor;
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
                        // Boost priority for castles with religion district (lower eval = higher priority)
                        int religionSlots = BuildingHelper.CountReligionSlots(castle, religionDistrict);
                        float divisor = 1.0f + (religionSlots * GameBalance.ReligionBuildingBoostPerSlot);
                        option.eval /= divisor;
                        Castle.upgrade_options[i] = option;
                    }
                }
            }
        }
    }
}
