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
        private bool _hasStarted = false;
        private string _targetKingdom = "Champagne";
        private int _provinces = 2;
        private int _difficulty = 2;
        private bool _spectatorEnabled = false;

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
                StartCoroutine(AutoStartRoutine());
            }
            else
            {
                AIOverhaulPlugin.LogInfo("AutoStart: NOT enabled (-autoStart flag not found)");
            }
        }

        System.Collections.IEnumerator AutoStartRoutine()
        {
            AIOverhaulPlugin.LogInfo("=== AutoStart: Routine Started ===");

            // STEP 1: Wait for game engine to initialize
            AIOverhaulPlugin.LogInfo("AutoStart: Step 1 - Waiting 10s for game engine initialization...");
            yield return new WaitForSeconds(10f);
            AIOverhaulPlugin.LogInfo("AutoStart: Step 1 - Initial wait complete");

            // STEP 2: Find or wait for Game instance
            Logic.Game game = null;
            int maxAttempts = 30;
            int attempt = 0;

            AIOverhaulPlugin.LogInfo("AutoStart: Step 2 - Searching for Game instance...");
            while (game == null && attempt < maxAttempts)
            {
                attempt++;
                AIOverhaulPlugin.LogInfo($"AutoStart: Step 2 - Attempt {attempt}/{maxAttempts} to find Game instance");

                // Try to get game from CurrentGame
                game = AIOverhaulPlugin.CurrentGame;
                if (game != null)
                {
                    AIOverhaulPlugin.LogInfo("AutoStart: Step 2 - Found Game via CurrentGame");
                    break;
                }

                // Try to find game through Unity's object system
                // Logic.Game might be referenced by Unity components
                try
                {
                    var allMonoBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                    AIOverhaulPlugin.LogInfo($"AutoStart: Step 2 - Found {allMonoBehaviours.Length} MonoBehaviours, searching for Game reference");

                    foreach (var mb in allMonoBehaviours)
                    {
                        var gameField = Traverse.Create(mb).Field("game").GetValue<Logic.Game>();
                        if (gameField != null)
                        {
                            game = gameField;
                            AIOverhaulPlugin.LogInfo($"AutoStart: Step 2 - Found Game via MonoBehaviour: {mb.GetType().Name}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AIOverhaulPlugin.LogWarning($"AutoStart: Step 2 - Exception while searching: {ex.Message}");
                }

                if (game == null)
                {
                    AIOverhaulPlugin.LogInfo("AutoStart: Step 2 - Game not found yet, waiting 2s...");
                    yield return new WaitForSeconds(2f);
                }
            }

            if (game == null)
            {
                AIOverhaulPlugin.LogError("AutoStart: Step 2 - FAILED - Could not find Game instance after all attempts!");
                yield break;
            }

            AIOverhaulPlugin.LogInfo($"AutoStart: Step 2 - SUCCESS - Game instance found (State: {game.state})");

            // STEP 3: Create Campaign
            AIOverhaulPlugin.LogInfo("AutoStart: Step 3 - Creating single player campaign...");
            try
            {
                var campaign = Logic.Campaign.CreateSinglePlayerCampaign("europe", "1110_1");
                if (campaign == null)
                {
                    AIOverhaulPlugin.LogError("AutoStart: Step 3 - FAILED - Campaign.CreateSinglePlayerCampaign returned null");
                    yield break;
                }
                AIOverhaulPlugin.LogInfo($"AutoStart: Step 3 - SUCCESS - Campaign created (ID: {campaign.id}, State: {campaign.state})");

                // Assign campaign to game if not already set
                if (game.campaign == null)
                {
                    AIOverhaulPlugin.LogInfo("AutoStart: Step 3 - Assigning campaign to game");
                    game.campaign = campaign;
                }
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"AutoStart: Step 3 - FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
                yield break;
            }

            // STEP 4: Start Game
            AIOverhaulPlugin.LogInfo("AutoStart: Step 4 - Calling game.StartGame()...");
            try
            {
                game.StartGame(true, "europe");
                AIOverhaulPlugin.LogInfo($"AutoStart: Step 4 - SUCCESS - StartGame called (State: {game.state})");
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"AutoStart: Step 4 - FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
                yield break;
            }

            // Wait for game to load
            AIOverhaulPlugin.LogInfo("AutoStart: Step 4 - Waiting 10s for game to load map...");
            yield return new WaitForSeconds(10f);
            AIOverhaulPlugin.LogInfo($"AutoStart: Step 4 - Load wait complete (State: {game.state})");

            // STEP 5: Create Shattered Map
            AIOverhaulPlugin.LogInfo($"AutoStart: Step 5 - Creating Shattered Map with {_provinces} provinces...");
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

        private int _lastLoggedDay = -1;
        private float _gameStartTime = -1f;

        void Update()
        {
            if (!_hasStarted || !_spectatorEnabled) return;

            var game = AIOverhaulPlugin.CurrentGame;
            if (game == null)
            {
                return;
            }

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