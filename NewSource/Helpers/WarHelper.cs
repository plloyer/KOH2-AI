using System.Collections.Generic;
using Logic;

namespace AIOverhaul
{
    public static class WarHelper
    {
        public static List<Logic.Kingdom> GetEnemiesInWar(this Logic.Kingdom kingdom, War war)
        {
            int ourSide = TraverseAPI.GetWarSide(war, kingdom);
            return (ourSide == 0) ? war.defenders : war.attackers;
        }
        
        public static List<Logic.Kingdom> GetAlliesInWar(this Logic.Kingdom kingdom, War war)
        {
            int ourSide = TraverseAPI.GetWarSide(war, kingdom);
            return (ourSide == 0) ? war.attackers : war.defenders;
        }
        
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

        /// <summary>
        /// Check if kingdom is in a dominant 1v1 war where solo attacks are viable.
        /// Returns true if: exactly 1 war, war is 1v1, and we're 1.5x stronger.
        /// </summary>
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

        /// <summary>
        /// Check if kingdom has any army currently sieging an enemy castle.
        /// </summary>
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
    }
}
