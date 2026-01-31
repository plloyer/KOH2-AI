using HarmonyLib;

namespace AIOverhaul
{
    // "ThinkAssaultSiege" decides whether a besieging army should launch an assault on the castle.
    // Intent: AssaultLogicPatch
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkAssaultSiege")]
    public class KingdomAI_ThinkAssaultSiege
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Army a)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            if (a == null || a.battle == null) return true;

            // BUDDY SYSTEM: Followers should NOT decide to assault independently.
            // They follow the Leader's lead (which is handled by the battle mechanic or tactical follow).
            if (BuddySystem.IsFollower(a))
            {
                // return false to skip assault logic -> Stay in siege?
                // OR should we check if leader is assaulting?
                // Usually better to be passive and let leader drive.
                return false; 
            }

            // Fix: Battle.castle -> Battle.settlement as Castle
            var castle = a.battle.settlement as Logic.Castle;
            
            if (castle != null)
            {
                var realm = castle.GetRealm();
                if (realm != null)
                {
                    // Check if this castle is the main castle of the realm (The City)
                    if (castle == realm.castle)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
    }
}
