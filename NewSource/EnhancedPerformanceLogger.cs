using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Logic;
using IOPath = System.IO.Path;

namespace AIOverhaul
{
    /// <summary>
    /// Enhanced performance logging with statistical rigor:
    /// - Baseline tracking for growth rate calculations
    /// - Normalized metrics relative to starting conditions
    /// - Aggregate statistics for comparative analysis
    /// - Expanded success indicators
    /// </summary>
    public static class EnhancedPerformanceLogger
    {
        private static string PerformanceLogPath = IOPath.Combine(Paths.ConfigPath, "AI_Performance_Enhanced.csv");
        private static string BaselineLogPath = IOPath.Combine(Paths.ConfigPath, "AI_Baseline_Initial.csv");
        private static string AggregateLogPath = IOPath.Combine(Paths.ConfigPath, "AI_Aggregate_Stats.csv");

        private static Dictionary<int, KingdomBaseline> kingdomBaselines = new Dictionary<int, KingdomBaseline>();
        private static int logCounter = 0;
        private static readonly int AGGREGATE_LOG_INTERVAL = 50; // Log aggregate stats every 50 cycles

        static EnhancedPerformanceLogger()
        {
            InitializeLogFiles();
        }

        private static void InitializeLogFiles()
        {
            // Performance log
            if (!System.IO.File.Exists(PerformanceLogPath))
            {
                string header = "Timestamp,GameYear,KingdomName,AI_Type," +
                               // Current state
                               "RealmsCount,Gold,ArmiesCount,TotalStrength,WarsCount,TraditionsCount,BooksCount,VassalsCount,AlliesCount," +
                               // Growth rates (relative to baseline)
                               "RealmsGrowthRate,GoldGrowthRate,StrengthGrowthRate,TraditionsGrowthRate,BooksGrowthRate," +
                               // Normalized metrics (current / initial)
                               "RealmsRatio,StrengthRatio,GoldPerRealm,StrengthPerRealm," +
                               // Additional indicators
                               "KingWritingSkill,KingClass,YearsElapsed," +
                               // Status
                               "IsDefeated,SurvivalYears\n";
                System.IO.File.WriteAllText(PerformanceLogPath, header);
            }

            // Baseline log
            if (!System.IO.File.Exists(BaselineLogPath))
            {
                var dummy = new KingdomBaseline();
                System.IO.File.WriteAllText(BaselineLogPath, dummy.ToCsvHeader() + ",AI_Type\n");
            }

            // Aggregate stats log
            if (!System.IO.File.Exists(AggregateLogPath))
            {
                string header = "Timestamp,GameYear," +
                               "EnhancedCount,BaselineCount," +
                               "EnhancedAvgRealms,BaselineAvgRealms,RealmsRatio," +
                               "EnhancedAvgStrength,BaselineAvgStrength,StrengthRatio," +
                               "EnhancedAvgGold,BaselineAvgGold,GoldRatio," +
                               "EnhancedAvgBooks,BaselineAvgBooks,BooksRatio," +
                               "EnhancedDefeated,BaselineDefeated," +
                               "EnhancedSurvivalRate,BaselineSurvivalRate\n";
                System.IO.File.WriteAllText(AggregateLogPath, header);
            }
        }

        public static void RecordBaseline(Logic.Kingdom k, string aiType, Logic.Game game)
        {
            if (k == null || kingdomBaselines.ContainsKey(k.id)) return;

            var baseline = KingdomBaseline.Create(k, game);
            if (baseline == null) return;

            kingdomBaselines[k.id] = baseline;

            // Log to baseline file
            string line = baseline.ToCsvLine() + $",{aiType}";
            System.IO.File.AppendAllText(BaselineLogPath, line + "\n");
        }

