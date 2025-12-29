using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace AIOverhaul
{
    // TODO: BuddySystem - Commented out due to compilation errors with Army.id
    // Need to investigate correct way to track army relationships
    /*
    public static class BuddySystem
    {
        private static Dictionary<int, int> buddyMap = new Dictionary<int, int>();

        public static void ClearCache()
        {
            buddyMap.Clear();
        }

        public static List<Logic.Army> GetAllArmies(Logic.Kingdom kingdom)
        {
            return kingdom?.armies ?? new List<Logic.Army>();
        }

        public static bool IsFollower(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return false;
            var buddy = GetBuddy(army, kingdom);
            if (buddy == null) return false;

            // Determining "follower" role deterministically: Lower ID is leader, Higher ID is follower
            return army.id > buddy.id;
        }

        public static Logic.Army GetBuddy(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return null;

            // 1. Check existing cache
            if (buddyMap.TryGetValue(army.id, out int buddyId))
            {
                var buddy = kingdom.armies.Find(a => a.id == buddyId);
                // Verify buddy is alive and valid. Army.id should be unique and persistent.
                if (buddy != null && buddy.realm_in != null) 
                {
                    return buddy;
                }
                else
                {
                    // Buddy is dead or invalid, clean up
                    buddyMap.Remove(army.id);
                }
            }

            // 2. Try to find a new buddy
            foreach (var potentialBuddy in kingdom.armies)
            {
                if (potentialBuddy == null || potentialBuddy == army) continue;
                if (potentialBuddy.realm_in == null) continue; // Skip invalid armies

                // Check if this potential buddy is already assigned (or has me as buddy? Logic below handles mutual assignment)
                // We want to find a free agent.
                if (!buddyMap.ContainsKey(potentialBuddy.id))
                {
                    // Found a free agent! Link them.
                    buddyMap[army.id] = potentialBuddy.id;
                    buddyMap[potentialBuddy.id] = army.id;
                    return potentialBuddy;
                }
            }

            return null;
        }
    }
    */

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
                if (buddy != null && buddy.IsValid() && buddy.is_alive)
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
                        AIOverhaulPlugin.Instance.Log($"[AI-Mod] Waiting for buddy before attacking. Combined chance: {winChance:P0}");
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
                                AIOverhaulPlugin.Instance.Log($"[AI-Mod] Low win chance ({winChance:P0}) - Retreating army to {castle.name}");
                                Traverse.Create(__instance).Method("Send", new object[] { army, castle, "retreat_low_chance", null }).GetValue();
                                __result = true;
                                return false;
                            }
                        }
                    }

                    AIOverhaulPlugin.Instance.Log($"[AI-Mod] Skipping battle: Win chance {winChance:P0} < 45%");
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
                        AIOverhaulPlugin.Instance.Log($"[AI-Mod] Follower following leader to {targetName}");
                        Traverse.Create(__instance).Method("Send", new object[] { army, leaderTarget, "follow_buddy", null }).GetValue();
                        return;
                    }
                }
            }

            if (status == "idle" && army.castle == null)
            {
                Logic.Castle nearest = (Logic.Castle)Traverse.Create(__instance).Method("FindNearestOwnCastle", new object[] { army, true }).GetValue();
                if (nearest != null)
                {
                    AIOverhaulPlugin.Instance.Log($"[AI-Mod] Idle Knight - Returning to garrison at {nearest.name}");
                    Traverse.Create(__instance).Method("Send", new object[] { army, nearest, "go_inside", null }).GetValue();
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
}
