using System;
using HarmonyLib;
using Logic;
using Object = Logic.Object;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(SpyPlot), "ValidateTarget")]
    public class SpyPlot_ValidateTarget
    {
        const float k_MinIncomeToSpy = 300f;
        
        static bool Prefix(SpyPlot __instance, Object target, ref bool __result)
        {
            try
            {
                // Access owner character
                Logic.Character spy = __instance.own_character;
                if (spy == null) return true;

                Logic.Kingdom kingdom = spy.GetKingdom();
                if (kingdom == null) return true;

                // 1. Only run for Enhanced AI
                if (!AIOverhaulPlugin.IsEnhancedAI(kingdom)) return true; // Vanilla AI runs original logic

                // 2. Income Check: Must have at least 150 gold income
                // Access 'Gold' resource from the 'income' Resource object
                float income = kingdom.income[ResourceType.Gold];

                if (income < k_MinIncomeToSpy)
                {
                    __result = false; // Invalid target
                    return false;     // Skip original method
                }

                return true;
            }
            catch (Exception e)
            {
                AIOverhaulPlugin.LogError($"Error in SpyPlot_ValidateTarget: {e}");
                return true;
            }
        }
    }
}
