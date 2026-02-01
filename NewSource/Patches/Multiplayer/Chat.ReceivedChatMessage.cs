using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Chat), "ReceivedChatMessage")]
    public static class Chat_ReceivedChatMessage
    {
        public static bool Prefix(Chat __instance, Campaign campaign, string playerId, string message)
        {
            // Check if this is a command
            if (MultiplayerAICommandHelper.HandleChatCommand(campaign, playerId, message))
            {
                // If it was a valid command, swallow the message (don't show in chat)
                AIOverhaulPlugin.LogInfo($"Intercepted Chat Command from {playerId}: {message}", LogCategory.Spectator);
                return false;
            }

            return true;
        }
    }
}
