using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "ConsiderHireMercenaryArmy" decides which mercenary camps the AI considers hiring.
    // Vanilla only checks mercenaries inside kingdom borders (kingdom.mercenaries_in).
    // Intent: Let Enhanced AI consider ALL unhired mercs on the map (at doubled cost via GetCost postfix).
    [HarmonyPatch(typeof(KingdomAI), "ConsiderHireMercenaryArmy")]
    public class KingdomAI_ConsiderHireMercenaryArmy
    {
        static bool Prefix(KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            Logic.Kingdom kingdom = __instance.kingdom;

            // Check hired merc cap (same as vanilla)
            if (__instance.def.max_num_mercenaries != null && kingdom.mercenaries.Count >= __instance.def.max_num_mercenaries.Int(kingdom))
            {
                __result = false;
                return false;
            }

            // Get ALL unhired mercs from the Mercenary faction kingdom
            Mercenary.Def mercDef = __instance.game.defs.GetBase<Mercenary.Def>();
            if (mercDef == null) { __result = false; return false; }

            Logic.Kingdom mercFaction = FactionUtils.GetFactionKingdom(__instance.game, mercDef.kingdom_key);
            if (mercFaction == null) { __result = false; return false; }

            for (int i = 0; i < mercFaction.armies.Count; i++)
            {
                Logic.Army army = mercFaction.armies[i];
                if (army?.mercenary == null || !army.mercenary.ValidForHireAsArmy()) continue;

                int startIdx = __instance.game.Random(0, MercenaryMission.defs.Count);
                for (int j = 0; j < MercenaryMission.defs.Count; j++)
                {
                    MercenaryMission.Def missionDef = MercenaryMission.defs[(j + startIdx) % MercenaryMission.defs.Count];
                    if (missionDef.Validate(army.mercenary, kingdom))
                    {
                        TraverseAPI.ConsiderExpense(__instance, KingdomAI.Expense.Type.HireMercenaryArmy, missionDef, army.mercenary, KingdomAI.Expense.Category.Military);
                        __result = true;
                        return false;
                    }
                }
            }

            __result = false;
            return false;
        }
    }
}
