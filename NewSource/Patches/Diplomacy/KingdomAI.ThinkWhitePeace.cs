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

            if (!HandleNemesis(__instance, k, ref __result, actor)) return false;
            if (!HandleEmergencyPeace(__instance, k, ref __result, actor)) return false;
            if (!HandleIndependance(__instance, k, out __result, actor)) return false;
            if (!HandleDesperateSurrender(__instance, k, ref __result, actor)) return false;

            return true;
        }

        static bool HandleNemesis(KingdomAI __instance, Logic.Kingdom k, ref bool __result, Logic.Kingdom actor)
        {
            if (actor.IsEnemy(k) && NemesisTeamManager.IsNemesis(actor))
            {
                if (HandleNemesisWhitePeace(__instance, actor, k, ref __result))
                    return false;
            }

            return true;
        }

        static bool HandleEmergencyPeace(KingdomAI __instance, Logic.Kingdom k, ref bool __result, Logic.Kingdom actor)
        {
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

            return true;
        }

        static bool HandleDesperateSurrender(KingdomAI __instance, Logic.Kingdom k, ref bool __result, Logic.Kingdom actor)
        {
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

        static bool HandleIndependance(KingdomAI __instance, Logic.Kingdom k, out bool __result, Logic.Kingdom actor)
        {
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

            __result = false;
            return true;
        }

        /// <summary>Nemesis peace logic: resolve teammate wars and trim side conflicts. Returns true if handled.</summary>
        static bool HandleNemesisWhitePeace(KingdomAI ai, Logic.Kingdom actor, Logic.Kingdom k, ref bool result)
        {
            // Teammates at war: immediately send plain peace offer
            if (NemesisTeamManager.AreNemesisTeammates(actor, k))
            {
                if (OfferHelper.TrySendOffer(DiplomacyConstants.Peace, ai, k))
                {
                    AIOverhaulPlugin.LogDebug($"NEMESIS: Sending peace to teammate {k.Name} (resolving accidental war)", LogCategory.Nemesis, actor);
                    result = true;
                    return true;
                }
            }

            // Fighting more than 1 war: seek peace with non-human enemies to stay focused
            if (!NemesisTeamManager.IsHumanTeam(k) && actor.wars != null && actor.wars.Count > 1)
            {
                if (OfferHelper.TrySendOffer(DiplomacyConstants.Peace, ai, k))
                {
                    AIOverhaulPlugin.LogDebug($"NEMESIS: Seeking peace with {k.Name} (have {actor.wars.Count} wars, trimming side conflicts)", LogCategory.Nemesis, actor);
                    result = true;
                    return true;
                }
            }

            return false;
        }
    }
}
