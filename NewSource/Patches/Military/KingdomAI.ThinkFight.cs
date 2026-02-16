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
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: follower blocked from fighting (leader {MilitaryHelper.DescribeArmy(leader)} heading to fight)", LogCategory.Military, kingdom);
                        __result = false;
                        return false;
                    }
                    // Leader idle — block offensive ops in enemy territory, allow defense in own territory
                    if (MilitaryHelper.IsEnemyTerritory(realmIn, kingdom))
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: follower blocked from offensive in enemy territory (leader idle)", LogCategory.Military, kingdom);
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
                    else if (kingdom.IsEnemy(a.kingdom_id) || (NemesisTeamManager.IsNemesis(kingdom) && NemesisTeamManager.IsTeammateEnemy(a.kingdom_id, kingdom)))
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

            // Nemesis: also check for battles involving nemesis teammates in this realm
            if (closestBattle == null && NemesisTeamManager.IsNemesis(kingdom) && realmIn.armies != null)
            {
                foreach (var a in realmIn.armies)
                {
                    if (a == null || a.battle == null) continue;
                    Logic.Kingdom armyKingdom = a.GetKingdom();
                    if (armyKingdom != null && NemesisTeamManager.AreNemesisTeammates(kingdom, armyKingdom))
                    {
                        float dist = a.position.SqrDist(army.position);
                        if (dist < closestBattleDist)
                        {
                            NemesisTeamManager.LogVerbose($"Found teammate {armyKingdom.Name}'s battle in {realmIn.name} — will reinforce", kingdom);
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
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: retreating into castle {castle.name}", LogCategory.Military, kingdom);
                        TraverseAPI.SendArmy(__instance, army, castle, AIStatusNames.EnemiesTooStrong);
                        __result = true;
                        return false;
                    }
                }

                // Battle exists with our ally -> reinforce desperately
                if (closestBattle != null)
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: desperately reinforcing battle in {realmIn.name}", LogCategory.Military, kingdom);
                    TraverseAPI.SendArmy(__instance, army, closestBattle, AIStatusNames.ReinforceDesperate);
                    __result = true;
                    return false;
                }

                // In enemy territory -> stop, wait
                if (MilitaryHelper.IsEnemyTerritory(realmIn, kingdom))
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: stopping in enemy territory, waiting for battle", LogCategory.Military, kingdom);
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

            // --- PRIORITY 2: Defend threatened teammate realm ---
            if (NemesisTeamManager.IsNemesis(kingdom) && !MilitaryHelper.IsEnemyTerritory(realmIn, kingdom))
            {
                foreach (int teammateId in NemesisTeamManager.GetTeammatesSortedByRealmCount(kingdom.game))
                {
                    if (teammateId == kingdom.id) continue;
                    var teammate = kingdom.game.GetKingdom(teammateId);
                    if (teammate?.realms == null) continue;

                    foreach (var tRealm in teammate.realms)
                    {
                        if (tRealm?.armies == null) continue;
                        bool hasEnemy = false;
                        foreach (var a in tRealm.armies)
                        {
                            if (a != null && teammate.IsEnemy(a.kingdom_id))
                            { hasEnemy = true; break; }
                        }
                        if (hasEnemy && tRealm.castle != null)
                        {
                            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: defending teammate {teammate.Name}'s realm {tRealm.name}", LogCategory.Military, kingdom);
                            TraverseAPI.SendArmy(__instance, army, tRealm.castle, AIStatusNames.DefendTeammate);
                            __result = true;
                            return false;
                        }
                    }
                }
            }

            // --- PRIORITY 3: Attack enemy army ---
            if (closestEnemyArmy != null)
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: attacking {MilitaryHelper.DescribeArmy(closestEnemyArmy)}", LogCategory.Military, kingdom);
                TraverseAPI.SendArmy(__instance, army, closestEnemyArmy, AIStatusNames.AttackArmy);
                __result = true;
                return false;
            }

            // --- PRIORITY 4: Plunder settlements if not overwhelming ---
            if (MilitaryHelper.IsEnemyTerritory(realmIn, kingdom))
            {
                float ourTop2 = MilitaryHelper.GetTop2ArmyStrength(kingdom);
                float enemyTop2 = MilitaryHelper.GetTop2ArmyStrength(realmIn.GetKingdom());
                bool overwhelming = MilitaryHelper.IsStrongerThan(ourTop2, enemyTop2, GameBalance.OverwhelmingStrengthRatio);

                if (!overwhelming)
                {
                    var plunderTarget = MilitaryHelper.FindNearestPlunderableSettlement(army);
                    if (plunderTarget != null)
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: pillaging {plunderTarget.def?.id} to lure defenders out (our top2:{ourTop2:F0} vs enemy top2:{enemyTop2:F0})", LogCategory.Military, kingdom);
                        TraverseAPI.SendArmy(__instance, army, plunderTarget, AIStatusNames.Plunder);
                        __result = true;
                        return false;
                    }
                }
            }

            // --- PRIORITY 5: Attack castle ---
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
                    else
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: castle {castle.name} too strong to siege (own:{ownTotal:F0} vs defense:{castleDefense:F0})", LogCategory.Military, kingdom);
                    }
                }
            }
            
            // Return true to let vanilla ThinkFight handle remaining logic (plunder, etc.)
            return true;
        }
    }
}
