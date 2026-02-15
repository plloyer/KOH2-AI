using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// Centralized API for accessing private members via Harmony Traverse.
    /// All method/field names are defined as constants to avoid hardcoded strings.
    /// Only methods that wrap private vanilla methods belong here.
    /// </summary>
    public static class TraverseAPI
    {
        // Method name constants
        const string k_MethodSend = "Send";
        const string k_MethodFindNearestOwnCastle = "FindNearestOwnCastle";
        const string k_MethodThinkProposeOfferThread = "ThinkProposeOfferThread";
        const string k_MethodConsiderExpense = "ConsiderExpense";

        public static bool SendArmy(this KingdomAI ai, Logic.Army army, MapObject target, string aiStatus, Logic.Battle battleViewBattle = null)
        {
            // Vanilla "Send" method signature: private bool Send(Army army, MapObject target, string status, Battle battle_view_battle = null)
            try
            {
                var method = AccessTools.Method(typeof(KingdomAI), k_MethodSend, new[] { typeof(Logic.Army), typeof(MapObject), typeof(string), typeof(Logic.Battle) });
                if (method != null)
                {
                    return (bool)method.Invoke(ai, new object[] { army, target, aiStatus, battleViewBattle });
                }

                AIOverhaulPlugin.LogError($"Could not find method {k_MethodSend} with params (Army, MapObject, string, Battle)");
                return false;
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"Could not invoke method {k_MethodSend}: {ex.Message}");
                return false;
            }
        }

        public static Castle FindNearestOwnCastle(this KingdomAI ai, Logic.Army army, bool allowGarrisoned)
        {
            return (Castle)Traverse.Create(ai).Method(k_MethodFindNearestOwnCastle, army, allowGarrisoned).GetValue();
        }

        public static IEnumerator ThinkProposeOfferThread(this KingdomAI ai, Logic.Kingdom target, string offerRelChangeType)
        {
            return (IEnumerator)Traverse.Create(ai).Method(k_MethodThinkProposeOfferThread, target, offerRelChangeType).GetValue();
        }

        public static void ConsiderExpense(KingdomAI ai, KingdomAI.Expense.Type type, BaseObject defParam, Logic.Object objectParam, KingdomAI.Expense.Category category, KingdomAI.Expense.Priority priority = KingdomAI.Expense.Priority.Normal, List<Value> args = null)
        {
            try
            {
                var method = AccessTools.Method(typeof(KingdomAI), k_MethodConsiderExpense,
                    new[] { typeof(KingdomAI.Expense.Type), typeof(BaseObject), typeof(Logic.Object), typeof(KingdomAI.Expense.Category), typeof(KingdomAI.Expense.Priority), typeof(List<Value>) });
                if (method != null)
                {
                    method.Invoke(ai, new object[] { type, defParam, objectParam, category, priority, args });
                    return;
                }

                AIOverhaulPlugin.LogError($"Could not find method {k_MethodConsiderExpense}");
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"Could not invoke method {k_MethodConsiderExpense}: {ex.Message}");
            }
        }

        /// <summary>
        /// Bypasses the 6-param ConsiderExpense (which our Harmony Prefix patches) by calling
        /// the 1-param ConsiderExpense(Expense) overload directly via Traverse.
        /// </summary>
        public static void ConsiderExpenseDirect(KingdomAI ai, KingdomAI.Expense.Type type, BaseObject defParam, Logic.Object objectParam, KingdomAI.Expense.Category category, KingdomAI.Expense.Priority priority = KingdomAI.Expense.Priority.Normal, List<Value> args = null)
        {
            var traverse = Traverse.Create(ai);
            var tmpExpense = traverse.Field("tmp_expense").GetValue<KingdomAI.Expense>();
            tmpExpense.Set(ai.kingdom, type, category, priority, defParam, objectParam, args);
            traverse.Method(k_MethodConsiderExpense, new[] { typeof(KingdomAI.Expense) }).GetValue(tmpExpense);
        }
    }
}
