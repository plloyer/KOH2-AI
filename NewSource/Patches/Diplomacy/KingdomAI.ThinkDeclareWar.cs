using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    // "ThinkDeclareWar" evaluates kingdom relations and power to decide if war should be declared.
    // Intent: WarDeclarationPatch
    [HarmonyPatch(typeof(KingdomAI), "ThinkDeclareWar")]
    public class KingdomAI_ThinkDeclareWar
    {
        const string k_LogPrefix = "[KingdomAI_ThinkDeclareWar] ";
        const int k_MinArmiesToDeclareWar = 2;
        const float k_MinPowerRatio = 1.2f;
        
        static bool Prefix(KingdomAI __instance, Logic.Kingdom k, ref bool __result)
        {
            if (k == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            if (__instance.kingdom.IsAlly(k)) return false;

            // Nemesis teammates never declare war on each other
            if (NemesisTeamManager.AreNemesisTeammates(__instance.kingdom, k))
            {
                NemesisTeamManager.LogVerbose($"Blocking war on nemesis teammate {k.Name}", __instance.kingdom);
                __result = false;
                return false;
            }

            __result = false;

            if (__instance.kingdom.wars != null && __instance.kingdom.wars.Count > 0)
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocked: Already at war.", LogCategory.Diplomacy, __instance.kingdom);
                return false;
            }

            // Nemesis: if a teammate is at war, don't start our own — SyncWars will join theirs
            if (NemesisTeamManager.IsNemesis(__instance.kingdom) && __instance.kingdom.wars.Count == 0)
            {
                War teammateWar = NemesisTeamManager.FindTeammateWar(__instance.kingdom, __instance.game);
                if (teammateWar != null)
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Nemesis: deferring to teammate war, SyncWars will join.", LogCategory.Diplomacy, __instance.kingdom);
                    return false;
                }
            }

            if (__instance.kingdom.HasDisorder())
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocked: Has disorder.", LogCategory.Diplomacy,  __instance.kingdom);
                return false;
            }

            float ownPower = __instance.kingdom.GetTotalPower();
            if (NemesisTeamManager.IsNemesis(__instance.kingdom))
                ownPower = NemesisTeamManager.GetTeamPower(__instance.game);
            float targetPower = k.GetTotalPower();
            float powerRatio = targetPower > 0 ? ownPower / targetPower : 10f;
            if (powerRatio < k_MinPowerRatio)
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Blocked: {k.Name} too strong. Us ({ownPower}) / Them ({targetPower}). Power Ratio {powerRatio}", LogCategory.Diplomacy,  __instance.kingdom);
                return false;
            }

            // War Preparation - Require MinArmiesForWar Full Armies
            int fullArmies = 0;
            if (__instance.kingdom.armies != null)
            {
                foreach (var army in __instance.kingdom.armies)
                {
                    if (army.leader != null && army.units.Count >= GameBalance.FullArmySize && army.battle == null)
                    {
                        // Check if fully healed/replenished
                        bool fullyReplenished = true;
                        foreach (var unit in army.units)
                        {
                            if (unit != null && unit.health < GameBalance.FullHealthThreshold)
                            {
                                fullyReplenished = false;
                                break;
                            }
                        }

                        if (fullyReplenished)
                            fullArmies++;
                    }
                }
            }

            if (fullArmies < k_MinArmiesToDeclareWar)
            {
                AIOverhaulPlugin.LogDebug($"[War] {__instance.kingdom.Name}: Not enough full armies to declare war. Has {fullArmies}, needs {GameBalance.MinArmiesToDeclareWar}.", LogCategory.Diplomacy, __instance.kingdom);
                return false;
            }

            // Directly send DeclareWar offer, bypassing vanilla CheckThreshold
            Offer cachedOffer = Offer.GetCachedOffer(DiplomacyConstants.DeclareWar, __instance.kingdom, k);
            cachedOffer.AI = true;
            string validation = cachedOffer.Validate();
            if (validation == DiplomacyConstants.ValidationOk)
            {
                cachedOffer.Send();
                __result = true;
                AIOverhaulPlugin.LogDebug($"Declaring war on {k.Name}. Power Ratio: {powerRatio:F2}", LogCategory.Diplomacy, __instance.kingdom);
            }
            else
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} War on {k.Name} blocked by game validation: {validation}", LogCategory.Diplomacy, __instance.kingdom);
            }
            return false;
        }
    }
}
