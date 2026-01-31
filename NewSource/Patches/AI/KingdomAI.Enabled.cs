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

            // Scenario 1: Spectator Mode (Local Player)
            bool isSpectator = AIOverhaulPlugin.SpectatorMode && kingdom.is_player;

            // Scenario 2: Multiplayer Forced AI (Host delegates for Client)
            bool isForcedMP = MultiplayerAIHelper.IsAIForced(kingdom.id);

            if (isSpectator || isForcedMP)
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
