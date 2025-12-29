using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AIOverhaul
{
    [BepInPlugin("com.mod.aioverhaul", "AI Overhaul", "1.1.0")]
    public class AIOverhaulPlugin : BaseUnityPlugin
    {
        public static AIOverhaulPlugin Instance;
        public static HashSet<int> EnhancedKingdomIds = new HashSet<int>();
        public static HashSet<int> BaselineKingdomIds = new HashSet<int>();
        private static Logic.Game current_game;

        private void Awake()
        {
            Instance = this;
            var harmony = new Harmony("com.mod.aioverhaul");
            harmony.PatchAll();
            Log("AI Overhaul Plugin Loaded with dynamic selection logic.");
        }

        public const string LogPrefix = "[AI-Mod]";

        public static bool SpectatorMode = false;

        public void Log(string message)
        {
            Logger.LogInfo(message);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                SpectatorMode = !SpectatorMode;
                string status = SpectatorMode ? "ENABLED" : "DISABLED";
                Log($"{LogPrefix} Spectator Mode {status}");
                
                // Visual feedback (Toast or just log)
                // Logic.UI.Toast.Show($"Spectator Mode: {status}"); // If accessible
            }
        }

        public static bool IsEnhancedAI(Logic.Kingdom k)
        {
            if (k == null) return false;
            return EnhancedKingdomIds.Contains(k.id);
        }

        public static bool IsBaselineAI(Logic.Kingdom k)
        {
            if (k == null) return false;
            return BaselineKingdomIds.Contains(k.id);
        }

        public static void InitializeEnhancedKingdoms(Logic.Game game)
        {
            if (game == null || game.kingdoms == null) return;
            if (game == current_game) return;
            current_game = game;

            EnhancedKingdomIds.Clear();
            BaselineKingdomIds.Clear();

            List<Logic.Kingdom> aiKingdoms = game.kingdoms.Where(k => k != null && !k.is_player && !k.IsDefeated()).ToList();

            // Increased to 30% for better statistical validity
            int targetCount = Mathf.Max(1, Mathf.RoundToInt(aiKingdoms.Count * 0.30f));

            // Randomize selection
            System.Random rand = new System.Random();
            var shuffled = aiKingdoms.OrderBy(x => rand.Next()).ToList();

            var enhanced = shuffled.Take(targetCount).ToList();
            var baseline = shuffled.Skip(targetCount).Take(targetCount).ToList();

            foreach (var k in enhanced)
            {
                EnhancedKingdomIds.Add(k.id);
                EnhancedPerformanceLogger.RecordBaseline(k, "Enhanced", game);
            }

            foreach (var k in baseline)
            {
                BaselineKingdomIds.Add(k.id);
                EnhancedPerformanceLogger.RecordBaseline(k, "Baseline", game);
            }

            Instance.Log($"{LogPrefix} New game session detected. Selected {EnhancedKingdomIds.Count} enhanced and {BaselineKingdomIds.Count} baseline kingdoms out of {aiKingdoms.Count} total AI kingdoms.");
            
            if (enhanced.Count > 0)
                Instance.Log($"{LogPrefix} Enhanced ({enhanced.Count}): {string.Join(", ", enhanced.Select(k => k.Name))}");
            else
                Instance.Log($"{LogPrefix} Enhanced (0): None");

            if (baseline.Count > 0)
                Instance.Log($"{LogPrefix} Baseline ({baseline.Count}): {string.Join(", ", baseline.Select(k => k.Name))}");
            else
                Instance.Log($"{LogPrefix} Baseline (0): None");
        }
    }

    [HarmonyPatch(typeof(Logic.Game), "Clear")] // Good hook for session ending/cleanup
    public class GameClearPatch
    {
        static void Postfix()
        {
            AIOverhaulPlugin.EnhancedKingdomIds.Clear();
            AIOverhaulPlugin.BaselineKingdomIds.Clear();
            EnhancedPerformanceLogger.ClearData();
            // BuddySystem.ClearCache(); // TODO: Uncomment when BuddySystem is implemented
        }
    }

    [HarmonyPatch(typeof(Logic.Game), "Load")]
    public class GameLoadPatch
    {
        static void Postfix(Logic.Game __instance)
        {
            AIOverhaulPlugin.InitializeEnhancedKingdoms(__instance);
        }
    }

    [HarmonyPatch(typeof(Logic.Kingdom), "Destroy")]
    public class KingdomDestroyPatch
    {
        static void Prefix(Logic.Kingdom __instance)
        {
            if (__instance == null) return;

            // Enhanced logger with survival time tracking
            if (__instance.game != null)
            {
                float currentYear = KingdomBaseline.GetGameYear(__instance.game);
                EnhancedPerformanceLogger.LogDefeat(__instance, currentYear);
            }

            AIOverhaulPlugin.EnhancedKingdomIds.Remove(__instance.id);
            AIOverhaulPlugin.BaselineKingdomIds.Remove(__instance.id);
        }
    }

    // --- Spectator Mode Patches ---

    [HarmonyPatch(typeof(Logic.KingdomAI), "Enabled")]
    public class ForceAIEnabledPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, ref bool __result, Logic.KingdomAI.EnableFlags flag)
        {
            // Only interfere if Spectator Mode is ON and this is the PLAYER kingdom
            if (AIOverhaulPlugin.SpectatorMode && __instance.kingdom.is_player)
            {
                // Respect global AI switch (e.g. if game is paused/disabled)
                if (__instance.game != null && !__instance.game.ai.enabled)
                {
                    __result = false;
                    return false;
                }

                // BYPASS the internal 'enabled' bitmask check
                // Force return true to say "Yes, this AI feature is enabled"
                __result = true;
                return false; // Skip original method
            }
            return true; // Run original method
        }
    }
}
