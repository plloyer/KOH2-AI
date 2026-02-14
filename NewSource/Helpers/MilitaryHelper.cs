using System;
using Logic;

namespace AIOverhaul
{
    public static class MilitaryHelper
    {
        // --- Strength Helpers ---

        public static bool IsStrongerThan(float ourStrength, float enemyStrength, float ratio)
        {
            if (enemyStrength <= 0) return ourStrength > 0;
            return ourStrength >= enemyStrength * ratio;
        }

        public static float GetRealmEnemyStrength(Logic.Realm realm, Logic.Kingdom kingdom)
        {
            float strength = 0;
            if (realm?.armies == null || kingdom == null) return strength;

            foreach (var a in realm.armies)
            {
                if (a != null && kingdom.IsEnemy(a.kingdom_id))
                    strength += a.EvalStrength();
            }
            return strength;
        }

        public static float GetRealmOwnStrength(Logic.Realm realm, Logic.Kingdom kingdom)
        {
            float strength = 0;
            if (realm?.armies == null || kingdom == null) return strength;

            foreach (var a in realm.armies)
            {
                if (a != null && a.kingdom_id == kingdom.id)
                    strength += a.EvalStrength();
            }
            return strength;
        }

        // --- Target Helpers ---

        public static bool IsEnemyTerritory(Logic.Realm realm, Logic.Kingdom kingdom)
        {
            if (realm == null || kingdom == null) return false;
            var realmKingdom = realm.GetKingdom();
            return realmKingdom != null && realmKingdom.IsEnemy(kingdom.id);
        }

        public static bool IsLeaderHeadingToFight(Logic.Army leader, Logic.Kingdom kingdom)
        {
            if (leader == null) return false;

            if (leader.ai_status == AIStatusNames.SiegeRecall) return true;

            // In battle
            if (leader.battle != null) return true;

            // Check target
            var target = leader.GetTarget();
            if (target == null) return false;

            if (target is Logic.Battle) return true;

            if (target is Logic.Army targetArmy)
                return targetArmy.IsEnemy(kingdom);

            if (target is Castle targetCastle)
            {
                var castleKingdom = targetCastle.GetKingdom();
                return castleKingdom != null && castleKingdom.IsEnemy(kingdom.id);
            }

            return false;
        }

        public static string DescribeTarget(object target)
        {
            if (target == null) return "None";

            if (target is Castle c) return $"Castle:{c.name}";
            if (target is Logic.Army a) return $"Army:{a.leader?.Name ?? a.GetNid().ToString()}";
            if (target is Logic.Battle) return "Battle";
            if (target is Logic.Realm r) return $"Realm:{r.name}";

            return target.GetType().Name;
        }

        public static string DescribeArmy(Logic.Army army)
        {
            if (army == null) return "null";
            string name = army.leader?.Name ?? $"Army#{army.GetNid()}";
            int units = army.units?.Count ?? 0;
            int str = army.EvalStrength();
            return $"{name} ({units} units, STR:{str})";
        }

        // --- Unit Transfer for Buddy Refill ---

        /// <summary>
        /// Transfer units from non-first-pair armies to the target army.
        /// Only transfers if both armies are in the same castle/garrison.
        /// </summary>
        public static void RefillFromNonPairArmies(Logic.Army target, Logic.Kingdom kingdom)
        {
            if (target == null || kingdom == null || kingdom.armies == null) return;
            if (target.castle == null) return; // Must be in a castle to transfer

            foreach (var donor in kingdom.armies)
            {
                if (donor == null || !donor.IsValid() || donor == target) continue;
                if (donor.IsHiredMercenary()) continue;

                // Only take from armies NOT in the first buddy pair
                if (BuddySystem.IsFirstPair(donor, kingdom)) continue;

                // Donor must be in the same castle
                if (donor.castle != target.castle) continue;

                // Transfer units from donor to target
                if (donor.units == null) continue;
                for (int i = donor.units.Count - 1; i >= 0; i--)
                {
                    if (target.units != null && target.units.Count >= GameBalance.FullArmySize)
                        break;

                    var unit = donor.units[i];
                    if (unit == null) continue;

                    target.TransferUnit(target, unit, null);
                    AIOverhaulPlugin.LogDebug($"[Buddy] Transferred unit from {DescribeArmy(donor)} to {DescribeArmy(target)}", LogCategory.Military, kingdom);
                }
            }
        }

        // --- Existing Helpers (kept) ---

        public static bool HasTwoReadyArmies(Logic.Kingdom kingdom)
        {
            if (kingdom?.armies == null || kingdom.armies.Count < GameBalance.FirstTwoArmiesCount)
                return false;

            int readyArmies = 0;
            for (int i = 0; i < Math.Min(GameBalance.FirstTwoArmiesCount, kingdom.armies.Count); i++)
            {
                var army = kingdom.armies[i];
                if (army == null) continue;

                bool isFull = army.units.Count >= GameBalance.FullArmySize;
                int strength = army.EvalStrength();
                bool hasStrength = strength >= GameBalance.MinArmyStrengthForFortification;

                if (isFull && hasStrength)
                    readyArmies++;
            }

            return readyArmies >= GameBalance.FirstTwoArmiesCount;
        }

        public static Logic.Realm FindNearbyEnemyRealmInDisorder(Logic.Kingdom ourKingdom, int maxDistance)
        {
            if (ourKingdom == null || ourKingdom.game == null) return null;

            foreach (var enemy in ourKingdom.game.kingdoms)
            {
                if (enemy == null || enemy == ourKingdom || enemy.IsDefeated()) continue;
                if (!ourKingdom.IsEnemy(enemy)) continue;

                if (enemy.realms != null)
                {
                    foreach (var realm in enemy.realms)
                    {
                        if (realm == null) continue;
                        if (!realm.IsDisorder()) continue;

                        var castle = realm.castle;
                        if (castle == null || castle.battle != null) continue;

                        if (ourKingdom.IsRealmWithinDistance(realm, maxDistance, out var dist))
                            return realm;

                        //AIOverhaulPlugin.LogDebug($"[ThinkPlunder] {enemy.Name}'s realm {realm.name} is in disorder but too far ({dist}>{maxDistance} provinces)", LogCategory.Military, ourKingdom);
                    }
                }
            }

            return null;
        }
    }
}
