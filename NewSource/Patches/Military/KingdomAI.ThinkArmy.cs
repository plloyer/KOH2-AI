using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "ThinkArmy")]
    public class KingdomAI_ThinkArmy
    {
        const string k_LogPrefix = "[ThinkArmy]";

        static void Postfix(KingdomAI __instance, Logic.Army army)
        {
            if (army == null || __instance == null || __instance.kingdom == null) return;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (army.battle != null || army.IsHiredMercenary()) return;
            if (army.IsFleeing()) return;

            var kingdom = __instance.kingdom;
            var realmIn = army.realm_in;

            // --- SIEGE DEFENSE RECALL ---
            // Recall offensive armies to defend own realms under siege
            bool isActiveFollower = BuddySystem.IsFollower(army, kingdom)
                && MilitaryHelper.IsLeaderHeadingToFight(BuddySystem.GetLeader(army, kingdom), kingdom);
            if (!isActiveFollower)
            {
                Logic.Realm bestSiegedRealm = null;
                float bestEnemyStr = 0f;

                foreach (var realm in kingdom.realms)
                {
                    if (realm?.castle?.battle == null) continue;
                    var battle = realm.castle.battle;
                    if (battle.type != Logic.Battle.Type.Siege) continue;

                    // Make sure the attacker is an enemy (not our own siege during rebellion)
                    var attacker = battle.attacker;
                    if (attacker == null || !kingdom.IsEnemy(attacker.kingdom_id)) continue;

                    // Only count top 2 armies per side (battle cap is 2v2)
                    float eTop1 = 0f, eTop2 = 0f;
                    float dTop1 = 0f, dTop2 = 0f;
                    if (realm.armies != null)
                    {
                        foreach (var a in realm.armies)
                        {
                            if (a == null) continue;
                            float s = a.EvalStrength();
                            if (kingdom.IsEnemy(a.kingdom_id))
                            {
                                if (s > eTop1) { eTop2 = eTop1; eTop1 = s; }
                                else if (s > eTop2) { eTop2 = s; }
                            }
                            else if (a.kingdom_id == kingdom.id)
                            {
                                if (s > dTop1) { dTop2 = dTop1; dTop1 = s; }
                                else if (s > dTop2) { dTop2 = s; }
                            }
                        }
                    }
                    float enemyStr = eTop1 + eTop2;
                    float currentDefense = dTop1 + dTop2;

                    // Skip if existing defenders can already handle it
                    if (MilitaryHelper.IsStrongerThan(currentDefense, enemyStr, GameBalance.MinAttackStrengthRatio))
                        continue;

                    // Pick the most urgent siege (strongest enemy)
                    if (enemyStr > bestEnemyStr)
                    {
                        bestEnemyStr = enemyStr;
                        bestSiegedRealm = realm;
                    }
                }

                if (bestSiegedRealm != null)
                {
                    string armyName = MilitaryHelper.DescribeArmy(army);

                    // Skip if already in or heading to the besieged realm
                    if (army.realm_in == bestSiegedRealm || army.tgt_realm == bestSiegedRealm)
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName} already heading to defend siege at {bestSiegedRealm.name}", LogCategory.Military, kingdom);
                    }
                    else
                    {
                        // Projected defense = top 2 of (existing defenders + recalled army + buddy)
                        float pTop1 = 0f, pTop2 = 0f;

                        // Existing own armies in the besieged realm
                        if (bestSiegedRealm.armies != null)
                        {
                            foreach (var a in bestSiegedRealm.armies)
                            {
                                if (a == null || a.kingdom_id != kingdom.id) continue;
                                float s = a.EvalStrength();
                                if (s > pTop1) { pTop2 = pTop1; pTop1 = s; }
                                else if (s > pTop2) { pTop2 = s; }
                            }
                        }

                        // Add recalled army
                        float armyStr = army.EvalStrength();
                        if (armyStr > pTop1) { pTop2 = pTop1; pTop1 = armyStr; }
                        else if (armyStr > pTop2) { pTop2 = armyStr; }

                        // Add buddy if available
                        var buddy = BuddySystem.GetBuddy(army, kingdom);
                        if (buddy != null && buddy.IsValid() && buddy.battle == null)
                        {
                            float bStr = buddy.EvalStrength();
                            if (bStr > pTop1) { pTop2 = pTop1; pTop1 = bStr; }
                            else if (bStr > pTop2) { pTop2 = bStr; }
                        }

                        float projected = pTop1 + pTop2;

                        if (MilitaryHelper.IsStrongerThan(projected, bestEnemyStr, GameBalance.SiegeRecallStrengthRatio))
                        {
                            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} RECALLING {armyName} to defend siege at {bestSiegedRealm.name} (top2 projected:{projected:F0} vs enemy:{bestEnemyStr:F0})", LogCategory.Military, kingdom);
                            TraverseAPI.SendArmy(__instance, army, bestSiegedRealm.castle, AIStatusNames.SiegeRecall);
                            return;
                        }
                        else
                        {
                            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName} cannot relieve siege at {bestSiegedRealm.name} (projected:{projected:F0} vs enemy:{bestEnemyStr:F0})", LogCategory.Military, kingdom);
                        }
                    }
                }
            }

            // --- RETREAT FROM STRONGER ENEMIES WHILE PLUNDERING ---
            if (realmIn != null && MilitaryHelper.IsEnemyTerritory(realmIn, kingdom))
            {
                float enemyStr = MilitaryHelper.GetRealmEnemyStrength(realmIn, kingdom);
                if (enemyStr > 0)
                {
                    float ownStr = army.EvalStrength();
                    if (!MilitaryHelper.IsStrongerThan(ownStr, enemyStr, GameBalance.MinAttackStrengthRatio))
                    {
                        Castle safeCastle = TraverseAPI.FindNearestOwnCastle(__instance, army, true);
                        if (safeCastle != null)
                        {
                            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: retreating from stronger enemies in {realmIn.name} (own:{ownStr:F0} vs enemy:{enemyStr:F0})", LogCategory.Military, kingdom);
                            TraverseAPI.SendArmy(__instance, army, safeCastle, AIStatusNames.EnemiesTooStrong);
                            return;
                        }
                    }
                }
            }

            // --- FOLLOWER FOLLOW LOGIC ---
            if (BuddySystem.IsFollower(army, kingdom))
            {
                var leader = BuddySystem.GetLeader(army, kingdom);
                if (leader == null || !leader.IsValid()) return;

                // If leader is NOT heading to fight, follower acts independently
                if (!MilitaryHelper.IsLeaderHeadingToFight(leader, kingdom)) return;

                string armyName = MilitaryHelper.DescribeArmy(army);

                // Check if follower has enough units
                int unitCount = army.units?.Count ?? 0;
                if (unitCount < GameBalance.MinBuddyUnitsToFollow)
                {
                    // Already heading to refill — don't recalculate target
                    if (army.ai_status == AIStatusNames.Refill && army.GetTarget() != null)
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: already refilling, keeping target {MilitaryHelper.DescribeTarget(army.GetTarget())}", LogCategory.Military, kingdom);
                        return;
                    }

                    // HIGH PRIORITY: follower needs to refill
                    // Try to take units from non-pair armies if in same castle
                    MilitaryHelper.RefillFromNonPairArmies(army, kingdom);

                    // Recheck after transfer
                    unitCount = army.units?.Count ?? 0;
                    if (unitCount < GameBalance.MinBuddyUnitsToFollow)
                    {
                        // Go to nearest castle to hire
                        Castle castle = TraverseAPI.FindNearestOwnCastle(__instance, army, true);
                        if (castle != null)
                        {
                            TraverseAPI.SendArmy(__instance, army, castle, AIStatusNames.Refill);
                            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: follower refilling ({unitCount} units < {GameBalance.MinBuddyUnitsToFollow})", LogCategory.Military, kingdom);
                        }
                        return;
                    }
                }

                // Follow leader's target
                var leaderTarget = leader.GetTarget();
                MapObject followTarget = leaderTarget as MapObject ?? leader;

                TraverseAPI.SendArmy(__instance, army, followTarget, AIStatusNames.FollowLeader);
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: following leader {MilitaryHelper.DescribeArmy(leader)} -> {MilitaryHelper.DescribeTarget(leaderTarget)}", LogCategory.Military, kingdom);
                return;
            }

            // --- LEADER: Refill when weak ---
            if (BuddySystem.IsLeader(army, kingdom))
            {
                string armyName = MilitaryHelper.DescribeArmy(army);
                int unitCount = army.units?.Count ?? 0;
                float healthPct = army.GetArmyHealthPercentage();
                bool needsRefill = unitCount < GameBalance.MinFullArmyUnits || healthPct < GameBalance.HealthRetreatThreshold;

                if (needsRefill)
                {
                    // Already heading to refill — don't recalculate target
                    if (army.ai_status == AIStatusNames.Refill && army.GetTarget() != null)
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Leader {armyName}: already refilling, keeping target {MilitaryHelper.DescribeTarget(army.GetTarget())}", LogCategory.Military, kingdom);
                        return;
                    }

                    Castle castle = TraverseAPI.FindNearestOwnCastle(__instance, army, true);
                    if (castle != null)
                    {
                        TraverseAPI.SendArmy(__instance, army, castle, AIStatusNames.Refill);
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Leader {armyName}: refilling ({unitCount} units, {healthPct:P0} health)", LogCategory.Military, kingdom);
                        return;
                    }
                }
            }
        }
    }
}
