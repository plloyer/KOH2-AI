using System.Collections;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "ThinkMilitary")]
    public class KingdomAI_ThinkMilitary
    {
        static bool Prefix(KingdomAI __instance)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            BuddySystem.EvaluatePairs(__instance.kingdom);

            return true; // Always run vanilla after
        }

        // Ready-to-use replacement for vanilla ThinkMilitary.
        // To activate: change Prefix signature to include `ref IEnumerator __result`,
        // set `__result = VanillaLogic(__instance); return false;`
        static IEnumerator VanillaLogic(KingdomAI ai)
        {
            TraverseAPI.CalcBudget(ai);
            TraverseAPI.ClearExpenses(ai, ai.military_expenses);
            TraverseAPI.ClearExpenses(ai, ai.urgent_expenses);

            yield return CoopThread.Call("KingdomAI.CalcThreat", TraverseAPI.CalcThreat(ai));

            if (ai.game.path_finding?.data == null || !ai.game.path_finding.data.initted)
                yield break;

            BuddySystem.EvaluatePairs(ai.kingdom);

            if (ai.Enabled(KingdomAI.EnableFlags.Armies))
                yield return CoopThread.Call("ThinkThreats", TraverseAPI.ThinkThreats(ai));

            if (ai.Enabled(KingdomAI.EnableFlags.Units | KingdomAI.EnableFlags.Garrison))
                yield return CoopThread.Call("ThinkHireUnits", TraverseAPI.ThinkHireUnits(ai));
            if (ai.personality == KingdomAI.AIPersonality.RichArmies && ai.Enabled(KingdomAI.EnableFlags.Units | KingdomAI.EnableFlags.Garrison))
                yield return CoopThread.Call("ThinkHireUnits", TraverseAPI.ThinkHireUnits(ai));

            if (ai.Enabled(KingdomAI.EnableFlags.Armies))
                yield return CoopThread.Call("ThinkArmies", TraverseAPI.ThinkArmies(ai));

            if (ai.urgent_expenses.options.Count > 0)
                yield return CoopThread.Call("Spend urgent expenses", TraverseAPI.SpendExpenses(ai, ai.urgent_expenses));
            else
                yield return CoopThread.Call("Spend military expenses", TraverseAPI.SpendExpenses(ai, ai.military_expenses));
        }
    }
}
