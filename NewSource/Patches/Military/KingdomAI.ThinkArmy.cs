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

            var kingdom = __instance.kingdom;
            var realmIn = army.realm_in;

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
