namespace AIOverhaul.Constants
{
    /// <summary>
    /// Game balance constants for AI behavior tuning
    /// </summary>
    public static class GameBalance
    {
        // Battle Thresholds
        public const float MinBattleWinChance = 0.45f;
        public const float HealthRetreatThreshold = 0.7f;

        // Army Composition - Early Game (First Two Armies)
        public const int FirstTwoArmiesCount = 2;
        public const int EarlyGameRangedCount = 4;
        public const int EarlyGameMeleeCount = 4;

        // Army Composition - Late Game
        public const int FullArmySize = 8;
        public const float LateGameRangedMeleeRatio = 0.8f; // 4:5 ratio (3.5:4.5)
        public const float RatioToleranceLow = 0.9f; // 90% of target ratio

        // Army Strength Requirements
        public const int MinArmyStrengthForFortification = 250;
        public const int MinArmyStrengthPerRealm = 250;
        public const int MinFullArmyUnits = 4;

        // Evaluation Multipliers
        public const float StrongBoostMultiplier = 2.0f;
        public const float MediumBoostMultiplier = 1.5f;
        public const float WeakBoostMultiplier = 1.3f;
        public const float StrongPenaltyMultiplier = 0.1f;
        public const float MediumPenaltyMultiplier = 0.2f;
        public const float HighPriorityMultiplier = 0.7f; // Lower eval = higher priority
        public const float StrictBlockMultiplier = 0.01f;

        // War Score Thresholds (negative = losing)
        public const float WarScorePeaceSeeking = -15f;
        public const float WarScoreSurvival = -20f;
        public const float WarScoreIndependence = -30f;
        public const float WarScoreSurrender = -40f;
        public const float WarScoreDesperateIndependence = -10f;

        // Diplomacy - Power Ratios
        public const float PowerRatioSoloCapable = 2.0f; // We can handle alone if 2x stronger
        public const float PowerRatioWeakAttack = 1.5f; // Don't attack if 1.5x weaker
        public const float PowerRatioVeryWeak = 2.5f; // Never attack if 2.5x weaker
        public const float PowerRatioThreatening = 0.75f; // Seek help if neighbors are 75%+ our power
        public const float PowerRatioCombinedThreat = 2.0f; // Defer war if combined threat is 2x stronger
        public const float PowerRatioStrongerEnemy = 1.3f; // Consider peace if enemy 1.3x stronger

        // Diplomacy - Target Selection
        public const float TargetPowerRatioMin = 0.3f;
        public const float TargetPowerRatioMax = 1.0f;
        public const float TargetWeaknessScore = 100f;
        public const float TargetRelationshipMultiplier = 0.1f;

        // Diplomacy Thresholds
        public const float MinGoldForDiplomacy = 5000f;
        public const float FriendlyRelationshipThreshold = 200f;
        public const int MaxAlliesCount = 2;
        public const int MaxWarsCount = 2;
        public const int MinTradeCountForNAP = 3;

        // Diplomacy - War Preparation
        public const int MinArmiesForWar = 2;

        // Economy - Court Hiring
        public const int RequiredMerchantCount = 2;
        public const float MinGoldForCourtHiring = 500f;
        public const float MinGoldIncomeForSpies = 500f;
        public const float MinGoldIncomeForClerics = 50f;
        public const int CommercePerMerchant = 10;

        // Economy - Resource Thresholds
        public const float MinBooksForFirstTradition = 400f;
        public const float HighBooksThreshold = 350f;
        public const float LowGoldThreshold = 2000f;
        public const float HighBuildingEvalBoost = 20f;

        // Kingdom Selection
        public const float EnhancedAISelectionPercentage = 0.30f;

        // Building Bonuses
        public const float ReligionBuildingBoostPerSlot = 0.2f; // 20% per religion slot
        public const float BarracksSlotBoostPerSlot = 0.25f; // 25% per barracks slot

        // Island Detection
        public const int IslandMaxNeighbors = 1;
        public const int IslandMinRealms = 2;

        // Time Conversion
        public const float HoursPerDay = 24f;
        public const float DaysPerYear = 365f;

        // Logging
        public const int AggregateLogInterval = 50; // Log every 50 cycles

        // Misc
        public const float MinYearsElapsedForStats = 0.1f; // Avoid division by zero
        public const int KingdomSideAttackLevel = 3; // Level.Attack or higher
    }
}
