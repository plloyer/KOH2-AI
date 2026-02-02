using System;
using System.Collections.Generic;
using Logic;

namespace AIOverhaul
{
    public static class MultiplayerAICommandHelper
    {
        // Tracks kingdom IDs where the AI is forced ON (even if it's a human player)
        // Key = Kingdom ID
        public static HashSet<int> ForcedAIKingdoms { get; } = new HashSet<int>();

        /// <summary>
        /// Enable AI for a specific kingdom ID (e.g. human player going AFK)
        /// </summary>
        public static void EnableAI(int kingdomId)
        {
            if (!ForcedAIKingdoms.Contains(kingdomId))
            {
                ForcedAIKingdoms.Add(kingdomId);
                var k = AIOverhaulPlugin.CurrentGame?.GetKingdom(kingdomId);
                AIOverhaulPlugin.LogInfo($"AI Enabled for kingdom {k?.Name ?? kingdomId.ToString()} (MP Command)", LogCategory.Spectator, k);
            }
        }

        /// <summary>
        /// Disable AI for a specific kingdom ID (return control to human)
        /// </summary>
        public static void DisableAI(int kingdomId)
        {
            if (ForcedAIKingdoms.Contains(kingdomId))
            {
                ForcedAIKingdoms.Remove(kingdomId);
                var k = AIOverhaulPlugin.CurrentGame?.GetKingdom(kingdomId);
                AIOverhaulPlugin.LogInfo($"AI Disabled for kingdom {k?.Name ?? kingdomId.ToString()} (MP Command)", LogCategory.Spectator, k);
            }
        }

        /// <summary>
        /// Check if AI should be forced on for this kingdom
        /// </summary>
        public static bool IsAIForced(int kingdomId)
        {
            return ForcedAIKingdoms.Contains(kingdomId);
        }

        /// <summary>
        /// Processes a chat message to see if it's a command.
        /// Returns true if it was a command and should be swallowed.
        /// </summary>
        public static bool HandleChatCommand(Campaign campaign, string senderPlayerId, string message)
        {
            if (string.IsNullOrEmpty(message) || !message.StartsWith("/")) return false;

            string cmd = message.Trim().ToLowerInvariant();

            if (cmd == "/ai_on" || cmd == "/ai_start")
            {
                int kingdomId = GetKingdomIdForPlayer(campaign, senderPlayerId);
                if (kingdomId != -1)
                {
                    EnableAI(kingdomId);
                    return true;
                }
            }
            else if (cmd == "/ai_off" || cmd == "/ai_stop")
            {
                int kingdomId = GetKingdomIdForPlayer(campaign, senderPlayerId);
                if (kingdomId != -1)
                {
                    DisableAI(kingdomId);
                    return true;
                }
            }

            return false;
        }

        static int GetKingdomIdForPlayer(Campaign campaign, string playerId)
        {
            if (campaign == null || string.IsNullOrEmpty(playerId)) return -1;

            int playerIndex = campaign.GetPlayerIndex(playerId);
            if (playerIndex >= 0 && playerIndex < campaign.playerDataPersistent.Length)
            {
                string kingdomName = campaign.GetKingdomName(playerIndex);
                if (!string.IsNullOrEmpty(kingdomName))
                {
                    var k = AIOverhaulPlugin.CurrentGame?.GetKingdom(kingdomName);
                    if (k != null) return k.id;
                }
            }
            return -1;
        }
    }
}
