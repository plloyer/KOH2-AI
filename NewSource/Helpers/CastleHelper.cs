using System;
using System.Collections.Generic;
using Logic;

namespace AIOverhaul
{
    public static class CastleHelper
    {
        public static bool HasDistrict(this Castle castle, string districtName)
        {
            var district = (castle?.game).GetDistrictDefinition(districtName);
            return district != null && castle.HasDistrict(district);
        }

        /// <summary>
        /// Count how many religion building slots exist in a district definition
        /// </summary>
        public static int CountReligionSlots(this Castle castle, District.Def religionDistrict)
        {
            if (religionDistrict?.buildings == null) return 0;

            // Count how many religion building slots exist in this district definition
            return religionDistrict.buildings.Count;
        }

        /// <summary>
        /// Returns the count of unique trade goods CURRENTLY produced by a castle.
        /// </summary>
        public static int GetCurrentGoodsCount(this Castle castle)
        {
            var goods = GetCurrentGoods(castle);
            return goods != null ? goods.Count : 0;
        }

        /// <summary>
        /// Returns a list of unique trade goods CURRENTLY produced by a castle.
        /// </summary>
        public static List<string> GetCurrentGoods(this Castle castle)
        {
            if (castle == null || castle.buildings == null) return new List<string>();

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

            return new List<string>(activeGoods);
        }
    }
}
