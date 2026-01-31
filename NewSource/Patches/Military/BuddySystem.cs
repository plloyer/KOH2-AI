using System.Collections.Generic;
using System.Linq;

namespace AIOverhaul
{
    public static class BuddySystem
    {
        // Key: Army ID, Value: Buddy Army ID
        public static Dictionary<int, int> buddyMap = new Dictionary<int, int>();

        // Key: Kingdom ID, Value: Last re-evaluation time (real time seconds)
        private static Dictionary<int, float> lastReevalTime = new Dictionary<int, float>();

        // Key: Kingdom ID, Value: Strike force army IDs (the two strongest)
        private static Dictionary<int, (int, int)> strikeForceMap = new Dictionary<int, (int, int)>();

        public static void ClearCache()
        {
            buddyMap.Clear();
            lastReevalTime.Clear();
            strikeForceMap.Clear();
        }

        /// <summary>
        /// Re-evaluate all buddy pairs for a kingdom based on strength.
        /// The two strongest armies become the strike force (buddies).
        /// </summary>
        public static void ReevaluateBuddies(Logic.Kingdom kingdom)
        {
            if (kingdom == null || kingdom.armies == null) return;

            int kingdomId = kingdom.id;
            float currentTime = UnityEngine.Time.time;

            // Check if re-evaluation is needed
            if (lastReevalTime.TryGetValue(kingdomId, out float lastTime))
            {
                float elapsed = currentTime - lastTime;
                if (elapsed < GameBalance.BuddyReevalIntervalMinutes * 60f)
                    return; // Not time yet
            }

            lastReevalTime[kingdomId] = currentTime;

            // Get all valid armies sorted by strength (strongest first)
            var validArmies = kingdom.armies
                .Where(a => a != null && a.IsValid() && !a.IsHiredMercenary())
                .OrderByDescending(a => a.EvalStrength())
                .ToList();

            if (validArmies.Count < GameBalance.MinArmiesForStrikeForce)
            {
                // Not enough armies for strike force
                strikeForceMap.Remove(kingdomId);
                return;
            }

            // Clear old buddy assignments for this kingdom's armies
            foreach (var army in validArmies)
            {
                int armyId = army.GetNid();
                if (buddyMap.ContainsKey(armyId))
                {
                    int oldBuddyId = buddyMap[armyId];
                    buddyMap.Remove(armyId);
                    if (buddyMap.ContainsKey(oldBuddyId) && buddyMap[oldBuddyId] == armyId)
                        buddyMap.Remove(oldBuddyId);
                }
            }

            // Pair the two strongest as the strike force
            var strongest = validArmies[0];
            var secondStrongest = validArmies[1];

            int id1 = strongest.GetNid();
            int id2 = secondStrongest.GetNid();

            buddyMap[id1] = id2;
            buddyMap[id2] = id1;
            strikeForceMap[kingdomId] = (id1, id2);

            AIOverhaulPlugin.LogDebug($"[Buddy] Strike Force formed: {strongest.leader?.Name ?? $"Army#{id1}"} (Str:{strongest.EvalStrength():F0}) + {secondStrongest.leader?.Name ?? $"Army#{id2}"} (Str:{secondStrongest.EvalStrength():F0})", LogCategory.Military, kingdom);
        }

        /// <summary>
        /// Check if an army is part of the strike force (two strongest armies)
        /// </summary>
        public static bool IsStrikeForce(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return false;

            if (strikeForceMap.TryGetValue(kingdom.id, out var pair))
            {
                int armyId = army.GetNid();
                return armyId == pair.Item1 || armyId == pair.Item2;
            }
            return false;
        }

        public static Logic.Army GetBuddy(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return null;

            // Trigger periodic re-evaluation
            ReevaluateBuddies(kingdom);

            int armyId = army.GetNid();

            // Check existing buddy
            if (buddyMap.ContainsKey(armyId))
            {
                int buddyId = buddyMap[armyId];
                var buddy = kingdom.armies.Find(a => a.GetNid() == buddyId);

                // Validate buddy exists and is valid
                if (buddy != null && buddy.IsValid())
                {
                    // For strike force, don't break on distance - they should regroup
                    if (IsStrikeForce(army, kingdom))
                    {
                        return buddy;
                    }

                    // For non-strike force pairs, use distance hysteresis
                    float distSq = buddy.position.SqrDist(army.position);
                    float maxDistSq = GameBalance.BuddyBreakDistance * GameBalance.BuddyBreakDistance;

                    if (distSq <= maxDistSq)
                    {
                        return buddy;
                    }
                }

                // Buddy died, invalid, or too far (non-strike force)
                buddyMap.Remove(armyId);
                if (buddyMap.ContainsKey(buddyId) && buddyMap[buddyId] == armyId)
                    buddyMap.Remove(buddyId);
            }

            return null;
        }

        public static bool IsFollower(Logic.Army army)
        {
            if (army == null) return false;

            // In strike force: weaker army follows stronger army
            if (buddyMap.ContainsKey(army.GetNid()))
            {
                int buddyId = buddyMap[army.GetNid()];
                // Lower ID is the follower (preserves original behavior)
                // This works well since we assign strongest first
                return army.GetNid() < buddyId;
            }
            return false;
        }
    }
}
