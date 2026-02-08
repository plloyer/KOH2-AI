using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIOverhaul
{
    public static class BuddySystem
    {
        public struct BuddyPair
        {
            public int LeaderId;
            public int FollowerId;
        }

        // Key: Kingdom ID, Value: List of buddy pairs
        static Dictionary<int, List<BuddyPair>> s_Pairs = new Dictionary<int, List<BuddyPair>>();

        // Key: Kingdom ID, Value: Last re-evaluation time (real time seconds)
        static Dictionary<int, float> s_LastReevalTime = new Dictionary<int, float>();

        public static void ClearCache()
        {
            s_Pairs.Clear();
            s_LastReevalTime.Clear();
        }

        /// <summary>
        /// Evaluate and form buddy pairs for a kingdom.
        /// Pairs sorted by knight class level (highest level marshals form first pair).
        /// </summary>
        public static void EvaluatePairs(Logic.Kingdom kingdom)
        {
            if (kingdom == null || kingdom.armies == null) return;

            int kingdomId = kingdom.id;
            float currentTime = Time.time;

            if (s_LastReevalTime.TryGetValue(kingdomId, out float lastTime))
            {
                if (currentTime - lastTime < GameBalance.BuddyReevalIntervalMinutes * 60f)
                    return;
            }

            s_LastReevalTime[kingdomId] = currentTime;

            // Get valid, non-mercenary armies sorted by leader class level descending
            var validArmies = kingdom.armies
                .Where(a => a != null && a.IsValid() && !a.IsHiredMercenary() && a.leader != null)
                .OrderByDescending(a => a.leader.GetClassLevel())
                .ToList();

            var pairs = new List<BuddyPair>();

            // Form pairs: armies[0]+[1] = pair 1, armies[2]+[3] = pair 2
            int maxPairs = Math.Min(GameBalance.MaxBuddyPairs, validArmies.Count / 2);
            for (int i = 0; i < maxPairs; i++)
            {
                int idx = i * 2;
                if (idx + 1 >= validArmies.Count) break;

                var first = validArmies[idx];
                var second = validArmies[idx + 1];

                // Leader = higher class level (first is already higher due to sort)
                var pair = new BuddyPair
                {
                    LeaderId = first.GetNid(),
                    FollowerId = second.GetNid()
                };
                pairs.Add(pair);

                string leaderName = first.leader?.Name ?? $"Army#{pair.LeaderId}";
                string followerName = second.leader?.Name ?? $"Army#{pair.FollowerId}";
                AIOverhaulPlugin.LogDebug($"[Buddy] Pair {i + 1}: {leaderName} (Lv{first.leader.GetClassLevel()}) + {followerName} (Lv{second.leader.GetClassLevel()})", LogCategory.Military, kingdom);
            }

            s_Pairs[kingdomId] = pairs;
        }

        public static bool IsLeader(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return false;
            if (!s_Pairs.TryGetValue(kingdom.id, out var pairs)) return false;

            int armyId = army.GetNid();
            foreach (var pair in pairs)
            {
                if (pair.LeaderId == armyId) return true;
            }
            return false;
        }

        public static bool IsFollower(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return false;
            if (!s_Pairs.TryGetValue(kingdom.id, out var pairs)) return false;

            int armyId = army.GetNid();
            foreach (var pair in pairs)
            {
                if (pair.FollowerId == armyId) return true;
            }
            return false;
        }

        public static Logic.Army GetBuddy(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return null;
            if (!s_Pairs.TryGetValue(kingdom.id, out var pairs)) return null;

            int armyId = army.GetNid();
            foreach (var pair in pairs)
            {
                if (pair.LeaderId == armyId)
                    return kingdom.armies?.Find(a => a.GetNid() == pair.FollowerId);
                if (pair.FollowerId == armyId)
                    return kingdom.armies?.Find(a => a.GetNid() == pair.LeaderId);
            }
            return null;
        }

        /// <summary>
        /// Get the leader of the pair this army belongs to.
        /// Returns null if army is not in a pair.
        /// </summary>
        public static Logic.Army GetLeader(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return null;
            if (!s_Pairs.TryGetValue(kingdom.id, out var pairs)) return null;

            int armyId = army.GetNid();
            foreach (var pair in pairs)
            {
                if (pair.FollowerId == armyId)
                    return kingdom.armies?.Find(a => a.GetNid() == pair.LeaderId);
                if (pair.LeaderId == armyId)
                    return army; // army IS the leader
            }
            return null;
        }

        /// <summary>
        /// Get pair index: 0 = first pair, 1 = second pair, -1 = not paired
        /// </summary>
        public static int GetPairIndex(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return -1;
            if (!s_Pairs.TryGetValue(kingdom.id, out var pairs)) return -1;

            int armyId = army.GetNid();
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].LeaderId == armyId || pairs[i].FollowerId == armyId)
                    return i;
            }
            return -1;
        }

        public static bool IsFirstPair(Logic.Army army, Logic.Kingdom kingdom)
        {
            return GetPairIndex(army, kingdom) == 0;
        }

        /// <summary>
        /// Get a label for the army's buddy role: L1, F1, L2, F2, or --
        /// </summary>
        public static string GetRoleLabel(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return "--";
            if (!s_Pairs.TryGetValue(kingdom.id, out var pairs)) return "--";

            int armyId = army.GetNid();
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].LeaderId == armyId)
                    return $"L{i + 1}";
                if (pairs[i].FollowerId == armyId)
                    return $"F{i + 1}";
            }
            return "--";
        }
    }
}
