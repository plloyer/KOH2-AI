namespace AIOverhaul.Helpers
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
        public static bool TrySendOffer(string offerId, Logic.KingdomAI ai, Logic.Kingdom target)
        {
            Logic.Offer offer = Logic.Offer.GetCachedOffer(offerId, (Logic.Object)ai.kingdom, (Logic.Object)target);
            if (offer == null) return false;

            string validation = offer.Validate();
            if (validation != "ok")
            {
                AIOverhaulPlugin.LogMod($" Offer {offerId} to {target.Name} validation failed: {validation}");
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
    }
}
