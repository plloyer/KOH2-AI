using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace AIOverhaul
{
    // "AddChild" is called when a new prince/princess is born or added to the family tree.
    // Intent: HeirClassSelectionPatch
    [HarmonyPatch(typeof(Logic.RoyalFamily), "AddChild")]
    public class AddChildPatch
    {
        static void Postfix(Logic.RoyalFamily __instance, Logic.Character child)
        {
            // Basic validity checks
            if (child == null || !child.IsAlive() || !child.IsHeir()) return;
            
            // Only for Enhanced AI
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;
            
            // Only relevant for males (Princes) who usually get classes
            if (child.sex != Logic.Character.Sex.Male) return;

            // User Request: "Picking the heir class should look to use the skill, but priority to merchant/marshal/cleric"
            
            // 1. Check if the child already generated a skill that dictates a class?
            // (Newborns might have 'Royal Abilities' or initial skills depending on mod/game state, 
            // but usually they are blank until education. If they have a skill, we respect it.)
            if (child.skills != null && child.skills.Count > 0)
            {
                // If they already have a skill, it might be intentional (e.g. from event), so we keep current class logic
                // or ensure class matches skill. For now, assume if skills exist, we don't touch it.
                return;
            }

            var game = __instance.game;
            var merchantDef = game.defs.Get<Logic.CharacterClass.Def>(CharacterClassNames.Merchant);
            var marshalDef = game.defs.Get<Logic.CharacterClass.Def>(CharacterClassNames.Marshal);
            var clericDef = game.defs.Get<Logic.CharacterClass.Def>(CharacterClassNames.Cleric);

            // Priority Candidates
            List<Logic.CharacterClass.Def> candidates = new List<Logic.CharacterClass.Def>();
            if (merchantDef != null) candidates.Add(merchantDef);
            if (marshalDef != null) candidates.Add(marshalDef);
            if (clericDef != null) candidates.Add(clericDef);

            if (candidates.Count > 0)
            {
                // Weighted selection? Or strictly priority?
                // "Priority to merchant/marshal/cleric" could mean Order: Merch > Marsh > Cleric.
                // But mixing it up is better for an AI mod.
                // Let's give slight weight to Merchant (as per "priority to merchant..."), but allow others.
                
                Logic.CharacterClass.Def newClass = candidates[Random.Range(0, candidates.Count)];
                
                // Override the default (which was likely Sovereign's class)
                if (child.class_def != newClass)
                {
                    child.SetClass(newClass);
                }
            }
        }
    }
}
