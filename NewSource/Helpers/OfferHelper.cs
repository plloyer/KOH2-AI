using System;
using System.Collections.Generic;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// Helper methods for creating and sending diplomatic offers
    /// </summary>
    public static class OfferHelper
    {
        /// <summary>
        /// Creates, validates, and sends an offer. Handles player offer time tracking.
        /// </summary>
        /// <param name="offerId">The offer type ID (e.g., "OfferJoinInDefensivePact")</param>
        /// <param name="ai">The KingdomAI making the offer</param>
        /// <param name="target">The target kingdom receiving the offer</param>
        /// <returns>True if offer was sent successfully</returns>
        public static bool TrySendOffer(string offerId, KingdomAI ai, Logic.Kingdom target)
        {
            Offer offer = Offer.GetCachedOffer(offerId, ai.kingdom, target);
            if (offer == null) return false;

            string validation = offer.Validate();
            if (validation != "ok")
            {
                return false;
            }

            offer.AI = true;
            offer.Send();

            // Track offer time if sent to player
            if (target.is_player)
            {
                ai.SetLastOfferTimeToKingdom(target, offer);
                target.t_last_ai_offer_time = ai.game.time;
            }

            return true;
        }

        /// <summary>
        /// Sends a war invite (DemandSupportInWar) to ask a target kingdom to join our war.
        /// </summary>
        /// <param name="ai">The KingdomAI making the request</param>
        /// <param name="target">The target kingdom to invite</param>
        /// <param name="war">The war to invite them to join</param>
        /// <returns>True if invite was sent successfully</returns>
        public static bool TrySendWarInvite(KingdomAI ai, Logic.Kingdom target, War war)
        {
            if (ai == null || target == null || war == null) return false;

            // Use DemandSupportInWar - asks target to join our war
            Offer offer = Offer.GetCachedOffer("DemandSupportInWar",
                ai.kingdom, target);
            if (offer == null) return false;

            // Set war as argument
            offer.args = new List<Value> { new Value(war) };

            string validation = offer.Validate();
            if (validation != "ok") return false;

            offer.AI = true;
            offer.Send();

            // Track offer time if sent to player
            if (target.is_player)
            {
                ai.SetLastOfferTimeToKingdom(target, offer);
                target.t_last_ai_offer_time = ai.game.time;
            }

            AIOverhaulPlugin.LogDebug($"Sent war invite to {target.Name} for war against {war.GetEnemyLeader(ai.kingdom)?.Name}",
                LogCategory.Diplomacy, ai.kingdom);
            return true;
        }
    }
}
