using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// Executes rebel prisoners to gain crown authority when not at max.
    /// Runs before vanilla logic so rebels get executed immediately.
    /// </summary>
    [HarmonyPatch(typeof(KingdomAI), "ThinkForeignPrisoners")]
    public static class KingdomAI_ThinkForeignPrisoners
    {
        [HarmonyPrefix]
        public static bool Prefix(KingdomAI __instance, ref bool __result)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            var kingdom = __instance.kingdom;
            if (kingdom.prisoners == null || kingdom.prisoners.Count == 0) return true;

            var crownAuthority = kingdom.GetCrownAuthority();
            if (crownAuthority == null) return true;

            // Skip if already at max crown authority
            if (crownAuthority.GetValue() >= crownAuthority.Max()) return true;

            // Check each prisoner (iterate backwards since we may remove)
            for (int i = kingdom.prisoners.Count - 1; i >= 0; i--)
            {
                var prisoner = kingdom.prisoners[i];
                if (prisoner == null) continue;

                // Execute rebel prisoners to gain crown authority
                if (prisoner.IsRebel())
                {
                    var killAction = kingdom.actions.Find(ActionNames.KillPrisoner) as KillPrisonerAction;
                    if (killAction != null)
                    {
                        killAction.target = prisoner;
                        killAction.Execute(prisoner);
                        AIOverhaulPlugin.LogDebug(
                            $"Executed rebel prisoner {prisoner.GetName()} for crown authority (current: {crownAuthority.GetValue()}/{crownAuthority.Max()})",
                            LogCategory.General,
                            kingdom);
                    }
                }
            }

            // Continue with vanilla logic for non-rebels
            return true;
        }
    }
}
