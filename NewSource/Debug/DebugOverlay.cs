using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Logic;
using UnityEngine;
using Event = UnityEngine.Event;

namespace AIOverhaul
{
    public class DebugOverlay : MonoBehaviour
    {
        public static DebugOverlay Instance;

        // Utility colors
        const string ColorGrey = "grey";
        const string ColorWhite = "white";
        const string ColorGreen = "green";
        const string ColorRed = "red";
        const string ColorCyan = "cyan";
        const string ColorYellow = "yellow";
        const string ColorOrange = "orange";
        const string ColorMagenta = "magenta";
        
        // Color Constants (HTML color names or hex codes)
        const string ColorEconomy = ColorOrange;
        const string ColorFood = ColorYellow;
        const string ColorMilitary = "#FF6666";      // Light red
        const string ColorReligion = "#CC99FF";      // Light purple
        const string ColorCoastal = "#6699FF";       // Blue


        // Configuration
        Rect windowRect = new Rect(20, 150, 800, 800);
        Vector2 scrollPosition;
        bool isVisible;

        // Data Storage
        public struct ExpenseRecord
        {
            public string Name;
            public float Score;
            public string Category;
        }

        List<ExpenseRecord> consideredExpenses = new List<ExpenseRecord>();

        void Awake()
        {
            Instance = this;

            // Subscribe to AI evaluation events to clear expenses when new cycle starts
            AIEvaluationEvents.OnGeneralEvaluationStart += OnEvaluationStart;
            AIEvaluationEvents.OnMilitaryEvaluationStart += OnEvaluationStart;
        }

        void Start()
        {
        }

        void OnEnable()
        {
            AIOverhaulPlugin.LogInfo("DebugOverlay Component ENABLED");
            AIOverhaulPlugin.OnSpectatorModeChanged += OnSpectatorModeChanged;
            
            // Sync initial state
            OnSpectatorModeChanged(AIOverhaulPlugin.SpectatorMode);
        }

        void OnDisable()
        {
            AIOverhaulPlugin.LogInfo("DebugOverlay Component DISABLED");
            AIOverhaulPlugin.OnSpectatorModeChanged -= OnSpectatorModeChanged;

            // Unsubscribe from AI evaluation events
            AIEvaluationEvents.OnGeneralEvaluationStart -= OnEvaluationStart;
            AIEvaluationEvents.OnMilitaryEvaluationStart -= OnEvaluationStart;
        }

        void OnSpectatorModeChanged(bool isSpectatorMode)
        {
            bool wasVisible = isVisible;
            isVisible = isSpectatorMode;

            if (isVisible && !wasVisible)
            {
                // Reset to default position if toggled on
                windowRect = new Rect(50, 150, 800, 800);
                AIOverhaulPlugin.LogInfo("Overlay toggled ON via Event.");
            }
        }

        void OnEvaluationStart(KingdomAI kingdomAI)
        {
            // Clear expenses for new evaluation cycle
            ClearExpenses();
        }

        void Update()
        {
            // Input is now handled by Plugin.cs -> OnSpectatorModeChanged event
        }

        public void RecordConsideredExpense(string name, float score, string category)
        {
            if (!isVisible) return;

            consideredExpenses.Add(new ExpenseRecord { Name = name, Score = score, Category = category });
            
            // Keep list manageable
            if (consideredExpenses.Count > 100)
            {
                consideredExpenses.RemoveAt(0);
            }
        }

        void ClearExpenses()
        {
            consideredExpenses.Clear();
        }

        void OnGUI()
        {
            if (!isVisible) return;

            // Use GUILayout.Area for a non-interactive, transparent overlay
            GUILayout.BeginArea(windowRect);
            
            // Define Custom Style for larger text and compact spacing
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 18; // Increased from 16
            style.richText = true;
            style.wordWrap = true;
            style.margin = new RectOffset(0, 0, 0, 0); // Zero margins for tight stacking
            style.padding = new RectOffset(0, 0, 0, 0);
            
            // Diagnostics Input
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.R)
                {
                    Logic.Kingdom k = GetPlayerKingdom();
                    if (k != null) 
                    {
                        k.RecalculateNeighbors();
                        AIOverhaulPlugin.LogInfo("Forced Neighbor Recalculation via Debug Overlay");
                    }
                    Event.current.Use(); // Consume input
                }
                // Force Breakpoint for Rider
                if (Event.current.keyCode == KeyCode.B && Event.current.shift)
                {
                    AIOverhaulPlugin.LogInfo("Triggering Manual Breakpoint...");
                    Debugger.Break();
                    Event.current.Use(); // Consume input
                }
            }

