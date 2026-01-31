using HarmonyLib;
using System;

namespace AIOverhaul
{
    // "ConsiderExpense" is the core method where AI decides to create an expense proposal for an action.
    // Intent: SpyRestrictionsPatch
    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderExpense")]
    // Overloading makes it tricky, we need to specify parameter types to target the correct overload
    [HarmonyPatch(new Type[] { typeof(Logic.KingdomAI.Expense.Type), typeof(Logic.BaseObject), typeof(Logic.Object), typeof(Logic.KingdomAI.Expense.Category), typeof(Logic.KingdomAI.Expense.Priority), typeof(System.Collections.Generic.List<Logic.Value>) })]
    public class KingdomAI_HireSpy
    {
        const float k_MinIncomeToHireSpy = 300f;
        
        static bool Prefix(Logic.KingdomAI __instance, Logic.KingdomAI.Expense.Type type, Logic.BaseObject defParam)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Check if we are hiring a character
            // Note: Enum spelling "HireChacacter" matches game code typo
            if (type == Logic.KingdomAI.Expense.Type.HireChacacter)
            {
                // defParam should be CharacterClass.Def
                var classDef = defParam as Logic.CharacterClass.Def;
                if (classDef != null && classDef.name == "Spy")
                {
                    // Restriction: Must have at least 300 gold income
                    float income = __instance.kingdom.income[Logic.ResourceType.Gold];
                    if (income < k_MinIncomeToHireSpy)
                    {
                        AIOverhaulPlugin.LogDebug($"[HireSpy] Blocking Spy hiring: Income {income:F1} < {k_MinIncomeToHireSpy}", LogCategory.Knights, __instance.kingdom);
                        return false; // Skip execution (don't consider this expense)
                    }
                }
            }

            return true;
        }
    }
}
