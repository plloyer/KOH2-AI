using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // Vanilla ConsiderUpgradeFortifications is too conservative: only upgrades at Border/Attack threat, Low priority,
    // and blocks if another military expense is pending. Enhanced AI upgrades proactively at Normal priority
    // once initial military build (Barracks + Swordsmith + Fletcher) is complete.
    [HarmonyPatch(typeof(KingdomAI), "ConsiderUpgradeFortifications")]
    public static class KingdomAI_ConsiderUpgradeFortifications
    {
        [HarmonyPrefix]
        public static bool Prefix(KingdomAI __instance, Castle castle, ref bool __result)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Don't spend on fortifications while build queue is still being processed
            var queue = Castle_ChooseBuildOption.GetBuildQueue(__instance.kingdom);
            if (queue != null && queue.Count > 0) { __result = true; return false; }

            if (!castle.CanUpgradeFortification()) { __result = true; return false; }
            if (!castle.CanAffordFortificationsUpgrade()) { __result = true; return false; }

            float currentLevy = __instance.kingdom.resources[ResourceType.Levy];
            float maxLevy = __instance.kingdom.GetStat(Stats.ks_max_levy);
            var priority = (maxLevy > 0f && currentLevy >= maxLevy) ? KingdomAI.Expense.Priority.High : KingdomAI.Expense.Priority.Normal;

            TraverseAPI.ConsiderExpense(__instance, KingdomAI.Expense.Type.UpgradeFortifications, null, castle, KingdomAI.Expense.Category.Military, priority);
            __result = true;
            return false;
        }
    }
}
