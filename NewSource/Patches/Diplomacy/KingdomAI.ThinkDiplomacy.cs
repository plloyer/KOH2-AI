using System;
using System.Collections;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "ThinkDiplomacy" handles diplomatic actions like proposing alliances, pacts, or peace treaties.
    // Intent: SurvivalDiplomacyPatch
    [HarmonyPatch(typeof(KingdomAI), "ThinkDiplomacy")]
    public class KingdomAI_ThinkDiplomacy
    {
        static bool Prefix(KingdomAI __instance, ref IEnumerator __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            Logic.Kingdom actor = __instance.kingdom;
            float score = WarLogicHelper.GetAverageWarScore(actor);

            // CRITICAL: If we have disorder and are at war, seek peace immediately
            if (actor.HasDisorder() && actor.wars != null && actor.wars.Count > 0)
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
            Logic.Kingdom expansionTarget = actor.SelectExpansionTarget();

            // Offer non-aggression pacts to all neighbors EXCEPT the expansion target
            Logic.Kingdom napTarget = actor.FindNonAggressionTarget(expansionTarget);
            if (napTarget != null)
            {
                float relationship = actor.GetRelationship(napTarget);

                // Check if expansion target is mortal enemy for logging
                Logic.Kingdom mortalEnemy = AIOverhaulPlugin.GetMortalEnemy(actor, actor.game);

                // TRADE RUSH (Priority over NAP)
                // If we have few trade partners, try to sign trade agreements first
                int tradeCount = actor.GetTradeAgreementCount();
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

                __result = RunNonAggressionProposal(__instance, napTarget);
                return false;
            }

            // NEW: Defensive pact formation when facing threats
            if (actor.ShouldSeekDefensivePact())
            {
                Logic.Kingdom pactTarget = actor.FindBestDefensivePactTarget();
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
                        if (actor.IsStrategicNeighbor(k))
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
                    AIOverhaulPlugin.LogDebug($"In survival mode. Focusing on {target.Name}", LogCategory.Diplomacy, actor);
                    __result = RunDiplomacyWithTarget(__instance, target);
                    return false;
                }
            }

            return true;
        }

        static IEnumerator RunDiplomacyWithTarget(KingdomAI ai, Logic.Kingdom target)
        {
            yield return CoopThread.Call("ThinkProposeOffer", TraverseAPI.ThinkProposeOfferThread(ai, target, "neutral"));
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

        static void Postfix(KingdomAI __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;

            // Consider inviting neighbors to join our wars if we need help
            __instance.ConsiderInvitingNeighborsToWar();
        }
    }
}
