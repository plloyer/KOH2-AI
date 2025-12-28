using BepInEx;
using HarmonyLib;
using Logic;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AIOverhaul
{
    [BepInPlugin("com.poyer.koh2.aioverhaul", "AI Overhaul", "1.6.7")]
    public class AIOverhaulPlugin : BaseUnityPlugin
    {
        public static AIOverhaulPlugin Instance;
        void Awake()
        {
            Instance = this;
            var harmony = new Harmony("com.poyer.koh2.aioverhaul");
            harmony.PatchAll();
            Logger.LogInfo("AI Overhaul 1.6.7 (Gwynedd/Provence) Loaded!");
        }

        public void Log(string message)
        {
            Logger.LogInfo(message);
        }

        public static bool IsEnhancedAI(Logic.Kingdom k)
        {
            if (k == null) return false;
            string name = k.Name.ToLower();
            return name.Contains("castille") || name.Contains("castile") || 
                   name.Contains("britany") || name.Contains("brittany") || 
                   name.Contains("lorraine") || 
                   name.Contains("scottland") || name.Contains("scotland") || 
                   name.Contains("flanders") || name.Contains("gwynedd");
        }
    }

    public static class BuddySystem
    {
        public static List<Logic.Army> GetAllArmies(Logic.Kingdom kingdom)
        {
            // Logic.Kingdom.armies is public!
            return kingdom?.armies ?? new List<Logic.Army>();
        }

        public static bool IsFollower(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return false;
            var armies = GetAllArmies(kingdom);
            int index = armies.IndexOf(army);
            return index != -1 && index % 2 == 1;
        }

        public static Logic.Army GetBuddy(Logic.Army army, Logic.Kingdom kingdom)
        {
            if (army == null || kingdom == null) return null;
            var armies = GetAllArmies(kingdom);
            int index = armies.IndexOf(army);
            if (index == -1) return null;
            int buddyIndex = (index % 2 == 0) ? index + 1 : index - 1;
            if (buddyIndex >= 0 && buddyIndex < armies.Count) return armies[buddyIndex];
            return null;
        }
    }

    // --- BATTLE ENGAGEMENT & COORDINATION ---
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkFight")]
    public class BattleEngagementPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Army army, ref bool __result)
        {
            if (army == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            
            Logic.Realm realmIn = army.realm_in;
            if (realmIn == null) return true;

            float ownStrength = 0;
            float friendStrength = 0;
            float enemyStrength = 0;

            // Logic.Realm.armies is public!
            if (realmIn.armies != null)
            {
                foreach (var a in realmIn.armies)
                {
                    if (a == null) continue;
                    int aKingdomId = a.kingdom_id;
                    int aStrength = a.EvalStrength();
                    if (aKingdomId == __instance.kingdom.id)
                    {
                        if (a == army) ownStrength += aStrength;
                        else friendStrength += aStrength;
                    }
                    else if (__instance.kingdom.IsEnemy(aKingdomId))
                        enemyStrength += aStrength;
                }
            }

            Logic.Army buddy = BuddySystem.GetBuddy(army, __instance.kingdom);
            bool buddyPresent = false;
            if (buddy != null)
            {
                if (buddy.realm_in == realmIn) buddyPresent = true;
            }

            if (ownStrength + friendStrength + enemyStrength > 0)
            {
                float totalFriendly = ownStrength + friendStrength;
                float winChance = totalFriendly / (totalFriendly + enemyStrength);

                // --- Buddy Coordination Logic ---
                if (buddy != null && !buddyPresent && enemyStrength > 0)
                {
                    float soloWinChance = ownStrength / (ownStrength + enemyStrength);
                    if (soloWinChance < 0.45f && winChance >= 0.45f)
                    {
                        AIOverhaulPlugin.Instance.Log($"[AI-Mod] Waiting for buddy before attacking. Combined chance: {winChance:P0}");
                        army.Stop();
                        army.ai_status = "wait_for_buddy";
                        __result = true;
                        return false;
                    }
                }

                if (winChance < 0.45f)
                {
                    if (realmIn.kingdom_id == __instance.kingdom.id && army.castle == null)
                    {
                        Logic.Castle castle = realmIn.castle;
                        if (castle != null)
                        {
                            if (castle.army == null || castle.army == army)
                            {
                                AIOverhaulPlugin.Instance.Log($"[AI-Mod] Low win chance ({winChance:P0}) - Retreating army to {castle.name}");
                                // Logic.KingdomAI.Send is private, but we can use Traverse or just army.MoveTo
                                Traverse.Create(__instance).Method("Send", new object[] { army, castle, "retreat_low_chance", null }).GetValue();
                                __result = true;
                                return false;
                            }
                        }
                    }

                    AIOverhaulPlugin.Instance.Log($"[AI-Mod] Skipping battle: Win chance {winChance:P0} < 45%");
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }

    // --- IDLE BEHAVIOR & BUDDY MOVEMENT ---
    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkArmy")]
    public class IdleArmyPatch
    {
        static void Postfix(Logic.KingdomAI __instance, Logic.Army army)
        {
            if (army == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            
            if (army.IsHiredMercenary() || army.battle != null) return;

            string status = army.ai_status;

            // --- Buddy Movement Logic (Follower) ---
            if (BuddySystem.IsFollower(army, __instance.kingdom) && (status == "idle" || status == "wait_orders"))
            {
                Logic.Army leader = BuddySystem.GetBuddy(army, __instance.kingdom);
                if (leader != null)
                {
                    Logic.MapObject leaderTarget = leader.GetTarget();
                    if (leaderTarget != null && leaderTarget != army.GetTarget())
                    {
                        string targetName = (leaderTarget is Logic.Castle c) ? c.name : leaderTarget.ToString();
                        AIOverhaulPlugin.Instance.Log($"[AI-Mod] Follower following leader to {targetName}");
                        Traverse.Create(__instance).Method("Send", new object[] { army, leaderTarget, "follow_buddy", null }).GetValue();
                        return;
                    }
                }
            }

            // Normal Idle Logic
            if (status == "idle" && army.castle == null)
            {
                Logic.Castle nearest = (Logic.Castle)Traverse.Create(__instance).Method("FindNearestOwnCastle", new object[] { army, true }).GetValue();
                if (nearest != null)
                {
                    AIOverhaulPlugin.Instance.Log($"[AI-Mod] Idle Knight - Returning to garrison at {nearest.name}");
                    Traverse.Create(__instance).Method("Send", new object[] { army, nearest, "go_inside", null }).GetValue();
                }
            }
        }
    }

    // --- MERCHANT & HIRING LOGIC ---
    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderHireMerchant")]
    public class MerchantHiringPatch
    {
        static bool Prefix(Logic.KingdomAI __instance)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            int currentMerchants = Traverse.Create(__instance)
                .Method("CountCourtSlots", new object[] { __instance.game.ai.merchant_def })
                .GetValue<int>();
            
            if (currentMerchants < 2)
            {
                AIOverhaulPlugin.Instance.Log($"[AI-Mod] Priority Merchant Hiring for {__instance.kingdom.Name}");
                Traverse.Create(__instance).Method("ConsiderExpense", new object[] { 
                    Logic.KingdomAI.Expense.Type.HireChacacter, 
                    (Logic.BaseObject)__instance.game.ai.merchant_def, 
                    null, 
                    Logic.KingdomAI.Expense.Category.Economy, 
                    Logic.KingdomAI.Expense.Priority.Urgent,
                    null
                }).GetValue();
                return false; 
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ConsiderExpense", new[] { typeof(Logic.KingdomAI.Expense.Type), typeof(Logic.BaseObject), typeof(Logic.Object), typeof(Logic.KingdomAI.Expense.Category), typeof(Logic.KingdomAI.Expense.Priority), typeof(List<Logic.Value>) })]
    public class TradeActionPriorityPatch
    {
        static void Prefix(Logic.KingdomAI __instance, Logic.KingdomAI.Expense.Type type, Logic.BaseObject defParam, ref Logic.KingdomAI.Expense.Priority priority)
        {
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return;
            if (type == Logic.KingdomAI.Expense.Type.ExecuteAction)
            {
                if (defParam is Logic.Action action && action.def.id == "TradeWithKingdomAction")
                {
                    priority = Logic.KingdomAI.Expense.Priority.Urgent;
                }
            }
        }
    }

    // --- BUILDING EVALUATION (BUILD ORDER) ---
    [HarmonyPatch(typeof(Logic.Castle), "EvalBuild")]
    public class BuildingPrioritizationPatch
    {
        static void Postfix(ref float __result, Logic.Castle __instance, Logic.Building.Def def)
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
                        int kCount = r.settlements.Count(s => s.def.id == "Keep");
                        if (kCount > maxKeeps) maxKeeps = kCount;
                    }
                    int myKeeps = realm.settlements.Count(s => s.def.id == "Keep");
                    if (myKeeps >= maxKeeps && myKeeps > 0) __result += 50000f;
                }
                if (def.id == "VillageMilitia")
                {
                    int villageCount = realm.settlements.Count(s => s.def.id == "Village");
                    if (villageCount >= 2) __result += 40000f;
                }
                if (def.id == "Fletcher_Barracks" || def.id == "TrainingGrounds" || def.id == "Swordsmith")
                {
                    __result += 60000f;
                }
            }

            if (def.ai_category == Logic.KingdomAI.Expense.Category.Economy) __result *= 1.5f;
            if (realm.ai_specialization == Logic.AI.ProvinceSpecialization.MilitarySpec && def.ai_category == Logic.KingdomAI.Expense.Category.Military)
                __result *= 2.0f;
            if (__instance.CalcPriority(def) == Logic.KingdomAI.Expense.Priority.Urgent) __result += 10000f;
        }
    }

    // --- GOVERNOR ASSIGNMENT & SELECTION ---
    [HarmonyPatch(typeof(Logic.KingdomAI.GovernOption), "Eval")]
    public class KnightAssignmentPatch
    {
        static void Postfix(ref float __result, Logic.KingdomAI.GovernOption __instance)
        {
            if (__instance.governor == null || __instance.castle == null || !AIOverhaulPlugin.IsEnhancedAI(__instance.castle.GetKingdom())) return;
            var realm = __instance.castle.GetRealm();
            if (realm == null) return;
            bool isMatch = false;
            var spec = realm.ai_specialization;
            var character = __instance.governor;
            if (spec == Logic.AI.ProvinceSpecialization.MilitarySpec && character.IsMarshal()) isMatch = true;
            if (spec == Logic.AI.ProvinceSpecialization.TradeSpec && character.IsMerchant()) isMatch = true;
            if (spec == Logic.AI.ProvinceSpecialization.ReligionSpec && character.IsCleric()) isMatch = true;
            if (isMatch) { __result *= 10.0f; __result += 5000f; }
        }
    }

    [HarmonyPatch(typeof(Logic.Castle), "ChooseBuildOption")]
    public class SelectionPatch
    {
        // static BuildOption ChooseBuildOption(Game game, List<BuildOption> options, float sum)
        static bool Prefix(Logic.Game game, List<Logic.Castle.BuildOption> options, float sum, ref Logic.Castle.BuildOption __result)
        {
            if (options == null || options.Count == 0) return true;
            
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

            if (k.realms != null)
            {
                foreach (var realm in k.realms)
                {
                    if (realm.armies != null)
                    {
                        foreach (var army in realm.armies)
                        {
                            if (army == null) continue;
                            total += army.EvalStrength();
                        }
                    }
                    if (realm.castle != null && k.ai != null)
                    {
                        total += Logic.KingdomAI.Threat.EvalCastleStrength(realm.castle);
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

    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkDeclareWar")]
    public class WarDeclarationPatch
    {
        static bool Prefix(Logic.KingdomAI __instance, Logic.Kingdom k, ref bool __result)
        {
            if (k == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;

            if (__instance.kingdom.IsAlly(k))
            {
                AIOverhaulPlugin.Instance.Log($"[AI-Mod] Blocking war declaration on ally: {k.Name}");
                __result = false;
                return false;
            }

            if (!WarLogicHelper.IsStrategicNeighbor(__instance.kingdom, k))
            {
                AIOverhaulPlugin.Instance.Log($"[AI-Mod] Blocking war declaration on non-neighbor: {k.Name}");
                __result = false;
                return false;
            }

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
            "Castille", "Castile", "Britany", "Brittany", "Lorraine", "Scottland", "Scotland", "Flanders", "Gwynedd", // Enhanced
            "England", "France", "Navarra", "Toulouse", "Leon", "Provence" // Vanilla
        };

        public static void LogState(Logic.Game game)
        {
            if (game == null) return;
            float currentTime = (float)game.time.seconds;

            if (currentTime < lastLogTime + 60f) return;
            lastLogTime = currentTime;

            bool fileExists = System.IO.File.Exists(LogPath);
            using (StreamWriter sw = new StreamWriter(LogPath, true))
            {
                if (!fileExists)
                {
                    sw.WriteLine("Time,Kingdom,IsEnhanced,Gold,Income_Gold,Books,Income_Books,Piety,Income_Piety,Trade,Income_Trade,Levies,Income_Levies,Armies,Realms,Buildings,IsDefeated");
                }

                foreach (var k in game.kingdoms)
                {
                    if (k == null) continue;
                    string name = k.Name;
                    if (!TrackedKingdoms.Any(t => name.IndexOf(t, System.StringComparison.OrdinalIgnoreCase) >= 0)) continue;

                    bool isDefeated = k.IsDefeated();
                    bool isEnhanced = AIOverhaulPlugin.IsEnhancedAI(k);

                    if (isDefeated)
                    {
                        sw.WriteLine($"{currentTime:F0},{name},{isEnhanced},0,0,0,0,0,0,0,0,0,0,0,0,0,1");
                        continue;
                    }
                    
                    float gold = k.resources.Get(Logic.ResourceType.Gold);
                    float incomeGold = k.income.Get(Logic.ResourceType.Gold);
                    float books = k.resources.Get(Logic.ResourceType.Books);
                    float incomeBooks = k.income.Get(Logic.ResourceType.Books);
                    float piety = k.resources.Get(Logic.ResourceType.Piety);
                    float incomePiety = k.income.Get(Logic.ResourceType.Piety);
                    float trade = k.resources.Get(Logic.ResourceType.Trade);
                    float incomeTrade = k.income.Get(Logic.ResourceType.Trade);
                    float levies = k.resources.Get(Logic.ResourceType.Levy);
                    float incomeLevies = k.income.Get(Logic.ResourceType.Levy);

                    int armyCount = 0;
                    int buildingCount = 0;
                    int realmCount = k.realms?.Count ?? 0;

                    if (k.realms != null)
                    {
                        foreach (var realm in k.realms)
                        {
                            if (realm.armies != null) armyCount += realm.armies.Count;
                            if (realm.castle != null && realm.castle.buildings != null)
                            {
                                buildingCount += realm.castle.buildings.Count;
                            }
                        }
                    }

                    sw.WriteLine($"{currentTime:F0},{name},{isEnhanced},{gold:F0},{incomeGold:F1},{books:F0},{incomeBooks:F1},{piety:F0},{incomePiety:F1},{trade:F0},{incomeTrade:F1},{levies:F0},{incomeLevies:F1},{armyCount},{realmCount},{buildingCount},0");
                }
            }
            AIOverhaulPlugin.Instance.Log($"[AI-Mod] Performance state logged at game time: {currentTime:F0}");
        }
    }

    [HarmonyPatch(typeof(Logic.KingdomAI), "ThinkGeneral")]
    public class LoggingPatch
    {
        static void Prefix(Logic.KingdomAI __instance)
        {
            if (__instance.kingdom.id == 1) 
            {
                AILogger.LogState(__instance.game);
            }
            else if (__instance.game.kingdoms.Count > 0 && __instance.game.kingdoms[0] == __instance.kingdom)
            {
                 AILogger.LogState(__instance.game);
            }
        }
    }
}
