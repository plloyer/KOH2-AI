using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "ThinkFight")]
    public class KingdomAI_ThinkFight
    {
        const string k_LogPrefix = "[ThinkFight]";

        static bool Prefix(KingdomAI __instance, Logic.Army army, ref bool __result)
        {
            Logic.Realm realmIn = army?.realm_in;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance?.kingdom) || realmIn == null) return true;

            var kingdom = __instance.kingdom;

            // Followers: block offensive fighting, allow defensive when leader is idle
            if (BuddySystem.IsFollower(army, kingdom))
            {
                var leader = BuddySystem.GetLeader(army, kingdom);
                if (leader != null && leader.IsValid())
                {
                    // Leader heading to fight — follower must follow, not fight alone
                    if (MilitaryHelper.IsLeaderHeadingToFight(leader, kingdom))
                    {
                        __result = false;
                        return false;
                    }
                    // Leader idle — block offensive ops in enemy territory, allow defense in own territory
                    if (MilitaryHelper.IsEnemyTerritory(realmIn, kingdom))
                    {
                        __result = false;
                        return false;
                    }
                }
            }

            // Scan realm: mirror vanilla structure
            float ownStrength = 0;
            float friendStrength = 0;
            float enemyTotal = 0;
            float enemyNotInBattle = 0;
            Logic.Battle closestBattle = null;
            float closestBattleDist = float.MaxValue;
            Logic.Army closestEnemyArmy = null;
            float closestEnemyDist = float.MaxValue;

            if (realmIn.armies != null)
            {
                foreach (var a in realmIn.armies)
                {
                    if (a == null) continue;
                    int aStr = a.EvalStrength();

                    if (a.kingdom_id == kingdom.id)
                    {
                        if (a == army)
                            ownStrength += aStr;
                        else
                            friendStrength += aStr;
                    }
                    else if (kingdom.IsEnemy(a.kingdom_id))
                    {
                        enemyTotal += aStr;

                        if (a.battle != null)
                        {
                            // Track closest battle to reinforce
                            float dist = a.position.SqrDist(army.position);
                            if (dist < closestBattleDist)
                            {
                                closestBattle = a.battle;
                                closestBattleDist = dist;
                            }
                        }
                        else
                        {
                            enemyNotInBattle += aStr;
                            float dist = a.position.SqrDist(army.position);
                            if (dist < closestEnemyDist)
                            {
                                closestEnemyArmy = a;
                                closestEnemyDist = dist;
                            }
                        }
                    }
                }
            }

            // Also check for battles involving our allies in this realm
            if (closestBattle == null && realmIn.armies != null)
            {
                foreach (var a in realmIn.armies)
                {
                    if (a == null || a.battle == null) continue;
                    if (a.kingdom_id == kingdom.id && a != army)
                    {
                        float dist = a.position.SqrDist(army.position);
                        if (dist < closestBattleDist)
                        {
                            closestBattle = a.battle;
                            closestBattleDist = dist;
                        }
                    }
                }
            }

            float ownTotal = ownStrength + friendStrength;
            bool canAttack = MilitaryHelper.IsStrongerThan(ownTotal, enemyTotal, GameBalance.MinAttackStrengthRatio);
            string armyName = MilitaryHelper.DescribeArmy(army);

            // --- ENEMIES TOO STRONG ---
            if (!canAttack && enemyTotal > 0)
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: enemies too strong (own:{ownTotal:F0} vs enemy:{enemyTotal:F0})", LogCategory.Military, kingdom);

                // In own realm with castle available -> retreat inside
                if (realmIn.kingdom_id == kingdom.id && army.castle == null)
                {
                    Castle castle = realmIn.castle;
                    if (castle != null && (castle.army == null || castle.army == army))
                    {
                        TraverseAPI.SendArmy(__instance, army, castle, AIStatusNames.EnemiesTooStrong);
                        __result = true;
                        return false;
                    }
                }

                // Battle exists with our ally -> reinforce desperately
                if (closestBattle != null)
                {
                    TraverseAPI.SendArmy(__instance, army, closestBattle, AIStatusNames.ReinforceDesperate);
                    __result = true;
                    return false;
                }

                // In enemy territory -> stop, wait
                if (MilitaryHelper.IsEnemyTerritory(realmIn, kingdom))
                {
                    army.Stop();
                    army.SetAIStatus(AIStatusNames.WaitForBattle);
                    __result = false;
                    return false;
                }

                __result = false;
                return false;
            }

            // --- PRIORITY 1: Join existing battle ---
            if (closestBattle != null)
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: reinforcing battle", LogCategory.Military, kingdom);
                TraverseAPI.SendArmy(__instance, army, closestBattle, AIStatusNames.Reinforce);
                __result = true;
                return false;
            }

            // --- PRIORITY 2: Attack enemy army ---
            if (closestEnemyArmy != null)
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: attacking {MilitaryHelper.DescribeArmy(closestEnemyArmy)}", LogCategory.Military, kingdom);
                TraverseAPI.SendArmy(__instance, army, closestEnemyArmy, AIStatusNames.AttackArmy);
                __result = true;
                return false;
            }

            // --- PRIORITY 3: Attack castle ---
            if (MilitaryHelper.IsEnemyTerritory(realmIn, kingdom))
            {
                var castle = realmIn.castle;
                if (castle != null && castle.battle == null)
                {
                    // Check if we can siege (strong enough)
                    float castleDefense = castle.army?.EvalStrength() ?? 0;
                    if (MilitaryHelper.IsStrongerThan(ownTotal, castleDefense, GameBalance.MinAttackStrengthRatio))
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: attacking castle {castle.name}", LogCategory.Military, kingdom);
                        TraverseAPI.SendArmy(__instance, army, castle, AIStatusNames.AttackCastle);
                        __result = true;
                        return false;
                    }
                }
            }

            // --- PRIORITY 4: Plunder (delegate to vanilla ThinkPlunder which is already patched) ---
            // Let vanilla handle by returning true from here, or fall through
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: no targets found in {realmIn.name}, falling through", LogCategory.Military, kingdom);

            // Return true to let vanilla ThinkFight handle remaining logic (plunder, etc.)
            return true;
        }
    }
}
