using HarmonyLib;
using System.Collections;
using System.Collections.Generic;

namespace AIOverhaul
{
    /// <summary>
    /// Centralized API for accessing private members via Harmony Traverse.
    /// All method/field names are defined as constants to avoid hardcoded strings.
    /// </summary>
    public static class TraverseAPI
    {
        // Method name constants
        public const string METHOD_HIRE_KNIGHT = "HireKnight";
        public const string METHOD_GET_MAX_COMMERCE = "GetMaxCommerce";
        public const string METHOD_CONSIDER_EXPENSE = "ConsiderExpense";
        public const string METHOD_GET_EXPENSE_CATEGORY = "GetExpenseCategory";
        public const string METHOD_SEND = "Send";
        public const string METHOD_FIND_NEAREST_OWN_CASTLE = "FindNearestOwnCastle";
        public const string METHOD_GET_SIDE = "GetSide";
        public const string METHOD_GET_WAR_SCORE = "GetWarScore";
        public const string METHOD_THINK_PROPOSE_OFFER_THREAD = "ThinkProposeOfferThread";

        // Field name constants
        public const string FIELD_SKILLS = "skills";
        public const string FIELD_CATEGORIES = "categories";

        // KingdomAI Methods
        public static void HireKnight(Logic.KingdomAI ai, string knightClass)
        {
            Traverse.Create(ai).Method(METHOD_HIRE_KNIGHT, new object[] { knightClass }).GetValue();
        }

        public static float GetMaxCommerce(Logic.Kingdom kingdom)
        {
            return Traverse.Create(kingdom).Method(METHOD_GET_MAX_COMMERCE).GetValue<float>();
        }

        public static void ConsiderExpense(Logic.KingdomAI ai, Logic.KingdomAI.Expense.Type type, object defParam, object objectParam, Logic.KingdomAI.Expense.Category category, Logic.KingdomAI.Expense.Priority priority, List<Logic.Value> args = null)
        {
            Traverse.Create(ai).Method(METHOD_CONSIDER_EXPENSE,
                new[] { type, defParam, objectParam, category, priority, args }).GetValue();
        }

        public static Logic.KingdomAI.Expense.Category GetExpenseCategory(object instance)
        {
            return (Logic.KingdomAI.Expense.Category)Traverse.Create(instance).Method(METHOD_GET_EXPENSE_CATEGORY).GetValue();
        }

        public static void SendArmy(Logic.KingdomAI ai, Logic.Army army, object target, string aiStatus, object extraParam = null)
        {
            Traverse.Create(ai).Method(METHOD_SEND, new[] { army, target, aiStatus, extraParam }).GetValue();
        }

        public static Logic.Castle FindNearestOwnCastle(Logic.KingdomAI ai, Logic.Army army, bool allowGarrisoned)
        {
            return (Logic.Castle)Traverse.Create(ai).Method(METHOD_FIND_NEAREST_OWN_CASTLE, new object[] { army, allowGarrisoned }).GetValue();
        }

        public static IEnumerator ThinkProposeOfferThread(Logic.KingdomAI ai, Logic.Kingdom target, string offerRelChangeType)
        {
            return (IEnumerator)Traverse.Create(ai).Method(METHOD_THINK_PROPOSE_OFFER_THREAD, new object[] { target, offerRelChangeType }).GetValue();
        }

        // War Methods
        public static int GetWarSide(Logic.War war, Logic.Kingdom kingdom)
        {
            return Traverse.Create(war).Method(METHOD_GET_SIDE, new object[] { kingdom }).GetValue<int>();
        }

        public static float GetWarScore(Logic.War war, int side)
        {
            return Traverse.Create(war).Method(METHOD_GET_WAR_SCORE, new object[] { side }).GetValue<float>();
        }

        // Field Accessors
        public static List<Logic.Skill> GetSkills(object instance)
        {
            return Traverse.Create(instance).Field(FIELD_SKILLS).GetValue<List<Logic.Skill>>();
        }

        public static Logic.KingdomAI.CategoryData[] GetCategories(Logic.KingdomAI ai)
        {
            return Traverse.Create(ai).Field(FIELD_CATEGORIES).GetValue<Logic.KingdomAI.CategoryData[]>();
        }
    }
}
