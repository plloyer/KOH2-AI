 using System;
using HarmonyLib;
using Logic;
using UnityEngine;
using AIOverhaul.Constants;
using AIOverhaul.Helpers;

namespace AIOverhaul
{
    // "ConsiderExpense" evaluates a specific expense (hiring, building, bribing) to decide if the AI should pay for it.
    // Intent: ConsiderExpense
    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderExpense", typeof(Logic.KingdomAI.Expense))]
    public class KingdomAI_ConsiderExpense
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.KingdomAI.Expense expense)
        {
            if (__instance.kingdom == null)
                return true;
            
            // Check if this is a hiring expense
            if (expense.type != Logic.KingdomAI.Expense.Type.HireChacacter)
                return true;

            if (!(expense.defParam is Logic.CharacterClass.Def cDef))
                return true;

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
                    AIOverhaulPlugin.LogDebug($"ALLOWING merchant #{merchants + 1} (first 2 are guaranteed)", LogCategory.Economy, __instance.kingdom);
                    return true; // Allow vanilla to hire (ConsiderExpense just evaluates, actual hiring happens elsewhere)
                }

                // For 3rd+ merchant: strict commerce check
                if (requiredCommerce > maxCommerce)
                    return false; // Block hire

                AIOverhaulPlugin.LogDebug($"ALLOWING merchant hire: {requiredCommerce} <= {maxCommerce} (Merchants: {merchants})", LogCategory.Economy, __instance.kingdom);
                return true; // Allow hiring (commerce check passed)
            }
            
            // Gate: Require 2 Merchants before hiring any other class (Clerics, Spies, Diplomats, Marshals)
            int currentMerchants = KingdomHelper.CountMerchants(__instance.kingdom);
            if (currentMerchants < GameBalance.RequiredMerchantCount)
            {
                // Allow 1st Marshal even if merchants are low
                if (cDef.id == CharacterClassNames.Marshal)
                {
                   int currentMarshals = KingdomHelper.CountCourtMembers(__instance.kingdom, CharacterClassNames.Marshal);
                   if (currentMarshals == 0) 
                   {
                        AIOverhaulPlugin.LogDebug("ALLOWING first Marshal hire despite low merchant count", LogCategory.Economy, __instance.kingdom);
                        return true;
                   }
                }

                // Strict rule: No non-merchant characters (and no 2nd+ Marshal) until we have 2 merchants
                return false;
            }
            
            // CLERIC HIRING LOGIC
            if (cDef.id == CharacterClassNames.Cleric)
            {
                // Rule: Hire 1 cleric after 2 merchants and 50+ gold income
                float income = KingdomHelper.GetGoldIncome(__instance.kingdom);
                bool hasCleric = KingdomHelper.HasCleric(__instance.kingdom);
                
                if (hasCleric || income < GameBalance.MinGoldIncomeForClerics)
                    return false;

                AIOverhaulPlugin.LogDebug($"ALLOWING cleric hire (Income: {income:F1} >= {GameBalance.MinGoldIncomeForClerics}, HasCleric: {hasCleric})", LogCategory.Economy, __instance.kingdom);
                return true;
            }

            // SPY HIRING LOGIC
            if (cDef.id == CharacterClassNames.Spy)
            {
                float income = KingdomHelper.GetGoldIncome(__instance.kingdom);
                if (income < GameBalance.MinGoldIncomeForSpies)
                    return false; // Block this expense from being considered

                if (!WarLogicHelper.WantsSpy(__instance.kingdom))
                    return false;

                AIOverhaulPlugin.LogDebug($"ALLOWING spy hire (Income: {income:F1} >= {GameBalance.MinGoldIncomeForSpies}, WantsSpy: True)", LogCategory.Economy, __instance.kingdom);
                return true;
            }

            // DIPLOMAT HIRING LOGIC
            if (cDef.id == CharacterClassNames.Diplomat)
            {
                bool wants = WarLogicHelper.WantsDiplomat(__instance.kingdom);
                if (!wants)
                    return false;

                AIOverhaulPlugin.LogDebug("ALLOWING diplomat hire (WantsDiplomat: True)", LogCategory.Economy, __instance.kingdom);
            }

            return true;
        }
    }

    // "ChooseNewSkill" picks a new skill for a character when they gain a level or slot.
    // Intent: ChooseNewSkill (Writing Tradition Logic)
    [HarmonyPatch(typeof(Logic.Character), "ChooseNewSkill")]
    public static class Character_ChooseNewSkill
    {
        [HarmonyPrefix]
        public static bool Prefix(Logic.Character __instance, System.Collections.Generic.List<Logic.Skill.Def> skills, ref Logic.Skill.Def __result)
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
    public static class Character_ThinkUpgradeSkill
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

    [HarmonyPatch(typeof(KingdomAI), "AddExpense", new[] { typeof(WeightedRandom<KingdomAI.Expense>), typeof(KingdomAI.Expense) })]
    public class KingdomAI_AddExpense
    {
        static void Prefix(KingdomAI __instance, object expenses, KingdomAI.Expense expense)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;

            // URGENT MERCHANT HIRING
            if (expense.type == KingdomAI.Expense.Type.HireChacacter &&
                expense.defParam is CharacterClass.Def cd &&
                cd.id == CharacterClassNames.Merchant)
            {
                float maxCommerce = TraverseAPI.GetMaxCommerce(__instance.kingdom);
                int merchants = KingdomHelper.CountMerchants(__instance.kingdom);
                int usedCommerce = merchants * GameBalance.CommercePerMerchant;
                float availableCommerce = maxCommerce - usedCommerce;

                // Check if we have idle merchants (merchants without active trade routes)
                bool hasIdleMerchant = KingdomHelper.HasIdleMerchant(__instance.kingdom);

                // FORCE priority for the first two merchants, OR if we have idle slots and commerce
                if (merchants < GameBalance.RequiredMerchantCount || (availableCommerce >= GameBalance.MinCommerceForMerchant && hasIdleMerchant))
                {
                    expense.eval *= GameBalance.UrgentPriorityMultiplier;
                    AIOverhaulPlugin.LogDebug($"URGENT merchant hire - Merchants: {merchants}, AvailableCommerce: {availableCommerce}, HasIdle: {hasIdleMerchant}", LogCategory.Economy, __instance.kingdom);
                }
            }

            if (expense.category == KingdomAI.Expense.Category.Diplomacy)
            {
                // Trade is free, but lowering eval ensures it's prioritized over other free diplomatic actions
                if (expense.defParam is Logic.Action action && action.def.id == ActionNames.Trade)
                    expense.eval *= GameBalance.HighPriorityMultiplier; // Lower eval = higher priority
            }
        }
    }

    // "ConsiderIncreaseCrownAuthority" decides if the kingdom should spend resources to increase crown authority.
    // Intent: SpendingPriorityPatch
    [HarmonyPatch(typeof(KingdomAI), "ConsiderIncreaseCrownAuthority")]
    public static class KingdomAI_ConsiderIncreaseCrownAuthority
    {
        [HarmonyPrefix]
        public static bool Prefix(KingdomAI __instance, ref bool __result)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Block until kingdom has built military infrastructure
            bool hasBarracks = BuildingHelper.HasBuilding(__instance.kingdom, BuildingNames.Barracks);
            bool hasSwordsmith = BuildingHelper.HasBuildingUpgrade(__instance.kingdom, BuildingUpgradeNames.Swordsmith);
            bool hasFletcher = BuildingHelper.HasBuildingUpgrade(__instance.kingdom, BuildingUpgradeNames.Fletcher_Barracks);

            if (!hasBarracks || !hasSwordsmith || !hasFletcher)
            {
                __result = false;
                return false; // Block Crown Authority
            }

            // Block CA if rushing tradition (400+ books, Writing/Learning available)
            if (TraditionHelper.ShouldRushTradition(__instance.kingdom))
            {
                __result = false;
                return false;
            }

            // Block CA if any province can upgrade fortifications to level 1
            if (__instance.kingdom.realms != null)
            {
                foreach (var realm in __instance.kingdom.realms)
                {
                    if (realm?.castle != null &&
                        realm.castle.CanUpgradeFortification() &&
                        realm.castle.fortifications.level == 0)
                    {
                        __result = false;
                        return false;
                    }
                }
            }

            return true;
        }
    }

    // "ConsiderHireMarshal" checks if a Marshal should be hired.
    // Intent: Prioritize Merchants over Marshals by skipping this check if we don't have enough merchants.
    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderHireMarshal")]
    public static class KingdomAI_ConsiderHireMarshal
    {
        [HarmonyPrefix]
        public static bool Prefix(Logic.KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            return true;
        }
    }

    // "ConsiderHireMerchant" checks if a Merchant should be hired.
    // Intent: Bypass the "Trade Disagreement" limit for the first 2 merchants.
    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderHireMerchant")]
    public static class KingdomAI_ConsiderHireMerchant
    {
        [HarmonyPrefix]
        public static bool Prefix(Logic.KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Vanilla logic caps merchants at Trade Agreements count (or max commerce logic).
            // We want to force the first 2 merchants regardless of trade agreements.
            if (KingdomHelper.CountMerchants(__instance.kingdom) < GameBalance.RequiredMerchantCount)
            {
                // Verify we have a valid merchant def to use
                if (__instance.game?.ai?.merchant_def == null) return true;

                AIOverhaulPlugin.LogDebug("Forcing Merchant consideration (Bypassing Trade Agreement limits)", LogCategory.Economy, __instance.kingdom);

                // Manually trigger the expense consideration
                // This will hit our KingdomAI_ConsiderExpense patch, which allows the first 2 merchants unconditionally.
                var merchantDef = __instance.game.ai.merchant_def;
                TraverseAPI.ConsiderExpense(
                    __instance, 
                    Logic.KingdomAI.Expense.Type.HireChacacter, 
                    merchantDef, 
                    null, 
                    merchantDef.ai_category
                );

                __result = true; // "I have considered hiring a merchant"
                return false;    // Skip original vanilla logic which would have returned false due to limits
            }

            return true;
        }
    }
}
