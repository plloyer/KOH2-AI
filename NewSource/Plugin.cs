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

        // Mortal Enemy System: Tracks the FIRST kingdom that declared war on each Enhanced AI kingdom
        // Key = defender kingdom ID, Value = attacker kingdom ID (mortal enemy)
        public static Dictionary<int, int> MortalEnemies = new Dictionary<int, int>();

        private static Logic.Game current_game;

        private void Awake()
        {
            Instance = this;
            var harmony = new Harmony("com.mod.aioverhaul");
            harmony.PatchAll();
            
            // Listen to all Unity logs to capture game errors/warnings into BepInEx log
            Application.logMessageReceived += OnUnityLogMessage;
            
            Log("AI Overhaul Plugin Loaded with dynamic selection logic.");
        }
        
        private void OnUnityLogMessage(string condition, string stackTrace, LogType type)
        {
            // Avoid infinite loops - ignore our own logs
            if (condition.StartsWith(LogPrefix) || condition.StartsWith("[BepInEx]")) return;

            // Suppress benign base game errors regarding remote vars
            if (condition.Contains("invalid remote vars") || condition.Contains("Received data_changed") || condition.Contains("Could not resolve target")) return;

            // Map Unity LogType to BepInEx LogLevel
            BepInEx.Logging.LogLevel level = BepInEx.Logging.LogLevel.Info;
            string prefix = "[Unity]";
            
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    level = BepInEx.Logging.LogLevel.Error;
                    prefix = "[Unity Error]";
                    break;
                case LogType.Warning:
                    level = BepInEx.Logging.LogLevel.Warning;
                    prefix = "[Unity Warning]";
                    break;
            }

            // Write to BepInEx log (disk)
            // Note: We use LogInfo/LogWarning/LogError manually to ensure it hits the file
            string msg = $"{prefix} {condition}";
            if (level == BepInEx.Logging.LogLevel.Error)
            {
                Logger.LogError(msg + (string.IsNullOrEmpty(stackTrace) ? "" : $"\n{stackTrace}"));
            }
            else if (level == BepInEx.Logging.LogLevel.Warning)
            {
                Logger.LogWarning(msg);
            }
            else
            {
                Logger.LogInfo(msg);
            }
        }

        public const string LogPrefix = "[AI-Mod]";

        public static bool SpectatorMode = false;

        public void Log(string message)
        {
            Logger.LogInfo(message);
        }

        /// <summary>
        /// Static logging helper that automatically adds the [AI-Mod] prefix
        /// </summary>
        public static void LogMod(string message)
        {
            Instance?.Log($"{LogPrefix} {message}");
        }

        // Update() method removed - F9 detection now handled in GameUpdatePatch

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
            MortalEnemies.Clear(); // Reset mortal enemies for new game
            EnhancedPerformanceLogger.ClearData(); // Moved here from failed GameClearPatch

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

            LogMod($"New game session detected. Selected {EnhancedKingdomIds.Count} enhanced and {BaselineKingdomIds.Count} baseline kingdoms out of {aiKingdoms.Count} total AI kingdoms.");

            if (enhanced.Count > 0)
                LogMod($"Enhanced ({enhanced.Count}): {string.Join(", ", enhanced.Select(k => k.Name))}");
            else
                LogMod("Enhanced (0): None");

            if (baseline.Count > 0)
                LogMod($"Baseline ({baseline.Count}): {string.Join(", ", baseline.Select(k => k.Name))}");
            else
                LogMod("Baseline (0): None");
        }

        /// <summary>
        /// Get the mortal enemy of a kingdom (if any exists)
        /// Returns null if no mortal enemy has been set
        /// </summary>
        public static Logic.Kingdom GetMortalEnemy(Logic.Kingdom k, Logic.Game game)
        {
            if (k == null || game == null) return null;
            if (!MortalEnemies.ContainsKey(k.id)) return null;

            int enemyId = MortalEnemies[k.id];
            Logic.Kingdom enemy = game.GetKingdom(enemyId);

            // Clear mortal enemy if they're defeated
            if (enemy == null || enemy.IsDefeated())
            {
                MortalEnemies.Remove(k.id);
                return null;
            }

            return enemy;
        }
    }

    // Removed GameClearPatch and GameLoadPatch as target methods do not exist
    // Initialization is now triggered by EnhancedLoggingPatch in EnhancedPerformanceLogger.cs

    // Removed KingdomDestroyPatch as target method 'Destroy' does not exist.
    // Defeat logging is handled by EnhancedPerformanceLogger.LogState.
    // EnhancedKingdomIds cleanup is handled on new game initialization.

    // Mortal Enemy System: Detect when someone declares war on an Enhanced AI kingdom
    [HarmonyPatch(typeof(Logic.War), MethodType.Constructor, new System.Type[] {
        typeof(Logic.Kingdom),
        typeof(Logic.Kingdom),
        typeof(Logic.War.InvolvementReason),
        typeof(bool),
        typeof(Logic.War.Def)
    })]
    public class WarDeclarationDetectionPatch
    {
        static void Postfix(Logic.Kingdom k1, Logic.Kingdom k2)
        {
            // k1 = attacker (declares war)
            // k2 = defender (receives declaration)

            if (k1 == null || k2 == null) return;

            // Only track for Enhanced AI kingdoms
            if (!AIOverhaulPlugin.IsEnhancedAI(k2)) return;

            // Only if defender doesn't already have a mortal enemy
            if (AIOverhaulPlugin.MortalEnemies.ContainsKey(k2.id)) return;

            // Only if attacker is a DIRECT neighbor
            bool isNeighbor = false;
            if (k2.neighbors != null)
            {
                foreach (var neighbor in k2.neighbors)
                {
                    if (neighbor is Logic.Kingdom nk && nk == k1)
                    {
                        isNeighbor = true;
                        break;
                    }
                }
            }

            if (!isNeighbor) return;

            // Record as mortal enemy - the FIRST kingdom to declare war becomes the permanent grudge
            AIOverhaulPlugin.MortalEnemies[k2.id] = k1.id;
            AIOverhaulPlugin.LogMod($"MORTAL ENEMY: {k2.Name} will never forgive {k1.Name} for attacking first!");
        }
    }

    // --- Spectator Mode Patches ---

    // Hook into Logic.Game.Update() to detect F9 key press
    [HarmonyPatch(typeof(Logic.Game), "Update")]
    public class GameUpdatePatch
    {
        static void Postfix(Logic.Game __instance)
        {
            // Detect F9 key press to toggle spectator mode
            if (Input.GetKeyDown(KeyCode.F9))
            {
                AIOverhaulPlugin.SpectatorMode = !AIOverhaulPlugin.SpectatorMode;

                // Find player kingdom and add/remove from Enhanced AI
                if (__instance?.kingdoms != null)
                {
                    var playerKingdom = __instance.kingdoms.FirstOrDefault(k => k != null && k.is_player);
                    if (playerKingdom != null)
                    {
                        if (AIOverhaulPlugin.SpectatorMode)
                        {
                            // Enable Enhanced AI for player when spectator mode is on
                            if (!AIOverhaulPlugin.EnhancedKingdomIds.Contains(playerKingdom.id))
                            {
                                AIOverhaulPlugin.EnhancedKingdomIds.Add(playerKingdom.id);
                            }
                            AIOverhaulPlugin.LogMod($"Spectator Mode ENABLED - Enhanced AI is now controlling {playerKingdom.Name}");
                        }
                        else
                        {
                            // Remove player from Enhanced AI when spectator mode is off
                            AIOverhaulPlugin.EnhancedKingdomIds.Remove(playerKingdom.id);
                            AIOverhaulPlugin.LogMod($"Spectator Mode DISABLED - Player control restored for {playerKingdom.Name}");
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "Enabled")]
    public class ForceAIEnabledPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, ref bool __result, Logic.KingdomAI.EnableFlags flag)
        {
            // Only interfere if Spectator Mode is ON and this is the PLAYER kingdom
            if (AIOverhaulPlugin.SpectatorMode && __instance?.kingdom != null && __instance.kingdom.is_player)
            {
                // Respect global AI switch (e.g. if game is paused/disabled)
                if (__instance.game != null && !__instance.game.ai.enabled)
                {
                    __result = false;
                    return false;
                }

                // BYPASS the internal 'enabled' bitmask check
                // Force return true to enable AI for player kingdom
                __result = true;
                return false; // Skip original method
            }

            return true; // Run original method
        }
    }
}
