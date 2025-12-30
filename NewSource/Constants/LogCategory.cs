namespace AIOverhaul.Constants
{
    /// <summary>
    /// Log categories for filtering and organizing mod logs
    /// </summary>
    public enum LogCategory
    {
        General,      // Miscellaneous logs
        War,          // War declarations, peace, surrenders
        Military,     // Army management, battles, fortifications
        Diplomacy,    // NAPs, alliances, trade agreements
        Economy,      // Merchants, buildings, resources
        Knights,      // Character hiring (all court members)
        Tradition,    // Tradition selection and adoption
        RoyalFamily,  // Heirs, succession, family management
        Governor,     // Governor assignments
        Spectator     // F9 spectator mode toggles
    }
}
