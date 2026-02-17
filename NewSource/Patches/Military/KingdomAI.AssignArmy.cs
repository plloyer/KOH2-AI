using System;
using System.Linq;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // See ARMY_MANAGEMENT_GUIDE.md
    // "AssignArmy" assigns a specific army to a threat.
    // Intent: ArmyCoordinationPatch
    [HarmonyPatch(typeof(KingdomAI), "AssignArmy")]
    public class KingdomAI_AssignArmy
    {
        const string k_LogPrefix = "[AssignArmy]";
        const float k_PowerRatioToFight = 1.3f;
        static bool Prefix(KingdomAI __instance, KingdomAI.Threat threat, int pass, ref bool __result)
        {
            if (__instance?.kingdom?.IsEnhancedAI() ?? threat == null) return true;

            // OFFENSIVE: Attacking enemy territory - require MinArmiesForWar full armies
            // Exception: In dominant 1v1 wars, allow attacking with just 1 full army
            if (threat.level == KingdomAI.Threat.Level.Attack)
            {
                int readyArmies = 0;
                if (__instance.kingdom.armies != null)
                    readyArmies = __instance.kingdom.armies.Count(a => a != null && a.IsValid() && KingdomAI.IsFull(a));

                int requiredArmies = __instance.kingdom.IsDominantIn1v1War() ? 1 : GameBalance.MinArmiesToDeclareWar;

                if (readyArmies < requiredArmies)
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocking offensive assignment to {threat.realm?.name}: waiting for {requiredArmies} full armies (have {readyArmies})", LogCategory.Military, __instance.kingdom);
                    __result = false;
                    return false;
                }

                // Block offensive if our best combat pair can't match the enemy's combined threat at the target
                float bestPairStr = 0;
                foreach (var a in __instance.kingdom.armies)
                {
                    if (a == null || !a.IsValid()) continue;
                    bestPairStr = Math.Max(bestPairStr, MilitaryHelper.GetCombatPairStrength(a, __instance.kingdom));
                }
                float enemyStr = MilitaryHelper.GetEnemyOffensiveStrength(__instance.kingdom, threat.realm?.castle);
                if (!MilitaryHelper.IsStrongerThan(bestPairStr, enemyStr, GameBalance.MinAttackStrengthRatio))
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocking offensive to {threat.realm?.name} — outmatched (best pair:{bestPairStr:F0} vs enemy:{enemyStr:F0})", LogCategory.Military, __instance.kingdom);
                    __result = false;
                    return false;
                }
            }
            // DEFENSIVE: Our territory is invaded - defend if we're stronger than the enemy
            else if (threat.level >= KingdomAI.Threat.Level.Invaded)
            {
                float ourStrength = threat.assigned.eval;
                float enemyStrength = threat.enemies_in.eval;

                // Allow defense if we have any assigned strength and are stronger than enemy
                if (ourStrength > 0 && ourStrength >= enemyStrength * k_PowerRatioToFight)
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Allowing defensive assignment to {threat.realm?.name}: our strength {ourStrength:F0} >= enemy {enemyStrength:F0}", LogCategory.Military, __instance.kingdom);
                    // Let vanilla handle the assignment
                    return true;
                }
            }

            return true;
        }

        static void Postfix(KingdomAI __instance, KingdomAI.Threat threat, bool __result)
        {
            // If assignment failed or not enhanced AI, do nothing
            if (!__result || __instance == null || __instance.kingdom == null) return;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;

            // Find which army was just assigned (it's the last one in the threat's assigned list)
            // Find which army was just assigned
            if (threat == null) return;
            // threat.assigned is a struct, cannot check for null directly. Check its internal list.
            if (threat.assigned.armies == null) return;

            // Iterate through assigned armies to find any leader that needs their buddy
            // We iterate a copy or index to avoid modification issues if we add to the list
            // But we are adding to the list, so we should be careful.
            // Actually, we are adding to threat.assigned, which modifies the list.
            
            var currentArmies = threat.assigned.armies.ToList(); // Safety copy
            
            foreach (var armyEval in currentArmies)
            {
                var army = armyEval.army;
                if (army == null) continue;
                
                if (army.tgt_realm == threat.realm)
                {
                    // This army is assigned to this threat
                    var buddy = BuddySystem.GetBuddy(army, __instance.kingdom);
                    if (buddy != null && buddy.IsValid())
                    {
                        // Check if buddy is also assigned
                        // Verify buddy isn't already in the threat list
                        bool buddyInThreat = false;
                         foreach(var check in threat.assigned.armies) {
                             if(check.army == buddy) { buddyInThreat = true; break; }
                         }

                        if (!buddyInThreat)
                        {
                            int buddyUnits = buddy.units?.Count ?? 0;
                            if (buddyUnits < GameBalance.MinBuddyUnitsToFollow)
                            {
                                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Not dragging weak Buddy {MilitaryHelper.DescribeArmy(buddy)} ({buddyUnits} < {GameBalance.MinBuddyUnitsToFollow} units)", LogCategory.Military, __instance.kingdom);
                            }
                            else
                            {
                                // FORCE ASSIGN BUDDY
                                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Leader {MilitaryHelper.DescribeArmy(army)} dragging Buddy {MilitaryHelper.DescribeArmy(buddy)} to {threat.realm.name}", LogCategory.Military, __instance.kingdom);

                                // Remove from old threat if any
                                if (buddy.tgt_realm != null)
                                {
                                    var oldThreat = __instance.GetThreat(buddy.tgt_realm);
                                    oldThreat?.assigned.Del(buddy);
                                }

                                buddy.tgt_realm = threat.realm;
                                threat.assigned.Add(buddy);
                            }
                        }
                    }
                }
            }
        }
    }
}
