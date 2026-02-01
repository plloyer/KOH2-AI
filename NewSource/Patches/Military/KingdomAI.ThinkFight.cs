using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // See ARMY_MANAGEMENT_GUIDE.md
    // ThinkFight is the Field Commander. It is called by ThinkArmy when the army is in position. It scans the local province to decide what
    // specifically to attack (an enemy army, a castle, or a village).
    [HarmonyPatch(typeof(KingdomAI), "ThinkFight")]
    public class KingdomAI_ThinkFight
    {
        const string LogPrefix = "[ThinkFight]";
        static bool Prefix(KingdomAI __instance, Logic.Army army, ref bool __result)
        {
            Logic.Realm realmIn = army?.realm_in;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom) || realmIn == null) return true;
            
            // If we are a Follower, we DO NOT decide to fight or retreat independently.
            // We rely entirely on the Leader's decision (propagated via ThinkArmy follow logic).
            if (BuddySystem.IsFollower(army, __instance.kingdom))
            {
                __result = false; 
                return false; 
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

            Logic.Army buddy = BuddySystem.GetBuddy(army, __instance.kingdom);
            bool buddyPresent = false;
            if (buddy?.realm_in == realmIn)
                buddyPresent = true;
            
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
                        // Check if this army should help (enough units or changes outcome)
                        float buddyStr = buddy.EvalStrength();
                        if (!BuddySystem.ShouldBuddyHelp(army, buddyStr, enemyStrength, __instance.kingdom))
                        {
                            AIOverhaulPlugin.LogDebug($"{LogPrefix} Army {army.GetNid()} too weak to help buddy {buddy.GetNid()}, not engaging", LogCategory.Military, __instance.kingdom);
                            // Don't force engage, let vanilla handle it
                        }
                        else
                        {
                            AIOverhaulPlugin.LogDebug($"{LogPrefix} Force engaging to help buddy {buddy.GetNid()} in battle!", LogCategory.Military, __instance.kingdom);
                            __result = true; // Fight!
                            return false; // Skip vanilla calc
                        }
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
                            AIOverhaulPlugin.LogDebug($"{LogPrefix} Leader {army.GetNid()} is waiting for buddy {buddy.GetNid()}", LogCategory.Military, __instance.kingdom);
                            army.Stop();
                            army.SetAIStatus(AIStatusNames.WaitForBuddy);
                            __result = true;
                            return false;
                        }
                    }
                }

                if (winChance < GameBalance.MinBattleWinChance)
                {
                    // If is home and not in castle
                    if (realmIn.kingdom_id == __instance.kingdom.id && army.castle == null)
                    {
                        Castle castle = realmIn.castle;
                        if (castle != null)
                        {
                            if (castle.army == null || castle.army == army)
                            {
                                TraverseAPI.SendArmy(__instance, army, castle, AIStatusNames.RetreatLowChance);
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
