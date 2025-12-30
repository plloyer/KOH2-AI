using HarmonyLib;
using Logic;
using System.Collections.Generic;
using AIOverhaul.Helpers;

namespace AIOverhaul
{
    // "Eval" (GovernOption) scores how suitable a specific character is for governing a specific town.
    [HarmonyPatch(typeof(Logic.KingdomAI.GovernOption), "Eval")]
    public class EarlyGameGovernorPatch
    {
        static void Postfix(ref Logic.KingdomAI.GovernOption __instance, ref float __result)
        {
            Logic.Character governor = __instance.governor;
            Logic.Castle castle = __instance.castle;
            if (governor == null || castle == null) return;

            Logic.Kingdom kingdom = governor.GetKingdom();
            if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;

            // Rule: Early game (2-3 provinces), Marshals should govern the castle with most districts (military potential)
            if (kingdom.realms.Count >= 2 && kingdom.realms.Count <= 3 && governor.IsMarshal())
            {
                // Find the best military province in the kingdom
                Logic.Realm bestMilitaryRealm = null;
                float bestMilitaryEval = -1f;

                foreach (var realm in kingdom.realms)
                {
                    if (realm == null) continue;
                    float militaryEval = CalcMilitaryPotential(realm);
                    if (militaryEval > bestMilitaryEval)
                    {
                        bestMilitaryEval = militaryEval;
                        bestMilitaryRealm = realm;
                    }
                }

                if (bestMilitaryRealm != null && bestMilitaryRealm.castle == castle)
                {
                    // Give a massive boost to ensure Marshal chooses this castle
                    __result += 10000f;
                }
            }
        }

        private static float CalcMilitaryPotential(Logic.Realm realm)
        {
            if (realm == null) return 0f;
            float score = 0f;

            // 1. Base Military Evaluation from settlements
            if (realm.settlements != null)
            {
                foreach (var s in realm.settlements)
                {
                    if (s?.def != null)
                    {
                        score += s.def.ai_eval_military;
                    }
                }
            }

            // 2. Iron Ore Bonus (Same as vanilla specialization logic)
            if (realm.features != null && realm.features.Contains("IronOre"))
            {
                score += 15f;
            }

            // 3. Castle Slots (Districts)
            if (realm.castle != null)
            {
                // Unlocked slots represent "Castle Districts" development
                score += (float)realm.castle.AvailableBuildingSlots() * 2f; 
            }

            return score;
        }
    }
}
