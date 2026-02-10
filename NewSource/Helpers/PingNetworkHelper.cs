using System.Globalization;
using Logic;
using UnityEngine;

namespace AIOverhaul
{
    public static class PingNetworkHelper
    {
        const string k_Prefix = "!ping ";

        public static string Encode(Vector3 position, string kingdomName)
        {
            return k_Prefix +
                   position.x.ToString("F1", CultureInfo.InvariantCulture) + "," +
                   position.y.ToString("F1", CultureInfo.InvariantCulture) + "," +
                   position.z.ToString("F1", CultureInfo.InvariantCulture) + "," +
                   kingdomName;
        }

        public static bool TryDecode(string message, out Vector3 position, out string kingdomName)
        {
            position = Vector3.zero;
            kingdomName = null;

            if (string.IsNullOrEmpty(message) || !message.StartsWith(k_Prefix))
                return false;

            string payload = message.Substring(k_Prefix.Length);
            // Format: x,y,z,KingdomName (name may contain commas, so split max 4)
            string[] parts = payload.Split(new[] { ',' }, 4);
            if (parts.Length < 4)
                return false;

            float x, y, z;
            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                return false;

            position = new Vector3(x, y, z);
            kingdomName = parts[3];
            return true;
        }

        public static void SendPing(Vector3 position, Logic.Kingdom kingdom)
        {
            var game = AIOverhaulPlugin.CurrentGame;
            if (game?.multiplayer?.chat == null) return;

            string message = Encode(position, kingdom.Name);
            game.multiplayer.chat.SendInGameChatMessage(Chat.Channel.All, message, null);
        }
    }
}
