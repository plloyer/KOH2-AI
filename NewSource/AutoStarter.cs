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
        static Game s_CapturedGame;
        static AutoStarter s_Instance;

        public static bool IsAutoStartEnabled => s_Instance != null && s_Instance.m_HasStarted;

        bool m_HasStarted;
        string m_TargetKingdom = KingdomNames.Champagne;
        int m_Provinces = 2;
        int m_Difficulty = 2;
        bool m_SpectatorEnabled;

        bool m_SceneMonitoringStarted;
        string m_WebhookUrl;

        /// <summary>
        /// Called by GameCreateMultiplayerPatch to provide the Game instance
        /// </summary>
        public static void SetGameInstance(Game game)
        {
            s_CapturedGame = game;
            AIOverhaulPlugin.LogInfo("Game instance received and stored", LogCategory.AutoStart);
        }

        void OnEnable()
        {
            s_Instance = this;
            AIOverhaulPlugin.LogInfo("Component enabled", LogCategory.AutoStart);
            ParseArgs();
        }

        void ParseArgs()
        {
            var args = Environment.GetCommandLineArgs();

            // Log ALL command line arguments for debugging
            AIOverhaulPlugin.LogInfo($"Parsing {args.Length} command line arguments:", LogCategory.AutoStart);
            for (int i = 0; i < args.Length; i++)
            {
                AIOverhaulPlugin.LogInfo($"  arg[{i}] = '{args[i]}'", LogCategory.AutoStart);
            }

            // Parse arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-autostart")
                {
                    m_HasStarted = true;
                    AIOverhaulPlugin.LogInfo("Detected -autostart flag", LogCategory.AutoStart);
                }
                if (args[i] == "-provinces" && i + 1 < args.Length)
                {
                    int.TryParse(args[i + 1], out m_Provinces);
                    AIOverhaulPlugin.LogInfo($"Detected -provinces {m_Provinces}", LogCategory.AutoStart);
                }
                if (args[i] == "-difficulty" && i + 1 < args.Length)
                {
                    int.TryParse(args[i + 1], out m_Difficulty);
                    AIOverhaulPlugin.LogInfo($"Detected -difficulty {m_Difficulty}", LogCategory.AutoStart);
                }
                if (args[i] == "-hours" && i + 1 < args.Length)
                {
                    float.TryParse(args[i + 1], out m_TargetGameHours);
                    AIOverhaulPlugin.LogInfo($"Detected -hours {m_TargetGameHours}", LogCategory.AutoStart);
                }
                if (args[i] == "-webhook" && i + 1 < args.Length)
                {
                    m_WebhookUrl = args[i + 1];
                    AIOverhaulPlugin.LogInfo("Detected -webhook <url>", LogCategory.AutoStart);
                }
            }

            if (m_HasStarted)
            {
                AIOverhaulPlugin.LogInfo($"Enabled with provinces={m_Provinces}, difficulty={m_Difficulty}, Target {m_TargetGameHours}h", LogCategory.AutoStart);
                AIOverhaulPlugin.LogInfo("Will monitor for scene load before starting...", LogCategory.AutoStart);
                // DON'T start coroutine immediately - wait for Update() to detect scene is ready
                m_SceneMonitoringStarted = true;
            }
            else
            {
                AIOverhaulPlugin.LogInfo("NOT enabled (-autostart flag not found)", LogCategory.AutoStart);
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
                AIOverhaulPlugin.LogInfo("Started monitoring for scene load...", LogCategory.AutoStart);
            }

            float elapsed = Time.realtimeSinceStartup - m_SceneCheckStartTime;

            // Wait at least 20 seconds for the main menu scene to load
            // This gives Unity time to load all assets and initialize the UI
            if (elapsed >= 20f)
            {
                AIOverhaulPlugin.LogInfo($"{elapsed:F1}s elapsed - Main menu should be ready. Starting routine...", LogCategory.AutoStart);
                m_HasRoutineStarted = true;
                m_SceneMonitoringStarted = false;
                StartCoroutine(AutoStartRoutine());
            }
            else if (Mathf.FloorToInt(elapsed) % 5 == 0 && Mathf.FloorToInt(elapsed) != Mathf.FloorToInt(elapsed - Time.deltaTime))
            {
                // Log every 5 seconds
                AIOverhaulPlugin.LogInfo($"Waiting for scene... ({elapsed:F0}s / 20s)", LogCategory.AutoStart);
            }
        }

        IEnumerator AutoStartRoutine()
        {
            AIOverhaulPlugin.LogInfo("Routine started", LogCategory.AutoStart);

            // Wait for Game instance to be captured by our patch
            AIOverhaulPlugin.LogInfo("Waiting for Game instance to be captured...", LogCategory.AutoStart);
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
                    AIOverhaulPlugin.LogError("Failed to get the game instance.", LogCategory.AutoStart);
                    yield break;
                }
            }
            
            AIOverhaulPlugin.LogInfo("Game instance found. Waiting 10 sec (for luck!) to start game.", LogCategory.AutoStart);
            yield return new WaitForSeconds(10f);

            // Create Campaign and assign to game
            AIOverhaulPlugin.LogInfo("Creating Campaign...", LogCategory.AutoStart);
            Campaign campaign = null;
            try
            {
                campaign = Campaign.CreateSinglePlayerCampaign(MapNames.Europe, PeriodNames.Early);
                if (campaign == null)
                {
                    AIOverhaulPlugin.LogError("Campaign.CreateSinglePlayerCampaign returned null", LogCategory.AutoStart);
                    yield break;
                }
                AIOverhaulPlugin.LogInfo($"Campaign created (ID: {campaign.id})", LogCategory.AutoStart);

                // Assign campaign to game
                game.campaign = campaign;
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"Exception: {ex.Message}\n{ex.StackTrace}", LogCategory.AutoStart);
                yield break;
            }

            // Configure Game Variables (Before Start)
            // This triggers internal game logic (like Shattered Map creation) automatically during start
            AIOverhaulPlugin.LogInfo("Configuring Game Variables...", LogCategory.AutoStart);
            try
            {
                // Set Shattered Map configuration
                string shatteredVal = $"{m_Provinces}_shattered";
                AIOverhaulPlugin.LogInfo("Setting options on campaignData...", LogCategory.AutoStart);
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
                AIOverhaulPlugin.LogError($"Configuring Game Variables failed: {ex.Message}\n{ex.StackTrace}", LogCategory.AutoStart);
            }

            // STEP 4: Start the game
            AIOverhaulPlugin.LogInfo("Calling game.StartGame()...", LogCategory.AutoStart);
            try
            {
                game.StartGame(true, MapNames.Europe);
                AIOverhaulPlugin.LogInfo($"StartGame called (State: {game.state})", LogCategory.AutoStart);
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"StartGame failed: {ex.Message}\n{ex.StackTrace}", LogCategory.AutoStart);
                yield break;
            }

            // Wait for map to load
            AIOverhaulPlugin.LogInfo("Waiting for map to load...", LogCategory.AutoStart);
            yield return new WaitForSeconds(10f);
            AIOverhaulPlugin.LogInfo($"Map load complete (State: {game.state})", LogCategory.AutoStart);

            // Enable Spectator & Speed
            AIOverhaulPlugin.LogInfo("Enabling Spectator Mode and Speed...", LogCategory.AutoStart);
            AIOverhaulPlugin.ToggleSpectatorMode();
            m_SpectatorEnabled = true;
            game.SetSpeed(100f);

            AIOverhaulPlugin.LogInfo("Complete. Ready to play.", LogCategory.AutoStart);
        }

        void SendWebhook(string message)
        {
            if (string.IsNullOrEmpty(m_WebhookUrl)) return;
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    client.Headers[System.Net.HttpRequestHeader.ContentType] = "application/json";
                    string json = "{\"content\":\"" + message.Replace("\"", "\\\"") + "\"}";
                    client.UploadString(m_WebhookUrl, json);
                }
                AIOverhaulPlugin.LogInfo("Webhook notification sent", LogCategory.AutoStart);
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"Webhook failed: {ex.Message}", LogCategory.AutoStart);
            }
        }

        float m_LastLoggedHour = -1f;
        float m_StartingGameHours = -1f;

        float m_TargetGameHours = 10f;

        void MonitorGameProgress()
        {
            var game = AIOverhaulPlugin.CurrentGame;
            if (game == null) return;

            // Calculate hours elapsed using session_time (in-game time)
            float gameHours = game.session_time.hours;
            
            // Capture starting hours on first valid reading
            if (m_StartingGameHours < 0 && gameHours > 0)
            {
                m_StartingGameHours = gameHours;
                int hStart = Mathf.FloorToInt(gameHours);
                int mStart = Mathf.FloorToInt((gameHours - hStart) * 60);
                AIOverhaulPlugin.LogInfo($"Game time tracking started. Current time: {hStart}h {mStart}m", LogCategory.AutoStart);
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

                AIOverhaulPlugin.LogInfo($"Progress - Played {hPlayed}h {mPlayed}m / Target {m_TargetGameHours:F0}h (Total Game Time: {hTotal}h {mTotal}m)", LogCategory.AutoStart);
            }

            // Check if target game hours reached
            if (hoursPlayed >= m_TargetGameHours)
            {
                int hPlayed = Mathf.FloorToInt(hoursPlayed);
                int mPlayed = Mathf.FloorToInt((hoursPlayed - hPlayed) * 60);
                AIOverhaulPlugin.LogInfo($"Target reached - {hPlayed}h {mPlayed}m of game time played. Quitting...", LogCategory.AutoStart);
                SendWebhook($"KoH2 AutoStart test complete — {hPlayed}h {mPlayed}m of game time played.");
                Application.Quit();
            }
        }
    }
}