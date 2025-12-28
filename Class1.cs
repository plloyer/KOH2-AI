using BepInEx;
using HarmonyLib;
using Logic;
using UnityEngine;
using System.Collections.Generic;

namespace AIOverhaul
{
    [BepInPlugin("com.poyer.koh2.aioverhaul", "AI Overhaul", "1.5.0")]
    public class AIOverhaulPlugin : BaseUnityPlugin
    {
        public static AIOverhaulPlugin Instance;
        void Awake()
        {
            Instance = this;
            var harmony = new Harmony("com.poyer.koh2.aioverhaul");
            harmony.PatchAll();
            Logger.LogInfo("AI Overhaul 1.5.0 (Strategic War Declaration) Loaded!");
        }

        public void Log(string message)
        {
            Logger.LogInfo(message);
        }
    }

    public static class BuddySystem
    {
        public static Army GetBuddy(Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return null;
            var armies = Traverse.Create(kingdom).Field<List<Army>>("armies").Value;
            if (armies == null) return null;
            int index = armies.IndexOf(army);
            if (index == -1) return null;
            int buddyIndex = (index % 2 == 0) ? index + 1 : index - 1;
            if (buddyIndex >= 0 && buddyIndex < armies.Count) return armies[buddyIndex];
            return null;
        }

        public static bool IsFollower(Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return false;
            var armies = Traverse.Create(kingdom).Field<List<Army>>("armies").Value;
            if (armies == null) return false;
            int index = armies.IndexOf(army);
            return index != -1 && index % 2 == 1;
        }
    }

