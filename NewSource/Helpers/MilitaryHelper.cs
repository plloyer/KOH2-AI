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

        public static void FindEnemiesInRealm(this Logic.Realm realm, Logic.Kingdom ourKingdom, List<Logic.Army> armyList)
        {
            if (realm == null || ourKingdom == null || realm.armies == null || armyList == null) return;

            foreach (var army in realm.armies)
            {
                if (army == null || !army.IsValid()) continue;

                var armyOwner = army.GetKingdom();
                if (armyOwner != null && armyOwner != ourKingdom && ourKingdom.IsEnemy(armyOwner))
                    armyList.Add(army);
            }
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
