namespace AIOverhaul.Helpers
{
    /// <summary>
    /// Helper methods for kingdom operations
    /// </summary>
    public static class KingdomHelper
    {
        // Resource Access
        public static float GetGold(Logic.Kingdom k)
        {
            return k?.resources?[Logic.ResourceType.Gold] ?? 0f;
        }

        public static float GetFood(Logic.Kingdom k)
        {
            return k?.resources?[Logic.ResourceType.Food] ?? 0f;
        }

        public static float GetBooks(Logic.Kingdom k)
        {
            return k?.resources?.Get(Logic.ResourceType.Books) ?? 0f;
        }

        public static float GetGoldIncome(Logic.Kingdom k)
        {
            return k?.income?.Get(Logic.ResourceType.Gold) ?? 0f;
        }



        // Validation Helpers
        public static bool IsValidKingdom(Logic.Kingdom k) => k != null && !k.IsDefeated();

        public static bool IsValidKingdomWithResources(Logic.Kingdom k) => k != null && k.resources != null;

        public static bool IsValidKingdomWithWarsAndResources(Logic.Kingdom k) => k != null && k.wars != null && k.resources != null && k.traditions != null;


    }
}
