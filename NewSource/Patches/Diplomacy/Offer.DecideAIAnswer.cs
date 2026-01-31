using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "DecideAIAnswer" determines how the AI responds to an offer.
    // Intent: DiplomacyAcceptancePatch
    [HarmonyPatch(typeof(Offer), "DecideAIAnswer")]
    public class Offer_DecideAIAnswer
    {
        static bool Prefix(Offer __instance, ref string __result)
        {
            // Only intervene if the receiver is a Kingdom
            if (!(__instance.to is Logic.Kingdom receiver)) return true;

            // Check if the Offer is Trade or Non-Aggression using type checking
            bool isTrade = __instance.IsOfType(typeof(SignTrade));
            bool isNAP = __instance.IsOfType(typeof(SignNonAggression));

            if (!isTrade && !isNAP) return true;

            // Get the Sender
            if (!(__instance.from is Logic.Kingdom sender)) return true;

            // Log all NAP/Trade offers for debugging
            bool isEnhanced = AIOverhaulPlugin.IsEnhancedAI(receiver);
            string offerTypeName = __instance.GetType().Name;
            AIOverhaulPlugin.LogDebug($"[Offer] {sender.Name} -> {receiver.Name}: {offerTypeName} (Enhanced: {isEnhanced})", LogCategory.Diplomacy, receiver);

            // Only intervene for Enhanced AI
            if (!isEnhanced) return true;

            // LOGIC: Accept unless it's a target or mortal enemy

            // 1. Check Mortal Enemy (Never accept friendly pacts with them)
            if (receiver.IsMortalEnemy(sender))
            {
                AIOverhaulPlugin.LogDebug($"REFUSING {offerTypeName} from MORTAL ENEMY {sender.Name}", LogCategory.Diplomacy, receiver);
                return true;
            }

            // 2. Check Expansion Target (Don't tie our hands if we plan to attack)
            Logic.Kingdom expansionTarget = receiver.SelectExpansionTarget();
            if (expansionTarget == sender)
            {
                AIOverhaulPlugin.LogDebug($"REFUSING {offerTypeName} from EXPANSION TARGET {sender.Name}", LogCategory.Diplomacy, receiver);
                return true;
            }

            // 3. Auto-accept Trade/NAP from non-targets (Enhanced AI is opportunistic)
            // Sanity check: Don't accept if already at war
            if (receiver.IsEnemy(sender)) return true;

            AIOverhaulPlugin.LogDebug($"AUTO-ACCEPTING {offerTypeName} from {sender.Name}", LogCategory.Diplomacy, receiver);
            __result = "accept";
            return false;
        }
    }
}
