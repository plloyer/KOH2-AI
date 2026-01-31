using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "EvalHireUnit")]
    public class KingdomAI_EvalHireUnit
    {
        static void Postfix(Logic.Unit.Def udef, Logic.Army army, ref float __result)
        {
            if (army == null || udef == null) return;
            var kingdom = army.IsValid() ? army.GetKingdom() : null;
            if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;

            if (!udef.is_ranged) return;

            int rangedCount = MilitaryHelper.CountRangedUnits(army);
            if (rangedCount >= GameBalance.MaxRangedUnitsPerArmy)
            {
                __result = 0f;
            }
        }
    }
}
