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

        /// <summary>
        /// Finds the closest enemy realm in disorder that is within maxDistance of our realms.
        /// Returns null if no such realm exists.
        /// </summary>
        public static Logic.Realm FindNearbyEnemyRealmInDisorder(Logic.Kingdom ourKingdom, int maxDistance)
        {
            if (ourKingdom == null || ourKingdom.game == null) return null;

            foreach (var enemy in ourKingdom.game.kingdoms)
            {
                if (enemy == null || enemy == ourKingdom || enemy.IsDefeated()) continue;
                if (!ourKingdom.IsEnemy(enemy)) continue;

                if (enemy.realms != null)
                {
                    foreach (var realm in enemy.realms)
                    {
                        if (realm == null) continue;
                        if (!realm.IsDisorder()) continue;

                        // Check castle is attackable
                        var castle = realm.castle;
                        if (castle == null || castle.battle != null) continue;

                        // Check if within distance
                        if (ourKingdom.IsRealmWithinDistance(realm, maxDistance, out var dist))
                            return realm;
                        
                        AIOverhaulPlugin.LogDebug($"[ThinkPlunder] {enemy.Name}'s realm {realm.name} is in disorder but too far ({dist}>{maxDistance} provinces)", LogCategory.Military, ourKingdom);
                    }
                }
            }

            return null;
        }

    }
}
