using AIOverhaul.Constants;

namespace AIOverhaul.Helpers
{
    /// <summary>
    /// Helper methods for tradition logic
    /// </summary>
    public static class TraditionHelper
    {
        /// <summary>
        /// Check if the kingdom should aggressively save resources to rush the first tradition.
        /// Criteria: No traditions yet, enough books (400+), and Writing or Learning is available.
        /// </summary>
        public static bool ShouldRushTradition(Logic.Kingdom kingdom)
        {
            if (kingdom == null) return false;
            
            // Already has traditions? No rush.
            if (kingdom.traditions != null && kingdom.traditions.Count > 0) return false;

            // Enough books?
            float currentBooks = KingdomHelper.GetBooks(kingdom);
            if (currentBooks < GameBalance.MinBooksForFirstTradition) return false;

            // Writing or Learning available?
            var options = kingdom.GetNewTraditionOptions();
            if (options == null) return false;

            return options.Find(t => t.id == TraditionNames.WritingTradition ||
                                     t.id == TraditionNames.LearningTradition) != null;
        }
    }
}
