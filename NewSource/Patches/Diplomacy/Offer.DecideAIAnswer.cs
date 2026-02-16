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

            // Nemesis teammates: auto-accept cooperative offers, coordinate peace
            if (NemesisTeamManager.AreNemesisTeammates(receiver, sender))
            {
                if (__instance.IsOfType(typeof(SignTrade)) || __instance.IsOfType(typeof(SignNonAggression))
                    || __instance.IsOfType(typeof(OfferSupportInWar)) || __instance.def?.field?.key == DiplomacyConstants.OfferJoinInDefensivePact)
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} NEMESIS AUTO-ACCEPT {__instance.GetType().Name} from teammate {sender.Name}", LogCategory.Nemesis, receiver);
                    __result = DiplomacyConstants.Accept;
                    return false;
                }

                // Peace offers from teammate: accept (resolve accidental wars)
                bool isPeace = __instance.def?.field?.key?.Contains("Peace") ?? false;
                if (isPeace)
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} NEMESIS AUTO-ACCEPT peace from teammate {sender.Name}", LogCategory.Nemesis, receiver);
                    __result = DiplomacyConstants.Accept;
                    return false;
                }
            }

            // Nemesis: auto-accept trade/NAP from kingdoms that a teammate already has pacts with
            if (NemesisTeamManager.IsNemesis(receiver) && !NemesisTeamManager.IsHumanTeam(sender))
            {
                bool isTradeOffer = __instance.IsOfType(typeof(SignTrade));
                bool isNAPOffer = __instance.IsOfType(typeof(SignNonAggression));
                if (isTradeOffer || isNAPOffer)
                {
                    foreach (int id in NemesisTeamManager.NemesisKingdomIds)
                    {
                        if (id == receiver.id) continue;
                        var teammate = receiver.game.GetKingdom(id);
                        if (teammate == null || teammate.IsDefeated()) continue;
                        if ((isTradeOffer && teammate.HasTradeAgreement(sender)) || (isNAPOffer && teammate.HasStance(sender, RelationUtils.Stance.NonAggression)))
                        {
                            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} NEMESIS: accepting {__instance.GetType().Name} from {sender.Name} (teammate {teammate.Name} already has pact)", LogCategory.Nemesis, receiver);
                            __result = DiplomacyConstants.Accept;
                            return false;
                        }
                    }
                }
            }

            // Check if this is a peace offer (WhitePeaceOffer, PeaceOfferTribute, etc.)
            bool isPeaceOffer = __instance.def?.field?.key?.Contains("Peace") ?? false;
            if (isPeaceOffer)
            {
                return HandlePeaceOffer(__instance, receiver, sender, ref __result);
            }

            // War support: always accept (OfferSupportInWar covers DemandSupportInWar and AskForProtection)
            if (__instance.IsOfType(typeof(OfferSupportInWar)))
            {
                string supportType = __instance.GetType().Name;
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} AUTO-ACCEPTING {supportType} from {sender.Name}", LogCategory.Diplomacy, receiver);
                __result = DiplomacyConstants.Accept;
                return false;
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
            int side = war.GetSide(receiver);
            float score = war.GetSideScore(side);
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
