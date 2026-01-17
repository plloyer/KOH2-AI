using UnityEngine;
using System;
using AIOverhaul.Constants;
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
        
        static Logic.Game _capturedGame = null;

        bool _hasStarted = false;
        string _targetKingdom = KingdomNames.Champagne;
        int _provinces = 2;
        int _difficulty = 2;
        bool _spectatorEnabled = false;

        bool _sceneMonitoringStarted = false;

        /// <summary>
        /// Called by GameCreateMultiplayerPatch to provide the Game instance
        /// </summary>
        public static void SetGameInstance(Logic.Game game)
        {
            _capturedGame = game;
            AIOverhaulPlugin.LogInfo("[AutoStarter] Game instance received and stored");
        }

        void OnEnable()
        {
            AIOverhaulPlugin.LogInfo("=== AutoStarter Component ENABLED ===");
            ParseArgs();
        }

        void ParseArgs()
        {
            var args = System.Environment.GetCommandLineArgs();

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
                if (args[i] == "-kingdom" && i + 1 < args.Length)
                {
                    _targetKingdom = args[i + 1];
                    AIOverhaulPlugin.LogInfo($"AutoStart: Detected -kingdom '{_targetKingdom}'");
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
                AIOverhaulPlugin.LogInfo($"=== AutoStart: ENABLED - kingdom '{_targetKingdom}', provinces {_provinces}, difficulty {_difficulty} ===");
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

        bool _hasRoutineStarted = false;
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

        System.Collections.IEnumerator AutoStartRoutine()
        {
            AIOverhaulPlugin.LogInfo("=== AutoStart: Routine Started ===");

            // Wait for Game instance to be captured by our patch
            AIOverhaulPlugin.LogInfo("AutoStart: Step 1 - Waiting for Game instance to be captured...");
            Logic.Game game = null;

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
            Logic.Campaign campaign = null;
            try
            {
                campaign = Logic.Campaign.CreateSinglePlayerCampaign(MapNames.Europe, PeriodNames.Early);
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
                // Logic.Game.GetKingdomSizeWhenShatteredMap() reads this from campaignData, NOT game.vars
                string shatteredVal = $"{_provinces}_shattered";
                AIOverhaulPlugin.LogInfo($"{LogPrefix}: Setting options on campaignData...");
                var data = game.campaign.campaignData;
                
                data.Set(CampaignVarNames.KingdomSize, new Logic.Value(shatteredVal));
                data.Set(CampaignVarNames.PickKingdom, new Logic.Value("pick")); // "pick" allows specific selection
                data.Set(CampaignVarNames.MapSize, new Logic.Value("normal"));
                data.Set(CampaignVarNames.StartPeriod, new Logic.Value(PeriodNames.Early));
                data.Set(CampaignVarNames.AllowOffline, new Logic.Value(true));
                data.Set(CampaignVarNames.MainGoal, new Logic.Value("domination")); // Default goal

                // Set Player Kingdom (Pre-selection)
                // We set the internal lists so that when StartGame runs, it picks up the correct player kingdom.
                
                // 1. Set ID
                int localIndex = 0; 
                AIOverhaulPlugin.LogInfo($"{LogPrefix}: Pre-selecting kingdom '{_targetKingdom}' for Player " + localIndex);
                game.campaign.SetPlayerID(localIndex, Logic.Campaign.single_player_id, false);
                if (game.campaign.playerIDs != null && game.campaign.playerIDs.Length > localIndex)
                    game.campaign.playerIDs[localIndex] = Logic.Campaign.single_player_id;

                // 2. Set persistent data name using API
                // Using SetPlayerKingdomName as requested, which handles persistent data and other logic.
                game.campaign.SetLocalPlayerKingdomName(_targetKingdom, "");

                // 3. Set internal list override
                if (game.campaign.player_kingdoms == null)
                    game.campaign.player_kingdoms = new System.Collections.Generic.List<string>();
                
                if (game.campaign.player_kingdoms.Count <= localIndex)
                {
                    while (game.campaign.player_kingdoms.Count <= localIndex)
                        game.campaign.player_kingdoms.Add("");
                }
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
        }

        int _lastLoggedDay = -1;
        float _gameStartTime = -1f;

        void MonitorGameProgress()
        {
            var game = AIOverhaulPlugin.CurrentGame;
            if (game == null) return;

            // Try to find the day counter through multiple methods
            int currentDay = -1;
            var traverse = Traverse.Create(game);

            // Method 1: Try 'day' field
            try
            {
                currentDay = traverse.Field("day").GetValue<int>();
                if (currentDay > 0)
                {
                    // Log progress every 10 days
                    if (_lastLoggedDay == -1)
                    {
                        AIOverhaulPlugin.LogInfo($"{LogPrefix}: Day counter found. Starting day: {currentDay}");
                        _lastLoggedDay = currentDay;
                    }
                    else if (currentDay >= _lastLoggedDay + 10)
                    {
                        AIOverhaulPlugin.LogInfo($"{LogPrefix}: Progress - Day {currentDay}");
                        _lastLoggedDay = currentDay;
                    }

                    // Check if 100 days reached
                    if (currentDay >= 100)
                    {
                        AIOverhaulPlugin.LogInfo($"{LogPrefix}: Target reached - Day {currentDay}/100. Quitting game...");
                        Application.Quit();
                        return;
                    }
                }
            }
            catch
            {
                // Field doesn't exist or error accessing it
            }

            // Method 2: Fallback - Track real time if day counter not found
            if (currentDay <= 0)
            {
                if (_gameStartTime < 0)
                {
                    _gameStartTime = Time.realtimeSinceStartup;
                    AIOverhaulPlugin.LogInfo($"{LogPrefix}: Day counter not found. Using time-based tracking instead.");
                    AIOverhaulPlugin.LogInfo($"{LogPrefix}: Game will run for approximately 10 minutes (600s) as a safety limit.");
                }

                float elapsedTime = Time.realtimeSinceStartup - _gameStartTime;

                // Log progress every 60 seconds
                int elapsedMinutes = Mathf.FloorToInt(elapsedTime / 60f);
                if (elapsedMinutes > _lastLoggedDay)
                {
                    AIOverhaulPlugin.LogInfo($"{LogPrefix}: Progress - {elapsedMinutes} minutes elapsed ({elapsedTime:F0}s)");
                    _lastLoggedDay = elapsedMinutes;
                }

                // Quit after 10 minutes (safety limit if day counter doesn't work)
                if (elapsedTime >= 600f)
                {
                    AIOverhaulPlugin.LogInfo($"{LogPrefix}: Time limit reached - {elapsedTime:F0}s. Quitting game...");
                    Application.Quit();
                    return;
                }
            }
        }
    }
}