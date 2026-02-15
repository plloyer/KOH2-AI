using System;
using System.Collections.Generic;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Logic.Character), "ChooseNewSkill", typeof(List<Skill.Def>))]
    public static class Character_ChooseNewSkill
    {
        const string k_LogPrefix = "[Character_ChooseNewSkill] ";
        public static void Postfix(Logic.Character __instance, ref Skill.Def __result, List<Skill.Def> skills)
        {
            if (__instance == null || skills == null || skills.Count == 0) return;
            var kingdom = __instance.GetKingdom();
            if (!AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;

            // Block Leadership — it's a terrible skill
            if (__result != null && __result.Is(SkillNames.Leadership))
            {
                // Pick anything else
                foreach (var skill in skills)
                {
                    if (!skill.Is(SkillNames.Leadership))
                    {
                        AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocked Leadership, picking {skill.id} instead", LogCategory.Governor, kingdom);
                        __result = skill;
                        break;
                    }
                }
            }

            // Merchant: prioritize Bargain then Logistics
            if (__instance.class_def?.id == CharacterClassNames.Merchant)
            {
                if (TryPickSkill(skills, SkillNames.Bargain, kingdom, ref __result)) return;
                if (TryPickSkill(skills, SkillNames.Logistics, kingdom, ref __result)) return;
            }

            // Non-marshal: prioritize Writing (generates books for traditions)
            if (!__instance.IsMarshal())
            {
                if (TryPickSkill(skills, SkillNames.Writing, kingdom, ref __result)) return;
            }

            // King with enough books: prioritize Learning for faster tradition unlocking
            if (__instance.IsKing() && kingdom.GetBooks() >= GameBalance.MinBooksForFirstSkillUpgrade)
            {
                if (TryPickSkill(skills, SkillNames.Learning, kingdom, ref __result)) return;
            }

            // All: prefer skills matching an active tradition
            if (TryPickTraditionSkill(skills, kingdom, __result, ref __result)) return;
        }

        static bool TryPickSkill(List<Skill.Def> skills, string skillName, Logic.Kingdom kingdom, ref Skill.Def result)
        {
            foreach (var skill in skills)
            {
                if (skill.Is(skillName))
                {
                    result = skill;
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Picking skill: {skillName}", LogCategory.Governor, kingdom);
                    return true;
                }
            }
            return false;
        }

        static bool TryPickTraditionSkill(List<Skill.Def> skills, Logic.Kingdom kingdom, Skill.Def current, ref Skill.Def result)
        {
            if (kingdom.traditions == null || kingdom.traditions.Count == 0) return false;

            // If current pick already matches a tradition, keep it
            if (current != null && SkillMatchesTradition(current, kingdom)) return false;

            foreach (var skill in skills)
            {
                if (skill.Is(SkillNames.Leadership)) continue;
                if (SkillMatchesTradition(skill, kingdom))
                {
                    result = skill;
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix}Picking tradition-matching skill: {skill.id}", LogCategory.Governor, kingdom);
                    return true;
                }
            }
            return false;
        }

        static bool SkillMatchesTradition(Skill.Def skill, Logic.Kingdom kingdom)
        {
            for (int i = 0; i < kingdom.traditions.Count; i++)
            {
                var tdef = kingdom.traditions[i]?.def;
                if (tdef != null && (tdef.BuffsSkill(skill) || tdef.GrantsSkill(skill))) return true;
            }
            return false;
        }
    }
}
