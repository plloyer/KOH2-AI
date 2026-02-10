using HarmonyLib;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Logic.PathFinding), "LoadSettings")]
    public class PathFinding_LoadSettings
    {
        static void Postfix(Logic.PathFinding __instance)
        {
            __instance.settings.enter_water_weight_mod = 10;
            AIOverhaulPlugin.LogInfo("[PathFinding] Reduced enter_water_weight_mod from 255 to 10");

            __instance.settings.road_stickiness = 1.5f;
            AIOverhaulPlugin.LogInfo("[PathFinding] Increased road_stickiness from 1.25 to 1.5");
        }
    }
}
