using System;
using System.Collections;
using System.Collections.Generic;
using Logic;
using UnityEngine;
using Time = UnityEngine.Time;
using HarmonyLib;

namespace AIOverhaul
{
    /// <summary>
    /// Automated game startup for CI/CD testing.
    /// Parses command line arguments and automatically starts a game with specified parameters.
    /// </summary>
    public class AutoStarter : MonoBehaviour
    {
        const string LogPrefix = "AutoStart";
        
        static Game _capturedGame;
        static AutoStarter _instance;

        public static bool IsAutoStartEnabled => _instance != null && _instance._hasStarted;

        bool _hasStarted;
        string _targetKingdom = KingdomNames.Champagne;
        int _provinces = 2;
        int _difficulty = 2;
        bool _spectatorEnabled = false;

        bool _sceneMonitoringStarted;

        /// <summary>
        /// Called by GameCreateMultiplayerPatch to provide the Game instance
        /// </summary>
        public static void SetGameInstance(Game game)
        {
            _capturedGame = game;
            AIOverhaulPlugin.LogInfo("[AutoStarter] Game instance received and stored");
        }

        void OnEnable()
        {
            _instance = this;
            AIOverhaulPlugin.LogInfo("=== AutoStarter Component ENABLED ===");
            ParseArgs();
        }

        void ParseArgs()
        {
            var args = Environment.GetCommandLineArgs();

            // Log ALL command line arguments for debugging
            AIOverhaulPlugin.LogInfo($"AutoStart: Parsing {args.Length} command line arguments:");
            for (int i = 0; i < args.Length; i++)
            {
                AIOverhaulPlugin.LogInfo($"  arg[{i}] = '{args[i]}'");
            }

            // Parse arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-autoStart")
                {
                    _hasStarted = true;
                    AIOverhaulPlugin.LogInfo("AutoStart: Detected -autoStart flag");
                }
                if (args[i] == "-provinces" && i + 1 < args.Length)
                {
                    int.TryParse(args[i + 1], out _provinces);
                    AIOverhaulPlugin.LogInfo($"AutoStart: Detected -provinces {_provinces}");
                }
                if (args[i] == "-difficulty" && i + 1 < args.Length)
                {
                    int.TryParse(args[i + 1], out _difficulty);
                    AIOverhaulPlugin.LogInfo($"AutoStart: Detected -difficulty {_difficulty}");
                }
            }

            if (_hasStarted)
            {
                AIOverhaulPlugin.LogInfo($"=== AutoStart: ENABLED - provinces {_provinces}, difficulty {_difficulty} ===");
                AIOverhaulPlugin.LogInfo("AutoStart: Will monitor for scene load before starting...");
                // DON'T start coroutine immediately - wait for Update() to detect scene is ready
                _sceneMonitoringStarted = true;
            }
            else
            {
                AIOverhaulPlugin.LogInfo("AutoStart: NOT enabled (-autoStart flag not found)");
            }
        }

        void Update()
        {
            // Monitor for scene loading
            if (_sceneMonitoringStarted && !_hasRoutineStarted)
            {
                CheckSceneAndStart();
            }

            // Monitor game progress (day counter, etc.)
            if (_hasStarted && _spectatorEnabled)
            {
                MonitorGameProgress();
            }
        }

        bool _hasRoutineStarted;
        float _sceneCheckStartTime = -1f;

        void CheckSceneAndStart()
        {
            if (_sceneCheckStartTime < 0)
            {
                _sceneCheckStartTime = Time.realtimeSinceStartup;
                AIOverhaulPlugin.LogInfo("AutoStart: Started monitoring for scene load...");
            }

            float elapsed = Time.realtimeSinceStartup - _sceneCheckStartTime;

            // Wait at least 20 seconds for the main menu scene to load
            // This gives Unity time to load all assets and initialize the UI
            if (elapsed >= 20f)
            {
                AIOverhaulPlugin.LogInfo($"AutoStart: {elapsed:F1}s elapsed - Main menu should be ready. Starting routine...");
                _hasRoutineStarted = true;
                _sceneMonitoringStarted = false;
                StartCoroutine(AutoStartRoutine());
            }
            else if (Mathf.FloorToInt(elapsed) % 5 == 0 && Mathf.FloorToInt(elapsed) != Mathf.FloorToInt(elapsed - Time.deltaTime))
            {
                // Log every 5 seconds
                AIOverhaulPlugin.LogInfo($"AutoStart: Waiting for scene... ({elapsed:F0}s / 20s)");
            }
        }

