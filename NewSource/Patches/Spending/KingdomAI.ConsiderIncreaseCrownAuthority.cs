using AIOverhaul.Constants;
using AIOverhaul.Helpers;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "ConsiderIncreaseCrownAuthority" decides if the kingdom should spend resources to increase crown authority.
    // Intent: SpendingPriorityPatch
    [HarmonyPatch(typeof(KingdomAI), "ConsiderIncreaseCrownAuthority")]
    public static class KingdomAI_ConsiderIncreaseCrownAuthority
    {
        [HarmonyPrefix]
        public static bool Prefix(KingdomAI __instance, ref bool __result)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Block until kingdom has built military infrastructure
            bool hasBarracks = BuildingHelper.HasBuilding(__instance.kingdom, BuildingNames.Barracks);
            bool hasSwordsmith = BuildingHelper.HasBuildingUpgrade(__instance.kingdom, BuildingUpgradeNames.Swordsmith);
            bool hasFletcher = BuildingHelper.HasBuildingUpgrade(__instance.kingdom, BuildingUpgradeNames.Fletcher_Barracks);

            if (!hasBarracks || !hasSwordsmith || !hasFletcher)
            {
                __result = false;
                return false; // Block Crown Authority
            }

            // Block CA if rushing tradition (400+ books, Writing/Learning available)
            if (TraditionHelper.ShouldRushTradition(__instance.kingdom))
            {
                __result = false;
                return false;
            }

            // Block CA if any province can upgrade fortifications to level 1
            if (__instance.kingdom.realms != null)
            {
                foreach (var realm in __instance.kingdom.realms)
                {
                    if (realm?.castle != null &&
                        realm.castle.CanUpgradeFortification() &&
                        realm.castle.fortifications.level == 0)
                    {
                        __result = false;
                        return false;
                    }
                }
            }

            return true;
        }
    }
}