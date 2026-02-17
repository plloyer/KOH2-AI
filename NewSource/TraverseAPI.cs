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

        // Cached MethodInfo for vanilla Send — avoids repeated reflection
        static MethodInfo s_SendMethod;

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
    }
}
