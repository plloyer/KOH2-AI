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

            // 1. Current goods — use the game's own tracking
            current = realm.goods_produced?.Count ?? 0;

            // 2. Max goods — district-aware potential
            HashSet<string> potentialGoods = new HashSet<string>();
            var castle = realm.castle;
            if (castle != null)
            {
                var game = realm.game;

                // Helper: collect goods from a building def and its upgrade tree
                void CollectGoods(Logic.Building.Def bdef)
                {
                    if (bdef == null || !realm.IsPotentiallyBuildable(bdef)) return;
                    GoodsHelper.AddGoodsFromList(bdef.produces, potentialGoods, game);
                    GoodsHelper.AddGoodsFromList(bdef.produces_completed, potentialGoods, game);
                    // Recurse into upgrade district
                    if (bdef.upgrades?.buildings != null)
                    {
                        foreach (var uInfo in bdef.upgrades.buildings)
                            CollectGoods(uInfo?.def);
                    }
                }

                // Common district (always available)
                var common = Logic.District.Def.GetCommon(game);
                if (common?.buildings != null)
                    foreach (var bi in common.buildings)
                        CollectGoods(bi?.def);

                // PF district (province-feature buildings)
                var pf = Logic.District.Def.GetPF(game);
                if (pf?.buildings != null)
                    foreach (var bi in pf.buildings)
                        CollectGoods(bi?.def);

                // Settlement-gated districts
                var districts = castle.GetBuildableDistricts();
                if (districts != null)
                    foreach (var d in districts)
                        if (d?.buildings != null)
                            foreach (var bi in d.buildings)
                                CollectGoods(bi?.def);
            }
            max = potentialGoods.Count;
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
                if (s?.def != null && DistrictHelper.IsReligiousSettlement(s.def.id))
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

        public static void FindEnemiesInRealm(this Logic.Realm realm, Logic.Kingdom ourKingdom, List<Logic.Army> armyList)
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

        public static bool IsPotentiallyBuildable(this Logic.Realm realm, Logic.Building.Def def, int depth = 0)
        {
            if (depth > 10) return false; // Guard against circular deps

            if (def.requires != null)
            {
                foreach (var req in def.requires)
                {
                    if (req.type == GlobalConstants.ReqType_Resource) continue;
                    if (req.type == GlobalConstants.ReqType_Region && req.key == GlobalConstants.Region_Europe) continue;

                    var bDef = realm.game.defs.Find<Logic.Building.Def>(req.key);
                    if (bDef != null)
                    {
                        // Recursively check if parent building is also buildable
                        if (!realm.IsPotentiallyBuildable(bDef, depth + 1)) return false;
                        continue;
                    }

                    // Feature check
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
