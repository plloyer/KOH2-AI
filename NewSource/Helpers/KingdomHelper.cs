using System;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// Helper methods for kingdom operations
    /// </summary>
    public static class KingdomHelper
    {
        // Resource Access
        public static float GetGold(this Logic.Kingdom k)
        {
            return k?.resources?[ResourceType.Gold] ?? 0f;
        }

        public static float GetFood(this Logic.Kingdom k)
        {
            return k?.resources?[ResourceType.Food] ?? 0f;
        }

        public static float GetBooks(this Logic.Kingdom k)
        {
            return k?.resources?.Get(ResourceType.Books) ?? 0f;
        }

        public static float GetGoldIncome(this Logic.Kingdom k)
        {
            return k?.income?.Get(ResourceType.Gold) ?? 0f;
        }

        public static float GetFoodIncome(this Logic.Kingdom k)
        {
            return k?.income?.Get(ResourceType.Food) ?? 0f;
        }

        // Validation Helpers
        public static bool IsValidKingdom(this Logic.Kingdom k) => k != null && !k.IsDefeated();

        public static bool IsValidKingdomWithResources(this Logic.Kingdom k) => k != null && k.resources != null;

        public static bool IsValidKingdomWithWarsAndResources(this Logic.Kingdom k) => k != null && k.wars != null && k.resources != null && k.traditions != null;
        
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

        // From MilitaryHelper
        public static bool HasTwoReadyArmies(this Logic.Kingdom kingdom)
        {
            if (kingdom?.armies == null || kingdom.armies.Count < GameBalance.FirstTwoArmiesCount)
                return false;

            int readyArmies = 0;
            for (int i = 0; i < Math.Min(GameBalance.FirstTwoArmiesCount, kingdom.armies.Count); i++)
            {
                var army = kingdom.armies[i];
                if (army == null) continue;

                bool isFull = army.units.Count >= GameBalance.FullArmySize;
                int strength = army.EvalStrength();
                bool hasStrength = strength >= GameBalance.MinArmyStrengthForFortification;

                if (isFull && hasStrength)
                    readyArmies++;
            }

            return readyArmies >= GameBalance.FirstTwoArmiesCount;
        }

        // From BuildingHelper
        public static bool HasBuilding(this Logic.Kingdom k, string buildingName) => k.GetBuildingCount(buildingName) > 0;

        public static int GetBuildingCount(this Logic.Kingdom k, string buildingName)
        {
            int count = 0;
            if (k.realms == null) return count;
            foreach (var realm in k.realms)
            {
                if (realm.castle?.buildings == null) continue;
                foreach (var b in realm.castle.buildings)
                {
                    if (b?.def?.id == buildingName) count++;
                }
            }
            return count;
        }

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

        // From CourtHelper
        public static int CountCourtMembers(this Logic.Kingdom k, string classId)
        {
            if (k?.court == null) return 0;
            int count = 0;
            foreach (var c in k.court)
            {
                if (c != null && c.class_def?.id == classId)
                    count++;
            }
            return count;
        }

        public static int CountMerchants(this Logic.Kingdom k)
        {
            return k.CountCourtMembers(CharacterClassNames.Merchant);
        }

        public static int CountClerics(this Logic.Kingdom k)
        {
            return k.CountCourtMembers(CharacterClassNames.Cleric);
        }

        public static int CountDiplomats(this Logic.Kingdom k)
        {
            return k.CountCourtMembers(CharacterClassNames.Diplomat);
        }

        public static bool HasCleric(this Logic.Kingdom k)
        {
            if (k?.court == null) return false;
            foreach (var c in k.court)
            {
                if (c != null && c.IsCleric()) return true;
            }
            return false;
        }

        public static bool HasIdleMerchant(this Logic.Kingdom k)
        {
            if (k?.court == null) return false;

            foreach (var character in k.court)
            {
                if (character == null || character.class_def?.id != CharacterClassNames.Merchant) continue;

                // Check if this merchant has an active trade route
                bool hasTradeRoute = false;
                if (character.actions?.active != null)
                {
                    foreach (var action in character.actions.active)
                    {
                        string aid = action?.def?.id;
                        if (string.IsNullOrEmpty(aid)) continue;

                        if (aid == ActionNames.Trade || 
                            aid == ActionNames.TradeWithKingdom ||
                            aid == ActionNames.EstablishTradeRoute)
                        {
                            hasTradeRoute = true;
                            break;
                        }
                    }
                }

                if (!hasTradeRoute) return true; // Found an idle merchant
            }

            return false; // No idle merchants
        }

        public static Logic.Character GetKnightAtSlot(this Logic.Kingdom k, int index)
        {
            if (k?.court == null) return null;
            if (index < 0 || index >= k.court.Count) return null;
            
            return k.court[index];
        }

        public static void OrganizeCourt(this Logic.Kingdom k)
        {
            if (k?.court == null || k.court.Count < 2) return; // Need at least 2 to organize

            int courtSize = k.court.Count;
            var slots = new Logic.Character[courtSize];
            var unassigned = new System.Collections.Generic.List<Logic.Character>(k.court);

            if (unassigned.Count > 0)
            {
                slots[0] = unassigned[0];
                unassigned.RemoveAt(0);
            }

            var marshals = new System.Collections.Generic.List<Logic.Character>();
            var merchants = new System.Collections.Generic.List<Logic.Character>();
            var clerics = new System.Collections.Generic.List<Logic.Character>();
            var diplomats = new System.Collections.Generic.List<Logic.Character>();
            var spies = new System.Collections.Generic.List<Logic.Character>();
            var others = new System.Collections.Generic.List<Logic.Character>();

            foreach (var c in unassigned)
            {
                if (c == null) continue;
                string classId = c.class_def?.id;

                if (classId == CharacterClassNames.Marshal) marshals.Add(c);
                else if (classId == CharacterClassNames.Merchant) merchants.Add(c);
                else if (classId == CharacterClassNames.Cleric) clerics.Add(c);
                else if (classId == CharacterClassNames.Diplomat) diplomats.Add(c);
                else if (classId == CharacterClassNames.Spy) spies.Add(c);
                else others.Add(c);
            }

            for (int i = 1; i <= 4; i++)
            {
                if (courtSize > i && slots[i] == null && marshals.Count > 0)
                {
                    slots[i] = marshals[0];
                    marshals.RemoveAt(0);
                }
            }

            for (int i = 5; i <= 9; i++)
            {
                if (courtSize > i && slots[i] == null && merchants.Count > 0)
                {
                    slots[i] = merchants[0];
                    merchants.RemoveAt(0);
                }
            }

            if (courtSize > 9 && slots[9] == null && clerics.Count > 0)
            {
                slots[9] = clerics[0];
                clerics.RemoveAt(0);
            }
            if (courtSize > 8 && slots[8] == null && clerics.Count > 0)
            {
                slots[8] = clerics[0];
                clerics.RemoveAt(0);
            }

            if (courtSize > 4 && slots[4] == null && diplomats.Count > 0)
            {
                slots[4] = diplomats[0];
                diplomats.RemoveAt(0);
            }

            var remaining = new System.Collections.Generic.List<Logic.Character>();
            remaining.AddRange(marshals);
            remaining.AddRange(merchants);
            remaining.AddRange(clerics); 
            remaining.AddRange(diplomats);
            remaining.AddRange(spies);
            remaining.AddRange(others);

            for (int i = 1; i < courtSize; i++)
            {
                if (slots[i] == null && remaining.Count > 0)
                {
                    slots[i] = remaining[0];
                    remaining.RemoveAt(0);
                }
            }

            k.court.Clear();
            for (int i = 0; i < courtSize; i++)
            {
                k.court.Add(slots[i]);
            }
            if (remaining.Count > 0)
            {
                k.court.AddRange(remaining);
            }
        }
        // From WarHelper
        public static System.Collections.Generic.List<Logic.Kingdom> GetEnemiesInWar(this Logic.Kingdom kingdom, War war)
        {
            int ourSide = TraverseAPI.GetWarSide(war, kingdom);
            return (ourSide == 0) ? war.defenders : war.attackers;
        }
        
        public static System.Collections.Generic.List<Logic.Kingdom> GetAlliesInWar(this Logic.Kingdom kingdom, War war)
        {
            int ourSide = TraverseAPI.GetWarSide(war, kingdom);
            return (ourSide == 0) ? war.attackers : war.defenders;
        }
        
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

        public static bool HasHighThreat(this Logic.Kingdom k)
        {
            if (k == null || k.realms == null) return false;

            // Iterate through all realms in the kingdom and check their threat level
            foreach (var realm in k.realms)
            {
                if (realm == null || realm.threat == null) continue;
                
                // Level 3 is Level.Attack, 4 is Invaded, 5 is Siege
                if ((int)realm.threat.level >= GameBalance.KingdomSideAttackLevel) 
                    return true;
            }

            return false;
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

            return ourStrength >= enemyStrength * GameBalance.SoloAttackStrengthRatio;
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
            var enemyKingdoms = kingdom.GetEnemiesInWar(war);

            foreach (var enemyKingdom in enemyKingdoms)
            {
                if (enemyKingdom.neighbors != null)
                {
                    foreach (var neighborObj in enemyKingdom.neighbors)
                    {
                        if (neighborObj is Logic.Kingdom neighbor)
                            kingdom.InviteNeighborsToWarTarget(neighbor, war, ai);
                    }
                }
            }

            if (kingdom.neighbors != null)
            {
                foreach (var neighborObj in kingdom.neighbors)
                {
                    if (neighborObj is Logic.Kingdom neighbor)
                        kingdom.InviteNeighborsToWarTarget(neighbor, war, ai);
                }
            }
        }

        private static void InviteNeighborsToWarTarget(this Logic.Kingdom kingdom, Logic.Kingdom targetKingdom, War war, KingdomAI ai)
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
            var armies = k.armies ?? new System.Collections.Generic.List<Logic.Army>();
            if (armies.Count == 0) return true;
            float totalStr = 0;
            foreach (var a in armies) totalStr += a.EvalStrength();
            return totalStr < k.realms.Count * 250f;
        }
    }
}
