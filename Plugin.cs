using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AIOverhaul
{
    [BepInPlugin("com.mod.aioverhaul", "AI Overhaul", "1.1.0")]
    public class AIOverhaulPlugin : BaseUnityPlugin
    {
        public static AIOverhaulPlugin Instance;
        public static HashSet<int> EnhancedKingdomIds = new HashSet<int>();
        public static HashSet<int> BaselineKingdomIds = new HashSet<int>();
        private static Logic.Game current_game;

        private void Awake()
        {
            Instance = this;
            var harmony = new Harmony("com.mod.aioverhaul");
            harmony.PatchAll();
            Log("AI Overhaul Plugin Loaded with dynamic selection logic.");
        }

        public void Log(string message)
        {
            Logger.LogInfo(message);
        }

        public static bool IsEnhancedAI(Logic.Kingdom k)
        {
            if (k == null) return false;
            return EnhancedKingdomIds.Contains(k.id);
        }

        public static bool IsBaselineAI(Logic.Kingdom k)
        {
            if (k == null) return false;
            return BaselineKingdomIds.Contains(k.id);
        }

        public static void InitializeEnhancedKingdoms(Logic.Game game)
        {
            if (game == null || game.kingdoms == null) return;
            if (game == current_game) return;
            current_game = game;

            EnhancedKingdomIds.Clear();
            BaselineKingdomIds.Clear();

            List<Logic.Kingdom> aiKingdoms = game.kingdoms.Where(k => k != null && !k.is_player && !k.IsDefeated()).ToList();

            int targetCount = Mathf.Max(1, Mathf.RoundToInt(aiKingdoms.Count * 0.10f));

            // Randomize selection
            System.Random rand = new System.Random();
            var shuffled = aiKingdoms.OrderBy(x => rand.Next()).ToList();

            var enhanced = shuffled.Take(targetCount).ToList();
            var baseline = shuffled.Skip(targetCount).Take(targetCount).ToList();

            foreach (var k in enhanced)
            {
                EnhancedKingdomIds.Add(k.id);
            }

            foreach (var k in baseline)
            {
                BaselineKingdomIds.Add(k.id);
            }

            Instance.Log($"[AI-Mod] New game session detected. Selected {EnhancedKingdomIds.Count} enhanced and {BaselineKingdomIds.Count} baseline kingdoms out of {aiKingdoms.Count} total AI kingdoms.");
            Instance.Log($"[AI-Mod] Enhanced: {string.Join(", ", enhanced.Select(k => k.Name))}");
            Instance.Log($"[AI-Mod] Baseline: {string.Join(", ", baseline.Select(k => k.Name))}");
        }
    }

    [HarmonyPatch(typeof(Logic.Game), "Clear")] // Good hook for session ending/cleanup
    public class GameClearPatch
    {
        static void Postfix()
        {
            AIOverhaulPlugin.EnhancedKingdomIds.Clear();
            AIOverhaulPlugin.BaselineKingdomIds.Clear();
        }
    }

    [HarmonyPatch(typeof(Logic.Kingdom), "Destroy")]
    public class KingdomDestroyPatch
    {
        static void Prefix(Logic.Kingdom __instance)
        {
            if (__instance == null) return;
            AILogger.LogDefeat(__instance);
            AIOverhaulPlugin.EnhancedKingdomIds.Remove(__instance.id);
            AIOverhaulPlugin.BaselineKingdomIds.Remove(__instance.id);
        }
    }
}
