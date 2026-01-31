 using System;
 using UnityEngine;

 namespace AIOverhaul
{

    // "ChooseNewSkill" picks a new skill for a character when they gain a level or slot.
    // Intent: ChooseNewSkill (Writing Tradition Logic)
    // [HarmonyPatch(typeof(Logic.Character), "ChooseNewSkill")]
    // public static class Character_ChooseNewSkill
    // {
    //     [HarmonyPrefix]
    //     public static bool Prefix(Logic.Character __instance, System.Collections.Generic.List<Logic.Skill.Def> skills, ref Logic.Skill.Def __result)
    //     {
    //         if (__instance.IsKing() && AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom()))
    //         {
    //             var writingSkill = skills.Find(s => s != null && s.id == SkillNames.Writing + "Skill");
    //             if (writingSkill != null)
    //             {
    //                 __result = writingSkill;
    //                 return false;
    //             }
    //         }
    //
    //         return true;
    //     }
    // }

    // "ThinkUpgradeSkill" decides whether to upgrade an existing skill to the next rank.
    // [HarmonyPatch(typeof(Logic.Character), "ThinkUpgradeSkill")]
    // public static class Character_ThinkUpgradeSkill
    // {
    //     [HarmonyPrefix]
    //     public static bool Prefix(Logic.Character __instance, bool for_free, ref bool __result)
    //     {
    //         if (__instance.IsKing() && AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom()))
    //         {
    //             var skillsRef = TraverseAPI.GetSkills(__instance);
    //             if (skillsRef != null)
    //             {
    //                 var writingSkill = skillsRef.Find(s => s != null && s.def != null && s.def.id == SkillNames.Writing + "Skill");
    //                 if (writingSkill != null && __instance.CanAddSkillRank(writingSkill))
    //                 {
    //                     if (!for_free)
    //                     {
    //                         var kingdom = __instance.GetKingdom();
    //                         if (kingdom != null)
    //                         {
    //                             var upgradeCost = writingSkill.def.GetUpgardeCost(__instance);
    //                             if (!kingdom.resources.CanAfford(upgradeCost, 1f)) return false;
    //
    //                             var expenseCategory = TraverseAPI.GetExpenseCategory(__instance);
    //                             Logic.Kingdom.in_AI_spend = true;
    //                             kingdom.SubResources(expenseCategory, upgradeCost);
    //                             Logic.Kingdom.in_AI_spend = false;
    //                         }
    //                     }
    //
    //                     __instance.AddSkillRank(writingSkill);
    //                     __result = true;
    //                     return false;
    //                 }
    //             }
    //         }
    //
    //         return true;
    //     }
    // }

    // "ConsiderHireMerchant" checks if a Merchant should be hired.
    // Intent: Bypass the "Trade Disagreement" limit for the first 2 merchants.
    // [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderHireMerchant")]
    // public static class KingdomAI_ConsiderHireMerchant
    // {
    //     [HarmonyPrefix]
    //     public static bool Prefix(Logic.KingdomAI __instance, ref bool __result)
    //     {
    //         if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
    //
    //         // Vanilla logic caps merchants at Trade Agreements count (or max commerce logic).
    //         // We want to force the first 2 merchants regardless of trade agreements.
    //         if (KingdomHelper.CountMerchants(__instance.kingdom) < GameBalance.RequiredMerchantCount)
    //         {
    //             // Verify we have a valid merchant def to use
    //             if (__instance.game?.ai?.merchant_def == null) return true;
    //
    //             AIOverhaulPlugin.LogDebug("Forcing Merchant consideration (Bypassing Trade Agreement limits)", LogCategory.Economy, __instance.kingdom);
    //
    //             // Manually trigger the expense consideration
    //             // This will hit our KingdomAI_ConsiderExpense patch, which allows the first 2 merchants unconditionally.
    //             var merchantDef = __instance.game.ai.merchant_def;
    //             TraverseAPI.ConsiderExpense(
    //                 __instance, 
    //                 Logic.KingdomAI.Expense.Type.HireChacacter, 
    //                 merchantDef, 
    //                 null, 
    //                 merchantDef.ai_category
    //             );
    //
    //             __result = true; // "I have considered hiring a merchant"
    //             return false;    // Skip original vanilla logic which would have returned false due to limits
    //         }
    //
    //         return true;
    //     }
    // }
}
