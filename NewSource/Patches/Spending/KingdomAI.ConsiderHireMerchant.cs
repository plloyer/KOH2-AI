using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // Vanilla caps merchant count at tradeAgreementsWith.Count, so kingdoms with 0 trade
    // agreements can never hire merchants. We guarantee at least GameBalance.MinMerchantsBeforeTradition merchants
    // by bypassing that cap while still respecting court slot and papal cleric constraints.
    [HarmonyPatch(typeof(KingdomAI), "ConsiderHireMerchant")]
    static class KingdomAI_ConsiderHireMerchant
    {
        static bool Prefix(KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            var kingdom = __instance.kingdom;
            var merchantDef = __instance.game?.ai?.merchant_def;
            if (merchantDef == null) return true;

            int currentMerchants = kingdom.CountMerchants();

            // Guarantee a minimum number of merchants early game, but respect commerce cap
            if (currentMerchants < GameBalance.MinMerchantsBeforeTradition)
            {
                AIOverhaulPlugin.LogDebug($"Forcing merchant consideration {currentMerchants}/{GameBalance.MinMerchantsBeforeTradition}", LogCategory.Spending, kingdom);
                TraverseAPI.ConsiderExpenseDirect(__instance, KingdomAI.Expense.Type.HireChacacter, merchantDef, null, merchantDef.ai_category, KingdomAI.Expense.Priority.Urgent);
                __result = true;
                return false;
            }

            if (kingdom.HasIdleMerchant()) { __result = false; return false; }
            
            int maxCommerce = (int)kingdom.GetMaxCommerce();
            int currentCommerce = (int)kingdom.GetAllocatedCommerce();
            int delta = maxCommerce - currentCommerce;
            
            if (delta >= GameBalance.CommercePerMerchant)
            {
                AIOverhaulPlugin.LogDebug($"Extra merchant: {currentCommerce}/{maxCommerce} -> {delta} available", LogCategory.Spending, kingdom);
                TraverseAPI.ConsiderExpenseDirect(__instance, KingdomAI.Expense.Type.HireChacacter, merchantDef, null, merchantDef.ai_category);
                __result = true;
                return false;
            }

            __result = false;
            return false;
        }
    }
}
