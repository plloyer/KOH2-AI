using System;
using System.Collections.Generic;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// Tracks the initial state of a kingdom for baseline comparison and growth rate calculations
    /// </summary>
    public class KingdomBaseline
    {
        /// <summary>
        /// Escapes a string for CSV output by wrapping in quotes and escaping internal quotes
        /// </summary>
        static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
        public int KingdomId { get; set; }
        public string KingdomName { get; set; }
        public DateTime RecordedAt { get; set; }
        public float GameYear { get; set; }

        // Starting metrics
        public int InitialRealms { get; set; }
        public float InitialGold { get; set; }
        public int InitialArmies { get; set; }
        public float InitialTotalStrength { get; set; }
        public int InitialWars { get; set; }
        public int InitialTraditions { get; set; }
        public int InitialBooks { get; set; }
        public int InitialVassals { get; set; }
        public int InitialAllies { get; set; }

        // Geographic/situational factors
        public int NeighborCount { get; set; }
        public float NeighborAvgStrength { get; set; }
        public bool IsIsland { get; set; }
        public string Religion { get; set; }

        // Track if kingdom was defeated and when
        public bool IsDefeated { get; set; }
        public DateTime? DefeatedAt { get; set; }
        public float? SurvivalYears { get; set; }

        public static KingdomBaseline Create(Logic.Kingdom k, Logic.Game game)
        {
            if (k == null) return null;

            var baseline = new KingdomBaseline
            {
                KingdomId = k.id,
                KingdomName = k.Name,
                RecordedAt = DateTime.Now,
                GameYear = GetGameYear(game),

                InitialRealms = k.realms?.Count ?? 0,
                InitialGold = k.resources?[ResourceType.Gold] ?? 0,
                InitialArmies = k.armies?.Count ?? 0,
                InitialTotalStrength = WarLogicHelper.GetTotalPower(k),
                InitialWars = k.wars?.Count ?? 0,
                InitialTraditions = k.traditions?.Count ?? 0,
                InitialBooks = k.books?.Count ?? 0,
                InitialVassals = k.vassalStates?.Count ?? 0,
                InitialAllies = k.allies?.Count ?? 0,

                NeighborCount = k.neighbors?.Count ?? 0,
                NeighborAvgStrength = CalculateNeighborAvgStrength(k),
                IsIsland = CheckIfIsland(k),
                Religion = k.religion?.name ?? "Unknown",

                IsDefeated = false
            };

            return baseline;
        }

        public void MarkDefeated(float currentGameYear)
        {
            IsDefeated = true;
            DefeatedAt = DateTime.Now;
            SurvivalYears = currentGameYear - GameYear;
        }

        static float CalculateNeighborAvgStrength(Logic.Kingdom k)
        {
            if (k?.neighbors == null || k.neighbors.Count == 0) return 0f;

            float totalStrength = 0f;
            int count = 0;

            foreach (var neighbor in k.neighbors)
            {
                if (neighbor != null && !neighbor.IsDefeated())
                {
                    totalStrength += WarLogicHelper.GetTotalPower(neighbor);
                    count++;
                }
            }

            return count > 0 ? totalStrength / count : 0f;
        }

        static bool CheckIfIsland(Logic.Kingdom k)
        {
            if (k?.realms == null) return false;

            // A kingdom is considered "island" if it has no land borders with other kingdoms
            // This is a simplification - checks if it has very few neighbors relative to realm count
            int neighborCount = k.neighbors?.Count ?? 0;
            int realmCount = k.realms.Count;

            return neighborCount <= 1 && realmCount > 2;
        }

        /// <summary>
        /// Calculate game year from session time (hours / 24 / 365 = years)
        /// Assumes 1 game year = 365 game days
        /// </summary>
        public static float GetGameYear(Logic.Game game)
        {
            if (game == null) return 0f;
            float hours = game.session_time.hours;
            return hours / 24f / 365f; // Convert hours to years
        }

        public string ToCsvHeader()
        {
            return "KingdomId,KingdomName,RecordedAt,GameYear,InitialRealms,InitialGold,InitialArmies,InitialTotalStrength," +
                   "InitialWars,InitialTraditions,InitialBooks,InitialVassals,InitialAllies," +
                   "NeighborCount,NeighborAvgStrength,IsIsland,Religion,IsDefeated,DefeatedAt,SurvivalYears";
        }

        public string ToCsvLine()
        {
            return $"{KingdomId},{EscapeCsv(KingdomName)},{RecordedAt:yyyy-MM-dd HH:mm:ss},{GameYear:F1}," +
                   $"{InitialRealms},{InitialGold:F0},{InitialArmies},{InitialTotalStrength:F0}," +
                   $"{InitialWars},{InitialTraditions},{InitialBooks},{InitialVassals},{InitialAllies}," +
                   $"{NeighborCount},{NeighborAvgStrength:F0},{IsIsland},{EscapeCsv(Religion)}," +
                   $"{IsDefeated},{(DefeatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "")},{(SurvivalYears?.ToString("F1") ?? "")}";
        }
    }
}
