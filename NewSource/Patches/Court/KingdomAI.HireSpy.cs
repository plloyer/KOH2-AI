using System;
using System.Collections.Generic;
using HarmonyLib;
using Logic;
using Object = Logic.Object;

namespace AIOverhaul
{
    // "ConsiderExpense" is the core method where AI decides to create an expense proposal for an action.
    // Intent: SpyRestrictionsPatch
    [HarmonyPatch(typeof(KingdomAI), "ConsiderExpense")]
    // Overloading makes it tricky, we need to specify parameter types to target the correct overload
    [HarmonyPatch(new[] { typeof(KingdomAI.Expense.Type), typeof(BaseObject), typeof(Object), typeof(KingdomAI.Expense.Category), typeof(KingdomAI.Expense.Priority), typeof(List<Value>) })]
    public class KingdomAI_HireSpy
    {
        const float k_MinIncomeToHireSpy = 300f;
        
        static bool Prefix(KingdomAI __instance, KingdomAI.Expense.Type type, BaseObject defParam)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Check if we are hiring a character
            // Note: Enum spelling "HireChacacter" matches game code typo
            if (type == KingdomAI.Expense.Type.HireChacacter)
            {
                // defParam should be CharacterClass.Def
                var classDef = defParam as CharacterClass.Def;
                if (classDef != null && classDef.name == "Spy")
                {
                    // Restriction: Must have at least 300 gold income
                    float income = __instance.kingdom.income[ResourceType.Gold];
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
