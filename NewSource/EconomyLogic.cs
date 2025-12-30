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
    [HarmonyPatch(typeof(Castle), "AddBuildOptions", new Type[] { typeof(bool), typeof(Resource) })]
    public class ReligiousBuildingPatch
    {
        static void Postfix(Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

            // Get Religion district definition
            Logic.District.Def religionDistrict = __instance.game?.defs?.Get<Logic.District.Def>("Religion");
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

    [HarmonyPatch(typeof(KingdomAI), "ConsiderHireMerchant")]
    public class MerchantHiringPatch
    {
        static bool Prefix(KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            float gold = KingdomHelper.GetGold(__instance.kingdom);
            if (gold > GameBalance.MinGoldForCourtHiring && __instance.kingdom.court.Count < GameConstants.MaxCourtSize)
            {
                int merchants = KingdomHelper.CountMerchants(__instance.kingdom);

                if (merchants < GameBalance.RequiredMerchantCount)
                {
                    // USER OVERRIDE: Force 2 Merchants NO MATTER WHAT.
                    // Removed checks for Commerce and Busy status for the first 2 slots.
                    // This ensures the AI always has a baseline commercial capacity.
                    TraverseAPI.HireKnight(__instance, CharacterClassNames.Merchant);
                    __result = true;
                    return false;
                }
                else
                {
                    // Strict Commerce Check for 3rd+ Merchant
                    // Vanilla uses Ceiling(Max/10), which allows hiring when close (e.g. 24 commerce -> 3 merchants).
                    // We strictly enforce 10 commerce per merchant (e.g. 24 commerce -> max 2 merchants).
                    float maxCommerce = TraverseAPI.GetMaxCommerce(__instance.kingdom);
                    int requiredCommerce = (merchants + 1) * GameBalance.CommercePerMerchant;

                    if (__instance.kingdom.Name == "England")
                    {
                        AIOverhaulPlugin.LogMod($"[ENGLAND] Merchant hiring check: merchants={merchants}, maxCommerce={maxCommerce}, requiredCommerce={requiredCommerce}", LogCategory.Economy);
                    }

                    if (requiredCommerce > maxCommerce)
                    {
                        if (__instance.kingdom.Name == "England")
                        {
                            AIOverhaulPlugin.LogMod($"[ENGLAND] BLOCKING merchant hire: need {requiredCommerce} but only have {maxCommerce} max commerce", LogCategory.Economy);
                        }
                        __result = false;
                        return false; // Block hire
                    }
                    else
                    {
                        // We have enough commerce - hire the merchant ourselves
                        if (__instance.kingdom.Name == "England")
                        {
                            AIOverhaulPlugin.LogMod($"[ENGLAND] ALLOWING merchant hire: {requiredCommerce} <= {maxCommerce}", LogCategory.Economy);
                        }
                        TraverseAPI.HireKnight(__instance, CharacterClassNames.Merchant);
                        __result = true;
                        return false; // Skip vanilla - we handled it
                    }
                }
            }

            // Not enough gold or court full - let vanilla decide
            return true;
        }
    }

    [HarmonyPatch(typeof(KingdomAI), "ConsiderHireCleric")]
    public class ClericHiringPatch
    {
        static bool Prefix(KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Rule: Have at least 1 clergy when positive in Clergy opinion
            // (but only after the first two merchants and at least 50 gold income)

            var k = __instance.kingdom;
            float income = KingdomHelper.GetGoldIncome(k);

            // Check prerequisites
            if (income < GameBalance.MinGoldIncomeForClerics) return true; // Let vanilla decide if poor


            // Count merchants
            int merchants = KingdomHelper.CountMerchants(k);
            if (merchants < GameBalance.RequiredMerchantCount) return true;

            // Simplify: Remove Opinion logic for now to fix build (requires finding SocialGroup enum)
            // if (k.opinions.GetOpinion(Logic.Kingdom.SocialGroup.Clergy) <= 0) return true;

            // Check if we already have a cleric
            if (KingdomHelper.HasCleric(k)) return true;

            // Simplify: Assume court size max is 9 (standard) or check court.Count
            if (k.court.Count < GameConstants.MaxCourtSize)
            {
                 TraverseAPI.HireKnight(__instance, CharacterClassNames.Cleric);
                 __result = true;
                 return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(KingdomAI), "ConsiderExpense", new Type[] { typeof(KingdomAI.Expense) })]
    public class SpyHiringBlockerPatch
    {
        static bool Prefix(KingdomAI __instance, KingdomAI.Expense expense)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Check if this is a hiring expense for a Spy
            if (expense.type == KingdomAI.Expense.Type.HireChacacter) // Note: Game typo 'HireChacacter'
            {
                if (expense.defParam is Logic.CharacterClass.Def cDef && cDef.name == "Spy")
                {
                    // Rule: No spies until the AI has 500 gold income
                    float income = KingdomHelper.GetGoldIncome(__instance.kingdom);
                    if (income < GameBalance.MinGoldIncomeForSpies)
                    {
                        return false; // Block this expense from being considered
                    }
                }
            }

            return true;
        }
    }

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

    [HarmonyPatch(typeof(KingdomAI), "ConsiderExpense")]
    [HarmonyPatch(new Type[] { typeof(Logic.KingdomAI.Expense.Type), typeof(BaseObject), typeof(Logic.Object), typeof(KingdomAI.Expense.Category), typeof(Logic.KingdomAI.Expense.Priority), typeof(List<Value>) })]
    public static class CharacterHiringPatch
    {
        static bool Prefix(KingdomAI __instance, BaseObject defParam, Logic.KingdomAI.Expense.Type type)
        {
            try
            {
                if (__instance.kingdom == null || __instance.kingdom.is_player || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom))
                    return true;

                if (type == KingdomAI.Expense.Type.HireChacacter && defParam is Logic.CharacterClass.Def cDef)
                {
                    // Logic for Diplomat:
                    if (cDef.id == CharacterClassNames.Diplomat)
                    {
                        float goldIncome = __instance.kingdom.income.Get(ResourceType.Gold);

                        if (__instance.kingdom.Name == "England")
                        {
                            AIOverhaulPlugin.LogMod($"[ENGLAND] ConsiderExpense DIPLOMAT: goldIncome={goldIncome}", LogCategory.Knights);
                        }

                        if (goldIncome <= 150f)
                        {
                            if (__instance.kingdom.Name == "England")
                            {
                                AIOverhaulPlugin.LogMod($"[ENGLAND] BLOCKING diplomat: goldIncome too low ({goldIncome} <= 150)", LogCategory.Knights);
                            }
                            return false;
                        }

                        bool wants = WarLogicHelper.WantsDiplomat(__instance.kingdom);
                        if (!wants)
                        {
                            if (__instance.kingdom.Name == "England")
                            {
                                AIOverhaulPlugin.LogMod($"[ENGLAND] BLOCKING diplomat: WantsDiplomat returned false", LogCategory.Knights);
                            }
                            return false;
                        }

                        if (__instance.kingdom.Name == "England")
                        {
                            AIOverhaulPlugin.LogMod($"[ENGLAND] ALLOWING diplomat hire to proceed", LogCategory.Knights);
                        }
                    }
                    
                    // Logic for Spy:
                    if (cDef.id == CharacterClassNames.Spy)
                    {
                        float goldIncome = __instance.kingdom.income.Get(ResourceType.Gold);
                        if (goldIncome <= GameBalance.MinGoldIncomeForSpies)
                        {
                            return false;
                        }

                        if (!WarLogicHelper.WantsSpy(__instance.kingdom))
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogMod($"Error in CharacterHiringPatch: {ex}", LogCategory.General);
            }

            return true;
        }
    }
}
