using System;
using System.Collections.Generic;
using System.Linq;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// Helper methods for kingdom operations
    /// </summary>
    public static class KingdomHelper
    {
        public static bool IsEnhancedAI(this Logic.Kingdom k) => AIOverhaulPlugin.IsEnhancedAI(k);
        
        // Resource Access
        public static float GetGold(this Logic.Kingdom k) => k?.resources?[ResourceType.Gold] ?? 0f;

        public static float GetFood(this Logic.Kingdom k) => k?.resources?[ResourceType.Food] ?? 0f;

        public static float GetBooks(this Logic.Kingdom k) => k?.resources?.Get(ResourceType.Books) ?? 0f;

        public static float GetGoldIncome(this Logic.Kingdom k) => k?.income?.Get(ResourceType.Gold) ?? 0f;

        public static float GetFoodIncome(this Logic.Kingdom k) => k?.income?.Get(ResourceType.Food) ?? 0f;

        // Validation Helpers
        public static bool IsValidKingdom(this Logic.Kingdom k) => k != null && !k.IsDefeated();

        public static bool IsValidKingdomWithResources(this Logic.Kingdom k) => k != null && k.resources != null;

        public static bool IsValidKingdomWithWarsAndResources(this Logic.Kingdom k) => k != null && k.wars != null && k.resources != null && k.traditions != null;
        
        public static bool HasDisorder(this Logic.Kingdom k)
        {
            if (k == null || k.realms == null) return false;
            return k.realms.Any(realm => realm != null && realm.IsDisorder());
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

        public static bool HasBuilding(this Logic.Kingdom k, string buildingName) => k.GetBuildingCount(buildingName) > 0;

        public static bool HasBuildingUpgrade(this Logic.Kingdom k, string upgradeId)
        {
            if (k?.realms == null) return false;

            foreach (var realm in k.realms)
            {
                if (realm?.castle?.buildings == null) continue;

                foreach (var building in realm.castle.buildings)
                {
                    if (building?.def?.id == upgradeId)
                        return true;
                }
            }
            return false;
        }

        public static float GetTotalPower(this Logic.Kingdom k)
        {
            if (k == null) return 0f;
            float total = 0f;

            //k.Get
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
                        total += KingdomAI.Threat.EvalCastleStrength(realm.castle);
                }
            }

            return total;
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
                        totalThreat += neighborKingdom.GetTotalPower();
                }
            }

            return totalThreat;
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

        public static bool IsDominantIn1v1War(this Logic.Kingdom k)
        {
            if (k == null || k.wars == null || k.wars.Count != 1) return false;

            var war = k.wars[0];
            if (war == null) return false;

            // Check if 1v1 (no allies on either side)
            if (war.attackers.Count != 1 || war.defenders.Count != 1) return false;

            // Get enemy kingdom
            var enemy = war.GetEnemyLeader(k);
            if (enemy == null) return false;

            // Compare total army strength
            float ourStrength = k.GetTotalArmyStrength();
            float enemyStrength = enemy.GetTotalArmyStrength();

            if (enemyStrength <= 0) return true; // Enemy has no armies

            return ourStrength >= enemyStrength * GameBalance.PowerRatioSoloCapable;
        }

        public static float GetTotalArmyStrength(this Logic.Kingdom k)
        {
            if (k == null || k.armies == null) return 0f;
            float total = 0f;
            foreach (var army in k.armies)
            {
                if (army != null && army.IsValid())
                    total += army.EvalStrength();
            }
            return total;
        }

        public static bool IsSiegingEnemyCastle(this Logic.Kingdom k)
        {
            if (k == null || k.armies == null) return false;
            foreach (var army in k.armies)
            {
                if (army?.battle == null) continue;
                if (!army.battle.is_siege) continue;
                if (army.battle.attacker_kingdom == k) return true;
            }
            return false;
        }

        // From WarDiplomacyHelper
        public static bool WantsInvasionPlan(this Logic.Kingdom k)
        {
            if (k == null) return false;

            Logic.Kingdom target = k.SelectExpansionTarget();
            if (target == null) return false;

            float ownPower = k.GetTotalPower();
            float targetPower = target.GetTotalPower();
            
            if (ownPower > targetPower * GameBalance.PowerRatioSoloCapable) return false;

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

            if (k.allies != null && k.allies.Count >= 2) return false;

            float ownPower = k.GetTotalPower();

            Logic.Kingdom mortalEnemy = AIOverhaulPlugin.GetMortalEnemy(k, k.game);
            if (mortalEnemy != null && !mortalEnemy.IsDefeated())
            {
                float enemyPower = mortalEnemy.GetTotalPower();
                if (enemyPower >= ownPower)
                {
                    return true;
                }
            }

            float gold = k.GetGold();
            if (gold < 5000f) return false;

            float neighborThreat = k.GetNeighborThreat();
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
                if (k.IsAlly(potentialAlly)) continue;

                if (potentialAlly.wars != null && potentialAlly.wars.Count > 0) continue;

                int score = 0;

                if (mortalEnemy != null && potentialAlly.IsEnemy(mortalEnemy))
                {
                    score += GameBalance.AllianceScoreFightingMortalEnemy;
                }

                if (mortalEnemy != null && potentialAlly.IsStrategicNeighbor(mortalEnemy))
                {
                    score += GameBalance.AllianceScoreNeighborOfMortalEnemy;
                }

                float relationship = k.GetRelationship(potentialAlly);
                if (relationship < 0 && k.IsStrategicNeighbor(potentialAlly))
                {
                    score += GameBalance.AllianceScoreUnfriendlyNeighbor;
                }

                if (k.wars != null && potentialAlly.wars != null)
                {
                    foreach (var ourWar in k.wars)
                    {
                        Logic.Kingdom ourEnemy = ourWar.GetEnemyLeader(k);
                        if (potentialAlly.IsEnemy(ourEnemy))
                        {
                            score++;
                        }
                    }
                }

                if (k.wars != null)
                {
                    foreach (var ourWar in k.wars)
                    {
                        Logic.Kingdom ourEnemy = ourWar.GetEnemyLeader(k);
                        if (ourEnemy != null && potentialAlly.IsStrategicNeighbor(ourEnemy))
                        {
                            score++;
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

        public static Logic.Kingdom SelectExpansionTarget(this Logic.Kingdom k)
        {
            if (k == null || k.neighbors == null) return null;

            Logic.Kingdom selectedTarget = null;
            string reason = "";

            Logic.Kingdom mortalEnemy = AIOverhaulPlugin.GetMortalEnemy(k, k.game);
            if (mortalEnemy != null)
            {
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
                        if (neighborKingdom.IsDefeated()) continue;
                        if (k.IsAlly(neighborKingdom)) continue;

                        float relationship = k.GetRelationship(neighborKingdom);
                        float power = neighborKingdom.GetTotalPower();

                        if (relationship < lowestRelation)
                        {
                            lowestRelation = relationship;
                            worstNeighbor = neighborKingdom;
                        }

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

                if (weakestHostile != null)
                {
                    selectedTarget = weakestHostile;
                    reason = $"WEAKEST < NEUTRAL ({minPower:F0})";
                }
                else if (worstNeighbor != null)
                {
                    selectedTarget = worstNeighbor;
                    reason = $"LOWEST RELATION: {lowestRelation:F0}";
                }
            }

            AIOverhaulPlugin.ExpansionTargets.TryGetValue(k.id, out var previousTargetId);
            int newTargetId = selectedTarget?.id ?? -1;

            if (previousTargetId != newTargetId)
            {
                if (selectedTarget != null)
                {
                    string previousName = previousTargetId >= 0 ? k.game.GetKingdom(previousTargetId)?.Name ?? "Unknown" : "None";
                    AIOverhaulPlugin.LogDebug($"Expansion Target CHANGED: {previousName} -> {selectedTarget.Name} ({reason})", LogCategory.Diplomacy, k);
                    AIOverhaulPlugin.ExpansionTargets[k.id] = newTargetId;
                }
                else
                {
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

        public static Logic.Kingdom FindNonAggressionTarget(this Logic.Kingdom k, Logic.Kingdom expansionTarget)
        {
            if (k == null || k.neighbors == null) return null;

            Logic.Kingdom bestTarget = null;
            float bestRelationship = -1000f;

            foreach (var neighbor in k.neighbors)
            {
                if (neighbor is Logic.Kingdom neighborKingdom)
                {
                    if (neighborKingdom.IsDefeated()) continue;
                    if (k.IsEnemy(neighborKingdom)) continue;
                    if (neighborKingdom == expansionTarget) continue;
                    if (k.HasStance(neighborKingdom, RelationUtils.Stance.NonAggression)) continue;
                    if (k.IsAlly(neighborKingdom)) continue;

                    float relationship = k.GetRelationship(neighborKingdom);
                    float reservedThreshold = RelationUtils.Def.GetLowerTreshold(RelationUtils.RelationshipType.Reserved);
                    if (relationship < reservedThreshold) continue;

                    if (relationship > bestRelationship)
                    {
                        bestRelationship = relationship;
                        bestTarget = neighborKingdom;
                    }
                }
            }

            return bestTarget;
        }

        public static bool IsMortalEnemy(this Logic.Kingdom kingdom, Logic.Kingdom potentialEnemy)
        {
            if (kingdom == null || potentialEnemy == null) return false;
            if (AIOverhaulPlugin.MortalEnemies.ContainsKey(kingdom.id))
            {
                return AIOverhaulPlugin.MortalEnemies[kingdom.id] == potentialEnemy.id;
            }
            return false;
        }

        public static void InviteNeighborsToWar(this Logic.Kingdom kingdom, War war, KingdomAI ai)
        {
            var enemyKingdoms = war.GetEnemiesInWar(kingdom);

            foreach (var enemyKingdom in enemyKingdoms)
            {
                if (enemyKingdom.neighbors != null)
                {
                    foreach (var neighborObj in enemyKingdom.neighbors)
                    {
                        if (neighborObj is Logic.Kingdom neighbor)
                            kingdom.InviteNeighborsOfKingdomToWar(neighbor, war, ai);
                    }
                }
            }

            if (kingdom.neighbors != null)
            {
                foreach (var neighborObj in kingdom.neighbors)
                {
                    if (neighborObj is Logic.Kingdom neighbor)
                        kingdom.InviteNeighborsOfKingdomToWar(neighbor, war, ai);
                }
            }
        }

        static void InviteNeighborsOfKingdomToWar(this Logic.Kingdom kingdom, Logic.Kingdom targetKingdom, War war, KingdomAI ai)
        {
            if (string.IsNullOrEmpty(targetKingdom.Name)) return; // Sanity check

            if (targetKingdom.neighbors != null)
            {
                foreach (var neighborObj in targetKingdom.neighbors)
                {
                    if (!(neighborObj is Logic.Kingdom neighbor)) continue;
                    if (neighbor == kingdom) continue;
                    if (neighbor.IsDefeated()) continue;
                    if (kingdom.IsEnemy(neighbor)) continue;
                    
                    float relation = kingdom.GetRelationship(neighbor);
                    if (relation < GameBalance.MinRelationToInviteToWar) continue;
                    ai.TrySendWarInvite(neighbor, war);
                }
            }
        }

        public static bool IsDesperate(this Logic.Kingdom k)
        {
            if (k == null || k.realms == null) return false;
            if (k.realms.Count == 0) return true;
            var armies = k.armies ?? new List<Logic.Army>();
            if (armies.Count == 0) return true;
            float totalStr = 0;
            foreach (var a in armies) totalStr += a.EvalStrength();
            return totalStr < k.realms.Count * 250f;
        }
        
        /// <summary>
        /// Checks if a target realm is within maxDistance provinces of any of the kingdom's realms.
        /// Uses breadth-first search from all owned realms.
        /// </summary>
        public static bool IsRealmWithinDistance(this Logic.Kingdom ourKingdom, Logic.Realm targetRealm, int maxDistance, out int distance)
        {
            distance = 0;
            if (targetRealm == null || ourKingdom == null || ourKingdom.realms == null) return false;

            // BFS from all our realms
            var visited = new HashSet<Logic.Realm>();
            var queue = new Queue<(Logic.Realm realm, int distance)>();

            // Start from all our realms at distance 0
            foreach (var ourRealm in ourKingdom.realms)
            {
                if (ourRealm != null)
                {
                    visited.Add(ourRealm);
                    queue.Enqueue((ourRealm, 0));
                }
            }

            while (queue.Count > 0)
            {
                var (current, dist) = queue.Dequeue();

                // Check neighbors at dist+1
                if (dist < maxDistance && current.neighbors != null)
                {
                    foreach (var neighbor in current.neighbors)
                    {
                        if (neighbor == null || visited.Contains(neighbor)) continue;

                        // Found target within range
                        if (neighbor == targetRealm) 
                        {
                            distance = dist + 1;
                            return true;
                        }

                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, dist + 1));
                    }
                }
            }

            return false;
        }
    }
}
