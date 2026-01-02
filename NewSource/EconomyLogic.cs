using System;
using HarmonyLib;
using Logic;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AIOverhaul.Constants;
using AIOverhaul.Helpers;

namespace AIOverhaul
{


    // "ConsiderExpense" evaluates a specific expense (hiring, building, bribing) to decide if the AI should pay for it.
    // Intent: ConsiderExpense
    [HarmonyPatch(typeof(KingdomAI), "ConsiderExpense", typeof(KingdomAI.Expense))]
    public class ConsiderExpensePatch
    {
        static bool Prefix(KingdomAI __instance, KingdomAI.Expense expense)
        {
            if (__instance.kingdom == null)
                return true;



            // Check if this is a hiring expense
            if (expense.type != KingdomAI.Expense.Type.HireChacacter)
                return true;

            if (!(expense.defParam is CharacterClass.Def cDef))
                return true;

            // DIAGNOSTIC: Log every character hiring ConsiderExpense call to verify patch is working
            bool isEnhanced = AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom);

            if (!isEnhanced)
                return true;

            // MERCHANT HIRING LOGIC
            if (cDef.id == CharacterClassNames.Merchant)
            {
                int merchants = KingdomHelper.CountMerchants(__instance.kingdom);
                float maxCommerce = TraverseAPI.GetMaxCommerce(__instance.kingdom);
                int requiredCommerce = (merchants + 1) * GameBalance.CommercePerMerchant;
                
                // Allow first 2 merchants unconditionally
                if (merchants < GameBalance.RequiredMerchantCount)
                {
                    AIOverhaulPlugin.LogDiagnostic($"ALLOWING merchant #{merchants + 1} (first 2 are guaranteed)", LogCategory.Economy, __instance.kingdom);
                    return true; // Allow vanilla to hire (ConsiderExpense just evaluates, actual hiring happens elsewhere)
                }

                // For 3rd+ merchant: strict commerce check
                if (requiredCommerce > maxCommerce)
                    return false; // Block hire

                AIOverhaulPlugin.LogDiagnostic($"ALLOWING merchant hire: {requiredCommerce} <= {maxCommerce} (Merchants: {merchants})", LogCategory.Economy, __instance.kingdom);
                return true; // Allow hiring (commerce check passed)
            }
            
            // Gate: Require 2 Merchants before hiring any other class (Clerics, Spies, Diplomats, Marshals)
            int currentMerchants = KingdomHelper.CountMerchants(__instance.kingdom);
            if (currentMerchants < GameBalance.RequiredMerchantCount)
            {
                // Strict rule: No non-merchant characters until we have 2 merchants
                return false;
            }
            

            // CLERIC HIRING LOGIC
            if (cDef.id == CharacterClassNames.Cleric)
            {
                // Rule: Hire 1 cleric after 2 merchants and 50+ gold income
                float income = KingdomHelper.GetGoldIncome(__instance.kingdom);
                bool hasCleric = KingdomHelper.HasCleric(__instance.kingdom);
                
                if (income < GameBalance.MinGoldIncomeForClerics)
                    return false;

                if (hasCleric)
                    return false;

                AIOverhaulPlugin.LogDiagnostic($"ALLOWING cleric hire (Income: {income:F1} >= {GameBalance.MinGoldIncomeForClerics}, HasCleric: {hasCleric})", LogCategory.Economy, __instance.kingdom);
                return true;
            }

            // SPY HIRING LOGIC
            if (cDef.id == CharacterClassNames.Spy)
            {
                float income = KingdomHelper.GetGoldIncome(__instance.kingdom);
                if (income < GameBalance.MinGoldIncomeForSpies)
                {
                    return false; // Block this expense from being considered
                }

                if (!WarLogicHelper.WantsSpy(__instance.kingdom))
                {
                    return false;
                }

                AIOverhaulPlugin.LogDiagnostic($"ALLOWING spy hire (Income: {income:F1} >= {GameBalance.MinGoldIncomeForSpies}, WantsSpy: True)", LogCategory.Economy, __instance.kingdom);
                return true;
            }

            // DIPLOMAT HIRING LOGIC
            if (cDef.id == CharacterClassNames.Diplomat)
            {
                bool wants = WarLogicHelper.WantsDiplomat(__instance.kingdom);
                if (!wants)
                {
                    return false;
                }

                AIOverhaulPlugin.LogDiagnostic("ALLOWING diplomat hire (WantsDiplomat: True)", LogCategory.Economy, __instance.kingdom);
            }

