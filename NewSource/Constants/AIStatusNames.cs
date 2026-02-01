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
        // Used by the Buddy System and Enhanced AI logic to coordinate and explain decisions.
        
        public const string RetreatHeal = "retreat_heal";                   // Retreating to a friendly castle to heal units
        public const string RetreatHealBuddy = "retreat_heal_buddy";         // Following a buddy who is retreating to heal
        public const string RetreatLowChance = "retreat_low_chance";         // Retreating because win probability is too low
        public const string WaitForBuddy = "wait_for_buddy";                 // Leader waiting for follower to arrive before engaging
        public const string RescueLeaderSiege = "rescue_leader_siege";       // Follower rushing to break a siege on the leader's castle
        public const string JoinLeaderBattle = "join_leader_battle";         // Follower joining a battle the leader already started
        public const string FollowBuddyForce = "follow_buddy_force";         // Follower copying leader's military target
        public const string FollowBuddyMove = "follow_buddy_move";           // Follower following leader's movement into enemy territory
        public const string JoinLeaderTerritory = "join_leader_territory";   // Follower joining leader who is stationary in enemy territory
        public const string RescueBuddy = "rescue_buddy";                    // Leader returning to help a follower who is in battle
        public const string AttackSallyOut = "attack";                       // Specifically used when sallying out (vanilla string, but logic is custom)
    }
}
