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

            // Nemesis teammates at war: immediately send plain peace offer (resolve accidental war)
            if (actor.IsEnemy(k) && NemesisTeamManager.AreNemesisTeammates(actor, k))
            {
                if (OfferHelper.TrySendOffer(DiplomacyConstants.Peace, __instance, k))
                {
                    AIOverhaulPlugin.LogDebug($"NEMESIS: Sending peace to teammate {k.Name} (resolving accidental war)", LogCategory.Nemesis, actor);
                    __result = true;
                    return false;
                }
            }

            // Emergency: Enemy is assaulting our realm — offer peace immediately
            if (actor.IsEnemy(k) && actor.FindAssaultAttacker() == k)
            {
                Offer peace = Offer.GetCachedOffer(DiplomacyConstants.PeaceOfferTribute, actor, k);
                Offer vassal = Offer.GetCachedOffer(DiplomacyConstants.OfferVassalage, actor, k);
                if (peace != null && vassal != null)
                {
                    peace.args = new List<Value> { new Value(vassal) };
                    peace.AI = true;
                    if (peace.Validate() == DiplomacyConstants.ValidationOk)
                    {
                        AIOverhaulPlugin.LogDebug($"EMERGENCY: Offering surrender+vassalage to {k.Name} — they are assaulting our realm!", LogCategory.War, actor);
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

            // Independence
            if (actor.sovereignState == k)
            {
                float myStr = actor.GetTotalPower();
                float theirStr = k.GetTotalPower();
                float kScore = k.GetAverageWarScore();
                if (myStr > theirStr * GameBalance.PowerRatioStrongerEnemy ||
                    k.wars.Count > GameBalance.MaxWarsCount ||
                    kScore < GameBalance.WarScoreIndependence)
                {
                    if (OfferHelper.TrySendOffer(DiplomacyConstants.ClaimIndependence, __instance, k))
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
                float score = actor.GetAverageWarScore();
                if (score < GameBalance.WarScoreSurrender || (score < GameBalance.WarScoreDesperateIndependence && actor.IsDesperate()))
                {
                    Offer peace = Offer.GetCachedOffer(DiplomacyConstants.PeaceOfferTribute, actor, k);
                    Offer vassal = Offer.GetCachedOffer(DiplomacyConstants.OfferVassalage, actor, k);
                    if (peace != null && vassal != null)
                    {
                        peace.args = new List<Value> { new Value(vassal) };
                        peace.AI = true;
                        if (peace.Validate() == DiplomacyConstants.ValidationOk)
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
