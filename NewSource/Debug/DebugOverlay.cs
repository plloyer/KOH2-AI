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
        private Rect windowRect = new Rect(20, 20, 400, 800);
        private Vector2 scrollPosition;
        private bool isVisible = false;

        // Data Storage
        public struct ExpenseRecord
        {
            public string Name;
            public float Score;
            public string Category;
        }

        private List<ExpenseRecord> consideredExpenses = new List<ExpenseRecord>();
        private float lastClearTime = 0f;
        private const float CLEAR_INTERVAL = 3.0f; // Clear log every 3 seconds to keep it fresh but readable

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        void Update()
        {
            // Toggle visibility matches Spectator Mode
            isVisible = AIOverhaulPlugin.SpectatorMode;

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

        private void ClearExpenses()
        {
            consideredExpenses.Clear();
        }

        void OnGUI()
        {
            if (!isVisible) return;

            GUI.skin.window.fontSize = 14;
            GUI.skin.label.fontSize = 12;

            windowRect = GUI.Window(0, windowRect, DrawWindow, "AI Debug Overlay (F9)");
        }

        void DrawWindow(int windowID)
        {
            Logic.Kingdom k = GetPlayerKingdom();

            if (k == null)
            {
                GUILayout.Label("No active Player Kingdom found.");
            }
            else
            {
                DrawKingdomInfo(k);
                GUILayout.Space(10);
                DrawKeyRelations(k);
                GUILayout.Space(10);
                DrawExpenseLog();
            }

            GUI.DragWindow();
        }

        Logic.Kingdom GetPlayerKingdom()
        {
            // Logic.Game.instance might not be easily accessible, but we can try via Plugin's references if needed.
            // For now, let's assume we can access it via a static helper or just finding the player kingdom.
            // Since we are in the game process, we need to find the Logic.Game instance.
            // Best way is to use a known reference. Let's try to pass it from the Plugin or find it.
            
            // NOTE: Logic.Game.current is not a standard Unity singleton.
            // We'll trust that we can get it from AIOverhaulPlugin or similar if we exposed it.
            // For this draft, I will assume we can find it or it is passed.
            // Actually, we can use accessing the static logic if available, or just rely on the plugin having a reference.
            // Plugin.cs has `static Logic.Game current_game;` but it is private.
            // I will add a public getter to Plugin.cs in the next step.
            
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

        void DrawKingdomInfo(Logic.Kingdom k)
        {
            GUILayout.Label($"<b>Kingdom:</b> {k.Name}");
            // Use dictionary lookups or resource helper methods as direct fields don't exist
            float gold = k.resources[Logic.ResourceType.Gold];
            float income = k.income.Get(Logic.ResourceType.Gold);
            float books = k.resources[Logic.ResourceType.Books];
            float piety = k.resources[Logic.ResourceType.Piety];
            
            GUILayout.Label($"<b>Gold:</b> {gold:F0} ({income:F1})");
            GUILayout.Label($"<b>Books:</b> {books:F0}");
            GUILayout.Label($"<b>Piety:</b> {piety:F0}");
        }

        void DrawKeyRelations(Logic.Kingdom k)
        {
            GUILayout.Label("<b>--- Relations ---</b>");
            
            // Mortal Enemy
            var nemesis = AIOverhaulPlugin.GetMortalEnemy(k, k.game);
            string nemesisName = nemesis != null ? nemesis.Name : "None";
            GUILayout.Label($"Mortal Enemy: <color=red>{nemesisName}</color>");

            // Neighbors
            GUILayout.Label("Neighbors:");
            GUILayout.BeginHorizontal();
            if (k.neighbors != null)
            {
                foreach (var n in k.neighbors)
                {
                    if (n is Logic.Kingdom nk)
                    {
                        var rel = Logic.KingdomAndKingdomRelation.Get(k, nk, false);
                        string color = "white";
                        
                        // Check enum flags manually to avoid missing extension methods
                        if ((rel.stance & Logic.RelationUtils.Stance.War) != 0) color = "red";
                        else if ((rel.stance & Logic.RelationUtils.Stance.Alliance) != 0) color = "cyan";
                        else if ((rel.stance & Logic.RelationUtils.Stance.Trade) != 0) color = "green";
                        
                        GUILayout.Label($"<color={color}>{nk.Name}</color>");
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        void DrawExpenseLog()
        {
            GUILayout.Label($"<b>--- Considered Expenses (Last {CLEAR_INTERVAL}s) ---</b>");
            
            // Sort by Score descending
            var sorted = consideredExpenses.OrderByDescending(e => e.Score).ToList();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));
            
            foreach (var record in sorted)
            {
                string color = record.Score > 100 ? "green" : (record.Score > 10 ? "white" : "grey");
                GUILayout.Label($"<color={color}>[{record.Score:F1}]</color> {record.Name} ({record.Category})");
            }

            GUILayout.EndScrollView();
        }
    }
}