        public static void LogState(Logic.Game game)
        {
            if (game == null || game.kingdoms == null) return;

            List<string> lines = new List<string>();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            float currentYear = KingdomBaseline.GetGameYear(game);

            foreach (var k in game.kingdoms)
            {
                if (k == null) continue;

                bool isEnhanced = AIOverhaulPlugin.IsEnhancedAI(k);
                bool isBaseline = AIOverhaulPlugin.IsBaselineAI(k);

                if (!isEnhanced && !isBaseline) continue;

                // Skip if defeated but log survival stats
                if (k.IsDefeated())
                {
                    LogDefeat(k, currentYear);
                    continue;
                }

                string aiType = isEnhanced ? "Enhanced" : "Baseline";

                // Get or create baseline
                if (!kingdomBaselines.ContainsKey(k.id))
                {
                    RecordBaseline(k, aiType, game);
                }

                KingdomBaseline baseline;
                if (!kingdomBaselines.TryGetValue(k.id, out baseline) || baseline == null) continue;

                // Current metrics
                int currentRealms = k.realms?.Count ?? 0;
                float currentGold = k.resources?[ResourceType.Gold] ?? 0;
                int currentArmies = k.armies?.Count ?? 0;
                float currentStrength = WarLogicHelper.GetTotalPower(k);
                int currentWars = k.wars?.Count ?? 0;
                int currentTraditions = k.traditions?.Count ?? 0;
                int currentBooks = k.books?.Count ?? 0;
                int currentVassals = k.vassalStates?.Count ?? 0;
                int currentAllies = k.allies?.Count ?? 0;

                // Time elapsed
                float yearsElapsed = currentYear - baseline.GameYear;
                if (yearsElapsed <= 0) yearsElapsed = 0.1f; // Avoid division by zero

                // Growth rates (per year)
                float realmsGrowthRate = (currentRealms - baseline.InitialRealms) / yearsElapsed;
                float goldGrowthRate = (currentGold - baseline.InitialGold) / yearsElapsed;
                float strengthGrowthRate = (currentStrength - baseline.InitialTotalStrength) / yearsElapsed;
                float traditionsGrowthRate = (currentTraditions - baseline.InitialTraditions) / yearsElapsed;
                float booksGrowthRate = (currentBooks - baseline.InitialBooks) / yearsElapsed;

                // Normalized ratios
                float realmsRatio = baseline.InitialRealms > 0 ? (float)currentRealms / baseline.InitialRealms : 0f;
                float strengthRatio = baseline.InitialTotalStrength > 0 ? currentStrength / baseline.InitialTotalStrength : 0f;
                float goldPerRealm = currentRealms > 0 ? currentGold / currentRealms : 0f;
                float strengthPerRealm = currentRealms > 0 ? currentStrength / currentRealms : 0f;

                // Character info
                int kingWritingSkill = k.royalFamily?.Sovereign?.GetSkillRank(SkillNames.Writing) ?? 0;
                string kingClass = k.royalFamily?.Sovereign?.class_name ?? "None";

                string line = $"{timestamp},{currentYear:F1},{k.Name},{aiType}," +
                             $"{currentRealms},{currentGold:F0},{currentArmies},{currentStrength:F0},{currentWars},{currentTraditions},{currentBooks},{currentVassals},{currentAllies}," +
                             $"{realmsGrowthRate:F2},{goldGrowthRate:F0},{strengthGrowthRate:F0},{traditionsGrowthRate:F2},{booksGrowthRate:F2}," +
                             $"{realmsRatio:F2},{strengthRatio:F2},{goldPerRealm:F0},{strengthPerRealm:F0}," +
                             $"{kingWritingSkill},{kingClass},{yearsElapsed:F1}," +
                             $"False,";

                lines.Add(line);
            }

            if (lines.Count > 0)
            {
                System.IO.File.AppendAllLines(PerformanceLogPath, lines);
            }

            // Periodic aggregate logging
            logCounter++;
            if (logCounter >= AGGREGATE_LOG_INTERVAL)
            {
                LogAggregateStats(game);
                logCounter = 0;
            }
        }

        public static void LogDefeat(Logic.Kingdom k, float currentYear)
        {
            if (k == null) return;

            bool isEnhanced = AIOverhaulPlugin.IsEnhancedAI(k);
            bool isBaseline = AIOverhaulPlugin.IsBaselineAI(k);
            if (!isEnhanced && !isBaseline) return;

            string aiType = isEnhanced ? "Enhanced" : "Baseline";

            // Update baseline with defeat info
            if (kingdomBaselines.TryGetValue(k.id, out var baseline))
            {
                if (!baseline.IsDefeated)
                {
                    baseline.MarkDefeated(currentYear);

                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string line = $"{timestamp},{currentYear:F1},{k.Name},{aiType}," +
                                 $"0,0,0,0,0,0,0,0,0," + // All current metrics zero
                                 $"0,0,0,0,0," + // Growth rates zero
                                 $"0,0,0,0," + // Ratios zero
                                 $"0,None,{baseline.SurvivalYears:F1}," +
                                 $"True,{baseline.SurvivalYears:F1}";

                    System.IO.File.AppendAllText(PerformanceLogPath, line + "\n");
                }
            }
        }

