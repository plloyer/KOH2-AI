using HarmonyLib;
using AIOverhaul.Constants;
using UnityEngine;

namespace AIOverhaul.Patches
{
    [HarmonyPatch(typeof(global::Logic.EmperorOfTheWorld), "StartVote")]
    public static class EmperorOfTheWorld_StartVote
    {
        const string LogPrefix = "[EoW]";

        public static void Postfix(global::Logic.EmperorOfTheWorld __instance)
        {
            try
            {
                // Only act if we are in Spectator Mode
                if (!AIOverhaulPlugin.SpectatorMode) return;

                AIOverhaulPlugin.LogInfo($"{LogPrefix} Spectator Mode detected during Vote Start. Attempting Auto-Resolve...", LogCategory.General, null);

                // Find the player voter index
                int playerVoterIdx = -1;
                global::Logic.Kingdom playerKingdom = null;

                for (int i = 0; i < __instance.voters.Count; i++)
                {
                    if (__instance.voters[i].is_player)
                    {
                        playerVoterIdx = i;
                        playerKingdom = __instance.voters[i];
                        break;
                    }
                }

                if (playerVoterIdx == -1 || playerKingdom == null)
                {
                    AIOverhaulPlugin.LogInfo($"{LogPrefix} Player is not a voter. No action needed.", LogCategory.General, null);
                    return;
                }

                // AI Decision Logic for Player Vote
                int bestCandidateId = -1;
                float bestWeight = -99999f;
                global::Logic.Kingdom bestCandidate = null;

                // Evaluate candidates similar to AI
                foreach (var candidate in __instance.candidates)
                {
                    // Vanilla AI uses CalcVoteWeight
                    int weight = __instance.CalcVoteWeight(playerKingdom, candidate, -1);
                    
                    // Simple logic: Vote for self if candidate, otherwise highest weight
                    if (candidate == playerKingdom)
                    {
                        weight += 1000; // Strong bias to vote for self
                    }

                    if (weight > bestWeight)
                    {
                        bestWeight = weight;
                        bestCandidateId = candidate.id;
                        bestCandidate = candidate;
                    }
                }

                if (bestCandidateId != -1)
                {
                    string candidateName = bestCandidate != null ? bestCandidate.Name : "Unknown";
                    AIOverhaulPlugin.LogInfo($"{LogPrefix} Auto-Voting for {candidateName} (ID: {bestCandidateId}) with weight {bestWeight}.", LogCategory.General, playerKingdom);
                    
                    // Force casting the vote
                    __instance.SetPlayerVote(playerVoterIdx, bestCandidateId);
                    
                    // Force end voting to unpause and proceed
                    __instance.SetEndVoting();
                    
                    AIOverhaulPlugin.LogInfo($"{LogPrefix} Vote finalized and game unpaused.", LogCategory.General, playerKingdom);
                }
                else
                {
                     AIOverhaulPlugin.LogInfo($"{LogPrefix} Could not determine best candidate. Skipping auto-vote.", LogCategory.General, playerKingdom);
                }

            }
            catch (System.Exception ex)
            {
                AIOverhaulPlugin.LogInfo($"{LogPrefix} Exception in Auto-Vote: {ex.Message}", LogCategory.General, null);
            }
        }
    }
}
