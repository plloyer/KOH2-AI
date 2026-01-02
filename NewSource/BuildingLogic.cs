using HarmonyLib;
using Logic;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using AIOverhaul.Constants;
using AIOverhaul.Helpers;

namespace AIOverhaul
{
    // "AddBuildOptions" generates the list of available buildings and upgrades for a castle.
    // Intent: Consolidated AddBuildOptionsPatch
    // Merges logic for:
    // 1. Swordsmith Priority (Military)
    // 2. Barracks Placement (Military)
    // 3. Religion Building Logic (Economy)
    [HarmonyPatch(typeof(Logic.Castle), "AddBuildOptions", new Type[] { typeof(bool), typeof(Logic.Resource) })]
    public class AddBuildOptionsPatch
    {
        static void Postfix(Logic.Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

            ApplySwordsmithLogic(__instance);
            ApplyBarracksLogic(__instance);
            ApplyReligionLogic(__instance);
        }

        // --- Logic Blocks ---

        private static void ApplySwordsmithLogic(Logic.Castle castle)
        {
            // Boost Swordsmith evaluation, reduce Fletcher evaluation
            for (int i = 0; i < Logic.Castle.upgrade_options.Count; i++)
            {
                var option = Logic.Castle.upgrade_options[i];
                if (option.def != null)
                {
                    if (option.def.id == "Swordsmith")
                    {
                        // Significantly boost Swordsmith evaluation (need melee units first)
                        option.eval *= GameBalance.StrongBoostMultiplier;
                        Logic.Castle.upgrade_options[i] = option;
                    }
                    else if (option.def.id == "Fletcher_Barracks")
                    {
                        // Check if Swordsmith is not yet built
                        bool hasSwordsmith = false;
                        if (castle.buildings != null)
                        {
                            foreach (var building in castle.buildings)
                            {
                                if (building?.def?.id == "Swordsmith")
                                {
                                    hasSwordsmith = true;
                                    break;
                                }
                            }
                        }

                        if (!hasSwordsmith)
                        {
                            // Drastically reduce Fletcher priority until Swordsmith is built
                            option.eval *= GameBalance.StrongPenaltyMultiplier;
                            Logic.Castle.upgrade_options[i] = option;
                        }
                    }
                }
            }
        }

        private static void ApplyBarracksLogic(Logic.Castle castle)
        {
            // Get Castle district definition (Barracks goes in Castle district)
            Logic.District.Def castleDistrict = DistrictHelper.GetDistrict(castle.game, DistrictNames.Castle);
            if (castleDistrict == null) return;

            // Check if this castle HAS the Castle district
            bool hasCastleDistrict = castle.HasDistrict(castleDistrict);

            // Check if kingdom already has any barracks (across all provinces)
            bool kingdomHasBarracks = false;
            Logic.Building.Def barracksDef = castle.game?.defs?.Get<Logic.Building.Def>(BuildingNames.Barracks);
            if (barracksDef != null)
            {
                var kingdom = castle.GetKingdom();
                if (kingdom?.realms != null)
                {
                    foreach (var realm in kingdom.realms)
                    {
                        if (realm?.castle != null && realm.castle.HasBuilding(barracksDef))
                        {
                            kingdomHasBarracks = true;
                            break;
                        }
                    }
                }
            }

            // Find Barracks in build options
            for (int i = Logic.Castle.build_options.Count - 1; i >= 0; i--)
            {
                var option = Logic.Castle.build_options[i];
                if (option.def != null && option.def.id == BuildingNames.Barracks)
                {
                    if (!kingdomHasBarracks)
                    {
                        // FIRST barracks - allow anywhere, but boost Castle districts
                        if (hasCastleDistrict)
                        {
                            // Boost based on Castle district slots
                            int slots = castleDistrict.buildings?.Count ?? 0;
                            float boost = 1.0f + (slots * GameBalance.BarracksSlotBoostPerSlot);

                            option.eval *= boost;
                            Logic.Castle.build_options[i] = option;

                            AIOverhaulPlugin.LogDiagnostic($"BOOSTING first Barracks in {castle.name} (Slots: {slots}, Boost: {boost:F1}x)", LogCategory.Military, castle.GetKingdom());
                        }
                        // else: no boost but still allow (fallback if no Castle district exists)
                    }
                    else
                    {
                        // Kingdom already has barracks - ONLY allow in Castle districts
                        if (!hasCastleDistrict)
                        {
                            // BLOCK second+ barracks if no Castle district
                            Logic.Castle.build_options.RemoveAt(i);

                            AIOverhaulPlugin.LogDiagnostic($"BLOCKING second Barracks in {castle.name} - requires Castle district", LogCategory.Military, castle.GetKingdom());
                        }
                    }
                }
            }
        }

        private static void ApplyReligionLogic(Logic.Castle castle)
        {
            // Get Religion district definition
            Logic.District.Def religionDistrict = DistrictHelper.GetDistrict(castle.game, DistrictNames.Religion);
            if (religionDistrict == null) return;

            // Check if this castle has the Religion district
            bool hasReligionDistrict = castle.HasDistrict(religionDistrict);

            // Find all religious buildings in build options
            for (int i = Castle.build_options.Count - 1; i >= 0; i--)
            {
                var option = Castle.build_options[i];
                if (option.def == null) continue;

                bool isReligiousBuilding = IsReligiousBuilding(option.def.id);

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
                        int religionSlots = CountReligionSlots(castle, religionDistrict);
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

                bool isReligiousBuilding = IsReligiousBuilding(option.def.id);

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
                        int religionSlots = CountReligionSlots(castle, religionDistrict);
                        float boost = 1.0f + (religionSlots * GameBalance.ReligionBuildingBoostPerSlot);
                        option.eval *= boost;
                        Castle.upgrade_options[i] = option;
                    }
                }
            }
        }

        // --- Helpers ---

        static bool IsReligiousBuilding(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return false;

            // Religious buildings include Church, Masjid, Temple, Cathedral, GreatMosque
            return buildingId == BuildingNames.Church ||
                   buildingId == BuildingNames.Masjid ||
                   buildingId == BuildingNames.Temple ||
                   buildingId == BuildingNames.Cathedral ||
                   buildingId == BuildingNames.GreatMosque;
        }

        static int CountReligionSlots(Castle castle, Logic.District.Def religionDistrict)
        {
            if (religionDistrict?.buildings == null) return 0;

            // Count how many religion building slots exist in this district definition
            return religionDistrict.buildings.Count;
        }
    }
}
