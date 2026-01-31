using System;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// Helper methods for kingdom operations
    /// </summary>
    public static class KingdomHelper
    {
        // Resource Access
        public static float GetGold(Logic.Kingdom k)
        {
            return k?.resources?[ResourceType.Gold] ?? 0f;
        }

        public static float GetFood(Logic.Kingdom k)
        {
            return k?.resources?[ResourceType.Food] ?? 0f;
        }

        public static float GetBooks(Logic.Kingdom k)
        {
            return k?.resources?.Get(ResourceType.Books) ?? 0f;
        }

        public static float GetGoldIncome(Logic.Kingdom k)
        {
            return k?.income?.Get(ResourceType.Gold) ?? 0f;
        }

        public static float GetFoodIncome(Logic.Kingdom k)
        {
            return k?.income?.Get(ResourceType.Food) ?? 0f;
        }

        // Validation Helpers
        public static bool IsValidKingdom(Logic.Kingdom k) => k != null && !k.IsDefeated();

        public static bool IsValidKingdomWithResources(Logic.Kingdom k) => k != null && k.resources != null;

        public static bool IsValidKingdomWithWarsAndResources(Logic.Kingdom k) => k != null && k.wars != null && k.resources != null && k.traditions != null;
        
        public static bool HasDisorder(this Logic.Kingdom k)
        {
            if (k == null || k.realms == null) return false;

            foreach (var realm in k.realms)
            {
                if (realm != null && realm.IsDisorder())
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetTradeAgreementCount(this Logic.Kingdom k)
        {
            if (k == null || k.game == null) return 0;

            // Iterate known kingdoms or just all active kingdoms to count trade agreements
            int count = 0;
            if (k.game.kingdoms != null)
            {
                foreach (var other in k.game.kingdoms)
                {
                    if (other != null && other != k && !other.IsDefeated())
                    {
                        if (k.HasTradeAgreement(other))
                            count++;
                    }
                }
            }
            return count;
        }
    }
}
