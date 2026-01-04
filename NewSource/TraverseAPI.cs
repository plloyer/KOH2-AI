using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using AIOverhaul.Constants;

namespace AIOverhaul
{
    /// <summary>
    /// Centralized API for accessing private members via Harmony Traverse.
    /// All method/field names are defined as constants to avoid hardcoded strings.
    /// </summary>
    public static class TraverseAPI
    {
        // Method name constants
        public const string METHOD_HIRE_CHARACTER = "HireCharacter";
        public const string METHOD_GET_MAX_COMMERCE = "GetMaxCommerce";
        public const string METHOD_CONSIDER_EXPENSE = "ConsiderExpense";
        public const string METHOD_GET_EXPENSE_CATEGORY = "GetExpenseCategory";
        public const string METHOD_SEND = "Send";
        public const string METHOD_FIND_NEAREST_OWN_CASTLE = "FindNearestOwnCastle";
        public const string METHOD_GET_SIDE = "GetSide";
        public const string METHOD_GET_WAR_SCORE = "GetSideScore";
        public const string METHOD_THINK_PROPOSE_OFFER_THREAD = "ThinkProposeOfferThread";
        public const string METHOD_CHOOSE_NEW_SKILL = "ChooseNewSkill";

        // Value constants
        public const string CONST_TITLE_KING = "King";

        public const string FIELD_SKILLS = "skills";
        public const string FIELD_CATEGORIES = "categories";

        // Character Methods/Fields (using ReflectionNames constants where possible to avoid dupes, or just defining here)
        // Since ReflectionNames exists, let's use it implicitly or duplicate the string if we want TraverseAPI self-contained.
        // User asked for "Traverse.* in Traverse API".
        public const string METHOD_IS_KING = "IsKing";
        public const string METHOD_GET_KINGDOM = "GetKingdom";
        public const string FIELD_TITLE = "title";

        // KingdomAI Methods
        // KingdomAI Methods

        public static float GetMaxCommerce(Logic.Kingdom kingdom)
        {
            return Traverse.Create(kingdom).Method(METHOD_GET_MAX_COMMERCE).GetValue<float>();
        }

        public static void ConsiderExpense(Logic.KingdomAI ai, Logic.KingdomAI.Expense.Type type, object defParam, object objectParam, Logic.KingdomAI.Expense.Category category, Logic.KingdomAI.Expense.Priority priority = Logic.KingdomAI.Expense.Priority.Normal, List<Logic.Value> args = null)
        {
            // Use AccessTools to find the specific overload with 6 parameters to avoid ambiguity
            // private void ConsiderExpense(Expense.Type type, BaseObject defParam, Object objectParam, Expense.Category category, Expense.Priority priority, List<Value> args)
            var method = AccessTools.FirstMethod(typeof(Logic.KingdomAI), m => m.Name == METHOD_CONSIDER_EXPENSE && m.GetParameters().Length == 6);
            if (method != null)
            {
                method.Invoke(ai, new object[] { type, defParam, objectParam, category, priority, args });
            }
            else
            {
                AIOverhaulPlugin.LogInfo($"[Error] Could not find method {METHOD_CONSIDER_EXPENSE} with 6 params", LogCategory.General);
            }
        }

        public static Logic.KingdomAI.Expense.Category GetExpenseCategory(object instance)
        {
            return (Logic.KingdomAI.Expense.Category)Traverse.Create(instance).Method(METHOD_GET_EXPENSE_CATEGORY).GetValue();
        }

        public static void SendArmy(Logic.KingdomAI ai, Logic.Army army, object target, string aiStatus, object extraParam = null)
        {
            // Vanilla "Send" method has 3 arguments: Send(Army army, Object target, string status)
            // It does not accept a 4th argument.
            var method = AccessTools.Method(typeof(Logic.KingdomAI), METHOD_SEND, new[] { typeof(Logic.Army), typeof(UnityEngine.Object), typeof(string) });
            
            // If Object doesn't work, try BaseObject or just loose matching
            if (method == null)
                 method = AccessTools.FirstMethod(typeof(Logic.KingdomAI), m => m.Name == METHOD_SEND && m.GetParameters().Length == 3);

            if (method != null)
            {
                method.Invoke(ai, new object[] { army, target, aiStatus });
            }
            else
            {
                 AIOverhaulPlugin.LogInfo($"[Error] Could not find method {METHOD_SEND} with 3 params", LogCategory.General);
            }
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
        // Character Accessors
        public static bool GetCharacterIsKing(Logic.Character character)
        {
            return Traverse.Create(character).Method(METHOD_IS_KING).GetValue<bool>();
        }

        public static string GetCharacterTitle(Logic.Character character)
        {
            return Traverse.Create(character).Field(FIELD_TITLE).GetValue<string>();
        }

        public static Logic.Kingdom GetCharacterKingdom(Logic.Character character)
        {
            return Traverse.Create(character).Method(METHOD_GET_KINGDOM).GetValue<Logic.Kingdom>();
        }
    }
}
