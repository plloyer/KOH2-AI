using System;
using System.Collections.Generic;
using Logic;

namespace AIOverhaul
{
    public static class ArmyHelper
    {
        public static int CountRangedUnits(this Logic.Army army)
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

        public static bool IsDamaged(this Logic.Army army)
        {
            if (army.units == null) return false;
            foreach (var u in army.units)
            {
                if (u.damage > 0) return true;
            }
            return false;
        }

        public static float GetArmyHealthPercentage(this Logic.Army army)
        {
            if (army.units == null || army.units.Count == 0) return 0;
            float max = 0;
            foreach(var u in army.units) max += u.def.strength_eval;
            float current = army.EvalStrength();
            return max > 0 ? (current / max) : 0; // Avoid division by zero
        }

        public static bool IsSieging(this Logic.Army army)
        {
            return army != null && army.battle != null && army.battle.type == Logic.Battle.Type.Siege && army.battle.attacker_kingdom == army.GetKingdom();
        }

        public static bool IsHealingNeeded(this Logic.Army army)
        {
            if (army == null || army.units == null || army.units.Count == 0) return false;

            var realm = army.realm_in;
            var owner = army.GetKingdom();
            if (owner == null) return false;

            bool inOwnTerritory = realm != null && realm.GetKingdom() == owner;

            if (inOwnTerritory)
            {
                return IsDamaged(army);
            }
            else
            {
                float healthPerc = GetArmyHealthPercentage(army);
                return healthPerc < GameBalance.HealthRetreatThreshold;
            }
        }

        public static float EvalTotalStrength(this Logic.Army army)
        {
            if (army == null) return 0f;

            float strength = army.EvalStrength();
            
            var kingdom = army.GetKingdom();
            if (kingdom != null)
            {
                var buddy = AIOverhaul.BuddySystem.GetBuddy(army, kingdom);
                if (buddy != null && buddy.IsValid())
                {
                    strength += buddy.EvalStrength();
                }
            }

            return strength;
        }
    }
}
