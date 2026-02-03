using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "SpendExpenses")]
    public static class KingdomAI_SpendExpenses
    {
        public static void Prefix(KingdomAI __instance, WeightedRandom<KingdomAI.Expense> expenses)
        {
            if (__instance.kingdom == null) return;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (expenses == null || expenses.options.Count == 0) return;

            // Dump returns a string summary of all options in the queue
            string dump = expenses.Dump();
            //AIOverhaulPlugin.LogDebug($"[QUEUE] {expenses.options.Count} options: {dump}", LogCategory.Spending, __instance.kingdom);
        }
    }
}
