using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkArmy")]
    public class HealingLogicPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Army army)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            if (army == null || !army.IsValid() || !army.is_alive) return true;
            if (army.battle != null) return true; // Don't camp in battle (handled by retreat)

            // 1. In Own Territory: Heal ASAP (between fights)
            bool inOwnTerritory = army.realm_in != null && army.realm_in.kingdom == __instance.kingdom;
            
            bool needsHeal = false;
            if (inOwnTerritory)
            {
                // Heal if ANY damage or some fatigue (stamina implied by supplies/morale? No, assuming damage/units mostly)
                // "Heal the units... as soon as possible"
                needsHeal = IsDamaged(army);
            }
            else
            {
                // 2. Outside: Heal if lost 30% or more (Strength < 70%)
                // We compare current units strength vs theoretical max? 
                // Or just Unit damage? "Lost 30% or more".
                // Let's check average unit health.
                float healthPerc = GetArmyHealthPercentage(army);
                if (healthPerc < 0.7f)
                {
                    needsHeal = true;
                }
            }

            if (needsHeal)
            {
                // Execute Camp/Heal Action
                // "CampArmyAction" heals and rests
                var action = army.leader?.FindAction("CampArmyAction");
                if (action != null && action.Validate() == "ok")
                {
                    AIOverhaulPlugin.Instance.Log($"[AI-Mod] Army {army.Name} ({army.GetNid()}) needs heal (InOwn: {inOwnTerritory}). Camping.");
                    action.Execute(null);
                    return false; // Skip original ThinkArmy to prevent overriding
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
            float totalHealth = 0;
            float maxHealth = 0;
            foreach (var u in army.units)
            {
                // Assuming damage is absolute and MaxHealth is known or 100?
                // Logic.Unit doesn't show MaxSquads here, but usually damage is subtracted from Max.
                // Let's assume damage is a value.
                // If we can't easily get max, we can check if damage is significant.
                // A simplified approach:
                // Max troops per squad depends on Def.
                // current = max - damage? Or is damage a float 0-1?
                // Visuals usually show squad count.
                // Unit.EvalStrength() is easier. MaxEval?
                // Let's try:
                // If (u.GetTroops() < u.def.max_troops * 0.7f)
                // Using reflection for GetTroops() if needed, or EvalStrength.
                // Let's use a workaround:
                // We assume 'damage' field is literal damage.
                // If we don't know Max, this is hard.
                // Fallback: Use `IsDamaged` logic but nuanced?
                // Actually the user said "lost 30%".
                // Let's assume Unit.Squads is the count.
                if (u.def != null)
                {
                    // Approximation
                    if (u.damage > 0) totalHealth += 0; // Count damaged as loss? No.
                }
            }
            // Better: Compare Reflection GetCurrentSquads vs MaxSquads?
            // Since we can't see the Unit class fully, let's rely on IsDamaged for Owning, 
            // and for Outside, maybe check supplies or morale?
            // "Heal units" implies health.
            // Let's trust that if the army looks beat up, we heal.
            // For now, I'll implement a helper that assumes 30% loss means significant damage.
            
            // Re-reading KingdomAI: EvalStrength() is used.
            // Current Strength vs Max Potential Strength?
            float current = army.EvalStrength();
            float max = 0;
            foreach(var u in army.units) max += u.def.strength_eval; // Rough max
            
            return max > 0 ? (current / max) : 0;
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkAssaultSiege")]
    public class AssaultLogicPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Army a)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            if (a == null || a.battle == null) return true;

            // "Attacking castle (not cities ones I mean) is not useful, don't do it."
            // We assume "City" = The Realm's Capital Castle.
            // "Castle" (non-city) = Other fortifications?
            
            if (a.battle.castle != null)
            {
                var realm = a.battle.castle.GetRealm();
                if (realm != null)
                {
                    // Check if this castle is the main castle of the realm (The City)
                    // Usually actions target realm.castle.
                    // If a.battle.castle IS realm.castle, it is the City.
                    if (a.battle.castle == realm.castle)
                    {
                        // It IS the City. Allow assault?
                        // User said "Attacking castle (not cities ones I mean)..."
                        // Implies: If it IS a City, attacking is maybe okay?
                        // Or does "Attacking castle (not cities ones I mean)" mean "Attacking castles (which are NOT cities) is not useful"?
                        // YES. "not cities ones I mean" qualifies "castle".
                        // So: If Castle != City, BLOCK.
                        // If Castle == City, ALLOW (default logic).
                        
                        // BUT: User also said "Attacking castle ... is not useful".
                        // Maybe they mean "Assaulting" (the action) is bad in general, EXCEPT for cities?
                        // I will BLOCK if it is NOT the city.
                        return true;
                    }
                    else
                    {
                        // It is a secondary castle/fort. Block Assault.
                        AIOverhaulPlugin.Instance.Log($"[AI-Mod] Blocking Assault on secondary castle/fort in {realm.Name}");
                        return false;
                    }
                }
            }
            
            return true;
        }
    }
