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
            var receiver = offer.to as Logic.Kingdom;   // Kingdom being claimed from (loses territory)
            var claimant = offer.from as Logic.Kingdom;  // Kingdom making the claim (gains territory)
            
            // Only care about Inheritance Claims
            if (receiver == null || !receiver.IsAuthority() || !AIOverhaulPlugin.IsEnhancedAI(receiver)) return true;
            if (!(offer is PrincessClaimInheritanceOffer)) return true;

            // When another kingdom claims from us → auto refuse
            AIOverhaulPlugin.LogDebug($"Auto-Declining Inheritance Claim: {claimant?.Name} claims from {receiver.Name} — refusing", LogCategory.Diplomacy, receiver);
            offer.Decline();
            return false;
        }
    }
}
