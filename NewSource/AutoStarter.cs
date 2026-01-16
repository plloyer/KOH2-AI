using UnityEngine;
using System;
using System.Linq;
using HarmonyLib;

namespace AIOverhaul
{
    /// <summary>
    /// Automated game startup for CI/CD testing.
    /// Parses command line arguments and automatically starts a game with specified parameters.
    /// </summary>
    public class AutoStarter : MonoBehaviour
    {
        private static Logic.Game _capturedGame = null;

        bool _hasStarted = false;
        string _targetKingdom = "Champagne";
        int _provinces = 2;
        int _difficulty = 2;
        bool _spectatorEnabled = false;

        private bool _sceneMonitoringStarted = false;

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

        private bool _hasRoutineStarted = false;
        private float _sceneCheckStartTime = -1f;

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

            // STEP 1: Wait for main menu to load
            AIOverhaulPlugin.LogInfo("AutoStart: Step 1 - Waiting 5s for main menu to load...");
            yield return new WaitForSeconds(5f);
            AIOverhaulPlugin.LogInfo("AutoStart: Step 1 - Main menu loaded");

            // STEP 2: Wait a bit longer for game systems to be ready
            AIOverhaulPlugin.LogInfo("AutoStart: Step 2 - Waiting additional 5s for game systems...");
            yield return new WaitForSeconds(5f);

            // STEP 3: Wait for Game instance to be captured by our patch
            AIOverhaulPlugin.LogInfo("AutoStart: Step 3 - Waiting for Game instance to be captured...");
            Logic.Game game = null;

            // Wait for the game to be captured (CreateMultiplayer is called during engine init)
            for (int i = 0; i < 30; i++)  // Wait up to 30 seconds
            {
                game = _capturedGame;
                if (game != null)
                {
                    AIOverhaulPlugin.LogInfo($"AutoStart: Step 3 - Game instance captured after {i}s (State: {game.state})");
                    break;
                }

                if (i % 5 == 0)  // Log every 5 seconds
                {
                    AIOverhaulPlugin.LogInfo($"AutoStart: Step 3 - Waiting for Game... ({i}s / 30s)");
                }

                yield return new WaitForSeconds(1f);
            }

            if (game == null)
            {
                AIOverhaulPlugin.LogError("AutoStart: Step 3 - FAILED - Game instance was never captured");
                AIOverhaulPlugin.LogError("AutoStart: Step 3 - This means CreateMultiplayer was never called, or our patch didn't run");
                yield break;
            }

            // STEP 4: Create Campaign and assign to game
            AIOverhaulPlugin.LogInfo("AutoStart: Step 4 - Creating Campaign for 'europe' map...");
            Logic.Campaign campaign = null;
            try
            {
                campaign = Logic.Campaign.CreateSinglePlayerCampaign("europe", "1110_1");
                if (campaign == null)
                {
                    AIOverhaulPlugin.LogError("AutoStart: Step 4 - FAILED - Campaign.CreateSinglePlayerCampaign returned null");
                    yield break;
                }
                AIOverhaulPlugin.LogInfo($"AutoStart: Step 4 - SUCCESS - Campaign created (ID: {campaign.id})");

                // Assign campaign to game
                AIOverhaulPlugin.LogInfo("AutoStart: Step 4 - Assigning campaign to game...");
                game.campaign = campaign;
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"AutoStart: Step 4 - FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
                yield break;
            }

            // STEP 5: Start the game
            AIOverhaulPlugin.LogInfo("AutoStart: Step 5 - Calling game.StartGame()...");
            try
            {
                game.StartGame(true, "europe");
                AIOverhaulPlugin.LogInfo($"AutoStart: Step 5 - SUCCESS - StartGame called (State: {game.state})");
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"AutoStart: Step 5 - FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
                yield break;
            }

            // Wait for map to load
            AIOverhaulPlugin.LogInfo("AutoStart: Step 5 - Waiting 15s for map to load...");
            yield return new WaitForSeconds(15f);
            AIOverhaulPlugin.LogInfo($"AutoStart: Step 5 - Map load complete (State: {game.state})");

            // STEP 6: Create Shattered Map
            AIOverhaulPlugin.LogInfo($"AutoStart: Step 6 - Creating Shattered Map with {_provinces} provinces...");
            try
            {
                var method = AccessTools.Method(typeof(Logic.Game), "CreateShatteredMap", new Type[] { typeof(int) });
                if (method == null)
                {
                    AIOverhaulPlugin.LogError("AutoStart: Step 5 - FAILED - Could not find CreateShatteredMap method via reflection");
                    yield break;
                }

                AIOverhaulPlugin.LogInfo("AutoStart: Step 5 - Found CreateShatteredMap method, invoking...");
                method.Invoke(game, new object[] { _provinces });
                AIOverhaulPlugin.LogInfo("AutoStart: Step 5 - SUCCESS - Shattered Map created");
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"AutoStart: Step 5 - FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
                yield break;
            }

            // Wait for kingdoms to initialize
            AIOverhaulPlugin.LogInfo("AutoStart: Step 5 - Waiting 3s for kingdoms to initialize...");
            yield return new WaitForSeconds(3f);

