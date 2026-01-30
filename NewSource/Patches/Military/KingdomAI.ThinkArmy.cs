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
            if (army == null || !army.IsValid()) return true;
            if (army.battle != null) return true;

            // Check if any realm is under urgent threat (Invaded or Siege) AND we're stronger
            bool shouldDefendInstead = false;
            float armyStrength = army.EvalStrength();

            if (__instance.kingdom.realms != null)
            {
                foreach (var r in __instance.kingdom.realms)
                {
                    var threat = TraverseAPI.GetThreat(__instance, r);
                    if (threat != null && threat.level >= Logic.KingdomAI.Threat.Level.Invaded)
                    {
                        float enemyStrength = threat.enemies_in.eval;
                        // Only prioritize defense if we're stronger than the enemy
                        if (armyStrength >= enemyStrength)
                        {
                            shouldDefendInstead = true;
                            break;
                        }
                    }
                }
            }

            // If we should defend and are stronger, skip camping - let the army move
            if (shouldDefendInstead)
            {
                return true;
            }

            // 1. In Own Territory
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
            
            // Logic: If we are idle OR if strict buddy system overrides us
            // If we are a Follower, we MUST do what the leader does.
            if (BuddySystem.IsFollower(army))
            {
                Logic.Army leader = BuddySystem.GetBuddy(army, __instance.kingdom);
                if (leader != null && leader.IsValid())
                {
                    // STRICT BUDDY SYSTEM: Always copy target
                    var leaderTargetRealm = leader.GetTargetRealm(); // Use GetTargetRealm() not tgt_realm directly to be safe
                    // Or army.tgt_realm is the strategic target.
                    
                    // 1. Strategic Sync: If Leader is going to a realm, we go too.
                    if (leader.tgt_realm != null && army.tgt_realm != leader.tgt_realm)
                    {
                         // This is handled by AssignArmy Postfix mostly, but if it desyncs:
                         // TraverseAPI.SendArmy(__instance, army, leader.tgt_realm.castle, "follow_buddy_strat", null);
                         // But that sends to castle... we might just correct the tgt_realm variable?
                         // Better to let tactical logic handle the movement.
                    }

                    // 2. Tactical Sync: Copy Leader's concrete target (Enemy Army, Castle, Position)
                    Logic.MapObject leaderTarget = leader.GetTarget(); // The object leader is interacting with
                    
                    // If Leader has a target, we copy it.
                    if (leaderTarget != null && army.GetTarget() != leaderTarget)
                    {
                        TraverseAPI.SendArmy(__instance, army, leaderTarget, "follow_buddy_force", null);
                        return;
                    }
                    
                    // 3. Movement Sync: If Leader is moving (and not to a specific target we already copied), follow him.
                    // This handles empty terrain movement
                    if (leader.movement.IsMoving() && army.GetTarget() != leader && leaderTarget == null)
                    {
                        TraverseAPI.SendArmy(__instance, army, leader, "follow_buddy_move", null);
                        return;
                    }

                    // 4. Idle Sync: If Leader is idle/waiting, we wait near him.
                    // If we are not doing anything, go to leader.
                    if ((status == "idle" || status == "wait_orders") && army.GetTarget() != leader)
                    {
                         TraverseAPI.SendArmy(__instance, army, leader, "follow_buddy_idle", null);
                         return;
                    }
                }
            }
            else
            {
                // LEADER LOGIC: Rescue Follower if they are in trouble
                Logic.Army buddy = BuddySystem.GetBuddy(army, __instance.kingdom);
                if (buddy != null && buddy.IsValid())
                {
                     // If Buddy is in battle and we are NOT, go help!
                     if (buddy.battle != null && army.battle == null)
                     {
                         // We are the leader, our buddy is fighting.
                         // Check distance or feasibility? 
                         // For now, if within reasonable range (e.g. same realm or nearby), RUSH.
                         
                         // Check if we are already moving to the battle
                         var currentTarget = army.GetTarget();
                         // The battle object itself might be the target? Or the enemy army?
                         // buddy.battle is a BattleView object usually associated with location.
                         
                         // Simplest rescue: Move to buddy's position (which is the battle)
                         // But we want to JOIN. SendArmy to buddy should work if buddy is in battle?
                         // Or send to buddy.battle.
                         
                         if (currentTarget != buddy && currentTarget != buddy.battle)
                         {
                             AIOverhaulPlugin.LogDebug($"[ThinkArmy] Leader {army.GetNid()} RESCUING Buddy {buddy.GetNid()} in battle!", LogCategory.Military, __instance.kingdom);
                             TraverseAPI.SendArmy(__instance, army, buddy, "rescue_buddy", null);
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
                    AIOverhaulPlugin.LogDebug($" Idle Knight - Returning to garrison at {nearest.name}", LogCategory.Military, __instance.kingdom);
                    TraverseAPI.SendArmy(__instance, army, nearest, "go_inside", null);
                }
            }
        }
    }
}
