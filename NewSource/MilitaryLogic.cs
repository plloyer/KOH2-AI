using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

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
                    if (soloWinChance < 0.45f && winChance >= 0.45f)
                    {
                        AIOverhaulPlugin.LogMod($" Waiting for buddy before attacking. Combined chance: {winChance:P0}");
                        army.Stop();
                        army.ai_status = "wait_for_buddy";
                        __result = true;
                        return false;
                    }
                }

                if (winChance < 0.45f)
                {
                    if (realmIn.kingdom_id == __instance.kingdom.id && army.castle == null)
                    {
                        Logic.Castle castle = realmIn.castle;
                        if (castle != null)
                        {
                            if (castle.army == null || castle.army == army)
                            {
                                AIOverhaulPlugin.LogMod($" Low win chance ({winChance:P0}) - Retreating army to {castle.name}");
                                TraverseAPI.SendArmy(__instance, army, castle, "retreat_low_chance", null);
                                __result = true;
                                return false;
                            }
                        }
                    }

                    AIOverhaulPlugin.LogMod($" Skipping battle: Win chance {winChance:P0} < 45%");
                    __result = false;
                    return false;
                }
            }

            return true;
        }
    }

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
                        string targetName = (leaderTarget is Logic.Castle c) ? c.name : leaderTarget.ToString();
                        AIOverhaulPlugin.LogMod($" Follower following leader to {targetName}");
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
                    AIOverhaulPlugin.LogMod($" Idle Knight - Returning to garrison at {nearest.name}");
                    TraverseAPI.SendArmy(__instance, army, nearest, "go_inside", null);
                }
            }
        }
    }

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

    // Prioritize Fletcher upgrade before Swordsmith in barracks
    [HarmonyPatch(typeof(Logic.Castle), "AddBuildOptions", new Type[] { typeof(bool), typeof(Logic.Resource) })]
    public class FletcherPriorityPatch
    {
        static void Postfix(Logic.Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

            // Boost Fletcher_Barracks evaluation, reduce Swordsmith evaluation
            for (int i = 0; i < Logic.Castle.upgrade_options.Count; i++)
            {
                var option = Logic.Castle.upgrade_options[i];
                if (option.def != null)
                {
                    if (option.def.id == "Fletcher_Barracks")
                    {
                        // Significantly boost Fletcher evaluation
                        option.eval *= 2.0f;
                        Logic.Castle.upgrade_options[i] = option;
                    }
                    else if (option.def.id == "Swordsmith")
                    {
                        // Check if Fletcher_Barracks is not yet built
                        Logic.Building.Def fletcherDef = __instance.game?.defs?.Get<Logic.Building.Def>("Fletcher_Barracks");
                        bool hasFletcherBarracks = fletcherDef != null && __instance.HasBuilding(fletcherDef);
                        if (!hasFletcherBarracks)
                        {
                            // Drastically reduce Swordsmith priority until Fletcher is built
                            option.eval *= 0.1f;
                            Logic.Castle.upgrade_options[i] = option;
                        }
                    }
                }
            }
        }
    }

    // Prioritize first Barracks placement in provinces with most military districts
    [HarmonyPatch(typeof(Logic.Castle), "AddBuildOptions", new Type[] { typeof(bool), typeof(Logic.Resource) })]
    public class BarracksPlacementPatch
    {
        static void Postfix(Logic.Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

            // Get Military district definition
            Logic.District.Def militaryDistrict = __instance.game?.defs?.Get<Logic.District.Def>("Military");
            if (militaryDistrict == null) return;

            // Check if this castle already has Barracks (don't boost if it does, as we want to pick the location for the FIRST one)
            Logic.Building.Def barracksDef = __instance.game?.defs?.Get<Logic.Building.Def>(BuildingNames.Barracks);
            if (barracksDef != null && __instance.HasBuilding(barracksDef)) return;

            // Find Barracks in build options
            for (int i = 0; i < Logic.Castle.build_options.Count; i++)
            {
                var option = Logic.Castle.build_options[i];
                if (option.def != null && option.def.id == BuildingNames.Barracks)
                {
                    // Boost priority based on military district slots
                    int slots = militaryDistrict.buildings?.Count ?? 0;
                    float boost = 1.0f + (slots * 0.25f); // 25% boost per slot
                    
                    option.eval *= boost;
                    Logic.Castle.build_options[i] = option;
                    
                    AIOverhaulPlugin.LogMod($" Boosting Barracks evaluation for {__instance.name} (Slots: {slots}, Boost: {boost:F1}x)");
                }
            }
        }
    }

    // Army composition: Prefer 3-4 ranged for every 4-5 melee
    // First two armies: 4 archers, 4 swordsmen
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
            bool isFirstTwoArmies = totalArmies <= 2;

            bool isRanged = udef.is_ranged;
            bool isMelee = udef.is_infantry;

            if (isFirstTwoArmies)
            {
                // First two armies: 4 archers, 4 swordsmen target
                if (isRanged && rangedCount >= 4)
                {
                    __result *= 0.01f; // Strictly discourage more ranged
                    AIOverhaulPlugin.LogMod($" Blocking extra Ranged for {kingdom.Name} army {totalArmies} (Count: {rangedCount}/4)");
                }
                else if (isMelee && meleeCount >= 4)
                {
                    __result *= 0.01f; // Strictly discourage more melee
                    AIOverhaulPlugin.LogMod($" Blocking extra Melee for {kingdom.Name} army {totalArmies} (Count: {meleeCount}/4)");
                }
                else if (isRanged && rangedCount < 4)
                {
                    __result *= 2.0f; // Strongly boost ranged priority
                }
                else if (isMelee && meleeCount < 4)
                {
                    __result *= 1.5f; // Boost melee
                }
            }
            else
            {
                // Late game: 3-4 ranged for 4-5 melee (approximately 3.5:4.5 ratio = 0.778)
                float currentRatio = meleeCount > 0 ? (float)rangedCount / meleeCount : (rangedCount > 0 ? 999f : 0.5f);
                float targetRatio = 0.8f; // ~4 ranged for 5 melee

                if (isRanged)
                {
                    if (currentRatio > targetRatio * 1.1f) // Too many ranged
                    {
                        __result *= 0.1f;
                    }
                    else if (currentRatio < targetRatio * 0.9f) // Need more ranged
                    {
                        __result *= 2.0f;
                    }
                }
                else if (isMelee)
                {
                    if (currentRatio < targetRatio * 0.9f) // Need more melee
                    {
                        __result *= 1.8f;
                    }
                    else if (currentRatio > targetRatio * 1.1f) // Too much melee
                    {
                        __result *= 0.2f;
                    }
                }
            }
        }
    }
    // Prioritize fortification upgrades when first two armies are established
    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderUpgradeFortifications")]
    public class FortificationPriorityPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Castle castle, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Check if first two armies are ready
            bool firstTwoArmiesReady = AreFirstTwoArmiesReady(__instance.kingdom);

            // Only boost priority for level 0 -> 1 upgrade when first two armies are ready
            if (!firstTwoArmiesReady || castle?.fortifications == null || castle.fortifications.level != 0)
            {
                return true; // Run original method
            }

            // Reimplementation with High priority for first fortification level
            Logic.Realm realm = castle.GetRealm();
            if (realm == null)
            {
                __result = false;
                return false;
            }

            Logic.KingdomAI.Threat threat = realm.threat;
            if (threat.level == Logic.KingdomAI.Threat.Level.Safe || threat.level >= Logic.KingdomAI.Threat.Level.Invaded)
            {
                __result = false;
                return false;
            }

            // Access categories[1] for Military weight check
            var categories = TraverseAPI.GetCategories(__instance);
            if (categories == null || categories.Length < 2 || categories[1].weight <= 0f)
            {
                __result = false;
                return false;
            }

            if (!castle.CanUpgradeFortification() || !castle.CanAffordFortificationsUpgrade())
            {
                __result = false;
                return false;
            }

            // Boost to High priority when first two armies are ready
            Logic.KingdomAI.Expense.Priority priority = Logic.KingdomAI.Expense.Priority.High;
            AIOverhaulPlugin.LogMod($" First two armies ready - upgrading {castle.name} fortifications to level 1 with HIGH priority");

            // Call ConsiderExpense with High priority
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

        static bool AreFirstTwoArmiesReady(Logic.Kingdom kingdom)
        {
            if (kingdom?.armies == null || kingdom.armies.Count < 2) return false;

            int readyArmies = 0;
            for (int i = 0; i < Math.Min(2, kingdom.armies.Count); i++)
            {
                var army = kingdom.armies[i];
                if (army == null || army.units == null) continue;

                // Check if army is full (8 units)
                bool isFull = army.units.Count >= 8;

                // Check if army has at least 250 strength
                int strength = army.EvalStrength();
                bool hasStrength = strength >= 250;

                if (isFull && hasStrength)
                {
                    readyArmies++;
                }
            }

            return readyArmies >= 2;
        }
    }

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
                if (healthPerc < 0.7f)
                {
                    needsHeal = true;
                }
            }

            if (needsHeal)
            {
                var action = army.leader?.FindAction("CampArmyAction");
                if (action != null && action.Validate() == "ok")
                {
                    // Fix: army.Name -> army.ToString()
                    AIOverhaulPlugin.LogMod($" Army {army.ToString()} ({army.GetNid()}) needs heal (InOwn: {inOwnTerritory}). Camping.");
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
                        AIOverhaulPlugin.LogMod($" Blocking Assault on secondary castle/fort in {realm.ToString()}");
                        return false;
                    }
                }
            }
            
            return true;
        }
    }
}
