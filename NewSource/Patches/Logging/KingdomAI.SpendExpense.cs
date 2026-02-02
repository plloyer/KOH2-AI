using HarmonyLib;
using Logic;
using System;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "SpendExpense")]
    public static class KingdomAI_SpendExpense
    {
        const string k_LogPrefix = "[AI SPEND]";
        public static void Postfix(KingdomAI __instance, KingdomAI.Expense expense, bool __result)
        {
            // Only log if successful spend
            if (!__result) return;
            if (__instance.kingdom == null || __instance.kingdom.is_player) return;

            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {__instance.kingdom.Name} Executed: Type={expense.type}, Category={expense.category}, Target={expense.objectParam}, Cost={expense.cost}", LogCategory.Economy, __instance.kingdom);
        }
    }
}
