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
            float score = actor.GetAverageWarScore();

            // Nemesis team: highest priority — form pacts with teammates before anything else
            if (NemesisTeamManager.IsNemesis(actor))
            {
                string pactType;
                Logic.Kingdom teammate = NemesisTeamManager.FindTeammateNeedingPact(actor, actor.game, out pactType);
                if (teammate != null)
                {
                    AIOverhaulPlugin.LogDebug($"NEMESIS: Proposing {pactType} to teammate {teammate.Name}", LogCategory.Nemesis, actor);
                    if (pactType == DiplomacyConstants.SignTrade)
                    {
                        __result = __instance.RunTradeAgreementProposal(teammate);
                        return false;
                    }
                    if (pactType == DiplomacyConstants.SignNonAggression)
                    {
                        __result = __instance.RunNonAggressionProposal(teammate);
                        return false;
                    }
                    if (pactType == DiplomacyConstants.OfferJoinInDefensivePact)
                    {
                        __result = __instance.RunDefensivePactProposal(teammate);
                        return false;
                    }
                }
            }

            // Nemesis: force-sync all teammates into each other's wars
            if (NemesisTeamManager.IsNemesis(actor))
                NemesisTeamManager.SyncWars(actor.game);

            // EMERGENCY: Realm being assaulted — seek peace immediately
            Logic.Kingdom assaultAttacker = actor.FindAssaultAttacker();
            if (assaultAttacker != null && actor.IsEnemy(assaultAttacker))
            {
                AIOverhaulPlugin.LogDebug($"EMERGENCY: Realm under assault by {assaultAttacker.Name}! Seeking peace.", LogCategory.Diplomacy, actor);
                __result = __instance.RunDiplomacyWithTarget(assaultAttacker);
                return false;
            }

            // If we have disorder and are at war, seek peace immediately
            if (actor.HasDisorder() && actor.wars.Count > 0)
            {
                // Focus on making peace with ALL enemies
                if (actor.wars.Count > 0)
                {
                    Logic.Kingdom target = actor.wars[0].GetEnemyLeader(actor);
                    if (target != null)
                    {
                        __result = __instance.RunDiplomacyWithTarget(target);
                        return false;
                    }
                }
            }

            // Strategic expansion targeting - keep ONE enemy neighbor, NAP with all others
            // This creates focused expansion direction with secure flanks
            Logic.Kingdom expansionTarget = actor.SelectExpansionTarget();

            // Nemesis: try team-aligned trade target first (converge on same partners)
            if (NemesisTeamManager.IsNemesis(actor))
            {
                Logic.Kingdom alignedTarget = NemesisTeamManager.FindTeamAlignedTradeTarget(actor, actor.game);
                if (alignedTarget != null)
                {
                    AIOverhaulPlugin.LogDebug($"NEMESIS: Aligned trade with {alignedTarget.Name} (teammate already has trade)", LogCategory.Nemesis, actor);
                    __result = __instance.RunTradeAgreementProposal(alignedTarget);
                    return false;
                }
            }

            // Proactively seek trade agreements when below minimum (prioritize over NAPs for early commerce)
            int tradeCount = actor.GetTradeAgreementCount();
            if (tradeCount < GameBalance.MinTradeAgreements)
            {
                Logic.Kingdom tradeTarget = actor.FindTradeAgreementTarget(expansionTarget);
                if (tradeTarget != null)
                {
                    AIOverhaulPlugin.LogDebug($"Seeking trade agreement with {tradeTarget.Name} (current: {tradeCount}/{GameBalance.MinTradeAgreements})", LogCategory.Diplomacy, actor);
                    __result = __instance.RunTradeAgreementProposal(tradeTarget);
                    return false;
                }
            }

            // Nemesis: try team-aligned NAP target first
            if (NemesisTeamManager.IsNemesis(actor))
            {
                Logic.Kingdom alignedTarget = NemesisTeamManager.FindTeamAlignedNAPTarget(actor, actor.game);
                if (alignedTarget != null)
                {
                    AIOverhaulPlugin.LogDebug($"NEMESIS: Aligned NAP with {alignedTarget.Name} (teammate already has NAP)", LogCategory.Nemesis, actor);
                    __result = __instance.RunNonAggressionProposal(alignedTarget);
                    return false;
                }
            }

            // Offer non-aggression pacts to all neighbors EXCEPT the expansion target
            Logic.Kingdom napTarget = actor.FindNonAggressionTarget(expansionTarget);
            if (napTarget != null)
            {
                __result = __instance.RunNonAggressionProposal(napTarget);
                return false;
            }

            // Defensive pact formation when facing threats
            if (actor.ShouldSeekDefensivePact())
            {
                Logic.Kingdom pactTarget = actor.FindBestDefensivePactTarget();
                if (pactTarget != null)
                {
                    __result = __instance.RunDefensivePactProposal(pactTarget);
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
                        int side = war.GetSide(actor);
                        float s = war.GetSideScore(side);
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
                    __result = __instance.RunDiplomacyWithTarget(target);
                    return false;
                }
            }

            return true;
        }

        static void Postfix(KingdomAI __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;

            // Consider inviting neighbors to join our wars if we need help
            __instance.ConsiderInvitingNeighborsToWar();
        }
    }
}
