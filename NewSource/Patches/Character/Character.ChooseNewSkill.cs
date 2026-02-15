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

            if (!__instance.IsKing()) return;
            if (kingdom.GetBooks() < GameBalance.MinBooksForFirstSkillUpgrade) return; // Not enough books

            // Prioritize Writing then Learning for Tradition unlocking
            if (TryPickSkill(skills, SkillNames.Writing, kingdom, ref __result)) return;
            if (TryPickSkill(skills, SkillNames.Learning, kingdom, ref __result)) return;
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
    }
}
