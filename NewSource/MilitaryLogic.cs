using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using AIOverhaul.Constants;
using AIOverhaul.Helpers;

namespace AIOverhaul
{
    public static class BuddySystem
    {
        // Key: Army ID, Value: Buddy Army ID
        public static Dictionary<int, int> buddyMap = new Dictionary<int, int>();

        public static void ClearCache()
        {
            buddyMap.Clear();
        }

        public static Logic.Army GetBuddy(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return null;
            int armyId = army.GetNid();

            // 1. Check existing buddy
            if (buddyMap.ContainsKey(armyId))
            {
                int buddyId = buddyMap[armyId];
                // Find army object by ID
                var buddy = kingdom.armies.Find(a => a.GetNid() == buddyId);
                if (buddy != null && buddy.IsValid())
                {
                     return buddy;
                }
                else
                {
                    // Buddy died or invalid
                    buddyMap.Remove(armyId);
                    // Also clean up reverse mapping if it existed
                    if (buddyMap.ContainsKey(buddyId) && buddyMap[buddyId] == armyId)
                        buddyMap.Remove(buddyId);
                }
            }

            // 2. Assign new buddy if needed
            // Simple logic: Pair with the nearest unpartnered army
            var availableParams = kingdom.armies.Where(a => a != army && a.IsValid() && !buddyMap.ContainsKey(a.GetNid())).ToList();
            if (availableParams.Count > 0)
            {
                // Find nearest
                var nearest = availableParams.OrderBy(a => a.position.SqrDist(army.position)).First();
                buddyMap[armyId] = nearest.GetNid();
                buddyMap[nearest.GetNid()] = armyId; // Mutual
                return nearest;
            }

            return null;
        }

        public static bool IsFollower(Logic.Army army)
        {
            // Simple rule: Lower ID follows Higher ID to avoid circular following
            // Or use the map logic: if paired, one is leader, one is follower?
            // Let's say: The one with lower ID is the follower.
            if (buddyMap.ContainsKey(army.GetNid()))
            {
                int buddyId = buddyMap[army.GetNid()];
                return army.GetNid() < buddyId;
            }
            return false;
        }
    }

