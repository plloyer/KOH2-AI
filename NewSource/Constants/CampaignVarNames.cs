namespace AIOverhaul.Constants
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
    }
}
