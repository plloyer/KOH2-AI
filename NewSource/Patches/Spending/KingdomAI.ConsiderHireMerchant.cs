using HarmonyLib;

namespace AIOverhaul
{
    // Vanilla caps merchant count at tradeAgreementsWith.Count, so kingdoms with 0 trade
    // agreements can never hire merchants. We guarantee at least k_MinMerchantCount merchants
    // by bypassing that cap while still respecting court slot and papal cleric constraints.
    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderHireMerchant")]
    static class KingdomAI_ConsiderHireMerchant
    {
        const int k_MinMerchantCount = 2;

        static bool Prefix(Logic.KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            var kingdom = __instance.kingdom;
            int currentMerchants = kingdom.GetMerchantsCount();

            // Only intervene when below our guaranteed minimum
            if (currentMerchants >= k_MinMerchantCount) return true;

            var merchantDef = __instance.game?.ai?.merchant_def;
            if (merchantDef == null) return true;

            // Respect papal cleric slot reservation (mirrors vanilla BlockSlotForPapalCleric)
            if (IsPapalSlotBlocked(__instance)) return true;

            AIOverhaulPlugin.LogDebug($"Forcing merchant consideration ({currentMerchants}/{k_MinMerchantCount}, bypassing trade agreement cap)", LogCategory.Economy, kingdom);

            TraverseAPI.ConsiderExpense(__instance, Logic.KingdomAI.Expense.Type.HireChacacter, merchantDef, null, merchantDef.ai_category);

            __result = true;
            return false;
        }

        // Replicates vanilla BlockSlotForPapalCleric() using public APIs.
        // Returns true if the papal kingdom must reserve a court slot for a cardinal.
        static bool IsPapalSlotBlocked(Logic.KingdomAI ai)
        {
            var catholic = ai.game?.religions?.catholic;
            if (catholic == null) return false;
            if (ai.kingdom != catholic.hq_kingdom) return false;

            int clericsMinusOne = ai.kingdom.GetClericsCount() - 1;
            int minCardinals = catholic.min_cardinals_min;
            if (clericsMinusOne >= minCardinals) return false;

            return ai.kingdom.GetFreeCourtSlots() <= minCardinals - clericsMinusOne;
        }
    }
}
