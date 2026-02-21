using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "ThinkFight")]
    public class KingdomAI_ThinkFight
    {
        const string k_LogPrefix = "[ThinkFight]";

        struct RealmScan
        {
            public float OwnStrength;
            public float FriendStrength;
            public float EnemyTotal;
            public float EnemyNotInBattle;
            public float OwnTotal;
            public bool CanAttack;
            public Logic.Battle ClosestBattle;
            public bool HasOwnArmyInBattle;
            public Logic.Army ClosestEnemyArmy;
        }

        static bool Prefix(KingdomAI __instance, Logic.Army army, ref bool __result)
        {
            Logic.Realm realmIn = army?.realm_in;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance?.kingdom) || realmIn == null) { return true; }

            var kingdom = __instance.kingdom;

            // --- Guards ---
            if (TooSoonRetreat(army, __instance))                                    { __result = false; return false; }
            if (HandleFollowerBlock(army, kingdom))                                  { __result = false; return false; }

            // --- Scan realm (populates shared scan result) ---
            var scan = ScanRealm(__instance, army, realmIn, kingdom);

            // --- Overwhelmed: enemies too strong ---
            if (!scan.CanAttack && scan.EnemyTotal > 0)
            {
                __result = HandleOverwhelmed(__instance, army, realmIn, kingdom, scan);
                return false;
            }

            // --- Combat decisions (highest to lowest priority) ---
            if (HandleReinforce(__instance, army, scan))                              { __result = true; return false; }
            if (HandleDefendTeammate(__instance, army, realmIn, kingdom))             { __result = true; return false; }
            if (HandleAttackArmy(__instance, army, kingdom, scan))                    { __result = true; return false; }
            if (HandlePlunder(__instance, army, realmIn, kingdom))                    { __result = true; return false; }
            if (HandleSiegeCastle(__instance, army, realmIn, kingdom, scan))          { __result = true; return false; }

            // Nothing to do — let ThinkArmy handle fallback
            __result = false;
            return false;
        }

        // =====================================================================
        // Guards
        // =====================================================================

        static bool TooSoonRetreat(Logic.Army army, KingdomAI ai)
        {
            return army.last_retreat_time != Logic.Time.Zero && ai.game.time - army.last_retreat_time <= ai.def.retreat_fight_cooldown;
        }

        static bool HandleFollowerBlock(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (!BuddySystem.IsFollower(army, kingdom)) return false;

            var leader = BuddySystem.GetLeader(army, kingdom);
            if (leader == null || !leader.IsValid()) return false;

            if (MilitaryHelper.IsLeaderHeadingToFight(leader, kingdom))
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: follower blocked from fighting (leader {MilitaryHelper.DescribeArmy(leader)} heading to fight)", LogCategory.Military, kingdom);
                return true;
            }

            if (MilitaryHelper.IsEnemyTerritory(army.realm_in, kingdom))
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: follower blocked from offensive in enemy territory (leader idle)", LogCategory.Military, kingdom);
                return true;
            }

            return false;
        }

        // =====================================================================
        // Scanning
        // =====================================================================

        static bool ValidToFightRebel(KingdomAI ai, Logic.Army army, Logic.Army target)
        {
            if (target?.rebel?.rebellion == null) return true;
            if (ai.kingdom.rebellions.Contains(target.rebel.rebellion)) return true;
            for (int i = 0; i < ai.helpWithRebels.Count; i++)
                if (ai.helpWithRebels[i].Item1.rebellions.Contains(target.rebel.rebellion)) return true;
            return false;
        }

        static RealmScan ScanRealm(KingdomAI ai, Logic.Army army, Logic.Realm realmIn, Logic.Kingdom kingdom)
        {
            var scan = new RealmScan();
            if (realmIn.armies == null) return scan;

            float closestBattleDist = float.MaxValue;
            float closestEnemyDist = float.MaxValue;

            // --- Main pass: strengths + enemy battles + closest enemy army ---
            foreach (var a in realmIn.armies)
            {
                if (a == null) continue;
                int aStr = a.EvalStrength();

                if (a.kingdom_id == kingdom.id)
                {
                    if (a == army) scan.OwnStrength += aStr;
                    else scan.FriendStrength += aStr;
                    continue;
                }

                bool isEnemy = kingdom.IsEnemy(a.kingdom_id) || (NemesisTeamManager.IsNemesis(kingdom) && NemesisTeamManager.IsTeammateEnemy(a.kingdom_id, kingdom));
                if (!isEnemy || !ValidToFightRebel(ai, army, a)) continue;

                scan.EnemyTotal += aStr;
                if (a.battle != null)
                {
                    float dist = a.position.SqrDist(army.position);
                    if (dist < closestBattleDist) { scan.ClosestBattle = a.battle; closestBattleDist = dist; }
                }
                else
                {
                    scan.EnemyNotInBattle += aStr;
                    if (a.castle == null && !a.IsFleeing())
                    {
                        float dist = a.position.SqrDist(army.position);
                        if (dist < closestEnemyDist) { scan.ClosestEnemyArmy = a; closestEnemyDist = dist; }
                    }
                }
            }

            // --- Own-army battle fallback (if no battle found from enemies) ---
            if (scan.ClosestBattle == null)
            {
                foreach (var a in realmIn.armies)
                {
                    if (a == null || a.battle == null || a == army) continue;
                    if (a.kingdom_id == kingdom.id)
                    {
                        float dist = a.position.SqrDist(army.position);
                        if (dist < closestBattleDist) { scan.ClosestBattle = a.battle; closestBattleDist = dist; }
                    }
                }
            }

            // --- Nemesis teammate battle fallback ---
            if (scan.ClosestBattle == null && NemesisTeamManager.IsNemesis(kingdom))
            {
                foreach (var a in realmIn.armies)
                {
                    if (a == null || a.battle == null) continue;
                    Logic.Kingdom ak = a.GetKingdom();
                    if (ak != null && NemesisTeamManager.AreNemesisTeammates(kingdom, ak))
                    {
                        float dist = a.position.SqrDist(army.position);
                        if (dist < closestBattleDist)
                        {
                            NemesisTeamManager.LogVerbose($"Found teammate {ak.Name}'s battle in {realmIn.name} — will reinforce", kingdom);
                            scan.ClosestBattle = a.battle;
                            closestBattleDist = dist;
                        }
                    }
                }
            }

            // --- Check HasOwnArmyInBattle for the closest battle ---
            if (scan.ClosestBattle != null)
            {
                foreach (var a in realmIn.armies)
                {
                    if (a != null && a != army && a.kingdom_id == kingdom.id && a.battle == scan.ClosestBattle)
                    { scan.HasOwnArmyInBattle = true; break; }
                }
            }

            scan.OwnTotal = scan.OwnStrength + scan.FriendStrength;
            scan.CanAttack = MilitaryHelper.IsStrongerThan(scan.OwnTotal, scan.EnemyTotal, GameBalance.MinAttackStrengthRatio);
            return scan;
        }

        // =====================================================================
        // Handlers
        // =====================================================================

        static bool HandleOverwhelmed(KingdomAI ai, Logic.Army army, Logic.Realm realmIn, Logic.Kingdom kingdom, RealmScan scan)
        {
            string armyName = MilitaryHelper.DescribeArmy(army);
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: enemies too strong (own:{scan.OwnTotal:F0} vs enemy:{scan.EnemyTotal:F0})", LogCategory.Military, kingdom);

            // In own realm with castle available → retreat inside
            if (realmIn.kingdom_id == kingdom.id && army.castle == null)
            {
                Castle castle = realmIn.castle;
                if (castle != null && (castle.army == null || castle.army == army))
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: retreating into castle {castle.name}", LogCategory.Military, kingdom);
                    TraverseAPI.SendArmy(ai, army, castle, AIStatusNames.EnemiesTooStrong);
                    return true;
                }
            }

            // Battle with our army → reinforce desperately
            if (scan.ClosestBattle != null && scan.HasOwnArmyInBattle)
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: desperately reinforcing battle in {realmIn.name}", LogCategory.Military, kingdom);
                TraverseAPI.SendArmy(ai, army, scan.ClosestBattle, AIStatusNames.ReinforceDesperate);
                return true;
            }

            // In enemy territory → stop, wait for battle
            if (MilitaryHelper.IsEnemyTerritory(realmIn, kingdom))
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: stopping in enemy territory, waiting for battle", LogCategory.Military, kingdom);
                army.Stop();
                army.SetAIStatus(AIStatusNames.WaitForBattle);
            }

            return false;
        }

        static bool HandleReinforce(KingdomAI ai, Logic.Army army, RealmScan scan)
        {
            if (scan.ClosestBattle == null) return false;
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: reinforcing battle", LogCategory.Military, ai.kingdom);
            TraverseAPI.SendArmy(ai, army, scan.ClosestBattle, AIStatusNames.Reinforce);
            return true;
        }

        static bool HandleDefendTeammate(KingdomAI ai, Logic.Army army, Logic.Realm realmIn, Logic.Kingdom kingdom)
        {
            if (!NemesisTeamManager.IsNemesis(kingdom) || MilitaryHelper.IsEnemyTerritory(realmIn, kingdom)) return false;

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
                        if (a != null && teammate.IsEnemy(a.kingdom_id)) { hasEnemy = true; break; }
                    }
                    if (hasEnemy && tRealm.castle != null)
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: defending teammate {teammate.Name}'s realm {tRealm.name}", LogCategory.Military, kingdom);
                        TraverseAPI.SendArmy(ai, army, tRealm.castle, AIStatusNames.DefendTeammate);
                        return true;
                    }
                }
            }
            return false;
        }

        static bool HandleAttackArmy(KingdomAI ai, Logic.Army army, Logic.Kingdom kingdom, RealmScan scan)
        {
            if (scan.ClosestEnemyArmy == null) return false;
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: attacking {MilitaryHelper.DescribeArmy(scan.ClosestEnemyArmy)}", LogCategory.Military, kingdom);
            TraverseAPI.SendArmy(ai, army, scan.ClosestEnemyArmy, AIStatusNames.AttackArmy);
            return true;
        }

        static bool HandlePlunder(KingdomAI ai, Logic.Army army, Logic.Realm realmIn, Logic.Kingdom kingdom)
        {
            if (!MilitaryHelper.IsEnemyTerritory(realmIn, kingdom)) return false;

            float ourTop2 = MilitaryHelper.GetTop2ArmyStrength(kingdom);
            float enemyTop2 = MilitaryHelper.GetTop2ArmyStrength(realmIn.GetKingdom());
            if (MilitaryHelper.IsStrongerThan(ourTop2, enemyTop2, GameBalance.OverwhelmingStrengthRatio)) return false;

            var plunderTarget = MilitaryHelper.FindNearestPlunderableSettlement(army);
            if (plunderTarget == null) return false;

            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: pillaging {plunderTarget.def?.id} to lure defenders out (our top2:{ourTop2:F0} vs enemy top2:{enemyTop2:F0})", LogCategory.Military, kingdom);
            TraverseAPI.SendArmy(ai, army, plunderTarget, AIStatusNames.Plunder);
            return true;
        }

        static bool HandleSiegeCastle(KingdomAI ai, Logic.Army army, Logic.Realm realmIn, Logic.Kingdom kingdom, RealmScan scan)
        {
            if (!MilitaryHelper.IsEnemyTerritory(realmIn, kingdom)) return false;

            var castle = realmIn.castle;
            if (castle == null || castle.battle != null) return false;

            float castleDefense = castle.army?.EvalStrength() ?? 0;
            if (MilitaryHelper.IsStrongerThan(scan.OwnTotal, castleDefense, GameBalance.MinAttackStrengthRatio))
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: attacking castle {castle.name}", LogCategory.Military, kingdom);
                TraverseAPI.SendArmy(ai, army, castle, AIStatusNames.AttackCastle);
                return true;
            }

            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: castle {castle.name} too strong to siege (own:{scan.OwnTotal:F0} vs defense:{castleDefense:F0})", LogCategory.Military, kingdom);
            return false;
        }
    }
}
