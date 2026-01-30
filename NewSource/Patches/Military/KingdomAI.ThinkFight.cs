using HarmonyLib;
using AIOverhaul.Constants;

namespace AIOverhaul.Patches.Military
{
    // "ThinkFight" controls whether an army should engage in battle or retreat.
    // Intent: BattleEngagementPatch
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkFight")]
    public class KingdomAI_ThinkFight
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Army army, ref bool __result)
        {
            if (army == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            Logic.Realm realmIn = army.realm_in;
            if (realmIn == null) 
            {
                // ADDED NULL CHECK
                return true; 
            }

            float ownStrength = 0;
            float friendStrength = 0;
            float enemyStrength = 0;

            if (realmIn.armies != null)
            {
                foreach (var a in realmIn.armies)
                {
                    if (a == null) continue;
                    int aKingdomId = a.kingdom_id;
                    int aStrength = a.EvalStrength();
                    if (aKingdomId == __instance.kingdom.id)
                    {
                        if (a == army) ownStrength += aStrength;
                        else friendStrength += aStrength;
                    }
                    else if (__instance.kingdom.IsEnemy(aKingdomId))
                        enemyStrength += aStrength;
                }
            }

            Logic.Army buddy = null;
            if (AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom))
            {
                buddy = BuddySystem.GetBuddy(army, __instance.kingdom);
                if (buddy != null && buddy.battle != null && buddy.battle != army.battle)
                {
                    // Buddy is in a different battle, maybe join them?
                    // This creates a tendency to swarm
                    // For now, just logging or minor influence
                }
            }
            bool buddyPresent = false; // Disabled
            if (buddy != null)
            {
                if (buddy.realm_in == realmIn) buddyPresent = true;
            }

            // OSCILLATION FIX:
            // If we are a Follower, we DO NOT decide to fight or retreat independently.
            // We rely entirely on the Leader's decision (propagated via ThinkArmy follow logic).
            // If we run this logic, we might decide to "Wait for Buddy" (circular) or Retreat when Leader attacks.
            if (BuddySystem.IsFollower(army))
            {
                // Verify leader is actually alive/valid before skipping (already checked in IsFollower/GetBuddy somewhat, but let's be safe)
                // If we skip here (__result = false, return false), we tell ThinkArmy "I didn't do combat logic".
                // ThinkArmy then proceeds. Our ThinkArmy Postfix then forces us to follow the Leader's target.
                __result = false; 
                return false; 
            }

            if (ownStrength + friendStrength + enemyStrength > 0)
            {
                float totalFriendly = ownStrength + friendStrength;
                float winChance = totalFriendly / (totalFriendly + enemyStrength);

                if (buddy != null && !buddyPresent && enemyStrength > 0)
                {
                    // CRITICAL RESCUE LOGIC:
                    // If buddy is already fighting, we MUST engage to help them.
                    // Do not wait, do not retreat.
                    if (buddy.battle != null)
                    {
                        AIOverhaulPlugin.LogDebug($"[ThinkFight] Force engaging to help buddy {buddy.GetNid()} in battle!", LogCategory.Military, __instance.kingdom);
                        __result = true; // Fight!
                        return false; // Skip vanilla calc
                    }

                    // Check if buddy is available to help
                    bool isBuddyAvailable = buddy.battle == null;
                    // Check distance
                    float distToBuddySq = buddy.position.SqrDist(army.position);
                    float maxWaitDistSq = GameBalance.BuddyWaitDistance * GameBalance.BuddyWaitDistance;
                    bool isBuddyClose = distToBuddySq <= maxWaitDistSq;

                    if (isBuddyAvailable && isBuddyClose)
                    {
                        float soloWinChance = ownStrength / (ownStrength + enemyStrength);
                        if (soloWinChance < GameBalance.MinBattleWinChance && winChance >= GameBalance.MinBattleWinChance)
                        {
                            army.Stop();
                            army.ai_status = "wait_for_buddy";
                            __result = true;
                            return false;
                        }
                    }
                }

                if (winChance < GameBalance.MinBattleWinChance)
                {
                    if (realmIn.kingdom_id == __instance.kingdom.id && army.castle == null)
                    {
                        Logic.Castle castle = realmIn.castle;
                        if (castle != null)
                        {
                            if (castle.army == null || castle.army == army)
                            {
                                TraverseAPI.SendArmy(__instance, army, castle, "retreat_low_chance", null);
                                __result = true;
                                return false;
                            }
                        }
                    }

                    __result = false;
                    return false;
                }
            }

            return true;
        }
    }
}
