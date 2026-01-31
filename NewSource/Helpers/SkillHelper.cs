using System;
using Logic;

namespace AIOverhaul
{
    public static class SkillHelper
    {
        /// <summary>
        /// Checks if the skill definition matches the given skill name (ID).
        /// Safely handles null fields.
        /// </summary>
        public static bool Is(this Skill.Def skillDef, string skillName)
        {
            if (skillDef == null || skillDef.field == null)
            {
                return false;
            }
            return skillDef.field.key == skillName;
        }
    }
}
