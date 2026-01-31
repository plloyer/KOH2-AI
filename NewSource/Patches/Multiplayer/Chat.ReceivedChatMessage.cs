using HarmonyLib;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Logic.Chat), "ReceivedChatMessage")]
    public static class Chat_ReceivedChatMessage
    {
        public static bool Prefix(Logic.Chat __instance, Logic.Campaign campaign, string playerId, string message)
        {
            // Check if this is a command
            if (MultiplayerAIHelper.HandleChatCommand(campaign, playerId, message))
            {
                // If it was a valid command, swallow the message (don't show in chat)
                AIOverhaulPlugin.LogInfo($"Intercepted Chat Command from {playerId}: {message}", LogCategory.Spectator);
                return false;
            }

            return true;
        }
    }
}
