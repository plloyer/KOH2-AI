using System;
using System.Collections.Generic;
using Logic;
using UnityEngine;

namespace AIOverhaul
{
    public static class WarLogicHelper
    {
        /// <summary>
        /// Calculate average war score for a kingdom across all wars.
        /// Replacement for KingdomAI.GetAverageWarScore() which doesn't exist.
        /// Negative score = losing, positive = winning.
        /// </summary>
        public static float GetAverageWarScore(this Logic.Kingdom k)
        {
            if (k == null || k.wars == null || k.wars.Count == 0) return 0f;

            float totalScore = 0f;
            int validWars = 0;

            foreach (var war in k.wars)
            {
                if (war == null) continue;

                try
                {
                    int side = TraverseAPI.GetWarSide(war, k);
                    float warScore = TraverseAPI.GetWarScore(war, side);
                    totalScore += warScore;
                    validWars++;
                }
                catch
                {
                    // Skip wars where we can't get score
                }
            }

            return validWars > 0 ? totalScore / validWars : 0f;
        }

        public static float GetTotalPower(this Logic.Kingdom k)
        {
            if (k == null) return 0f;
            float total = 0f;

            if (k.realms != null)
            {
                foreach (var realm in k.realms)
                {
                    if (realm.armies != null)
                    {
                        foreach (var army in realm.armies)
                        {
                            if (army == null) continue;
                            total += army.EvalStrength();
                        }
                    }

                    if (realm.castle != null && k.ai != null)
                    {
                        total += KingdomAI.Threat.EvalCastleStrength(realm.castle);
                    }
                }
            }

            return total;
        }

        public static bool HasDisorder(this Logic.Kingdom k)
        {
            if (k == null || k.realms == null) return false;

            foreach (var realm in k.realms)
            {
                if (realm != null && realm.IsDisorder())
                {
                    return true;
                }
            }

            return false;
        }

        public static float GetNeighborThreat(this Logic.Kingdom k)
        {
            if (k == null || k.neighbors == null) return 0f;

            float totalThreat = 0f;

            foreach (var neighbor in k.neighbors)
            {
                if (neighbor is Logic.Kingdom neighborKingdom)
                {
                    if (neighborKingdom.IsDefeated()) continue;

                    // Consider enemies and those with bad relations as threats
                    if (k.IsEnemy(neighborKingdom))
                    {
                        totalThreat += neighborKingdom.GetTotalPower();
                    }
                }
            }

            return totalThreat;
        }

        public static bool HasHighThreat(this Logic.Kingdom k)
        {
            if (k == null || k.realms == null) return false;

            // Iterate through all realms in the kingdom and check their threat level
            foreach (var realm in k.realms)
            {
                if (realm == null || realm.threat == null) continue;
                
                // Level 3 is Level.Attack, 4 is Invaded, 5 is Siege
                if ((int)realm.threat.level >= GameBalance.KingdomSideAttackLevel) 
                {
                    return true;
                }
            }

            return false;
        }

