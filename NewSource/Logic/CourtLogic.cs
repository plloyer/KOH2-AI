using HarmonyLib;
using AIOverhaul.Helpers;

namespace AIOverhaul
{
    // Patch to automatically organize the court when a new knight is hired
    // Intent: HireKnightPatch (Court Organization)
    [HarmonyPatch(typeof(Logic.KingdomAI), "HireKnight")]
    public class KingdomAI_HireKnight
    {
        static void Postfix(Logic.KingdomAI __instance)
        {
            if (__instance == null || __instance.kingdom == null) return;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            
            KingdomHelper.OrganizeCourt(__instance.kingdom);
        }
    }
}
