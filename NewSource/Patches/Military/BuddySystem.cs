using System.Collections.Generic;
using System.Linq;

namespace AIOverhaul
{
    public static class BuddySystem
    {
        // Key: Army ID, Value: Buddy Army ID
        public static Dictionary<int, int> buddyMap = new Dictionary<int, int>();

        public static void ClearCache()
        {
            buddyMap.Clear();
        }

        public static Logic.Army GetBuddy(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return null;
            int armyId = army.GetNid();

            // 1. Check existing buddy
            if (buddyMap.ContainsKey(armyId))
            {
                int buddyId = buddyMap[armyId];
                // Find army object by ID
                var buddy = kingdom.armies.Find(a => a.GetNid() == buddyId);
                
                // Validate buddy exists AND is close enough (hysteresis)
                if (buddy != null && buddy.IsValid())
                {
                    float distSq = buddy.position.SqrDist(army.position);
                    float maxDistSq = GameBalance.BuddyBreakDistance * GameBalance.BuddyBreakDistance;
                    
                    if (distSq <= maxDistSq)
                    {
                        return buddy;
                    }
                }
                
                // Buddy died, invalid, or too far
                buddyMap.Remove(armyId);
                // Also clean up reverse mapping if it existed
                if (buddyMap.ContainsKey(buddyId) && buddyMap[buddyId] == armyId)
                    buddyMap.Remove(buddyId);
            }

            // 2. Assign new buddy if needed
            // Simple logic: Pair with the nearest unpartnered army WITHIN RANGE
            var availableParams = kingdom.armies.Where(a => a != army && a.IsValid() && !buddyMap.ContainsKey(a.GetNid())).ToList();
            if (availableParams.Count > 0)
            {
                // Find nearest
                var nearest = availableParams.OrderBy(a => a.position.SqrDist(army.position)).First();
                float distSq = nearest.position.SqrDist(army.position);
                float maxAssignDistSq = GameBalance.MaxBuddyDistance * GameBalance.MaxBuddyDistance;

                if (distSq <= maxAssignDistSq)
                {
                    buddyMap[armyId] = nearest.GetNid();
                    buddyMap[nearest.GetNid()] = armyId; // Mutual
                    return nearest;
                }
            }

            return null;
        }

        public static bool IsFollower(Logic.Army army)
        {
            // Simple rule: Lower ID follows Higher ID to avoid circular following
            if (buddyMap.ContainsKey(army.GetNid()))
            {
                int buddyId = buddyMap[army.GetNid()];
                // Lower ID is the follower. Higher ID is the leader.
                return army.GetNid() < buddyId;
            }
            return false;
        }
    }
}