        public static bool WantsInvasionPlan(this Logic.Kingdom k)
        {
            if (k == null) return false;

            // We want an invasion plan if we have a clear expansion target
            // and we are strong enough to consider attacking but would like allies.
            
            // Re-use existing SelectExpansionTarget logic
            Logic.Kingdom target = k.SelectExpansionTarget();
            if (target == null) return false;

            // If we are significantly stronger than the target, we might not need a plan
            float ownPower = k.GetTotalPower();
            float targetPower = target.GetTotalPower();
            
            if (ownPower > targetPower * GameBalance.PowerRatioSoloCapable) return false; // We can handle it alone

            // If we have at least one neighbor who is an ally or high relation, 
            // we might want a diplomat to coordinate.
            foreach (var neighbor in k.neighbors)
            {
                if (neighbor is Logic.Kingdom nk && nk != target && !nk.IsDefeated())
                {
                    if (k.IsAlly(nk) || k.GetRelationship(nk) > GameBalance.FriendlyRelationshipThreshold)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool ShouldSeekDefensivePact(this Logic.Kingdom k)
        {
            if (k == null) return false;

            // Don't seek pacts if we already have 2+ allies
            if (k.allies != null && k.allies.Count >= 2) return false;

            float ownPower = k.GetTotalPower();

            // PRIORITY: If we have a mortal enemy that's equal or stronger, ALWAYS seek allies FAST
            Logic.Kingdom mortalEnemy = AIOverhaulPlugin.GetMortalEnemy(k, k.game);
            if (mortalEnemy != null && !mortalEnemy.IsDefeated())
            {
                float enemyPower = mortalEnemy.GetTotalPower();

                // If mortal enemy is equal or stronger, we MUST seek allies before attacking
                if (enemyPower >= ownPower)
                {
                    return true; // Prioritize forming coalition against equal/stronger mortal enemy
                }
            }

            // Need sufficient gold for diplomacy (5000+ gold)
            float gold = k.resources?[ResourceType.Gold] ?? 0f;
            if (gold < 5000f) return false;

            // Check if we face significant neighbor threats
            float neighborThreat = k.GetNeighborThreat();

            // Seek pacts if neighbor threat > 0.75x our power
            return neighborThreat > ownPower * 0.75f;
        }

        public static Logic.Kingdom FindBestDefensivePactTarget(this Logic.Kingdom k)
        {
            if (k == null || k.game == null) return null;

            Logic.Kingdom mortalEnemy = AIOverhaulPlugin.GetMortalEnemy(k, k.game);
            Logic.Kingdom bestTarget = null;
            int bestScore = 0;

            foreach (var potentialAlly in k.game.kingdoms)
            {
                if (potentialAlly == null || potentialAlly == k) continue;
                if (potentialAlly.IsDefeated()) continue;
                if (k.IsEnemy(potentialAlly)) continue;
                if (k.IsAlly(potentialAlly)) continue; // Already allied

                // REQUIREMENT: Cannot form defensive pact with kingdoms currently at war
                if (potentialAlly.wars != null && potentialAlly.wars.Count > 0) continue;

                int score = 0;

                // HIGHEST PRIORITY: If they're already enemies with our mortal enemy
                if (mortalEnemy != null && potentialAlly.IsEnemy(mortalEnemy))
                {
                    score += GameBalance.AllianceScoreFightingMortalEnemy;
                }

                // HIGH PRIORITY: If they're neighbors of our mortal enemy (can join war later)
                if (mortalEnemy != null && potentialAlly.IsStrategicNeighbor(mortalEnemy))
                {
                    score += GameBalance.AllianceScoreNeighborOfMortalEnemy;
                }

                // PRIORITY: Unfriendly neighbors are good alliance targets
                float relationship = k.GetRelationship(potentialAlly);
                if (relationship < 0 && k.IsStrategicNeighbor(potentialAlly))
                {
                    score += GameBalance.AllianceScoreUnfriendlyNeighbor;
                }

                // Count common enemies
                if (k.wars != null && potentialAlly.wars != null)
                {
                    foreach (var ourWar in k.wars)
                    {
                        Logic.Kingdom ourEnemy = ourWar.GetEnemyLeader(k);
                        if (potentialAlly.IsEnemy(ourEnemy))
                        {
                            score++; // Bonus for each common enemy
                        }
                    }
                }

                // Also consider if they're neighbors of our enemies
                if (k.wars != null)
                {
                    foreach (var ourWar in k.wars)
                    {
                        Logic.Kingdom ourEnemy = ourWar.GetEnemyLeader(k);
                        if (ourEnemy != null && potentialAlly.IsStrategicNeighbor(ourEnemy))
                        {
                            score++; // Small bonus for being positioned against our enemies
                        }
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = potentialAlly;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Select the best neighbor to designate as expansion target
        /// Strategy: MORTAL ENEMY first (never forgive), then current wars, then LOWEST RELATION
        /// Re-evaluates dynamically based on current relations (not locked in during peace)
        /// Only logs when the expansion target changes
        /// </summary>
        public static Logic.Kingdom SelectExpansionTarget(this Logic.Kingdom k)
        {
            if (k == null || k.neighbors == null) return null;

            Logic.Kingdom selectedTarget = null;
            string reason = "";

            // PRIORITY 1: Mortal Enemy - the FIRST kingdom that declared war on us
            // This is a permanent grudge that overrides all other considerations
            Logic.Kingdom mortalEnemy = AIOverhaulPlugin.GetMortalEnemy(k, k.game);
            if (mortalEnemy != null)
            {
                // Only if they're still a neighbor (could have lost border provinces)
                bool isStillNeighbor = false;
                foreach (var neighbor in k.neighbors)
                {
                    if (neighbor is Logic.Kingdom nk && nk == mortalEnemy)
                    {
                        isStillNeighbor = true;
                        break;
                    }
                }

                if (isStillNeighbor)
                {
                    selectedTarget = mortalEnemy;
                    reason = "MORTAL ENEMY";
                }
            }

            // PRIORITY 2: If already at war with a neighbor, that's our expansion target
            if (selectedTarget == null)
            {
                foreach (var neighbor in k.neighbors)
                {
                    if (neighbor is Logic.Kingdom nk && k.IsEnemy(nk) && !nk.IsDefeated())
                    {
                        selectedTarget = nk;
                        reason = "CURRENT WAR";
                        break;
                    }
                }
            }

            // PRIORITY 3: Select neighbor with LOWEST relationship (RE-EVALUATED EACH TIME)
            // This ensures we only refuse NAPs with our worst enemy
            // If relations improve with current target, we'll automatically switch to a worse neighbor
            if (selectedTarget == null)
            {
                Logic.Kingdom worstNeighbor = null;
                float lowestRelation = float.MaxValue;

                Logic.Kingdom weakestHostile = null;
                float minPower = float.MaxValue;

                foreach (var neighbor in k.neighbors)
                {
                    if (neighbor is Logic.Kingdom neighborKingdom)
                    {
                        // Skip if defeated
                        if (neighborKingdom.IsDefeated()) continue;

                        // Skip if allied (don't betray allies)
                        if (k.IsAlly(neighborKingdom)) continue;

                        // Get relationship value
                        float relationship = k.GetRelationship(neighborKingdom);
                        float power = neighborKingdom.GetTotalPower();

                        // Track WORST relationship (fallback)
                        if (relationship < lowestRelation)
                        {
                            lowestRelation = relationship;
                            worstNeighbor = neighborKingdom;
                        }

                        // Track WEAKEST below Neutral threshold (primary)
                        if (relationship < GameBalance.NeutralRelationThreshold)
                        {
                            if (power < minPower)
                            {
                                minPower = power;
                                weakestHostile = neighborKingdom;
                            }
                        }
                    }
                }

                // Primary: Weakest neighbor with bad relations
                if (weakestHostile != null)
                {
                    selectedTarget = weakestHostile;
                    reason = $"WEAKEST < NEUTRAL ({minPower:F0})";
                }
                // Fallback: Neighbor with worst relations
                else if (worstNeighbor != null)
                {
                    selectedTarget = worstNeighbor;
                    reason = $"LOWEST RELATION: {lowestRelation:F0}";
                }
            }

            // Check if target changed and log if it did
            int previousTargetId = -1;
            AIOverhaulPlugin.ExpansionTargets.TryGetValue(k.id, out previousTargetId);

            int newTargetId = selectedTarget?.id ?? -1;

            if (previousTargetId != newTargetId)
            {
                // Target changed - log it
                if (selectedTarget != null)
                {
                    string previousName = previousTargetId >= 0 ? k.game.GetKingdom(previousTargetId)?.Name ?? "Unknown" : "None";
                    AIOverhaulPlugin.LogDebug($"Expansion Target CHANGED: {previousName} -> {selectedTarget.Name} ({reason})", LogCategory.Diplomacy, k);
                    AIOverhaulPlugin.ExpansionTargets[k.id] = newTargetId;
                }
                else
                {
                    // No longer have a target
                    if (previousTargetId >= 0)
                    {
                        string previousName = k.game.GetKingdom(previousTargetId)?.Name ?? "Unknown";
                        AIOverhaulPlugin.LogDebug($"Expansion Target CLEARED: {previousName} -> None (all neighbors are allies or defeated)", LogCategory.Diplomacy, k);
                    }
                    AIOverhaulPlugin.ExpansionTargets.Remove(k.id);
                }
            }

            return selectedTarget;
        }

        /// <summary>
        /// Find the best neighbor to offer a non-aggression pact to
        /// EXCLUDES the designated expansion target - we want to keep one enemy neighbor
        /// </summary>
        public static Logic.Kingdom FindNonAggressionTarget(this Logic.Kingdom k, Logic.Kingdom expansionTarget)
        {
            if (k == null || k.neighbors == null) return null;

            Logic.Kingdom bestTarget = null;
            float bestRelationship = -1000f;

            foreach (var neighbor in k.neighbors)
            {
                if (neighbor is Logic.Kingdom neighborKingdom)
                {
                    // Skip if defeated
                    if (neighborKingdom.IsDefeated()) continue;

                    // Skip if at war
                    if (k.IsEnemy(neighborKingdom)) continue;

                    // CRITICAL: Skip if this is our designated expansion target
                    // We want to keep this neighbor as potential enemy for expansion
                    if (neighborKingdom == expansionTarget) continue;

                    // Skip if already have non-aggression pact
                    if (k.HasStance(neighborKingdom, RelationUtils.Stance.NonAggression)) continue;

                    // Skip if already allied (better than NAP)
                    if (k.IsAlly(neighborKingdom)) continue;

                    // Get relationship value
                    float relationship = k.GetRelationship(neighborKingdom);

                    // Skip if relationship is too hostile (below "Reserved" threshold)
                    // Reserved = -200, so only offer to neighbors we have at least neutral-ish relations with
                    float reservedThreshold = RelationUtils.Def.GetLowerTreshold(RelationUtils.RelationshipType.Reserved);
                    if (relationship < reservedThreshold) continue;

                    // Prioritize neighbors with better relations (more likely to accept)
                    // Focus on neutral/sympathetic/trusting neighbors to build security buffer
                    if (relationship > bestRelationship)
                    {
                        bestRelationship = relationship;
                        bestTarget = neighborKingdom;
                    }
                }
            }

            return bestTarget;
        }

        public static bool IsStrategicNeighbor(this Logic.Kingdom a, Logic.Kingdom b)
        {
            if (a == null || b == null) return false;
            if (a.neighbors == null) return false;
            foreach (var n in a.neighbors)
            {
                if (n is Logic.Kingdom k && k == b) return true;
            }

            return false;
        }

        public static bool HasCommonEnemyWithAlly(this Logic.Kingdom a, Logic.Kingdom b)
        {
            if (a == null || b == null || a.wars == null || b.wars == null) return false;
            foreach (var warA in a.wars)
            {
                Logic.Kingdom enemyA = warA.GetEnemyLeader(a);
                foreach (var warB in b.wars)
                {
                    if (warB.GetEnemyLeader(b) == enemyA) return true;
                }
            }

            return false;
        }

        public static int GetTradeAgreementCount(this Logic.Kingdom k)
        {
            if (k == null || k.game == null) return 0;

            // Iterate known kingdoms or just all active kingdoms to count trade agreements
            int count = 0;
            if (k.game.kingdoms != null)
            {
                foreach (var other in k.game.kingdoms)
                {
                    if (other != null && other != k && !other.IsDefeated())
                    {
                        if (k.HasTradeAgreement(other))
                            count++;
                    }
                }
            }
            return count;
        }

        public static bool IsMortalEnemy(this Logic.Kingdom kingdom, Logic.Kingdom potentialEnemy)
        {
            if (kingdom == null || potentialEnemy == null) return false;

            // Check if potentialEnemy is kingdom's mortal enemy
            if (AIOverhaulPlugin.MortalEnemies.ContainsKey(kingdom.id))
            {
                return AIOverhaulPlugin.MortalEnemies[kingdom.id] == potentialEnemy.id;
            }

            return false;
        }

        /// <summary>
        /// When at war and either outnumbered or outpowered, invite neighbors with good relations to join the war.
        /// </summary>
        public static void ConsiderInvitingNeighborsToWar(this KingdomAI ai)
        {
            var kingdom = ai.kingdom;
            if (kingdom?.wars == null || kingdom.wars.Count == 0) return;

            foreach (var war in kingdom.wars)
            {
                if (war == null) continue;

                // Get our side (0 = attackers, 1 = defenders)
                int ourSide = TraverseAPI.GetWarSide(war, kingdom);
                var ourKingdoms = (ourSide == 0) ? war.attackers : war.defenders;
                var enemyKingdoms = (ourSide == 0) ? war.defenders : war.attackers;

                if (ourKingdoms == null || enemyKingdoms == null) continue;

                // Calculate strength
                float ourStrength = GetWarSideStrength(ourKingdoms);
                float enemyStrength = GetWarSideStrength(enemyKingdoms);

                bool outnumbered = enemyKingdoms.Count >= ourKingdoms.Count;
                bool outpowered = enemyStrength >= ourStrength;

                // Only invite if we need help
                if (!outnumbered && !outpowered) continue;

                AIOverhaulPlugin.LogDebug($"War needs help: Ours={ourKingdoms.Count} ({ourStrength:F0}) vs Enemy={enemyKingdoms.Count} ({enemyStrength:F0})",
                    LogCategory.Diplomacy, kingdom);

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
                    OfferHelper.TrySendWarInvite(ai, neighbor, war);
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
        
        public static bool IsDesperate(this Logic.Kingdom k)
        {
            if (k == null || k.realms == null) return false;
            if (k.realms.Count == 0) return true;
            var armies = k?.armies ?? new List<Logic.Army>();
            if (armies.Count == 0) return true;
            float totalStr = 0;
            foreach (var a in armies) totalStr += a.EvalStrength();
            return totalStr < k.realms.Count * 250f;
        }
    }
}
