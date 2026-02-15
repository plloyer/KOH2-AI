using HarmonyLib;

namespace AIOverhaul
{
    /// <summary>
    /// Block "Demand to Attack" when the AI sender isn't actually at war with the target kingdom.
    /// Vanilla allows this if the AI merely borders the target, leading to nonsensical demands.
    /// Only applies to exact DemandAttackKingdom type — subclasses (OfferAttackKingdom, SummonVassal) keep vanilla behavior.
    /// </summary>
    [HarmonyPatch(typeof(Logic.DemandAttackKingdom), nameof(Logic.DemandAttackKingdom.IsValidForAI))]
    public static class DemandAttackKingdom_IsValidForAI
    {
        public static void Postfix(Logic.DemandAttackKingdom __instance, ref bool __result)
        {
            if (!__result) return;

            // Only apply to DemandAttackKingdom, not subclasses (OfferAttackKingdom, SummonVassal have different semantics)
            if (__instance.GetType() != typeof(Logic.DemandAttackKingdom)) return;

            var aiKingdom = __instance.from as Logic.Kingdom;
            var targetKingdom = __instance.GetArg<Logic.Kingdom>(0);
            if (aiKingdom == null || targetKingdom == null) return;

            // AI should only demand us to attack kingdoms they are actually at war with
            if (!aiKingdom.IsEnemy(targetKingdom))
                __result = false;
        }
    }
}
