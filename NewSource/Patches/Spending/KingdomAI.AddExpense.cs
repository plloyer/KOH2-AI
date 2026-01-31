using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "AddExpense" adds an evaluated expense to the appropriate queue (WeightedRandom).
    // Intent: Log when an expense is actually added to the queue to trace decision flow.
    [HarmonyPatch(typeof(KingdomAI), "AddExpense")]
    public class KingdomAI_AddExpense
    {
        static void Prefix(KingdomAI __instance, object expenses, KingdomAI.Expense expense)
        {
            try
            {
                if (__instance.kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom))
                    return;

                if (expense == null) return;

                AIOverhaulPlugin.LogDebug(
                    $"Adding Expense: {expense.type} | Param: {expense.defParam} | Priority: {expense.priority} | Eval: {expense.eval:F2}",
                    LogCategory.Economy,
                    __instance.kingdom
                );
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogDebug($"Error logging AddExpense: {ex.Message}", LogCategory.Economy, __instance.kingdom);
            }
        }
    }
}
