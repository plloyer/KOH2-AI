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

            // Nemesis team: form pacts with teammates and sync wars
            if (NemesisTeamManager.IsNemesis(actor))
            {
                IEnumerator nemesisResult = HandleNemesisTeamPacts(__instance, actor);
                if (nemesisResult != null) { __result = nemesisResult; return false; }
                NemesisTeamManager.SyncWars(actor.game);
            }

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
            if (SettingsMenu.IsDefensive(actor)) expansionTarget = null;

            // Nemesis: try team-aligned trade target first (converge on same partners)
            if (NemesisTeamManager.IsNemesis(actor))
            {
                IEnumerator alignedResult = HandleNemesisAlignedTrade(__instance, actor);
                if (alignedResult != null) { __result = alignedResult; return false; }
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
                IEnumerator alignedResult = HandleNemesisAlignedNAP(__instance, actor);
                if (alignedResult != null) { __result = alignedResult; return false; }
            }

            // Offer non-aggression pacts to all neighbors EXCEPT the expansion target
            Logic.Kingdom napTarget = SettingsMenu.CanProposeNAP(actor) ? actor.FindNonAggressionTarget(expansionTarget) : null;
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

        /// <summary>Nemesis: form pacts with teammates. Returns IEnumerator if handled, null otherwise.</summary>
        static IEnumerator HandleNemesisTeamPacts(KingdomAI ai, Logic.Kingdom actor)
        {
            string pactType;
            Logic.Kingdom teammate = NemesisTeamManager.FindTeammateNeedingPact(actor, actor.game, out pactType);
            if (teammate == null) return null;

            AIOverhaulPlugin.LogDebug($"NEMESIS: Proposing {pactType} to teammate {teammate.Name}", LogCategory.Nemesis, actor);
            if (pactType == DiplomacyConstants.SignTrade) return ai.RunTradeAgreementProposal(teammate);
            if (pactType == DiplomacyConstants.SignNonAggression) return ai.RunNonAggressionProposal(teammate);
            if (pactType == DiplomacyConstants.OfferJoinInDefensivePact) return ai.RunDefensivePactProposal(teammate);
            return null;
        }

        /// <summary>Nemesis: converge on same trade partners as teammates. Returns IEnumerator if handled.</summary>
        static IEnumerator HandleNemesisAlignedTrade(KingdomAI ai, Logic.Kingdom actor)
        {
            Logic.Kingdom target = NemesisTeamManager.FindTeamAlignedTradeTarget(actor, actor.game);
            if (target == null) return null;
            AIOverhaulPlugin.LogDebug($"NEMESIS: Aligned trade with {target.Name} (teammate already has trade)", LogCategory.Nemesis, actor);
            return ai.RunTradeAgreementProposal(target);
        }

        /// <summary>Nemesis: converge on same NAP partners as teammates. Returns IEnumerator if handled.</summary>
        static IEnumerator HandleNemesisAlignedNAP(KingdomAI ai, Logic.Kingdom actor)
        {
            Logic.Kingdom target = NemesisTeamManager.FindTeamAlignedNAPTarget(actor, actor.game);
            if (target == null) return null;
            AIOverhaulPlugin.LogDebug($"NEMESIS: Aligned NAP with {target.Name} (teammate already has NAP)", LogCategory.Nemesis, actor);
            return ai.RunNonAggressionProposal(target);
        }

        static void Postfix(KingdomAI __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;

            // Consider inviting neighbors to join our wars if we need help
            if (SettingsMenu.CanDemandSupportInWar(__instance.kingdom))
                __instance.ConsiderInvitingNeighborsToWar();
        }
    }
}
