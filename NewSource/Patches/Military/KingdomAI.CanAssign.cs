using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "CanAssign" determines if an army is eligible to handle a specific threat.
    // Intent: BuddySystemPatch (Leader Check)
    [HarmonyPatch(typeof(KingdomAI), "CanAssign")]
    public class KingdomAI_CanAssign
    {
        static bool Prefix(KingdomAI __instance, Logic.Army army, ref bool __result)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            if (army == null) return true;

            // Strict Buddy System: Followers cannot be assigned to threats directly.
            // They must only follow their Leader.
            if (BuddySystem.IsFollower(army, __instance.kingdom))
            {
                // AIOverhaulPlugin.LogDebug($"[CanAssign] Blocking Follower {army.GetNid()} from independent assignment.", LogCategory.Military, __instance.kingdom);
                __result = false;
                return false;
            }

            return true;
        }
    }
}
