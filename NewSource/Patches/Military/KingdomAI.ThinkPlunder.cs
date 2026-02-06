// using System;
// using HarmonyLib;
// using Logic;
//
// namespace AIOverhaul
// {
//     // See ARMY_MANAGEMENT_GUIDE.md
//     /// <summary>
//     /// Called by ThinkFight.
//     /// Patch for KingdomAI.ThinkPlunder to prevent AI armies from attacking Keep settlements.
//     /// Keeps are military fortifications that should not be targeted for plundering.
//     /// </summary>
//     [HarmonyPatch(typeof(KingdomAI), "ThinkPlunder")]
//     public class KingdomAI_ThinkPlunder
//     {
//         static bool Prefix(KingdomAI __instance, Logic.Army army, ref bool __result)
//         {
//             if (__instance == null || __instance.kingdom == null) return true;
//             if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
//
//             // Priority 1: Find any enemy realm in disorder within 2 provinces of our territory
//             var disorderRealm = MilitaryHelper.FindNearbyEnemyRealmInDisorder(__instance.kingdom, GameBalance.DisorderAttackMaxDistance);
//             if (disorderRealm != null)
//             {
//                 var castle = disorderRealm.castle;
//                 TraverseAPI.SendArmy(__instance, army, castle, AIStatusNames.AttackRealm);
//                 __result = true;
//                 return false;
//             }
//             
//             Logic.Settlement target = FindNearestSettlementInRealm(army);
//             if (target == null)
//             {
//                 // No settlements to plunder - attack the castle if in enemy territory
//                 var castle = army.realm_in?.castle;
//                 var castleKingdom = castle?.GetKingdom();
//                 if (castle != null && castle.battle == null && castleKingdom != null && castleKingdom.IsEnemy(__instance.kingdom.id))
//                 {
//                     TraverseAPI.SendArmy(__instance, army, castle, AIStatusNames.AttackRealm);
//                     __result = true;
//                     return false;
//                 }
//
//                 __result = false;
//                 return false;
//             }
//
//             TraverseAPI.SendArmy(__instance, army, target, AIStatusNames.Plunder);
//             __result = true;
//             return false;
//         }
//
//         static Logic.Settlement FindNearestSettlementInRealm(Logic.Army army)
//         {
//             Logic.Settlement target = null;
//             float minDist = float.MaxValue;
//             foreach (var settlement in army.realm_in.settlements)
//             {
//                 // Skip inactive, razed, in-battle, or friendly settlements. Copied from vanilla code.
//                 if (!settlement.IsActiveSettlement()) continue;
//                 if (settlement is Castle) continue;
//                 if (settlement.razed) continue;
//                 if (settlement.battle != null) continue;
//                 if (!settlement.IsEnemy(army)) continue;
//
//                 // NEW: Skip Keep settlements (military fortifications)
//                 if (settlement.def?.id == SettlementNames.Keep) continue;
//
//                 float dist = settlement.position.SqrDist(army.position);
//                 if (dist < minDist)
//                 {
//                     target = settlement;
//                     minDist = dist;
//                 }
//             }
//
//             return target;
//         }
//     }
// }