    // "ThinkFight" controls whether an army should engage in battle or retreat.
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkFight")]
    public class BattleEngagementPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Army army, ref bool __result)
        {
            if (army == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            Logic.Realm realmIn = army.realm_in;
            if (realmIn == null) 
            {
                // ADDED NULL CHECK
                return true; 
            }

            float ownStrength = 0;
            float friendStrength = 0;
            float enemyStrength = 0;

            if (realmIn.armies != null)
            {
                foreach (var a in realmIn.armies)
                {
                    if (a == null) continue;
                    int aKingdomId = a.kingdom_id;
                    int aStrength = a.EvalStrength();
                    if (aKingdomId == __instance.kingdom.id)
                    {
                        if (a == army) ownStrength += aStrength;
                        else friendStrength += aStrength;
                    }
                    else if (__instance.kingdom.IsEnemy(aKingdomId))
                        enemyStrength += aStrength;
                }
            }

            Logic.Army buddy = null;
            if (AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom))
            {
                buddy = BuddySystem.GetBuddy(army, __instance.kingdom);
                if (buddy != null && buddy.battle != null && buddy.battle != army.battle)
                {
                    // Buddy is in a different battle, maybe join them?
                    // This creates a tendency to swarm
                    // For now, just logging or minor influence
                }
            }
            bool buddyPresent = false; // Disabled
            if (buddy != null)
            {
                if (buddy.realm_in == realmIn) buddyPresent = true;
            }

            if (ownStrength + friendStrength + enemyStrength > 0)
            {
                float totalFriendly = ownStrength + friendStrength;
                float winChance = totalFriendly / (totalFriendly + enemyStrength);

                if (buddy != null && !buddyPresent && enemyStrength > 0)
                {
                    float soloWinChance = ownStrength / (ownStrength + enemyStrength);
                    if (soloWinChance < GameBalance.MinBattleWinChance && winChance >= GameBalance.MinBattleWinChance)
                    {
                        army.Stop();
                        army.ai_status = "wait_for_buddy";
                        __result = true;
                        return false;
                    }
                }

                if (winChance < GameBalance.MinBattleWinChance)
                {
                    if (realmIn.kingdom_id == __instance.kingdom.id && army.castle == null)
                    {
                        Logic.Castle castle = realmIn.castle;
                        if (castle != null)
                        {
                            if (castle.army == null || castle.army == army)
                            {
                                TraverseAPI.SendArmy(__instance, army, castle, "retreat_low_chance", null);
                                __result = true;
                                return false;
                            }
                        }
                    }

                    __result = false;
                    return false;
                }
            }

            return true;
        }
    }

    // "ThinkArmy" handles general army tick logic including movement and actions.
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkArmy")]
    public class IdleArmyPatch
    {
        static void Postfix(Logic.KingdomAI __instance, Logic.Army army)
        {
            if (army == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (army.IsHiredMercenary() || army.battle != null) return;

            string status = army.ai_status;

            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;

            // Access private field via reflection if needed, but usually public?
            // Actually ai_status is likely internal/private. 
            // Assuming "idle" check is correct from decompilation context.
            // If status is not accessible, we skip.
            
            // Logic: If we are idle, follow our buddy
            if (BuddySystem.IsFollower(army))
            {
                Logic.Army leader = BuddySystem.GetBuddy(army, __instance.kingdom);
                if (leader != null && leader.movement.IsMoving() && !army.movement.IsMoving())
                {
                    // Order to follow
                    // Need access to army methods. 
                    // army.Interact(leader, false); // Interact often handles 'follow' for friendly units
                }
            }
            
            if (status == "idle" || status == "wait_orders") // was: BuddySystem.IsFollower
            {
                Logic.Army leader = BuddySystem.GetBuddy(army, __instance.kingdom); // was: BuddySystem.GetBuddy(army, __instance.kingdom);
                if (leader != null)
                {
                    Logic.MapObject leaderTarget = leader.GetTarget();
                    if (leaderTarget != null && leaderTarget != army.GetTarget())
                    {
                        TraverseAPI.SendArmy(__instance, army, leaderTarget, "follow_buddy", null);
                        return;
                    }
                }
            }

            if (status == "idle" && army.castle == null)
            {
                Logic.Castle nearest = TraverseAPI.FindNearestOwnCastle(__instance, army, true);
                if (nearest != null)
                {
                    AIOverhaulPlugin.LogMod($" Idle Knight - Returning to garrison at {nearest.name}", LogCategory.Military);
                    TraverseAPI.SendArmy(__instance, army, nearest, "go_inside", null);
                }
            }
        }
    }

    // "EvalHireUnits" determines if militia/peasants should be raised in a castle.
    [HarmonyPatch(typeof(Logic.Castle), "EvalHireUnits")]
    public class PeasantRecruitmentBlockPatch
    {
        static void Prefix(Logic.Castle __instance, ref bool allow_militia)
        {
            if (AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom()))
            {
                allow_militia = false;
            }
        }
    }

    // Prioritize Swordsmith upgrade before Fletcher in barracks
    // Swordsmith = melee units (swordsmen), Fletcher = ranged units (archers)
    // We need melee first for balanced armies (4 melee + 4 ranged)
    // "AddBuildOptions" generates the list of available buildings and upgrades for a castle.
    [HarmonyPatch(typeof(Logic.Castle), "AddBuildOptions", new Type[] { typeof(bool), typeof(Logic.Resource) })]
    public class SwordsmithPriorityPatch
    {
        static void Postfix(Logic.Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

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
                        if (__instance.buildings != null)
                        {
                            foreach (var building in __instance.buildings)
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
    }

    // Prioritize first Barracks placement in provinces with most military districts
    // "AddBuildOptions" generates the list of available buildings and upgrades for a castle.
    [HarmonyPatch(typeof(Logic.Castle), "AddBuildOptions", new Type[] { typeof(bool), typeof(Logic.Resource) })]
    public class BarracksPlacementPatch
    {
        static void Postfix(Logic.Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

            // Get Castle district definition (Barracks goes in Castle district)
            Logic.District.Def castleDistrict = DistrictHelper.GetDistrict(__instance.game, DistrictNames.Castle);
            if (castleDistrict == null) return;

            // Check if this castle HAS the Castle district
            bool hasCastleDistrict = __instance.HasDistrict(castleDistrict);

            // Check if kingdom already has any barracks (across all provinces)
            bool kingdomHasBarracks = false;
            Logic.Building.Def barracksDef = __instance.game?.defs?.Get<Logic.Building.Def>(BuildingNames.Barracks);
            if (barracksDef != null)
            {
                var kingdom = __instance.GetKingdom();
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

                            if (__instance.GetKingdom()?.Name == "England")
                            {
                                AIOverhaulPlugin.LogMod($"[ENGLAND] BOOSTING first Barracks in {__instance.name} (Slots: {slots}, Boost: {boost:F1}x)", LogCategory.Military);
                            }
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

                            if (__instance.GetKingdom()?.Name == "England")
                            {
                                AIOverhaulPlugin.LogMod($"[ENGLAND] BLOCKING second Barracks in {__instance.name} - requires Castle district", LogCategory.Military);
                            }
                        }
                    }
                }
            }
        }
    }

    // Army composition: Prefer 3-4 ranged for every 4-5 melee
    // First two armies: 4 archers, 4 swordsmen
    // "EvalHireUnit" evaluates the desirability of hiring a specific unit type for an army.
    [HarmonyPatch(typeof(Logic.KingdomAI), "EvalHireUnit")]
    public class ArmyCompositionPatch
    {
        static void Postfix(Logic.Unit.Def udef, Logic.Army army, ref float __result)
        {
            if (army == null || udef == null) return;
            Logic.Kingdom kingdom = army.GetKingdom();
            if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;

            // Count current ranged vs melee units in this army
            int rangedCount = 0;
            int meleeCount = 0;

            if (army.units != null)
            {
                foreach (var unit in army.units)
                {
                    if (unit?.def == null) continue;
                    if (unit.def.is_ranged)
                        rangedCount++;
                    else if (unit.def.is_infantry)
                        meleeCount++;
                }
            }

            // Count total armies to determine if this is one of the first two
            int totalArmies = kingdom.armies?.Count ?? 0;
            bool isFirstTwoArmies = totalArmies <= GameBalance.FirstTwoArmiesCount;

            bool isRanged = udef.is_ranged;
            bool isMelee = udef.is_infantry;

            if (isFirstTwoArmies)
            {
                // CRITICAL: Check if Swordsmith is built (required for melee units)
                // If we have 4+ archers but few melee, and Swordsmith isn't built,
                // block ALL hiring to force building Swordsmith first
                bool hasSwordsmith = false;
                if (kingdom.realms != null)
                {
                    foreach (var realm in kingdom.realms)
                    {
                        if (realm?.castle != null)
                        {
                            var buildings = realm.castle.buildings;
                            if (buildings != null)
                            {
                                foreach (var building in buildings)
                                {
                                    if (building?.def?.id == "Swordsmith")
                                    {
                                        hasSwordsmith = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (hasSwordsmith) break;
                    }
                }

                // If we have imbalanced armies (lots of archers, few melee) and no Swordsmith,
                // block ALL hiring until Swordsmith is built
                if (!hasSwordsmith && rangedCount >= GameBalance.EarlyGameRangedCount && meleeCount < GameBalance.EarlyGameMeleeCount)
                {
                    if (kingdom.Name == "England")
                    {
                        AIOverhaulPlugin.LogMod($"[ENGLAND] BLOCKING all unit hiring: ranged={rangedCount}, melee={meleeCount}, need Swordsmith first!", LogCategory.Military);
                    }
                    __result *= GameBalance.StrictBlockMultiplier;
                    return;
                }

                // First two armies: 4 archers, 4 swordsmen target
                if (isRanged && rangedCount >= GameBalance.EarlyGameRangedCount)
                {
                    __result *= GameBalance.StrictBlockMultiplier;
                }
                else if (isMelee && meleeCount >= GameBalance.EarlyGameMeleeCount)
                {
                    __result *= GameBalance.StrictBlockMultiplier;
                }
                else if (isRanged && rangedCount < GameBalance.EarlyGameRangedCount)
                {
                    __result *= GameBalance.StrongBoostMultiplier;
                }
                else if (isMelee && meleeCount < GameBalance.EarlyGameMeleeCount)
                {
                    __result *= GameBalance.MediumBoostMultiplier;
                }
            }
            else
            {
                // Late game: 3-4 ranged for 4-5 melee (approximately 3.5:4.5 ratio = 0.778)
                float currentRatio = meleeCount > 0 ? (float)rangedCount / meleeCount : (rangedCount > 0 ? 999f : 0.5f);
                float targetRatio = GameBalance.LateGameRangedMeleeRatio;

                if (isRanged)
                {
                    if (currentRatio > targetRatio * 1.1f) // Too many ranged
                    {
                        __result *= GameBalance.StrongPenaltyMultiplier;
                    }
                    else if (currentRatio < targetRatio * GameBalance.RatioToleranceLow) // Need more ranged
                    {
                        __result *= GameBalance.StrongBoostMultiplier;
                    }
                }
                else if (isMelee)
                {
                    if (currentRatio < targetRatio * GameBalance.RatioToleranceLow) // Need more melee
                    {
                        __result *= 1.8f;
                    }
                    else if (currentRatio > targetRatio * 1.1f) // Too much melee
                    {
                        __result *= GameBalance.MediumPenaltyMultiplier;
                    }
                }
            }
        }
    }
    // Prioritize fortification upgrades when first two armies are established
    // "ConsiderUpgradeFortifications" decides if castle walls and defenses should be upgraded.
    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderUpgradeFortifications")]
    public class FortificationPriorityPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Castle castle, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Check if first two armies are ready
            bool firstTwoArmiesReady = KingdomHelper.HasTwoReadyArmies(__instance.kingdom);

            // If not ready, use default logic (which tends to be low priority)
            if (!firstTwoArmiesReady) return true;
            
            // Reimplementation with URGENT priority for ALL fortification levels if armies are ready
            Logic.Realm realm = castle?.GetRealm();
            if (realm == null)
            {
                __result = false;
                return false;
            }

            // REMOVED SAFE CHECK: We want to upgrade even in peace time if we have the armies ready
            Logic.KingdomAI.Threat threat = realm.threat;
            
            // Still block if invaded as building during siege is usually invalid/risky logic
            if (threat.level >= Logic.KingdomAI.Threat.Level.Invaded)
            {
                __result = false;
                return false;
            }

            // Check affordability
            if (!castle.CanUpgradeFortification() || !castle.CanAffordFortificationsUpgrade())
            {
                __result = false;
                return false;
            }

            // Boost to URGENT priority
            // This normally bypasses category weight checks in ConsiderExpense
            Logic.KingdomAI.Expense.Priority priority = Logic.KingdomAI.Expense.Priority.Urgent;

            // Call ConsiderExpense with Urgent priority
            TraverseAPI.ConsiderExpense(__instance,
                Logic.KingdomAI.Expense.Type.UpgradeFortifications,
                null,
                castle,
                Logic.KingdomAI.Expense.Category.Military,
                priority,
                null);

            __result = true;
            return false; // Skip original method
        }
    }

    // "ThinkArmy" handles general army tick logic including movement and actions.
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkArmy")]
    public class HealingLogicPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Army army)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            // Removed is_alive, used IsValid() which is standard.
            if (army == null || !army.IsValid()) return true;
            if (army.battle != null) return true;

            // 1. In Own Territory
            // Fix: Realm.kingdom -> Realm.GetKingdom()
            var realm = army.realm_in;
            bool inOwnTerritory = realm != null && realm.GetKingdom() == __instance.kingdom;
            
            bool needsHeal = false;
            if (inOwnTerritory)
            {
                needsHeal = IsDamaged(army);
            }
            else
            {
                float healthPerc = GetArmyHealthPercentage(army);
                if (healthPerc < GameBalance.HealthRetreatThreshold)
                {
                    needsHeal = true;
                }
            }

            if (needsHeal)
            {
                var action = army.leader?.FindAction("CampArmyAction");
                if (action != null && action.Validate() == "ok")
                {
                    action.Execute(null);
                    return false;
                }
            }

            return true;
        }

        static bool IsDamaged(Logic.Army army)
        {
            if (army.units == null) return false;
            foreach (var u in army.units)
            {
                if (u.damage > 0) return true;
            }
            return false;
        }

        static float GetArmyHealthPercentage(Logic.Army army)
        {
            if (army.units == null || army.units.Count == 0) return 0;
            float max = 0;
            foreach(var u in army.units) max += u.def.strength_eval;
            float current = army.EvalStrength();
            return max > 0 ? (current / max) : 0;
        }
    }

    // "ThinkAssaultSiege" decides whether a besieging army should launch an assault on the castle.
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkAssaultSiege")]
    public class AssaultLogicPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Army a)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            
            // Paranoid check for army and battle
            if (a == null) return true;
            // Accessing a.battle might technically throw if 'a' is in a weird state, but usually property access is safe-ish
            if (a.battle == null) return true;

            // Fix: Battle.castle -> Battle.settlement as Castle
            var castle = a.battle.settlement as Logic.Castle;
            
            if (castle != null)
            {
                var realm = castle.GetRealm();
                if (realm != null)
                {
                    // Check if this castle is the main castle of the realm (The City)
                    if (castle == realm.castle)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
    }
}
