using HarmonyLib;
using Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Castle), nameof(Castle.ChooseBuildOption))]
    public static class Castle_ChooseBuildOption
    {
        const string k_LogPrefix = "[AI Decision]";
        public static void Prefix(Game game, List<Castle.BuildOption> options, float sum)
        {
            if (options == null || options.Count == 0) return;

            var castle = options[0].castle;
            var kingdom = castle?.GetKingdom();

            // Only log for AI (non-player kingdoms)
            if (kingdom == null || kingdom.is_player) return;

            StringBuilder sb = new StringBuilder();
            sb.Append($"{k_LogPrefix} {kingdom.Name} ({castle.name}) Choosing Build/Upgrade from {options.Count} options:");
            
            var sortedOptions = new List<Castle.BuildOption>(options);
            sortedOptions.Sort((a, b) => b.eval.CompareTo(a.eval));

            int count = 0;
            foreach (var opt in sortedOptions)
            {
                if (count++ > 10) 
                {
                    sb.Append("    ... (more options truncated)");
                    break;
                }
                // opt.def.Name causes error. using opt.def.ToString() or opt.def
                sb.Append($"    [{opt.eval:F1}] {opt.def} (Priority: {opt.priority})");
            }

            AIOverhaulPlugin.LogDebug(sb.ToString(), LogCategory.Economy, kingdom);
        }
    }
}
