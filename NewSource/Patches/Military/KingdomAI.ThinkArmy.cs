using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "ThinkArmy")]
    public class KingdomAI_ThinkArmy
    {
        const string k_LogPrefix = "[ThinkArmy]";

        static bool Prefix(KingdomAI __instance, Logic.Army army)
        {
            if (army == null || __instance?.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Enhanced AI: skip vanilla entirely, run our logic
            var kingdom = __instance.kingdom;
            var realmIn = army.realm_in;

            if (realmIn == null || army.IsHiredMercenary()) return false;

            if (army.battle != null) { HandleInBattle(__instance, army); return false; }
            if (army.IsFleeing()) return false;

            // --- Safety overrides (highest priority) ---
            if (HandleSiegeDefenseRecall(__instance, army, kingdom)) return false;
            if (HandleRetreatFromEnemyTerritory(__instance, army, realmIn, kingdom)) return false;
            if (HandleAbortOffensiveOutmatched(__instance, army, kingdom)) return false;

            // --- Strategic movement ---
            if (HandleTargetRealmMovement(__instance, army, kingdom)) return false;

            // --- Combat ---
            if (HandleCombat(__instance, army)) return false;

            // --- Logistics ---
            HandleResupplyInCastle(army);
            if (HandleHireMercenaries(__instance, army)) return false;
            if (HandleRefill(__instance, army, kingdom)) return false;

            // --- Follower follow ---
            if (HandleFollowerFollow(__instance, army, kingdom)) return false;

            // --- Rebels ---
            if (HandleHelpWithRebels(__instance, army)) return false;

            // --- Fallback ---
            HandleGoHomeOrIdle(__instance, army, kingdom);
            return false;
        }

        // =====================================================================
        // Handler functions
        // =====================================================================

        static void HandleInBattle(KingdomAI ai, Logic.Army army)
        {
            // Vanilla in-battle dispatch: ThinkRetreat → ThinkBreakSiege → ThinkAssaultSiege
            ai.ThinkRetreat(army);
            ai.ThinkBreakSiege(army);
            KingdomAI.ThinkAssaultSiege(army);
        }

        static bool HandleSiegeDefenseRecall(KingdomAI ai, Logic.Army army, Logic.Kingdom kingdom)
        {
            bool isActiveFollower = BuddySystem.IsFollower(army, kingdom)
                && MilitaryHelper.IsLeaderHeadingToFight(BuddySystem.GetLeader(army, kingdom), kingdom);
            if (isActiveFollower) return false;

            Logic.Realm bestSiegedRealm = null;
            float bestEnemyStr = 0f;

            foreach (var realm in kingdom.realms)
            {
                if (realm?.castle?.battle == null) continue;
                var battle = realm.castle.battle;
                if (battle.type != Logic.Battle.Type.Siege) continue;

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

                if (MilitaryHelper.IsStrongerThan(currentDefense, enemyStr, GameBalance.MinAttackStrengthRatio))
                    continue;

                if (enemyStr > bestEnemyStr)
                {
                    bestEnemyStr = enemyStr;
                    bestSiegedRealm = realm;
                }
            }

            if (bestSiegedRealm == null) return false;

            // Skip if already in or heading to the besieged realm
            if (army.realm_in == bestSiegedRealm || army.tgt_realm == bestSiegedRealm) return false;

            // Projected defense = top 2 of (existing defenders + recalled army + buddy)
            float pTop1 = 0f, pTop2 = 0f;

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

            float armyStr = army.EvalStrength();
            if (armyStr > pTop1) { pTop2 = pTop1; pTop1 = armyStr; }
            else if (armyStr > pTop2) { pTop2 = armyStr; }

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
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: siege recall to {bestSiegedRealm.name} (projected:{projected:F0} vs enemy:{bestEnemyStr:F0})", LogCategory.Military, kingdom);
                TraverseAPI.SendArmy(ai, army, bestSiegedRealm.castle, AIStatusNames.SiegeRecall);
                return true;
            }

            return false;
        }

        static bool HandleRetreatFromEnemyTerritory(KingdomAI ai, Logic.Army army, Logic.Realm realmIn, Logic.Kingdom kingdom)
        {
            if (realmIn == null || !MilitaryHelper.IsEnemyTerritory(realmIn, kingdom)) return false;

            float enemyStr = MilitaryHelper.GetRealmEnemyStrength(realmIn, kingdom);
            if (enemyStr <= 0) return false;

            float ownStr = army.EvalStrength();
            if (MilitaryHelper.IsStrongerThan(ownStr, enemyStr, GameBalance.MinAttackStrengthRatio)) return false;

            Castle safeCastle = TraverseAPI.FindNearestOwnCastle(ai, army, true);
            if (safeCastle == null) return false;

            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: retreating from {realmIn.name} (own:{ownStr:F0} vs enemy:{enemyStr:F0})", LogCategory.Military, kingdom);
            TraverseAPI.SendArmy(ai, army, safeCastle, AIStatusNames.EnemiesTooStrong);
            return true;
        }

        static bool HandleAbortOffensiveOutmatched(KingdomAI ai, Logic.Army army, Logic.Kingdom kingdom)
        {
            if (BuddySystem.IsFollower(army, kingdom)) return false;
            if (army.tgt_realm == null || !MilitaryHelper.IsEnemyTerritory(army.tgt_realm, kingdom)) return false;

            float ourStr = MilitaryHelper.GetCombatPairStrength(army, kingdom);
            float enemyStr = MilitaryHelper.GetEnemyOffensiveStrength(kingdom, army.tgt_realm?.castle);
            if (MilitaryHelper.IsStrongerThan(ourStr, enemyStr, GameBalance.MinAttackStrengthRatio)) return false;

            Castle safeCastle = TraverseAPI.FindNearestOwnCastle(ai, army, true);
            if (safeCastle == null) return false;

            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: aborting offensive — outmatched (pair:{ourStr:F0} vs enemy:{enemyStr:F0})", LogCategory.Military, kingdom);
            TraverseAPI.SendArmy(ai, army, safeCastle, AIStatusNames.EnemiesTooStrong);
            return true;
        }

        static bool HandleTargetRealmMovement(KingdomAI ai, Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army.tgt_realm == null) return false;

            // Path correction: if tgt_realm changed owner or is no longer enemy, clear it
            if (!MilitaryHelper.IsEnemyTerritory(army.tgt_realm, kingdom))
            {
                army.tgt_realm = null;
                return false;
            }

            // ShouldWait: stagger army arrivals at borders
            if (ai.ShouldWait(army))
            {
                army.Stop();
                army.SetAIStatus(AIStatusNames.WaitOthers);
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: waiting for allies at border", LogCategory.Military, kingdom);
                return true;
            }

            // Still traveling to tgt_realm — let the army continue
            if (army.GetTarget() != null) return true;

            // Not moving yet — send to tgt_realm castle
            var castle = army.tgt_realm.castle;
            if (castle != null)
            {
                TraverseAPI.SendArmy(ai, army, castle, AIStatusNames.AttackRealm);
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: marching to {army.tgt_realm.name}", LogCategory.Military, kingdom);
                return true;
            }

            return false;
        }

        static bool HandleCombat(KingdomAI ai, Logic.Army army)
        {
            // Gate: weak armies skip combat, go to refill instead
            if (KingdomAI.IsLow(army)) return false;

            // ThinkFight handles scanning the realm for enemies, reinforcing battles, attacking castles, etc.
            return ai.ThinkFight(army);
        }

        static void HandleResupplyInCastle(Logic.Army army)
        {
            if (army.castle == null) return;

            // In a castle — resupply (mirrors vanilla: unconditional when garrisoned)
            army.castle.ResupplyArmy(army);
            army.SetAIStatus(AIStatusNames.Resupplied);
        }

        static bool HandleHireMercenaries(KingdomAI ai, Logic.Army army)
        {
            return ai.ConsiderHireMercenaries(army);
        }

        static bool HandleRefill(KingdomAI ai, Logic.Army army, Logic.Kingdom kingdom)
        {
            // --- Buddy follower refill ---
            if (BuddySystem.IsFollower(army, kingdom))
            {
                int unitCount = army.units?.Count ?? 0;
                if (unitCount < GameBalance.MinBuddyUnitsToFollow)
                {
                    if (army.ai_status == AIStatusNames.Refill && army.GetTarget() != null) return true;

                    MilitaryHelper.RefillFromNonPairArmies(army, kingdom);
                    unitCount = army.units?.Count ?? 0;
                    if (unitCount < GameBalance.MinBuddyUnitsToFollow)
                    {
                        Castle castle = MilitaryHelper.FindNearestCastleWithBarracks(army, kingdom) ?? TraverseAPI.FindNearestOwnCastle(ai, army, true);
                        if (castle != null)
                        {
                            TraverseAPI.SendArmy(ai, army, castle, AIStatusNames.Refill);
                            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: follower refilling ({unitCount} units < {GameBalance.MinBuddyUnitsToFollow})", LogCategory.Military, kingdom);
                        }
                        return true;
                    }
                }
                return false;
            }

            // --- Buddy leader refill ---
            if (BuddySystem.IsLeader(army, kingdom))
            {
                int unitCount = army.units?.Count ?? 0;
                float healthPct = army.GetArmyHealthPercentage();
                bool needsRefill = unitCount < GameBalance.MinFullArmyUnits || healthPct < GameBalance.HealthRetreatThreshold;
                if (needsRefill)
                {
                    if (army.ai_status == AIStatusNames.Refill && army.GetTarget() != null) return true;

                    Castle castle = MilitaryHelper.FindNearestCastleWithBarracks(army, kingdom) ?? TraverseAPI.FindNearestOwnCastle(ai, army, true);
                    if (castle != null)
                    {
                        TraverseAPI.SendArmy(ai, army, castle, AIStatusNames.Refill);
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: leader refilling ({unitCount} units, {healthPct:P0} health)", LogCategory.Military, kingdom);
                    }
                    return true;
                }
                return false;
            }

            // --- Non-buddy refill (vanilla IsLow + militia check) ---
            if (KingdomAI.IsLow(army))
            {
                if (army.ai_status == AIStatusNames.Refill && army.GetTarget() != null) return true;

                Castle castle = ai.DecideOwnCastleForArmy(army.leader) ?? TraverseAPI.FindNearestOwnCastle(ai, army, true);
                if (castle != null)
                {
                    TraverseAPI.SendArmy(ai, army, castle, AIStatusNames.Refill);
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: refilling (IsLow)", LogCategory.Military, kingdom);
                }
                return true;
            }

            return false;
        }

        static bool HandleFollowerFollow(KingdomAI ai, Logic.Army army, Logic.Kingdom kingdom)
        {
            if (!BuddySystem.IsFollower(army, kingdom)) return false;

            var leader = BuddySystem.GetLeader(army, kingdom);
            if (leader == null || !leader.IsValid()) return false;
            if (!MilitaryHelper.IsLeaderHeadingToFight(leader, kingdom)) return false;

            var leaderTarget = leader.GetTarget();
            MapObject followTarget = leaderTarget as MapObject ?? leader;

            TraverseAPI.SendArmy(ai, army, followTarget, AIStatusNames.FollowLeader);
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: following leader {MilitaryHelper.DescribeArmy(leader)} -> {MilitaryHelper.DescribeTarget(leaderTarget)}", LogCategory.Military, kingdom);
            return true;
        }

        static bool HandleHelpWithRebels(KingdomAI ai, Logic.Army army)
        {
            return ai.ThinkHelpWithRebels(army);
        }

        static void HandleGoHomeOrIdle(KingdomAI ai, Logic.Army army, Logic.Kingdom kingdom)
        {
            // Already in a castle — idle
            if (army.castle != null)
            {
                army.SetAIStatus(AIStatusNames.Idle);
                return;
            }

            // Find nearest own castle and go there
            Castle homeCastle = TraverseAPI.FindNearestOwnCastle(ai, army, true);
            if (homeCastle != null)
            {
                // If castle has room, go inside; otherwise just go home
                string status = (homeCastle.army == null || homeCastle.army == army) ? AIStatusNames.GoInside : AIStatusNames.GoHome;
                TraverseAPI.SendArmy(ai, army, homeCastle, status);
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(army)}: going home to {homeCastle.name}", LogCategory.Military, kingdom);
                return;
            }

            army.SetAIStatus(AIStatusNames.Idle);
        }
    }
}
