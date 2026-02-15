using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "Mercenary.Def.Load" loads mercenary config from data files (singleton def).
    // Intent: Double spawn limits so more mercenary camps appear across the map.
    [HarmonyPatch(typeof(Mercenary.Def), "Load")]
    public class MercenaryDef_Load
    {
        static void Postfix(Mercenary.Def __instance, ref bool __result)
        {
            if (!__result) return;

            __instance.max_mercs = (int)(__instance.max_mercs * GameBalance.MercenarySpawnLimitMultiplier);
            __instance.max_mercs_per_realm = GameBalance.MaxMercsPerRealm;
            __instance.max_mercs_per_kingdom = (int)(__instance.max_mercs_per_kingdom * GameBalance.MercenarySpawnLimitMultiplier);

            AIOverhaulPlugin.LogInfo($"[Mercenary] Spawn limits adjusted ({GameBalance.MercenarySpawnLimitMultiplier}x): max={__instance.max_mercs}, per_realm={__instance.max_mercs_per_realm}, per_kingdom={__instance.max_mercs_per_kingdom}");
        }
    }
}