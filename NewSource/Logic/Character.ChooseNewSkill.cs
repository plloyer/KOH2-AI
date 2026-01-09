using System;
using System.Collections.Generic;
using AIOverhaul.Constants;
using AIOverhaul.Helpers;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Logic.Character), "ChooseNewSkill", typeof(List<Skill.Def>))]
    public static class Character_ChooseNewSkill
    {
        public static void Postfix(Logic.Character __instance, ref Skill.Def __result, List<Skill.Def> skills)
        {
            // Code Quality: Basic validation
            if (__instance == null || skills == null || skills.Count == 0) return;

            // Use TraverseAPI to access potentially internal/private members if public access fails
            // IsKing() might be internal or a specific check, but checking title is safer if IsKing() refers to title
            // However, to be robust based on the previous error, let's use reflection/Traverse.
            // Assuming IsKing() is a method as per decompilation reading "IsKing()" calls.
            bool isKing = TraverseAPI.GetCharacterIsKing(__instance);
            
            // If IsKing reflection fails (e.g. if it's protected/private), check title directly if public
            // Based on decompilation usage check: "if (title == "King")" is used in CanHaveSkills (line 6345)
            // So we can fallback to checking the title field/property "title"
            if (!isKing)
            {
                string title = TraverseAPI.GetCharacterTitle(__instance);
                if (title != TraverseAPI.CONST_TITLE_KING) return;
            }

            // GetKingdom() resulted in error, so using TraverseAPI
            Logic.Kingdom kingdom = TraverseAPI.GetCharacterKingdom(__instance);
            
            if (kingdom == null) return;

            // Only applies if Enhanced AI is active for this kingdom
            if (!AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;

            // Goal: Prioritize Writing (LiteracySkill) or Learning (LearningSkill) for Tradition unlocking
            
            // 1. Prioritize Writing (Literacy) - Highest Priority
            foreach (var skill in skills)
            {
                // Checking field.key as verified in StudySkillAction.cs
                if (skill.Is(SkillNames.Writing))
                {
                    __result = skill;
                    // AIOverhaul.Helpers.ModLog.Log($"[CharacterSkillLogic] King {__instance.Name} prioritized Writing (LiteracySkill)");
                    return;
                }
            }

            // 2. Prioritize Learning - Secondary Priority
            // Only if we haven't already picked Writing
            foreach (var skill in skills)
            {
                if (skill.Is(SkillNames.Learning))
                {
                    __result = skill;
                     // AIOverhaul.Helpers.ModLog.Log($"[CharacterSkillLogic] King {__instance.Name} prioritized Learning (LearningSkill)");
                    return;
                }
            }
        }
    }
}
