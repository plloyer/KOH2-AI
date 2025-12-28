using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace AIOverhaul
{
    public static class AILogger
    {
        private static string LogPath = System.IO.Path.Combine(Paths.ConfigPath, "AI_Performance_Log.csv");

        static AILogger()
        {
            if (!File.Exists(LogPath))
            {
                File.WriteAllText(LogPath, "Timestamp,KingdomName,AI_Type,RealmsCount,Gold,ArmiesCount,TotalStrength,AverageWarScore,WarsCount,TraditionsCount,KingWritingSkill,Defeated\n");
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
                bool isBaseline = AIOverhaulPlugin.IsBaselineAI(k);
                
                if (!isEnhanced && !isBaseline) continue;

                string aiType = isEnhanced ? "Enhanced" : "Baseline";
                float totalStr = WarLogicHelper.GetTotalPower(k);
                float avgWarScore = Traverse.Create(k.ai).Method("GetAverageWarScore").GetValue<float>();
                int wars = k.wars?.Count ?? 0;
                int traditionsCount = k.traditions?.Count ?? 0;
                int kingWritingSkill = k.royalFamily?.Sovereign?.GetSkillRank("Writing") ?? 0;

                string line = $"{timestamp},{k.Name},{aiType},{k.realms.Count},{k.resources[Logic.ResourceType.Gold]:F0},{k.armies.Count},{totalStr:F0},{avgWarScore:F2},{wars},{traditionsCount},{kingWritingSkill},False";
                lines.Add(line);
            }

            if (lines.Count > 0)
            {
                File.AppendAllLines(LogPath, lines);
            }
        }

        public static void LogDefeat(Logic.Kingdom k)
        {
            if (k == null) return;
            
            bool isEnhanced = AIOverhaulPlugin.IsEnhancedAI(k);
            bool isBaseline = AIOverhaulPlugin.IsBaselineAI(k);
            if (!isEnhanced && !isBaseline) return;

            string aiType = isEnhanced ? "Enhanced" : "Baseline";
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string line = $"{timestamp},{k.Name},{aiType},0,0,0,0,0,0,0,0,True\n";
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
