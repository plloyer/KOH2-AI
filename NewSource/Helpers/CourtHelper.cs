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
    }
}