            return true;
        }
    }

    // "AddExpense" adds a potential expense option to the AI's consideration list.
    // Intent: TradeActionPriorityPatch
    [HarmonyPatch(typeof(KingdomAI), "AddExpense", new[] { typeof(WeightedRandom<KingdomAI.Expense>), typeof(KingdomAI.Expense) })]
    public class AddExpensePatch
    {
        static void Prefix(KingdomAI __instance, object expenses, KingdomAI.Expense expense)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (expense.category == KingdomAI.Expense.Category.Diplomacy)
            {
                if (expense.defParam is Logic.Action action && action.def.id == ActionNames.Trade)
                {
                    expense.eval *= 0.7f; // Lower eval = higher priority
                }
            }
        }
    }

    // "EvalBuild" evaluates the priority/desirability of constructing a specific building definition.
    // Intent: BuildingPrioritizationPatch
    [HarmonyPatch(typeof(Castle), "EvalBuild")]
    public class EvalBuildPatch
    {
        static void Postfix(Castle __instance, Logic.Building.Def def, Resource production_weights, ref float __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;
            if (def.id.Contains(BuildingNames.MarketSquare) || def.id.Contains("Farm") || def.id.Contains(CharacterClassNames.Merchant))
            {
                __result *= 1.3f;
            }
        }
    }

    // "Eval" (GovernOption) scores how suitable a specific character is for governing a specific town.
    // Intent: KnightAssignmentPatch
    [HarmonyPatch(typeof(KingdomAI.GovernOption), "Eval")]
    public class EvalPatch_2
    {
        static void Postfix(ref KingdomAI.GovernOption __instance, ref float __result)
        {
            if (__instance.governor == null || __instance.castle == null) return;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.castle.GetKingdom())) return;

            if (__instance.governor.class_def?.id == CharacterClassNames.Merchant)
            {
                if (__instance.castle.buildings.Any(b => b.def.id.Contains(BuildingNames.MarketSquare)))
                {
                    __result += 20f;
                }
            }
        }
    }

    // "ConsiderIncreaseCrownAuthority" decides if the kingdom should spend resources to increase crown authority.
    // Intent: SpendingPriorityPatch
    [HarmonyPatch(typeof(KingdomAI), "ConsiderIncreaseCrownAuthority")]
    public static class ConsiderIncreaseCrownAuthorityPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(KingdomAI __instance, ref bool __result)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Safety checks
            if (__instance.kingdom.traditions == null || __instance.kingdom.wars == null || __instance.kingdom.resources == null) return true;

            // Block CA if Rushing Tradition
            // Check 0 Traditions, Not at War
            if (__instance.kingdom.traditions.Count == 0 && __instance.kingdom.wars.Count == 0)
            {
                 // If we have books, save gold for tradition.
                 // Correctly access resources
                 float books = __instance.kingdom.resources.Get(ResourceType.Books);
                 float gold = __instance.kingdom.resources.Get(ResourceType.Gold);

                 if (books >= 350f && gold < 2000f) // Thresholds
                 {
                     __result = false;
                     return false; // Block CA
                 }
            }

            // Block CA if Fortifications needed
            if (__instance.kingdom.realms != null)
            {
                foreach (var realm in __instance.kingdom.realms)
                {
                     // Remove IsDepleted check, rely on CanUpgradeFortification
                     if (realm.castle != null && realm.castle.CanUpgradeFortification())
                     {
                         if (realm.castle.fortifications.level == 0) // Prioritize level 1 heavily
                         {
                             // Also checking if we can mostly afford it? 
                             // If we can afford it, we should definitely block CA.
                             // Even if we can't afford it yet, we should probably save for it if it's level 0?
                             __result = false;
                             return false; 
                         }
                     }
                }
            }

            return true;
        }
    }

    // "ChooseNewSkill" picks a new skill for a character when they gain a level or slot.
    // Intent: ChooseNewSkill (Writing Tradition Logic)
    [HarmonyPatch(typeof(Logic.Character), "ChooseNewSkill")]
    public static class ChooseNewSkillPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Logic.Character __instance, List<Logic.Skill.Def> skills, ref Logic.Skill.Def __result)
        {
            if (__instance.IsKing() && AIOverhaulPlugin.EnhancedKingdomIds.Contains(__instance.GetKingdom().id))
            {
                var writingSkill = skills.Find(s => s != null && s.id == SkillNames.Writing + "Skill");
                if (writingSkill != null)
                {
                    __result = writingSkill;
                    return false;
                }
            }

            return true;
        }
    }

    // "ThinkUpgradeSkill" decides whether to upgrade an existing skill to the next rank.
    // Intent: ThinkUpgradeSkill (Writing Upgrade Logic)
    [HarmonyPatch(typeof(Logic.Character), "ThinkUpgradeSkill")]
    public static class ThinkUpgradeSkillPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Logic.Character __instance, bool for_free, ref bool __result)
        {
            if (__instance.IsKing() && AIOverhaulPlugin.EnhancedKingdomIds.Contains(__instance.GetKingdom().id))
            {
                var skillsRef = TraverseAPI.GetSkills(__instance);
                if (skillsRef != null)
                {
                    var writingSkill = skillsRef.Find(s => s != null && s.def != null && s.def.id == SkillNames.Writing + "Skill");
                    if (writingSkill != null && __instance.CanAddSkillRank(writingSkill))
                    {
                        if (!for_free)
                        {
                            var kingdom = __instance.GetKingdom();
                            if (kingdom != null)
                            {
                                var upgradeCost = writingSkill.def.GetUpgardeCost(__instance);
                                if (!kingdom.resources.CanAfford(upgradeCost, 1f)) return false;

                                var expenseCategory = TraverseAPI.GetExpenseCategory(__instance);
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

    // OBSOLETE: ConsiderExpense overload with complex parameters doesn't exist
    // All character hiring goes through ConsiderExpense(KingdomAI.Expense)
    // See CharacterHiringControlPatch above
}
