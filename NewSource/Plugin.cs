using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Logic;
using UnityEngine;
using Random = System.Random;

namespace AIOverhaul
{
    [BepInPlugin("com.mod.aioverhaul", "AI Overhaul", "1.1.0")]
    public class AIOverhaulPlugin : BaseUnityPlugin
    {
        public static AIOverhaulPlugin Instance { get; set; }
        public static HashSet<int> EnhancedKingdomIds { get; } = new HashSet<int>();
        public static HashSet<int> BaselineKingdomIds { get; } = new HashSet<int>();

        // Mortal Enemy System: Tracks the FIRST kingdom that declared war on each Enhanced AI kingdom
        // Now persisted using Kingdom.SetVar/GetVar for automatic save/load support
        // Key = defender kingdom ID, Value = attacker kingdom ID (mortal enemy)
        // NOTE: Dictionary kept for backwards compatibility and fast lookups, but data is actually stored in Kingdom vars
        public static Dictionary<int, int> MortalEnemies { get; } = new Dictionary<int, int>();

        // Expansion Target Tracking: Caches current expansion target to detect changes
        // Key = kingdom ID, Value = expansion target kingdom ID
        // Used only for logging when target changes (not persisted)
        public static Dictionary<int, int> ExpansionTargets { get; } = new Dictionary<int, int>();

        public static Game CurrentGame => s_CurrentGame;
        static Game s_CurrentGame;

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
            if (condition.StartsWith(k_LogPrefix) || condition.StartsWith("[BepInEx]")) return;

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

        const string k_LogPrefix = "[AI-Mod]"; // Primary plugin tag, other specific log blocks use local constants

        public static bool SpectatorMode { get; set; }

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
            Log(message, category, kingdom);
        }

        /// <summary>
        /// Log a diagnostic message (only shown for player kingdom)
        /// </summary>
        public static void LogDebug(string message, LogCategory category, Logic.Kingdom kingdom)
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
            string levelTag = level == LogLevel.Diagnostic ? "[DIAG]" : "";
            string formattedMessage = $"{k_LogPrefix}{levelTag}[{kingdom?.Name}][{category}]{message}";

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

        public static void InitializeEnhancedKingdoms(Game game)
        {
            if (game == null || game.kingdoms == null) return;
            if (game == s_CurrentGame) return;
            s_CurrentGame = game;

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
                Log($"Player kingdoms added to Enhanced AI: {string.Join(", ", playerKingdoms.Select(k => k.Name))}");
            }

            // Now select enhanced/baseline from AI kingdoms only
            List<Logic.Kingdom> aiKingdoms = game.kingdoms.Where(k => k != null && !k.is_player && !k.IsDefeated()).ToList();

            // Increased to 30% for better statistical validity
            int targetCount = Mathf.Max(1, Mathf.RoundToInt(aiKingdoms.Count * GameBalance.EnhancedAISelectionPercentage));

            // Randomize selection
            Random rand = new Random();
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

            Log($"New game session detected. Selected {EnhancedKingdomIds.Count} enhanced and {BaselineKingdomIds.Count} baseline kingdoms out of {aiKingdoms.Count} total AI kingdoms.");

            if (enhanced.Count > 0)
                Log($"-----> Enhanced ({enhanced.Count}): {string.Join(", ", enhanced.Select(k => k.Name))}");
            else
                Log("Enhanced (0): None");

