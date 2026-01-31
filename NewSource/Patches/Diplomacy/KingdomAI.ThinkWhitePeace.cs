using System;
using System.Collections.Generic;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "ThinkWhitePeace")]
    public class KingdomAI_ThinkWhitePeace
    {
        static bool Prefix(KingdomAI __instance, Logic.Kingdom k, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            Logic.Kingdom actor = __instance.kingdom;
            if (actor == null || k == null) return true;

            // Independence
            if (actor.sovereignState == k)
            {
                float myStr = actor.GetTotalPower();
                float theirStr = k.GetTotalPower();
                float kScore = WarLogicHelper.GetAverageWarScore(k);
                if (myStr > theirStr * GameBalance.PowerRatioStrongerEnemy ||
                    k.wars.Count > GameBalance.MaxWarsCount ||
                    kScore < GameBalance.WarScoreIndependence)
                {
                    if (OfferHelper.TrySendOffer("ClaimIndependence", __instance, k))
                    {
                        AIOverhaulPlugin.LogDebug($"Claiming independence from {k.Name}", LogCategory.War, actor);
                        __result = true;
                        return false;
                    }
                }
            }

            // Desperate Surrender
            if (actor.IsEnemy(k))
            {
                float score = WarLogicHelper.GetAverageWarScore(actor);
                if (score < GameBalance.WarScoreSurrender || (score < GameBalance.WarScoreDesperateIndependence && actor.IsDesperate()))
                {
                    Offer peace = Offer.GetCachedOffer("PeaceOfferTribute", actor, k);
                    Offer vassal = Offer.GetCachedOffer("OfferVassalage", actor, k);
                    if (peace != null && vassal != null)
                    {
                        peace.args = new List<Value> { new Value(vassal) };
                        peace.AI = true;
                        if (peace.Validate() == "ok")
                        {
                            AIOverhaulPlugin.LogDebug($"SURRENDERING to {k.Name} as vassal!", LogCategory.War, actor);
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
}
