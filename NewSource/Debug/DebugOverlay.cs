using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AIOverhaul;

namespace AIOverhaul
{
    public class DebugOverlay : MonoBehaviour
    {
        public static DebugOverlay Instance;

        // Configuration
        Rect windowRect = new Rect(20, 150, 800, 600);
        Vector2 scrollPosition;
        bool isVisible = false;

        // Data Storage
        public struct ExpenseRecord
        {
            public string Name;
            public float Score;
            public string Category;
        }

        List<ExpenseRecord> consideredExpenses = new List<ExpenseRecord>();
        float lastClearTime = 0f;
        const float CLEAR_INTERVAL = 3.0f; // Clear log every 3 seconds to keep it fresh but readable


        void Awake()
        {
            Instance = this;
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
        }

        void OnSpectatorModeChanged(bool isSpectatorMode)
        {
            bool wasVisible = isVisible;
            isVisible = isSpectatorMode;

            if (isVisible && !wasVisible)
            {
                // Reset to default position if toggled on
                windowRect = new Rect(50, 250, 800, 600);
                AIOverhaulPlugin.LogInfo("Overlay toggled ON via Event.");
            }
        }

        void Update()
        {
            // Input is now handled by Plugin.cs -> OnSpectatorModeChanged event
            


            // Auto-clear expenses periodically
            if (Time.time > lastClearTime + CLEAR_INTERVAL)
            {
                ClearExpenses();
                lastClearTime = Time.time;
            }
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
            style.fontSize = 16;
            style.richText = true;
            style.wordWrap = true;
            style.margin = new RectOffset(4, 4, 0, 0); // 4px horizontal, 0 vertical for compactness
            style.padding = new RectOffset(0, 0, 0, 0);
            // style.alignment = TextAnchor.UpperLeft; // Removed to avoid TextRenderingModule dependency
            
            GUILayout.Label("<b>AI Debug Overlay (F9)</b>", style);
            // Reduced space
            GUILayout.Space(2);

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
                GUILayout.Label($"<b>{k.Name}</b>", style);
                DrawKeyRelations(k, style);
                DrawExpenseLog(style);
            }
        }

        Logic.Kingdom GetPlayerKingdom()
        {
            var game = AIOverhaulPlugin.CurrentGame; 
            if (game != null)
            {
                foreach(var kingdom in game.kingdoms)
                {
                    if (kingdom.is_player) return kingdom;
                }
            }
            return null;
        }

        void DrawKeyRelations(Logic.Kingdom k, GUIStyle style)
        {
            // Mortal Enemy
            var nemesis = AIOverhaulPlugin.GetMortalEnemy(k, k.game);
            string nemesisName = nemesis != null ? nemesis.Name : "None";
            GUILayout.Label($"Mortal Enemy: <color=red>{nemesisName}</color>", style);

            // Neighbors - Combined into one label to avoid gaps
            if (k.neighbors != null)
            {
                var neighborsData = new List<(Logic.Kingdom k, float rel)>();
                foreach (var n in k.neighbors)
                {
                    if (n is Logic.Kingdom nk)
                    {
                        // Calc relationship (false = don't calc fade, just get current)
                        float val = Logic.KingdomAndKingdomRelation.Get(k, nk, false).GetRelationship();
                        neighborsData.Add((nk, val));
                    }
                }

                // Sort: Friend (High) -> Neutral -> Enemy (Low)
                neighborsData.Sort((a, b) => b.rel.CompareTo(a.rel));

                // Build string
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("Neighbors: "); // Prefix directly in string

                float minRel = Logic.RelationUtils.Def.minRelationship;
                float maxRel = Logic.RelationUtils.Def.maxRelationship;

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
            GUILayout.Label($"<b>--- Considered Expenses (Last {CLEAR_INTERVAL}s) ---</b>", style);
            
            // Sort by Score descending
            var sorted = consideredExpenses.OrderByDescending(e => e.Score).ToList();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));
            
            foreach (var record in sorted)
            {
                string color = record.Score > 100 ? "green" : (record.Score > 10 ? "white" : "grey");
                GUILayout.Label($"<color={color}>[{record.Score:F1}]</color> {record.Name} ({record.Category})", style);
            }

            GUILayout.EndScrollView();
        }
    }
}
