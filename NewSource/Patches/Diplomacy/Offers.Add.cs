using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul.Patches.Diplomacy
{
    // Fix for AmbiguousMatchException: explicitly target Add(Offer) instance method
    [HarmonyPatch(typeof(Offers), "Add", typeof(Offer))]
    public static class Offers_Add
    {
        public static bool Prefix(Offers __instance, Offer offer)
        {
            // Only care about Inheritance Claims
            if (!(offer is PrincessClaimInheritanceOffer)) return true;

            // Check if receiver is a Kingdom
            var receiver = offer.to as Logic.Kingdom;
            if (receiver == null) return true;

            // Only act if this is an Enhanced AI (which covers Spectator player and regular AI)
            if (AIOverhaulPlugin.IsEnhancedAI(receiver))
            {
                // Verify Authority (Host-side only)
                if (!receiver.IsAuthority()) return true;

                AIOverhaulPlugin.LogDebug($"Auto-Accepting Inheritance Claim for {receiver.Name} (AI Controlled)", LogCategory.Diplomacy, receiver);
                
                offer.Accept();

                // Prevent adding to the offers list (since we just accepted it)
                return false;
            }

            return true;
        }
    }
}
