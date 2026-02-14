using System;
using System.Collections.Generic;

namespace AIOverhaul
{
    /// <summary>
    /// Helper methods for court management and character operations
    /// </summary>
    public static class CourtHelper
    {
        public static int CountCourtMembers(this Logic.Kingdom kingdom, string classId)
        {
            if (kingdom == null) return 0;
            int count = 0;
            bool kingCounted = false;
            var king = kingdom.GetKing();

            if (kingdom.court != null)
            {
                foreach (var character in kingdom.court)
                {
                    if (character != null && character.class_def?.id == classId)
                    {
                        count++;
                        if (character == king) kingCounted = true;
                    }
                }
            }

            // Ruler may not be in the court array — count them if they match
            if (!kingCounted && king != null && king.class_def?.id == classId)
                count++;

            return count;
        }

        public static int CountMerchants(this Logic.Kingdom k) => k.CountCourtMembers(CharacterClassNames.Merchant);

        public static int CountClerics(this Logic.Kingdom k) => k.CountCourtMembers(CharacterClassNames.Cleric);

        public static int CountDiplomats(this Logic.Kingdom k) => k.CountCourtMembers(CharacterClassNames.Diplomat);

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
        public static bool MoveKnightToSlot(this Logic.Kingdom k, Logic.Character c, int targetIndex)
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

        public static void OrganizeCourt(this Logic.Kingdom k)
        {
            if (k?.court == null || k.court.Count < 2) return; // Need at least 2 to organize

            int courtSize = k.court.Count;
            var slots = new Logic.Character[courtSize];
            var unassigned = new List<Logic.Character>(k.court);

            if (unassigned.Count > 0)
            {
                slots[0] = unassigned[0];
                unassigned.RemoveAt(0);
            }

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

            for (int i = 1; i <= 4; i++)
            {
                if (courtSize > i && slots[i] == null && marshals.Count > 0)
                {
                    slots[i] = marshals[0];
                    marshals.RemoveAt(0);
                }
            }

            for (int i = 5; i <= 9; i++)
            {
                if (courtSize > i && slots[i] == null && merchants.Count > 0)
                {
                    slots[i] = merchants[0];
                    merchants.RemoveAt(0);
                }
            }

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

            if (courtSize > 4 && slots[4] == null && diplomats.Count > 0)
            {
                slots[4] = diplomats[0];
                diplomats.RemoveAt(0);
            }

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
