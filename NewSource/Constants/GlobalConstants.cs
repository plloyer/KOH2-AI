using System;

namespace AIOverhaul
{
    public static class GlobalConstants
    {
        // Common Requirement Types
        public const string ReqType_Resource = "Resource";
        public const string ReqType_Region = "Region";

        // Region Keys
        // TODO: Move to a dedicated RegionConstants if this grows
        public const string Region_Europe = "Europe";

        // Resource Names (for exclusion logic)
        public const string Res_Gold = "Gold";
        public const string Res_Books = "Books";
        public const string Res_Piety = "Piety";
        public const string Res_Workers = "Workers";
        public const string Res_Levies = "Levies";
        public const string Res_Food = "Food";
        public const string Res_Hammers = "Hammers";
    }
}
