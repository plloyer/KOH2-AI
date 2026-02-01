using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIOverhaul
{
    public static class RealmHelper
    {
        public static void GetGoodsStats(this Logic.Realm realm, out int current, out int max)
        {
            current = 0;
            max = 0;
            if (realm == null || realm.game == null) return;

            GoodsHelper.EnsureCache(realm.game);

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
                            GoodsHelper.AddGoodsFromList(b.def.produces, currentGoods, realm.game);
                        }
                        // Check produces_completed
                        if (b.IsBuilt() && b.CalcCompleted())
                        {
                            GoodsHelper.AddGoodsFromList(b.def.produces_completed, currentGoods, realm.game);
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
                            GoodsHelper.AddGoodsFromList(u.def.produces, currentGoods, realm.game);
                            
                            if (u.CalcCompleted())
                            {
                                GoodsHelper.AddGoodsFromList(u.def.produces_completed, currentGoods, realm.game);
                            }
                        }
                    }
                }
            }
            current = currentGoods.Count;

            // 2. Calculate Potential (Cached per realm)
            if (GoodsHelper.RealmMaxGoodsCache.TryGetValue(realm.id, out int cachedMax))
            {
                max = cachedMax;
            }
            else
            {
                // Calculate and cache
                HashSet<string> potentialGoods = new HashSet<string>();
                foreach (var def in GoodsHelper.ProductiveBuildings)
                {
                    // Must be buildable in this realm
                    if (!realm.IsPotentiallyBuildable(def)) continue;

                    GoodsHelper.AddGoodsFromList(def.produces, potentialGoods, realm.game);
                    GoodsHelper.AddGoodsFromList(def.produces_completed, potentialGoods, realm.game);
                }
                max = potentialGoods.Count;
                GoodsHelper.RealmMaxGoodsCache[realm.id] = max;
            }
        }

        public static bool HasReligiousSettlement(this Logic.Realm realm)
        {
            if (realm == null) return false;
            return realm.GetReligiousCount() > 0;
        }

        public static int GetKeepCount(this Logic.Realm realm)
        {
            if (realm?.settlements == null) return 0;
            int count = 0;
            foreach (var s in realm.settlements)
            {
                if (s?.def?.id == SettlementNames.Keep)
                    count++;
            }
            return count;
        }

        public static int GetVillageCount(this Logic.Realm realm)
        {
            if (realm?.settlements == null) return 0;
            int count = 0;
            foreach (var s in realm.settlements)
            {
                if (s?.def?.id == SettlementNames.Village)
                    count++;
            }
            return count;
        }

        public static int GetReligiousCount(this Logic.Realm realm)
        {
            if (realm?.settlements == null) return 0;
            int count = 0;
            foreach (var s in realm.settlements)
            {
                if (s?.def != null && IsReligiousSettlement(s.def.id))
                    count++;
            }
            return count;
        }

        public static int GetFarmCount(this Logic.Realm realm)
        {
            if (realm?.settlements == null) return 0;
            int count = 0;
            foreach (var s in realm.settlements)
            {
                if (s?.def?.id == SettlementNames.Farm)
                    count++;
            }
            return count;
        }

        public static int GetCoastalCount(this Logic.Realm realm)
        {
            if (realm?.settlements == null) return 0;
            int count = 0;
            foreach (var s in realm.settlements)
            {
                if (s != null && s != realm.castle && s.coastal)
                    count++;
            }
            return count;
        }

        public static void FindEnemiesInRealm(this Logic.Realm realm, Logic.Kingdom ourKingdom, System.Collections.Generic.List<Logic.Army> armyList)
        {
            if (realm == null || ourKingdom == null || realm.armies == null || armyList == null) return;

            foreach (var army in realm.armies)
            {
                if (army == null || !army.IsValid()) continue;

                var armyOwner = army.GetKingdom();
                if (armyOwner != null && armyOwner != ourKingdom && ourKingdom.IsEnemy(armyOwner))
                    armyList.Add(army);
            }
        }

        public static bool IsPotentiallyBuildable(this Logic.Realm realm, Logic.Building.Def def)
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

        static bool IsReligiousSettlement(string id)
        {
            return id == SettlementNames.Monastery ||
                   id == SettlementNames.Mosque ||
                   id == SettlementNames.Shrine;
        }
    }
}
