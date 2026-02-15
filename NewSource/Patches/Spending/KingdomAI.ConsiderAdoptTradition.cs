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
        internal static readonly string[] PreferredFirstTraditions =
        {
            TraditionNames.WritingTradition,
            TraditionNames.LearningTradition,
            TraditionNames.MedicineTradition,
            TraditionNames.LogisticsTradition,
            TraditionNames.InfantryTacticsTradition
        };

        [HarmonyPrefix]
        public static bool Prefix(KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            var traditionOptions = __instance.kingdom.GetNewTraditionOptions();
            if (traditionOptions == null || traditionOptions.Count == 0) return true;

            bool rushingTradition = __instance.kingdom.GetBooks() > GameBalance.MinBooksForFirstTradition;
            if (!rushingTradition) return true;

            Tradition.Def preferredTradition = null;
            for (int i = 0; i < PreferredFirstTraditions.Length; i++)
            {
                preferredTradition = traditionOptions.Find(t => t.id == PreferredFirstTraditions[i]);
                if (preferredTradition != null) break;
            }

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
