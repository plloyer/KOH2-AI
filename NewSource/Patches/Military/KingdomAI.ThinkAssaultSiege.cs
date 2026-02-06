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
        static bool Prefix(Logic.Army a)
        {
            if (a?.battle == null) return true;
            var kingdom = a.GetKingdom();
            if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return true;

            // estimation: 0 = attacker wins easily, 0.5 = even odds, 1 = defender wins easily
            // Vanilla uses 0.2f; we use 0.4f to make AI assault more aggressively
            if (a.battle.type == Logic.Battle.Type.Siege && a.battle.attacker == a && !(a.battle.simulation.estimation > 0.4f))
            {
                a.battle.Assault();
            }

            return false; // skip vanilla
        }
    }
}
