using System;
using System.Collections.Generic;
using HarmonyLib;
using Logic;
using Action = Logic.Action;
using Object = Logic.Object;

namespace AIOverhaul
{
    // "ConsiderExpense" is the core method where AI decides to create an expense proposal for an action.
    [HarmonyPatch(typeof(KingdomAI), "ConsiderExpense")]
    [HarmonyPatch(new[] { typeof(KingdomAI.Expense.Type), typeof(BaseObject), typeof(Object), typeof(KingdomAI.Expense.Category), typeof(KingdomAI.Expense.Priority), typeof(List<Value>) })]
    class KingdomAI_ConsiderExpense
    {
        const float k_MinIncomeForEspionage = 300f;
        const float k_MinIncomeForDiplomat = 200f;
        const float k_MinIncomeForReligion = 200f;
        const string k_LogPrefix = "[ConsiderExpense]";

        static bool Prefix(KingdomAI __instance, KingdomAI.Expense.Type type, BaseObject defParam, KingdomAI.Expense.Category category)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            
            if (!ConsiderEspionageExpense(__instance, category)) return false;
            if (type != KingdomAI.Expense.Type.HireChacacter && IsSavingForFirstTradition(__instance.kingdom)) return false;

            switch (type)
            {
                case KingdomAI.Expense.Type.HireChacacter:      return ConsiderHireCharacter(__instance, defParam);
                case KingdomAI.Expense.Type.ExecuteOpportunity: return ConsiderExecuteOpportunity(__instance, defParam, category);
                case KingdomAI.Expense.Type.ExecuteAction:      return ConsiderExecuteAction(__instance, defParam, category);
                default:                                        return true;
            }
        }

        // Block all espionage expenses when income is too low.
        static bool ConsiderEspionageExpense(KingdomAI kingdomAI, KingdomAI.Expense.Category category)
        {
            if (category != KingdomAI.Expense.Category.Espionage) return true;

            float income = kingdomAI.kingdom.income[ResourceType.Gold];
            if (income < k_MinIncomeForEspionage)
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocking espionage expense: Income {income:F1} < {k_MinIncomeForEspionage}", LogCategory.Knights, kingdomAI.kingdom);
                return false;
            }
            return true;
        }

        // Block non-essential religion opportunities when income is low. Allows AppeaseClergyAction.
        static bool ConsiderExecuteOpportunity(KingdomAI kingdomAI, BaseObject defParam, KingdomAI.Expense.Category category)
        {
            if (category == KingdomAI.Expense.Category.Religion)
            {
                var opp = defParam as Opportunity;
                if (opp?.action != null && !(opp.action is AppeaseClergyAction))
                {
                    float income = kingdomAI.kingdom.income[ResourceType.Gold];
                    if (income < k_MinIncomeForReligion)
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocking religion opportunity ({opp.action.GetType().Name}): Income {income:F1} < {k_MinIncomeForReligion}", LogCategory.Spending, kingdomAI.kingdom);
                        return false;
                    }
                }
            }
            return true;
        }

        // Block expensive piety missions (MissionInRome/Constantinople) when income is low.
        static bool ConsiderExecuteAction(KingdomAI kingdomAI, BaseObject defParam, KingdomAI.Expense.Category category)
        {
            if (category == KingdomAI.Expense.Category.Religion)
            {
                var action = defParam as Action;
                if (action is MissionInRomeAction || action is MissionInConstantinopleAction)
                {
                    float income = kingdomAI.kingdom.income[ResourceType.Gold];
                    if (income < k_MinIncomeForReligion)
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocking piety mission ({action.GetType().Name}): Income {income:F1} < {k_MinIncomeForReligion}", LogCategory.Spending, kingdomAI.kingdom);
                        return false;
                    }
                }
            }
            return true;
        }

        // Returns true when the kingdom should be saving gold for its first tradition:
        // has enough books, 0 traditions, and a preferred tradition is available.
        static bool IsSavingForFirstTradition(Logic.Kingdom kingdom)
        {
            if (kingdom.traditions?.Count != 0) return false;
            if (kingdom.GetBooks() < GameBalance.MinBooksForFirstTradition) return false;

            List<Tradition.Def> options = kingdom.GetNewTraditionOptions();
            if (options == null) return false;

            for (int i = 0; i < options.Count; i++)
            {
                string name = options[i].name;
                for (int j = 0; j < KingdomAI_ConsiderAdoptTradition.PreferredFirstTraditions.Length; j++)
                {
                    if (name == KingdomAI_ConsiderAdoptTradition.PreferredFirstTraditions[j])
                        return true;
                }
            }
            return false;
        }

        static bool ConsiderHireCharacter(KingdomAI kingdomAI, BaseObject defParam)
        {
            var classDef = defParam as CharacterClass.Def;
            if (classDef == null) return true;

            switch (classDef.name)
            {
                case CharacterClassNames.Spy:
                {
                    float income = kingdomAI.kingdom.income[ResourceType.Gold];
                    if (income < k_MinIncomeForEspionage)
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocking Spy hiring: Income {income:F1} < k_MinIncomeForEspionage", LogCategory.Knights, kingdomAI.kingdom);
                        return false;
                    }

                    break;
                }

                case CharacterClassNames.Diplomat:
                {
                    if (kingdomAI.kingdom.CountDiplomats() >= 1)
                        return false;

                    float income = kingdomAI.kingdom.income[ResourceType.Gold];
                    if (income < k_MinIncomeForDiplomat)
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocking Diplomat hiring: Income {income:F1} < {k_MinIncomeForDiplomat}", LogCategory.Knights, kingdomAI.kingdom);
                        return false;
                    }

                    break;
                }

                case "Merchant":
                    return false;  // All merchant hiring routed through ConsiderHireMerchant → ConsiderExpenseDirect
            }

            return true;
        }
    }
}
