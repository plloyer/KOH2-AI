using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIOverhaul
{
    public static class BuddySystem
    {
        // Key: Army ID, Value: Buddy Army ID
        public static Dictionary<int, int> BuddyMap { get; } = new Dictionary<int, int>();

        // Key: Kingdom ID, Value: Last re-evaluation time (real time seconds)
        static Dictionary<int, float> s_LastReevalTime = new Dictionary<int, float>();

        // Key: Kingdom ID, Value: Strike force army IDs (the two strongest)
        static Dictionary<int, (int, int)> s_StrikeForceMap = new Dictionary<int, (int, int)>();

        public static void ClearCache()
        {
            BuddyMap.Clear();
            s_LastReevalTime.Clear();
            s_StrikeForceMap.Clear();
        }

        /// <summary>
        /// Re-evaluate all buddy pairs for a kingdom based on strength.
        /// The two strongest armies become the strike force (buddies).
        /// </summary>
        public static void ReevaluateBuddies(Logic.Kingdom kingdom)
        {
            if (kingdom == null || kingdom.armies == null) return;

            int kingdomId = kingdom.id;
            float currentTime = Time.time;

            // Check if re-evaluation is needed
            if (s_LastReevalTime.TryGetValue(kingdomId, out float lastTime))
            {
                float elapsed = currentTime - lastTime;
                if (elapsed < GameBalance.BuddyReevalIntervalMinutes * 60f)
                    return; // Not time yet
            }

            s_LastReevalTime[kingdomId] = currentTime;

            // Get all valid armies sorted by strength (strongest first)
            var validArmies = kingdom.armies
                .Where(a => a != null && a.IsValid() && !a.IsHiredMercenary())
                .OrderByDescending(a => a.EvalStrength())
                .ToList();

            if (validArmies.Count < GameBalance.MinArmiesForStrikeForce)
            {
                // Not enough armies for strike force
                s_StrikeForceMap.Remove(kingdomId);
                return;
            }

            // Clear old buddy assignments for this kingdom's armies
            foreach (var army in validArmies)
            {
                int armyId = army.GetNid();
                if (BuddyMap.ContainsKey(armyId))
                {
                    int oldBuddyId = BuddyMap[armyId];
                    BuddyMap.Remove(armyId);
                    if (BuddyMap.ContainsKey(oldBuddyId) && BuddyMap[oldBuddyId] == armyId)
                        BuddyMap.Remove(oldBuddyId);
                }
            }

            // Pair the two strongest as the strike force
            var strongest = validArmies[0];
            var secondStrongest = validArmies[1];

            int id1 = strongest.GetNid();
            int id2 = secondStrongest.GetNid();

            BuddyMap[id1] = id2;
            BuddyMap[id2] = id1;
            s_StrikeForceMap[kingdomId] = (id1, id2);

            AIOverhaulPlugin.LogDebug($"[Buddy] Strike Force formed: {strongest.leader?.Name ?? $"Army#{id1}"} (Str:{strongest.EvalStrength():F0}) + {secondStrongest.leader?.Name ?? $"Army#{id2}"} (Str:{secondStrongest.EvalStrength():F0})", LogCategory.Military, kingdom);
        }

        /// <summary>
        /// Check if an army is part of the strike force (two strongest armies)
        /// </summary>
        public static bool IsStrikeForce(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return false;

            if (s_StrikeForceMap.TryGetValue(kingdom.id, out var pair))
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
            if (BuddyMap.ContainsKey(armyId))
            {
                int buddyId = BuddyMap[armyId];
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
                BuddyMap.Remove(armyId);
                if (BuddyMap.ContainsKey(buddyId) && BuddyMap[buddyId] == armyId)
                    BuddyMap.Remove(buddyId);
            }

            return null;
        }

        public static bool IsFollower(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return false;

            // In strike force: weaker army follows stronger army
            if (BuddyMap.ContainsKey(army.GetNid()))
            {
                int buddyId = BuddyMap[army.GetNid()];
                var buddy = kingdom.armies?.Find(a => a.GetNid() == buddyId);

                if (buddy == null || !buddy.IsValid()) return false;

                // Strongest is the leader, weaker follows
                return army.EvalStrength() < buddy.EvalStrength();
            }
            return false;
        }

        /// <summary>
        /// Check if a buddy should be sent to help in battle.
        /// - If enough units (MinBuddyUnitsToHelp): always help
        /// - If fewer units: only help if it changes battle outcome
        /// </summary>
        /// <param name="buddy">The army considering joining</param>
        /// <param name="friendlyStrength">Current friendly strength in the fight</param>
        /// <param name="enemyStrength">Current enemy strength in the fight</param>
        /// <param name="kingdom">Kingdom for logging</param>
        public static bool ShouldBuddyHelp(Logic.Army buddy, float friendlyStrength, float enemyStrength, Logic.Kingdom kingdom = null)
        {
            if (buddy == null || buddy.units == null) return false;

            int unitCount = buddy.units.Count;
            string buddyName = buddy.leader?.Name ?? $"Army#{buddy.GetNid()}";

            // Enough units: always help
            if (unitCount >= GameBalance.MinBuddyUnitsToHelp)
            {
                AIOverhaulPlugin.LogDebug($"[Buddy] {buddyName} has {unitCount} units (>={GameBalance.MinBuddyUnitsToHelp}), will help", LogCategory.Military, kingdom);
                return true;
            }

            // Fewer units: only help if it changes the outcome
            if (enemyStrength <= 0) return false;

            float buddyStrength = buddy.EvalStrength();

            // Calculate win chance without buddy
            float winChanceWithout = friendlyStrength / (friendlyStrength + enemyStrength);

            // Calculate win chance with buddy
            float winChanceWith = (friendlyStrength + buddyStrength) / (friendlyStrength + buddyStrength + enemyStrength);

            // Only send if it changes from losing to winning
            bool wouldLoseWithout = winChanceWithout < GameBalance.MinBattleWinChance;
            bool wouldWinWith = winChanceWith >= GameBalance.MinBattleWinChance;
            bool changesOutcome = wouldLoseWithout && wouldWinWith;

            AIOverhaulPlugin.LogDebug($"[Buddy] {buddyName} has {unitCount} units (<{GameBalance.MinBuddyUnitsToHelp}), " +
                $"WinChance: {winChanceWithout:P0}->{winChanceWith:P0}, ChangesOutcome={changesOutcome}", LogCategory.Military, kingdom);

            return changesOutcome;
        }
    }
}
