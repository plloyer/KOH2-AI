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
    /// </summary>
    public static class TraverseAPI
    {
        // Method name constants
        public const string METHOD_GET_MAX_COMMERCE = "GetMaxCommerce";
        public const string METHOD_CONSIDER_EXPENSE = "ConsiderExpense";
        public const string METHOD_GET_EXPENSE_CATEGORY = "GetExpenseCategory";
        public const string METHOD_SEND = "Send";
        public const string METHOD_FIND_NEAREST_OWN_CASTLE = "FindNearestOwnCastle";
        public const string METHOD_GET_SIDE = "GetSide";
        public const string METHOD_GET_WAR_SCORE = "GetSideScore";
        public const string METHOD_THINK_PROPOSE_OFFER_THREAD = "ThinkProposeOfferThread";

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

        public static void ConsiderExpense(KingdomAI ai, KingdomAI.Expense.Type type, object defParam, object objectParam, KingdomAI.Expense.Category category, KingdomAI.Expense.Priority priority = KingdomAI.Expense.Priority.Normal, List<Value> args = null)
        {
            // Use AccessTools to find the specific overload with 6 parameters to avoid ambiguity
            // private void ConsiderExpense(Expense.Type type, BaseObject defParam, Object objectParam, Expense.Category category, Expense.Priority priority, List<Value> args)
            var method = AccessTools.FirstMethod(typeof(KingdomAI), m => m.Name == METHOD_CONSIDER_EXPENSE && m.GetParameters().Length == 6);
            if (method != null)
            {
                method.Invoke(ai, new[] { type, defParam, objectParam, category, priority, args });
            }
            else
            {
                AIOverhaulPlugin.LogError($"Could not find method {METHOD_CONSIDER_EXPENSE} with 6 params");
            }
        }

        public static KingdomAI.Expense.Category GetExpenseCategory(object instance)
        {
            return (KingdomAI.Expense.Category)Traverse.Create(instance).Method(METHOD_GET_EXPENSE_CATEGORY).GetValue();
        }

        public static bool SendArmy(KingdomAI ai, Logic.Army army, MapObject target, string aiStatus, Logic.Battle battleViewBattle = null)
        {
            // Vanilla "Send" method signature: private bool Send(Army army, MapObject target, string status, Battle battle_view_battle = null)
            try
            {
                var method = AccessTools.Method(typeof(KingdomAI), METHOD_SEND, new[] { typeof(Logic.Army), typeof(MapObject), typeof(string), typeof(Logic.Battle) });
                if (method != null)
                {
                    return (bool)method.Invoke(ai, new object[] { army, target, aiStatus, battleViewBattle });
                }

                AIOverhaulPlugin.LogError($"Could not find method {METHOD_SEND} with params (Army, MapObject, string, Battle)");
                return false;
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"Could not invoke method {METHOD_SEND}: {ex.Message}");
                return false;
            }
        }

        public static Castle FindNearestOwnCastle(KingdomAI ai, Logic.Army army, bool allowGarrisoned)
        {
            return (Castle)Traverse.Create(ai).Method(METHOD_FIND_NEAREST_OWN_CASTLE, army, allowGarrisoned).GetValue();
        }

        public static IEnumerator ThinkProposeOfferThread(KingdomAI ai, Logic.Kingdom target, string offerRelChangeType)
        {
            return (IEnumerator)Traverse.Create(ai).Method(METHOD_THINK_PROPOSE_OFFER_THREAD, target, offerRelChangeType).GetValue();
        }

        // War Methods
        public static int GetWarSide(War war, Logic.Kingdom kingdom)
        {
            return Traverse.Create(war).Method(METHOD_GET_SIDE, kingdom).GetValue<int>();
        }

        public static float GetWarScore(War war, int side)
        {
            return Traverse.Create(war).Method(METHOD_GET_WAR_SCORE, side).GetValue<float>();
        }

        // Field Accessors
        public static List<Skill> GetSkills(object instance)
        {
            return Traverse.Create(instance).Field(FIELD_SKILLS).GetValue<List<Skill>>();
        }

        public static KingdomAI.CategoryData[] GetCategories(KingdomAI ai)
        {
            return Traverse.Create(ai).Field(FIELD_CATEGORIES).GetValue<KingdomAI.CategoryData[]>();
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

        // Threat Methods
        public const string METHOD_GET_THREAT = "GetThreat";

        public static KingdomAI.Threat GetThreat(KingdomAI ai, Logic.Realm realm)
        {
            return Traverse.Create(ai).Method(METHOD_GET_THREAT, realm).GetValue<KingdomAI.Threat>();
        }
    }
}
