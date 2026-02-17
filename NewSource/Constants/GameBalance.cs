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
        public const float OverwhelmingStrengthRatio = 1.5f; // Top2 strength ratio to skip pillaging and go straight for castle
        public const float SallyOutStrengthRatio = 1.2f; // Must be stronger to sally out
        public const float SiegeRecallStrengthRatio = 1.0f; // Recalled force + defenders must match enemy to justify recall

        // Buddy System
        public const int MaxBuddyPairs = 2; // Max buddy pairs (4 marshals / 2)
        public const int MinBuddyUnitsToFollow = 7; // Follower needs this many units to follow leader
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

        // Diplomacy - AI Offer Cooldowns (seconds of game time)
        // Vanilla default is 0 for all offers. These override specific spammy offer types.
        public const float DemandSupportInWarCooldown = 3600f;    // 1 hour game-time
        public const float DemandAttackKingdomCooldown = 3600f;   // 1 hour game-time

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
        public const float MinBooksForGovernorSkills = 450f;

        // Economy - Early Game Build Order
        public const int MinMerchantsBeforeTradition = 2; // Hire this many merchants before saving for first tradition
        public const float CommercePerMerchant = 10f; // Commerce capacity required per merchant
        public const float MinCommerceForExtraMerchant = 30f; // Hire extra merchant when total commerce reaches this threshold
        public const int MinTradeAgreements = 3; // Proactively seek trade agreements when below this count
        public const int MinVillagesForMilitia = 2; // Realm needs this many villages to warrant VillageMilitia

        // Kingdom Selection
        public const float EnhancedAISelectionPercentage = 0.30f;

        // Governor Logic
        public const float MerchantGovernorGoodsBonus = 10f; // Per good produced — dominates village count
        public const float MarshalEarlyGameBoost = 100f; // Massive boost for Marshals in best military province

        // Building Priority Multipliers
        // CRITICAL: Higher eval = higher priority. Multiply eval by these values (eval *= multiplier) to increase priority.
        public const float BoostPerDistrict = 1000f; // Bonus per castle/farm/religious/sea district
        public const float HighPriorityBuildingMultiplier = 100.0f;

        // Time Conversion
        public const float HoursPerDay = 24f;
        public const float DaysPerYear = 365f;

        // Mercenary System
        public const float MercenarySpawnLimitMultiplier = 2f;
        public const int MaxMercsPerRealm = 1;
        public const float OutOfTerritoryMercenaryPriceMultiplier = 2.0f;

        // Build Queue
        public const float BuildQueueStallTimeoutSec = 900f; // 15 game-minutes in seconds

        // Speed Control
        public const float HighSpeed = 20f; // F7 high speed toggle
        public const float UltraSpeed = 50f; // F8 ultra speed toggle

        // Nemesis Team System - Scoring constants for AI team selection
        public const float NemesisHumanNeighborPenalty = 50f;  // Penalty for directly bordering a human player
        public const float NemesisAINeighborBonus = 10f;       // Bonus per AI neighbor (easier to cluster)
        public const float NemesisRealmCountBonus = 5f;        // Bonus per realm owned (prefer established kingdoms)
        public const float NemesisClusterAdjacencyBonus = 20f; // Bonus per existing nemesis member a candidate borders
        public const float NemesisPowerWeight = 0.01f;         // Weight for kingdom power in scoring
        public const int NemesisMinTeamSize = 2;               // Minimum viable nemesis team size

        // Nemesis Team System - Distance tuning (kingdom hops from human players)
        public const int NemesisIdealDistanceMin = 4;          // Ideal minimum hops from human players
        public const int NemesisIdealDistanceMax = 4;          // Ideal maximum hops from human players
        public const float NemesisIdealDistanceBonus = 30f;    // Bonus for being in the ideal range
        public const float NemesisCloseDistancePenaltyPerHop = 10f; // Penalty per hop below ideal min
        public const float NemesisFarDistancePenaltyPerHop = 10f;   // Penalty per hop above ideal max
    }
}
