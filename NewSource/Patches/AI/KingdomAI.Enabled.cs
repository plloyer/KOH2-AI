using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul.Patches.AI
{
    [HarmonyPatch(typeof(KingdomAI), "Enabled")]
    public class EnabledPatch
    {
        static bool Prefix(KingdomAI __instance, ref bool __result, KingdomAI.EnableFlags flag)
        {
            var kingdom = __instance?.kingdom;
            if (kingdom == null) return true;

            // Unified check: F9 spectator mode and /ai_on chat commands both route
            // through ForcedAIKingdoms, so a single check handles both scenarios.
            bool isForcedAI = MultiplayerAICommandHelper.IsAIForced(kingdom.id);

            if (isForcedAI)
            {
                // CRITICAL: Authority Check
                // In MP, AI must only run on the Host.
                // In Local Game, IsAuthority() is true for the player.
                if (!kingdom.IsAuthority())
                {
                    // Logic.KingdomAI.Enabled calls CheckAuthority internally if checkAuthority=true
                    // But if we bypass original method, we must be careful.
                    // The original method returns false if !IsAuthority().
                    // So we should verify authority before forcing true.
                    return true; // Use original method to handle failure (it returns false)
                }

                // Respect global AI switch (e.g. if game is paused/disabled)
                if (__instance.game != null && !__instance.game.ai.enabled)
                {
                    __result = false;
                    return false;
                }

                // BYPASS the internal 'enabled' bitmask check
                // Force return true to enable AI for this kingdom
                __result = true;
                return false; // Skip original method
            }

            return true; // Run original method
        }
    }
}
