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
                
                // Validate buddy exists AND is close enough (hysteresis)
                if (buddy != null && buddy.IsValid())
                {
                    float distSq = buddy.position.SqrDist(army.position);
                    float maxDistSq = GameBalance.BuddyBreakDistance * GameBalance.BuddyBreakDistance;
                    
                    if (distSq <= maxDistSq)
                    {
                        return buddy;
                    }
                }
                
                // Buddy died, invalid, or too far
                buddyMap.Remove(armyId);
                // Also clean up reverse mapping if it existed
                if (buddyMap.ContainsKey(buddyId) && buddyMap[buddyId] == armyId)
                    buddyMap.Remove(buddyId);
            }

            // 2. Assign new buddy if needed
            // Simple logic: Pair with the nearest unpartnered army WITHIN RANGE
            var availableParams = kingdom.armies.Where(a => a != army && a.IsValid() && !buddyMap.ContainsKey(a.GetNid())).ToList();
            if (availableParams.Count > 0)
            {
                // Find nearest
                var nearest = availableParams.OrderBy(a => a.position.SqrDist(army.position)).First();
                float distSq = nearest.position.SqrDist(army.position);
                float maxAssignDistSq = GameBalance.MaxBuddyDistance * GameBalance.MaxBuddyDistance;

                if (distSq <= maxAssignDistSq)
                {
                    buddyMap[armyId] = nearest.GetNid();
                    buddyMap[nearest.GetNid()] = armyId; // Mutual
                    return nearest;
                }
            }

            return null;
        }

        public static bool IsFollower(Logic.Army army)
        {
            // Simple rule: Lower ID follows Higher ID to avoid circular following
            if (buddyMap.ContainsKey(army.GetNid()))
            {
                int buddyId = buddyMap[army.GetNid()];
                // Lower ID is the follower. Higher ID is the leader.
                return army.GetNid() < buddyId;
            }
            return false;
        }
    }

    // "ThinkFight" controls whether an army should engage in battle or retreat.
    // Intent: BattleEngagementPatch
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkFight")]
    public class KingdomAI_ThinkFight
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
                    // Check if buddy is available to help
                    bool isBuddyAvailable = buddy.battle == null;
                    // Check distance
                    float distToBuddySq = buddy.position.SqrDist(army.position);
                    float maxWaitDistSq = GameBalance.BuddyWaitDistance * GameBalance.BuddyWaitDistance;
                    bool isBuddyClose = distToBuddySq <= maxWaitDistSq;

                    if (isBuddyAvailable && isBuddyClose)
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
    // Intent: ThinkArmy patches (IdleArmyPatch + HealingLogicPatch)
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkArmy")]
    public class KingdomAI_ThinkArmy
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
                needsHeal = AIOverhaul.Helpers.MilitaryHelper.IsDamaged(army);
            }
            else
            {
                float healthPerc = AIOverhaul.Helpers.MilitaryHelper.GetArmyHealthPercentage(army);
                if (healthPerc < GameBalance.HealthRetreatThreshold)
                {
                    needsHeal = true;
                }
            }

            if (needsHeal)
            {
                var action = army.leader?.FindAction(ActionNames.CampArmy);
                if (action != null && action.Validate() == "ok")
                {
                    action.Execute(null);
                    return false;
                }
            }

            return true;
        }
        
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
            
            // Logic: If we are idle, follow our buddy (ONLY if we are the follower)
            if (BuddySystem.IsFollower(army))
            {
                Logic.Army leader = BuddySystem.GetBuddy(army, __instance.kingdom);
                if (leader != null && leader.IsValid())
                {
                    // If we are idle or waiting, and leader is doing something, copy them
                    if (status == "idle" || status == "wait_orders" || status == "wait_for_buddy")
                    {
                        // 1. If leader has a specific target (enemy, castle), engage it
                        Logic.MapObject leaderTarget = leader.GetTarget();
                        if (leaderTarget != null && leaderTarget != army.GetTarget())
                        {
                            // Avoid following if leader is just moving to a point (target is null usually for move pos?)
                            // Actually GetTarget returns the interacted object.
                            
                            // Check if leaderTarget is valid target for us
                            TraverseAPI.SendArmy(__instance, army, leaderTarget, "follow_buddy_target", null);
                            return;
                        }

                        // 2. If leader is moving but has no target, follow the leader himself
                        if (leader.movement.IsMoving() && army.GetTarget() != leader)
                        {
                             // LogInfo("Follower {army.GetNid()} following Leader {leader.GetNid()}");
                             TraverseAPI.SendArmy(__instance, army, leader, "follow_buddy_leader", null);
                             return;
                        }
                    }
                }
            }

            if (status == "idle" && army.castle == null)
            {
                Logic.Castle nearest = TraverseAPI.FindNearestOwnCastle(__instance, army, true);
                if (nearest != null)
                {
                    AIOverhaulPlugin.LogInfo($" Idle Knight - Returning to garrison at {nearest.name}", LogCategory.Military);
                    TraverseAPI.SendArmy(__instance, army, nearest, "go_inside", null);
                }
            }
        }
    }

    // "EvalHireUnits" determines if militia/peasants should be raised in a castle.
    // Intent: PeasantRecruitmentBlockPatch
    [HarmonyPatch(typeof(Logic.Castle), "EvalHireUnits")]
    public class Castle_EvalHireUnits
    {
        static void Prefix(Logic.Castle __instance, ref bool allow_militia)
        {
            if (AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom()))
            {
                allow_militia = false;
            }
        }
    }
    
    // Army composition: Prefer 3-4 ranged for every 4-5 melee
    // First two armies: 4 archers, 4 swordsmen
    // "EvalHireUnit" evaluates the desirability of hiring a specific unit type for an army.
    // Intent: ArmyCompositionPatch
    // [HarmonyPatch(typeof(Logic.KingdomAI), "EvalHireUnit")]
    // public class KingdomAI_EvalHireUnit
    // {
    //     static void Postfix(Logic.Unit.Def udef, Logic.Army army, ref float __result)
    //     {
    //         if (army == null || udef == null) return;
    //         Logic.Kingdom kingdom = army.GetKingdom();
    //         if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;
    //
    //         // Count current ranged vs melee units in this army
    //
    //         int rangedCount = 0;
    //         int meleeCount = 0;
    //
    //         if (army.units != null)
    //         {
    //             foreach (var unit in army.units)
    //             {
    //                 if (unit?.def == null) continue;
    //                 if (unit.def.is_ranged)
    //                     rangedCount++;
    //                 else if (unit.def.is_infantry)
    //                     meleeCount++;
    //             }
    //         }
    //
    //         // Count total armies to determine if this is one of the first two
    //         int totalArmies = kingdom.armies?.Count ?? 0;
    //         bool isFirstTwoArmies = totalArmies <= GameBalance.FirstTwoArmiesCount;
    //
    //         bool isRanged = udef.is_ranged;
    //         bool isMelee = udef.is_infantry;
    //
    //         if (isFirstTwoArmies)
    //         {
    //             // CRITICAL: Check if Swordsmith is built (required for melee units)
    //             // If we have 4+ archers but few melee, and Swordsmith isn't built,
    //             // block ALL hiring to force building Swordsmith first
    //             bool hasSwordsmith = false;
    //             if (kingdom.realms != null)
    //             {
    //                 foreach (var realm in kingdom.realms)
    //                 {
    //                     if (realm?.castle != null)
    //                     {
    //                         var buildings = realm.castle.buildings;
    //                         if (buildings != null)
    //                         {
    //                             foreach (var building in buildings)
    //                             {
    //                                 if (building?.def?.id == BuildingUpgradeNames.Swordsmith)
    //                                 {
    //                                     hasSwordsmith = true;
    //                                     break;
    //                                 }
    //                             }
    //                         }
    //                     }
    //                     if (hasSwordsmith) break;
    //                 }
    //             }
    //
    //             // If we have imbalanced armies (lots of archers, few melee) and no Swordsmith,
    //             // block ALL hiring until Swordsmith is built
    //             if (!hasSwordsmith && rangedCount >= GameBalance.EarlyGameRangedCount && meleeCount < GameBalance.EarlyGameMeleeCount)
    //             {
    //                 AIOverhaulPlugin.LogDebug($"BLOCKING all unit hiring: ranged={rangedCount}, melee={meleeCount}, need Swordsmith first!", LogCategory.Military, kingdom);
    //                 __result *= GameBalance.StrictBlockMultiplier;
    //                 return;
    //             }
    //
    //             // First two armies: 4 archers, 4 swordsmen target
    //             if (isRanged && rangedCount >= GameBalance.EarlyGameRangedCount)
    //             {
    //                 __result *= GameBalance.StrictBlockMultiplier;
    //             }
    //             else if (isMelee && meleeCount >= GameBalance.EarlyGameMeleeCount)
    //             {
    //                 __result *= GameBalance.StrictBlockMultiplier;
    //             }
    //             else if (isRanged && rangedCount < GameBalance.EarlyGameRangedCount)
    //             {
    //                 __result *= GameBalance.StrongBoostMultiplier;
    //             }
    //             else if (isMelee && meleeCount < GameBalance.EarlyGameMeleeCount)
    //             {
    //                 __result *= GameBalance.MediumBoostMultiplier;
    //             }
    //         }
    //         else
    //         {
    //             // Late game: 3-4 ranged for 4-5 melee (approximately 3.5:4.5 ratio = 0.778)
    //             float currentRatio = meleeCount > 0 ? (float)rangedCount / meleeCount : (rangedCount > 0 ? 999f : 0.5f);
    //             float targetRatio = GameBalance.LateGameRangedMeleeRatio;
    //
    //             if (isRanged)
    //             {
    //                 if (currentRatio > targetRatio * 1.1f) // Too many ranged
    //                 {
    //                     __result *= GameBalance.StrongPenaltyMultiplier;
    //                 }
    //                 else if (currentRatio < targetRatio * GameBalance.RatioToleranceLow) // Need more ranged
    //                 {
    //                     __result *= GameBalance.StrongBoostMultiplier;
    //                 }
    //             }
    //             else if (isMelee)
    //             {
    //                 if (currentRatio < targetRatio * GameBalance.RatioToleranceLow) // Need more melee
    //                 {
    //                     __result *= 1.8f;
    //                 }
    //                 else if (currentRatio > targetRatio * 1.1f) // Too much melee
    //                 {
    //                     __result *= GameBalance.MediumPenaltyMultiplier;
    //                 }
    //             }
    //         }
    //     }
    // }
    
    // Prioritize fortification upgrades when first two armies are established
    // "ConsiderUpgradeFortifications" decides if castle walls and defenses should be upgraded.
    // Intent: FortificationPriorityPatch
    // [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderUpgradeFortifications")]
    // public class KingdomAI_ConsiderUpgradeFortifications
    // {
    //     static bool Prefix(Logic.KingdomAI __instance, Logic.Castle castle, ref bool __result)
    //     {
    //         if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
    //
    //         // Block fortifications if rushing tradition (save gold for Writing/Learning)
    //         if (TraditionHelper.ShouldRushTradition(__instance.kingdom))
    //         {
    //             __result = false;
    //             return false;
    //         }
    //
    //         // Check if first two armies are ready (Strategy requirement)
    //         bool firstTwoArmiesReady = KingdomHelper.HasTwoReadyArmies(__instance.kingdom);
    //         if (!firstTwoArmiesReady)
    //         {
    //             __result = false;
    //             return false;
    //         }
    //
    //         // Make fortifications URGENT priority once armies are ready
    //         Logic.Realm realm = castle?.GetRealm();
    //         if (realm == null)
    //         {
    //             __result = false;
    //             return false;
    //         }
    //
    //         // Check affordability
    //         if (!castle.CanUpgradeFortification() || !castle.CanAffordFortificationsUpgrade())
    //         {
    //             __result = false;
    //             return false;
    //         }
    //
    //         // Upgrade with URGENT priority
    //         TraverseAPI.ConsiderExpense(__instance,
    //             Logic.KingdomAI.Expense.Type.UpgradeFortifications,
    //             null,
    //             castle,
    //             Logic.KingdomAI.Expense.Category.Military,
    //             Logic.KingdomAI.Expense.Priority.Urgent,
    //             null);
    //
    //         __result = true;
    //         return false;
    //     }
    // }



    // "ThinkAssaultSiege" decides whether a besieging army should launch an assault on the castle.
    // Intent: AssaultLogicPatch
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkAssaultSiege")]
    public class KingdomAI_ThinkAssaultSiege
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
