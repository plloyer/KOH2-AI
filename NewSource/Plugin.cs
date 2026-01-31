using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AIOverhaul
{
    [BepInPlugin("com.mod.aioverhaul", "AI Overhaul", "1.1.0")]
    public class AIOverhaulPlugin : BaseUnityPlugin
    {
        public static AIOverhaulPlugin Instance;
        public static HashSet<int> EnhancedKingdomIds = new HashSet<int>();
        public static HashSet<int> BaselineKingdomIds = new HashSet<int>();

        // Mortal Enemy System: Tracks the FIRST kingdom that declared war on each Enhanced AI kingdom
        // Now persisted using Kingdom.SetVar/GetVar for automatic save/load support
        // Key = defender kingdom ID, Value = attacker kingdom ID (mortal enemy)
        // NOTE: Dictionary kept for backwards compatibility and fast lookups, but data is actually stored in Kingdom vars
        public static Dictionary<int, int> MortalEnemies = new Dictionary<int, int>();

        // Expansion Target Tracking: Caches current expansion target to detect changes
        // Key = kingdom ID, Value = expansion target kingdom ID
        // Used only for logging when target changes (not persisted)
        public static Dictionary<int, int> ExpansionTargets = new Dictionary<int, int>();

        public const string MORTAL_ENEMY_VAR = "aimod_mortal_enemy";

        public static Logic.Game CurrentGame => current_game;
        static Logic.Game current_game;

        void Awake()
        {
            Instance = this;
            var harmony = new Harmony("com.mod.aioverhaul");
            harmony.PatchAll();

            // Initialize Debug Overlay on a dedicated GameObject to allow it to persist independently
            var overlayGO = new GameObject("AI_Debug_Overlay");
            DontDestroyOnLoad(overlayGO);
            overlayGO.hideFlags = HideFlags.HideAndDontSave;
            overlayGO.AddComponent<DebugOverlay>();

            // Initialize AutoStarter on a dedicated GameObject (like DebugOverlay)
            var autoStartGO = new GameObject("AI_AutoStarter");
            DontDestroyOnLoad(autoStartGO);
            autoStartGO.hideFlags = HideFlags.HideAndDontSave;
            autoStartGO.AddComponent<AutoStarter>();

            // Listen to all Unity logs to capture game errors/warnings into BepInEx log
            Application.logMessageReceived += OnUnityLogMessage;

            Logger.LogInfo("AI Overhaul Plugin Loaded with dynamic selection logic.");
        }

        /// <summary>
        /// Capture Unity logs and forward Warnings/Errors to BepInEx log
        /// </summary>
        void OnUnityLogMessage(string condition, string stackTrace, LogType type)
        {
            // Only capture warnings and errors to avoid spam
            // Ignore benign "invalid remote vars" errors which are common in base game
            if (condition.Contains("invalid remote vars") || condition.Contains("Received data_changed")) return;

            // Ignore our own logs and BepInEx logs to prevent infinite recursion
            if (condition.StartsWith(LogPrefix) || condition.StartsWith("[BepInEx]")) return;

            string prefix = "[Unity] ";
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    Logger.LogError($"{prefix}{condition}\n{stackTrace}");
                    break;
                case LogType.Warning:
                    Logger.LogWarning($"{prefix}{condition}");
                    break;
                // Explicitly ignore LogType.Log to prevent spam
            }
        }

        public const string LogPrefix = "[AI-Mod]";

        public static bool SpectatorMode = false;

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void LogError(string message, LogCategory category = LogCategory.General, Logic.Kingdom kingdom = null)
        {
            Log(message, category, kingdom, LogLevel.Error);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void LogWarning(string message, LogCategory category = LogCategory.General, Logic.Kingdom kingdom = null)
        {
            Log(message, category, kingdom, LogLevel.Warning);
        }

        /// <summary>
        /// Log a standard informational message
        /// </summary>
        public static void LogInfo(string message, LogCategory category = LogCategory.General, Logic.Kingdom kingdom = null)
        {
            Log(message, category, kingdom, LogLevel.Log);
        }

        /// <summary>
        /// Log a diagnostic message (only shown for player kingdom)
        /// </summary>
        public static void LogDebug(string message, LogCategory category = LogCategory.General, Logic.Kingdom kingdom = null)
        {
            // Filter Diagnostic logs - only show for player kingdom
            if (kingdom != null && !kingdom.is_player)
                return; // Skip this log

            Log(message, category, kingdom, LogLevel.Diagnostic);
        }

        /// <summary>
        /// Static logging helper that automatically adds the [AI-Mod] prefix, category tag, and kingdom name
        /// </summary>
        static void Log(string message, LogCategory category = LogCategory.General, Logic.Kingdom kingdom = null, LogLevel level = LogLevel.Log)
        {
            string levelTag = level == LogLevel.Diagnostic ? "[DIAG] " : "";
            string formattedMessage = $"{LogPrefix}[{kingdom?.Name}][{category}]{levelTag}{message}";

            // Call the appropriate Logger method based on log level
            switch (level)
            {
                case LogLevel.Error:
                    Instance?.Logger.LogError(formattedMessage);
                    break;
                case LogLevel.Warning:
                    Instance?.Logger.LogWarning(formattedMessage);
                    break;
                case LogLevel.Log:
                case LogLevel.Diagnostic:
                default:
                    Instance?.Logger.LogInfo(formattedMessage);
                    break;
            }
        }
        
        public static bool IsEnhancedAI(Logic.Kingdom k)
        {
            if (k == null) return false;
            
            // Allow if strictly enhanced OR if it's the player in spectator mode
            if (k.is_player && SpectatorMode) return true;

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

            // ALWAYS add player kingdoms to enhanced AI list (for spectator mode testing)
            List<Logic.Kingdom> playerKingdoms = game.kingdoms.Where(k => k != null && k.is_player && !k.IsDefeated()).ToList();
            foreach (var playerKingdom in playerKingdoms)
            {
                EnhancedKingdomIds.Add(playerKingdom.id);
                EnhancedPerformanceLogger.RecordBaseline(playerKingdom, "Enhanced", game);
            }

            if (playerKingdoms.Count > 0)
            {
                Log($"Player kingdoms added to Enhanced AI: {string.Join(", ", playerKingdoms.Select(k => k.Name))}", LogCategory.General);
            }

            // Now select enhanced/baseline from AI kingdoms only
            List<Logic.Kingdom> aiKingdoms = game.kingdoms.Where(k => k != null && !k.is_player && !k.IsDefeated()).ToList();

            // Increased to 30% for better statistical validity
            int targetCount = Mathf.Max(1, Mathf.RoundToInt(aiKingdoms.Count * GameBalance.EnhancedAISelectionPercentage));

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

            Log($"New game session detected. Selected {EnhancedKingdomIds.Count} enhanced and {BaselineKingdomIds.Count} baseline kingdoms out of {aiKingdoms.Count} total AI kingdoms.", LogCategory.General);

            if (enhanced.Count > 0)
                Log($"-----> Enhanced ({enhanced.Count}): {string.Join(", ", enhanced.Select(k => k.Name))}", LogCategory.General);
            else
                Log("Enhanced (0): None", LogCategory.General);

            if (baseline.Count > 0)
                Log($"Baseline ({baseline.Count}): {string.Join(", ", baseline.Select(k => k.Name))}", LogCategory.General);
            else
                Log("Baseline (0): None", LogCategory.General);
        }

        /// <summary>
        /// Get the mortal enemy of a kingdom (if any exists)
        /// Returns null if no mortal enemy has been set
        /// Reads from persisted Kingdom variable for automatic save/load support
        /// </summary>
        public static Logic.Kingdom GetMortalEnemy(Logic.Kingdom k, Logic.Game game)
        {
            if (k == null || game == null) return null;

            // Try to read from Kingdom vars (persisted data)
            Logic.Value var = k.GetVar(MORTAL_ENEMY_VAR);
            if (var.type != Logic.Value.Type.Int)
            {
                // Not set or wrong type, return null
                return null;
            }

            int enemyId = (int)var;
            Logic.Kingdom enemy = game.GetKingdom(enemyId);

            // Clear mortal enemy if they're defeated
            if (enemy == null || enemy.IsDefeated())
            {
                k.SetVar(MORTAL_ENEMY_VAR, new Logic.Value()); // Clear the var
                MortalEnemies.Remove(k.id); // Clear cache too
                return null;
            }

            // Update cache for fast lookups
            if (!MortalEnemies.ContainsKey(k.id))
            {
                MortalEnemies[k.id] = enemyId;
            }

            return enemy;
        }
        public static void ToggleSpectatorMode()
        {
            SpectatorMode = !SpectatorMode;

            // Notify listeners
            OnSpectatorModeChanged?.Invoke(SpectatorMode);

            // Find player kingdom and add/remove from Enhanced AI
            if (CurrentGame?.kingdoms != null)
            {
                var playerKingdom = CurrentGame.kingdoms.FirstOrDefault(k => k != null && k.is_player);
                if (playerKingdom != null)
                {
                    if (SpectatorMode)
                    {
                        // Enable Enhanced AI for player when spectator mode is on
                        if (!EnhancedKingdomIds.Contains(playerKingdom.id))
                        {
                            EnhancedKingdomIds.Add(playerKingdom.id);
                        }
                        LogInfo($"Spectator Mode ENABLED - Enhanced AI is now controlling kingdom", LogCategory.Spectator, playerKingdom);
                    }
                    else
                    {
                        // Remove player from Enhanced AI when spectator mode is off
                        EnhancedKingdomIds.Remove(playerKingdom.id);
                        LogInfo($"Spectator Mode DISABLED - Player control restored", LogCategory.Spectator, playerKingdom);
                    }
                }
            }
        }

        public static event Action<bool> OnSpectatorModeChanged;
    }

    // --- Spectator Mode Patches ---

    // Hook into Logic.Game.Update() to detect F9 key press
    // "Update" is the main game loop update function, called every frame.
    // Intent: GameUpdatePatch (Spectator Mode)
    [HarmonyPatch(typeof(Logic.Game), "Update")]
    public class UpdatePatch
    {
        static void Postfix(Logic.Game __instance)
        {
            // Detect F9 key press to toggle spectator mode
            if (Input.GetKeyDown(KeyCode.F9))
            {
                AIOverhaulPlugin.ToggleSpectatorMode();
            }
        }
    }

    // Removed GameClearPatch and GameLoadPatch as target methods do not exist
    // Initialization is now triggered by EnhancedLoggingPatch in EnhancedPerformanceLogger.cs

    // Removed KingdomDestroyPatch as target method 'Destroy' does not exist.
    // Defeat logging is handled by EnhancedPerformanceLogger.LogState.
    // EnhancedKingdomIds cleanup is handled on new game initialization.

    // Mortal Enemy System: Detect when someone declares war on an Enhanced AI kingdom
    // "Logic.War" constructor is called when a new war is declared between two kingdoms.
    // Intent: WarDeclarationDetectionPatch
    [HarmonyPatch(typeof(Logic.War), MethodType.Constructor, new[] {
        typeof(Logic.Kingdom),
        typeof(Logic.Kingdom),
        typeof(Logic.War.InvolvementReason),
        typeof(bool),
        typeof(Logic.War.Def)
    })]
    public class WarConstructorPatch
    {
        static void Postfix(Logic.Kingdom k1, Logic.Kingdom k2)
        {
            // k1 = attacker (declares war)
            // k2 = defender (receives declaration)

            if (k1 == null || k2 == null) return;

            // Only track for Enhanced AI kingdoms
            if (!AIOverhaulPlugin.IsEnhancedAI(k2)) return;

            // Check if defender already has a mortal enemy (check persisted var)
            Logic.Value existingVar = k2.GetVar(AIOverhaulPlugin.MORTAL_ENEMY_VAR);
            if (existingVar.type == Logic.Value.Type.Int)
            {
                // Already has a mortal enemy set
                return;
            }

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
            // Store in Kingdom variable for automatic persistence
            k2.SetVar(AIOverhaulPlugin.MORTAL_ENEMY_VAR, new Logic.Value(k1.id));

            // Update cache for fast lookups this session
            AIOverhaulPlugin.MortalEnemies[k2.id] = k1.id;

            AIOverhaulPlugin.LogDebug($"MORTAL ENEMY: will never forgive {k1.Name} for attacking first!", LogCategory.War, k2);
        }
    }



}
