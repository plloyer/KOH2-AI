using System;
using System.Collections.Generic;
using HarmonyLib;
using Logic;
using Object = Logic.Object;

namespace AIOverhaul
{
    // "ConsiderExpense" is the core method where AI decides to create an expense proposal for an action.
    [HarmonyPatch(typeof(KingdomAI), "ConsiderExpense")]
    [HarmonyPatch(new[] { typeof(KingdomAI.Expense.Type), typeof(BaseObject), typeof(Object), typeof(KingdomAI.Expense.Category), typeof(KingdomAI.Expense.Priority), typeof(List<Value>) })]
    public class KingdomAI_ConsiderExpense
    {
        const float k_MinIncomeToHireSpy = 300f;
        const string LogPrefix = "[KingdomAI_ConsiderExpense]";
        
        static bool Prefix(KingdomAI __instance, KingdomAI.Expense.Type type, BaseObject defParam)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            if (type == KingdomAI.Expense.Type.HireChacacter)
                return ConsiderHireCharacter(__instance, defParam as CharacterClass.Def);

            return true;
        }

        static bool ConsiderHireCharacter(KingdomAI kingdomAI, CharacterClass.Def classDef)
        {
            if (classDef == null) return false;
            
            switch (classDef.name)
            {
                case CharacterClassNames.Spy:
                {
                    // Restriction: Must have at least 300 gold income
                    float income = kingdomAI.kingdom.income[ResourceType.Gold];
                    if (income < k_MinIncomeToHireSpy)
                    {
                        AIOverhaulPlugin.LogDebug($"{LogPrefix} Blocking Spy hiring: Income {income:F1} < {k_MinIncomeToHireSpy}", LogCategory.Knights, kingdomAI.kingdom);
                        return false; // Skip execution (don't consider this expense)
                    }

                    break;
                }
                
                case CharacterClassNames.Diplomat:
                    if (kingdomAI.kingdom.CountDiplomats() >= 1)
                        return false; // Skip execution (don't consider this expense)
                    break;
            }

            return true; // Allow expense consideration
        }
    }
}
