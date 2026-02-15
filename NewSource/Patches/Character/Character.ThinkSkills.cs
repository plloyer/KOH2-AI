using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(Logic.Character), "ThinkSkills")]
    public static class Character_ThinkSkills
    {
        const string k_LogPrefix = "[Character_ThinkSkills] ";

        [HarmonyPrefix]
        public static bool Prefix(Logic.Character __instance, ref bool __result, bool all, bool for_free)
        {
            if (__instance == null) return true;
            var kingdom = __instance.GetKingdom();
            if (!AIOverhaulPlugin.IsEnhancedAI(kingdom)) return true;
            if (__instance.IsKingOrPrince()) return true;

            var king = kingdom.royalFamily?.Sovereign;
            if (king == null || !AreSkillsMaxed(king)) return true;

            // King is maxed — relax books threshold for governors
            if (!__instance.IsInCourt() || __instance.IsPrisoner()) { __result = false; return false; }

            var governedCastle = __instance.GetGovernedCastle();
            if (governedCastle == null) return true; // not a governor, use vanilla

            float booksThreshold = 0.5f; // default: 50% for governors
            if (__instance.IsMerchant() && IsPriorityMerchant(__instance, kingdom))
            {
                booksThreshold = 0f; // priority merchant: no threshold
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix}{__instance.Name} is priority merchant — no books threshold", LogCategory.Governor, kingdom);
            }

            float books = kingdom.resources[ResourceType.Books];
            float maxBooks = kingdom.GetStat(Stats.ks_max_books);
            if (books < GameBalance.MinBooksForGovernorSkills || (maxBooks > 0 && books < maxBooks * booksThreshold))
            {
                __result = false;
                return false;
            }

            // Run upgrade logic (mirrors vanilla)
            if (__instance.game.Random(0, 100) < 50 && __instance.ThinkUpgradeSkill(for_free))
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix}{__instance.Name} upgraded a skill (king maxed)", LogCategory.Governor, kingdom);
                __result = true;
                return false;
            }
            if (__instance.ThinkNewSkill(all, for_free))
            {
                AIOverhaulPlugin.LogDebug($"{k_LogPrefix}{__instance.Name} learned a new skill (king maxed)", LogCategory.Governor, kingdom);
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }

        static bool AreSkillsMaxed(Logic.Character c)
        {
            if (c.skills == null || c.GetSkillsCount() < c.NumSkillSlots()) return false;
            for (int i = 0; i < c.skills.Count; i++)
            {
                if (c.skills[i] != null && c.CanAddSkillRank(c.skills[i])) return false;
            }
            return true;
        }

        static bool IsPriorityMerchant(Logic.Character merchant, Logic.Kingdom kingdom)
        {
            var castle = merchant.GetGovernedCastle();
            if (castle == null) return false;
            var realm = castle.GetRealm();
            if (realm == null) return false;

            // Find the realm with the most goods + potential goods
            int bestScore = -1;
            Logic.Realm bestRealm = null;
            foreach (var r in kingdom.realms)
            {
                if (r == null) continue;
                r.GetGoodsStats(out int current, out int max);
                int score = current + max;
                if (score > bestScore) { bestScore = score; bestRealm = r; }
            }
            return bestRealm == realm;
        }
    }
}
