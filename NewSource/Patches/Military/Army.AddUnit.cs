using System;
using HarmonyLib;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Logic.Army), "AddUnit", typeof(Logic.Unit.Def), typeof(int), typeof(bool), typeof(bool), typeof(bool))]
    public class Army_AddUnit
    {
        static bool Prefix(Logic.Army __instance, Logic.Unit.Def def, ref Logic.Unit __result)
        {
            if (__instance == null || def == null) return true;

            var kingdom = __instance.IsValid() ? __instance.GetKingdom() : null;
            if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return true;

            if (!def.is_ranged) return true;

            int rangedCount = MilitaryHelper.CountRangedUnits(__instance);
            if (rangedCount >= GameBalance.MaxRangedUnitsPerArmy)
            {
                AIOverhaulPlugin.LogDebug($"[AddUnit] BLOCKED ranged unit {def.id} - army already has {rangedCount} ranged units", LogCategory.Military, kingdom);
                __result = null;
                return false;
            }

            return true;
        }
    }
}
