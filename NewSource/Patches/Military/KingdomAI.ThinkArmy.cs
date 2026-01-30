using HarmonyLib;
using AIOverhaul.Constants;

namespace AIOverhaul.Patches.Military
{
    // "ThinkArmy" handles general army tick logic including movement and actions.
    // Intent: ThinkArmy patches (IdleArmyPatch + HealingLogicPatch)
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkArmy")]
    public class KingdomAI_ThinkArmy
    {
        private const float SallyOutStrengthRatio = 1.1f;

        static bool Prefix(Logic.KingdomAI __instance, Logic.Army army)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            if (army == null || !army.IsValid()) return true;
            if (army.battle != null) return true;

            // Check if any realm is under urgent threat (Invaded or Siege) AND we're stronger
            if (__instance.kingdom.realms == null) return true;

            float armyStrength = army.EvalStrength();

            foreach (var r in __instance.kingdom.realms)
            {
                var threat = TraverseAPI.GetThreat(__instance, r);
                if (threat == null || threat.level < Logic.KingdomAI.Threat.Level.Invaded)
                    continue;

                float enemyStrength = threat.enemies_in.eval;

                // Log siege situations for debugging
                if (threat.level == Logic.KingdomAI.Threat.Level.Siege)
                {
                    AIOverhaulPlugin.LogDebug($"[ThinkArmy] SIEGE detected at {r.name}! Army {army.GetNid()} Str: {armyStrength:F0}, Enemy: {enemyStrength:F0}, Stronger: {armyStrength >= enemyStrength}", LogCategory.Military, __instance.kingdom);
                }

                // Only prioritize defense if we're stronger than the enemy
                if (armyStrength < enemyStrength)
                    continue;

                // Override: Instead of yielding to vanilla (which might camp for recruits), FORCE ATTACK

                // 1. Check for Siege Battle
                if (threat.level == Logic.KingdomAI.Threat.Level.Siege && r.castle != null)
                {
                    var siegeBattle = Traverse.Create(r.castle).Field("battle").GetValue<Logic.Battle>();
                    if (siegeBattle != null && army.battle != siegeBattle)
                    {
                        AIOverhaulPlugin.LogDebug($"[ThinkArmy] FORCE DEFEND: Army {army.GetNid()} breaking siege at {r.name}! (Str: {armyStrength:F0} vs {enemyStrength:F0})", LogCategory.Military, __instance.kingdom);
                        TraverseAPI.SendArmy(__instance, army, siegeBattle, "attack", null);
                        return false;
                    }

                    // No siege battle object - try to find and attack the besieging army directly
                    var besiegingArmy = AIOverhaul.Helpers.MilitaryHelper.FindEnemyInRealm(r, __instance.kingdom);
                    if (besiegingArmy != null && army.GetTarget() != besiegingArmy)
                    {
                        AIOverhaulPlugin.LogDebug($"[ThinkArmy] FORCE DEFEND: Army {army.GetNid()} attacking besieging army at {r.name}! (Str: {armyStrength:F0} vs {enemyStrength:F0})", LogCategory.Military, __instance.kingdom);
                        TraverseAPI.SendArmy(__instance, army, besiegingArmy, "attack", null);
                        return false;
                    }
                }

                // 2. Check for Invading Army
                var invadingArmy = AIOverhaul.Helpers.MilitaryHelper.FindEnemyInRealm(r, __instance.kingdom);
                if (invadingArmy != null && army.GetTarget() != invadingArmy)
                {
                    AIOverhaulPlugin.LogDebug($"[ThinkArmy] FORCE DEFEND: Army {army.GetNid()} intercepting invader {invadingArmy.GetNid()} at {r.name}! (Str: {armyStrength:F0} vs {enemyStrength:F0})", LogCategory.Military, __instance.kingdom);
                    TraverseAPI.SendArmy(__instance, army, invadingArmy, "attack", null);
                    return false;
                }
            }

            // In Own Territory
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

            // Logic: If we are idle OR if strict buddy system overrides us
            // If we are a Follower, we MUST do what the leader does.
            if (BuddySystem.IsFollower(army))
            {
                Logic.Army leader = BuddySystem.GetBuddy(army, __instance.kingdom);
                if (leader != null && leader.IsValid())
                {
                    // If leader is garrisoned (inside a castle), check if castle is under siege
                    if (leader.castle != null)
                    {
                        var leaderRealm = leader.castle.GetRealm();
                        if (leaderRealm != null)
                        {
                            var threat = TraverseAPI.GetThreat(__instance, leaderRealm);
                            if (threat != null && threat.level == Logic.KingdomAI.Threat.Level.Siege)
                            {
                                // Leader's castle is under siege - follower should help break it!
                                var siegeBattle = Traverse.Create(leader.castle).Field("battle").GetValue<Logic.Battle>();
                                if (siegeBattle != null && army.GetTarget() != siegeBattle)
                                {
                                    AIOverhaulPlugin.LogDebug($"[ThinkArmy] Follower {army.GetNid()} rushing to break siege on leader's castle {leader.castle.name}!", LogCategory.Military, __instance.kingdom);
                                    TraverseAPI.SendArmy(__instance, army, siegeBattle, "rescue_leader_siege", null);
                                    return;
                                }
                            }
                        }
                        // Leader is idle in castle (not under siege), follower does its own thing
                        return;
                    }

                    // 1. Tactical Sync: Copy Leader's concrete target (Enemy Army, Castle, Position)
                    Logic.MapObject leaderTarget = leader.GetTarget(); // The object leader is interacting with

                    // If Leader has a target, we copy it.
                    if (leaderTarget != null && army.GetTarget() != leaderTarget)
                    {
                        TraverseAPI.SendArmy(__instance, army, leaderTarget, "follow_buddy_force", null);
                        return;
                    }

                    // 2. Movement Sync: If Leader is moving (and not to a specific target we already copied), follow him.
                    // This handles empty terrain movement
                    if (leader.movement.IsMoving() && army.GetTarget() != leader && leaderTarget == null)
                    {
                        TraverseAPI.SendArmy(__instance, army, leader, "follow_buddy_move", null);
                        return;
                    }

                    // 3. If leader is idle (no target, not moving), follower should also be idle
                    // Do NOT chase the leader - just let the follower do its own thing (return to garrison, etc.)
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


                // SALLY OUT LOGIC (For Garrisoned Armies)
                if (army.castle != null)
                {
                    var realm = army.castle.GetRealm();
                    if (realm != null)
                    {
                        var threat = TraverseAPI.GetThreat(__instance, realm);
                        // Check if under Siege
                        if (threat != null && threat.level == Logic.KingdomAI.Threat.Level.Siege)
                        {
                            float myStrength = army.EvalStrength();
                            float enemyStrength = threat.enemies_in.eval;

                            // If we are stronger (with buffer), attack!
                            if (enemyStrength > 0 && myStrength > enemyStrength * SallyOutStrengthRatio)
                            {
                                // Find the battle (Siege) attached to the castle
                                // Using Reflection to access 'battle' field on Castle/Settlement to be safe
                                var siegeBattle = Traverse.Create(army.castle).Field("battle").GetValue<Logic.Battle>();
                                
                                if (siegeBattle != null)
                                {
                                    AIOverhaulPlugin.LogDebug($"[ThinkArmy] Garrison {army.GetNid()} SALLYING OUT from {army.castle.name}! (Str: {myStrength:F0} vs {enemyStrength:F0})", LogCategory.Military, __instance.kingdom);
                                    TraverseAPI.SendArmy(__instance, army, siegeBattle, "attack", null);
                                    return;
                                }
                            }
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
