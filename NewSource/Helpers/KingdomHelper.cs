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

        // Court Slot Manipulation API
        
        /// <summary>
        /// Get the knight at a specific UI slot index (0-based)
        /// </summary>
        public static Logic.Character GetKnightAtSlot(Logic.Kingdom k, int index)
        {
            if (k?.court == null) return null;
            if (index < 0 || index >= k.court.Count) return null;
            
            return k.court[index];
        }

        /// <summary>
        /// Move a knight to a specific UI slot index (0-based).
        /// Returns true if successful.
        /// </summary>
        public static bool MoveKnightToSlot(Logic.Kingdom k, Logic.Character c, int targetIndex)
        {
            if (k?.court == null || c == null) return false;
            
            // Validate bounds
            if (targetIndex < 0 || targetIndex >= k.court.Count) return false;

            // Find current index
            int currentIndex = k.court.IndexOf(c);
            if (currentIndex == -1) return false; // Character not in court
            
            if (currentIndex == targetIndex) return true; // Already there

            // Move character
            k.court.RemoveAt(currentIndex);
            k.court.Insert(targetIndex, c);
            
            return true;
        }
        /// <summary>
        /// Organize the court members into specific slots based on their class.
        /// Rules:
        /// - 0: King (Fixed)
        /// - 1, 2: Marshals
        /// - 3: Diplomat (Priority) or Marshal
        /// - 4: Spare (Marshal/Spy/Other)
        /// - 5, 6, 7: Merchants
        /// - 8: Cleric (Last)
        /// </summary>
        public static void OrganizeCourt(Logic.Kingdom k)
        {
            if (k?.court == null || k.court.Count < 2) return; // Need at least 2 to organize

            int courtSize = k.court.Count; // Usually 9, but could vary
            var slots = new Logic.Character[courtSize];
            var unassigned = new System.Collections.Generic.List<Logic.Character>(k.court);

            // 1. Lock the King/Queen to Slot 0 (if present)
            // They are usually index 0, let's keep the current index 0 character if they are the sovereign
            // Or usually the first element is the King.
            // Let's identify the King/Sovereign properly.
            // Note: Logic.Character doesn't have IsKing property directly exposed here, 
            // but usually the first one in the list is the leader.
            // Let's assume the current Slot 0 is the King/Leader and keep them there.
            if (unassigned.Count > 0)
            {
                slots[0] = unassigned[0];
                unassigned.RemoveAt(0);
            }

            // 2. Identify remaining characters
            var marshals = new System.Collections.Generic.List<Logic.Character>();
            var merchants = new System.Collections.Generic.List<Logic.Character>();
            var diplomats = new System.Collections.Generic.List<Logic.Character>();
            var clerics = new System.Collections.Generic.List<Logic.Character>();
            var others = new System.Collections.Generic.List<Logic.Character>();

            foreach (var c in unassigned)
            {
                if (c == null) continue;
                string classId = c.class_def?.id;

                if (classId == CharacterClassNames.Marshal) marshals.Add(c);
                else if (classId == CharacterClassNames.Merchant) merchants.Add(c);
                else if (classId == CharacterClassNames.Diplomat) diplomats.Add(c);
                else if (classId == CharacterClassNames.Cleric) clerics.Add(c);
                else others.Add(c);
            }

            // 3. Assign preferred slots
            // Slot 8: Cleric (Last)
            if (courtSize > 8 && slots[8] == null && clerics.Count > 0)
            {
                slots[8] = clerics[0];
                clerics.RemoveAt(0);
            }

            // Slot 3: Diplomat
            if (courtSize > 3 && slots[3] == null)
            {
                if (diplomats.Count > 0)
                {
                    slots[3] = diplomats[0];
                    diplomats.RemoveAt(0);
                }
                else if (marshals.Count > 0) // Fallback to Marshal if user said Marshal 0-3
                {
                    // But maybe prioritize Marshals at 1, 2 first?
                    // Let's save one marshal for slot 3 if we have enough?
                    // Actually, let's fill 1 and 2 first.
                }
            }

            // Slots 5, 6, 7: Merchants
            for (int i = 5; i <= 7; i++)
            {
                if (courtSize > i && slots[i] == null && merchants.Count > 0)
                {
                    slots[i] = merchants[0];
                    merchants.RemoveAt(0);
                }
            }

            // Slots 1, 2: Marshals
            for (int i = 1; i <= 2; i++)
            {
                if (courtSize > i && slots[i] == null && marshals.Count > 0)
                {
                    slots[i] = marshals[0];
                    marshals.RemoveAt(0);
                }
            }
            
            // Revisit Slot 3 (if empty) -> Marshal, then others
            if (courtSize > 3 && slots[3] == null)
            {
                if (marshals.Count > 0)
                {
                    slots[3] = marshals[0];
                    marshals.RemoveAt(0);
                }
                else if (others.Count > 0)
                {
                    slots[3] = others[0];
                    others.RemoveAt(0);
                }
            }

            // 4. Fill all remaining slots (gaps) with remaining characters
            var remaining = new System.Collections.Generic.List<Logic.Character>();
            remaining.AddRange(marshals);
            remaining.AddRange(merchants);
            remaining.AddRange(diplomats);
            remaining.AddRange(clerics);
            remaining.AddRange(others);

            for (int i = 0; i < courtSize; i++)
            {
                if (slots[i] == null && remaining.Count > 0)
                {
                    slots[i] = remaining[0];
                    remaining.RemoveAt(0);
                }
            }

            // 5. Apply changes to kingdom court
            k.court.Clear();
            for (int i = 0; i < courtSize; i++)
            {
                if (slots[i] != null)
                {
                    k.court.Add(slots[i]);
                }
            }
            
            // If any somehow remaining (shouldn't happen if logic is correct), add them back
            if (remaining.Count > 0)
            {
                k.court.AddRange(remaining);
            }
        }
    }
}
