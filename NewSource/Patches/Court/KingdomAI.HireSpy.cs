using HarmonyLib;
using System;
using AIOverhaul.Constants;

namespace AIOverhaul.Patches.Court
{
    // "ConsiderExpense" is the core method where AI decides to create an expense proposal for an action.
    // Intent: SpyRestrictionsPatch
    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderExpense")]
    // Overloading makes it tricky, we need to specify parameter types to target the correct overload
    [HarmonyPatch(new Type[] { typeof(Logic.KingdomAI.Expense.Type), typeof(Logic.BaseObject), typeof(Logic.Object), typeof(Logic.KingdomAI.Expense.Category), typeof(Logic.KingdomAI.Expense.Priority), typeof(System.Collections.Generic.List<Logic.Value>) })]
    public class KingdomAI_HireSpy
    {
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
                    if (income < 300f)
                    {
                        AIOverhaulPlugin.LogInfo($"[HireSpy] Blocking Spy hiring for {__instance.kingdom.name}: Income {income:F1} < 300", LogCategory.Court);
                        return false; // Skip execution (don't consider this expense)
                    }
                }
            }

            return true;
        }
    }
}
