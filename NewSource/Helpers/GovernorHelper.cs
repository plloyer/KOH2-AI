using System.Linq;

namespace AIOverhaul
{
    public static class GovernorHelper
    {
        public static void ReassignKingToOptimalRealm(Logic.Kingdom kingdom)
        {
            if (kingdom == null) return;

            var king = kingdom.royalFamily?.Sovereign;
            if (king == null) return;

            string classId = king.class_def?.id;
            if (string.IsNullOrEmpty(classId) || classId == CharacterClassNames.Spy || classId == CharacterClassNames.ClasslessPrince)
                return;

            Logic.Realm bestRealm = null;
            float bestScore = float.MinValue;

            foreach (var realm in kingdom.realms)
            {
                if (realm?.castle == null) continue;
                float score = ScoreRealmForClass(realm, classId);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestRealm = realm;
                }
            }

            if (bestRealm == null) return;

            var currentCastle = king.GetGovernedCastle();
            if (currentCastle == bestRealm.castle) return;

            var scoreLog = string.Join(", ", kingdom.realms
                .Where(r => r?.castle != null)
                .Select(r => $"{r.castle.name}={ScoreRealmForClass(r, classId):F0}")
            );
            AIOverhaulPlugin.LogDebug($"King {king.Name} ({classId}) → {bestRealm.castle.name} | Scores: {scoreLog}", LogCategory.Governor, kingdom);
            king.Govern(bestRealm.castle);
        }

        static float ScoreRealmForClass(Logic.Realm realm, string classId)
        {
            float score = 0f;
            float tiebreaker = (realm.settlements?.Count ?? 0) * 0.01f;

            if (classId == CharacterClassNames.Marshal)
            {
                score = realm.GetKeepCount();
            }
            else if (classId == CharacterClassNames.Merchant)
            {
                realm.GetGoodsStats(out int currentGoods, out _);
                score = currentGoods * GameBalance.MerchantGovernorGoodsBonus + realm.GetVillageCount();
            }
            else if (classId == CharacterClassNames.Cleric)
            {
                score = realm.GetReligiousCount();
            }
            else if (classId == CharacterClassNames.Diplomat)
            {
                score = realm.GetVillageCount();
            }

            return score + tiebreaker;
        }
    }
}
