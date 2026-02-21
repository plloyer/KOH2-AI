using System.Collections;
using HarmonyLib;
using Logic;

namespace AIOverhaul
{
    [HarmonyPatch(typeof(KingdomAI), "ThinkThreats")]
    public class KingdomAI_ThinkThreats
    {
        static bool Prefix(KingdomAI __instance, ref IEnumerator __result)
        {
            if (__instance == null || __instance.kingdom == null) return true;
            if (!AIOverhaulPlugin.IsEnhancedAI(__instance.kingdom)) return true;
            __result = ThinkThreatsOverride(__instance);
            return false;
        }

        // =====================================================================
        // Main coroutine — mirrors vanilla ThinkThreats: 2-pass loop + cleanup
        // =====================================================================

        static IEnumerator ThinkThreatsOverride(KingdomAI ai)
        {
            for (int pass = 0; pass <= 1; pass++)
            {
                for (int i = 0; i < KingdomAI.threats.Count; i++)
                {
                    AssignArmiesToThreat(ai, KingdomAI.threats[i], pass);
                    yield return null;
                }
            }
            CleanupStaleAssignments(ai);
        }

        // =====================================================================
        // Per-threat assignment — mirrors vanilla ThinkThreat(t, pass)
        // =====================================================================

        static void AssignArmiesToThreat(KingdomAI ai, KingdomAI.Threat t, int pass)
        {
            float threshold = (pass != 0) ? t.max_needed : t.min_needed;
            while (t.assigned.eval < threshold && TryAssignArmy(ai, t, pass)) { }
        }

        // =====================================================================
        // Find closest eligible army and assign — mirrors vanilla AssignArmy
        // =====================================================================

        static bool TryAssignArmy(KingdomAI ai, KingdomAI.Threat threat, int pass)
        {
            Logic.Army best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < ai.kingdom.armies.Count; i++)
            {
                Logic.Army a = ai.kingdom.armies[i];
                int eval = -1;
                if (!CanAssign(ai, a, threat, ref eval, pass)) continue;
                float dist = a.position.SqrDist(threat.realm.castle.position);
                if (dist < bestDist) { best = a; bestDist = dist; }
            }

            if (best == null) return false;

            ai.GetThreat(best.tgt_realm)?.assigned.Del(best);
            best.tgt_realm = threat.realm;
            threat.assigned.Add(best);
            return true;
        }

        // =====================================================================
        // Eligibility checks — mirrors vanilla CanAssign (clean early-return)
        // =====================================================================

        static bool CanAssign(KingdomAI ai, Logic.Army army, KingdomAI.Threat threat, ref int eval, int pass)
        {
            if (threat?.realm == null) return false;
            if (army?.realm_in == null) return false;
            if (army.battle != null) return false;
            if (army.tgt_realm == threat.realm) return false;
            if (KingdomAI.IsLow(army)) return false;

            if (threat.level < KingdomAI.Threat.Level.Attack && !KingdomAI.IsFull(army)) return false;

            if (threat.level == KingdomAI.Threat.Level.Attack)
            {
                if (KingdomAI.IsLowSupplies(army)) return false;
                if (army.realm_in.GetKingdom() != threat.realm.GetKingdom() && !KingdomAI.IsFull(army)) return false;
                if (ai.TooSoonRetreat(army)) return false;
            }

            if (threat.level < KingdomAI.Threat.Level.Invaded && KingdomAI.IsLowSupplies(army)) return false;

            KingdomAI.Threat curThreat = ai.GetThreat(army.tgt_realm);
            if (curThreat == null) return true;
            if (!CanReassign(army, curThreat, threat, ref eval, pass)) return false;
            return true;
        }

        // =====================================================================
        // Can army leave current threat for a new one? — mirrors vanilla
        // =====================================================================

        static bool CanReassign(Logic.Army army, KingdomAI.Threat curThreat, KingdomAI.Threat newThreat, ref int eval, int pass)
        {
            if (curThreat.reinforceable_battles) return false;
            if (curThreat.max_needed <= 0f) return true;
            if (curThreat.assigned.eval <= curThreat.min_needed) return true;

            bool higherPriority = newThreat.index < curThreat.index;
            float threshold;
            if (pass != 0)
            {
                threshold = higherPriority ? curThreat.min_needed : curThreat.max_needed;
            }
            else
            {
                if (higherPriority) return true;
                threshold = curThreat.min_needed;
            }

            if (curThreat.assigned.eval <= threshold) return false;
            if (eval < 0) eval = army.EvalStrength();
            if (curThreat.assigned.eval - (float)eval <= threshold) return false;
            return true;
        }

        // =====================================================================
        // Remove armies from unsatisfied threats — mirrors vanilla cleanup loop
        // =====================================================================

        static void CleanupStaleAssignments(KingdomAI ai)
        {
            for (int i = 0; i < ai.kingdom.armies.Count; i++)
            {
                Logic.Army army = ai.kingdom.armies[i];
                KingdomAI.Threat threat = ai.GetThreat(army.tgt_realm);
                if (threat != null && (!(threat.max_needed > 0f) || !(threat.assigned.eval >= threat.min_needed)))
                {
                    threat.assigned.Del(army);
                    army.tgt_realm = null;
                }
            }
        }
    }
}
