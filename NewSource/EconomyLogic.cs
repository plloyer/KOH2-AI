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
    // Prevent building churches in settlements without religion districts
    // Prioritize provinces with the most religion district slots
    // "AddBuildOptions" generates the list of available buildings and upgrades for a castle.
    [HarmonyPatch(typeof(Castle), "AddBuildOptions", new Type[] { typeof(bool), typeof(Resource) })]
    public class ReligiousBuildingPatch
    {
        static void Postfix(Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

            // Get Religion district definition
            Logic.District.Def religionDistrict = DistrictHelper.GetDistrict(__instance.game, DistrictNames.Religion);
            if (religionDistrict == null) return;

            // Check if this castle has the Religion district
            bool hasReligionDistrict = __instance.HasDistrict(religionDistrict);

            // Find all religious buildings in build options
            for (int i = Castle.build_options.Count - 1; i >= 0; i--)
            {
                var option = Castle.build_options[i];
                if (option.def == null) continue;

                bool isReligiousBuilding = IsReligiousBuilding(option.def.id);

                if (isReligiousBuilding)
                {
                    if (!hasReligionDistrict)
                    {
                        // Block building if no religion district
                        Castle.build_options.RemoveAt(i);
                    }
                    else
                    {
                        // Boost priority for castles with religion district
                        // Further boost based on how many religion slots available
                        int religionSlots = CountReligionSlots(__instance, religionDistrict);
                        float boost = 1.0f + (religionSlots * GameBalance.ReligionBuildingBoostPerSlot);
                        option.eval *= boost;
                        Castle.build_options[i] = option;
                    }
                }
            }

            // Do the same for upgrade options
            for (int i = Castle.upgrade_options.Count - 1; i >= 0; i--)
            {
                var option = Castle.upgrade_options[i];
                if (option.def == null) continue;

                bool isReligiousBuilding = IsReligiousBuilding(option.def.id);

                if (isReligiousBuilding)
                {
                    if (!hasReligionDistrict)
                    {
                        // Block building if no religion district
                        Castle.upgrade_options.RemoveAt(i);
                    }
                    else
                    {
                        // Boost priority for castles with religion district
                        int religionSlots = CountReligionSlots(__instance, religionDistrict);
                        float boost = 1.0f + (religionSlots * GameBalance.ReligionBuildingBoostPerSlot);
                        option.eval *= boost;
                        Castle.upgrade_options[i] = option;
                    }
                }
            }
        }

        static bool IsReligiousBuilding(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return false;

            // Religious buildings include Church, Masjid, Temple, Cathedral, GreatMosque
            return buildingId == BuildingNames.Church ||
                   buildingId == BuildingNames.Masjid ||
                   buildingId == BuildingNames.Temple ||
                   buildingId == BuildingNames.Cathedral ||
                   buildingId == BuildingNames.GreatMosque;
        }

        static int CountReligionSlots(Castle castle, Logic.District.Def religionDistrict)
        {
            if (religionDistrict?.buildings == null) return 0;

            // Count how many religion building slots exist in this district definition
            return religionDistrict.buildings.Count;
        }
    }

    // OBSOLETE: ConsiderHireMerchant and ConsiderHireCleric methods don't exist
    // All character hiring goes through ConsiderExpense(KingdomAI.Expense)
    // See CharacterHiringControlPatch below

    // "ConsiderExpense" evaluates a specific expense (hiring, building, bribing) to decide if the AI should pay for it.
    [HarmonyPatch(typeof(KingdomAI), "ConsiderExpense", new Type[] { typeof(KingdomAI.Expense) })]
    public class CharacterHiringControlPatch
    {
        static bool Prefix(KingdomAI __instance, KingdomAI.Expense expense)
        {
            if (__instance.kingdom == null || __instance.kingdom.is_player)
                return true;

            // Check if this is a hiring expense
            if (expense.type != KingdomAI.Expense.Type.HireChacacter)
                return true;

            if (!(expense.defParam is Logic.CharacterClass.Def cDef))
                return true;

            // DIAGNOSTIC: Log every character hiring ConsiderExpense call to verify patch is working
            AIOverhaulPlugin.LogMod($"[DIAGNOSTIC] ConsiderExpense called: kingdom={__instance.kingdom?.Name ?? "null"}, character={cDef.id}", LogCategory.General);

            // Log ALL character hiring attempts for England (before Enhanced AI check)
            if (__instance.kingdom.Name == "England")
            {
                bool isEnhanced = AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom);
                AIOverhaulPlugin.LogMod($"[ENGLAND] ConsiderExpense(Expense) called for {cDef.id}, isEnhancedAI={isEnhanced}", LogCategory.Economy);
            }

            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom))
                return true;

            // MERCHANT HIRING LOGIC
            if (cDef.id == CharacterClassNames.Merchant)
            {
                int merchants = KingdomHelper.CountMerchants(__instance.kingdom);
                float maxCommerce = TraverseAPI.GetMaxCommerce(__instance.kingdom);
                int requiredCommerce = (merchants + 1) * GameBalance.CommercePerMerchant;

                if (__instance.kingdom.Name == "England")
                {
                    AIOverhaulPlugin.LogMod($"[ENGLAND] Merchant hiring check: merchants={merchants}, maxCommerce={maxCommerce}, requiredCommerce={requiredCommerce}", LogCategory.Economy);
                }

                // Allow first 2 merchants unconditionally
                if (merchants < GameBalance.RequiredMerchantCount)
                {
                    if (__instance.kingdom.Name == "England")
                    {
                        AIOverhaulPlugin.LogMod($"[ENGLAND] ALLOWING merchant #{merchants + 1} (first 2 are guaranteed)", LogCategory.Economy);
                    }
                    return true; // Allow vanilla to hire (ConsiderExpense just evaluates, actual hiring happens elsewhere)
                }

                // For 3rd+ merchant: strict commerce check
                if (requiredCommerce > maxCommerce)
                {
                    if (__instance.kingdom.Name == "England")
                    {
                        AIOverhaulPlugin.LogMod($"[ENGLAND] BLOCKING merchant hire: need {requiredCommerce} but only have {maxCommerce} max commerce", LogCategory.Economy);
                    }
                    return false; // Block hire
                }

                if (__instance.kingdom.Name == "England")
                {
                    AIOverhaulPlugin.LogMod($"[ENGLAND] ALLOWING merchant hire: {requiredCommerce} <= {maxCommerce}", LogCategory.Economy);
                }
                return true; // Allow hiring (commerce check passed)
            }

            // CLERIC HIRING LOGIC
            if (cDef.id == CharacterClassNames.Cleric)
            {
                // Rule: Hire 1 cleric after 2 merchants and 50+ gold income
                float income = KingdomHelper.GetGoldIncome(__instance.kingdom);
                int merchants = KingdomHelper.CountMerchants(__instance.kingdom);
                bool hasCleric = KingdomHelper.HasCleric(__instance.kingdom);

                if (__instance.kingdom.Name == "England")
                {
                    AIOverhaulPlugin.LogMod($"[ENGLAND] Cleric hiring check: income={income}, merchants={merchants}, hasCleric={hasCleric}", LogCategory.Economy);
                }

                // Prerequisites: 2 merchants first, then 50+ income
                if (merchants < GameBalance.RequiredMerchantCount)
                {
                    if (__instance.kingdom.Name == "England")
                    {
                        AIOverhaulPlugin.LogMod($"[ENGLAND] BLOCKING cleric: need {GameBalance.RequiredMerchantCount} merchants first", LogCategory.Economy);
                    }
                    return false;
                }

                if (income < GameBalance.MinGoldIncomeForClerics)
                {
                    if (__instance.kingdom.Name == "England")
                    {
                        AIOverhaulPlugin.LogMod($"[ENGLAND] BLOCKING cleric: income too low ({income} < {GameBalance.MinGoldIncomeForClerics})", LogCategory.Economy);
                    }
                    return false;
                }

                if (hasCleric)
                {
                    if (__instance.kingdom.Name == "England")
                    {
                        AIOverhaulPlugin.LogMod($"[ENGLAND] BLOCKING cleric: already have one", LogCategory.Economy);
                    }
                    return false;
                }

                if (__instance.kingdom.Name == "England")
                {
                    AIOverhaulPlugin.LogMod($"[ENGLAND] ALLOWING cleric hire", LogCategory.Economy);
                }
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
            }

            // DIPLOMAT HIRING LOGIC
            if (cDef.id == CharacterClassNames.Diplomat)
            {
                bool wants = WarLogicHelper.WantsDiplomat(__instance.kingdom);
                if (!wants)
                {
                    if (__instance.kingdom.Name == "England")
                    {
                        AIOverhaulPlugin.LogMod($"[ENGLAND] BLOCKING diplomat in ConsiderExpense(Expense): WantsDiplomat returned false", LogCategory.Economy);
                    }
                    return false;
                }

                if (__instance.kingdom.Name == "England")
                {
                    AIOverhaulPlugin.LogMod($"[ENGLAND] ALLOWING diplomat hire in ConsiderExpense(Expense)", LogCategory.Economy);
                }
            }

            return true;
        }
    }

    // "AddExpense" adds a potential expense option to the AI's consideration list.
    [HarmonyPatch(typeof(KingdomAI), "AddExpense", new Type[] { typeof(WeightedRandom<KingdomAI.Expense>), typeof(KingdomAI.Expense) })]
    public class TradeActionPriorityPatch
    {
        static void Prefix(KingdomAI __instance, object expenses, KingdomAI.Expense expense)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (expense.category == KingdomAI.Expense.Category.Diplomacy)
            {
                if (expense.defParam is Logic.Action action && action.def.id == "TradeAction")
                {
                    expense.eval *= 0.7f; // Lower eval = higher priority
                }
            }
        }
    }

    // "EvalBuild" evaluates the priority/desirability of constructing a specific building definition.
    [HarmonyPatch(typeof(Castle), "EvalBuild")]
    public class BuildingPrioritizationPatch
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
    [HarmonyPatch(typeof(KingdomAI.GovernOption), "Eval")]
    public class KnightAssignmentPatch
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
    [HarmonyPatch(typeof(KingdomAI), "ConsiderIncreaseCrownAuthority")]
    public static class SpendingPriorityPatch
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
