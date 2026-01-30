using HarmonyLib;
using AIOverhaul.Constants;
using AIOverhaul.Helpers;

namespace AIOverhaul.Patches.Military
{
    [HarmonyPatch(typeof(Logic.Castle), "CanHireUnit")]
    public class Castle_CanHireUnit
    {
        static void Postfix(Logic.Castle __instance, Logic.Unit.Def unitDef, Logic.Army army, ref bool __result)
        {
            if (!__result) return;
            if (army == null || unitDef == null) return;

            var kingdom = __instance.GetKingdom();
            if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;

            if (!unitDef.is_ranged) return;

            int rangedCount = MilitaryHelper.CountRangedUnits(army);
            if (rangedCount >= GameBalance.MaxRangedUnitsPerArmy)
            {
                __result = false;
            }
        }
    }
}
