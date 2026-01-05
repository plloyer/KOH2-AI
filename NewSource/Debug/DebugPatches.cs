using HarmonyLib;
using AIOverhaul;
using Logic;
using UnityEngine;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderExpense", new[] { typeof(Logic.KingdomAI.Expense) })]
    public class ConsiderExpensePatch
    {
        static void Postfix(Logic.KingdomAI __instance, Logic.KingdomAI.Expense expense)
        {
            // Only run if Spectator Mode is active
            if (!AIOverhaulPlugin.SpectatorMode) return;
            
            // Only for the kingdom being spectated (Player Kingdom)
            if (__instance.kingdom == null || !__instance.kingdom.is_player) return;

            if (expense == null) return;
            if (DebugOverlay.Instance == null) return;

            // Generate a readable name for the expense
            string name = expense.type.ToString();
            
            // Add details based on type if possible
            if (expense.defParam is Logic.Def d) name += $": {d.field?.key ?? d.ToString()}";
            else if (expense.objectParam is Logic.BaseObject bo) name += $": {bo}";
            
            float score = expense.eval;
            string category = expense.category.ToString();

            DebugOverlay.Instance.RecordConsideredExpense(name, score, category);
        }
    }
}
