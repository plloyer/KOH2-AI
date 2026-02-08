using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "ThinkMilitary")]
    public class KingdomAI_ThinkMilitary
    {
        static bool Prefix(KingdomAI __instance)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            BuddySystem.EvaluatePairs(__instance.kingdom);

            return true; // Always run vanilla after
        }
    }
}
