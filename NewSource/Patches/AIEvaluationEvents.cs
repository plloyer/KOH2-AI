using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// Static events for AI evaluation cycles
    /// </summary>
    public static class AIEvaluationEvents
    {
        /// <summary>
        /// Fired when a kingdom starts a new general evaluation cycle (economy, buildings, etc.)
        /// </summary>
        public static event Action<KingdomAI> OnGeneralEvaluationStart;

        /// <summary>
        /// Fired when a kingdom starts a new military evaluation cycle
        /// </summary>
        public static event Action<KingdomAI> OnMilitaryEvaluationStart;

        internal static void RaiseGeneralEvaluationStart(KingdomAI kingdomAI)
        {
            OnGeneralEvaluationStart?.Invoke(kingdomAI);
        }

        internal static void RaiseMilitaryEvaluationStart(KingdomAI kingdomAI)
        {
            OnMilitaryEvaluationStart?.Invoke(kingdomAI);
        }
    }

    /// <summary>
    /// Patch to detect when general evaluation (ThinkGeneral) starts
    /// </summary>
    [HarmonyPatch(typeof(KingdomAI), "ThinkGeneral")]
    public class KingdomAI_ThinkGeneral_Patch
    {
        static void Prefix(KingdomAI __instance)
        {
            // Only fire event for player kingdom (debug purposes)
            if (__instance?.kingdom != null && __instance.kingdom.is_player)
            {
                AIEvaluationEvents.RaiseGeneralEvaluationStart(__instance);
            }
        }
    }

    /// <summary>
    /// Patch to detect when military evaluation (ThinkMilitary) starts
    /// </summary>
    [HarmonyPatch(typeof(KingdomAI), "ThinkMilitary")]
    public class KingdomAI_ThinkMilitary_Patch
    {
        static void Prefix(KingdomAI __instance)
        {
            // Only fire event for player kingdom (debug purposes)
            if (__instance?.kingdom != null && __instance.kingdom.is_player)
            {
                AIEvaluationEvents.RaiseMilitaryEvaluationStart(__instance);
            }
        }
    }
}