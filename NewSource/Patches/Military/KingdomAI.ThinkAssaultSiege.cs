using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "ThinkAssaultSiege" decides whether a besieging army should launch an assault on the castle.
    // Intent: AssaultLogicPatch
    [HarmonyPatch(typeof(KingdomAI), "ThinkAssaultSiege")]
    public class KingdomAI_ThinkAssaultSiege
    {
        const string k_LogPrefix = "[ThinkAssaultSiege]";

        static bool Prefix(Logic.Army a)
        {
            if (a?.battle == null) return true;
            var kingdom = a.GetKingdom();
            if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return true;

            // estimation: 0 = attacker wins easily, 0.5 = even odds, 1 = defender wins easily
            // Vanilla uses 0.2f; we use 0.4f to make AI assault more aggressively
            if (a.battle.type == Logic.Battle.Type.Siege && a.battle.attacker == a)
            {
                float estimation = a.battle.simulation.estimation;
                string armyName = MilitaryHelper.DescribeArmy(a);
                if (!(estimation > 0.4f))
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: ASSAULTING castle (estimation:{estimation:F2}, threshold:0.40)", LogCategory.Military, kingdom);
                    a.battle.Assault();
                }
                else
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {armyName}: holding siege (estimation:{estimation:F2} > threshold:0.40)", LogCategory.Military, kingdom);
                }
            }

            return false; // skip vanilla
        }
    }
}