            if (baseline.Count > 0)
                Log($"Baseline ({baseline.Count}): {string.Join(", ", baseline.Select(k => k.Name))}");
            else
                Log("Baseline (0): None");
        }

        /// <summary>
        /// Get the mortal enemy of a kingdom (if any exists)
        /// Returns null if no mortal enemy has been set
        /// Reads from persisted Kingdom variable for automatic save/load support
        /// </summary>
        /// <summary>
        /// Get the mortal enemy of a kingdom (if any exists)
        /// Returns null if no mortal enemy has been set OR if the grudge is settled (enemy dead/defeated)
        /// Reads from persisted Kingdom variable for automatic save/load support
        /// </summary>
        public static Logic.Kingdom GetMortalEnemy(Logic.Kingdom k, Game game)
        {
            if (k == null || game == null) return null;

            // Try to read from Kingdom vars (persisted data)
            Value var = k.GetVar(CampaignVarNames.MortalEnemyId);
            if (var.type != Value.Type.Int)
            {
                // Not set or wrong type, return null
                return null;
            }

            int enemyId = var;
            Logic.Kingdom enemy = game.GetKingdom(enemyId);

            // Validations to clear the grudge
            bool clearGrudge = false;
            string clearReason = "";

            if (enemy == null)
            {
                clearGrudge = true;
                clearReason = "Kingdom not found";
            }
            else if (enemy.IsDefeated())
            {
                clearGrudge = true;
                clearReason = "Kingdom defeated";
            }
            else
            {
                // Check if the specific Sovereign we hated is still in charge
                Value sovVar = k.GetVar(CampaignVarNames.MortalEnemySovereignId);
                if (sovVar.type == Value.Type.Int)
                {
                    int hatedSovId = sovVar;
                    // If current sovereign is different, the "Mortal Enemy" is dead/gone
                    if (enemy.royalFamily?.Sovereign?.GetNid() != hatedSovId)
                    {
                        clearGrudge = true;
                        clearReason = "Sovereign dead/replaced";
                    }
                }
            }

            if (clearGrudge)
            {
                LogDebug($"Clearing Mortal Enemy for {k.Name}: {clearReason}", LogCategory.Diplomacy, k);
                k.SetVar(CampaignVarNames.MortalEnemyId, new Value()); // Clear the var
                k.SetVar(CampaignVarNames.MortalEnemySovereignId, new Value()); // Clear the sov var
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
                        LogInfo("Spectator Mode ENABLED - Enhanced AI is now controlling kingdom", LogCategory.Spectator, playerKingdom);
                    }
                    else
                    {
                        // Remove player from Enhanced AI when spectator mode is off
                        EnhancedKingdomIds.Remove(playerKingdom.id);
                        LogInfo("Spectator Mode DISABLED - Player control restored", LogCategory.Spectator, playerKingdom);
                    }
                }
            }
        }

        public static event Action<bool> OnSpectatorModeChanged;
    }

    // --- Spectator Mode Patches ---
    // Hook into Logic.Game.Update() to detect F8/F9 key presses
    // "Update" is the main game loop update function, called every frame.
    // Intent: GameUpdatePatch (Spectator Mode + Ultra Speed)
    [HarmonyPatch(typeof(Game), "Update")]
    public class UpdatePatch
    {
        static bool s_UltraSpeedActive;
        static float s_PreviousSpeed = 1f;

        static void Postfix(Game __instance)
        {
            // Detect F9 key press to toggle spectator mode
            if (Input.GetKeyDown(KeyCode.F9))
            {
                AIOverhaulPlugin.ToggleSpectatorMode();
            }

            // Detect F8 key press to toggle 50x speed
                if (Input.GetKeyDown(KeyCode.F8))
            {
                if (s_UltraSpeedActive)
                {
                    // Restore previous speed
                    __instance.speed = s_PreviousSpeed;
                    s_UltraSpeedActive = false;
                    AIOverhaulPlugin.LogInfo($"Ultra Speed DISABLED - Restored to {s_PreviousSpeed}x");
                }
                else
                {
                    // Save current speed and set 50x
                    s_PreviousSpeed = __instance.speed;
                    __instance.SetSpeed(GameBalance.UltraSpeed);
                    s_UltraSpeedActive = true;
                    AIOverhaulPlugin.LogInfo($"Ultra Speed ENABLED - {GameBalance.UltraSpeed}x speed");
                }
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                if (s_UltraSpeedActive)
                {
                    // Restore previous speed (re-using s_UltraSpeedActive for simplicity as "any boost active")
                    __instance.speed = s_PreviousSpeed;
                    s_UltraSpeedActive = false;
                    AIOverhaulPlugin.LogInfo($"High Speed DISABLED - Restored to {s_PreviousSpeed}x");
                }
                else
                {
                    // Save current speed and set 20x
                    s_PreviousSpeed = __instance.speed;
                    __instance.SetSpeed(GameBalance.HighSpeed);
                    s_UltraSpeedActive = true;
                    AIOverhaulPlugin.LogInfo($"High Speed ENABLED - {GameBalance.HighSpeed}x speed");
                }
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
    [HarmonyPatch(typeof(War), MethodType.Constructor, typeof(Logic.Kingdom), typeof(Logic.Kingdom), typeof(War.InvolvementReason), typeof(bool), typeof(War.Def))]
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
            Value existingVar = k2.GetVar(CampaignVarNames.MortalEnemyId);
            if (existingVar.type == Value.Type.Int)
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
            k2.SetVar(CampaignVarNames.MortalEnemyId, new Value(k1.id));
            
            // Record the ID of the Sovereign who attacked us
            if (k1.royalFamily?.Sovereign != null)
            {
                k2.SetVar(CampaignVarNames.MortalEnemySovereignId, new Value(k1.royalFamily.Sovereign.GetNid()));
            }

            // Update cache for fast lookups this session
            AIOverhaulPlugin.MortalEnemies[k2.id] = k1.id;

            AIOverhaulPlugin.LogDebug($"MORTAL ENEMY: will never forgive {k1.Name} ({k1.royalFamily?.Sovereign?.Name ?? "Ruler"}) for attacking first!", LogCategory.War, k2);
        }
    }



}