        private static void LogAggregateStats(Logic.Game game)
        {
            if (game == null || game.kingdoms == null) return;

            var enhanced = game.kingdoms.Where(k => k != null && AIOverhaulPlugin.IsEnhancedAI(k) && !k.IsDefeated()).ToList();
            var baseline = game.kingdoms.Where(k => k != null && AIOverhaulPlugin.IsBaselineAI(k) && !k.IsDefeated()).ToList();

            if (enhanced.Count == 0 && baseline.Count == 0) return;

            // Calculate averages
            float enhancedAvgRealms = enhanced.Count > 0 ? enhanced.Select(k => (float)(k.realms?.Count ?? 0)).Average() : 0;
            float baselineAvgRealms = baseline.Count > 0 ? (float)baseline.Average(k => k.realms?.Count ?? 0) : 0;

            float enhancedAvgStrength = enhanced.Count > 0 ? enhanced.Average(k => WarLogicHelper.GetTotalPower(k)) : 0;
            float baselineAvgStrength = baseline.Count > 0 ? baseline.Average(k => WarLogicHelper.GetTotalPower(k)) : 0;

            float enhancedAvgGold = enhanced.Count > 0 ? enhanced.Average(k => k.resources?[ResourceType.Gold] ?? 0) : 0;
            float baselineAvgGold = baseline.Count > 0 ? baseline.Average(k => k.resources?[ResourceType.Gold] ?? 0) : 0;

            float enhancedAvgBooks = enhanced.Count > 0 ? enhanced.Select(k => (float)(k.books?.Count ?? 0)).Average() : 0;
            float baselineAvgBooks = baseline.Count > 0 ? (float)baseline.Average(k => k.books?.Count ?? 0) : 0;

            // Calculate ratios (avoid division by zero)
            float realmsRatio = baselineAvgRealms > 0 ? enhancedAvgRealms / baselineAvgRealms : 0;
            float strengthRatio = baselineAvgStrength > 0 ? enhancedAvgStrength / baselineAvgStrength : 0;
            float goldRatio = baselineAvgGold > 0 ? enhancedAvgGold / baselineAvgGold : 0;
            float booksRatio = baselineAvgBooks > 0 ? enhancedAvgBooks / baselineAvgBooks : 0;

            // Count defeated
            int enhancedDefeated = kingdomBaselines.Values.Count(kb => kb.IsDefeated && AIOverhaulPlugin.EnhancedKingdomIds.Contains(kb.KingdomId));
            int baselineDefeated = kingdomBaselines.Values.Count(kb => kb.IsDefeated && AIOverhaulPlugin.BaselineKingdomIds.Contains(kb.KingdomId));

            int totalEnhanced = AIOverhaulPlugin.EnhancedKingdomIds.Count;
            int totalBaseline = AIOverhaulPlugin.BaselineKingdomIds.Count;

            float enhancedSurvivalRate = totalEnhanced > 0 ? (float)(totalEnhanced - enhancedDefeated) / totalEnhanced : 0;
            float baselineSurvivalRate = totalBaseline > 0 ? (float)(totalBaseline - baselineDefeated) / totalBaseline : 0;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            float currentYear = KingdomBaseline.GetGameYear(game);

            string line = $"{timestamp},{currentYear:F1}," +
                         $"{enhanced.Count},{baseline.Count}," +
                         $"{enhancedAvgRealms:F1},{baselineAvgRealms:F1},{realmsRatio:F2}," +
                         $"{enhancedAvgStrength:F0},{baselineAvgStrength:F0},{strengthRatio:F2}," +
                         $"{enhancedAvgGold:F0},{baselineAvgGold:F0},{goldRatio:F2}," +
                         $"{enhancedAvgBooks:F1},{baselineAvgBooks:F1},{booksRatio:F2}," +
                         $"{enhancedDefeated},{baselineDefeated}," +
                         $"{enhancedSurvivalRate:F2},{baselineSurvivalRate:F2}";

            System.IO.File.AppendAllText(AggregateLogPath, line + "\n");

            // Also log to console for immediate feedback
            AIOverhaulPlugin.Instance.Log($"[AI-Stats] Year {currentYear:F0}: Enhanced vs Baseline | " +
                                         $"Realms: {enhancedAvgRealms:F1} vs {baselineAvgRealms:F1} ({realmsRatio:P0}) | " +
                                         $"Strength: {enhancedAvgStrength:F0} vs {baselineAvgStrength:F0} ({strengthRatio:P0}) | " +
                                         $"Survival: {enhancedSurvivalRate:P0} vs {baselineSurvivalRate:P0}");
        }


        public static void ClearData()
        {
            kingdomBaselines.Clear();
            logCounter = 0;
        }
    }

    [HarmonyPatch(typeof(KingdomAI), "ThinkGeneral")]
    public class EnhancedLoggingPatch
    {
        static void Postfix(KingdomAI __instance)
        {
            AIOverhaulPlugin.InitializeEnhancedKingdoms(__instance.game);

            // Anchor logging to one kingdom's cycle to avoid duplicate logs
            if (__instance.kingdom.id == 1)
            {
                EnhancedPerformanceLogger.LogState(__instance.game);
            }
        }
    }
}
