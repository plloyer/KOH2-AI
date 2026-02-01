using System;
using System.Collections.Generic;
using Logic;

namespace AIOverhaul
{
    public static class CastleHelper
    {
        public static bool HasDistrict(this Castle castle, string districtName)
        {
            if (castle == null) return false;
            var district = castle.game.GetDistrictDefinition(districtName);
            return district != null && castle.HasDistrict(district);
        }

        /// <summary>
        /// Returns the count of unique trade goods CURRENTLY produced by a castle.
        /// </summary>
        public static int GetCurrentGoodsCount(this Castle castle)
        {
            var goods = castle.GetCurrentGoods();
            return goods?.Count ?? 0;
        }

        /// <summary>
        /// Returns a list of unique trade goods CURRENTLY produced by a castle.
        /// </summary>
        public static HashSet<string> GetCurrentGoods(this Castle castle)
        {
            if (castle == null || castle.buildings == null) return null;

            var activeGoods = new HashSet<string>();

            foreach (var building in castle.buildings)
            {
                if (building == null || building.def == null) continue;

                var goods = GoodsHelper.GetGoodsProducedByDef(building.def.id);
                foreach (var good in goods)
                {
                    activeGoods.Add(good);
                }
            }

            return new HashSet<string>(activeGoods);
        }
    }
}
