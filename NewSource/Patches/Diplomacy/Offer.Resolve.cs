using HarmonyLib;
using System;

namespace AIOverhaul.Patches.Diplomacy
{
    /// <summary>
    /// This patch is used to accept, in spectator mode, diplomacy like if it was a normal AI (instant).
    /// </summary>
    [HarmonyPatch(typeof(Logic.Offer), "Resolve")]
    public class Offer_Resolve
    {
        static bool Prefix(Logic.Offer __instance, ref bool __result)
        {
            try
            {
                // 1. Run only for spectator mode
                if (!AIOverhaulPlugin.SpectatorMode ||
                    __instance.def.outcomes == null || __instance.def.outcomes.options == null || __instance.def.outcomes.options.Count == 0)
                    return true; // Let original handle degenerate cases

                // 2. Target Check - Use fully qualified Logic.Kingdom
                if (!(__instance.to is Logic.Kingdom kingdom)) return true;

                // 3. The Specific Case: Player Kingdom + Spectator Mode
                // Original code: if (kingdom.is_player ...) return false;
                if (!kingdom.is_player) return true; // Not player? Let original logic run.

                // 4. Ensure AI is actually enabled for Diplomacy
                if (kingdom.ai == null || !kingdom.ai.Enabled(Logic.KingdomAI.EnableFlags.Diplomacy)) return true;

                // 5. Execute AI Logic (Bypassing is_player check)
                Logic.Offer counterOffer;
                string answer = __instance.DecideAIAnswer(out counterOffer);

                if (answer == AIOverhaul.Constants.DiplomacyConstants.CounterOffer)
                {
                    __instance.answer = answer;
                    __instance.from.FireEvent("offer_answered", __instance);
                    __instance.to.FireEvent("offer_answered", __instance);
                    counterOffer.Send();
                }
                else
                {
                    string finalAnswer = answer ?? __instance.def.default_outcome ?? AIOverhaul.Constants.DiplomacyConstants.Decline;
                    __instance.Answer(finalAnswer);
                }

                AIOverhaulPlugin.LogInfo($"[Spectator] Auto-answered offer {__instance} with '{answer}' for {kingdom.Name}");

                __result = true; // Signal that resolution occurred
                return false;    // Skip original method
            }
            catch (Exception e)
            {
                AIOverhaulPlugin.LogError($"Error in Offer_Resolve patch: {e}");
                return true; // Fallback to original
            }
        }
    }
}
