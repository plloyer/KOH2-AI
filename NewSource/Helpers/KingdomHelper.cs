using System.Linq;
using AIOverhaul.Constants;

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

        public static float GetBooks(Logic.Kingdom k)
        {
            return k?.resources?.Get(Logic.ResourceType.Books) ?? 0f;
        }

        public static float GetGoldIncome(Logic.Kingdom k)
        {
            return k?.income?.Get(Logic.ResourceType.Gold) ?? 0f;
        }

        // Court Member Counting
        public static int CountCourtMembers(Logic.Kingdom k, string classId)
        {
            if (k?.court == null) return 0;
            return k.court.Count(c => c != null && c.class_def?.id == classId);
        }

        public static int CountMerchants(Logic.Kingdom k)
        {
            return CountCourtMembers(k, CharacterClassNames.Merchant);
        }

        public static int CountClerics(Logic.Kingdom k)
        {
            return CountCourtMembers(k, CharacterClassNames.Cleric);
        }

        public static bool HasCleric(Logic.Kingdom k)
        {
            return k?.court?.Any(c => c != null && c.IsCleric()) ?? false;
        }

        // Army Checks
        public static bool HasTwoReadyArmies(Logic.Kingdom kingdom)
        {
            if (kingdom?.armies == null || kingdom.armies.Count < GameBalance.FirstTwoArmiesCount)
                return false;

            int readyArmies = 0;
            for (int i = 0; i < System.Math.Min(GameBalance.FirstTwoArmiesCount, kingdom.armies.Count); i++)
            {
                var army = kingdom.armies[i];
                if (army == null) continue;

                bool isFull = army.units.Count >= GameBalance.FullArmySize;
                int strength = army.EvalStrength();
                bool hasStrength = strength >= GameBalance.MinArmyStrengthForFortification;

                if (isFull && hasStrength)
                    readyArmies++;
            }

            return readyArmies >= GameBalance.FirstTwoArmiesCount;
        }

        // Validation Helpers
        public static bool IsValidKingdom(Logic.Kingdom k)
        {
            return k != null && !k.IsDefeated();
        }

        public static bool IsValidKingdomWithResources(Logic.Kingdom k)
        {
            return k != null && k.resources != null;
        }

        public static bool IsValidKingdomWithWarsAndResources(Logic.Kingdom k)
        {
            return k != null && k.wars != null && k.resources != null && k.traditions != null;
        }
    }
}
