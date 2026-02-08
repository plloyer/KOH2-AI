using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "ThinkBreakSiege")]
    public class KingdomAI_ThinkBreakSiege
    {
        const string k_LogPrefix = "[ThinkBreakSiege]";

        static bool Prefix(KingdomAI __instance, Logic.Army a)
        {
            if (a == null || __instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Only handle siege situations where we are the defender
            if (a.battle == null || a.battle.type != Logic.Battle.Type.Siege) return true;
            if (a.battle.attacker == a) return true; // We are the attacker, not defender

            // Get attacker strength
            var attacker = a.battle.attacker;
            float attackerStr = attacker?.EvalStrength() ?? 0;
            float ourStr = a.EvalStrength();

            if (MilitaryHelper.IsStrongerThan(ourStr, attackerStr, GameBalance.SallyOutStrengthRatio))
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {MilitaryHelper.DescribeArmy(a)} SALLYING OUT! (our:{ourStr:F0} vs enemy:{attackerStr:F0})", LogCategory.Military, __instance.kingdom);
                a.battle.BreakSiege(Logic.Battle.BreakSiegeFrom.Inside);
                return false; // Skip vanilla
            }

            return true; // Let vanilla handle food-based logic
        }
    }
}
