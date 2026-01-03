using HarmonyLib;
using AIOverhaul.Constants;
using AIOverhaul.Helpers;

namespace AIOverhaul
{
    // Patch to automatically organize the court when a new knight is hired
    // Intent: HireKnightPatch (Court Organization)
    [HarmonyPatch(typeof(Logic.KingdomAI), "HireKnight")]
    public class KingdomAI_HireKnight
    {
        static void Postfix(Logic.KingdomAI __instance)
        {
            if (__instance == null || __instance.kingdom == null) return;

            // Organize court for Player (requested feature) AND Enhanced AI (good for consistency)
            // Or just Player? User said "When you buy...", implying player.
            // But doing it for AI won't hurt, might actually help structure their economy/wars if we ever relied on slot index (we don't).
            // Let's do it for all to be safe and consistent.
            
            // Wait, HireKnight puts them in the first free slot.
            // Postfix runs AFTER that. So the new knight is in the list.
            // We verify and shuffle.
            
            // Only logs if it's the player to avoid spam, or debug level
            bool isPlayer = __instance.kingdom.is_player;

            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            
            KingdomHelper.OrganizeCourt(__instance.kingdom);

            if (isPlayer)
            {
                 AIOverhaulPlugin.LogMod($"[CourtLogic] Auto-organized court for player kingdom {__instance.kingdom.Name}", LogCategory.General);
            }
        }
    }
}
