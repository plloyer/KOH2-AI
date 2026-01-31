using System;
using System.Collections.Generic;

namespace AIOverhaul
{
    public static class MilitaryHelper
    {
        /// <summary>
        /// Checks if a target realm is within maxDistance provinces of any of the kingdom's realms.
        /// Uses breadth-first search from all owned realms.
        /// </summary>
        public static bool IsRealmWithinDistance(Logic.Realm targetRealm, Logic.Kingdom ourKingdom, int maxDistance, out int distance)
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
                        if (IsRealmWithinDistance(realm, ourKingdom, maxDistance, out var dist))
                        {
                            return realm;
                        }
                        AIOverhaulPlugin.LogDebug($"[ThinkPlunder] {enemy.Name}'s realm {realm.name} is in disorder but too far ({dist}>{maxDistance} provinces)", LogCategory.Military, ourKingdom);
                    }
                }
            }

            return null;
        }

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