        IEnumerator AutoStartRoutine()
        {
            AIOverhaulPlugin.LogInfo("=== AutoStart: Routine Started ===");

            // Wait for Game instance to be captured by our patch
            AIOverhaulPlugin.LogInfo("AutoStart: Step 1 - Waiting for Game instance to be captured...");
            Game game = null;

            // Wait for the game to be captured (CreateMultiplayer is called during engine init)
            int i = 0;
            const int maxWaitSecond = 60;
            while (game == null)
            {
                game = _capturedGame;
                yield return new WaitForSeconds(1f);

                if (i++ == maxWaitSecond)
                {
                    AIOverhaulPlugin.LogError("AutoStart: Failed to get the game instance.");
                    yield break;
                }
            }
            
            AIOverhaulPlugin.LogInfo("AutoStart: Game instance found. Waiting 10 sec (for luck!) to start game.");
            yield return new WaitForSeconds(10f);

            // Create Campaign and assign to game
            AIOverhaulPlugin.LogInfo($"{LogPrefix}: Step 2 - Creating Campaign...");
            Campaign campaign = null;
            try
            {
                campaign = Campaign.CreateSinglePlayerCampaign(MapNames.Europe, PeriodNames.Early);
                if (campaign == null)
                {
                    AIOverhaulPlugin.LogError($"{LogPrefix}: FAILED - Campaign.CreateSinglePlayerCampaign returned null");
                    yield break;
                }
                AIOverhaulPlugin.LogInfo($"{LogPrefix}: SUCCESS - Campaign created (ID: {campaign.id})");

                // Assign campaign to game
                game.campaign = campaign;
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"{LogPrefix}: FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
                yield break;
            }

            // Configure Game Variables (Before Start)
            // This triggers internal game logic (like Shattered Map creation) automatically during start
            AIOverhaulPlugin.LogInfo($"{LogPrefix}: Step 3 - Configuring Game Variables...");
            try
            {
                // Set Shattered Map configuration
                string shatteredVal = $"{_provinces}_shattered";
                AIOverhaulPlugin.LogInfo($"{LogPrefix}: Setting options on campaignData...");
                var data = game.campaign.campaignData;
                
                data.Set(CampaignVarNames.KingdomSize, new Value(shatteredVal));
                // Default kingdom will be picked by the game (usually Aragon/first in list)
                data.Set(CampaignVarNames.MapSize, new Value("normal"));
                data.Set(CampaignVarNames.StartPeriod, new Value(PeriodNames.Early));
                data.Set(CampaignVarNames.AllowOffline, new Value(true));
                data.Set(CampaignVarNames.MainGoal, new Value("domination")); // Default goal
                
                int localIndex = 0;
                if (game.campaign.player_kingdoms == null) game.campaign.player_kingdoms = new List<string>();
                while (game.campaign.player_kingdoms.Count <= localIndex) game.campaign.player_kingdoms.Add("");
                
                game.campaign.player_kingdoms[localIndex] = _targetKingdom;
                
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"{LogPrefix}: Step 3 - FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
            }

            // STEP 4: Start the game
            AIOverhaulPlugin.LogInfo($"{LogPrefix}: Step 4 - Calling game.StartGame()...");
            try
            {
                game.StartGame(true, MapNames.Europe);
                AIOverhaulPlugin.LogInfo($"{LogPrefix}: Step 4 - SUCCESS - StartGame called (State: {game.state})");
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"{LogPrefix}: Step 4 - FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
                yield break;
            }

            // Wait for map to load
            AIOverhaulPlugin.LogInfo($"{LogPrefix}: Step 5 - Waiting 15s for map to load...");
            yield return new WaitForSeconds(10f);
            AIOverhaulPlugin.LogInfo($"{LogPrefix}: Step 5 - Map load complete (State: {game.state})");

            // Enable Spectator & Speed
            AIOverhaulPlugin.LogInfo($"{LogPrefix}: Step 6 - Enabling Spectator Mode and Speed...");
            AIOverhaulPlugin.ToggleSpectatorMode();
            game.SetSpeed(100f);

            AIOverhaulPlugin.LogInfo($"{LogPrefix}: Step 7 - AutoStart Complete. Ready to play.");
        }

