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
            if (!BuddySystem.IsFollower(army, kingdom))
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

                    float enemyStr = attacker.EvalStrength();
                    float currentDefense = MilitaryHelper.GetRealmOwnStrength(realm, kingdom);

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
                        float currentDefense = MilitaryHelper.GetRealmOwnStrength(bestSiegedRealm, kingdom);
                        float armyStr = army.EvalStrength();

                        // Include buddy strength if buddy exists and is not in battle
                        float buddyStr = 0f;
                        var buddy = BuddySystem.GetBuddy(army, kingdom);
                        if (buddy != null && buddy.IsValid() && buddy.battle == null)
                            buddyStr = buddy.EvalStrength();

                        float projected = currentDefense + armyStr + buddyStr;

                        if (MilitaryHelper.IsStrongerThan(projected, bestEnemyStr, GameBalance.SiegeRecallStrengthRatio))
                        {
                            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} RECALLING {armyName} to defend siege at {bestSiegedRealm.name} (projected:{projected:F0} vs enemy:{bestEnemyStr:F0}, current defense:{currentDefense:F0})", LogCategory.Military, kingdom);
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

            // --- LEADER: Log status for debugging ---
            if (BuddySystem.IsLeader(army, kingdom))
            {
                var follower = BuddySystem.GetBuddy(army, kingdom);
                string armyName = MilitaryHelper.DescribeArmy(army);
                var target = army.GetTarget();
                string followerInfo = follower != null ? MilitaryHelper.DescribeArmy(follower) : "none";

                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Leader {armyName}: status={army.ai_status}, target={MilitaryHelper.DescribeTarget(target)}, follower={followerInfo}", LogCategory.Military, kingdom);
            }
        }
    }
}
