using System;
using HarmonyLib;
using Logic;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AIOverhaul;

namespace AIOverhaul
{
    // Prevent building churches in settlements without religion districts
    // Prioritize provinces with the most religion district slots
    [HarmonyPatch(typeof(Logic.Castle), "AddBuildOptions", new Type[] { typeof(bool), typeof(Logic.Resource) })]
    public class ReligiousBuildingPatch
    {
        static void Postfix(Logic.Castle __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;

            // Get Religion district definition
            Logic.District.Def religionDistrict = __instance.game?.defs?.Get<Logic.District.Def>("Religion");
            if (religionDistrict == null) return;

            // Check if this castle has the Religion district
            bool hasReligionDistrict = __instance.HasDistrict(religionDistrict);

            // Find all religious buildings in build options
            for (int i = Logic.Castle.build_options.Count - 1; i >= 0; i--)
            {
                var option = Logic.Castle.build_options[i];
                if (option.def == null) continue;

                bool isReligiousBuilding = IsReligiousBuilding(option.def.id);

                if (isReligiousBuilding)
                {
                    if (!hasReligionDistrict)
                    {
                        // Block building if no religion district
                        Logic.Castle.build_options.RemoveAt(i);
                        AIOverhaulPlugin.LogMod($" Blocking {option.def.id} in {__instance.name} - no Religion district");
                    }
                    else
                    {
                        // Boost priority for castles with religion district
                        // Further boost based on how many religion slots available
                        int religionSlots = CountReligionSlots(__instance, religionDistrict);
                        float boost = 1.0f + (religionSlots * 0.2f); // 20% boost per slot
                        option.eval *= boost;
                        Logic.Castle.build_options[i] = option;
                    }
                }
            }

            // Do the same for upgrade options
            for (int i = Logic.Castle.upgrade_options.Count - 1; i >= 0; i--)
            {
                var option = Logic.Castle.upgrade_options[i];
                if (option.def == null) continue;

                bool isReligiousBuilding = IsReligiousBuilding(option.def.id);

                if (isReligiousBuilding)
                {
                    if (!hasReligionDistrict)
                    {
                        // Block building if no religion district
                        Logic.Castle.upgrade_options.RemoveAt(i);
                        AIOverhaulPlugin.LogMod($" Blocking {option.def.id} upgrade in {__instance.name} - no Religion district");
                    }
                    else
                    {
                        // Boost priority for castles with religion district
                        int religionSlots = CountReligionSlots(__instance, religionDistrict);
                        float boost = 1.0f + (religionSlots * 0.2f);
                        option.eval *= boost;
                        Logic.Castle.upgrade_options[i] = option;
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

        static int CountReligionSlots(Logic.Castle castle, Logic.District.Def religionDistrict)
        {
            if (religionDistrict?.buildings == null) return 0;

            // Count how many religion building slots exist in this district definition
            return religionDistrict.buildings.Count;
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderHireMerchant")]
    public class MerchantHiringPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            if (__instance.kingdom.resources[Logic.ResourceType.Gold] > 500 && __instance.kingdom.court.Count < Constants.MaxCourtSize)
            {
                int merchants = 0;
                foreach (var k in __instance.kingdom.court)
                    if (k != null && k.class_def?.id == CharacterClassNames.Merchant)
                        merchants++;
                
                if (merchants < 2)
                {
                    // USER OVERRIDE: Force 2 Merchants NO MATTER WHAT.
                    // Removed checks for Commerce and Busy status for the first 2 slots.
                    // This ensures the AI always has a baseline commercial capacity.
                    
                    AIOverhaulPlugin.LogMod($" FORCE Merchant hire for {__instance.kingdom.Name} (Merchants: {merchants}/2, Gold: {__instance.kingdom.resources[Logic.ResourceType.Gold]})");
                    Traverse.Create(__instance).Method("HireKnight", new object[] { CharacterClassNames.Merchant }).GetValue();
                    __result = true;
                    return false;
                }
                else
                {
                    // Strict Commerce Check for 3rd+ Merchant
                    // Vanilla uses Ceiling(Max/10), which allows hiring when close (e.g. 24 commerce -> 3 merchants).
                    // We strictly enforce 10 commerce per merchant (e.g. 24 commerce -> max 2 merchants).
                    float maxCommerce = Traverse.Create(__instance.kingdom).Method("GetMaxCommerce").GetValue<float>();
                    if ((merchants + 1) * 10f > maxCommerce)
                    {
                         return false; // Block hire
                    }
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderHireCleric")]
    public class ClericHiringPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Rule: Have at least 1 clergy when positive in Clergy opinion
            // (but only after the first two merchants and at least 50 gold income)
            
            var k = __instance.kingdom;
            float income = k.income[Logic.ResourceType.Gold];
            
            // Check prerequisites
            if (income < 50f) return true; // Let vanilla decide if poor
            
            
            // Count merchants
            int merchants = k.court.Count(c => c != null && c.IsMerchant());
            if (merchants < 2) return true;

            // Simplify: Remove Opinion logic for now to fix build (requires finding SocialGroup enum)
            // if (k.opinions.GetOpinion(Logic.Kingdom.SocialGroup.Clergy) <= 0) return true;

            // Check if we already have a cleric
            if (k.court.Any(c => c != null && c.IsCleric())) return true;

            // Simplify: Assume court size max is 9 (standard) or check court.Count
            if (k.court.Count < Constants.MaxCourtSize) 
            {
                 AIOverhaulPlugin.LogMod($" Priority Cleric hire for {k.Name}");
                 Traverse.Create(__instance).Method("HireKnight", new object[] { CharacterClassNames.Cleric }).GetValue();
                 __result = true;
                 return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderExpense", new System.Type[] { typeof(Logic.KingdomAI.Expense) })]
    public class SpyHiringBlockerPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.KingdomAI.Expense expense)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            // Check if this is a hiring expense for a Spy
            if (expense.type == Logic.KingdomAI.Expense.Type.HireChacacter) // Note: Game typo 'HireChacacter'
            {
                if (expense.defParam is Logic.CharacterClass.Def cDef && cDef.name == "Spy")
                {
                    // Rule: No spies until the AI has 500 gold income
                    float income = __instance.kingdom.income[Logic.ResourceType.Gold];
                    if (income < 500f)
                    {
                        return false; // Block this expense from being considered
                    }
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
            if (def.id.Contains(BuildingNames.MarketSquare) || def.id.Contains("Farm") || def.id.Contains(CharacterClassNames.Merchant))
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

            if (__instance.governor.class_def?.id == CharacterClassNames.Merchant)
            {
                if (__instance.castle.buildings.Any(b => b.def.id.Contains(BuildingNames.MarketSquare)))
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
            if (AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom))
            {
                var traditionOptions = __instance.kingdom.GetNewTraditionOptions();
                if (traditionOptions == null || traditionOptions.Count == 0) return true;

                if (__instance == null || __instance.kingdom == null) return true;
                
                // Safety checks for kingdom properties
                if (__instance.kingdom.traditions == null || __instance.kingdom.wars == null || __instance.kingdom.resources == null) return true;

                // TRADITION RUSH LOGIC
                bool rushingTradition = false;
                // Use resources.Get(ResourceType.Books) and wars.Count
                if (__instance.kingdom.traditions.Count == 0 && __instance.kingdom.wars.Count == 0 && __instance.kingdom.resources.Get(Logic.ResourceType.Books) >= 400f)
                {
                    rushingTradition = true;
                }

                // Look for Writing or Learning tradition
                var preferredTradition = traditionOptions.Find(t => t.id == TraditionNames.WritingTradition);
                if (preferredTradition == null)
                    preferredTradition = traditionOptions.Find(t => t.id == TraditionNames.LearningTradition);

                // If rushing, force pick even if we have to save gold (priority Urgent?)
                if (rushingTradition && preferredTradition != null)
                {
                    Logic.Resource cost = preferredTradition.GetAdoptCost(__instance.kingdom);
                    if (__instance.kingdom.resources.CanAfford(cost, 1f))
                    {
                        Traverse.Create(__instance).Method("ConsiderExpense",
                            Logic.KingdomAI.Expense.Type.AdoptTradition,
                            (Logic.BaseObject)preferredTradition,
                            (UnityEngine.Object)null,
                            Logic.KingdomAI.Expense.Category.Economy,
                            Logic.KingdomAI.Expense.Priority.Urgent, // URGENT for rush
                            null
                        ).GetValue();

                        __result = true;
                        return false; 
                    }
                    else
                    {
                        // Wait for money
                        return false; 
                    }
                }

                // Default behavior for other cases
                if (preferredTradition != null)
                {
                    if (__instance.kingdom.resources.CanAfford(preferredTradition.GetAdoptCost(__instance.kingdom)))
                    {
                        Traverse.Create(__instance).Method("ConsiderExpense",
                            Logic.KingdomAI.Expense.Type.AdoptTradition,
                            (Logic.BaseObject)preferredTradition,
                            (UnityEngine.Object)null,
                            Logic.KingdomAI.Expense.Category.Economy,
                            Logic.KingdomAI.Expense.Priority.High,
                            null 
                        ).GetValue();

                        __result = true;
                        return false; 
                    }
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderIncreaseCrownAuthority")]
    public static class SpendingPriorityPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Logic.KingdomAI __instance, ref bool __result)
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
                 float books = __instance.kingdom.resources.Get(Logic.ResourceType.Books);
                 float gold = __instance.kingdom.resources.Get(Logic.ResourceType.Gold);

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
                var skillsRef = Traverse.Create(__instance).Field("skills").GetValue<List<Logic.Skill>>();
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

    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderExpense")]
    [HarmonyPatch(new System.Type[] { typeof(Logic.KingdomAI.Expense.Type), typeof(Logic.BaseObject), typeof(Logic.Object), typeof(Logic.KingdomAI.Expense.Category), typeof(Logic.KingdomAI.Expense.Priority), typeof(List<Logic.Value>) })]
    public static class DiplomatHiringPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.BaseObject defParam, Logic.KingdomAI.Expense.Type type)
        {
            try
            {
                if (__instance.kingdom == null || __instance.kingdom.is_player || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom))
                    return true;

                if (type == Logic.KingdomAI.Expense.Type.HireChacacter && defParam is Logic.CharacterClass.Def cDef)
                {
                    if (cDef.id == "Diplomat")
                    {
                        // Check Income > 150
                        float goldIncome = __instance.kingdom.income.Get(Logic.ResourceType.Gold);
                        if (goldIncome <= 150f)
                        {
                            return false; 
                        }

                        // Check Threat Level
                        var threatsField = typeof(Logic.KingdomAI).GetField("threats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        if (threatsField != null)
                        {
                            var threats = threatsField.GetValue(__instance) as System.Collections.IList;
                            if (threats != null)
                            {
                                bool highThreat = false;
                                foreach (var t in threats)
                                {
                                    var levelField = t.GetType().GetField("level");
                                    if (levelField != null)
                                    {
                                         var levelVal = (int)levelField.GetValue(t);
                                         if (levelVal >= 3) 
                                         {
                                             highThreat = true;
                                             break;
                                         }
                                    }
                                }
                                if (!highThreat) return false;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                AIOverhaulPlugin.LogMod($"Error in DiplomatHiringPatch: {ex}");
            }
            return true;
        }
    }
}
