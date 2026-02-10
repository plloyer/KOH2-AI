using System;
using HarmonyLib;
using Logic;
using UnityEngine;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Chat), "ReceivedChatMessage")]
    public static class Chat_ReceivedChatMessage
    {
        public static bool Prefix(Chat __instance, Campaign campaign, string playerId, string message)
        {
            // Check if this is a ping message — intercept before chat commands
            Vector3 pingPos;
            string senderName;
            if (PingNetworkHelper.TryDecode(message, out pingPos, out senderName))
            {
                if (PingSystem.Instance != null)
                    PingSystem.Instance.ReceiveNetworkPing(pingPos, senderName);
                return false; // Swallow — don't show in chat
            }

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
