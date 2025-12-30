using HarmonyLib;
using Logic;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AIOverhaul.Constants;
using AIOverhaul.Helpers;

namespace AIOverhaul
{
    public static class WarLogicHelper
    {
        /// <summary>
        /// Calculate average war score for a kingdom across all wars.
        /// Replacement for KingdomAI.GetAverageWarScore() which doesn't exist.
        /// Negative score = losing, positive = winning.
        /// </summary>
        public static float GetAverageWarScore(Logic.Kingdom k)
        {
            if (k == null || k.wars == null || k.wars.Count == 0) return 0f;

            float totalScore = 0f;
            int validWars = 0;

            foreach (var war in k.wars)
            {
                if (war == null) continue;

                try
                {
                    int side = TraverseAPI.GetWarSide(war, k);
                    float warScore = TraverseAPI.GetWarScore(war, side);
                    totalScore += warScore;
                    validWars++;
                }
                catch
                {
                    // Skip wars where we can't get score
                    continue;
                }
            }

            return validWars > 0 ? totalScore / validWars : 0f;
        }

        public static float GetTotalPower(Logic.Kingdom k)
        {
            if (k == null) return 0f;
            float total = 0f;

            if (k.realms != null)
            {
                foreach (var realm in k.realms)
                {
                    if (realm.armies != null)
                    {
                        foreach (var army in realm.armies)
                        {
                            if (army == null) continue;
                            total += army.EvalStrength();
                        }
                    }

                    if (realm.castle != null && k.ai != null)
                    {
                        total += KingdomAI.Threat.EvalCastleStrength(realm.castle);
                    }
                }
            }

            return total;
        }

        public static bool HasDisorder(Logic.Kingdom k)
        {
            if (k == null || k.realms == null) return false;

            foreach (var realm in k.realms)
            {
                if (realm != null && realm.IsDisorder())
                {
                    return true;
                }
            }

            return false;
        }

        public static float GetNeighborThreat(Logic.Kingdom k)
        {
            if (k == null || k.neighbors == null) return 0f;

            float totalThreat = 0f;

            foreach (var neighbor in k.neighbors)
            {
                if (neighbor is Logic.Kingdom neighborKingdom)
                {
                    if (neighborKingdom.IsDefeated()) continue;

                    // Consider enemies and those with bad relations as threats
                    if (k.IsEnemy(neighborKingdom))
                    {
                        totalThreat += GetTotalPower(neighborKingdom);
                    }
                }
            }

            return totalThreat;
        }

        public static bool HasHighThreat(Logic.Kingdom k)
        {
            if (k == null || k.realms == null) return false;

            // Iterate through all realms in the kingdom and check their threat level
            foreach (var realm in k.realms)
            {
                if (realm == null || realm.threat == null) continue;
                
                // Level 3 is Level.Attack, 4 is Invaded, 5 is Siege
                if ((int)realm.threat.level >= GameBalance.KingdomSideAttackLevel) 
                {
                    return true;
                }
            }

            return false;
        }

