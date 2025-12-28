using BepInEx;
using HarmonyLib;
using Logic;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AIOverhaul
{
    public static class AILogger
    {
        private static string LogPath = System.IO.Path.Combine(Paths.ConfigPath, "AI_Performance_Log.csv");

        static AILogger()
        {
            if (!File.Exists(LogPath))
            {
                File.WriteAllText(LogPath, "Timestamp,KingdomName,IsEnhanced,RealmsCount,Gold,Treasury,ArmiesCount,TotalStrength,AverageWarScore,WarsCount,Defeated\n");
            }
        }

        public static void LogState(Logic.Game game)
        {
            if (game == null || game.kingdoms == null) return;
            List<string> lines = new List<string>();
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            foreach (var k in game.kingdoms)
            {
                if (k == null || k.IsDefeated()) continue;
                bool isEnhanced = AIOverhaulPlugin.IsEnhancedAI(k);
                float totalStr = WarLogicHelper.GetTotalPower(k);
                float avgWarScore = Traverse.Create(k.ai).Method("GetAverageWarScore").GetValue<float>();
                int wars = k.wars?.Count ?? 0;

                string line = $"{timestamp},{k.Name},{isEnhanced},{k.realms.Count},{k.resources[Logic.ResourceType.Gold]:F0},{k.resources[Logic.ResourceType.Gold]:F0},{k.armies.Count},{totalStr:F0},{avgWarScore:F2},{wars},False";
                lines.Add(line);
            }
            File.AppendAllLines(LogPath, lines);
        }

        public static void LogDefeat(Logic.Kingdom k)
        {
            if (k == null) return;
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            bool isEnhanced = AIOverhaulPlugin.IsEnhancedAI(k);
            string line = $"{timestamp},{k.Name},{isEnhanced},0,0,0,0,0,0,0,True\n";
            File.AppendAllText(LogPath, line);
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkGeneral")]
    public class LoggingPatch
    {
        static void Postfix(Logic.KingdomAI __instance)
        {
            AIOverhaulPlugin.InitializeEnhancedKingdoms(__instance.game);
            if (__instance.kingdom.id == 1) // Anchor logging to one kingdom's cycle
            {
                AILogger.LogState(__instance.game);
            }
        }
    }
}