    // --- BATTLE ENGAGEMENT & COORDINATION ---
    [HarmonyPatch(typeof(KingdomAI), "ThinkFight")]
    public class BattleEngagementPatch
    {
        static bool Prefix(KingdomAI __instance, Army army, ref bool __result)
        {
            if (army == null) return true;
            
            var armyTraverse = Traverse.Create(army);
            var realmIn = armyTraverse.Field<Realm>("realm_in").Value;
            if (realmIn == null) return true;

            var realmTraverse = Traverse.Create(realmIn);
            float ownStrength = 0;
            float friendStrength = 0;
            float enemyStrength = 0;

            var armiesInRealm = realmTraverse.Field<List<Army>>("armies").Value;
            if (armiesInRealm != null)
            {
                foreach (var a in armiesInRealm)
                {
                    var aTraverse = Traverse.Create(a);
                    int aKingdomId = aTraverse.Field<int>("kingdom_id").Value;
                    int aStrength = aTraverse.Method("EvalStrength").GetValue<int>();
                    if (aKingdomId == __instance.kingdom.id)
                    {
                        if (a.Equals(army)) ownStrength += aStrength;
                        else friendStrength += aStrength;
                    }
                    else if (__instance.kingdom.IsEnemy(aKingdomId))
                        enemyStrength += aStrength;
                }
            }

            Army buddy = BuddySystem.GetBuddy(army, __instance.kingdom);
            bool buddyPresent = false;
            if (buddy != null)
            {
                var buddyRealm = Traverse.Create(buddy).Field<Realm>("realm_in").Value;
                if (buddyRealm == realmIn) buddyPresent = true;
            }

            if (ownStrength + friendStrength + enemyStrength > 0)
            {
                float totalFriendly = ownStrength + friendStrength;
                float winChance = totalFriendly / (totalFriendly + enemyStrength);

                // --- Buddy Coordination Logic ---
                if (buddy != null && !buddyPresent && enemyStrength > 0)
                {
                    float soloWinChance = ownStrength / (ownStrength + enemyStrength);
                    // If we need the buddy to hit 45% or higher, wait for them
                    if (soloWinChance < 0.45f && winChance >= 0.45f)
                    {
                        AIOverhaulPlugin.Instance.Log($"[AI-Mod] Waiting for buddy before attacking. Combined chance: {winChance:P0}");
                        army.Stop();
                        armyTraverse.Method("SetAIStatus", new object[] { "wait_for_buddy" }).GetValue();
                        __result = true;
                        return false;
                    }
                }

                if (winChance < 0.45f)
                {
                    // If we are in our own realm, retreat to castle if possible
                    int realmKingdomId = realmTraverse.Field<int>("kingdom_id").Value;
                    if (realmKingdomId == __instance.kingdom.id && armyTraverse.Field<Castle>("castle").Value == null)
                    {
                        Castle castle = realmTraverse.Field<Castle>("castle").Value;
                        if (castle != null)
                        {
                            var castleArmy = Traverse.Create(castle).Field<Army>("army").Value;
                            if (castleArmy == null || castleArmy.Equals(army))
                            {
                                AIOverhaulPlugin.Instance.Log($"[AI-Mod] Low win chance ({winChance:P0}) - Retreating army to {(castle != null ? castle.ToString() : "castle")}");
                                Traverse.Create(__instance).Method("Send", new object[] { army, castle, "retreat_low_chance", null }).GetValue();
                                __result = true;
                                return false;
                            }
                        }
                    }

                    // Block the attack/reinforce logic in ThinkFight
                    AIOverhaulPlugin.Instance.Log($"[AI-Mod] Skipping battle: Win chance {winChance:P0} < 45%");
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }

    // --- IDLE BEHAVIOR & BUDDY MOVEMENT ---
    [HarmonyPatch(typeof(KingdomAI), "ThinkArmy")]
    public class IdleArmyPatch
    {
        static void Postfix(KingdomAI __instance, Army army)
        {
            if (army == null) return;
            var armyTraverse = Traverse.Create(army);
            
            if (armyTraverse.Method("IsHiredMercenary").GetValue<bool>() || armyTraverse.Field<object>("battle").Value != null) return;

            string status = armyTraverse.Field<string>("ai_status").Value;

            // --- Buddy Movement Logic (Follower) ---
            if (BuddySystem.IsFollower(army, __instance.kingdom) && (status == "idle" || status == "wait_orders"))
            {
                Army leader = BuddySystem.GetBuddy(army, __instance.kingdom);
                if (leader != null)
                {
                    var leaderTraverse = Traverse.Create(leader);
                    var leaderTarget = leaderTraverse.Method("GetTarget").GetValue<MapObject>();
                    if (leaderTarget != null && !leaderTarget.Equals(armyTraverse.Method("GetTarget").GetValue<MapObject>()))
                    {
                        AIOverhaulPlugin.Instance.Log($"[AI-Mod] Follower following leader to {(leaderTarget != null ? leaderTarget.ToString() : "target")}");
                        Traverse.Create(__instance).Method("Send", new object[] { army, leaderTarget, "follow_buddy", null }).GetValue();
                        return;
                    }
                }
            }

            // Normal Idle Logic
            if (status == "idle" && armyTraverse.Field<Castle>("castle").Value == null)
            {
                Castle nearest = (Castle)Traverse.Create(__instance).Method("FindNearestOwnCastle", new object[] { army, true }).GetValue();
                if (nearest != null)
                {
                    AIOverhaulPlugin.Instance.Log($"[AI-Mod] Idle Knight - Returning to garrison at {(nearest != null ? nearest.ToString() : "castle")}");
                    Traverse.Create(__instance).Method("Send", new object[] { army, nearest, "go_inside", null }).GetValue();
                }
            }
        }
    }

    // --- MERCHANT & HIRING LOGIC ---
    [HarmonyPatch(typeof(KingdomAI), "ConsiderHireMerchant")]
    public class MerchantHiringPatch
    {
        static bool Prefix(KingdomAI __instance)
        {
            int currentMerchants = Traverse.Create(__instance)
                .Method("CountCourtSlots", new object[] { __instance.game.ai.merchant_def })
                .GetValue<int>();
            
            if (currentMerchants < 2)
            {
                AIOverhaulPlugin.Instance.Log($"[AI-Mod] Priority Merchant Hiring for {__instance.kingdom.Name}");
                Traverse.Create(__instance).Method("ConsiderExpense", new object[] { 
                    KingdomAI.Expense.Type.HireChacacter, 
                    (BaseObject)__instance.game.ai.merchant_def, 
                    null, 
                    __instance.game.ai.merchant_def.ai_category, 
                    KingdomAI.Expense.Priority.Urgent,
                    null
                }).GetValue();
                return false; 
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(KingdomAI), "ConsiderExpense", new[] { typeof(KingdomAI.Expense.Type), typeof(BaseObject), typeof(Logic.Object), typeof(KingdomAI.Expense.Category), typeof(KingdomAI.Expense.Priority), typeof(List<Value>) })]
    public class TradeActionPriorityPatch
    {
        static void Prefix(KingdomAI.Expense.Type type, BaseObject defParam, ref KingdomAI.Expense.Priority priority)
        {
            if (type == KingdomAI.Expense.Type.ExecuteAction)
            {
                if (defParam is Action action && action.def.id == "TradeWithKingdomAction")
                {
                    priority = KingdomAI.Expense.Priority.Urgent;
                }
            }
        }
    }

    // --- BUILDING EVALUATION (BUILD ORDER) ---
    [HarmonyPatch(typeof(Castle), "EvalBuild")]
    public class BuildingPrioritizationPatch
    {
        static void Postfix(ref float __result, Castle __instance, Building.Def def)
        {
            if (def == null) return;
            
            var kingdom = __instance.GetKingdom();
            var realm = __instance.GetRealm();
            if (kingdom == null || realm == null) return;

            // --- STRATEGIC BUILD ORDER FOR SMALL COUNTRIES ---
            if (kingdom.realms.Count <= 4)
            {
                if (def.id == "Barracks")
                {
                    int maxKeeps = 0;
                    foreach (var r in kingdom.realms)
                    {
                        int kCount = 0;
                        foreach (var s in r.settlements) if (s.def.id == "Keep") kCount++;
                        if (kCount > maxKeeps) maxKeeps = kCount;
                    }
                    int myKeeps = 0;
                    foreach (var s in realm.settlements) if (s.def.id == "Keep") myKeeps++;
                    if (myKeeps >= maxKeeps && myKeeps > 0) __result += 50000f;
                }
                if (def.id == "VillageMilitia")
                {
                    int villageCount = 0;
                    foreach (var s in realm.settlements) if (s.def.id == "Village") villageCount++;
                    if (villageCount >= 2) __result += 40000f;
                }
                if (def.id == "Fletcher_Barracks" || def.id == "TrainingGrounds" || def.id == "Swordsmith")
                {
                    __result += 60000f;
                }
            }

            if (def.ai_category == KingdomAI.Expense.Category.Economy) __result *= 1.5f;
            if (realm.ai_specialization == AI.ProvinceSpecialization.MilitarySpec && def.ai_category == KingdomAI.Expense.Category.Military)
                __result *= 2.0f;
            if (__instance.CalcPriority(def) == KingdomAI.Expense.Priority.Urgent) __result += 10000f;
        }
    }

    // --- GOVERNOR ASSIGNMENT & SELECTION ---
    [HarmonyPatch(typeof(KingdomAI.GovernOption), "Eval")]
    public class KnightAssignmentPatch
    {
        static void Postfix(ref float __result, KingdomAI.GovernOption __instance)
        {
            if (__instance.governor == null || __instance.castle == null) return;
            var realm = __instance.castle.GetRealm();
            if (realm == null) return;
            bool isMatch = false;
            var spec = realm.ai_specialization;
            var character = __instance.governor;
            if (spec == AI.ProvinceSpecialization.MilitarySpec && character.IsMarshal()) isMatch = true;
            if (spec == AI.ProvinceSpecialization.TradeSpec && character.IsMerchant()) isMatch = true;
            if (spec == AI.ProvinceSpecialization.ReligionSpec && character.IsCleric()) isMatch = true;
            if (isMatch) { __result *= 10.0f; __result += 5000f; }
        }
    }

    [HarmonyPatch(typeof(Castle), "ChooseBuildOption")]
    public class SelectionPatch
    {
        static bool Prefix(ref Castle.BuildOption __result, List<Castle.BuildOption> options)
        {
            if (options == null || options.Count == 0) return true;
            Castle.BuildOption bestOption = options[0];
            float maxEval = -1f;
            foreach (var opt in options) { if (opt.eval > maxEval) { maxEval = opt.eval; bestOption = opt; } }
            __result = bestOption;
            return false;
        }
    }

    // --- WAR DECLARATION LOGIC ---
    public static class WarLogicHelper
    {
        public static float GetTotalPower(Logic.Kingdom k)
        {
            if (k == null) return 0;
            float power = 0;
            var armies = Traverse.Create(k).Field<List<Army>>("armies").Value;
            if (armies != null)
            {
                foreach (var army in armies)
                {
                    power += Traverse.Create(army).Method("EvalStrength").GetValue<int>();
                }
            }
            foreach (var realm in k.realms)
            {
                if (realm.castle != null)
                {
                    power += KingdomAI.Threat.EvalCastleStrength(realm.castle);
                }
            }
            return power;
        }

        public static bool IsStrategicNeighbor(Logic.Kingdom a, Logic.Kingdom b)
        {
            if (a == null || b == null) return false;
            if (a.id == b.id) return false;
            foreach (var realm in a.realms)
            {
                if (realm == null) continue;
                if (realm.logicNeighborsAll != null)
                {
                    foreach (var n in realm.logicNeighborsAll)
                    {
                        if (n != null && n.kingdom_id == b.id) return true;
                    }
                }
                if (realm.neighbors != null)
                {
                    foreach (var n in realm.neighbors)
                    {
                        if (n != null && n.IsSeaRealm())
                        {
                            foreach (var sn in n.neighbors)
                            {
                                if (sn != null && sn.kingdom_id == b.id) return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static bool HasCommonEnemyWithAlly(Logic.Kingdom actor, Logic.Kingdom target)
        {
            var kingdoms = actor.game.kingdoms;
            foreach (var k in kingdoms)
            {
                if (k == null || k == actor || k == target) continue;
                if (actor.IsAlly(k))
                {
                    if (k.IsEnemy(target)) return true;
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(KingdomAI), "ThinkDeclareWar")]
    public class WarDeclarationPatch
    {
        static bool Prefix(KingdomAI __instance, Logic.Kingdom k, ref bool __result)
        {
            if (k == null || __instance.kingdom == null) return true;

            // 1. Maintain Alliances
            if (__instance.kingdom.IsAlly(k))
            {
                AIOverhaulPlugin.Instance.Log($"[AI-Mod] Blocking war declaration on ally: {k.Name}");
                __result = false;
                return false;
            }

            // 2. Proximity: Only neighbors (land or sea)
            if (!WarLogicHelper.IsStrategicNeighbor(__instance.kingdom, k))
            {
                AIOverhaulPlugin.Instance.Log($"[AI-Mod] Blocking war declaration on non-neighbor: {k.Name}");
                __result = false;
                return false;
            }

            // 3. Strength & Opportunity
            float ownPower = WarLogicHelper.GetTotalPower(__instance.kingdom);
            float targetPower = WarLogicHelper.GetTotalPower(k);
            
            bool targetAtWar = k.wars != null && k.wars.Count > 0;
            bool commonEnemy = WarLogicHelper.HasCommonEnemyWithAlly(__instance.kingdom, k);

            if (targetPower > ownPower * 1.5f)
            {
                if (!targetAtWar && !commonEnemy)
                {
                    AIOverhaulPlugin.Instance.Log($"[AI-Mod] Blocking war on {k.Name} - Target too strong ({targetPower:F0} vs {ownPower:F0}) and not distracted.");
                    __result = false;
                    return false;
                }
                AIOverhaulPlugin.Instance.Log($"[AI-Mod] Proceeding with war on stronger target {k.Name} due to opportunity (AtWar: {targetAtWar}, CommonEnemy: {commonEnemy})");
            }

            AIOverhaulPlugin.Instance.Log($"[AI-Mod] AI {__instance.kingdom.Name} declaring war on {k.Name}. Power Ratio: {ownPower/targetPower:F2}");
            return true;
        }
    }
}
