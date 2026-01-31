// using AIOverhaul.Constants;
// using AIOverhaul.Helpers;
// using HarmonyLib;
//
// namespace AIOverhaul
// {
//     // "ConsiderExpense" evaluates a specific expense (hiring, building, bribing) to decide if the AI should pay for it.
//     // Intent: ConsiderExpense
//     [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderExpense", typeof(Logic.KingdomAI.Expense))]
//     public class KingdomAI_ConsiderExpense
//     {
//         static bool Prefix(Logic.KingdomAI __instance, Logic.KingdomAI.Expense expense)
//         {
//             if (__instance.kingdom == null || expense == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom))
//                 return true;
//             
//             AIOverhaulPlugin.LogDebug($"Considering Expense: {expense.type} | Param: {expense.defParam} | Priority: {expense.priority} | Eval: {expense.eval:F2}", LogCategory.Economy, __instance.kingdom);
//
//             // MERCHANT HIRING LOGIC
//             var cDef = expense.defParam as Logic.CharacterClass.Def;
//             if (cDef?.id == CharacterClassNames.Merchant)
//             {
//                 int merchants = KingdomHelper.CountMerchants(__instance.kingdom);
//                 float maxCommerce = TraverseAPI.GetMaxCommerce(__instance.kingdom);
//                 int requiredCommerce = (merchants + 1) * GameBalance.CommercePerMerchant;
//                 
//                 // Allow first 2 merchants unconditionally
//                 if (merchants < GameBalance.RequiredMerchantCount)
//                 {
//                     AIOverhaulPlugin.LogDebug($"ALLOWING merchant #{merchants + 1} (first 2 are guaranteed)", LogCategory.Spending, __instance.kingdom);
//                     return true; // Allow vanilla to hire (ConsiderExpense just evaluates, actual hiring happens elsewhere)
//                 }
//
//                 // For 3rd+ merchant: strict commerce check
//                 if (requiredCommerce > maxCommerce)
//                     return false; // Block hire
//
//                 AIOverhaulPlugin.LogDebug($"ALLOWING merchant hire: {requiredCommerce} <= {maxCommerce} (Merchants: {merchants})", LogCategory.Spending, __instance.kingdom);
//                 return true; // Allow hiring (commerce check passed)
//             }
//             
//             // Gate: Require 2 Merchants before hiring any other class (Clerics, Spies, Diplomats, Marshals)
//             int currentMerchants = KingdomHelper.CountMerchants(__instance.kingdom);
//             if (currentMerchants < GameBalance.RequiredMerchantCount)
//             {
//                 // Allow 1st Marshal even if merchants are low
//                 if (cDef?.id == CharacterClassNames.Marshal)
//                 {
//                    int currentMarshals = KingdomHelper.CountCourtMembers(__instance.kingdom, CharacterClassNames.Marshal);
//                    if (currentMarshals == 0) 
//                    {
//                         AIOverhaulPlugin.LogDebug("ALLOWING first Marshal hire despite low merchant count", LogCategory.Spending, __instance.kingdom);
//                         return true;
//                    }
//                 }
//
//                 // Strict rule: No non-merchant characters (and no 2nd+ Marshal) until we have 2 merchants
//                 return false;
//             }
//             
//             // CLERIC HIRING LOGIC
//             if (cDef?.id == CharacterClassNames.Cleric)
//             {
//                 // Rule: Hire 1 cleric after 2 merchants and 50+ gold income
//                 float income = KingdomHelper.GetGoldIncome(__instance.kingdom);
//                 bool hasCleric = KingdomHelper.HasCleric(__instance.kingdom);
//                 
//                 if (hasCleric || income < GameBalance.MinGoldIncomeForClerics)
//                     return false;
//
//                 AIOverhaulPlugin.LogDebug($"ALLOWING cleric hire (Income: {income:F1} >= {GameBalance.MinGoldIncomeForClerics}, HasCleric: {hasCleric})", LogCategory.Spending, __instance.kingdom);
//                 return true;
//             }
//
//             // SPY HIRING LOGIC
//             if (cDef?.id == CharacterClassNames.Spy)
//             {
//                 float income = KingdomHelper.GetGoldIncome(__instance.kingdom);
//                 if (income < GameBalance.MinGoldIncomeForSpies)
//                     return false; // Block this expense from being considered
//
//                 if (!WarLogicHelper.WantsSpy(__instance.kingdom))
//                     return false;
//
//                 AIOverhaulPlugin.LogDebug($"ALLOWING spy hire (Income: {income:F1} >= {GameBalance.MinGoldIncomeForSpies}, WantsSpy: True)", LogCategory.Spending, __instance.kingdom);
//                 return true;
//             }
//
//             // DIPLOMAT HIRING LOGIC
//             if (cDef?.id == CharacterClassNames.Diplomat)
//             {
//                 bool wants = WarLogicHelper.WantsDiplomat(__instance.kingdom);
//                 if (!wants)
//                     return false;
//
//                 AIOverhaulPlugin.LogDebug("ALLOWING diplomat hire (WantsDiplomat: True)", LogCategory.Spending, __instance.kingdom);
//             }
//
//             return true;
//         }
//     }
// }

using System;