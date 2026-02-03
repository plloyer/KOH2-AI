using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "SpendExpense")]
    public static class KingdomAI_SpendExpense
    {
        public static void Postfix(KingdomAI __instance, KingdomAI.Expense expense, bool __result)
        {
            if (!__result) return;
            if (__instance.kingdom == null) return;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;

            string details = FormatExpenseDetails(expense);
            AIOverhaulPlugin.LogDebug($"[SPENT] {details}", LogCategory.Spending, __instance.kingdom);
        }

        static string FormatExpenseDetails(KingdomAI.Expense expense)
        {
            string defName = (expense.defParam as Def)?.id ?? "unknown";
            string costStr = expense.cost?.IsZero() == false ? $" for {expense.cost}" : " (Free)";

            switch (expense.type)
            {
                case KingdomAI.Expense.Type.BuildStructure:
                    return $"Built {defName} in {GetLocationName(expense)}{costStr}";

                case KingdomAI.Expense.Type.Upgrade:
                    return $"Upgraded to {defName} in {GetLocationName(expense)}{costStr}";

                case KingdomAI.Expense.Type.HireArmyUnit:
                    return $"Hired {defName} for army in {GetLocationName(expense)}{costStr}";

                case KingdomAI.Expense.Type.HireGarrison:
                    return $"Hired {defName} for garrison in {GetLocationName(expense)}{costStr}";

                case KingdomAI.Expense.Type.HireChacacter:
                    return $"Hired {defName}{costStr}";

                case KingdomAI.Expense.Type.ExpandCity:
                    return $"Expanded city {GetLocationName(expense)}{costStr}";

                case KingdomAI.Expense.Type.UpgradeFortifications:
                    return $"Upgraded fortifications in {GetLocationName(expense)}{costStr}";

                case KingdomAI.Expense.Type.AdoptTradition:
                    return $"Adopted tradition {defName}{costStr}";

                case KingdomAI.Expense.Type.HireMercenaryArmy:
                    return $"Hired mercenary army{costStr}";

                case KingdomAI.Expense.Type.HireArmyEquipment:
                    return $"Purchased equipment {defName}{costStr}";

                case KingdomAI.Expense.Type.ExecuteAction:
                    return $"{FormatAction(expense.defParam as Action)} -> {GetLocationName(expense)}{costStr}";

                case KingdomAI.Expense.Type.ExecuteOpportunity:
                {
                    var opportunity = expense.defParam as Opportunity;
                    return $"{FormatAction(opportunity?.action)} -> {GetTargetName(opportunity?.target)}{costStr}";
                }

                default:
                    return $"{expense.type}: {defName} in {GetLocationName(expense)}{costStr}";
            }
        }

        static string FormatAction(Action action)
        {
            if (action == null)
                return "unknown action";

            string actionName = action.def?.id ?? "unknown";

            if (action.owner is Logic.Character character)
                return $"{character.class_def?.name} {character.Name}.{actionName}";

            if (action.owner is Logic.Kingdom kingdom)
                return $"{kingdom.Name}.{actionName}";

            return actionName;
        }

        static string GetLocationName(KingdomAI.Expense expense)
        {
            if (expense.objectParam is Castle castle)
                return castle.name;
            if (expense.objectParam is Logic.Kingdom kingdom)
                return kingdom.Name;
            return expense.objectParam?.ToString() ?? "unknown";
        }

        static string GetTargetName(Object target)
        {
            if (target is Castle castle)
                return castle.name;
            if (target is Logic.Kingdom kingdom)
                return kingdom.Name;
            if (target is Logic.Character character)
                return character.Name ?? "unnamed";
            return target?.ToString() ?? "unknown";
        }
    }
}
