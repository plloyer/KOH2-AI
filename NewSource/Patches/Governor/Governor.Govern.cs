using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Logic.Character), "Govern")]
    public class Character_Govern
    {
        static void Prefix(Logic.Character __instance, Logic.Castle castle)
        {
            if (__instance == null || castle == null) return;

            Logic.Kingdom kingdom = castle.GetKingdom();
            if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;

            // No actual change — skip logging
            if (castle == __instance.GetGovernedCastle()) return;

            if (!GovernOption_Eval.PendingScores.TryGetValue(__instance, out var scores)) return;

            string classId = __instance.class_def?.id ?? "?";
            string scoreStr = string.Join(", ", scores.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value:F0}"));
            AIOverhaulPlugin.LogInfo($"[Governor] {__instance.Name} ({classId}) → {castle.name} | Scores: {scoreStr}", LogCategory.Governor, kingdom);

            GovernOption_Eval.PendingScores.Remove(__instance);
        }
    }
}
