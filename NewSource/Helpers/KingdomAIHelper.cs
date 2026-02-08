using System;
using System.Collections;
using System.Collections.Generic;
using Logic;

namespace AIOverhaul
{
    public static class KingdomAIHelper
    {
        public static IEnumerator RunDiplomacyWithTarget(this KingdomAI ai, Logic.Kingdom target)
        {
            yield return CoopThread.Call("ThinkProposeOffer", TraverseAPI.ThinkProposeOfferThread(ai, target, "neutral"));
        }

        public static IEnumerator RunDefensivePactProposal(this KingdomAI ai, Logic.Kingdom target)
        {
            // Try to propose a defensive pact
            OfferHelper.TrySendOffer(DiplomacyConstants.OfferJoinInDefensivePact, ai, target);
            yield break;
        }

        public static IEnumerator RunTradeAgreementProposal(this KingdomAI ai, Logic.Kingdom target)
        {
            // Try to propose a Trade Agreement (SignTrade)
            OfferHelper.TrySendOffer(DiplomacyConstants.SignTrade, ai, target);
            yield break;
        }

        public static IEnumerator RunNonAggressionProposal(this KingdomAI ai, Logic.Kingdom target)
        {
            // Offer a FREE non-aggression pact (no gold demanded) to build good relations
            OfferHelper.TrySendOffer(DiplomacyConstants.SignNonAggression, ai, target);
            yield break;
        }

        /// <summary>
        /// Sends a war invite (DemandSupportInWar) to ask a target kingdom to join our war.
        /// </summary>
        public static bool TrySendWarInvite(this KingdomAI ai, Logic.Kingdom target, War war)
        {
            if (ai == null || target == null || war == null) return false;

            Offer offer = Offer.GetCachedOffer(DiplomacyConstants.DemandSupportInWar, ai.kingdom, target);
            if (offer == null) return false;

            // Set war as argument
            offer.args = new List<Value> { new Value(war) };

            string validation = offer.Validate();
            if (validation != DiplomacyConstants.ValidationOk) return false;

            offer.AI = true;
            offer.Send();

            // Track offer time if sent to player
            if (target.is_player)
            {
                ai.SetLastOfferTimeToKingdom(target, offer);
                target.t_last_ai_offer_time = ai.game.time;
            }

            AIOverhaulPlugin.LogDebug($"Sent war invite to {target.Name} for war against {war.GetEnemyLeader(ai.kingdom)?.Name}", LogCategory.Diplomacy, ai.kingdom);
            return true;
        }

        /// <summary>
        /// When at war and either outnumbered or outpowered, invite neighbors with good relations to join the war.
        /// </summary>
        public static void ConsiderInvitingNeighborsToWar(this KingdomAI ai)
        {
            var kingdom = ai.kingdom;
            if (kingdom?.wars == null || kingdom.wars.Count == 0) return;

            if (kingdom.wars.Count > 1)
            {
                foreach (var war in kingdom.wars) 
                    kingdom.InviteNeighborsToWar(war, ai);
            }
            
            foreach (var war in kingdom.wars)
            {
                if (war == null) continue;

                var ourKingdoms = war.GetAlliesInWar(kingdom);
                var enemyKingdoms = war.GetEnemiesInWar(kingdom);

                if (ourKingdoms == null || enemyKingdoms == null) continue;

                // Calculate strength
                float ourStrength = GetWarSideStrength(ourKingdoms);
                float enemyStrength = GetWarSideStrength(enemyKingdoms);

                bool outnumbered = enemyKingdoms.Count >= ourKingdoms.Count;
                bool outpowered = enemyStrength >= ourStrength;

                // Only invite if we need help
                if (!outnumbered && !outpowered) continue;

                AIOverhaulPlugin.LogDebug($"War needs help: Ours={ourKingdoms.Count} ({ourStrength:F0}) vs Enemy={enemyKingdoms.Count} ({enemyStrength:F0})", LogCategory.Diplomacy, kingdom);

                // Find neighbors with good relations to invite
                if (kingdom.neighbors == null) continue;

                foreach (var neighborObj in kingdom.neighbors)
                {
                    if (!(neighborObj is Logic.Kingdom neighbor)) continue;
                    if (neighbor.IsDefeated()) continue;
                    if (kingdom.IsEnemy(neighbor)) continue; // At war with us

                    // Check if already in this war
                    bool alreadyInWar = war.attackers.Contains(neighbor) || war.defenders.Contains(neighbor);
                    if (alreadyInWar) continue;

                    // Check relation threshold
                    float relation = kingdom.GetRelationship(neighbor);
                    if (relation < GameBalance.MinRelationToInviteToWar) continue;

                    // Send invite
                    ai.TrySendWarInvite(neighbor, war);
                }
            }
        }

        static float GetWarSideStrength(List<Logic.Kingdom> kingdoms)
        {
            if (kingdoms == null) return 0f;

            float total = 0f;
            foreach (var k in kingdoms)
            {
                if (k?.armies == null) continue;
                foreach (var army in k.armies)
                {
                    if (army != null && army.IsValid())
                        total += army.EvalStrength();
                }
            }
            return total;
        }
    }
}
