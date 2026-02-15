using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // Diagnostic patch on the single-param ConsiderExpense overload.
    // Build expenses flow through this path (ThinkGeneral → ConsiderExpense(expense)) and bypass the 6-param overload entirely.
    // Logs the reason a build expense is dropped or queued.
    [HarmonyPatch(typeof(KingdomAI), "ConsiderExpense", new[] { typeof(KingdomAI.Expense) })]
    static class KingdomAI_ConsiderExpenseBuildDiag
    {
        const string k_LogPrefix = "[ConsiderExpense]";

        static void Prefix(KingdomAI __instance, KingdomAI.Expense expense)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (expense.type != KingdomAI.Expense.Type.BuildStructure &&
                expense.type != KingdomAI.Expense.Type.Upgrade &&
                expense.type != KingdomAI.Expense.Type.ExpandCity) return;

            string defName = (expense.defParam as Def)?.id ?? "?";
            string castle = (expense.objectParam as Castle)?.name ?? "?";

            if (expense.eval >= 30f)
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {defName} @ {castle}: DROPPED — eval {expense.eval:F1} >= 30 (can't afford). Priority={expense.priority}, Cost={expense.cost}", LogCategory.Spending, __instance.kingdom);
                return;
            }
            if (expense.kingdom_cost.IsZero())
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {defName} @ {castle}: FREE — spending immediately (eval={expense.eval:F1})", LogCategory.Spending, __instance.kingdom);
                return;
            }
            if (expense.priority < KingdomAI.Expense.Priority.Urgent && __instance.categories[(int)expense.category].weight <= 0f)
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {defName} @ {castle}: DROPPED — category {expense.category} weight is 0 (priority={expense.priority})", LogCategory.Spending, __instance.kingdom);
                return;
            }

            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {defName} @ {castle}: QUEUING (eval={expense.eval:F1}, priority={expense.priority}, cost={expense.cost})", LogCategory.Spending, __instance.kingdom);
        }
    }
}
