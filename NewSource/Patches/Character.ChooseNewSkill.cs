using System;
using System.Collections.Generic;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Logic.Character), "ChooseNewSkill", typeof(List<Skill.Def>))]
    public static class Character_ChooseNewSkill
    {
        public static void Postfix(Logic.Character __instance, ref Skill.Def __result, List<Skill.Def> skills)
        {
            if (__instance == null || skills == null || skills.Count == 0 || !__instance.IsKing()) return;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

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
