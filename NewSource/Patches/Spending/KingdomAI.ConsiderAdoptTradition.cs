using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// AI logic for tradition adoption and selection
    /// </summary>
    // "ConsiderAdoptTradition" decides which new kingdom tradition (e.g. "Writing") to adopt if slots are available.
    // Intent: TraditionSelectionPatch
    [HarmonyPatch(typeof(KingdomAI), "ConsiderAdoptTradition")]
    public static class KingdomAI_ConsiderAdoptTradition
    {
        [HarmonyPrefix]
        public static bool Prefix(KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            var traditionOptions = __instance.kingdom.GetNewTraditionOptions();
            if (traditionOptions == null || traditionOptions.Count == 0) return true;

            bool rushingTradition = __instance.kingdom.GetBooks() > 400;
            if (!rushingTradition) return true;
            
            // Look for Writing or Learning tradition
            var preferredTradition = traditionOptions.Find(t => t.id == TraditionNames.WritingTradition);
            if (preferredTradition == null)
                preferredTradition = traditionOptions.Find(t => t.id == TraditionNames.LearningTradition);
            if (preferredTradition == null)
                preferredTradition = traditionOptions.Find(t => t.id == TraditionNames.MedicineTradition);

            if (preferredTradition == null)
                return true;
            
            if (__instance.kingdom.resources.CanAfford(preferredTradition.GetAdoptCost(__instance.kingdom)))
            {
                TraverseAPI.ConsiderExpense(__instance, KingdomAI.Expense.Type.AdoptTradition, preferredTradition, null, KingdomAI.Expense.Category.Economy, KingdomAI.Expense.Priority.High, null);

                __result = true;
                return false;
            }

            return true;
        }
    }
}
