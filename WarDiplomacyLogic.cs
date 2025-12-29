using HarmonyLib;
using Logic;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AIOverhaul
{
    public static class WarLogicHelper
    {
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
                        total += Logic.KingdomAI.Threat.EvalCastleStrength(realm.castle);
                    }
                }
            }

            return total;
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
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkDeclareWar")]
    public class WarDeclarationPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Kingdom k, ref bool __result)
        {
            if (k == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

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

            bool targetAtWar = k.wars != null && k.wars.Count > 0;
            bool commonEnemy = WarLogicHelper.HasCommonEnemyWithAlly(__instance.kingdom, k);

            if (targetPower > ownPower * 1.5f)
            {
                if (!targetAtWar && !commonEnemy)
                {
                    AIOverhaulPlugin.Instance.Log($"[AI-Mod] Blocking war on {k.Name} - Target too strong ({targetPower:F0} vs {ownPower:F0}) and not distracted.");
                    __result = false;
                    return false;
                }

                AIOverhaulPlugin.Instance.Log($"[AI-Mod] Proceeding with war on stronger target {k.Name} due to opportunity (AtWar: {targetAtWar}, CommonEnemy: {commonEnemy})");
            }

            float powerRatio = targetPower > 0 ? ownPower / targetPower : (ownPower > 0 ? 10f : 1f);
            AIOverhaulPlugin.Instance.Log($"[AI-Mod] AI {__instance.kingdom.Name} declaring war on {k.Name}. Power Ratio: {powerRatio:F2}");
            return true;
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkDiplomacy")]
    public class SurvivalDiplomacyPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, ref IEnumerator __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            Logic.Kingdom actor = __instance.kingdom;
            float score = Traverse.Create(actor.ai).Method("GetAverageWarScore").GetValue<float>();

            if (score < -15f || actor.wars.Count >= 2)
            {
                Logic.Kingdom target = null;

                // Priority 1: Strongest enemy for peace
                if (score < -20f)
                {
                    float worst = 0;
                    foreach (var war in actor.wars)
                    {
                        int side = Traverse.Create(war).Method("GetSide", new object[] { actor }).GetValue<int>();
                        float s = Traverse.Create(war).Method("GetWarScore", new object[] { side }).GetValue<float>();
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
                    AIOverhaulPlugin.Instance.Log($"[AI-Mod] {actor.Name} in survival mode. Focusing on {target.Name}");
                    __result = RunDiplomacyWithTarget(__instance, target);
                    return false;
                }
            }

            return true;
        }

        static IEnumerator RunDiplomacyWithTarget(Logic.KingdomAI ai, Logic.Kingdom target)
        {
            yield return (object)CoopThread.Call("ThinkProposeOffer", (IEnumerator)Traverse.Create(ai).Method("ThinkProposeOfferThread", new object[] { target, "neutral" }).GetValue());
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkWhitePeace")]
    public class SurvivalPeacePatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Kingdom k, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            Logic.Kingdom actor = __instance.kingdom;
            if (actor == null || k == null) return true;

            // Independence
            if (actor.sovereignState == k)
            {
                float myStr = WarLogicHelper.GetTotalPower(actor);
                float theirStr = WarLogicHelper.GetTotalPower(k);
                float kScore = Traverse.Create(k.ai).Method("GetAverageWarScore").GetValue<float>();
                if (myStr > theirStr * 1.3f || k.wars.Count > 2 || kScore < -30f)
                {
                    Logic.Offer indep = Logic.Offer.GetCachedOffer("ClaimIndependence", (Logic.Object)actor, (Logic.Object)k);
                    if (indep != null && indep.Validate() == "ok")
                    {
                        AIOverhaulPlugin.Instance.Log($"[AI-Mod] {actor.Name} claiming independence from {k.Name}");
                        indep.Send();
                        if (k.is_player)
                        {
                            __instance.SetLastOfferTimeToKingdom(k, indep);
                            k.t_last_ai_offer_time = __instance.game.time;
                        }

                        __result = true;
                        return false;
                    }
                }
            }

            // Desperate Surrender
            if (actor.IsEnemy(k))
            {
                float score = Traverse.Create(actor.ai).Method("GetAverageWarScore").GetValue<float>();
                if (score < -40f || (score < -10f && SurvivalLogic.IsDesperate(actor)))
                {
                    Logic.Offer peace = Logic.Offer.GetCachedOffer("PeaceOfferTribute", (Logic.Object)actor, (Logic.Object)k);
                    Logic.Offer vassal = Logic.Offer.GetCachedOffer("OfferVassalage", (Logic.Object)actor, (Logic.Object)k);
                    if (peace != null && vassal != null)
                    {
                        peace.args = new List<Logic.Value> { new Logic.Value(vassal) };
                        peace.AI = true;
                        if (peace.Validate() == "ok")
                        {
                            AIOverhaulPlugin.Instance.Log($"[AI-Mod] {actor.Name} SURRENDERING to {k.Name} as vassal!");
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
            // TODO: BuddySystem disabled
            var armies = k?.armies ?? new List<Logic.Army>();
            if (armies.Count == 0) return true;
            float totalStr = 0;
            foreach (var a in armies) totalStr += a.EvalStrength();
            return totalStr < k.realms.Count * 250f;
        }
    }
}
