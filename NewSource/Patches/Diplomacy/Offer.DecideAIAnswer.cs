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
        const string k_LogPrefix = "[AI Decision]";
        
        static bool Prefix(Offer __instance, ref string __result)
        {
            // Only intervene if the receiver is a Kingdom
            if (!(__instance.to is Logic.Kingdom receiver)) return true;

            // Get the Sender
            if (!(__instance.from is Logic.Kingdom sender)) return true;

            // Only intervene for Enhanced AI
            bool isEnhanced = AIOverhaulPlugin.IsEnhancedAI(receiver);
            if (!isEnhanced) return true;

            // Check if this is a peace offer (WhitePeaceOffer, PeaceOfferTribute, etc.)
            bool isPeaceOffer = __instance.def?.field?.key?.Contains("Peace") ?? false;
            if (isPeaceOffer)
            {
                return HandlePeaceOffer(__instance, receiver, sender, ref __result);
            }

            // Check if the Offer is Trade or Non-Aggression using type checking
            bool isTrade = __instance.IsOfType(typeof(SignTrade));
            bool isNAP = __instance.IsOfType(typeof(SignNonAggression));

            if (!isTrade && !isNAP) return true;

            string offerTypeName = __instance.GetType().Name;
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {sender.Name} -> {receiver.Name}: {offerTypeName}", LogCategory.Diplomacy, receiver);

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
            __result = DiplomacyConstants.Accept;
            return false;
        }

        /// <summary>
        /// Handle incoming peace offers. Reject if winning or sieging enemy castle.
        /// </summary>
        /// <summary>
        /// Handle incoming peace offers. Reject if winning or sieging enemy castle.
        /// </summary>
        static bool HandlePeaceOffer(Offer offer, Logic.Kingdom receiver, Logic.Kingdom sender, ref string result)
        {
            string offerKey = offer.def?.field?.key ?? DiplomacyConstants.Peace;
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} [Peace] {sender.Name} -> {receiver.Name}: {offerKey}", LogCategory.Diplomacy, receiver);

            // Find the war between sender and receiver
            var war = receiver.wars?.Find(w => w.GetEnemyLeader(receiver) == sender);
            if (war == null)
            {
                // No war found, let vanilla handle
                AIOverhaulPlugin.LogDebug($"[Peace] No war found with {sender.Name}, letting vanilla handle", LogCategory.Diplomacy, receiver);
                return true;
            }

            // Check 1: Are we winning by a good margin?
            int side = TraverseAPI.GetWarSide(war, receiver);
            float score = TraverseAPI.GetWarScore(war, side);
            if (score >= GameBalance.WarScoreRejectPeace)
            {
                AIOverhaulPlugin.LogDebug($"[Peace] Rejecting peace from {sender.Name} - winning (score: {score:F1})", LogCategory.Diplomacy, receiver);
                result = DiplomacyConstants.Decline;
                return false;
            }

            // Check 2: Are we currently sieging an enemy castle?
            if (receiver.IsSiegingEnemyCastle())
            {
                AIOverhaulPlugin.LogDebug($"[Peace] Rejecting peace from {sender.Name} - currently sieging", LogCategory.Diplomacy, receiver);
                result = DiplomacyConstants.Decline;
                return false;
            }

            // Let vanilla handle all other cases
            AIOverhaulPlugin.LogDebug($"[Peace] Letting vanilla decide on peace from {sender.Name} (score: {score:F1})", LogCategory.Diplomacy, receiver);
            return true;
        }
    }
}
