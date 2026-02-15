using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "MercenaryMission.Def.GetCost(Mercenary, Kingdom)" computes the hiring cost for a mercenary.
    // Intent: Double the cost when hiring a mercenary outside your own territory.
    [HarmonyPatch(typeof(MercenaryMission.Def), "GetCost", new[] { typeof(Mercenary), typeof(Logic.Kingdom) })]
    public class MercenaryMissionDef_GetCost
    {
        static void Postfix(Mercenary merc, Logic.Kingdom hire_kingdom, ref Resource __result)
        {
            if (__result == null || merc?.army == null || hire_kingdom == null) return;

            Logic.Realm realmIn = merc.army.realm_in;
            if (realmIn == null) return;

            Logic.Kingdom realmOwner = (Logic.Kingdom)realmIn.GetKingdom();
            if (realmOwner == null || realmOwner.id != hire_kingdom.id)
                __result.Mul(GameBalance.OutOfTerritoryMercenaryPriceMultiplier);
        }
    }
}
