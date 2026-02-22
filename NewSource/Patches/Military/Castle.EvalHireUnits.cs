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
        static void Prefix(Castle __instance, ref bool allow_militia, ref float food, bool for_garrison)
        {
            var kingdom = __instance.GetKingdom();
            if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;

            allow_militia = false;

            if (kingdom.IsAtWar() && !for_garrison)
                food = float.PositiveInfinity;
        }
    }
}
