namespace AIOverhaul
{
    public class KingdomAI_ThinkMilitary
    {
        // 1. CalcThreat() — evaluates every realm (own and enemy) and computes a Threat with a level:                                                                                                               
        // - Safe — no danger                                                                                                                                                                                      
        //     - Border — non-enemy neighbor nearby
        //     - Neighbors — enemy armies in adjacent realms
        //     - Invaded — enemy armies inside your realm
        //     - Siege — your castle is under siege
        // - Attack — it's an enemy realm (offensive target)
        //
        // Each threat gets min_needed / max_needed troop strength values. For own realms (defense), it's enemies * 1.5. Threats are sorted by priority (highest threat first).
        //
        // 2. ThinkThreats() → ThinkThreat() → AssignArmy() — iterates threats in priority order and assigns armies via army.tgt_realm. This is where an army attacking an enemy realm gets pulled back to defend:
        // CanReassign checks if the army can be taken from its current threat (the offensive target) and reassigned to a higher-priority defensive threat.
        //
        // 3. ThinkArmies() → ThinkArmy() — only after realm assignments are decided, each army executes its local behavior (move toward tgt_realm, fight within the realm, etc.)
        //

    }
}
