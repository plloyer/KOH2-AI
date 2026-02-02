using System;
using System.Collections;
using System.Collections.Generic;
using Logic;
using UnityEngine;
using Time = UnityEngine.Time;

namespace AIOverhaul
{
    /// <summary>
    /// Automated game startup for CI/CD testing.
    /// Parses command line arguments and automatically starts a game with specified parameters.
    /// </summary>
    public class AutoStarter : MonoBehaviour
    {
        const string k_LogPrefix = "AutoStart";

        static Game s_CapturedGame;
        static AutoStarter s_Instance;

        public static bool IsAutoStartEnabled => s_Instance != null && s_Instance.m_HasStarted;

        bool m_HasStarted;
        string m_TargetKingdom = KingdomNames.Champagne;
        int m_Provinces = 2;
        int m_Difficulty = 2;
        bool m_SpectatorEnabled;

        bool m_SceneMonitoringStarted;

        /// <summary>
        /// Called by GameCreateMultiplayerPatch to provide the Game instance
        /// </summary>
        public static void SetGameInstance(Game game)
        {
            s_CapturedGame = game;
            AIOverhaulPlugin.LogInfo("[AutoStarter] Game instance received and stored");
        }

        void OnEnable()
        {
            s_Instance = this;
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
                    m_HasStarted = true;
                    AIOverhaulPlugin.LogInfo("AutoStart: Detected -autoStart flag");
                }
                if (args[i] == "-provinces" && i + 1 < args.Length)
                {
                    int.TryParse(args[i + 1], out m_Provinces);
                    AIOverhaulPlugin.LogInfo($"AutoStart: Detected -provinces {m_Provinces}");
                }
                if (args[i] == "-difficulty" && i + 1 < args.Length)
                {
                    int.TryParse(args[i + 1], out m_Difficulty);
                    AIOverhaulPlugin.LogInfo($"AutoStart: Detected -difficulty {m_Difficulty}");
                }
            }

