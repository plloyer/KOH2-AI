using BepInEx;
using HarmonyLib;
using Logic;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderHireMerchant")]
    public class MerchantHiringPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, ref bool __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            if (__instance.kingdom.gold > 500 && __instance.kingdom.GetKnightsInCourt() < 9)
            {
                int merchants = 0;
                foreach (var k in __instance.kingdom.court) if (k.type == Character.Type.Merchant) merchants++;
                if (merchants < 2)
                {
                    AIOverhaulPlugin.Instance.Log($"[AI-Mod] High priority Merchant hire for {__instance.kingdom.Name} (Gold: {__instance.kingdom.gold})");
                    Traverse.Create(__instance).Method("HireKnight", new object[] { Character.Type.Merchant }).GetValue();
                    __result = true;
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderExpense")]
    public class TradeActionPriorityPatch
    {
        static void Postfix(Logic.KingdomAI __instance, Logic.Budget.Category category, ref float __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (category == Logic.Budget.Category.Diplomacy)
            {
                foreach (var k in __instance.kingdom.court)
                {
                    if (k.type == Character.Type.Merchant && k.action != null && k.action.def.id == "TradeAction")
                    {
                        __result *= 1.5f;
                        return;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "EvalBuild")]
    public class BuildingPrioritizationPatch
    {
        static void Postfix(Logic.KingdomAI __instance, Logic.Castle castle, Logic.Building.Def def, ref float __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (def.id.Contains("Market") || def.id.Contains("Farm") || def.id.Contains("Merchant"))
            {
                __result *= 1.3f;
            }
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "GovernOption")]
    public class KnightAssignmentPatch
    {
        static void Postfix(Logic.KingdomAI __instance, Logic.Character knight, ref float __result)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (knight.type == Character.Type.Merchant && knight.realm_in != null)
            {
                if (knight.realm_in.castle != null && knight.realm_in.castle.GetBuildings().Any(b => b.def.id.Contains("Market")))
                {
                    __result += 20f;
                }
            }
        }
    }
}
