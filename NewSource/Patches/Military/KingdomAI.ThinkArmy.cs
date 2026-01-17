using HarmonyLib;
using AIOverhaul.Constants;

namespace AIOverhaul.Patches.Military
{
    // "ThinkArmy" handles general army tick logic including movement and actions.
    // Intent: ThinkArmy patches (IdleArmyPatch + HealingLogicPatch)
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkArmy")]
    public class KingdomAI_ThinkArmy
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Army army)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            // Removed is_alive, used IsValid() which is standard.
            if (army == null || !army.IsValid()) return true;
            if (army.battle != null) return true;

            // 1. In Own Territory
            // Fix: Realm.kingdom -> Realm.GetKingdom()
            var realm = army.realm_in;
            bool inOwnTerritory = realm != null && realm.GetKingdom() == __instance.kingdom;
            
            bool needsHeal = false;
            if (inOwnTerritory)
            {
                needsHeal = AIOverhaul.Helpers.MilitaryHelper.IsDamaged(army);
            }
            else
            {
                float healthPerc = AIOverhaul.Helpers.MilitaryHelper.GetArmyHealthPercentage(army);
                if (healthPerc < GameBalance.HealthRetreatThreshold)
                {
                    needsHeal = true;
                }
            }

            if (needsHeal)
            {
                var action = army.leader?.FindAction(ActionNames.CampArmy);
                if (action != null && action.Validate() == "ok")
                {
                    action.Execute(null);
                    return false;
                }
            }

            return true;
        }
        
        static void Postfix(Logic.KingdomAI __instance, Logic.Army army)
        {
            if (army == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (army.IsHiredMercenary() || army.battle != null) return;

            string status = army.ai_status;

            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;

            // Access private field via reflection if needed, but usually public?
            // Actually ai_status is likely internal/private. 
            // Assuming "idle" check is correct from decompilation context.
            // If status is not accessible, we skip.
            
            // Logic: If we are idle, follow our buddy (ONLY if we are the follower)
            if (BuddySystem.IsFollower(army))
            {
                Logic.Army leader = BuddySystem.GetBuddy(army, __instance.kingdom);
                if (leader != null && leader.IsValid())
                {
                    // If we are idle or waiting, and leader is doing something, copy them
                    if (status == "idle" || status == "wait_orders" || status == "wait_for_buddy")
                    {
                        // 1. If leader has a specific target (enemy, castle), engage it
                        Logic.MapObject leaderTarget = leader.GetTarget();
                        if (leaderTarget != null && leaderTarget != army.GetTarget())
                        {
                            // Avoid following if leader is just moving to a point (target is null usually for move pos?)
                            // Actually GetTarget returns the interacted object.
                            
                            // Check if leaderTarget is valid target for us
                            TraverseAPI.SendArmy(__instance, army, leaderTarget, "follow_buddy_target", null);
                            return;
                        }

                        // 2. If leader is moving but has no target, follow the leader himself
                        if (leader.movement.IsMoving() && army.GetTarget() != leader)
                        {
                             // LogInfo("Follower {army.GetNid()} following Leader {leader.GetNid()}");
                             TraverseAPI.SendArmy(__instance, army, leader, "follow_buddy_leader", null);
                             return;
                        }
                    }
                }
            }

            if (status == "idle" && army.castle == null)
            {
                Logic.Castle nearest = TraverseAPI.FindNearestOwnCastle(__instance, army, true);
                if (nearest != null)
                {
                    AIOverhaulPlugin.LogInfo($" Idle Knight - Returning to garrison at {nearest.name}", LogCategory.Military);
                    TraverseAPI.SendArmy(__instance, army, nearest, "go_inside", null);
                }
            }
        }
    }
}
