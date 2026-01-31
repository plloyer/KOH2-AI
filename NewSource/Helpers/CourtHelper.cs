using System;
using System.Collections.Generic;
using System.Linq;

namespace AIOverhaul
{
    /// <summary>
    /// Helper methods for court management and character operations
    /// </summary>
    public static class CourtHelper
    {
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

        public static bool HasIdleMerchant(Logic.Kingdom k)
        {
            if (k?.court == null) return false;

            foreach (var character in k.court)
            {
                if (character == null || character.class_def?.id != CharacterClassNames.Merchant) continue;

                // Check if this merchant has an active trade route
                bool hasTradeRoute = false;
                if (character.actions?.active != null)
                {
                    foreach (var action in character.actions.active)
                    {
                        string aid = action?.def?.id;
                        if (string.IsNullOrEmpty(aid)) continue;

                        if (aid == ActionNames.Trade || 
                            aid == ActionNames.TradeWithKingdom ||
                            aid == ActionNames.EstablishTradeRoute)
                        {
                            hasTradeRoute = true;
                            break;
                        }
                    }
                }

                if (!hasTradeRoute) return true; // Found an idle merchant
            }

            return false; // No idle merchants
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
            var unassigned = new List<Logic.Character>(k.court);

            // 1. Lock the King/Queen to Slot 0 (if present)
            if (unassigned.Count > 0)
            {
                slots[0] = unassigned[0];
                unassigned.RemoveAt(0);
            }

            // 2. Identify remaining characters
            var marshals = new List<Logic.Character>();
            var merchants = new List<Logic.Character>();
            var diplomats = new List<Logic.Character>();
            var clerics = new List<Logic.Character>();
            var others = new List<Logic.Character>();

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
            var remaining = new List<Logic.Character>();
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
                k.court.Add(slots[i]); // Add slot even if null
            }
            
            // If any somehow remaining, add them back
            if (remaining.Count > 0)
            {
                k.court.AddRange(remaining);
            }
        }
    }
}
