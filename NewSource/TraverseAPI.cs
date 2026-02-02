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
        const string k_MethodGetMaxCommerce = "GetMaxCommerce";
        const string k_MethodConsiderExpense = "ConsiderExpense";
        const string k_MethodGetExpenseCategory = "GetExpenseCategory";
        const string k_MethodSend = "Send";
        const string k_MethodFindNearestOwnCastle = "FindNearestOwnCastle";
        const string k_MethodGetSide = "GetSide";
        const string k_MethodGetWarScore = "GetSideScore";
        const string k_MethodThinkProposeOfferThread = "ThinkProposeOfferThread";

        // Value constants
        public const string CONST_TITLE_KING = "King";

        const string k_FieldSkills = "skills";
        const string k_FieldCategories = "categories";

        // Character Methods/Fields
        const string k_MethodIsKing = "IsKing";
        const string k_MethodGetKingdom = "GetKingdom";
        const string k_FieldTitle = "title";

        public static float GetMaxCommerce(Logic.Kingdom kingdom)
        {
            return Traverse.Create(kingdom).Method(k_MethodGetMaxCommerce).GetValue<float>();
        }

        public static void ConsiderExpense(KingdomAI ai, KingdomAI.Expense.Type type, object defParam, object objectParam, KingdomAI.Expense.Category category, KingdomAI.Expense.Priority priority = KingdomAI.Expense.Priority.Normal, List<Value> args = null)
        {
            // Use AccessTools to find the specific overload with 6 parameters to avoid ambiguity
            // private void ConsiderExpense(Expense.Type type, BaseObject defParam, Object objectParam, Expense.Category category, Expense.Priority priority, List<Value> args)
            var method = AccessTools.FirstMethod(typeof(KingdomAI), m => m.Name == k_MethodConsiderExpense && m.GetParameters().Length == 6);
            if (method != null)
            {
                method.Invoke(ai, new[] { type, defParam, objectParam, category, priority, args });
            }
            else
            {
                AIOverhaulPlugin.LogError($"Could not find method {k_MethodConsiderExpense} with 6 params");
            }
        }

        public static KingdomAI.Expense.Category GetExpenseCategory(object instance)
        {
            return (KingdomAI.Expense.Category)Traverse.Create(instance).Method(k_MethodGetExpenseCategory).GetValue();
        }

        public static bool SendArmy(KingdomAI ai, Logic.Army army, MapObject target, string aiStatus, Logic.Battle battleViewBattle = null)
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

        public static Castle FindNearestOwnCastle(KingdomAI ai, Logic.Army army, bool allowGarrisoned)
        {
            return (Castle)Traverse.Create(ai).Method(k_MethodFindNearestOwnCastle, army, allowGarrisoned).GetValue();
        }

        public static IEnumerator ThinkProposeOfferThread(KingdomAI ai, Logic.Kingdom target, string offerRelChangeType)
        {
            return (IEnumerator)Traverse.Create(ai).Method(k_MethodThinkProposeOfferThread, target, offerRelChangeType).GetValue();
        }

        // War Methods
        public static int GetWarSide(War war, Logic.Kingdom kingdom)
        {
            return Traverse.Create(war).Method(k_MethodGetSide, kingdom).GetValue<int>();
        }

        public static float GetWarScore(War war, int side)
        {
            return Traverse.Create(war).Method(k_MethodGetWarScore, side).GetValue<float>();
        }

        // Field Accessors
        public static List<Skill> GetSkills(object instance)
        {
            return Traverse.Create(instance).Field(k_FieldSkills).GetValue<List<Skill>>();
        }

        public static KingdomAI.CategoryData[] GetCategories(KingdomAI ai)
        {
            return Traverse.Create(ai).Field(k_FieldCategories).GetValue<KingdomAI.CategoryData[]>();
        }
        // Character Accessors
        public static bool GetCharacterIsKing(Logic.Character character)
        {
            return Traverse.Create(character).Method(k_MethodIsKing).GetValue<bool>();
        }

        public static string GetCharacterTitle(Logic.Character character)
        {
            return Traverse.Create(character).Field(k_FieldTitle).GetValue<string>();
        }

        public static Logic.Kingdom GetCharacterKingdom(Logic.Character character)
        {
            return Traverse.Create(character).Method(k_MethodGetKingdom).GetValue<Logic.Kingdom>();
        }

        // Threat Methods
        const string k_MethodGetThreat = "GetThreat";

        public static KingdomAI.Threat GetThreat(KingdomAI ai, Logic.Realm realm)
        {
            return Traverse.Create(ai).Method(k_MethodGetThreat, realm).GetValue<KingdomAI.Threat>();
        }
    }
}
