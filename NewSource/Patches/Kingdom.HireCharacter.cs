using HarmonyLib;

namespace AIOverhaul
{
    // Patch to automatically organize the court when a new knight is hired
    // Intent: HireKnightPatch (Court Organization)
    [HarmonyPatch(typeof(Logic.Kingdom), "HireCharacter")]
    public class Kingdom_HireCharacter
    {
        static void Postfix(Logic.Kingdom __instance, Logic.Character __result)
        {
            if (__instance == null || __result == null) return;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance)) return;
            
            CourtHelper.OrganizeCourt(__instance);
        }
    }
}