            GUILayout.Label("<b>AI Debug Overlay (F9)</b>", style);
            // Minimized space
            GUILayout.Space(0);

            DrawOverlayContent(style);

            GUILayout.EndArea();
        }

        void DrawOverlayContent(GUIStyle style)
        {
            Logic.Kingdom k = GetPlayerKingdom();

            if (k == null)
            {
                GUILayout.Label("No active Player Kingdom found.", style);
            }
            else
            {
                GUILayout.Label($"<b>{k.Name}</b> (ID: {k.id})", style);
                DrawKeyRelations(k, style);
                
                // Diagnostic View (Hold Alt)
                if (Event.current.alt)
                {
                    DrawRealmNeighborsDebug(k, style);
                }
                else
                {
                    DrawKingdomStats(k, style);
                    DrawRealmSettlements(k, style);
                    DrawBuddySystem(k, style);
                    DrawExpenseLog(style);
                }
            }
        }

        void DrawBuddySystem(Logic.Kingdom k, GUIStyle style)
        {
            GUILayout.Label("<b>--- Buddy System (Military) ---</b>", style);
            if (k.armies == null) return;

            int activeLinks = 0;
            foreach (var army in k.armies)
            {
                if (army == null || !army.IsValid()) continue;

                // Only show leaders to avoid duplicate lines (Leader->Follower shown once)
                bool isFollower = BuddySystem.IsFollower(army, k);
                if (isFollower) continue;

                var follower = BuddySystem.GetBuddy(army, k);
                if (follower == null) continue;

                activeLinks++;
                string leaderName = army.leader?.Name ?? "Unknown";
                string followerName = follower.leader?.Name ?? "Unknown";

                // Get Target Info from the leader army
                string targetInfo = "";
                var tgtObj = army.GetTarget();
                var tgtRealm = army.tgt_realm;

                if (tgtObj != null)
                {
                    string tName = "Unknown";
                    if (tgtObj is Castle c) tName = c.name;
                    else if (tgtObj is Logic.Army a) tName = "Army " + (a.leader?.Name ?? "?");
                    else if (tgtObj is Logic.Battle b) tName = "Battle";
                    else tName = tgtObj.ToString();

                    targetInfo = $" [Target: {tName}]";
                }
                else if (tgtRealm != null)
                {
                    targetInfo = $" [MoveTo: {tgtRealm.name}]";
                }
                else
                {
                    targetInfo = " [Idle]";
                }

                GUILayout.Label($"{leaderName} + {followerName}{targetInfo}", style);
            }

            if (activeLinks == 0)
            {
                GUILayout.Label($"<color={ColorGrey}>No active buddy links</color>", style);
            }
            GUILayout.Space(5);
        }

        void DrawRealmNeighborsDebug(Logic.Kingdom k, GUIStyle style)
        {
            GUILayout.Label("<color=orange>=== NEIGHBOR DIAGNOSTICS (Press 'R' to Recalc) ===</color>", style);
            GUILayout.Label($"Kingdom Neighbors Count: {k.neighbors.Count}", style);
            
            if (k.realms != null) 
            {
                foreach(var r in k.realms)
                {
                    StringBuilder sb = new StringBuilder();
                    // Display Town Name (Realm Name) for clarity
                    string realmLabel = !string.IsNullOrEmpty(r.town_name) ? $"{r.town_name} ({r.name})" : r.name;
                    
                    sb.Append($"<b>{realmLabel}</b>: ");
                    if (r.logicNeighborsRestricted != null)
                    {
                        foreach(var n in r.logicNeighborsRestricted)
                        {
                           var nk = n.GetKingdom();
                           string kName = nk != null ? nk.Name : "null";
                           string nLabel = !string.IsNullOrEmpty(n.town_name) ? n.town_name : n.name;
                           
                           string color = (nk == k) ? "grey" : "yellow";
                           sb.Append($"<color={color}>{nLabel} ({kName})</color>, ");
                        }
                    }
                    else
                    {
                         sb.Append("No Restricted Neighbors");
                    }
                    GUILayout.Label(sb.ToString(), style);
                }
            }
        }

        Logic.Kingdom GetPlayerKingdom()
        {
            var game = AIOverhaulPlugin.CurrentGame;
            if (game != null && game.kingdoms != null)
            {
                foreach(var kingdom in game.kingdoms)
                {
                    if (kingdom != null && kingdom.is_player) return kingdom;
                }
            }
            return null;
        }


        void DrawKingdomStats(Logic.Kingdom k, GUIStyle style)
        {
            float gold = k.GetGold();
            float goldIncome = k.GetGoldIncome();
            float books = k.GetBooks();
            int merchants = k.CountMerchants();

            bool hasBarracks = k.HasBuilding(BuildingNames.Barracks);

            string barracksStatus = hasBarracks ? "True" : "False";

            string barracksColor = hasBarracks ? "green" : "red";

            float food = k.GetFood();
            float foodIncome = k.GetFoodIncome();
            int tradeAgreements = k.GetTradeAgreementCount();

            GUILayout.Label($"Food: <color={ColorFood}>{food:F0} / {foodIncome:F0}</color>", style);
            GUILayout.Label($"Gold: <color={ColorEconomy}>{gold:F0}</color> (+{goldIncome:F0}/s) | Books: <color={ColorReligion}>{books:F0}</color> | Merchants: <color={ColorEconomy}>{merchants}</color> | TA: <color={ColorEconomy}>{tradeAgreements}</color>", style);
            
            // Build Options stats
            int buildCount = Castle.last_build_options.Count;
            int upgradeCount = Castle.last_upgrade_options.Count;
            GUILayout.Label($"Build Options: <color=white>{buildCount}</color> | Upgrade Options: <color=white>{upgradeCount}</color>", style);

            if (buildCount > 0)
            {
                string topBuilds = "Top Builds: ";
                for (int i = 0; i < Math.Min(buildCount, 3); i++)
                {
                    var opt = Castle.last_build_options[i];
                    string castleName = opt.castle?.name ?? "?";
                    topBuilds += $"{opt.def.id}@{castleName}({opt.eval:F0}) ";
                }
                GUILayout.Label(topBuilds, style);
            }

            if (upgradeCount > 0)
            {
                string topUpgrades = "Top Upgrades: ";
                for (int i = 0; i < Math.Min(upgradeCount, 3); i++)
                {
                    var opt = Castle.last_upgrade_options[i];
                    string castleName = opt.castle?.name ?? "?";
                    topUpgrades += $"{opt.def.id}@{castleName}({opt.eval:F0}) ";
                }
                GUILayout.Label(topUpgrades, style);
            }
            GUILayout.Space(5);
        }

        void DrawKeyRelations(Logic.Kingdom k, GUIStyle style)
        {
            // Mortal Enemy
            var nemesis = AIOverhaulPlugin.GetMortalEnemy(k, k.game);
            string nemesisDisplay = nemesis != null ? $"<color={ColorRed}>{nemesis.Name}</color>" : "None";
            GUILayout.Label($"Mortal Enemy: {nemesisDisplay}", style);

            // Expansion Target
            var expansionTarget = k.SelectExpansionTarget();
            string expansionDisplay = expansionTarget != null ? $"<color={ColorMilitary}>{expansionTarget.Name}</color>" : "None";
            GUILayout.Label($"Expansion Target: {expansionDisplay}", style);

            // Neighbors - Combined into one label to avoid gaps
            if (k.neighbors != null)
            {
                var neighborsData = new List<(Logic.Kingdom k, float rel)>();
                foreach (var n in k.neighbors)
                {
                    if (n is Logic.Kingdom nk)
                    {
                        // Calc relationship (false = don't calc fade, just get current)
                        float val = KingdomAndKingdomRelation.Get(k, nk, false).GetRelationship();
                        neighborsData.Add((nk, val));
                    }
                }

                // Sort: Friend (High) -> Neutral -> Enemy (Low)
                neighborsData.Sort((a, b) => b.rel.CompareTo(a.rel));

                // Build string
                StringBuilder sb = new StringBuilder();
                sb.Append("Neighbors: "); // Prefix directly in string

                float minRel = RelationUtils.Def.minRelationship;
                float maxRel = RelationUtils.Def.maxRelationship;

                for (int i = 0; i < neighborsData.Count; i++)
                {
                    var data = neighborsData[i];
                    Color c = Color.white;
                    
                    if (data.rel > 0)
                    {
                        // White -> Green
                        float t = Mathf.Clamp01(data.rel / maxRel);
                        c = Color.Lerp(Color.white, Color.green, t);
                    }
                    else
                    {
                        // Red -> White (rel is negative)
                        // t=0 (0) -> White, t=1 (min) -> Red
                        float t = Mathf.Clamp01(Mathf.Abs(data.rel) / Mathf.Abs(minRel));
                        c = Color.Lerp(Color.white, Color.red, t);
                    }

                    string hex = ColorUtility.ToHtmlStringRGB(c);
                    sb.Append($"<color=#{hex}>{data.k.Name}</color>");
                    
                    if (i < neighborsData.Count - 1) sb.Append(", ");
                }
                
                // Allow multiline
                GUILayout.Label(sb.ToString(), style);
            }
        }

        void DrawExpenseLog(GUIStyle style)
        {
            GUILayout.Label("<b>--- Considered Expenses (Until Next Cycle) ---</b>", style);
            
            // Sort by Score descending
            var sorted = consideredExpenses.OrderByDescending(e => e.Score).ToList();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            
            foreach (var record in sorted)
            {
                // Cyan (< 1.0), Green (< 10.0), White (< 100.0), Grey (>= 100.0)
                string color = record.Score < 1.0f ? "cyan" : (record.Score < 10.0f ? "green" : (record.Score < 100.0f ? "white" : "grey"));
                GUILayout.Label($"<color={color}>[{record.Score:F1}]</color> {record.Name} ({record.Category})", style);
            }

            GUILayout.EndScrollView();
        }
        
        void DrawRealmSettlements(Logic.Kingdom k, GUIStyle style)
        {
            GUILayout.Label("<b>--- Province Settlements ---</b>", style);

            if (k.realms == null) return;

            foreach (var r in k.realms)
            {
                if (r == null) continue;

                // Get counts using DistrictHelper - Order: Keeps, Village, Religious, Farm, Coastal
                int keeps = r.GetKeepCount();
                int villages = r.GetVillageCount();
                int religious = r.GetReligiousCount();
                int farms = r.GetFarmCount();
                int coastal = r.GetCoastalCount();

                // Get Goods stats
                int currentGoods, maxGoods;
                RealmHelper.GetGoodsStats(r, out currentGoods, out maxGoods);
                string goodsColor = currentGoods > 0 ? (currentGoods >= maxGoods ? ColorGreen : ColorYellow) : ColorGrey;

                // Format the output string with color coding
                StringBuilder sb = new StringBuilder();
                string realmName = !string.IsNullOrEmpty(r.town_name) ? r.town_name : r.name;
                sb.Append($"<b>{realmName}</b> [Goods: <color={goodsColor}>{currentGoods}/{maxGoods}</color>]: ");

                var parts = new List<string>();

                // Order: Keeps, Village, Religious, Farm, Coastal
                if (keeps > 0)
                    parts.Add($"<color={ColorMilitary}>{keeps} Keep</color>");
                if (villages > 0)
                    parts.Add($"<color={ColorEconomy}>{villages} Village</color>");
                if (religious > 0)
                    parts.Add($"<color={ColorReligion}>{religious} Religious</color>");
                if (farms > 0)
                    parts.Add($"<color={ColorFood}>{farms} Farm</color>");
                if (coastal > 0)
                    parts.Add($"<color={ColorCoastal}>{coastal} Coastal</color>");

                if (parts.Count > 0)
                    sb.Append(string.Join(", ", parts));
                else
                    sb.Append($"<color={ColorGrey}>Empty</color>");

                GUILayout.Label(sb.ToString(), style);
            }
            GUILayout.Space(5);
        }
    }
}