        float _lastLoggedHour = -1f;
        float _gameStartTime = -1f;
        float _startingGameHours = -1f;

        // Target game duration: 5 game hours
        const float TargetGameHours = 5f;
        // Real-time safety limit: 5 minutes
        const float RealTimeLimit = 300f;

        void MonitorGameProgress()
        {
            var game = AIOverhaulPlugin.CurrentGame;
            if (game == null) return;

            // Initialize tracking on first call
            if (_gameStartTime < 0)
            {
                _gameStartTime = Time.realtimeSinceStartup;
            }


            // Calculate hours elapsed using session_time
            float gameHours = game.session_time.hours;
            
            // Capture starting hours on first valid reading
            if (_startingGameHours < 0 && gameHours > 0)
            {
                _startingGameHours = gameHours;
                int hStart = Mathf.FloorToInt(gameHours);
                int mStart = Mathf.FloorToInt((gameHours - hStart) * 60);
                AIOverhaulPlugin.LogInfo($"{LogPrefix}: Game time tracking started. Current time: {hStart}h {mStart}m");
                return;
            }

            float hoursPlayed = gameHours - _startingGameHours;

            // Log progress every game hour
            if (hoursPlayed >= 0 && Mathf.Floor(hoursPlayed) > _lastLoggedHour)
            {
                _lastLoggedHour = Mathf.Floor(hoursPlayed);
                int hPlayed = Mathf.FloorToInt(hoursPlayed);
                int mPlayed = Mathf.FloorToInt((hoursPlayed - hPlayed) * 60);
                
                int hTotal = Mathf.FloorToInt(gameHours);
                int mTotal = Mathf.FloorToInt((gameHours - hTotal) * 60);

                AIOverhaulPlugin.LogInfo($"{LogPrefix}: Progress - Played {hPlayed}h {mPlayed}m / Target {TargetGameHours:F0}h (Total Game Time: {hTotal}h {mTotal}m)");
            }

            // Check if target hours reached
            if (hoursPlayed >= TargetGameHours)
            {
                int hPlayed = Mathf.FloorToInt(hoursPlayed);
                int mPlayed = Mathf.FloorToInt((hoursPlayed - hPlayed) * 60);
                AIOverhaulPlugin.LogInfo($"{LogPrefix}: Target reached - {hPlayed}h {mPlayed}m played. Quitting...");
                Application.Quit();
                return;
            }

            // Real-time safety limit
            float realTimeElapsed = Time.realtimeSinceStartup - _gameStartTime;
            if (realTimeElapsed >= RealTimeLimit)
            {
                // Re-calculate hoursPlayed for the log message since it might not be in scope if we didn't just calculate it
                float currentHoursPlayed = (game.session_time.hours) - _startingGameHours;
                int hPlayed = Mathf.FloorToInt(currentHoursPlayed);
                int mPlayed = Mathf.FloorToInt((currentHoursPlayed - hPlayed) * 60);
                AIOverhaulPlugin.LogInfo($"{LogPrefix}: Real-time limit reached ({realTimeElapsed:F0}s). Played: {hPlayed}h {mPlayed}m. Quitting...");
                Application.Quit();
            }
        }
    }
}