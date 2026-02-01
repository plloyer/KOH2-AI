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

    }
}
