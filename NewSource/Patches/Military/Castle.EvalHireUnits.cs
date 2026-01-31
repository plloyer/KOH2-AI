using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "EvalHireUnits" determines if militia/peasants should be raised in a castle.
    // Intent: PeasantRecruitmentBlockPatch
    [HarmonyPatch(typeof(Castle), "EvalHireUnits")]
    public class Castle_EvalHireUnits
    {
        static void Prefix(Castle __instance, ref bool allow_militia)
        {
            if (AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom()))
            {
                allow_militia = false;
            }
        }
    }
}
