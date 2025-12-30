using System;
using HarmonyLib;
using Logic;
using AIOverhaul.Constants;

namespace AIOverhaul
{
    /// <summary>
    /// AI logic for tradition adoption and selection
    /// </summary>
    // "ConsiderAdoptTradition" decides which new kingdom tradition (e.g. "Writing") to adopt if slots are available.
    [HarmonyPatch(typeof(KingdomAI), "ConsiderAdoptTradition")]
    public static class TraditionSelectionPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            var traditionOptions = __instance.kingdom.GetNewTraditionOptions();
            if (traditionOptions == null || traditionOptions.Count == 0) return true;

            if (__instance == null || __instance.kingdom == null) return true;

            // Safety checks for kingdom properties
            if (__instance.kingdom.traditions == null || __instance.kingdom.wars == null || __instance.kingdom.resources == null) return true;

            // TRADITION RUSH LOGIC
            bool rushingTradition = false;
            // Use resources.Get(ResourceType.Books) and wars.Count
            if (__instance.kingdom.traditions.Count == 0 &&
                __instance.kingdom.wars.Count == 0 &&
                __instance.kingdom.resources.Get(ResourceType.Books) >= GameBalance.MinBooksForFirstTradition)
            {
                rushingTradition = true;
            }

            // Look for Writing or Learning tradition
            var preferredTradition = traditionOptions.Find(t => t.id == TraditionNames.WritingTradition);
            if (preferredTradition == null)
                preferredTradition = traditionOptions.Find(t => t.id == TraditionNames.LearningTradition);

            // If rushing, force pick even if we have to save gold (priority Urgent)
            if (rushingTradition && preferredTradition != null)
            {
                Resource cost = preferredTradition.GetAdoptCost(__instance.kingdom);
                if (__instance.kingdom.resources.CanAfford(cost, 1f))
                {
                    TraverseAPI.ConsiderExpense(__instance,
                        KingdomAI.Expense.Type.AdoptTradition,
                        preferredTradition,
                        null,
                        KingdomAI.Expense.Category.Economy,
                        KingdomAI.Expense.Priority.Urgent,
                        null);

                    __result = true;
                    return false;
                }
                else
                {
                    // Wait for money
                    return false;
                }
            }

            // Default behavior for other cases
            if (preferredTradition != null)
            {
                if (__instance.kingdom.resources.CanAfford(preferredTradition.GetAdoptCost(__instance.kingdom)))
                {
                    TraverseAPI.ConsiderExpense(__instance,
                        KingdomAI.Expense.Type.AdoptTradition,
                        preferredTradition,
                        null,
                        KingdomAI.Expense.Category.Economy,
                        KingdomAI.Expense.Priority.High,
                        null);

                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }
}
