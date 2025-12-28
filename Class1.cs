using BepInEx;
using HarmonyLib;
using Logic;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AIOverhaul
{
    [BepInPlugin("com.poyer.koh2.aioverhaul", "AI Overhaul", "1.6.2")]
    public class AIOverhaulPlugin : BaseUnityPlugin
    {
        public static AIOverhaulPlugin Instance;
        void Awake()
        {
            Instance = this;
            var harmony = new Harmony("com.poyer.koh2.aioverhaul");
            harmony.PatchAll();
            Logger.LogInfo("AI Overhaul 1.6.2 (Final Type Fix) Loaded!");
        }

        public void Log(string message)
        {
            Logger.LogInfo(message);
        }

        public static bool IsEnhancedAI(Logic.Kingdom k)
        {
            if (k == null) return false;
            string name = k.Name.ToLower();
            // User requested: Castille, Britany, Lorraine, Scottland, Flanders
            // We use a simple contains check on the lowercase name to be safe
            return name.Contains("castille") || name.Contains("castile") || 
                   name.Contains("britany") || name.Contains("brittany") || 
                   name.Contains("lorraine") || 
                   name.Contains("scottland") || name.Contains("scotland") || 
                   name.Contains("flanders");
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
            if (army == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            
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
            if (army == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
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
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
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
        static void Prefix(KingdomAI __instance, KingdomAI.Expense.Type type, BaseObject defParam, ref KingdomAI.Expense.Priority priority)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
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
            if (def == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.GetKingdom())) return;
            
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
            if (__instance.governor == null || __instance.castle == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.castle.GetKingdom())) return;
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
        // ChooseBuildOption is STATIC: static BuildOption ChooseBuildOption(Game game, List<BuildOption> options, float sum)
        static bool Prefix(ref Castle.BuildOption __result, List<Castle.BuildOption> options)
        {
            if (options == null || options.Count == 0) return true;
            
            // Get kingdom from first option's castle
            var firstCastle = options[0].castle;
            if (firstCastle == null) return true;
            var kingdom = firstCastle.GetKingdom();
            if (kingdom == null) return true;

            if (!AIOverhaulPlugin.IsEnhancedAI(kingdom)) return true;

            __result = options[0]; // High priority building
            return false;
        }
    }

    // --- WAR DECLARATION LOGIC ---
    public static class WarLogicHelper
    {
        public static float GetTotalPower(Logic.Kingdom k)
        {
            if (k == null) return 0f;
            float total = 0f;

            // Iterate through all realms to find armies
            if (k.realms != null)
            {
                foreach (var realm in k.realms)
                {
                    if (realm.armies != null)
                    {
                        foreach (var army in realm.armies)
                        {
                            if (army == null) continue;
                            float armyPower = (float)Traverse.Create(army).Method("EvalStrength").GetValue<int>();
                            total += armyPower;
                        }
                    }
                }
            }

            // Kingdoms also have garrisons in their castles
            if (k.realms != null)
            {
                foreach (var realm in k.realms)
                {
                    if (realm.castle != null)
                    {
                        float castlePower = (float)Traverse.Create(k.ai).Method("EvalCastleStrength", realm.castle).GetValue<int>();
                        total += castlePower;
                    }
                }
            }
            return total;
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

            float powerRatio = targetPower > 0 ? ownPower / targetPower : (ownPower > 0 ? 10f : 1f);
            AIOverhaulPlugin.Instance.Log($"[AI-Mod] AI {__instance.kingdom.Name} declaring war on {k.Name}. Power Ratio: {powerRatio:F2}");
            return true;
        }
    }

    // --- PERFORMANCE LOGGING ---
    public static class AILogger
    {
        private static string LogPath = System.IO.Path.Combine(Paths.BepInExRootPath, "AI_Performance_Log.csv");
        private static float lastLogTime = -100f;
        private static string[] TrackedKingdoms = {
            "Castille", "Castile", "Britany", "Brittany", "Lorraine", "Scottland", "Scotland", "Flanders", // Enhanced
            "England", "France", "Navarra", "Toulouse", "Leon" // Vanilla
        };

        public static void LogState(Game game)
        {
            if (game == null) return;
            float currentTime = (float)game.time.seconds;

            // Log every 60 seconds of game time (approx 1 minute)
            if (currentTime < lastLogTime + 60f) return;
            lastLogTime = currentTime;

            bool fileExists = System.IO.File.Exists(LogPath);
            using (StreamWriter sw = new StreamWriter(LogPath, true))
            {
                if (!fileExists)
                {
                    sw.WriteLine("Time,Kingdom,IsEnhanced,Gold,Income_Gold,Books,Income_Books,Piety,Income_Piety,Trade,Income_Trade,Levies,Income_Levies,Armies,Realms,Buildings");
                }

                foreach (var k in game.kingdoms)
                {
                    if (k == null || k.IsDefeated()) continue;
                    string name = k.Name;
                    if (!TrackedKingdoms.Any(t => name.IndexOf(t, System.StringComparison.OrdinalIgnoreCase) >= 0)) continue;

                    bool isEnhanced = AIOverhaulPlugin.IsEnhancedAI(k);
                    
                    // Resources & Incomes
                    float gold = k.resources.Get(ResourceType.Gold);
                    float incomeGold = k.income.Get(ResourceType.Gold);
                    float books = k.resources.Get(ResourceType.Books);
                    float incomeBooks = k.income.Get(ResourceType.Books);
                    float piety = k.resources.Get(ResourceType.Piety);
                    float incomePiety = k.income.Get(ResourceType.Piety);
                    float trade = k.resources.Get(ResourceType.Trade);
                    float incomeTrade = k.income.Get(ResourceType.Trade);
                    float levies = k.resources.Get(ResourceType.Levy);
                    float incomeLevies = k.income.Get(ResourceType.Levy);

                    // Army & Realm count
                    int armyCount = 0;
                    int buildingCount = 0;
                    int realmCount = k.realms != null ? k.realms.Count : 0;

                    if (k.realms != null)
                    {
                        foreach (var realm in k.realms)
                        {
                            if (realm.armies != null) armyCount += realm.armies.Count;
                            if (realm.castle != null)
                            {
                                // Safe access to buildings - using AddBuildOptions as a hint that building data exists
                                var buildings = Traverse.Create(realm.castle).Field("buildings").GetValue();
                                if (buildings is System.Collections.IEnumerable enumerable)
                                {
                                    foreach (var b in enumerable) buildingCount++;
                                }
                            }
                        }
                    }

                    sw.WriteLine($"{currentTime:F0},{name},{isEnhanced},{gold:F0},{incomeGold:F1},{books:F0},{incomeBooks:F1},{piety:F0},{incomePiety:F1},{trade:F0},{incomeTrade:F1},{levies:F0},{incomeLevies:F1},{armyCount},{realmCount},{buildingCount}");
                }
            }
            AIOverhaulPlugin.Instance.Log($"[AI-Mod] Performance state logged at game time: {currentTime:F0}");
        }
    }

    [HarmonyPatch(typeof(KingdomAI), "ThinkGeneral")]
    public class LoggingPatch
    {
        static void Prefix(KingdomAI __instance)
        {
            // Only have one instance handle the logging for the whole game
            if (__instance.kingdom.id == 1) 
            {
                AILogger.LogState(__instance.game);
            }
            // Fallback: if kingdom 1 is defeated or doesn't exist, we'd need another way.
            // But usually kingdom 1 (often Bulgaria or similar in some maps) is there.
            // Better: use the first non-defeated kingdom.
            else if (__instance.game.kingdoms.Count > 0 && __instance.game.kingdoms[0] == __instance.kingdom)
            {
                 // This ensures the first valid AI runs it.
                 AILogger.LogState(__instance.game);
            }
        }
    }
}
