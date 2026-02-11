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