        public static bool WantsInvasionPlan(Logic.Kingdom k)
        {
            if (k == null) return false;

            // We want an invasion plan if we have a clear expansion target
            // and we are strong enough to consider attacking but would like allies.
            
            // Re-use existing SelectExpansionTarget logic
            Logic.Kingdom target = SelectExpansionTarget(k);
            if (target == null) return false;

            // If we are significantly stronger than the target, we might not need a plan
            float ownPower = GetTotalPower(k);
            float targetPower = GetTotalPower(target);
            
            if (ownPower > targetPower * GameBalance.PowerRatioSoloCapable) return false; // We can handle it alone

            // If we have at least one neighbor who is an ally or high relation, 
            // we might want a diplomat to coordinate.
            foreach (var neighbor in k.neighbors)
            {
                if (neighbor is Logic.Kingdom nk && nk != target && !nk.IsDefeated())
                {
                    if (k.IsAlly(nk) || k.GetRelationship(nk) > GameBalance.FriendlyRelationshipThreshold)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool WantsDiplomat(Logic.Kingdom k)
        {
            if (k == null) return false;

            // Diplomats are expensive luxury characters - only hire when we can afford it AND need diplomacy
            float goldIncome = k.income?.Get(ResourceType.Gold) ?? 0f;

            // Need solid income to afford a diplomat
            if (goldIncome < GameBalance.MinGoldIncomeForDiplomats)
            {
                if (k.Name == "England")
                {
                    AIOverhaulPlugin.LogMod($"[ENGLAND] BLOCKING diplomat: goldIncome too low ({goldIncome} < {GameBalance.MinGoldIncomeForDiplomats})", LogCategory.Diplomacy);
                }
                return false;
            }

            // Count neighbors that are stronger than us and threaten us
            float ownPower = GetTotalPower(k);
            int strongerThreats = 0;

            if (k.neighbors != null)
            {
                foreach (var neighbor in k.neighbors)
                {
                    if (neighbor is Logic.Kingdom nk && !nk.IsDefeated())
                    {
                        // Skip allies - they're not threats
                        if (k.IsAlly(nk)) continue;

                        float neighborPower = GetTotalPower(nk);

                        // Count as threat if they're stronger than us
                        if (neighborPower > ownPower * GameBalance.PowerRatioStrongerNeighbor)
                        {
                            strongerThreats++;
                        }
                    }
                }
            }

            if (k.Name == "England")
            {
                AIOverhaulPlugin.LogMod($"[ENGLAND] Diplomat check: goldIncome={goldIncome}, ownPower={ownPower}, strongerThreats={strongerThreats}", LogCategory.Diplomacy);
            }

            // Hire diplomat if we have enough stronger neighbors (need alliances/NAPs to secure flanks)
            if (strongerThreats >= GameBalance.MinStrongerThreatsForDiplomat)
            {
                if (k.Name == "England")
                {
                    AIOverhaulPlugin.LogMod($"[ENGLAND] ALLOWING diplomat: {strongerThreats} stronger neighbors threatening us", LogCategory.Diplomacy);
                }
                return true;
            }

            if (k.Name == "England")
            {
                AIOverhaulPlugin.LogMod($"[ENGLAND] BLOCKING diplomat: only {strongerThreats} stronger threats (need {GameBalance.MinStrongerThreatsForDiplomat}+)", LogCategory.Diplomacy);
            }
            return false;
        }

        public static bool WantsSpy(Logic.Kingdom k)
        {
            if (k == null) return false;

            // PREVENT EARLY HIRING: Spies are mid-game luxury characters
            int merchants = KingdomHelper.CountMerchants(k);

            // Must have economy established (at least 2 merchants)
            if (merchants < GameBalance.RequiredMerchantCount) return false;

            // Strategic Need: If we are at war, spies can be useful for destabilization
            if (k.wars != null && k.wars.Count > 0) return true;

            // If we have a lot of gold and nothing better to do? 
            // For now, let's keep it restricted to war or late-ish game
            if (k.resources?[ResourceType.Gold] > 10000f) return true;

            return false;
        }

        public static bool ShouldSeekDefensivePact(Logic.Kingdom k)
        {
            if (k == null) return false;

            // Don't seek pacts if we already have 2+ allies
            if (k.allies != null && k.allies.Count >= 2) return false;

            // Need sufficient gold for diplomacy (5000+ gold)
            float gold = k.resources?[ResourceType.Gold] ?? 0f;
            if (gold < 5000f) return false;

            // Check if we face significant neighbor threats
            float ownPower = GetTotalPower(k);
            float neighborThreat = GetNeighborThreat(k);

            // Seek pacts if neighbor threat > 0.75x our power
            return neighborThreat > ownPower * 0.75f;
        }

        public static Logic.Kingdom FindBestDefensivePactTarget(Logic.Kingdom k)
        {
            if (k == null || k.game == null) return null;

            Logic.Kingdom bestTarget = null;
            int mostCommonEnemies = 0;

            foreach (var potentialAlly in k.game.kingdoms)
            {
                if (potentialAlly == null || potentialAlly == k) continue;
                if (potentialAlly.IsDefeated()) continue;
                if (k.IsEnemy(potentialAlly)) continue;
                if (k.IsAlly(potentialAlly)) continue; // Already allied

                // Count common enemies
                int commonEnemies = 0;
                if (k.wars != null && potentialAlly.wars != null)
                {
                    foreach (var ourWar in k.wars)
                    {
                        Logic.Kingdom ourEnemy = ourWar.GetEnemyLeader(k);
                        if (potentialAlly.IsEnemy(ourEnemy))
                        {
                            commonEnemies++;
                        }
                    }
                }

                // Also consider if they're neighbors of our enemies
                if (k.wars != null)
                {
                    foreach (var ourWar in k.wars)
                    {
                        Logic.Kingdom ourEnemy = ourWar.GetEnemyLeader(k);
                        if (ourEnemy != null && IsStrategicNeighbor(potentialAlly, ourEnemy))
                        {
                            commonEnemies++;
                        }
                    }
                }

                if (commonEnemies > mostCommonEnemies)
                {
                    mostCommonEnemies = commonEnemies;
                    bestTarget = potentialAlly;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Select the best neighbor to designate as expansion target
        /// Strategy: MORTAL ENEMY first (never forgive), then current wars, then best target
        /// </summary>
        public static Logic.Kingdom SelectExpansionTarget(Logic.Kingdom k)
        {
            if (k == null || k.neighbors == null) return null;

            // PRIORITY 1: Mortal Enemy - the FIRST kingdom that declared war on us
            // This is a permanent grudge that overrides all other considerations
            Logic.Kingdom mortalEnemy = AIOverhaulPlugin.GetMortalEnemy(k, k.game);
            if (mortalEnemy != null)
            {
                // Only if they're still a neighbor (could have lost border provinces)
                bool isStillNeighbor = false;
                foreach (var neighbor in k.neighbors)
                {
                    if (neighbor is Logic.Kingdom nk && nk == mortalEnemy)
                    {
                        isStillNeighbor = true;
                        break;
                    }
                }

                if (isStillNeighbor)
                {
                    return mortalEnemy; // Revenge is priority #1
                }
            }

            // PRIORITY 2: If already at war with a neighbor, that's our expansion target
            foreach (var neighbor in k.neighbors)
            {
                if (neighbor is Logic.Kingdom nk && k.IsEnemy(nk) && !nk.IsDefeated())
                {
                    return nk;
                }
            }

            // Otherwise, select best expansion target from peaceful neighbors
            Logic.Kingdom bestTarget = null;
            float bestScore = -99999f;
            float ownPower = GetTotalPower(k);

            foreach (var neighbor in k.neighbors)
            {
                if (neighbor is Logic.Kingdom neighborKingdom)
                {
                    // Skip if defeated
                    if (neighborKingdom.IsDefeated()) continue;

                    // Skip if allied (don't betray allies)
                    if (k.IsAlly(neighborKingdom)) continue;

                    // Calculate expansion attractiveness score
                    float targetPower = GetTotalPower(neighborKingdom);
                    int targetRealms = neighborKingdom.realms?.Count ?? 0;
                    float relationship = k.GetRelationship(neighborKingdom);

                    // Prefer weaker neighbors (easier conquest)
                    float powerRatio = ownPower > 0 ? targetPower / ownPower : 1f;

                    // Score components:
                    // 1. Weakness: prefer 0.5-1.0 power ratio (winnable but not trivial)
                    float weaknessScore = (powerRatio >= 0.3f && powerRatio <= 1.0f) ? (1.0f - powerRatio) * 100f : -50f;

                    // 2. Strategic value: more realms = better
                    float valueScore = targetRealms * 10f;

                    // 3. Relationship: prefer enemies/hostile (already bad relations)
                    float relationScore = -relationship * 0.1f; // Negative relationship = positive score

                    float totalScore = weaknessScore + valueScore + relationScore;

                    if (totalScore > bestScore)
                    {
                        bestScore = totalScore;
                        bestTarget = neighborKingdom;
                    }
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Find the best neighbor to offer a non-aggression pact to
        /// EXCLUDES the designated expansion target - we want to keep one enemy neighbor
        /// </summary>
        public static Logic.Kingdom FindNonAggressionTarget(Logic.Kingdom k, Logic.Kingdom expansionTarget)
        {
            if (k == null || k.neighbors == null) return null;

            Logic.Kingdom bestTarget = null;
            float bestRelationship = -1000f;

            foreach (var neighbor in k.neighbors)
            {
                if (neighbor is Logic.Kingdom neighborKingdom)
                {
                    // Skip if defeated
                    if (neighborKingdom.IsDefeated()) continue;

                    // Skip if at war
                    if (k.IsEnemy(neighborKingdom)) continue;

                    // CRITICAL: Skip if this is our designated expansion target
                    // We want to keep this neighbor as potential enemy for expansion
                    if (neighborKingdom == expansionTarget) continue;

                    // Skip if already have non-aggression pact
                    if (k.HasStance(neighborKingdom, RelationUtils.Stance.NonAggression)) continue;

                    // Skip if already allied (better than NAP)
                    if (k.IsAlly(neighborKingdom)) continue;

                    // Get relationship value
                    float relationship = k.GetRelationship(neighborKingdom);

                    // Skip if relationship is too hostile (below "Reserved" threshold)
                    // Reserved = -200, so only offer to neighbors we have at least neutral-ish relations with
                    float reservedThreshold = RelationUtils.Def.GetLowerTreshold(RelationUtils.RelationshipType.Reserved);
                    if (relationship < reservedThreshold) continue;

                    // Prioritize neighbors with better relations (more likely to accept)
                    // Focus on neutral/sympathetic/trusting neighbors to build security buffer
                    if (relationship > bestRelationship)
                    {
                        bestRelationship = relationship;
                        bestTarget = neighborKingdom;
                    }
                }
            }

            return bestTarget;
        }

        public static bool IsStrategicNeighbor(Logic.Kingdom a, Logic.Kingdom b)
        {
            if (a == null || b == null) return false;
            if (a.neighbors == null) return false;
            foreach (var n in a.neighbors)
            {
                if (n is Logic.Kingdom k && k == b) return true;
            }

            return false;
        }

        public static bool HasCommonEnemyWithAlly(Logic.Kingdom a, Logic.Kingdom b)
        {
            if (a == null || b == null || a.wars == null || b.wars == null) return false;
            foreach (var warA in a.wars)
            {
                Logic.Kingdom enemyA = warA.GetEnemyLeader(a);
                foreach (var warB in b.wars)
                {
                    if (warB.GetEnemyLeader(b) == enemyA) return true;
                }
            }

            return false;
        }

        public static int GetTradeAgreementCount(Logic.Kingdom k)
        {
            if (k == null || k.game == null) return 0;
            
            // Iterate known kingdoms or just all active kingdoms to count trade agreements
            int count = 0;
            if (k.game.kingdoms != null)
            {
                foreach (var other in k.game.kingdoms)
                {
                    if (other != null && other != k && !other.IsDefeated())
                    {
                        if (k.HasTradeAgreement(other))
                            count++;
                    }
                }
            }
            return count;
        }
    }

    [HarmonyPatch(typeof(KingdomAI), "ThinkDeclareWar")]
    public class WarDeclarationPatch
    {
        static bool Prefix(KingdomAI __instance, Logic.Kingdom k, ref bool __result)
        {
            if (k == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // CRITICAL: Never declare war if we have disorder
            if (WarLogicHelper.HasDisorder(__instance.kingdom))
            {
                __result = false;
                return false;
            }

            if (__instance.kingdom.IsAlly(k))
            {
                __result = false;
                return false;
            }

            if (!WarLogicHelper.IsStrategicNeighbor(__instance.kingdom, k))
            {
                __result = false;
                return false;
            }

            float ownPower = WarLogicHelper.GetTotalPower(__instance.kingdom);
            float targetPower = WarLogicHelper.GetTotalPower(k);

            // Check neighbor threat - assess if declaring war leaves us vulnerable
            float neighborThreat = WarLogicHelper.GetNeighborThreat(__instance.kingdom);
            float combinedThreat = neighborThreat + targetPower;

            // If combined threats exceed our power by 2x, we're too vulnerable
            if (combinedThreat > ownPower * 2.0f)
            {
                __result = false;
                return false;
            }

            bool targetAtWar = k.wars != null && k.wars.Count > 0;
            bool commonEnemy = WarLogicHelper.HasCommonEnemyWithAlly(__instance.kingdom, k);

            // Improved opportunistic war logic with upper limit
            if (targetPower > ownPower * 1.5f)
            {
                // Don't attack if enemy is more than 2.5x stronger, even if distracted
                if (targetPower > ownPower * 2.5f)
                {
                    __result = false;
                    return false;
                }

                if (!targetAtWar && !commonEnemy)
                {
                    __result = false;
                    return false;
                }
            }

            // NEW: War Preparation - Require 2 Full Armies
            int fullArmies = 0;
            if (__instance.kingdom.armies != null)
            {
                foreach (var army in __instance.kingdom.armies)
                {
                    // Definition of "Full Army":
                    // 1. Has a leader (Marshal or General)
                    // 2. Has units (not just leader) - "Full" likely means combat ready.
                    // 3. Let's say at least 4 units (Commanders have max 5-8).
                    if (army.leader != null && army.units.Count >= 4 && army.battle == null)
                    {
                        fullArmies++;
                    }
                }
            }

            if (fullArmies < 2)
            {
                __result = false;
                return false;
            }

            float powerRatio = targetPower > 0 ? ownPower / targetPower : (ownPower > 0 ? 10f : 1f);
            AIOverhaulPlugin.LogMod($" AI {__instance.kingdom.Name} declaring war on {k.Name}. Power Ratio: {powerRatio:F2}", LogCategory.War);
            return true;
        }
    }

    [HarmonyPatch(typeof(KingdomAI), "ThinkDiplomacy")]
    public class SurvivalDiplomacyPatch
    {
        static bool Prefix(KingdomAI __instance, ref IEnumerator __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            Logic.Kingdom actor = __instance.kingdom;
            float score = WarLogicHelper.GetAverageWarScore(actor);

            // CRITICAL: If we have disorder and are at war, seek peace immediately
            if (WarLogicHelper.HasDisorder(actor) && actor.wars != null && actor.wars.Count > 0)
            {
                // Focus on making peace with ALL enemies
                if (actor.wars.Count > 0)
                {
                    Logic.Kingdom target = actor.wars[0].GetEnemyLeader(actor);
                    if (target != null)
                    {
                        __result = RunDiplomacyWithTarget(__instance, target);
                        return false;
                    }
                }
            }

            // NEW: Strategic expansion targeting - keep ONE enemy neighbor, NAP with all others
            // This creates focused expansion direction with secure flanks
            Logic.Kingdom expansionTarget = WarLogicHelper.SelectExpansionTarget(actor);

            // Offer non-aggression pacts to all neighbors EXCEPT the expansion target
            Logic.Kingdom napTarget = WarLogicHelper.FindNonAggressionTarget(actor, expansionTarget);
            if (napTarget != null)
            {
                float relationship = actor.GetRelationship(napTarget);

                // Check if expansion target is mortal enemy for logging
                Logic.Kingdom mortalEnemy = AIOverhaulPlugin.GetMortalEnemy(actor, actor.game);
                string targetInfo = "";
                if (expansionTarget != null)
                {
                    if (mortalEnemy != null && expansionTarget == mortalEnemy)
                    {
                        targetInfo = $" (MORTAL ENEMY: {expansionTarget.Name})";
                    }
                    else
                    {
                        targetInfo = $" (Expansion target: {expansionTarget.Name})";
                    }
                }

                // TRADE RUSH (Priority over NAP)
                // If we have few trade partners, try to sign trade agreements first
                int tradeCount = WarLogicHelper.GetTradeAgreementCount(actor);
                // User Request: Send to friends or "don't care" (neutral), but NOT enemies.
                if (tradeCount < 3 && relationship >= 0 && !actor.IsEnemy(napTarget) && napTarget != expansionTarget)
                {
                    // Check if we can afford a trade agreement (usually costs gold to establish route if not instant)
                    // But SignTrade offer validation handles cost.
                    // We just prefer Trade Agreement over NAP here if we need money/commerce.
                    
                    // Re-use napTarget as trade target if they are friendly enough
                    // But we must check if we already have trade with them
                    if (!actor.HasTradeAgreement(napTarget))
                    {
                        __result = RunTradeAgreementProposal(__instance, napTarget);
                        return false;
                    }
                }
                else
                {
                    targetInfo = " (No expansion target)";
                }

                __result = RunNonAggressionProposal(__instance, napTarget);
                return false;
            }

            // NEW: Defensive pact formation when facing threats
            if (WarLogicHelper.ShouldSeekDefensivePact(actor))
            {
                Logic.Kingdom pactTarget = WarLogicHelper.FindBestDefensivePactTarget(actor);
                if (pactTarget != null)
                {
                    __result = RunDefensivePactProposal(__instance, pactTarget);
                    return false;
                }
            }

            if (score < GameBalance.WarScorePeaceSeeking || actor.wars.Count >= GameBalance.MaxWarsCount)
            {
                Logic.Kingdom target = null;

                // Priority 1: Strongest enemy for peace
                if (score < GameBalance.WarScoreSurvival)
                {
                    float worst = 0;
                    foreach (var war in actor.wars)
                    {
                        int side = TraverseAPI.GetWarSide(war, actor);
                        float s = TraverseAPI.GetWarScore(war, side);
                        if (s < worst)
                        {
                            worst = s;
                            target = war.GetEnemyLeader(actor);
                        }
                    }
                }

                // Priority 2: Potential ally
                if (target == null && actor.allies.Count < 2)
                {
                    foreach (var k in actor.game.kingdoms)
                    {
                        if (k == null || k == actor || k.IsDefeated() || k.IsEnemy(actor) || k.IsAlly(actor)) continue;
                        if (WarLogicHelper.IsStrategicNeighbor(actor, k))
                        {
                            foreach (var war in actor.wars)
                                if (k.IsEnemy(war.GetEnemyLeader(actor)))
                                {
                                    target = k;
                                    break;
                                }
                        }

                        if (target != null) break;
                    }
                }

                if (target != null)
                {
                    AIOverhaulPlugin.LogMod($" {actor.Name} in survival mode. Focusing on {target.Name}", LogCategory.Diplomacy);
                    __result = RunDiplomacyWithTarget(__instance, target);
                    return false;
                }
            }

            return true;
        }

        static IEnumerator RunDiplomacyWithTarget(KingdomAI ai, Logic.Kingdom target)
        {
            yield return (object)CoopThread.Call("ThinkProposeOffer", TraverseAPI.ThinkProposeOfferThread(ai, target, "neutral"));
        }

        static IEnumerator RunDefensivePactProposal(KingdomAI ai, Logic.Kingdom target)
        {
            // Try to propose a defensive pact
            OfferHelper.TrySendOffer("OfferJoinInDefensivePact", ai, target);
            yield break;
        }

        static IEnumerator RunTradeAgreementProposal(KingdomAI ai, Logic.Kingdom target)
        {
            // Try to propose a Trade Agreement (SignTrade)
            OfferHelper.TrySendOffer("SignTrade", ai, target);
            yield break;
        }

        static IEnumerator RunNonAggressionProposal(KingdomAI ai, Logic.Kingdom target)
        {
            // Offer a FREE non-aggression pact (no gold demanded) to build good relations
            OfferHelper.TrySendOffer("SignNonAggression", ai, target);
            yield break;
        }
    }

    [HarmonyPatch(typeof(KingdomAI), "ThinkWhitePeace")]
    public class SurvivalPeacePatch
    {
        static bool Prefix(KingdomAI __instance, Logic.Kingdom k, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            Logic.Kingdom actor = __instance.kingdom;
            if (actor == null || k == null) return true;

            // Independence
            if (actor.sovereignState == k)
            {
                float myStr = WarLogicHelper.GetTotalPower(actor);
                float theirStr = WarLogicHelper.GetTotalPower(k);
                float kScore = WarLogicHelper.GetAverageWarScore(k);
                if (myStr > theirStr * GameBalance.PowerRatioStrongerEnemy ||
                    k.wars.Count > GameBalance.MaxWarsCount ||
                    kScore < GameBalance.WarScoreIndependence)
                {
                    if (OfferHelper.TrySendOffer("ClaimIndependence", __instance, k))
                    {
                        AIOverhaulPlugin.LogMod($" {actor.Name} claiming independence from {k.Name}", LogCategory.War);
                        __result = true;
                        return false;
                    }
                }
            }

            // Desperate Surrender
            if (actor.IsEnemy(k))
            {
                float score = WarLogicHelper.GetAverageWarScore(actor);
                if (score < GameBalance.WarScoreSurrender || (score < GameBalance.WarScoreDesperateIndependence && SurvivalLogic.IsDesperate(actor)))
                {
                    Offer peace = Offer.GetCachedOffer("PeaceOfferTribute", (Logic.Object)actor, (Logic.Object)k);
                    Offer vassal = Offer.GetCachedOffer("OfferVassalage", (Logic.Object)actor, (Logic.Object)k);
                    if (peace != null && vassal != null)
                    {
                        peace.args = new List<Value> { new Value(vassal) };
                        peace.AI = true;
                        if (peace.Validate() == "ok")
                        {
                            AIOverhaulPlugin.LogMod($" {actor.Name} SURRENDERING to {k.Name} as vassal!", LogCategory.War);
                            peace.Send();
                            if (k.is_player)
                            {
                                __instance.SetLastOfferTimeToKingdom(k, peace);
                                k.t_last_ai_offer_time = __instance.game.time;
                            }

                            __result = true;
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }

    public static class SurvivalLogic
    {
        public static bool IsDesperate(Logic.Kingdom k)
        {
            if (k == null || k.realms == null) return false;
            if (k.realms.Count == 0) return true;
            var armies = k?.armies ?? new List<Logic.Army>();
            if (armies.Count == 0) return true;
            float totalStr = 0;
            foreach (var a in armies) totalStr += a.EvalStrength();
            return totalStr < k.realms.Count * 250f;
        }
    }

    [HarmonyPatch(typeof(ProsAndCons), "Eval")]
    [HarmonyPatch(new System.Type[] { typeof(string) })]
    public static class TradeAcceptancePatch
    {
        static bool Prefix(ProsAndCons __instance, string threshold_name, ref float __result)
        {
            try
            {
                if (__instance.our_kingdom != null && !__instance.our_kingdom.is_player && AIOverhaulPlugin.IsEnhancedAI(__instance.our_kingdom))
                {
                    if (threshold_name == "accept")
                    {
                        if (__instance.def.id.EndsWith("SignTrade"))
                        {
                            Offer offer = __instance.offer;
                            if (offer != null && offer.from is Logic.Kingdom sender)
                            {
                                if (__instance.our_kingdom.IsEnemy(sender)) return true;

                                var expTargetField = typeof(KingdomAI).GetField("expansionTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                                Logic.Kingdom expTarget = null;
                                if (expTargetField != null)
                                {
                                    expTarget = expTargetField.GetValue(__instance.our_kingdom.ai) as Logic.Kingdom;
                                }

                                if (expTarget != null && sender == expTarget) return true;

                                __result = 1000f;
                                return false;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                AIOverhaulPlugin.LogMod($"Error in TradeAcceptancePatch: {ex}", LogCategory.General);
            }
            return true;
        }
    }
}
