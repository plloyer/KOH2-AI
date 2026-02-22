using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// Undoes the AI difficulty income boost for player kingdoms with forced AI (spectator mode).
    /// Without this, toggling F9 causes the player's income to spike because the vanilla code applies
    /// AI resource boosts to all AI-controlled kingdoms, including force-enabled ones.
    /// </summary>
    [HarmonyPatch(typeof(Logic.Kingdom), "ApplyIncomeModifiers")]
    public class Kingdom_ApplyIncomeModifiers
    {
        static void Postfix(Logic.Kingdom __instance)
        {
            if (!__instance.is_player || !MultiplayerAICommandHelper.IsAIForced(__instance.id)) return;
            if (__instance.game == null) return;

            UndoBoost(__instance, ResourceType.Gold, "gold");
            UndoBoost(__instance, ResourceType.Books, "books");
            UndoBoost(__instance, ResourceType.Piety, "piety");
            UndoBoost(__instance, ResourceType.Food, "food");
            UndoBoost(__instance, ResourceType.Levy, "levy");
        }

        static void UndoBoost(Logic.Kingdom k, ResourceType rt, string key)
        {
            float boost = k.game.GetAIResourcesBoost(key) * k.balance_factor_income;
            if (boost <= 0f || boost == 1f) return;

            var income = Traverse.Create(k).Field("_income").GetValue<Resource>();
            if (income != null)
                income[rt] /= boost;
        }
    }
}
