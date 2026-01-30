using HarmonyLib;
using AIOverhaul.Constants;

namespace AIOverhaul.Patches.Military
{
    // "EvalHireUnit" evaluates the priority/value of hiring a specific unit for an army.
    // Intent: Limit ranged units to MaxRangedUnitsPerArmy (default 4)
    [HarmonyPatch(typeof(Logic.KingdomAI), "EvalHireUnit")]
    public class KingdomAI_EvalHireUnit
    {
        static void Postfix(Logic.Unit.Def udef, Logic.Army army, ref float __result)
        {
            // Only apply to Enhanced AI
            if (army == null || udef == null) return;
            var kingdom = army.IsValid() ? army.GetKingdom() : null;
            if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;

            // Check if this is a ranged unit
            if (!udef.is_ranged) return;

            // Count current ranged units in the army
            int rangedCount = 0;
            if (army.units != null)
            {
                foreach (var unit in army.units)
                {
                    if (unit?.def != null && unit.def.is_ranged)
                        rangedCount++;
                }
            }

            // If already at max ranged, set eval to 0 to prevent hiring more
            if (rangedCount >= GameBalance.MaxRangedUnitsPerArmy)
            {
                __result = 0f;
            }
        }
    }
}
