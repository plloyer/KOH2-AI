using System.Collections.Generic;
using Logic;

namespace AIOverhaul
{
    public static class WarHelper
    {
        public static List<Logic.Kingdom> GetEnemiesInWar(this War war, Logic.Kingdom kingdom)
        {
            int ourSide = TraverseAPI.GetWarSide(war, kingdom);
            return (ourSide == 0) ? war.defenders : war.attackers;
        }
        
        public static List<Logic.Kingdom> GetAlliesInWar(this War war, Logic.Kingdom kingdom)
        {
            int ourSide = TraverseAPI.GetWarSide(war, kingdom);
            return (ourSide == 0) ? war.attackers : war.defenders;
        }
    }
}
