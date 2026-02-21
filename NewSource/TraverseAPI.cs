using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        const string k_MethodThinkRetreat = "ThinkRetreat";
        const string k_MethodThinkBreakSiege = "ThinkBreakSiege";
        const string k_MethodShouldWait = "ShouldWait";
        const string k_MethodConsiderHireMercenaries = "ConsiderHireMercenaries";
        const string k_MethodThinkHelpWithRebels = "ThinkHelpWithRebels";
        const string k_MethodDecideOwnCastleForArmy = "DecideOwnCastleForArmy";
        const string k_MethodCalcBudget = "CalcBudget";
        const string k_MethodClearExpenses = "ClearExpenses";
        const string k_MethodCalcThreat = "CalcThreat";
        const string k_MethodThinkThreats = "ThinkThreats";
        const string k_MethodThinkHireUnits = "ThinkHireUnits";
        const string k_MethodThinkArmies = "ThinkArmies";
        const string k_MethodSpendExpenses = "SpendExpenses";
        const string k_MethodTooSoonRetreat = "TooSoonRetreat";

        // Cached MethodInfo — avoids repeated reflection
        static MethodInfo s_SendMethod;
        static MethodInfo s_ThinkRetreatMethod;
        static MethodInfo s_ThinkBreakSiegeMethod;
        static MethodInfo s_ShouldWaitMethod;
        static MethodInfo s_ConsiderHireMercenariesMethod;
        static MethodInfo s_ThinkHelpWithRebelsMethod;
        static MethodInfo s_DecideOwnCastleForArmyMethod;
        static MethodInfo s_CalcBudgetMethod;
        static MethodInfo s_ClearExpensesMethod;
        static MethodInfo s_CalcThreatMethod;
        static MethodInfo s_ThinkThreatsMethod;
        static MethodInfo s_ThinkHireUnitsMethod;
        static MethodInfo s_ThinkArmiesMethod;
        static MethodInfo s_SpendExpensesMethod;
        static MethodInfo s_TooSoonRetreatMethod;

        static MethodInfo GetSendMethod()
        {
            if (s_SendMethod == null)
                s_SendMethod = AccessTools.Method(typeof(KingdomAI), k_MethodSend, new[] { typeof(Logic.Army), typeof(MapObject), typeof(string), typeof(Logic.Battle) });
            return s_SendMethod;
        }

        public static bool SendArmy(this KingdomAI ai, Logic.Army army, MapObject target, string aiStatus, Logic.Battle battleViewBattle = null)
        {
            // Vanilla "Send" method signature: private bool Send(Army army, MapObject target, string status, Battle battle_view_battle = null)
            try
            {
                var method = GetSendMethod();
                if (method != null)
                {
                    bool result = (bool)method.Invoke(ai, new object[] { army, target, aiStatus, battleViewBattle });
                    if (result) SyncBuddyFollower(ai, army, target, aiStatus);
                    return result;
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

        static void SyncBuddyFollower(KingdomAI ai, Logic.Army leader, MapObject target, string aiStatus)
        {
            var kingdom = ai?.kingdom;
            if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;
            if (!BuddySystem.IsLeader(leader, kingdom)) return;

            var follower = BuddySystem.GetBuddy(leader, kingdom);
            if (follower == null || !follower.IsValid()) return;
            if (follower.battle != null || follower.IsFleeing()) return;
            if ((follower.units?.Count ?? 0) < GameBalance.MinBuddyUnitsToFollow) return;

            var method = GetSendMethod();
            if (method == null) return;

            if (MilitaryHelper.IsLeaderHeadingToFight(leader, kingdom))
            {
                // Leader heading to fight → follower follows the same target
                var leaderTarget = leader.GetTarget();
                MapObject followTarget = leaderTarget as MapObject ?? leader;
                method.Invoke(ai, new object[] { follower, followTarget, AIStatusNames.FollowLeader, null });
                AIOverhaulPlugin.LogDebug($"[BuddySync] {MilitaryHelper.DescribeArmy(follower)}: immediately following leader {MilitaryHelper.DescribeArmy(leader)} -> {MilitaryHelper.DescribeTarget(leaderTarget)}", LogCategory.Military, kingdom);
            }
            else if (follower.ai_status == AIStatusNames.FollowLeader)
            {
                // Leader redirected to non-fight (retreat, refill, etc.) → follower was following, redirect too
                method.Invoke(ai, new object[] { follower, target, aiStatus, null });
                AIOverhaulPlugin.LogDebug($"[BuddySync] {MilitaryHelper.DescribeArmy(follower)}: mirroring leader redirect to {MilitaryHelper.DescribeTarget(target)} ({aiStatus})", LogCategory.Military, kingdom);
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

        // --- ThinkArmy vanilla method wrappers ---

        /// <summary>Instance method: bool ThinkRetreat(Army a)</summary>
        public static bool ThinkRetreat(this KingdomAI ai, Logic.Army army)
        {
            if (s_ThinkRetreatMethod == null)
                s_ThinkRetreatMethod = AccessTools.Method(typeof(KingdomAI), k_MethodThinkRetreat, new[] { typeof(Logic.Army) });
            return (bool)s_ThinkRetreatMethod.Invoke(ai, new object[] { army });
        }

        /// <summary>Instance method: void ThinkBreakSiege(Army a)</summary>
        public static void ThinkBreakSiege(this KingdomAI ai, Logic.Army army)
        {
            if (s_ThinkBreakSiegeMethod == null)
                s_ThinkBreakSiegeMethod = AccessTools.Method(typeof(KingdomAI), k_MethodThinkBreakSiege, new[] { typeof(Logic.Army) });
            s_ThinkBreakSiegeMethod.Invoke(ai, new object[] { army });
        }

        /// <summary>Instance method: bool ShouldWait(Army army)</summary>
        public static bool ShouldWait(this KingdomAI ai, Logic.Army army)
        {
            if (s_ShouldWaitMethod == null)
                s_ShouldWaitMethod = AccessTools.Method(typeof(KingdomAI), k_MethodShouldWait, new[] { typeof(Logic.Army) });
            return (bool)s_ShouldWaitMethod.Invoke(ai, new object[] { army });
        }

        /// <summary>Instance method: bool ConsiderHireMercenaries(Army army)</summary>
        public static bool ConsiderHireMercenaries(this KingdomAI ai, Logic.Army army)
        {
            if (s_ConsiderHireMercenariesMethod == null)
                s_ConsiderHireMercenariesMethod = AccessTools.Method(typeof(KingdomAI), k_MethodConsiderHireMercenaries, new[] { typeof(Logic.Army) });
            return (bool)s_ConsiderHireMercenariesMethod.Invoke(ai, new object[] { army });
        }

        /// <summary>Instance method: bool ThinkHelpWithRebels(Army army)</summary>
        public static bool ThinkHelpWithRebels(this KingdomAI ai, Logic.Army army)
        {
            if (s_ThinkHelpWithRebelsMethod == null)
                s_ThinkHelpWithRebelsMethod = AccessTools.Method(typeof(KingdomAI), k_MethodThinkHelpWithRebels, new[] { typeof(Logic.Army) });
            return (bool)s_ThinkHelpWithRebelsMethod.Invoke(ai, new object[] { army });
        }

        /// <summary>Instance method: Castle DecideOwnCastleForArmy(Character leader)</summary>
        public static Castle DecideOwnCastleForArmy(this KingdomAI ai, Logic.Character leader)
        {
            if (s_DecideOwnCastleForArmyMethod == null)
                s_DecideOwnCastleForArmyMethod = AccessTools.Method(typeof(KingdomAI), k_MethodDecideOwnCastleForArmy, new[] { typeof(Logic.Character) });
            return (Castle)s_DecideOwnCastleForArmyMethod.Invoke(ai, new object[] { leader });
        }

        /// <summary>Instance method: bool TooSoonRetreat(Army army)</summary>
        public static bool TooSoonRetreat(this KingdomAI ai, Logic.Army army)
        {
            if (s_TooSoonRetreatMethod == null)
                s_TooSoonRetreatMethod = AccessTools.Method(typeof(KingdomAI), k_MethodTooSoonRetreat, new[] { typeof(Logic.Army) });
            return (bool)s_TooSoonRetreatMethod.Invoke(ai, new object[] { army });
        }

        // --- ThinkMilitary vanilla method wrappers ---

        /// <summary>Instance method: void CalcBudget() — no params</summary>
        public static void CalcBudget(KingdomAI ai)
        {
            if (s_CalcBudgetMethod == null)
                s_CalcBudgetMethod = AccessTools.Method(typeof(KingdomAI), k_MethodCalcBudget, Type.EmptyTypes);
            s_CalcBudgetMethod.Invoke(ai, null);
        }

        /// <summary>Instance method: void ClearExpenses(WeightedRandom&lt;Expense&gt;)</summary>
        public static void ClearExpenses(KingdomAI ai, WeightedRandom<KingdomAI.Expense> expenses)
        {
            if (s_ClearExpensesMethod == null)
                s_ClearExpensesMethod = AccessTools.Method(typeof(KingdomAI), k_MethodClearExpenses, new[] { typeof(WeightedRandom<KingdomAI.Expense>) });
            s_ClearExpensesMethod.Invoke(ai, new object[] { expenses });
        }

        /// <summary>Instance method: IEnumerator CalcThreat() — no params</summary>
        public static IEnumerator CalcThreat(KingdomAI ai)
        {
            if (s_CalcThreatMethod == null)
                s_CalcThreatMethod = AccessTools.Method(typeof(KingdomAI), k_MethodCalcThreat, Type.EmptyTypes);
            return (IEnumerator)s_CalcThreatMethod.Invoke(ai, null);
        }

        /// <summary>Instance method: IEnumerator ThinkThreats() — no params</summary>
        public static IEnumerator ThinkThreats(KingdomAI ai)
        {
            if (s_ThinkThreatsMethod == null)
                s_ThinkThreatsMethod = AccessTools.Method(typeof(KingdomAI), k_MethodThinkThreats, Type.EmptyTypes);
            return (IEnumerator)s_ThinkThreatsMethod.Invoke(ai, null);
        }

        /// <summary>Instance method: IEnumerator ThinkHireUnits() — no params</summary>
        public static IEnumerator ThinkHireUnits(KingdomAI ai)
        {
            if (s_ThinkHireUnitsMethod == null)
                s_ThinkHireUnitsMethod = AccessTools.Method(typeof(KingdomAI), k_MethodThinkHireUnits, Type.EmptyTypes);
            return (IEnumerator)s_ThinkHireUnitsMethod.Invoke(ai, null);
        }

        /// <summary>Instance method: IEnumerator ThinkArmies() — no params</summary>
        public static IEnumerator ThinkArmies(KingdomAI ai)
        {
            if (s_ThinkArmiesMethod == null)
                s_ThinkArmiesMethod = AccessTools.Method(typeof(KingdomAI), k_MethodThinkArmies, Type.EmptyTypes);
            return (IEnumerator)s_ThinkArmiesMethod.Invoke(ai, null);
        }

        /// <summary>Instance method: IEnumerator SpendExpenses(WeightedRandom&lt;Expense&gt;)</summary>
        public static IEnumerator SpendExpenses(KingdomAI ai, WeightedRandom<KingdomAI.Expense> expenses)
        {
            if (s_SpendExpensesMethod == null)
                s_SpendExpensesMethod = AccessTools.Method(typeof(KingdomAI), k_MethodSpendExpenses, new[] { typeof(WeightedRandom<KingdomAI.Expense>) });
            return (IEnumerator)s_SpendExpensesMethod.Invoke(ai, new object[] { expenses });
        }
    }
}
