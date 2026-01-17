using HarmonyLib;
using System.Linq;

namespace AIOverhaul.Patches.Military
{
    // "AssignArmy" assigns a specific army to a threat.
    // Intent: ArmyCoordinationPatch
    [HarmonyPatch(typeof(Logic.KingdomAI), "AssignArmy")]
    public class KingdomAI_AssignArmy
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.KingdomAI.Threat threat, int pass, ref bool __result)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Check if this is an OFFENSIVE threat (Attacking enemy territory)
            // Level.Attack means we are targeting a realm that is NOT ours (or occupied by us but contested?)
            // Logic: KingdomAI.Recalc sets level=Attack if realm.GetKingdom() != k.
            if (threat != null && threat.level >= Logic.KingdomAI.Threat.Level.Attack)
            {
                // Requirement: Must have at least 2 FULL armies to launch an attack
                // We count how many armies are valid and "Full"
                int readyArmies = 0;
                if (__instance.kingdom.armies != null)
                    readyArmies = __instance.kingdom.armies.Count(a => a != null && a.IsValid() && Logic.KingdomAI.IsFull(a));

                if (readyArmies < 2)
                {
                    AIOverhaulPlugin.LogDebug($"[AssignArmy] Blocking offensive assignment to {threat.realm?.name}: waiting for 2 full armies (have {readyArmies})", Constants.LogCategory.Military, __instance.kingdom);
                    __result = false; // Failed to assign
                    return false;     // Skip original method
                }
            }

            return true;
        }

        static void Postfix(Logic.KingdomAI __instance, Logic.KingdomAI.Threat threat, bool __result)
        {
            // If assignment failed or not enhanced AI, do nothing
            if (!__result || __instance == null || __instance.kingdom == null) return;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;

            // Find which army was just assigned (it's the last one in the threat's assigned list)
            // Find which army was just assigned
            if (threat == null) return;
            // threat.assigned is a struct, cannot check for null directly. Check its internal list.
            if (threat.assigned.armies == null) return;

            // Iterate through assigned armies to find any leader that needs their buddy
            // We iterate a copy or index to avoid modification issues if we add to the list
            // But we are adding to the list, so we should be careful.
            // Actually, we are adding to threat.assigned, which modifies the list.
            
            var currentArmies = threat.assigned.armies.ToList(); // Safety copy
            
            foreach (var armyEval in currentArmies)
            {
                var army = armyEval.army;
                if (army == null) continue;
                
                if (army.tgt_realm == threat.realm)
                {
                    // This army is assigned to this threat
                    var buddy = BuddySystem.GetBuddy(army, __instance.kingdom);
                    if (buddy != null && buddy.IsValid())
                    {
                        // Check if buddy is also assigned
                        // Verify buddy isn't already in the threat list
                        bool buddyInThreat = false;
                         foreach(var check in threat.assigned.armies) {
                             if(check.army == buddy) { buddyInThreat = true; break; }
                         }

                        if (!buddyInThreat)
                        {
                            // FORCE ASSIGN BUDDY
                            // AIOverhaulPlugin.LogInfo($"[AssignArmy] Leader {army.GetNid()} dragging Buddy {buddy.GetNid()} to {threat.realm.name}", LogCategory.Military);
                            
                            // Remove from old threat if any
                            if (buddy.tgt_realm != null)
                            {
                                var oldThreat = __instance.GetThreat(buddy.tgt_realm);
                                oldThreat?.assigned.Del(buddy);
                            }
                            
                            buddy.tgt_realm = threat.realm;
                            threat.assigned.Add(buddy);
                        }
                    }
                }
            }
        }
    }
}
