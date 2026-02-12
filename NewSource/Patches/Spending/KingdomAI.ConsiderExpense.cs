using System.Collections.Generic;
using HarmonyLib;
using Logic;
using Object = Logic.Object;

namespace AIOverhaul
{
    // "ConsiderExpense" is the core method where AI decides to create an expense proposal for an action.
    [HarmonyPatch(typeof(KingdomAI), "ConsiderExpense")]
    [HarmonyPatch(new[] { typeof(KingdomAI.Expense.Type), typeof(BaseObject), typeof(Object), typeof(KingdomAI.Expense.Category), typeof(KingdomAI.Expense.Priority), typeof(List<Value>) })]
    class KingdomAI_ConsiderExpense
    {
        const float k_MinIncomeForEspionage = 300f;
        const string k_LogPrefix = "[ConsiderExpense]";

        static bool Prefix(KingdomAI __instance, KingdomAI.Expense.Type type, BaseObject defParam, KingdomAI.Expense.Category category)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            if (category == KingdomAI.Expense.Category.Espionage)
            {
                float income = __instance.kingdom.income[ResourceType.Gold];
                if (income < k_MinIncomeForEspionage)
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocking espionage expense: Income {income:F1} < k_MinIncomeForEspionage", LogCategory.Knights, __instance.kingdom);
                    return false;
                }
            }

            if (type == KingdomAI.Expense.Type.HireChacacter)
                return ConsiderHireCharacter(__instance, defParam as CharacterClass.Def);

            // Early game build order: save gold for first tradition by blocking non-essential expenses
            if (IsSavingForFirstTradition(__instance.kingdom) && !IsEarlyBuildOrderExpense(type, defParam, __instance.kingdom))
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocking expense {type}/{defParam} — saving gold for first tradition", LogCategory.Spending, __instance.kingdom);
                return false;
            }

            return true;
        }

        // Returns true when the kingdom should be saving gold for its first tradition:
        // has enough books (MinBooksForFirstTradition), 0 traditions, and Writing or Medicine is available.
        static bool IsSavingForFirstTradition(Logic.Kingdom kingdom)
        {
            if (kingdom.traditions?.Count != 0) return false;
            if (kingdom.GetBooks() < GameBalance.MinBooksForFirstTradition) return false;

            List<Tradition.Def> options = kingdom.GetNewTraditionOptions();
            if (options == null) return false;

            for (int i = 0; i < options.Count; i++)
            {
                string name = options[i].name;
                if (name == TraditionNames.WritingTradition || name == TraditionNames.MedicineTradition)
                    return true;
            }

            return false;
        }

        // Expenses allowed through even when saving for first tradition.
        // Build order: MinMerchantsBeforeTradition merchants → VillageMilitia → Barracks → tradition.
        static bool IsEarlyBuildOrderExpense(KingdomAI.Expense.Type type, BaseObject defParam, Logic.Kingdom kingdom)
        {
            if (type == KingdomAI.Expense.Type.AdoptTradition)
                return true;
            if (type == KingdomAI.Expense.Type.ExecuteAction)
                return true;
            if (type == KingdomAI.Expense.Type.ExecuteOpportunity)
                return true;

            if (type == KingdomAI.Expense.Type.BuildStructure)
            {
                var buildingDef = defParam as Building.Def;
                if (buildingDef == null) return false;
                if (buildingDef.id == BuildingNames.VillageMilitia)
                    return true;
                if (buildingDef.id == BuildingNames.Barracks && !kingdom.HasBuilding(BuildingNames.Barracks))
                    return true;
            }

            return false;
        }

        static bool ConsiderHireCharacter(KingdomAI kingdomAI, CharacterClass.Def classDef)
        {
            if (classDef == null) return false;

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
                    if (kingdomAI.kingdom.CountDiplomats() >= 1)
                        return false;
                    break;
            }

            return true;
        }
    }
}
