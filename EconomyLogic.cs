using HarmonyLib;
using Logic;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderHireMerchant")]
    public class MerchantHiringPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            if (__instance.kingdom.resources[Logic.ResourceType.Gold] > 500 && __instance.kingdom.court.Count < 9)
            {
                int merchants = 0;
                foreach (var k in __instance.kingdom.court)
                    if (k.class_def?.id == "Merchant")
                        merchants++;
                if (merchants < 2)
                {
                    AIOverhaulPlugin.Instance.Log($"[AI-Mod] High priority Merchant hire for {__instance.kingdom.Name} (Gold: {__instance.kingdom.resources[Logic.ResourceType.Gold]})");
                    Traverse.Create(__instance).Method("HireKnight", new object[] { "Merchant" }).GetValue();
                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "AddExpense", new System.Type[] { typeof(WeightedRandom<Logic.KingdomAI.Expense>), typeof(Logic.KingdomAI.Expense) })]
    public class TradeActionPriorityPatch
    {
        static void Prefix(Logic.KingdomAI __instance, object expenses, Logic.KingdomAI.Expense expense)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (expense.category == Logic.KingdomAI.Expense.Category.Diplomacy)
            {
                if (expense.defParam is Logic.Action action && action.def.id == "TradeAction")
                {
                    expense.eval *= 0.7f; // Lower eval = higher priority
                }
            }
        }
    }

    [HarmonyPatch(typeof(Logic.Castle), "EvalBuild")]
    public class BuildingPrioritizationPatch
    {
        static void Postfix(Logic.Castle __instance, Logic.Building.Def def, Logic.Resource production_weights, ref float __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;
            if (def.id.Contains("Market") || def.id.Contains("Farm") || def.id.Contains("Merchant"))
            {
                __result *= 1.3f;
            }
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI.GovernOption), "Eval")]
    public class KnightAssignmentPatch
    {
        static void Postfix(ref Logic.KingdomAI.GovernOption __instance, ref float __result)
        {
            if (__instance.governor == null || __instance.castle == null) return;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.castle.GetKingdom())) return;

            if (__instance.governor.class_def?.id == "Merchant")
            {
                if (__instance.castle.buildings.Any(b => b.def.id.Contains("Market")))
                {
                    __result += 20f;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderAdoptTradition")]
    public static class ChooseAdoptTraditionPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Logic.KingdomAI __instance, ref bool __result)
        {
            if (AIOverhaulPlugin.EnhancedKingdomIds.Contains(__instance.kingdom.id))
            {
                var traditionOptions = __instance.kingdom.GetNewTraditionOptions();
                if (traditionOptions == null || traditionOptions.Count == 0) return true;

                // Look for Writing tradition
                var writingTradition = traditionOptions.Find(t => t.name == "Writing");
                if (writingTradition != null)
                {
                    if (__instance.kingdom.resources.CanAfford(writingTradition.GetAdoptCost(__instance.kingdom)))
                    {
                        // Use Traverse to call private ConsiderExpense
                        Traverse.Create(__instance).Method("ConsiderExpense",
                            Logic.KingdomAI.Expense.Type.AdoptTradition,
                            (Logic.BaseObject)writingTradition,
                            (UnityEngine.Object)null,
                            Logic.KingdomAI.Expense.Category.Economy,
                            Logic.KingdomAI.Expense.Priority.High,
                            null // args
                        ).GetValue();

                        __result = true;
                        return false; // Skip original
                    }
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Logic.Character), "ChooseNewSkill")]
    public static class ChooseNewSkillPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Logic.Character __instance, List<Logic.Skill.Def> skills, ref Logic.Skill.Def __result)
        {
            if (__instance.IsKing() && AIOverhaulPlugin.EnhancedKingdomIds.Contains(__instance.GetKingdom().id))
            {
                var writingSkill = skills.Find(s => s.id == "WritingSkill");
                if (writingSkill != null)
                {
                    __result = writingSkill;
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Logic.Character), "ThinkUpgradeSkill")]
    public static class ThinkUpgradeSkillPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Logic.Character __instance, bool for_free, ref bool __result)
        {
            if (__instance.IsKing() && AIOverhaulPlugin.EnhancedKingdomIds.Contains(__instance.GetKingdom().id))
            {
                var skillsRef = Traverse.Create(__instance).Field("skills").GetValue<List<Logic.Skill>>();
                if (skillsRef != null)
                {
                    var writingSkill = skillsRef.Find(s => s.def.id == "WritingSkill");
                    if (writingSkill != null && __instance.CanAddSkillRank(writingSkill))
                    {
                        if (!for_free)
                        {
                            var kingdom = __instance.GetKingdom();
                            if (kingdom != null)
                            {
                                var upgradeCost = writingSkill.def.GetUpgardeCost(__instance);
                                if (!kingdom.resources.CanAfford(upgradeCost, 1f)) return false;

                                var expenseCategory = (Logic.KingdomAI.Expense.Category)Traverse.Create(__instance).Method("GetExpenseCategory").GetValue();
                                Logic.Kingdom.in_AI_spend = true;
                                kingdom.SubResources(expenseCategory, upgradeCost);
                                Logic.Kingdom.in_AI_spend = false;
                            }
                        }

                        __instance.AddSkillRank(writingSkill);
                        __result = true;
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
