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
    }
}
