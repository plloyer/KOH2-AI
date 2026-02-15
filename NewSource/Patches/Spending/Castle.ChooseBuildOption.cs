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
        const int k_MinGoodsFeatures = 3;
        const float k_VanillaEvalDivisor = 40000f;

        static readonly string[] s_GoodsFeatures = {
            FeatureNames.Sheep, FeatureNames.Cattle, FeatureNames.FlaxFields, FeatureNames.Herbage,
            FeatureNames.Vines, FeatureNames.DeepForests, FeatureNames.IronOre, FeatureNames.MarbleDeposit,
            FeatureNames.Horses, FeatureNames.AmberDeposits, FeatureNames.Camels, FeatureNames.RareGame,
            FeatureNames.MineralsDeposit, FeatureNames.SaltDeposit, FeatureNames.SaltpeterDeposits,
            FeatureNames.SulfurDeposits, FeatureNames.LimestoneDeposit, FeatureNames.SilverOre,
            FeatureNames.GoldOre, FeatureNames.LodestoneDeposits
        };

        // --- Build Queue System ---

        public struct BuildQueueEntry
        {
            public string buildingId;
            public bool isUpgrade;
            public string parentBuildingId;
            public int realmId; // -1 = any eligble castle
        }

        static readonly Dictionary<int, List<BuildQueueEntry>> s_BuildQueues = new Dictionary<int, List<BuildQueueEntry>>();
        static readonly Dictionary<int, (string buildingId, Logic.Time firstSeen)> s_QueueFrontTracker = new Dictionary<int, (string, Logic.Time)>();
        static string s_BuildLogPath;
        static bool s_BuildLogInitialized;
        static string s_LastPhase;

        public static void ClearBuildQueues() { s_BuildQueues.Clear(); s_QueueFrontTracker.Clear(); }

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
            if (!AIOverhaulPlugin.IsEnhancedAI(kingdom)) return;

            // Don't build anything until we have at least 2 merchants
            if (kingdom.CountMerchants() < 2)
            {
                if (kingdom == game.GetLocalPlayerKingdom())
                {
                    if (options == Castle.build_options) Castle.last_build_options.Clear();
                    else Castle.last_upgrade_options.Clear();
                }
                options.Clear();
                sum = 0f;
                return;
            }

            bool isBuildOptions = (options == Castle.build_options);
            var queue = GetOrBuildQueue(kingdom);

            if (queue.Count > 0)
            {
                AdvancePastBuilt(kingdom, queue);

                // Game-time timeout: if the front entry hasn't changed in 15 game minutes, force-skip it
                if (queue.Count > 0)
                {
                    var front = queue[0];
                    int kid = kingdom.id;
                    if (s_QueueFrontTracker.TryGetValue(kid, out var tracked))
                    {
                        if (tracked.buildingId == front.buildingId)
                        {
                            float elapsedSec = game.time - tracked.firstSeen;
                            if (elapsedSec > GameBalance.BuildQueueStallTimeoutSec)
                            {
                                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Queue: {front.buildingId} stalled for {elapsedSec:F0}s game-time, force-skipping", LogCategory.Spending, kingdom);
                                queue.RemoveAt(0);
                                s_QueueFrontTracker.Remove(kid);
                            }
                        }
                        else
                        {
                            // Front changed — reset tracker
                            s_QueueFrontTracker[kid] = (front.buildingId, game.time);
                        }
                    }
                    else
                    {
                        s_QueueFrontTracker[kid] = (front.buildingId, game.time);
                    }
                }

                bool queueApplied = false;
                while (queue.Count > 0)
                {
                    var entry = queue[0];
                    bool typeMismatch = (isBuildOptions != !entry.isUpgrade);

                    s_LastPhase = "Queue";
                    ApplyBuildQueue(options, kingdom, entry, isBuildOptions);

                    if (options.Count > 0 || typeMismatch)
                    {
                        queueApplied = true;
                        break;
                    }

                    // Entry is unbuildable in all castles — skip it
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Queue: {entry.buildingId} unbuildable, skipping", LogCategory.Spending, kingdom);
                    queue.RemoveAt(0);
                }

                if (!queueApplied)
                {
                    s_LastPhase = "Normal";
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Queue exhausted for {kingdom.Name}, switching to normal mode", LogCategory.Spending, kingdom);
                    ApplyNormalMode(options, kingdom, isBuildOptions);
                }
            }
            else
            {
                s_LastPhase = "Normal";
                NormalizeVanillaEvals(options);
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

            string kind = isBuildOptions ? "build" : "upgrade";
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Prefix done ({kind}): {options.Count} options, sum={newSum:F1}, phase={s_LastPhase}", LogCategory.Spending, kingdom);

            // Refresh the debug snapshot so the overlay sees normalized evals (local player only)
            if (kingdom == game.GetLocalPlayerKingdom())
            {
                if (isBuildOptions)
                {
                    Castle.last_build_options.Clear();
                    Castle.last_build_options.AddRange(options);
                }
                else
                {
                    Castle.last_upgrade_options.Clear();
                    Castle.last_upgrade_options.AddRange(options);
                }
            }
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
        
        // --- Queue Init and Normal mode ---

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
            
            // House next to barrack
            queue.Add(new BuildQueueEntry { buildingId = BuildingNames.Housings, isUpgrade = false, realmId = bestBarracksRealmId });

            // TrainingGrounds
            if (hasVillageMilitia)
                queue.Add(new BuildQueueEntry { buildingId = BuildingUpgradeNames.TrainingGrounds, isUpgrade = true, parentBuildingId = BuildingNames.VillageMilitia });
            
            return queue;
        }

        static void ApplyNormalBuildMode(List<Castle.BuildOption> options, Logic.Kingdom kingdom)
        {
            ApplyCropFarmingConstraint(options, kingdom);
            ApplyReligiousSettlementConstraint(options);

            ApplyFoodBuildings(options, kingdom);
            ApplyGoodsBuildings(options, kingdom);
        }

        static void ApplyFoodBuildings(List<Castle.BuildOption> options, Logic.Kingdom kingdom)
        {
            float food = KingdomHelper.GetFood(kingdom);
            if (food > 0 || kingdom.realms == null) return;

            var urgent = KingdomAI.Expense.Priority.Urgent;

            foreach (var realm in kingdom.realms)
            {
                var castle = realm?.castle;
                if (!IsCastleEligible(castle)) continue;

                int farmCount = realm.GetFarmCount();
                int coastalCount = realm.GetCoastalCount();
                bool hasRivers = realm.features != null && realm.features.Contains(FeatureNames.Rivers);
                bool hasRareGame = realm.features != null && realm.features.Contains(FeatureNames.RareGame);
                bool hasVines = realm.features != null && realm.features.Contains(FeatureNames.Vines);

                EnsureBuildOption(options, castle, BuildingNames.CropFarming, k_HighPriorityEval * (0.5f + farmCount), urgent);
                EnsureBuildOption(options, castle, BuildingNames.Harbor, k_HighPriorityEval * (0.5f + coastalCount), urgent);
                EnsureBuildOption(options, castle, BuildingNames.SheepFarming, k_HighPriorityEval * 5, urgent);
                EnsureBuildOption(options, castle, BuildingNames.CattleFarming, k_HighPriorityEval * 5, urgent);
                if (hasRivers)
                    EnsureBuildOption(options, castle, BuildingNames.Irrigation, k_HighPriorityEval * (1 + farmCount * 3), urgent);
                if (hasRareGame)
                    EnsureBuildOption(options, castle, BuildingNames.FurTrade, k_HighPriorityEval * 3, urgent);
                if (hasVines)
                    EnsureBuildOption(options, castle, BuildingNames.Viticulture, k_HighPriorityEval * 3, urgent);
            }
        }

        static void ApplyGoodsBuildings(List<Castle.BuildOption> options, Logic.Kingdom kingdom)
        {
            if (kingdom.realms == null) return;
            var normal = KingdomAI.Expense.Priority.Normal;

            foreach (var realm in kingdom.realms)
            {
                var castle = realm?.castle;
                if (!IsCastleEligible(castle)) continue;

                int goodsCount = CountGoodsFeatures(realm);
                if (goodsCount < k_MinGoodsFeatures) continue;

                float highEval = k_HighPriorityEval * 2;
                float mediumEval = k_HighPriorityEval;
                float lowEval = k_HighPriorityEval * 0.5f;

                // High-production buildings (produce more goods)
                EnsureBuildOption(options, castle, BuildingNames.SheepFarming, highEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.CattleFarming, highEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.FlaxGrowing, highEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.HerbGardening, highEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.Viticulture, highEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.Woodworking, highEval, normal);
                
                // Medium
                EnsureBuildOption(options, castle, BuildingNames.Metalworking, mediumEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.Stoneworking, mediumEval, normal);

                // Lower-production trade buildings
                EnsureBuildOption(options, castle, BuildingNames.HorseBreeding, highEval, normal);

                // Market is a prerequisite for trade buildings — ensure it's built first
                EnsureBuildOption(options, castle, BuildingNames.MarketSquare, lowEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.AmberTrade, lowEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.CamelsTrade, lowEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.FurTrade, lowEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.MineralsTrade, lowEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.SaltTrade, lowEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.SaltpeterTrade, lowEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.SulfurTrade, lowEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.LimeTrade, lowEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.SilverSmelting, lowEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.GoldSmelting, lowEval, normal);
                EnsureBuildOption(options, castle, BuildingNames.LodestoneTrade, lowEval, normal);
            }
        }

        static int CountGoodsFeatures(Logic.Realm realm)
        {
            if (realm.features == null) return 0;
            int count = 0;
            for (int i = 0; i < s_GoodsFeatures.Length; i++)
            {
                if (realm.features.Contains(s_GoodsFeatures[i]))
                    count++;
            }
            return count;
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

        static void AdvancePastBuilt(Logic.Kingdom kingdom, List<BuildQueueEntry> queue)
        {
            while (queue.Count > 0)
            {
                var entry = queue[0];
                bool alreadyBuilt, currentlyBuilding;

                // When entry targets a specific realm, check only that realm's castle
                Castle targetCastle = null;
                if (entry.realmId >= 0)
                    targetCastle = kingdom.realms?.Find(r => r != null && r.id == entry.realmId)?.castle;

                if (targetCastle != null)
                {
                    if (entry.isUpgrade)
                    {
                        var def = targetCastle.game?.defs.Find<Logic.Building.Def>(entry.buildingId);
                        alreadyBuilt = def != null && kingdom.HasBuildingUpgrade(def);
                        currentlyBuilding = kingdom.IsUpgradeInProgress(entry.buildingId);
                    }
                    else
                    {
                        var def = targetCastle.game?.defs.Find<Logic.Building.Def>(entry.buildingId);
                        alreadyBuilt = def != null && targetCastle.HasBuilding(def);
                        var buildDef = targetCastle.GetCurrentBuildingBuild();
                        currentlyBuilding = buildDef != null && buildDef.id == entry.buildingId;
                    }
                }
                else
                {
                    alreadyBuilt = entry.isUpgrade ? kingdom.HasBuildingUpgrade(entry.buildingId) : kingdom.HasBuilding(entry.buildingId);
                    currentlyBuilding = entry.isUpgrade ? kingdom.IsUpgradeInProgress(entry.buildingId) : kingdom.IsBuildingUnderConstruction(entry.buildingId);
                }

                if (alreadyBuilt || currentlyBuilding)
                {
                    string reason = alreadyBuilt ? "already built" : "under construction";
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} Queue: {entry.buildingId} (realm:{entry.realmId}) {reason}, advancing", LogCategory.Spending, kingdom);
                    queue.RemoveAt(0);
                }
                else
                    break;
            }
        }

        static void ApplyBuildQueue(List<Castle.BuildOption> options, Logic.Kingdom kingdom, BuildQueueEntry entry, bool isBuildOptions)
        {
            bool entryIsBuild = !entry.isUpgrade;
            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} ApplyBuildQueue: {entry.buildingId} (upgrade={entry.isUpgrade}, realm={entry.realmId}), isBuildOptions={isBuildOptions}", LogCategory.Spending, kingdom);

            if (isBuildOptions != entryIsBuild)
            {
                // Mismatch: this call is for builds but entry is upgrade (or vice versa) — clear so vanilla no-ops
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} ApplyBuildQueue: type mismatch, clearing options", LogCategory.Spending, kingdom);
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
                    bool ok = EnsureBuildOption(options, targetRealm.castle, entry.buildingId, k_QueueEval, urgent);
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} ApplyBuildQueue: target realm {entry.realmId} EnsureBuildOption={ok}, options={options.Count}", LogCategory.Spending, kingdom);
                    if (options.Count > 0)
                        return;
                }
                else
                {
                    AIOverhaulPlugin.LogDebug($"{k_LogPrefix} ApplyBuildQueue: target realm {entry.realmId} not eligible, falling through", LogCategory.Spending, kingdom);
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

            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} ApplyBuildQueue: final options count={options.Count}", LogCategory.Spending, kingdom);
        }

        // --- Normal Mode ---

        static void ApplyNormalMode(List<Castle.BuildOption> options, Logic.Kingdom kingdom, bool isBuildOptions)
        {
            if (isBuildOptions)
                ApplyNormalBuildMode(options, kingdom);
            else
                ApplyNormalUpgradeMode(options, kingdom);
        }

        static void ApplyNormalUpgradeMode(List<Castle.BuildOption> options, Logic.Kingdom kingdom)
        {
            ApplyFoodUpgrades(options, kingdom);
            ApplyGoodsUpgrades(options, kingdom);

            // Scribe Office: high priority once MarketSquare is built
            if (kingdom.realms != null)
            {
                foreach (var realm in kingdom.realms)
                {
                    var castle = realm?.castle;
                    if (!IsCastleEligible(castle)) continue;
                    EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.ScribesOffice, BuildingNames.MarketSquare, k_HighPriorityEval * 2, KingdomAI.Expense.Priority.High);
                }
            }

            // Aqueduct: high priority when barracks realm has no workers left for hiring
            if (kingdom.realms != null)
            {
                foreach (var realm in kingdom.realms)
                {
                    var castle = realm?.castle;
                    if (!IsCastleEligible(castle)) continue;
                    var barracksDef = castle.game.defs.Find<Logic.Building.Def>(BuildingNames.Barracks);
                    if (barracksDef == null || !castle.HasBuilding(barracksDef)) continue;
                    if (castle.population != null && castle.population.workers <= 1)
                        EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.Aqueduct, BuildingNames.Housings, k_HighPriorityEval * 2, KingdomAI.Expense.Priority.High);
                }
            }
        }

        static void ApplyFoodUpgrades(List<Castle.BuildOption> options, Logic.Kingdom kingdom)
        {
            float food = KingdomHelper.GetFood(kingdom);
            if (food > 0 || kingdom.realms == null) return;

            var urgent = KingdomAI.Expense.Priority.Urgent;

            foreach (var realm in kingdom.realms)
            {
                var castle = realm?.castle;
                if (!IsCastleEligible(castle)) continue;

                int farmCount = realm.GetFarmCount();
                int coastalCount = realm.GetCoastalCount();
                bool hasVines = realm.features != null && realm.features.Contains(FeatureNames.Vines);

                EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.CropsRotation, BuildingNames.CropFarming, k_HighPriorityEval * (0.5f + farmCount), urgent);
                EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.Docks_Harbor, BuildingNames.Harbor, k_HighPriorityEval * (0.5f + coastalCount), urgent);
                EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.Butcher_Sheep, BuildingNames.SheepFarming, k_HighPriorityEval * 5, urgent);
                EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.Butcher_Cattle, BuildingNames.CattleFarming, k_HighPriorityEval * 5, urgent);
                if (hasVines)
                    EnsureUpgradeOption(options, kingdom, castle, BuildingUpgradeNames.SunDryingGrapes, BuildingNames.Viticulture, k_HighPriorityEval * 3, urgent);
            }
        }

        static void ApplyGoodsUpgrades(List<Castle.BuildOption> options, Logic.Kingdom kingdom)
        {
            if (kingdom.realms == null) return;
            var priority = KingdomAI.Expense.Priority.High;

            foreach (var realm in kingdom.realms)
            {
                var castle = realm?.castle;
                if (!IsCastleEligible(castle)) continue;

                int goodsCount = CountGoodsFeatures(realm);
                if (goodsCount < k_MinGoodsFeatures) continue;

                float highEval = k_HighPriorityEval * 2;
                float mediumEval = k_HighPriorityEval;

                // High-production buildings
                EnsureUpgradesForBuilding(options, kingdom, castle, BuildingNames.SheepFarming, highEval, priority);
                EnsureUpgradesForBuilding(options, kingdom, castle, BuildingNames.CattleFarming, highEval, priority);
                EnsureUpgradesForBuilding(options, kingdom, castle, BuildingNames.FlaxGrowing, highEval, priority);
                EnsureUpgradesForBuilding(options, kingdom, castle, BuildingNames.HerbGardening, highEval, priority);
                EnsureUpgradesForBuilding(options, kingdom, castle, BuildingNames.Viticulture, highEval, priority);
                EnsureUpgradesForBuilding(options, kingdom, castle, BuildingNames.Woodworking, highEval, priority);

                // Medium
                EnsureUpgradesForBuilding(options, kingdom, castle, BuildingNames.Metalworking, mediumEval, priority);
                EnsureUpgradesForBuilding(options, kingdom, castle, BuildingNames.Stoneworking, mediumEval, priority);

                // Lower-production trade buildings
                EnsureUpgradesForBuilding(options, kingdom, castle, BuildingNames.HorseBreeding, highEval, priority);
            }
        }

        // --- Constraint Logic ---

        static void ApplyCropFarmingConstraint(List<Castle.BuildOption> options, Logic.Kingdom kingdom)
        {
            float food = KingdomHelper.GetFood(kingdom);
            if (food <= 0) return;

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

        static void NormalizeVanillaEvals(List<Castle.BuildOption> options)
        {
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                opt.eval /= k_VanillaEvalDivisor;
                options[i] = opt;
            }
        }

        /// <summary>
        /// Ensures a building option exists in the options list for the given castle.
        /// If missing and the building is available (not already built, can build), injects it.
        /// </summary>
        static bool EnsureBuildOption(List<Castle.BuildOption> options, Castle castle, string buildingId, float eval, KingdomAI.Expense.Priority priority)
        {
            var kingdom = castle.GetKingdom();
            var def = castle.game.defs.Find<Logic.Building.Def>(buildingId);
            if (def == null)
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} EnsureBuildOption: {buildingId} @ {castle.name} — def is null", LogCategory.Spending, kingdom);
                return false;
            }
            if (castle.HasBuilding(def))
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} EnsureBuildOption: {buildingId} @ {castle.name} — already built", LogCategory.Spending, kingdom);
                return false;
            }

            // Check if option already exists for this castle — update eval/priority if so
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                if (opt.def == def && opt.castle == castle)
                {
                    opt.eval = eval;
                    opt.priority = priority;
                    options[i] = opt;
                    return true;
                }
            }

            // Verify the castle can actually build it (ignore_must_expand: vanilla adds options even when expansion is needed,
            // the caller handles ExpandCity vs BuildStructure; ignore_under_construction: same as vanilla AddBuildOption)
            var availability = castle.CanBuildBuilding(def, ignore_cost: true, ignore_must_expand: true, ignore_under_construction: true);
            if (availability != Castle.StructureBuildAvailability.Available && availability != Castle.StructureBuildAvailability.NoParent)
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix} EnsureBuildOption: {buildingId} @ {castle.name} — availability={availability}", LogCategory.Spending, kingdom);
                return false;
            }

            AIOverhaulPlugin.LogDebug($"{k_LogPrefix} EnsureBuildOption: {buildingId} @ {castle.name} — injected (eval={eval:F0}, avail={availability})", LogCategory.Spending, kingdom);
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

            // Check if option already exists — update eval/priority if so
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                if (opt.def == def)
                {
                    opt.eval = eval;
                    opt.priority = priority;
                    options[i] = opt;
                    return true;
                }
            }

            options.Add(new Castle.BuildOption { castle = castle, def = def, eval = eval, priority = priority });
            return true;
        }

        /// <summary>
        /// Dynamically discovers all upgrades for a building via def.upgrades.buildings and ensures each one.
        /// </summary>
        static void EnsureUpgradesForBuilding(
            List<Castle.BuildOption> options, Logic.Kingdom kingdom, Castle castle,
            string parentBuildingId, float eval, KingdomAI.Expense.Priority priority)
        {
            var parentDef = castle.game.defs.Find<Logic.Building.Def>(parentBuildingId);
            if (parentDef == null || !castle.HasBuilding(parentDef)) return;

            var upgrades = parentDef.upgrades?.buildings;
            if (upgrades == null) return;

            for (int i = 0; i < upgrades.Count; i++)
            {
                var upgradeDef = upgrades[i].def;
                if (upgradeDef == null) continue;
                EnsureUpgradeOption(options, kingdom, castle, upgradeDef.id, parentBuildingId, eval, priority);
            }
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
