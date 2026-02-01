using System;
using Logic;

namespace AIOverhaul
{
    public static class GameHelper
    {
        /// <summary>
        /// Get a district definition by name
        /// </summary>
        public static District.Def GetDistrictDefinition(this Game game, string districtName)
        {
            return game?.defs?.Get<District.Def>(districtName);
        }
    }
}
