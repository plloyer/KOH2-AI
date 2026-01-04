using AIOverhaul.Constants;
using HarmonyLib;
using AIOverhaul.Helpers;

namespace AIOverhaul
{
    // Patch to automatically organize the court when a new knight is hired
    // Intent: HireKnightPatch (Court Organization)
    [HarmonyPatch(typeof(Logic.Kingdom), TraverseAPI.METHOD_HIRE_CHARACTER)]
    public class Kingdom_HireCharacter_Patch
    {
        static void Postfix(Logic.Kingdom __instance, Logic.Character __result)
        {
            if (__instance == null || __result == null) return;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance)) return;
            
            AIOverhaulPlugin.LogDebug($"On Knight Hired", LogCategory.Knights, __instance);
            KingdomHelper.OrganizeCourt(__instance);
        }
    }
}
