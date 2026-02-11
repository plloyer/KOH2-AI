using System;

namespace AIOverhaul
{
    /// <summary>
    /// Game balance constants for AI behavior tuning
    /// </summary>
    public static class GameBalance
    {
        // Battle Thresholds
        public const float MinBattleWinChance = 0.45f;
        public const float HealthRetreatThreshold = 0.7f;


        // Military - Attack/Defense Thresholds
        public const float MinAttackStrengthRatio = 1.2f; // Must be stronger to attack
        public const float SallyOutStrengthRatio = 1.2f; // Must be stronger to sally out
        public const float SiegeRecallStrengthRatio = 1.0f; // Recalled force + defenders must match enemy to justify recall

        // Buddy System
        public const int MaxBuddyPairs = 2; // Max buddy pairs (4 marshals / 2)
        public const int MinBuddyUnitsToFollow = 4; // Follower needs this many units to follow leader
        public const float BuddyReevalIntervalMinutes = 0.5f; // Re-evaluate buddies interval (real time)

        // Army Composition - Late Game
        public const int FullArmySize = 8;
        public const int MaxRangedUnitsPerArmy = 4; // Hard cap on ranged units per army

        // Army Strength Requirements
        public const int FirstTwoArmiesCount = 2;
        public const int MinArmyStrengthForFortification = 250;
        public const int MinArmyStrengthPerRealm = 250;
        public const int MinFullArmyUnits = 4;

        // Attack Priority
        public const int DisorderAttackMaxDistance = 2; // Max provinces away to prioritize disorder attack

        // War Score Thresholds (negative = losing, positive = winning)
        public const float WarScorePeaceSeeking = -15f;
        public const float WarScoreSurvival = -20f;
        public const float WarScoreIndependence = -30f;
        public const float WarScoreSurrender = -40f;
        public const float WarScoreDesperateIndependence = -10f;
        public const float WarScoreRejectPeace = 10f; // Reject peace if winning by this much

        // Diplomacy - Power Ratios
        public const float PowerRatioSoloCapable = 2.0f; // We can handle alone if stronger by this ratio
        public const float PowerRatioStrongerEnemy = 1.3f; // Consider peace if enemy is stronger

        // Diplomacy Thresholds
        public const int MaxWarsCount = 2;
        public const float NeutralRelationThreshold = 0f; 
        public const float MinRelationToInviteToWar = 5f; 
        public const float FriendlyRelationshipThreshold = 200f;

        // Diplomacy - War Preparation
        public const int MinArmiesToDeclareWar = 2; 
        public const float FullHealthThreshold = 1f; // Unit considered replenished at full health

        // Diplomacy - Alliance Scoring
        public const int AllianceScoreFightingMortalEnemy = 10; // Ally already fighting our mortal enemy
        public const int AllianceScoreNeighborOfMortalEnemy = 5; // Ally is neighbor of our mortal enemy
        public const int AllianceScoreUnfriendlyNeighbor = 3; // Ally is unfriendly neighbor (shared border concern)

        // Economy - Resource Thresholds
        public const float MinBooksForFirstSkillUpgrade = 200f;
        public const float MinBooksForFirstTradition = 400f;

        // Kingdom Selection
        public const float EnhancedAISelectionPercentage = 0.30f;

        // Governor Logic
        public const float MerchantGovernorMarketBonus = 20f; // Bonus for merchant governor in town with market
        public const float MarshalEarlyGameBoost = 10000f; // Massive boost for Marshals in best military province
        public const float IronOreMilitaryBonus = 15f; // Bonus for Iron Ore in military potential calc

        // Building Priority Multipliers
        // CRITICAL: Higher eval = higher priority. Multiply eval by these values (eval *= multiplier) to increase priority.
        public const float ReligionBuildingBoostPerSlot = 0.2f; // Bonus per religion slot
        public const float BarracksSlotBoostPerSlot = 1f; // Bonus per castle district slot for barracks placement
        public const float BarracksPriorityMultiplier = 100.0f; // Very high priority on first barracks
        public const float HighPriorityBuildingMultiplier = 100.0f;

        // Time Conversion
        public const float HoursPerDay = 24f;
        public const float DaysPerYear = 365f;

        // Logging
        public const int AggregateLogInterval = 50; // Log interval (cycles)

        // Speed Control
        public const float HighSpeed = 20f; // F7 high speed toggle
        public const float UltraSpeed = 50f; // F8 ultra speed toggle
    }
}
