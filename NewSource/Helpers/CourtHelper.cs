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
        public static int CountCourtMembers(this Logic.Kingdom k, string classId)
        {
            if (k?.court == null) return 0;
            return k.court.Count(c => c != null && c.class_def?.id == classId);
        }

        public static int CountMerchants(this Logic.Kingdom k)
        {
            return CountCourtMembers(k, CharacterClassNames.Merchant);
        }

        public static int CountClerics(this Logic.Kingdom k)
        {
            return CountCourtMembers(k, CharacterClassNames.Cleric);
        }

        public static int CountDiplomats(this Logic.Kingdom k)
        {
            return CountCourtMembers(k, CharacterClassNames.Diplomat);
        }

        public static bool HasCleric(this Logic.Kingdom k)
        {
            return k?.court?.Any(c => c != null && c.IsCleric()) ?? false;
        }

        public static bool HasIdleMerchant(this Logic.Kingdom k)
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
        public static Logic.Character GetKnightAtSlot(this Logic.Kingdom k, int index)
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
        /// <summary>
        /// Organize the court members into specific slots based on their class.
        /// Rules (User Request):
        /// - 0: King (Fixed)
        /// - 1-4: Marshals (Priority 1)
        /// - 5-9: Merchants (Priority 2)
        /// - 9, 8: Clerics (Priority 3, if available)
        /// - 4: Diplomat (Only if less than 4 marshals)
        /// - Remaining: Fill gaps
        /// </summary>
        public static void OrganizeCourt(Logic.Kingdom k)
        {
            if (k?.court == null || k.court.Count < 2) return; // Need at least 2 to organize

            int courtSize = k.court.Count;
            var slots = new Logic.Character[courtSize];
            var unassigned = new List<Logic.Character>(k.court);

            // 1. Lock the King/Queen to Slot 0 (if present)
            // Assuming King is usually the first added or identifier 0, 
            // but we should check existing slots if we want to be safe. 
            // Usually unassigned[0] is King/Sovereign.
            if (unassigned.Count > 0)
            {
                slots[0] = unassigned[0];
                unassigned.RemoveAt(0);
            }

            // 2. Separate by Class
            var marshals = new List<Logic.Character>();
            var merchants = new List<Logic.Character>();
            var clerics = new List<Logic.Character>();
            var diplomats = new List<Logic.Character>();
            var spies = new List<Logic.Character>();
            var others = new List<Logic.Character>();

            foreach (var c in unassigned)
            {
                if (c == null) continue;
                string classId = c.class_def?.id;

                if (classId == CharacterClassNames.Marshal) marshals.Add(c);
                else if (classId == CharacterClassNames.Merchant) merchants.Add(c);
                else if (classId == CharacterClassNames.Cleric) clerics.Add(c);
                else if (classId == CharacterClassNames.Diplomat) diplomats.Add(c);
                else if (classId == CharacterClassNames.Spy) spies.Add(c);
                else others.Add(c);
            }

            // 3. Assign Slots by Priority

            // Priority 1: Marshals -> Slots 1, 2, 3, 4
            for (int i = 1; i <= 4; i++)
            {
                if (courtSize > i && slots[i] == null && marshals.Count > 0)
                {
                    slots[i] = marshals[0];
                    marshals.RemoveAt(0);
                }
            }

            // Priority 2: Merchants -> Slots 5, 6, 7, 8, 9 (Indices 5-9)
            // Note: Indices 5 through 8 (if size 9)
            for (int i = 5; i <= 9; i++)
            {
                if (courtSize > i && slots[i] == null && merchants.Count > 0)
                {
                    slots[i] = merchants[0];
                    merchants.RemoveAt(0);
                }
            }

            // Priority 3: Clerics -> Slot 9, then 8 (Indices 9, 8)
            // "Cleric start from slot 9... if 2, then slot 8... doesn't have priority on merchant"
            // We check if slot is empty (Merchants took 5-9 first).
            if (courtSize > 9 && slots[9] == null && clerics.Count > 0)
            {
                slots[9] = clerics[0];
                clerics.RemoveAt(0);
            }
            if (courtSize > 8 && slots[8] == null && clerics.Count > 0)
            {
                slots[8] = clerics[0];
                clerics.RemoveAt(0);
            }

            // Priority 4: Diplomat catch -> Slot 4 IF empty (meaning < 4 Marshals)
            if (courtSize > 4 && slots[4] == null && diplomats.Count > 0)
            {
                slots[4] = diplomats[0];
                diplomats.RemoveAt(0);
            }

            // 4. Fill Remaining Gaps
            // "find an available spot... Same for spies"
            // Include remaining Marshals/Merchants/Clerics/Diplomats/Spies/Others
            var remaining = new List<Logic.Character>();
            remaining.AddRange(marshals);
            remaining.AddRange(merchants);
            remaining.AddRange(clerics); 
            remaining.AddRange(diplomats);
            remaining.AddRange(spies);
            remaining.AddRange(others);

            for (int i = 1; i < courtSize; i++)
            {
                if (slots[i] == null && remaining.Count > 0)
                {
                    slots[i] = remaining[0];
                    remaining.RemoveAt(0);
                }
            }

            // 5. Apply to Court List
            k.court.Clear();
            for (int i = 0; i < courtSize; i++)
            {
                k.court.Add(slots[i]);
            }
            if (remaining.Count > 0)
            {
                k.court.AddRange(remaining);
            }
        }
    }
}
