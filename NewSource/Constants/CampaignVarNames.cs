using System;

namespace AIOverhaul
{
    public static class CampaignVarNames
    {
        /// <summary>
        /// The variable key used by the game to determine kingdom size for shattered world generation.
        /// </summary>
        public const string KingdomSize = "kingdom_size";

        // Configuration / Rules
        public const string TeamSize = "team_size";
        public const string MainGoal = "main_goal";
        public const string PickKingdom = "pick_kingdom";
        public const string KingdomName = "kingdom_name";
        public const string OriginRealm = "origin_realm";
        public const string MapSize = "map_size";
        public const string StartPeriod = "start_period";
        public const string AllowOffline = "allow_offline";

        // Runtime State
        public const string EndGameTriggered = "end_game_triggered";
        public const string EarlyEndTriggered = "early_end_triggered";
        public const string EndGameReason = "end_game_reason";
        public const string State = "state";
        public const string Players = "players";
        public const string PlayerKingdoms = "player_kingdoms";
        public const string KingdomTeams = "kingdom_teams";
        public const string FromSaveId = "from_save_id";
        public const string GameLoaded = "game_loaded";
        public const string Victor = "victor";
        public const string Team = "team";

        // --- AIOverhaul Custom Vars (Non-Vanilla) ---
        // Used to track AI-specific state between saves
        public const string MortalEnemyId = "aimod_mortal_enemy";           // Points to the kingdom_id of the current target kingdom
        public const string MortalEnemySovereignId = "aimod_mortal_enemy_sov"; // Points to the nid of the target sovereign
        public const string NemesisTeam = "aimod_nemesis";                     // Marks kingdom as part of the nemesis team (value=1)
        public const string NemesisTeamIds = "aimod_nemesis_ids";                // Comma-separated kingdom IDs in campaignData
        public const string AutoRefusePrefix = "aimod_ar_";                    // Prefix for auto-refuse offer settings (per offer type)

        // AI Behavior settings (spectator mode)
        public const string AISettingPrefix = "aimod_ai_";
        public const string AIDeclareWar = "aimod_ai_declare_war";
        public const string AIDefensive = "aimod_ai_defensive";
        public const string AIRushCastle = "aimod_ai_rush_castle";
        public const string AICanNAP = "aimod_ai_can_nap";
        public const string AIDemandSupport = "aimod_ai_demand_support";
        public const string AIMercCastleOnly = "aimod_ai_merc_castle_only";
    }
}
