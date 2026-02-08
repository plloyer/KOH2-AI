using System;
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
            if (validation != DiplomacyConstants.ValidationOk) return false;

            AIOverhaulPlugin.LogDebug($"TrySendOffer {offerId} to {target.Name}", LogCategory.Diplomacy, ai.kingdom);
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
    }
}
