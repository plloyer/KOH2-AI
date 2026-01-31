using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "ThinkArmy" handles general army tick logic including movement and actions.
    // Intent: ThinkArmy patches (IdleArmyPatch + HealingLogicPatch)
    [HarmonyPatch(typeof(KingdomAI), "ThinkArmy")]
    public class KingdomAI_ThinkArmy
    {
        const float SallyOutStrengthRatio = 1.1f;

        static bool Prefix(KingdomAI __instance, Logic.Army army)
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
                if (threat == null || threat.level < KingdomAI.Threat.Level.Invaded)
                    continue;

                float enemyStrength = threat.enemies_in.eval;

                // Log siege situations for debugging
                if (threat.level == KingdomAI.Threat.Level.Siege)
                {
                    AIOverhaulPlugin.LogDebug($"[ThinkArmy] SIEGE detected at {r.name}! Army {army.GetNid()} Str: {armyStrength:F0}, Enemy: {enemyStrength:F0}, Stronger: {armyStrength >= enemyStrength}", LogCategory.Military, __instance.kingdom);
                }

                // Only prioritize defense if we're stronger than the enemy
                if (armyStrength < enemyStrength)
                    continue;

                // Override: Instead of yielding to vanilla (which might camp for recruits), FORCE ATTACK

                // 1. Check for Siege Battle
                if (threat.level == KingdomAI.Threat.Level.Siege && r.castle != null)
                {
                    var siegeBattle = Traverse.Create(r.castle).Field("battle").GetValue<Logic.Battle>();
                    if (siegeBattle != null && army.battle != siegeBattle)
                    {
                        AIOverhaulPlugin.LogDebug($"[ThinkArmy] FORCE DEFEND: Army {army.GetNid()} breaking siege at {r.name}! (Str: {armyStrength:F0} vs {enemyStrength:F0})", LogCategory.Military, __instance.kingdom);
                        TraverseAPI.SendArmy(__instance, army, siegeBattle, "attack");
                        return false;
                    }

                    // No siege battle object - try to find and attack the besieging army directly
                    var besiegingArmy = MilitaryHelper.FindEnemyInRealm(r, __instance.kingdom);
                    if (besiegingArmy != null && army.GetTarget() != besiegingArmy)
                    {
                        AIOverhaulPlugin.LogDebug($"[ThinkArmy] FORCE DEFEND: Army {army.GetNid()} attacking besieging army at {r.name}! (Str: {armyStrength:F0} vs {enemyStrength:F0})", LogCategory.Military, __instance.kingdom);
                        TraverseAPI.SendArmy(__instance, army, besiegingArmy, "attack");
                        return false;
                    }
                }

                // 2. Check for Invading Army
                var invadingArmy = MilitaryHelper.FindEnemyInRealm(r, __instance.kingdom);
                if (invadingArmy != null && army.GetTarget() != invadingArmy)
                {
                    AIOverhaulPlugin.LogDebug($"[ThinkArmy] FORCE DEFEND: Army {army.GetNid()} intercepting invader {invadingArmy.GetNid()} at {r.name}! (Str: {armyStrength:F0} vs {enemyStrength:F0})", LogCategory.Military, __instance.kingdom);
                    TraverseAPI.SendArmy(__instance, army, invadingArmy, "attack");
                    return false;
                }
            }

            // In Own Territory
            var realm = army.realm_in;
            bool inOwnTerritory = realm != null && realm.GetKingdom() == __instance.kingdom;

            bool needsHeal = false;
            if (inOwnTerritory)
            {
                needsHeal = MilitaryHelper.IsDamaged(army);
            }
            else
            {
                float healthPerc = MilitaryHelper.GetArmyHealthPercentage(army);
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
        
        static void Postfix(KingdomAI __instance, Logic.Army army)
        {
            if (army == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (army.IsHiredMercenary() || army.battle != null) return;

            string status = army.ai_status;
            string armyName = army.leader?.Name ?? $"Army#{army.GetNid()}";

            // Log current army state
            var currentTarget = army.GetTarget();
            string currentTargetName = DescribeTarget(currentTarget);
            string tgtRealmName = army.tgt_realm?.name ?? "None";
            bool isMoving = army.movement?.IsMoving() ?? false;

            AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: Status={status}, InCastle={army.castle?.name ?? "No"}, Target={currentTargetName}, TgtRealm={tgtRealmName}, Moving={isMoving}", LogCategory.Military, __instance.kingdom);

            // Check buddy relationship
            bool isFollower = BuddySystem.IsFollower(army, __instance.kingdom);
            Logic.Army buddy = BuddySystem.GetBuddy(army, __instance.kingdom);

            if (buddy == null)
            {
                AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: No buddy assigned", LogCategory.Military, __instance.kingdom);
            }
            else
            {
                string buddyName = buddy.leader?.Name ?? $"Army#{buddy.GetNid()}";
                string role = isFollower ? "FOLLOWER" : "LEADER";
                AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: Role={role}, Buddy={buddyName}", LogCategory.Military, __instance.kingdom);
            }

            if (isFollower && buddy != null && buddy.IsValid())
            {
                string leaderName = buddy.leader?.Name ?? $"Army#{buddy.GetNid()}";

                // Log leader state
                var leaderTarget = buddy.GetTarget();
                string leaderTargetName = DescribeTarget(leaderTarget);
                string leaderTgtRealm = buddy.tgt_realm?.name ?? "None";
                bool leaderMoving = buddy.movement?.IsMoving() ?? false;
                bool leaderInCastle = buddy.castle != null;

                AIOverhaulPlugin.LogDebug($"[Buddy] Leader {leaderName} state: InCastle={buddy.castle?.name ?? "No"}, Target={leaderTargetName}, TgtRealm={leaderTgtRealm}, Moving={leaderMoving}", LogCategory.Military, __instance.kingdom);

                // If leader is garrisoned (inside a castle), check if castle is under siege
                if (leaderInCastle)
                {
                    var leaderRealm = buddy.castle.GetRealm();
                    if (leaderRealm != null)
                    {
                        var threat = TraverseAPI.GetThreat(__instance, leaderRealm);
                        if (threat != null && threat.level == KingdomAI.Threat.Level.Siege)
                        {
                            var siegeBattle = Traverse.Create(buddy.castle).Field("battle").GetValue<Logic.Battle>();
                            if (siegeBattle != null && army.GetTarget() != siegeBattle)
                            {
                                // Check if this army should help (enough units or changes outcome)
                                float friendlyStr = buddy.EvalStrength();
                                float enemyStr = threat.enemies_in.eval;
                                if (!BuddySystem.ShouldBuddyHelp(army, friendlyStr, enemyStr, __instance.kingdom))
                                {
                                    AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: Skipping rescue_leader_siege - army too weak to impact", LogCategory.Military, __instance.kingdom);
                                    return;
                                }
                                AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: ACTION=rescue_leader_siege (Leader's castle {buddy.castle.name} under siege!)", LogCategory.Military, __instance.kingdom);
                                TraverseAPI.SendArmy(__instance, army, siegeBattle, "rescue_leader_siege");
                                return;
                            }
                        }
                    }
                    AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: Leader in castle (no siege) -> follower acts independently", LogCategory.Military, __instance.kingdom);
                    return;
                }

                // PRIORITY 1: If leader is in a battle (attacking settlement/district), join that battle
                if (buddy.battle != null && army.battle == null)
                {
                    // Leader is in a battle - check if we should join
                    float friendlyStr = buddy.EvalStrength();
                    float enemyStr = 0;

                    // Get enemy strength from the battle's realm
                    var battleRealm = buddy.realm_in ?? buddy.tgt_realm;
                    if (battleRealm?.armies != null)
                    {
                        foreach (var a in battleRealm.armies)
                        {
                            if (a != null && __instance.kingdom.IsEnemy(a.kingdom_id))
                                enemyStr += a.EvalStrength();
                        }
                    }

                    if (!BuddySystem.ShouldBuddyHelp(army, friendlyStr, enemyStr, __instance.kingdom))
                    {
                        AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: Skipping join_leader_battle - army too weak to impact", LogCategory.Military, __instance.kingdom);
                    }
                    else if (army.GetTarget() != buddy.battle && army.GetTarget() != buddy)
                    {
                        AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: ACTION=join_leader_battle (Leader is in battle!)", LogCategory.Military, __instance.kingdom);
                        TraverseAPI.SendArmy(__instance, army, buddy.battle, "join_leader_battle");
                        return;
                    }
                }

                // Check if leader's target is a military target
                bool isMilitaryTarget = false;
                string targetType = "None";
                if (leaderTarget != null)
                {
                    if (leaderTarget is Logic.Battle)
                    {
                        isMilitaryTarget = true;
                        targetType = "Battle";
                    }
                    else if (leaderTarget is Logic.Army targetArmy)
                    {
                        bool isEnemy = targetArmy.IsEnemy(__instance.kingdom);
                        isMilitaryTarget = isEnemy;
                        targetType = isEnemy ? "EnemyArmy" : "FriendlyArmy";
                    }
                    else if (leaderTarget is Castle targetCastle)
                    {
                        var castleKingdom = targetCastle.GetKingdom();
                        bool isEnemy = castleKingdom != null && castleKingdom.IsEnemy(__instance.kingdom.id);
                        isMilitaryTarget = isEnemy;
                        targetType = isEnemy ? "EnemyCastle" : "FriendlyCastle";
                    }
                    else
                    {
                        targetType = leaderTarget.GetType().Name;
                    }
                }

                AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: Leader target type={targetType}, IsMilitary={isMilitaryTarget}", LogCategory.Military, __instance.kingdom);

                // Only copy target if it's a military target
                if (isMilitaryTarget && army.GetTarget() != leaderTarget)
                {
                    // Estimate enemy strength based on target type
                    float friendlyStr = buddy.EvalStrength();
                    float enemyStr = 0;
                    if (leaderTarget is Logic.Army targetArmy2)
                    {
                        enemyStr = targetArmy2.EvalStrength();
                    }
                    else if (leaderTarget is Logic.Battle || leaderTarget is Castle)
                    {
                        // For battles/castles, check the realm for enemy armies
                        var targetRealm = buddy.tgt_realm ?? buddy.realm_in;
                        if (targetRealm != null && targetRealm.armies != null)
                        {
                            foreach (var a in targetRealm.armies)
                            {
                                if (a != null && __instance.kingdom.IsEnemy(a.kingdom_id))
                                    enemyStr += a.EvalStrength();
                            }
                        }
                    }

                    // Check if this army should help (enough units or changes outcome)
                    if (!BuddySystem.ShouldBuddyHelp(army, friendlyStr, enemyStr, __instance.kingdom))
                    {
                        AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: Skipping follow_buddy_force - army too weak to impact", LogCategory.Military, __instance.kingdom);
                    }
                    else
                    {
                        AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: ACTION=follow_buddy_force (copying leader's military target)", LogCategory.Military, __instance.kingdom);
                        TraverseAPI.SendArmy(__instance, army, leaderTarget, "follow_buddy_force");
                        return;
                    }
                }

                // Movement Sync: Follow if leader is moving toward enemy territory
                if (leaderMoving && leaderTarget == null && buddy.tgt_realm != null)
                {
                    var tgtRealmKingdom = buddy.tgt_realm.GetKingdom();
                    bool isEnemyTerritory = tgtRealmKingdom != null && tgtRealmKingdom.IsEnemy(__instance.kingdom.id);

                    AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: Leader moving to {buddy.tgt_realm.name} (Owner: {tgtRealmKingdom?.Name ?? "None"}, IsEnemy={isEnemyTerritory})", LogCategory.Military, __instance.kingdom);

                    if (isEnemyTerritory && army.GetTarget() != buddy)
                    {
                        AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: ACTION=follow_buddy_move (following leader to enemy territory)", LogCategory.Military, __instance.kingdom);
                        TraverseAPI.SendArmy(__instance, army, buddy, "follow_buddy_move");
                        return;
                    }
                }

                // Stationary in Enemy Territory: Leader stopped in/near enemy realm (waiting to attack)
                // This handles the case where leader has arrived at tgt_realm but hasn't started battle yet
                if (!leaderMoving && buddy.tgt_realm != null)
                {
                    var tgtRealmKingdom = buddy.tgt_realm.GetKingdom();
                    bool isEnemyTerritory = tgtRealmKingdom != null && tgtRealmKingdom.IsEnemy(__instance.kingdom.id);

                    if (isEnemyTerritory && army.GetTarget() != buddy)
                    {
                        AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: Leader stationary at enemy realm {buddy.tgt_realm.name}", LogCategory.Military, __instance.kingdom);
                        AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: ACTION=join_leader_territory (going to leader in enemy territory)", LogCategory.Military, __instance.kingdom);
                        TraverseAPI.SendArmy(__instance, army, buddy, "join_leader_territory");
                        return;
                    }
                }

                AIOverhaulPlugin.LogDebug($"[Buddy] {armyName}: No action taken - leader not doing military action", LogCategory.Military, __instance.kingdom);
            }
            else
            {
                // LEADER LOGIC: Rescue Follower if they are in trouble
                // (buddy was already retrieved above at line 124)
                if (buddy != null && buddy.IsValid())
                {
                     // If Buddy is in battle and we are NOT, go help!
                     if (buddy.battle != null && army.battle == null)
                     {
                         // Check if we are already moving to the battle (reuse currentTarget from line 115)
                         if (currentTarget != buddy && currentTarget != buddy.battle)
                         {
                             // Estimate battle strength from buddy's realm
                             float friendlyStr = buddy.EvalStrength();
                             float enemyStr = 0;
                             if (buddy.realm_in != null && buddy.realm_in.armies != null)
                             {
                                 foreach (var a in buddy.realm_in.armies)
                                 {
                                     if (a != null && __instance.kingdom.IsEnemy(a.kingdom_id))
                                         enemyStr += a.EvalStrength();
                                 }
                             }

                             // Check if this army should help (enough units or changes outcome)
                             if (!BuddySystem.ShouldBuddyHelp(army, friendlyStr, enemyStr, __instance.kingdom))
                             {
                                 AIOverhaulPlugin.LogDebug($"[ThinkArmy] Leader {army.GetNid()} skipping rescue - army too weak to impact battle", LogCategory.Military, __instance.kingdom);
                             }
                             else
                             {
                                 AIOverhaulPlugin.LogDebug($"[ThinkArmy] Leader {army.GetNid()} RESCUING Buddy {buddy.GetNid()} in battle!", LogCategory.Military, __instance.kingdom);
                                 TraverseAPI.SendArmy(__instance, army, buddy, "rescue_buddy");
                                 return;
                             }
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
                        if (threat != null && threat.level == KingdomAI.Threat.Level.Siege)
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
                                    TraverseAPI.SendArmy(__instance, army, siegeBattle, "attack");
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            if (status == "idle" && army.castle == null)
            {
                Castle nearest = TraverseAPI.FindNearestOwnCastle(__instance, army, true);
                if (nearest != null)
                {
                    AIOverhaulPlugin.LogDebug($" Idle Knight - Returning to garrison at {nearest.name}", LogCategory.Military, __instance.kingdom);
                    TraverseAPI.SendArmy(__instance, army, nearest, "go_inside");
                }
            }
        }

        static string DescribeTarget(object target)
        {
            if (target == null) return "None";

            if (target is Castle c) return $"Castle:{c.name}";
            if (target is Logic.Army a) return $"Army:{a.leader?.Name ?? a.GetNid().ToString()}";
            if (target is Logic.Battle) return "Battle";
            if (target is Logic.Realm r) return $"Realm:{r.name}";

            return target.GetType().Name;
        }
    }
}
