using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIOverhaul
{
    public static class RealmHelper
    {
        // Cache to avoid iterating defs every frame
        static List<Logic.Building.Def> s_ProductiveBuildings;

        // Cache for max goods per realm (init once, usable by all threads safe enough for read)
        // Key: Realm ID, Value: Max Goods Count
        static Dictionary<int, int> s_RealmMaxGoodsCache = new Dictionary<int, int>();
        
        static void EnsureCache(Logic.Game game)
        {
            if (s_ProductiveBuildings != null) return;
            s_ProductiveBuildings = new List<Logic.Building.Def>();

            if (game?.defs == null) return;

            var allBuildings = game.defs.GetDefs<Logic.Building.Def>();
            if (allBuildings == null) return;

            foreach (var def in allBuildings)
            {
                if (def == null) continue;
                bool producesGoods = false;

                // Check basics
                if (HasTradeGood(def.produces, game)) producesGoods = true;
                else if (HasTradeGood(def.produces_completed, game)) producesGoods = true;

                if (producesGoods)
                {
                    s_ProductiveBuildings.Add(def);
                }
            }
        }

        static bool HasTradeGood(List<Logic.Building.Def.ProducedResource> list, Logic.Game game)
        {
            if (list == null) return false;
            foreach (var p in list)
            {
                if (IsTradeGood(p.resource, game)) return true;
            }
            return false;
        }

        public static bool IsTradeGood(string resourceName, Logic.Game game)
        {
            if (string.IsNullOrEmpty(resourceName) || game == null) return false;

            // NATIVE DETECT: Check if a Resource.Def exists and has a Name.
            // Base resources (Gold, Food, etc.) do not have a loaded Resource.Def with a Name.
            var def = game.defs.Find<Logic.Resource.Def>(resourceName);
            if (def != null && !string.IsNullOrEmpty(def.Name))
            {
                return true;
            }
            return false;
        }

        public static void GetGoodsStats(Logic.Realm realm, out int current, out int max)
        {
            current = 0;
            max = 0;
            if (realm == null || realm.game == null) return;

            EnsureCache(realm.game);

            // 1. Calculate Current (Dynamic, must check every frame)
            // 1. Calculate Current (Dynamic, must check every frame)
            HashSet<string> currentGoods = new HashSet<string>();
            if (realm.castle != null)
            {
                // Iterate Buildings
                if (realm.castle.buildings != null)
                {
                    foreach (var b in realm.castle.buildings)
                    {
                        if (b == null || b.def == null) continue;
                        // Check produces
                        if (b.IsBuilt() && b.IsFullyFunctional())
                        {
                            AddGoodsFromList(b.def.produces, currentGoods, realm.game);
                        }
                        // Check produces_completed
                        if (b.IsBuilt() && b.CalcCompleted())
                        {
                            AddGoodsFromList(b.def.produces_completed, currentGoods, realm.game);
                        }
                    }
                }

                // Iterate Upgrades
                if (realm.castle.upgrades != null)
                {
                    foreach (var u in realm.castle.upgrades)
                    {
                        if (u == null || u.def == null) continue;
                        // Upgrades must be built and functional
                        if (u.IsBuilt() && u.IsFullyFunctional())
                        {
                            AddGoodsFromList(u.def.produces, currentGoods, realm.game);
                            
                            // Upgrades usually don't have "completed" state separate from built, 
                            // but let's check just in case or if it behaves like buildings
                            if (u.CalcCompleted())
                            {
                                AddGoodsFromList(u.def.produces_completed, currentGoods, realm.game);
                            }
                        }
                    }
                }
            }
            current = currentGoods.Count;

            // 2. Calculate Potential (Cached per realm)
            if (s_RealmMaxGoodsCache.TryGetValue(realm.id, out int cachedMax))
            {
                max = cachedMax;
            }
            else
            {
                // Calculate and cache
                HashSet<string> potentialGoods = new HashSet<string>();
                foreach (var def in s_ProductiveBuildings)
                {
                    // Must be buildable in this realm
                    if (!IsPotentiallyBuildable(def, realm)) continue;

                    AddGoodsFromList(def.produces, potentialGoods, realm.game);
                    AddGoodsFromList(def.produces_completed, potentialGoods, realm.game);
                }
                max = potentialGoods.Count;
                s_RealmMaxGoodsCache[realm.id] = max;
            }
        }

        static void AddGoodsFromList(List<Logic.Building.Def.ProducedResource> list, HashSet<string> set, Logic.Game game)
        {
            if (list == null) return;
            foreach (var p in list)
            {
                if (IsTradeGood(p.resource, game))
                {
                    set.Add(p.resource);
                }
            }
        }

        static bool IsPotentiallyBuildable(Logic.Building.Def def, Logic.Realm realm)
        {
            // Check features
            if (def.requires != null)
            {
                foreach (var req in def.requires)
                {
                    // Ignore Resources (Gold, etc.)
                    if (req.type == GlobalConstants.ReqType_Resource) continue;
                    if (req.type == GlobalConstants.ReqType_Region && req.key == GlobalConstants.Region_Europe) continue; // Hack: Assume Europe

                    // Ignore other Buildings (assume we can build them)
                    var bDef = realm.game.defs.Find<Logic.Building.Def>(req.key);
                    if (bDef != null) continue;

                    // It must be a Feature/Tag
                    // Check if realm has it
                    if (realm.features == null || !realm.features.Contains(req.key))
                    {
                        return false; 
                    }
                }
            }
            return true;
        }
    }
}
