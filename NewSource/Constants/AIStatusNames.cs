using System;

namespace AIOverhaul
{
    /// <summary>
    /// Constants for Army AI status strings used with SetAIStatus() and Send().
    /// </summary>
    public static class AIStatusNames
    {
        // Combat/Attack statuses
        public const string Attack = "attack";
        public const string AttackRealm = "attack_realm";
        public const string AttackArmy = "attack_army";
        public const string AttackDesperate = "attack_desperate";
        public const string AttackCastle = "attack_castle";
        public const string Reinforce = "reinforce";
        public const string ReinforceDesperate = "reinforce_desperate";

        // Defense statuses
        public const string Defend = "defend";
        public const string DefendRealm = "defend_realm";
        public const string EnemiesTooStrong = "enemies_too_strong";

        // Movement/Logistics
        public const string GoHome = "go_home";
        public const string GoInside = "go_inside";
        public const string Resupply = "resupply";
        public const string Refill = "refill";
        public const string Resupplied = "resupplied";
        public const string GoToMercenary = "go_to_mercenary";

        // Plunder/Raid
        public const string Plunder = "plunder";

        // Waiting states
        public const string Idle = "idle";
        public const string WaitOthers = "wait_others";
        public const string WaitOrders = "wait_orders";
        public const string WaitForBattle = "wait_for_battle";

        // Special
        public const string HelpWithRebels = "help_with_rebels";

        // --- AIOverhaul Custom Statuses (Non-Vanilla) ---
        public const string FollowLeader = "follow_leader";                 // Follower following leader's target
        public const string SallyOut = "sally_out";                         // Garrison sallying out from siege
        public const string SiegeRecall = "siege_recall";                   // Army recalled to defend own siege
    }
}
