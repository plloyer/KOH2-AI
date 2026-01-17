using HarmonyLib;

namespace AIOverhaul.Patches.Military
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
            
            // Paranoid check for army and battle
            if (a == null) return true;
            // Accessing a.battle might technically throw if 'a' is in a weird state, but usually property access is safe-ish
            if (a.battle == null) return true;

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
