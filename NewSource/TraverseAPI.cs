using System;
using System.Collections;
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
    }
}
