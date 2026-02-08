using System;
using HarmonyLib;
using Logic;
using UnityEngine;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "ConsiderExpense", typeof(KingdomAI.Expense))]
    public class ConsiderExpensePatch
    {
        static void Postfix(KingdomAI __instance, KingdomAI.Expense expense)
        {
            // Only run if AI is forced for this kingdom (spectator mode or /ai_on)
            if (!MultiplayerAICommandHelper.IsAIForced(__instance.kingdom?.id ?? -1)) return;
            
            // Only for the kingdom being spectated (Player Kingdom)
            if (__instance.kingdom == null || !__instance.kingdom.is_player) return;

            if (expense == null) return;
            if (DebugOverlay.Instance == null) return;

            // Generate a readable name for the expense
            string name = expense.type.ToString();
            
            // Add details based on type if possible
            if (expense.defParam is Def d) name += $": {d.field?.key ?? d.ToString()}";
            else if (expense.objectParam is BaseObject bo) name += $": {bo}";
            
            float score = expense.eval;
            string category = expense.category.ToString();

            DebugOverlay.Instance.RecordConsideredExpense(name, score, category);
        }
    }
}
