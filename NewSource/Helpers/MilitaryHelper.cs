using System;

namespace AIOverhaul
{
    public static class MilitaryHelper
    {
        public static bool HasTwoReadyArmies(Logic.Kingdom kingdom)
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

        public static int CountRangedUnits(Logic.Army army)
        {
            int count = 0;
            if (army?.units != null)
            {
                foreach (var unit in army.units)
                {
                    if (unit?.def != null && unit.def.is_ranged)
                        count++;
                }
            }
            return count;
        }

        public static bool IsDamaged(Logic.Army army)
        {
            if (army.units == null) return false;
            foreach (var u in army.units)
            {
                if (u.damage > 0) return true;
            }
            return false;
        }

        public static float GetArmyHealthPercentage(Logic.Army army)
        {
            if (army.units == null || army.units.Count == 0) return 0;
            float max = 0;
            foreach(var u in army.units) max += u.def.strength_eval;
            float current = army.EvalStrength();
            return max > 0 ? (current / max) : 0; // Avoid division by zero
        }

        public static Logic.Army FindEnemyInRealm(Logic.Realm realm, Logic.Kingdom ourKingdom)
        {
            if (realm == null || ourKingdom == null) return null;

            // Iterate through all kingdoms to find enemies
            foreach (var k in ourKingdom.game.kingdoms)
            {
                if (k == null || k == ourKingdom) continue;
                
                // check if at war
                if (!ourKingdom.IsEnemy(k)) continue;

                if (k.armies != null)
                {
                    foreach (var a in k.armies)
                    {
                        if (a.realm_in == realm && a.IsValid())
                        {
                            return a;
                        }
                    }
                }
            }
            return null;
        }
    }
}
