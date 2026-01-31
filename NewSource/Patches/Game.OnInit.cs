using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// Captures the Game instance when it's initialized.
    /// This gives AutoStarter access to the game instance early.
    /// </summary>
    [HarmonyPatch(typeof(Game), "OnInit")]
    public class GameOnInitPatch
    {
        static void Postfix(Game __instance)
        {
            if (__instance != null && __instance.type == "world_view")
            {
                // Store the game instance for AutoStarter to use
                AutoStarter.SetGameInstance(__instance);
                AIOverhaulPlugin.LogInfo($"[AutoStarter] Game instance captured via OnInit (Type: {__instance.type}, State: {__instance.state})");
            }
        }
    }
}