            if (m_HasStarted)
            {
                AIOverhaulPlugin.LogInfo($"=== AutoStart: ENABLED - provinces {m_Provinces}, difficulty {m_Difficulty} ===");
                AIOverhaulPlugin.LogInfo("AutoStart: Will monitor for scene load before starting...");
                // DON'T start coroutine immediately - wait for Update() to detect scene is ready
                m_SceneMonitoringStarted = true;
            }
            else
            {
                AIOverhaulPlugin.LogInfo("AutoStart: NOT enabled (-autoStart flag not found)");
            }
        }

        void Update()
        {
            // Monitor for scene loading
            if (m_SceneMonitoringStarted && !m_HasRoutineStarted)
            {
                CheckSceneAndStart();
            }

            // Monitor game progress (day counter, etc.)
            if (m_HasStarted && m_SpectatorEnabled)
            {
                MonitorGameProgress();
            }
        }

        bool m_HasRoutineStarted;
        float m_SceneCheckStartTime = -1f;

        void CheckSceneAndStart()
        {
            if (m_SceneCheckStartTime < 0)
            {
                m_SceneCheckStartTime = Time.realtimeSinceStartup;
                AIOverhaulPlugin.LogInfo("AutoStart: Started monitoring for scene load...");
            }

            float elapsed = Time.realtimeSinceStartup - m_SceneCheckStartTime;

            // Wait at least 20 seconds for the main menu scene to load
            // This gives Unity time to load all assets and initialize the UI
            if (elapsed >= 20f)
            {
                AIOverhaulPlugin.LogInfo($"AutoStart: {elapsed:F1}s elapsed - Main menu should be ready. Starting routine...");
                m_HasRoutineStarted = true;
                m_SceneMonitoringStarted = false;
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
                game = s_CapturedGame;
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
            AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Step 2 - Creating Campaign...");
            Campaign campaign = null;
            try
            {
                campaign = Campaign.CreateSinglePlayerCampaign(MapNames.Europe, PeriodNames.Early);
                if (campaign == null)
                {
                    AIOverhaulPlugin.LogError($"{k_LogPrefix}: FAILED - Campaign.CreateSinglePlayerCampaign returned null");
                    yield break;
                }
                AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: SUCCESS - Campaign created (ID: {campaign.id})");

                // Assign campaign to game
                game.campaign = campaign;
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"{k_LogPrefix}: FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
                yield break;
            }

            // Configure Game Variables (Before Start)
            // This triggers internal game logic (like Shattered Map creation) automatically during start
            AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Step 3 - Configuring Game Variables...");
            try
            {
                // Set Shattered Map configuration
                string shatteredVal = $"{m_Provinces}_shattered";
                AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Setting options on campaignData...");
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
                
                game.campaign.player_kingdoms[localIndex] = m_TargetKingdom;
                
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"{k_LogPrefix}: Step 3 - FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
            }

            // STEP 4: Start the game
            AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Step 4 - Calling game.StartGame()...");
            try
            {
                game.StartGame(true, MapNames.Europe);
                AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Step 4 - SUCCESS - StartGame called (State: {game.state})");
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"{k_LogPrefix}: Step 4 - FAILED - Exception: {ex.Message}\n{ex.StackTrace}");
                yield break;
            }

            // Wait for map to load
            AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Step 5 - Waiting 15s for map to load...");
            yield return new WaitForSeconds(10f);
            AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Step 5 - Map load complete (State: {game.state})");

            // Enable Spectator & Speed
            AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Step 6 - Enabling Spectator Mode and Speed...");
            AIOverhaulPlugin.ToggleSpectatorMode();
            game.SetSpeed(100f);

            AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Step 7 - AutoStart Complete. Ready to play.");
        }

        float m_LastLoggedHour = -1f;
        float m_GameStartTime = -1f;
        float m_StartingGameHours = -1f;

        // Target game duration: 5 game hours
        const float k_TargetGameHours = 5f;
        // Real-time safety limit: 5 minutes
        const float k_RealTimeLimit = 300f;

        void MonitorGameProgress()
        {
            var game = AIOverhaulPlugin.CurrentGame;
            if (game == null) return;

            // Initialize tracking on first call
            if (m_GameStartTime < 0)
            {
                m_GameStartTime = Time.realtimeSinceStartup;
            }


            // Calculate hours elapsed using session_time
            float gameHours = game.session_time.hours;
            
            // Capture starting hours on first valid reading
            if (m_StartingGameHours < 0 && gameHours > 0)
            {
                m_StartingGameHours = gameHours;
                int hStart = Mathf.FloorToInt(gameHours);
                int mStart = Mathf.FloorToInt((gameHours - hStart) * 60);
                AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Game time tracking started. Current time: {hStart}h {mStart}m");
                return;
            }

            float hoursPlayed = gameHours - m_StartingGameHours;

            // Log progress every game hour
            if (hoursPlayed >= 0 && Mathf.Floor(hoursPlayed) > m_LastLoggedHour)
            {
                m_LastLoggedHour = Mathf.Floor(hoursPlayed);
                int hPlayed = Mathf.FloorToInt(hoursPlayed);
                int mPlayed = Mathf.FloorToInt((hoursPlayed - hPlayed) * 60);
                
                int hTotal = Mathf.FloorToInt(gameHours);
                int mTotal = Mathf.FloorToInt((gameHours - hTotal) * 60);

                AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Progress - Played {hPlayed}h {mPlayed}m / Target {k_TargetGameHours:F0}h (Total Game Time: {hTotal}h {mTotal}m)");
            }

            // Check if target hours reached
            if (hoursPlayed >= k_TargetGameHours)
            {
                int hPlayed = Mathf.FloorToInt(hoursPlayed);
                int mPlayed = Mathf.FloorToInt((hoursPlayed - hPlayed) * 60);
                AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Target reached - {hPlayed}h {mPlayed}m played. Quitting...");
                Application.Quit();
                return;
            }

            // Real-time safety limit
            float realTimeElapsed = Time.realtimeSinceStartup - m_GameStartTime;
            if (realTimeElapsed >= k_RealTimeLimit)
            {
                // Re-calculate hoursPlayed for the log message since it might not be in scope if we didn't just calculate it
                float currentHoursPlayed = (game.session_time.hours) - m_StartingGameHours;
                int hPlayed = Mathf.FloorToInt(currentHoursPlayed);
                int mPlayed = Mathf.FloorToInt((currentHoursPlayed - hPlayed) * 60);
                AIOverhaulPlugin.LogInfo($"{k_LogPrefix}: Real-time limit reached ({realTimeElapsed:F0}s). Played: {hPlayed}h {mPlayed}m. Quitting...");
                Application.Quit();
            }
        }
    }
}