            // STEP 6: Select Kingdom
            AIOverhaulPlugin.LogInfo($"AutoStart: Step 6 - Selecting kingdom '{_targetKingdom}'...");
            if (game.kingdoms == null)
            {
                AIOverhaulPlugin.LogError("AutoStart: Step 6 - FAILED - game.kingdoms is null");
                yield break;
            }

            AIOverhaulPlugin.LogInfo($"AutoStart: Step 6 - Found {game.kingdoms.Count} kingdoms, searching for '{_targetKingdom}'...");
            foreach (var kingdom in game.kingdoms)
            {
                if (kingdom != null)
                {
                    AIOverhaulPlugin.LogInfo($"AutoStart: Step 6 - Kingdom: {kingdom.Name} (ID: {kingdom.id})");
                }
            }

            var targetKingdom = game.kingdoms.FirstOrDefault(x => x != null && x.Name.Contains(_targetKingdom));
            if (targetKingdom == null)
            {
                AIOverhaulPlugin.LogError($"AutoStart: Step 6 - FAILED - Kingdom '{_targetKingdom}' not found!");
                AIOverhaulPlugin.LogInfo($"AutoStart: Step 6 - Available kingdoms: {string.Join(", ", game.kingdoms.Where(k => k != null).Select(k => k.Name))}");
                yield break;
            }

            AIOverhaulPlugin.LogInfo($"AutoStart: Step 6 - Found target kingdom: {targetKingdom.Name} (ID: {targetKingdom.id})");

            try
            {
                if (game.campaign == null)
                {
                    AIOverhaulPlugin.LogError("AutoStart: Step 6 - FAILED - game.campaign is null");
                    yield break;
                }

                AIOverhaulPlugin.LogInfo($"AutoStart: Step 6 - Calling campaign.SetPlayerID(0, '{targetKingdom.Name}', true)");
                game.campaign.SetPlayerID(0, targetKingdom.Name, true);
                AIOverhaulPlugin.LogInfo($"AutoStart: Step 6 - SUCCESS - Player kingdom set to {targetKingdom.Name}");
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"AutoStart: Step 6 - FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
                yield break;
            }

            yield return new WaitForSeconds(2f);

            // STEP 7: Enable Spectator Mode
            AIOverhaulPlugin.LogInfo("AutoStart: Step 7 - Enabling Spectator Mode...");
            try
            {
                if (!AIOverhaulPlugin.SpectatorMode)
                {
                    AIOverhaulPlugin.ToggleSpectatorMode();
                    AIOverhaulPlugin.LogInfo("AutoStart: Step 7 - SUCCESS - Spectator Mode enabled");
                }
                else
                {
                    AIOverhaulPlugin.LogInfo("AutoStart: Step 7 - Spectator Mode already enabled");
                }
                _spectatorEnabled = true;
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"AutoStart: Step 7 - FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
            }

            // STEP 8: Set Game Speed
            AIOverhaulPlugin.LogInfo("AutoStart: Step 8 - Setting game speed to 3.0x...");
            try
            {
                game.SetSpeed(3f);
                AIOverhaulPlugin.LogInfo("AutoStart: Step 8 - SUCCESS - Game speed set to 3.0x");
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"AutoStart: Step 8 - FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
            }

            AIOverhaulPlugin.LogInfo("=== AutoStart: Setup Complete - Game Running ===");
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
                        AIOverhaulPlugin.LogInfo($"AutoStart: Day counter found. Starting day: {currentDay}");
                        _lastLoggedDay = currentDay;
                    }
                    else if (currentDay >= _lastLoggedDay + 10)
                    {
                        AIOverhaulPlugin.LogInfo($"AutoStart: Progress - Day {currentDay}");
                        _lastLoggedDay = currentDay;
                    }

                    // Check if 100 days reached
                    if (currentDay >= 100)
                    {
                        AIOverhaulPlugin.LogInfo($"AutoStart: Target reached - Day {currentDay}/100. Quitting game...");
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
                    AIOverhaulPlugin.LogInfo("AutoStart: Day counter not found. Using time-based tracking instead.");
                    AIOverhaulPlugin.LogInfo($"AutoStart: Game will run for approximately 10 minutes (600s) as a safety limit.");
                }

                float elapsedTime = Time.realtimeSinceStartup - _gameStartTime;

                // Log progress every 60 seconds
                int elapsedMinutes = Mathf.FloorToInt(elapsedTime / 60f);
                if (elapsedMinutes > _lastLoggedDay)
                {
                    AIOverhaulPlugin.LogInfo($"AutoStart: Progress - {elapsedMinutes} minutes elapsed ({elapsedTime:F0}s)");
                    _lastLoggedDay = elapsedMinutes;
                }

                // Quit after 10 minutes (safety limit if day counter doesn't work)
                if (elapsedTime >= 600f)
                {
                    AIOverhaulPlugin.LogInfo($"AutoStart: Time limit reached - {elapsedTime:F0}s. Quitting game...");
                    Application.Quit();
                    return;
                }
            }
        }
    }
}