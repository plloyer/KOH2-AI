using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "AddExpense" adds an evaluated expense to the appropriate queue (WeightedRandom).
    // Intent: Log when an expense is actually added to the queue to trace decision flow.
    [HarmonyPatch(typeof(KingdomAI), "AddExpense")]
    public class KingdomAI_AddExpense
    {
        static void Prefix(KingdomAI __instance, KingdomAI.Expense expense)
        {
            if (__instance.kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom))
                return;

            if (expense == null) return;

            string details = FormatExpenseOption(expense);
            AIOverhaulPlugin.LogDebug($"[OPTION] {details}", LogCategory.Spending, __instance.kingdom);
        }

        static string FormatExpenseOption(KingdomAI.Expense expense)
        {
            string defName = (expense.defParam as Def)?.id ?? "unknown";
            string location = GetLocationName(expense);
            string costStr = expense.cost?.IsZero() == false ? $"{expense.cost}" : "Free";
            string priorityStr = expense.priority != KingdomAI.Expense.Priority.Normal ? $"[{expense.priority}] " : "";

            switch (expense.type)
            {
                case KingdomAI.Expense.Type.BuildStructure:
                    return $"{priorityStr}Build {defName} in {location} (Eval:{expense.eval:F1}, Cost:{costStr})";

                case KingdomAI.Expense.Type.Upgrade:
                    return $"{priorityStr}Upgrade to {defName} in {location} (Eval:{expense.eval:F1}, Cost:{costStr})";

                case KingdomAI.Expense.Type.HireArmyUnit:
                    return $"{priorityStr}Hire {defName} for army in {location} (Eval:{expense.eval:F1}, Cost:{costStr})";

                case KingdomAI.Expense.Type.HireGarrison:
                    return $"{priorityStr}Hire {defName} for garrison in {location} (Eval:{expense.eval:F1}, Cost:{costStr})";

                case KingdomAI.Expense.Type.HireChacacter:
                    return $"{priorityStr}Hire {defName} (Eval:{expense.eval:F1}, Cost:{costStr})";

                case KingdomAI.Expense.Type.ExpandCity:
                    return $"{priorityStr}Expand city {location} (Eval:{expense.eval:F1}, Cost:{costStr})";

                case KingdomAI.Expense.Type.UpgradeFortifications:
                    return $"{priorityStr}Upgrade fortifications in {location} (Eval:{expense.eval:F1}, Cost:{costStr})";

                case KingdomAI.Expense.Type.AdoptTradition:
                    return $"{priorityStr}Adopt tradition {defName} (Eval:{expense.eval:F1}, Cost:{costStr})";

                case KingdomAI.Expense.Type.HireMercenaryArmy:
                    return $"{priorityStr}Hire mercenary army (Eval:{expense.eval:F1}, Cost:{costStr})";

                case KingdomAI.Expense.Type.HireArmyEquipment:
                    return $"{priorityStr}Purchase equipment {defName} (Eval:{expense.eval:F1}, Cost:{costStr})";

                case KingdomAI.Expense.Type.ExecuteAction:
                {
                    string actionName = (expense.defParam as Logic.Action)?.def?.id ?? "unknown";
                    return $"{priorityStr}Execute action {actionName} on {location} (Eval:{expense.eval:F1}, Cost:{costStr})";
                }

                case KingdomAI.Expense.Type.ExecuteOpportunity:
                {
                    var opportunity = expense.defParam as Logic.Opportunity;
                    string actionName = opportunity?.action?.def?.id ?? "unknown";
                    string targetName = GetTargetName(opportunity?.target);
                    return $"{priorityStr}Execute opportunity {actionName} on {targetName} (Eval:{expense.eval:F1}, Cost:{costStr})";
                }

                default:
                    return $"{priorityStr}{expense.type}: {defName} in {location} (Eval:{expense.eval:F1}, Cost:{costStr})";
            }
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
