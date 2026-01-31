using System;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// Patch for KingdomAI.ThinkPlunder to prevent AI armies from attacking Keep settlements.
    /// Keeps are military fortifications that should not be targeted for plundering.
    /// </summary>
    [HarmonyPatch(typeof(KingdomAI), "ThinkPlunder")]
    public class KingdomAI_ThinkPlunder
    {
        static bool Prefix(KingdomAI __instance, Logic.Army army, ref bool __result)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            Logic.Settlement target = null;
            float minDist = float.MaxValue;

            foreach (var settlement in army.realm_in.settlements)
            {
                // Skip inactive, razed, in-battle, or friendly settlements. Copied from vanilla code.
                if (!settlement.IsActiveSettlement()) continue;
                if (settlement is Castle) continue;
                if (settlement.razed) continue;
                if (settlement.battle != null) continue;
                if (!settlement.IsEnemy(army)) continue;

                // NEW: Skip Keep settlements (military fortifications)
                if (settlement.def?.id == SettlementNames.Keep) continue;

                float dist = settlement.position.SqrDist(army.position);
                if (dist < minDist)
                {
                    target = settlement;
                    minDist = dist;
                }
            }

            if (target == null)
            {
                // No settlements to plunder - attack the castle if in enemy territory
                var castle = army.realm_in?.castle;
                var castleKingdom = castle?.GetKingdom();
                if (castle != null && castle.battle == null && castleKingdom != null && castleKingdom.IsEnemy(__instance.kingdom.id))
                {
                    TraverseAPI.SendArmy(__instance, army, castle, AIStatusNames.AttackRealm);
                    __result = true;
                    return false;
                }

                __result = false;
                return false;
            }

            TraverseAPI.SendArmy(__instance, army, target, AIStatusNames.Plunder);
            __result = true;
            return false;
        }
    }
}
