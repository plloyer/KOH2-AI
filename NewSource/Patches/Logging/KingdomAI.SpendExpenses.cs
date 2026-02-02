using HarmonyLib;
using Logic;
using System;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "SpendExpenses")]
    public static class KingdomAI_SpendExpenses
    {
        const string k_LogPrefix = "[AI EXPENSE OPTIONS]";
        public static void Prefix(KingdomAI __instance, WeightedRandom<KingdomAI.Expense> expenses)
        {
            if (__instance.kingdom == null || __instance.kingdom.is_player) return;
            if (expenses == null || expenses.options.Count == 0) return;

            // dump() returns a string summary of options
            string dump = expenses.Dump();
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} {__instance.kingdom.Name}:\n{dump}",  LogCategory.Economy, __instance.kingdom);
        }
    }
}
