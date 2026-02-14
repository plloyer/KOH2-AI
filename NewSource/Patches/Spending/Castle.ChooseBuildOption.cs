using BepInEx;
using HarmonyLib;
using Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IOPath = System.IO.Path;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Castle), nameof(Castle.ChooseBuildOption))]
    public static class Castle_ChooseBuildOption
    {
        const string k_LogPrefix = "[Build]";
        const float k_HighPriorityEval = 100f;
        const float k_QueueEval = 1000f;
        const float k_InjectionEval = 1f; // Placeholder for injected options — eval overrides apply after

        // --- Build Queue System ---

        public struct BuildQueueEntry
        {
            public string buildingId;
            public bool isUpgrade;
            public string parentBuildingId;
            public int realmId; // -1 = any eligble castle
        }

        static readonly Dictionary<int, List<BuildQueueEntry>> s_BuildQueues = new Dictionary<int, List<BuildQueueEntry>>();
        static string s_BuildLogPath;
        static bool s_BuildLogInitialized;
        static string s_LastPhase;

        public static void ClearBuildQueues() => s_BuildQueues.Clear();

        public static void EnqueueBuild(Logic.Kingdom kingdom, string buildingId, bool isUpgrade, string parentBuildingId = null, int realmId = -1)
        {
            if (kingdom == null) return;
            var queue = GetOrBuildQueue(kingdom);
            queue.Add(new BuildQueueEntry { buildingId = buildingId, isUpgrade = isUpgrade, parentBuildingId = parentBuildingId, realmId = realmId });
        }

        public static string GetBuildQueueStatus(Logic.Kingdom kingdom)
        {
            if (kingdom == null) return "Phase: Normal";
            if (!s_BuildQueues.TryGetValue(kingdom.id, out var queue) || queue.Count == 0)
                return "Phase: Normal";
            return $"Phase: Initial ({queue.Count} remaining)";
        }

        public static List<BuildQueueEntry> GetBuildQueue(Logic.Kingdom kingdom)
        {
            if (kingdom == null) return null;
            s_BuildQueues.TryGetValue(kingdom.id, out var queue);
            return queue;
        }

        // --- Harmony Prefix ---

        public static void Prefix(Game game, List<Castle.BuildOption> options, ref float sum)
        {
            if (options == null || options.Count == 0) return;

            var castle = options[0].castle;
            var kingdom = castle?.GetKingdom();
            if (kingdom == null) return;
            if (!AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;

            bool isBuildOptions = (options == Castle.build_options);
            var queue = GetOrBuildQueue(kingdom);

            if (queue.Count > 0)
            {
                AdvancePastBuilt(kingdom, queue);
                if (queue.Count > 0)
                {
                    s_LastPhase = "Queue";
                    ApplyBuildQueue(options, kingdom, queue[0], isBuildOptions);
                }
                else
                {
                    s_LastPhase = "Normal";
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Queue empty for {kingdom.Name}, switching to normal mode", LogCategory.Spending, kingdom);
                    ApplyNormalMode(options, kingdom, isBuildOptions);
                }
            }
            else
            {
                s_LastPhase = "Normal";
                ApplyNormalMode(options, kingdom, isBuildOptions);
            }

            // Recalculate sum from modified evals
            float newSum = 0f;
            for (int i = 0; i < options.Count; i++)
                newSum += options[i].eval;
            sum = newSum;

            if (isBuildOptions)
                Castle.build_options_sum = newSum;
            else
                Castle.upgrade_options_sum = newSum;


        }

        // --- Harmony Postfix (CSV Analytics) ---

        public static void Postfix(Game game, List<Castle.BuildOption> options, Castle.BuildOption __result)
        {
            if (__result.def == null || __result.castle == null) return;
            var kingdom = __result.castle.GetKingdom();
            if (kingdom == null || !AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;

            bool isUpgrade = (options == Castle.upgrade_options);
            LogBuildToCsv(game, kingdom, __result, isUpgrade, s_LastPhase ?? "Normal");
        }

        // --- Queue Logic ---

        static List<BuildQueueEntry> GetOrBuildQueue(Logic.Kingdom kingdom)
        {
            if (!s_BuildQueues.TryGetValue(kingdom.id, out var queue))
            {
                queue = BuildInitialQueue(kingdom);
                s_BuildQueues[kingdom.id] = queue;
                if (queue.Count > 0)
                {
                    var sb = new StringBuilder();
                    sb.Append($"{k_LogPrefix} Built initial queue for {kingdom.Name}: ");
                    for (int i = 0; i < queue.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(queue[i].buildingId);
                        if (queue[i].isUpgrade) sb.Append(" (upgrade)");
                        if (queue[i].realmId >= 0) sb.Append($" (realm:{queue[i].realmId})");
                    }
                    AIOverhaulPlugin.LogDebug(sb.ToString(), LogCategory.Spending, kingdom);
                }
            }
            return queue;
        }

        static List<BuildQueueEntry> BuildInitialQueue(Logic.Kingdom kingdom)
        {
            var queue = new List<BuildQueueEntry>();
            if (kingdom.realms == null) return queue;

            // Village militia
            bool hasVillageMilitia = false;
            foreach (var realm in kingdom.realms)
            {
                if (realm?.GetVillageCount() >= GameBalance.MinVillagesForMilitia)
                {
                    hasVillageMilitia = true;
                    queue.Add(new BuildQueueEntry { buildingId = BuildingNames.VillageMilitia, isUpgrade = false, realmId = realm.id });
                }
            }
            
            // Barracks - Find the realm with most keeps
            int bestBarracksRealmId = -1, bestKeeps = -1;
            foreach (var realm in kingdom.realms)
            {
                if (realm == null) continue;
                int kc = realm.GetKeepCount();
                if (kc > bestKeeps)
                {
                    bestBarracksRealmId = realm.id;
                    bestKeeps = kc;
                }
            }
            
            queue.Add(new BuildQueueEntry { buildingId = BuildingNames.Barracks, isUpgrade = false, realmId = bestBarracksRealmId });

            // Swordsmith
            queue.Add(new BuildQueueEntry { buildingId = BuildingUpgradeNames.Swordsmith, isUpgrade = true, parentBuildingId = BuildingNames.Barracks });

            // Fletcher
            queue.Add(new BuildQueueEntry { buildingId = BuildingUpgradeNames.Fletcher, isUpgrade = true, parentBuildingId = BuildingNames.Barracks });

            // TrainingGrounds
            if (hasVillageMilitia)
                queue.Add(new BuildQueueEntry { buildingId = BuildingUpgradeNames.TrainingGrounds, isUpgrade = true, parentBuildingId = BuildingNames.VillageMilitia });

            return queue;
        }

        static void AdvancePastBuilt(Logic.Kingdom kingdom, List<BuildQueueEntry> queue)
        {
            while (queue.Count > 0)
            {
                var entry = queue[0];
                bool alreadyBuilt = entry.isUpgrade
                    ? kingdom.HasBuildingUpgrade(entry.buildingId)
                    : kingdom.HasBuilding(entry.buildingId);

                bool currentlyBuilding = entry.isUpgrade
                    ? kingdom.IsUpgradeInProgress(entry.buildingId)
                    : kingdom.IsBuildingUnderConstruction(entry.buildingId);

                if (alreadyBuilt || currentlyBuilding)
                {
                    string reason = alreadyBuilt ? "already built" : "under construction";
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Queue: {entry.buildingId} {reason}, advancing", LogCategory.Spending, kingdom);
                    queue.RemoveAt(0);
                }
                else
                    break;
            }
        }

        static void ApplyBuildQueue(List<Castle.BuildOption> options, Logic.Kingdom kingdom, BuildQueueEntry entry, bool isBuildOptions)
        {
            bool entryIsBuild = !entry.isUpgrade;
            if (isBuildOptions != entryIsBuild)
            {
                // Mismatch: this call is for builds but entry is upgrade (or vice versa) — clear so vanilla no-ops
                options.Clear();
                return;
            }

            options.Clear();
            if (kingdom.realms == null) return;

            var urgent = KingdomAI.Expense.Priority.Urgent;

            // For builds with a target realm, try that realm first
            if (!entry.isUpgrade && entry.realmId >= 0)
            {
                var targetRealm = kingdom.realms.Find(r => r != null && r.id == entry.realmId);
                if (targetRealm?.castle != null && IsCastleEligible(targetRealm.castle))
                {
                    EnsureBuildOption(options, targetRealm.castle, entry.buildingId, k_QueueEval, urgent);
                    if (options.Count > 0)
                        return;
                }
                // Target realm not eligible — fall through to all castles
            }

            foreach (var realm in kingdom.realms)
            {
                var castle = realm?.castle;
                if (!IsCastleEligible(castle)) continue;

                if (entry.isUpgrade)
                    EnsureUpgradeOption(options, kingdom, castle, entry.buildingId, entry.parentBuildingId, k_QueueEval, urgent);
                else
                    EnsureBuildOption(options, castle, entry.buildingId, k_QueueEval, urgent);
            }

        }

        // --- Normal Mode ---

        static void ApplyNormalMode(List<Castle.BuildOption> options, Logic.Kingdom kingdom, bool isBuildOptions)
        {
            if (isBuildOptions)
                ApplyNormalBuildMode(options, kingdom);
            else
                ApplyNormalUpgradeMode(options, kingdom);
        }

        static void ApplyNormalBuildMode(List<Castle.BuildOption> options, Logic.Kingdom kingdom)
        {
            bool needsBarracks = kingdom.GetBuildingCount(BuildingNames.Barracks) == 0;
            float food = KingdomHelper.GetFood(kingdom);
            var normal = KingdomAI.Expense.Priority.Normal;

            // Injection: add missing options across eligible castles
            if (kingdom.realms != null)
            {
                foreach (var realm in kingdom.realms)
                {
                    var castle = realm?.castle;
                    if (!IsCastleEligible(castle)) continue;

                    int villageCount = realm.GetVillageCount();
                    if (villageCount >= GameBalance.MinVillagesForMilitia)
                        EnsureBuildOption(options, castle, BuildingNames.VillageMilitia, k_InjectionEval, normal);

                    if (needsBarracks)
                        EnsureBuildOption(options, castle, BuildingNames.Barracks, k_InjectionEval, normal);

                    if (food <= 0)
                        InjectFoodBuildOptions(options, castle, realm);
                }
            }

            // Eval overrides on all options (both vanilla-added and injected)
            ApplyBuildingEvalOverrides(options, kingdom, needsBarracks, food);

            // Constraints
            ApplyCropFarmingConstraint(options, food);
            ApplyReligiousSettlementConstraint(options);
        }

        static void ApplyNormalUpgradeMode(List<Castle.BuildOption> options, Logic.Kingdom kingdom)
        {
            bool hasSwordsmith = kingdom.HasBuildingUpgrade(BuildingUpgradeNames.Swordsmith);
            bool hasFletcher = kingdom.HasBuildingUpgrade(BuildingUpgradeNames.Fletcher_Barracks);
            bool hasVillageMilitia = kingdom.HasBuilding(BuildingNames.VillageMilitia);
            bool hasTrainingGrounds = kingdom.HasBuildingUpgrade(BuildingUpgradeNames.TrainingGrounds);
            float food = KingdomHelper.GetFood(kingdom);
            var normal = KingdomAI.Expense.Priority.Normal;

            // Injection: add missing options across eligible castles
            if (kingdom.realms != null)
            {
                foreach (var realm in kingdom.realms)
                {
                    var castle = realm?.castle;
                    if (!IsCastleEligible(castle)) continue;

                    if (!hasSwordsmith)
                        EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.Swordsmith, BuildingNames.Barracks, k_InjectionEval, normal);

                    if (hasSwordsmith && !hasFletcher)
                        EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.Fletcher, BuildingNames.Barracks, k_InjectionEval, normal);

                    if (hasVillageMilitia && !hasTrainingGrounds)
                    {
                        EnsureUpgradeOption(
                            options, kingdom, castle, BuildingUpgradeNames.TrainingGrounds, BuildingNames.VillageMilitia, k_InjectionEval, normal);
                    }

                    if (food <= 0)
                        InjectFoodUpgradeOptions(options, kingdom, castle, realm);
                }
            }

            // Eval overrides on all options (both vanilla-added and injected)
            ApplyUpgradeEvalOverrides(options, kingdom, hasSwordsmith, hasFletcher, hasVillageMilitia, hasTrainingGrounds, food);
        }

        // --- Eval Overrides ---

        static void ApplyBuildingEvalOverrides(List<Castle.BuildOption> options, Logic.Kingdom kingdom, bool needsBarracks, float food)
        {
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                if (opt.def == null) continue;

                var realm = opt.castle.GetRealm();
                string id = opt.def.id;

                if (id == BuildingNames.VillageMilitia)
                {
                    int villageCount = realm.GetVillageCount();
                    if (villageCount >= GameBalance.MinVillagesForMilitia)
                    {
                        opt.eval = k_HighPriorityEval + (villageCount * GameBalance.BoostPerDistrict);
                        opt.priority = KingdomAI.Expense.Priority.Urgent;
                        options[i] = opt;
                    }
                }
                else if (id == BuildingNames.Barracks && needsBarracks)
                {
                    int keep = realm.GetKeepCount();
                    opt.eval = k_HighPriorityEval * (1 + keep * GameBalance.BoostPerDistrict);
                    opt.priority = KingdomAI.Expense.Priority.Urgent;
                    options[i] = opt;
                }
                else if (food <= 0)
                {
                    ApplyFoodBuildingOverride(options, i, opt, realm);
                }
            }
        }

        static void ApplyFoodBuildingOverride(List<Castle.BuildOption> options, int index, Castle.BuildOption opt, Logic.Realm realm)
        {
            string id = opt.def.id;
            float? newEval = null;

            if (id == BuildingNames.CropFarming)
                newEval = k_HighPriorityEval * (0.5f + realm.GetFarmCount());
            else if (id == BuildingNames.Harbor)
                newEval = k_HighPriorityEval * (0.5f + realm.GetCoastalCount());
            else if (id == BuildingNames.SheepFarming || id == BuildingNames.CattleFarming)
                newEval = k_HighPriorityEval * 5;
            else if (id == BuildingNames.Irrigation)
                newEval = k_HighPriorityEval * (1 + realm.GetFarmCount() * 3);
            else if (id == BuildingNames.FurTrade)
                newEval = k_HighPriorityEval * 3;
            else if (id == BuildingNames.Viticulture)
                newEval = k_HighPriorityEval * 3;

            if (newEval.HasValue)
            {
                opt.eval = newEval.Value;
                opt.priority = KingdomAI.Expense.Priority.Urgent;
                options[index] = opt;
            }
        }

        static void ApplyUpgradeEvalOverrides(
            List<Castle.BuildOption> options, Logic.Kingdom kingdom, bool hasSwordsmith, bool hasFletcher,
            bool hasVillageMilitia, bool hasTrainingGrounds, float food)
        {
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                if (opt.def == null) continue;
                string id = opt.def.id;

                if (id == BuildingUpgradeNames.Swordsmith && !hasSwordsmith)
                {
                    opt.eval = GameBalance.HighPriorityBuildingMultiplier;
                    opt.priority = KingdomAI.Expense.Priority.Urgent;
                    options[i] = opt;
                }
                else if (id == BuildingUpgradeNames.Fletcher && hasSwordsmith && !hasFletcher)
                {
                    opt.eval = GameBalance.HighPriorityBuildingMultiplier;
                    opt.priority = KingdomAI.Expense.Priority.Urgent;
                    options[i] = opt;
                }
                else if (id == BuildingUpgradeNames.TrainingGrounds && hasVillageMilitia && !hasTrainingGrounds)
                {
                    opt.eval = GameBalance.HighPriorityBuildingMultiplier;
                    opt.priority = KingdomAI.Expense.Priority.Urgent;
                    options[i] = opt;
                }
                else if (food <= 0)
                {
                    ApplyFoodUpgradeOverride(options, i, opt);
                }
            }
        }

        static void ApplyFoodUpgradeOverride(List<Castle.BuildOption> options, int index, Castle.BuildOption opt)
        {
            string id = opt.def.id;
            float? newEval = null;
            var realm = opt.castle.GetRealm();

            if (id == BuildingUpgradeNames.CropsRotation)
                newEval = k_HighPriorityEval * (0.5f + realm.GetFarmCount());
            else if (id == BuildingUpgradeNames.Docks_Harbor)
                newEval = k_HighPriorityEval * (0.5f + realm.GetCoastalCount());
            else if (id == BuildingUpgradeNames.Butcher_Sheep || id == BuildingUpgradeNames.Butcher_Cattle)
                newEval = k_HighPriorityEval * 5;
            else if (id == BuildingUpgradeNames.SunDryingGrapes)
                newEval = k_HighPriorityEval * 3;

            if (newEval.HasValue)
            {
                opt.eval = newEval.Value;
                opt.priority = KingdomAI.Expense.Priority.Urgent;
                options[index] = opt;
            }
        }

        // --- Injection Helpers ---

        static void InjectFoodBuildOptions(List<Castle.BuildOption> options, Castle castle, Logic.Realm realm)
        {
            bool hasRareGame = realm.features != null && realm.features.Contains(FeatureNames.RareGame);
            bool hasRivers = realm.features != null && realm.features.Contains(FeatureNames.Rivers);
            bool hasVines = realm.features != null && realm.features.Contains(FeatureNames.Vines);
            var p = KingdomAI.Expense.Priority.Normal;

            EnsureBuildOption(options, castle, BuildingNames.CropFarming, k_InjectionEval, p);
            EnsureBuildOption(options, castle, BuildingNames.Harbor, k_InjectionEval, p);
            EnsureBuildOption(options, castle, BuildingNames.SheepFarming, k_InjectionEval, p);
            EnsureBuildOption(options, castle, BuildingNames.CattleFarming, k_InjectionEval, p);
            if (hasRivers)
                EnsureBuildOption(options, castle, BuildingNames.Irrigation, k_InjectionEval, p);
            if (hasRareGame)
                EnsureBuildOption(options, castle, BuildingNames.FurTrade, k_InjectionEval, p);
            if (hasVines)
                EnsureBuildOption(options, castle, BuildingNames.Viticulture, k_InjectionEval, p);
        }

        static void InjectFoodUpgradeOptions(List<Castle.BuildOption> options, Logic.Kingdom kingdom, Castle castle, Logic.Realm realm)
        {
            bool hasVines = realm.features != null && realm.features.Contains(FeatureNames.Vines);
            var p = KingdomAI.Expense.Priority.Normal;

            EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.CropsRotation, BuildingNames.CropFarming, k_InjectionEval, p);
            EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.Docks_Harbor, BuildingNames.Harbor, k_InjectionEval, p);
            EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.Butcher_Sheep, BuildingNames.SheepFarming, k_InjectionEval, p);
            EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.Butcher_Cattle, BuildingNames.CattleFarming, k_InjectionEval, p);
            if (hasVines)
                EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.SunDryingGrapes, BuildingNames.Viticulture, k_InjectionEval, p);
        }

        // --- Constraint Logic ---

        static void ApplyCropFarmingConstraint(List<Castle.BuildOption> options, float food)
        {
            if (food <= 0) return; // Don't remove crop farming when food is critical

            for (int i = options.Count - 1; i >= 0; i--)
            {
                var opt = options[i];
                if (opt.def != null && opt.def.id == BuildingNames.CropFarming)
                {
                    var realm = opt.castle.GetRealm();
                    if (realm.GetFarmCount() == 0)
                        options.RemoveAt(i);
                }
            }
        }

        static void ApplyReligiousSettlementConstraint(List<Castle.BuildOption> options)
        {
            const int minReligiousDistrictCount = 2;

            for (int i = options.Count - 1; i >= 0; i--)
            {
                var opt = options[i];
                if (opt.def == null || !BuildingHelper.IsReligiousBuilding(opt.def.id)) continue;

                var realm = opt.castle.GetRealm();
                int religiousCount = realm.GetReligiousCount();

                if (religiousCount < minReligiousDistrictCount)
                {
                    options.RemoveAt(i);
                }
                else
                {
                    float multiplier = 1 + (religiousCount - minReligiousDistrictCount) * GameBalance.BoostPerDistrict;
                    opt.eval *= multiplier;
                    options[i] = opt;
                }
            }
        }

        // --- Helpers ---

        static bool IsCastleEligible(Castle castle)
        {
            return castle != null && castle.battle == null;
        }

        /// <summary>
        /// Ensures a building option exists in the options list for the given castle.
        /// If missing and the building is available (not already built, can build), injects it.
        /// </summary>
        static bool EnsureBuildOption(List<Castle.BuildOption> options, Castle castle, string buildingId, float eval, KingdomAI.Expense.Priority priority)
        {
            var def = castle.game.defs.Find<Logic.Building.Def>(buildingId);
            if (def == null) return false;
            if (castle.HasBuilding(def)) return false;

            // Check if option already exists for this castle
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                if (opt.def == def && opt.castle == castle)
                    return false;
            }

            // Verify the castle can actually build it
            if (castle.CanBuildBuilding(def, ignore_cost: true) != Castle.StructureBuildAvailability.Available)
                return false;

            options.Add(new Castle.BuildOption { castle = castle, def = def, eval = eval, priority = priority });
            return true;
        }

        /// <summary>
        /// Ensures an upgrade option exists in the options list for the given castle.
        /// If missing and parent building is built (and upgrade is available), injects it.
        /// </summary>
        static bool EnsureUpgradeOption(
            List<Castle.BuildOption> options, Logic.Kingdom kingdom, Castle castle,
            string upgradeId, string parentBuildingId, float eval, KingdomAI.Expense.Priority priority)
        {
            var def = castle.game.defs.Find<Logic.Building.Def>(upgradeId);
            if (def == null) return false;
            if (!kingdom.CanBuildUpgrade(castle, upgradeId)) return false;

            // Check parent building exists at this castle
            var parentDef = castle.game.defs.Find<Logic.Building.Def>(parentBuildingId);
            if (parentDef == null || !castle.HasBuilding(parentDef)) return false;

            // Check if option already exists
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                if (opt.def == def)
                    return false;
            }

            options.Add(new Castle.BuildOption { castle = castle, def = def, eval = eval, priority = priority });
            return true;
        }
        
        // --- CSV Build Log ---

        static void InitBuildLog()
        {
            if (s_BuildLogInitialized) return;
            s_BuildLogInitialized = true;
            try
            {
                string dir = IOPath.Combine(Paths.BepInExRootPath, "AI_Analytics");
                Directory.CreateDirectory(dir);
                s_BuildLogPath = IOPath.Combine(dir, "AI_BuildLog.csv");
                if (!File.Exists(s_BuildLogPath))
                    File.WriteAllText(s_BuildLogPath, "Timestamp,GameYear,Kingdom,Castle,BuildingId,IsUpgrade,Eval,Phase\n");
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"ERROR initializing build log: {ex.Message}");
            }
        }

        static void LogBuildToCsv(Game game, Logic.Kingdom kingdom, Castle.BuildOption option, bool isUpgrade, string phase)
        {
            InitBuildLog();
            if (s_BuildLogPath == null) return;
            try
            {
                float year = KingdomBaseline.GetGameYear(game);
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{year:F1},{CsvHelper.Escape(kingdom.Name)},{CsvHelper.Escape(option.castle.name)},{option.def.id},{isUpgrade},{option.eval:F1},{phase}\n";
                File.AppendAllText(s_BuildLogPath, line);
            }
            catch (Exception ex)
            {
                AIOverhaulPlugin.LogError($"ERROR writing build log: {ex.Message}");
            }
        }
    }
}
