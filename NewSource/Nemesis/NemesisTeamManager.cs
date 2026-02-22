using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Logic;

namespace AIOverhaul
{
    /// <summary>
    /// Manages the Nemesis Team system for multiplayer coop.
    /// Selects N neighboring AI kingdoms to form a coordinated rival faction against human coop players.
    /// </summary>
    public static class NemesisTeamManager
    {
        public static HashSet<int> NemesisKingdomIds { get; } = new HashSet<int>();
        public static HashSet<int> HumanTeamKingdomIds { get; } = new HashSet<int>();
        public static bool IsInitialized { get; private set; }
        public static bool VerboseLogging { get; set; } = true;

        /// <summary>
        /// Force a nemesis team of this size even in single player (0 = disabled, use normal coop detection).
        /// Set via -nemesis-size X command line argument.
        /// </summary>
        public static int ForcedTeamSize { get; set; }

        public static void LogVerbose(string message, Logic.Kingdom kingdom = null)
        {
            if (!VerboseLogging) return;
            AIOverhaulPlugin.LogInfo($"{message}", LogCategory.Nemesis, kingdom);
        }

        // --- Query Methods ---

        public static bool IsNemesis(Logic.Kingdom k)
        {
            if (k == null || k.IsDefeated()) return false;
            return NemesisKingdomIds.Contains(k.id);
        }

        public static bool IsHumanTeam(Logic.Kingdom k)
        {
            if (k == null) return false;
            return HumanTeamKingdomIds.Contains(k.id);
        }

        public static bool AreNemesisTeammates(Logic.Kingdom a, Logic.Kingdom b)
        {
            if (a == null || b == null || a == b) return false;
            return NemesisKingdomIds.Contains(a.id) && NemesisKingdomIds.Contains(b.id);
        }

        /// <summary>
        /// Find a nemesis teammate that is missing a trade agreement, NAP, or defensive pact with the actor.
        /// Returns the first teammate found missing any pact, or null.
        /// </summary>
        public static Logic.Kingdom FindTeammateNeedingPact(Logic.Kingdom actor, Game game, out string pactType)
        {
            pactType = null;
            if (actor == null || game == null || !NemesisKingdomIds.Contains(actor.id)) return null;

            foreach (int id in NemesisKingdomIds)
            {
                if (id == actor.id) continue;
                Logic.Kingdom teammate = game.GetKingdom(id);
                if (teammate == null || teammate.IsDefeated()) continue;

                // Don't try diplomacy if at war with each other (ThinkWhitePeace will resolve)
                if (actor.IsEnemy(teammate)) continue;

                if (!actor.HasTradeAgreement(teammate))
                {
                    pactType = DiplomacyConstants.SignTrade;
                    LogVerbose($"Teammate {teammate.Name} needs Trade with {actor.Name}", actor);
                    return teammate;
                }

                if (!actor.HasStance(teammate, RelationUtils.Stance.NonAggression))
                {
                    pactType = DiplomacyConstants.SignNonAggression;
                    LogVerbose($"Teammate {teammate.Name} needs NAP with {actor.Name}", actor);
                    return teammate;
                }

                if (!actor.IsAlly(teammate))
                {
                    pactType = DiplomacyConstants.OfferJoinInDefensivePact;
                    LogVerbose($"Teammate {teammate.Name} needs Defensive Pact with {actor.Name}", actor);
                    return teammate;
                }
            }

            LogVerbose($"All teammates have full pacts with {actor.Name}", actor);
            return null;
        }

        // --- Aligned Diplomacy ---

        /// <summary>
        /// Find a kingdom that a teammate already has a trade agreement with, but actor does not.
        /// This makes nemesis kingdoms converge on the same trade partners.
        /// </summary>
        public static Logic.Kingdom FindTeamAlignedTradeTarget(Logic.Kingdom actor, Game game)
        {
            if (actor == null || game == null || !NemesisKingdomIds.Contains(actor.id)) return null;

            foreach (int teammateId in NemesisKingdomIds)
            {
                if (teammateId == actor.id) continue;
                Logic.Kingdom teammate = game.GetKingdom(teammateId);
                if (teammate == null || teammate.IsDefeated()) continue;

                foreach (var k in game.kingdoms)
                {
                    if (k == null || k.IsDefeated() || k.id == actor.id) continue;
                    if (NemesisKingdomIds.Contains(k.id)) continue;
                    if (HumanTeamKingdomIds.Contains(k.id)) continue;
                    if (actor.IsEnemy(k)) continue;
                    if (actor.HasTradeAgreement(k)) continue;
                    if (teammate.HasTradeAgreement(k))
                    {
                        LogVerbose($"Aligned trade target: {k.Name} (teammate {teammate.Name} already has trade)", actor);
                        return k;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Find a kingdom that a teammate already has a NAP with, but actor does not.
        /// </summary>
        public static Logic.Kingdom FindTeamAlignedNAPTarget(Logic.Kingdom actor, Game game)
        {
            if (actor == null || game == null || !NemesisKingdomIds.Contains(actor.id)) return null;

            foreach (int teammateId in NemesisKingdomIds)
            {
                if (teammateId == actor.id) continue;
                Logic.Kingdom teammate = game.GetKingdom(teammateId);
                if (teammate == null || teammate.IsDefeated()) continue;

                foreach (var k in game.kingdoms)
                {
                    if (k == null || k.IsDefeated() || k.id == actor.id) continue;
                    if (NemesisKingdomIds.Contains(k.id)) continue;
                    if (HumanTeamKingdomIds.Contains(k.id)) continue;
                    if (actor.IsEnemy(k)) continue;
                    if (actor.HasStance(k, RelationUtils.Stance.NonAggression)) continue;
                    if (teammate.HasStance(k, RelationUtils.Stance.NonAggression))
                    {
                        LogVerbose($"Aligned NAP target: {k.Name} (teammate {teammate.Name} already has NAP)", actor);
                        return k;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns teammate ids sorted ascending by realm count (fewest realms first = weakest).
        /// </summary>
        public static List<int> GetTeammatesSortedByRealmCount(Game game)
        {
            var list = new List<int>(NemesisKingdomIds);
            list.Sort((a, b) =>
            {
                var ka = game.GetKingdom(a);
                var kb = game.GetKingdom(b);
                int ra = ka?.realms?.Count ?? 0;
                int rb = kb?.realms?.Count ?? 0;
                return ra.CompareTo(rb);
            });
            return list;
        }

        /// <summary>
        /// Checks if any teammate considers kingdomId an enemy (for rebel detection).
        /// </summary>
        public static bool IsTeammateEnemy(int kingdomId, Logic.Kingdom actor)
        {
            if (actor == null || !NemesisKingdomIds.Contains(actor.id)) return false;

            foreach (int teammateId in NemesisKingdomIds)
            {
                if (teammateId == actor.id) continue;
                Logic.Kingdom teammate = actor.game.GetKingdom(teammateId);
                if (teammate == null || teammate.IsDefeated()) continue;
                if (teammate.IsEnemy(kingdomId)) return true;
            }
            return false;
        }

        // --- War Coordination ---

        /// <summary>
        /// Force-joins all nemesis teammates into each other's wars — no diplomacy, no cooldowns.
        /// </summary>
        public static void SyncWars(Game game)
        {
            if (!IsInitialized || game == null) return;

            foreach (int id in NemesisKingdomIds)
            {
                Logic.Kingdom k = game.GetKingdom(id);
                if (k == null || k.IsDefeated() || k.wars == null) continue;

                // Copy list — war.Join may modify it
                var wars = new List<War>(k.wars);
                foreach (var war in wars)
                {
                    if (war == null) continue;
                    int side = war.GetSide(k);
                    if (side < 0) continue;

                    foreach (int teammateId in NemesisKingdomIds)
                    {
                        if (teammateId == id) continue;
                        Logic.Kingdom teammate = game.GetKingdom(teammateId);
                        if (teammate == null || teammate.IsDefeated()) continue;
                        if (war.CanJoin(teammate, side))
                        {
                            war.Join(teammate, k, War.InvolvementReason.DefensivePactActivated);
                            LogVerbose($"{teammate.Name} force-joined {k.Name}'s war (side {side})", teammate);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the first war any nemesis teammate is fighting, or null.
        /// </summary>
        public static War FindTeammateWar(Logic.Kingdom actor, Game game)
        {
            if (!IsInitialized || actor == null || game == null) return null;

            foreach (int id in NemesisKingdomIds)
            {
                if (id == actor.id) continue;
                Logic.Kingdom teammate = game.GetKingdom(id);
                if (teammate == null || teammate.IsDefeated() || teammate.wars == null) continue;
                if (teammate.wars.Count > 0) return teammate.wars[0];
            }
            return null;
        }

        /// <summary>
        /// Returns the combined GetTotalPower() of all living nemesis kingdoms.
        /// </summary>
        public static float GetTeamPower(Game game)
        {
            if (!IsInitialized || game == null) return 0f;

            float total = 0f;
            foreach (int id in NemesisKingdomIds)
            {
                Logic.Kingdom k = game.GetKingdom(id);
                if (k != null && !k.IsDefeated())
                    total += k.GetTotalPower();
            }
            return total;
        }

        // --- Selection Algorithm ---

        public static void Initialize(Game game)
        {
            if (game == null || game.kingdoms == null) return;

            Clear();

            // Try restoring from saved kingdom vars first (reload/save-load path)
            RestoreFromVars(game);
            if (IsInitialized) return;

            // Step 1: Detect coop - find human players on the same team (or use forced size for SP testing)
            var humanKingdoms = DetectCoopPlayers(game);
            int targetSize;

            if (ForcedTeamSize > 0)
            {
                // Forced mode: treat all player kingdoms as the "human team" even in single player
                humanKingdoms = game.kingdoms.FindAll(k => k != null && k.is_player && !k.IsDefeated());
                if (humanKingdoms.Count == 0)
                {
                    AIOverhaulPlugin.LogInfo("Nemesis: Forced mode but no player kingdoms found. Skipping.", LogCategory.Nemesis);
                    return;
                }
                targetSize = ForcedTeamSize;
                AIOverhaulPlugin.LogInfo($"FORCED mode: creating team of {targetSize} against {humanKingdoms.Count} player(s)", LogCategory.Nemesis);
            }
            else
            {
                if (humanKingdoms.Count < 2)
                {
                    AIOverhaulPlugin.LogInfo("Nemesis: No coop detected (need 2+ humans on same team). Skipping.", LogCategory.Nemesis);
                    return;
                }
                targetSize = humanKingdoms.Count;
            }

            foreach (var hk in humanKingdoms)
                HumanTeamKingdomIds.Add(hk.id);
            AIOverhaulPlugin.LogInfo($"Detected {humanKingdoms.Count} coop players: {string.Join(", ", humanKingdoms.Select(k => k.Name))}", LogCategory.Nemesis);

            // Step 2: BFS from human kingdoms to compute hop distance to every kingdom
            var distanceFromHumans = new Dictionary<int, int>();
            var humanNeighborIds = new HashSet<int>();
            var bfsQueue = new Queue<int>();
            foreach (var hk in humanKingdoms)
            {
                distanceFromHumans[hk.id] = 0;
                bfsQueue.Enqueue(hk.id);
            }
            while (bfsQueue.Count > 0)
            {
                int curId = bfsQueue.Dequeue();
                int curDist = distanceFromHumans[curId];
                Logic.Kingdom cur = game.GetKingdom(curId);
                if (cur?.neighbors == null) continue;
                foreach (var n in cur.neighbors)
                {
                    if (n is Logic.Kingdom nk && !nk.IsDefeated() && !distanceFromHumans.ContainsKey(nk.id))
                    {
                        distanceFromHumans[nk.id] = curDist + 1;
                        bfsQueue.Enqueue(nk.id);
                    }
                }
            }
            // Build direct-neighbor set (distance == 1) for backwards compat
            foreach (var kvp in distanceFromHumans)
            {
                if (kvp.Value == 1)
                {
                    Logic.Kingdom nk = game.GetKingdom(kvp.Key);
                    if (nk != null && !nk.is_player) humanNeighborIds.Add(kvp.Key);
                }
            }

            // Step 3: Score AI candidates
            var aiKingdoms = game.kingdoms.Where(k => k != null && !k.is_player && !k.IsDefeated()).ToList();
            var scores = new Dictionary<int, float>();

            foreach (var ai in aiKingdoms)
            {
                float score = 0f;
                float neighborPenalty = 0f, powerScore = 0f, aiNeighborScore = 0f, realmScore = 0f, distScore = 0f;

                // Penalty for directly neighboring a human player
                if (humanNeighborIds.Contains(ai.id))
                {
                    score -= GameBalance.NemesisHumanNeighborPenalty;
                    neighborPenalty = -GameBalance.NemesisHumanNeighborPenalty;
                }

                // Distance scoring: bonus for ideal range, penalty outside it
                int dist = distanceFromHumans.ContainsKey(ai.id) ? distanceFromHumans[ai.id] : 999;
                if (dist >= GameBalance.NemesisIdealDistanceMin && dist <= GameBalance.NemesisIdealDistanceMax)
                {
                    distScore = GameBalance.NemesisIdealDistanceBonus;
                }
                else if (dist < GameBalance.NemesisIdealDistanceMin)
                {
                    distScore = -(GameBalance.NemesisIdealDistanceMin - dist) * GameBalance.NemesisCloseDistancePenaltyPerHop;
                }
                else
                {
                    distScore = -(dist - GameBalance.NemesisIdealDistanceMax) * GameBalance.NemesisFarDistancePenaltyPerHop;
                }
                score += distScore;

                // Prefer strong kingdoms
                float power = ai.GetTotalPower();
                powerScore = power * GameBalance.NemesisPowerWeight;
                score += powerScore;

                // Prefer kingdoms with many AI neighbors (easier to cluster)
                int aiNeighborCount = 0;
                if (ai.neighbors != null)
                {
                    foreach (var n in ai.neighbors)
                    {
                        if (n is Logic.Kingdom nk && !nk.is_player && !nk.IsDefeated())
                        {
                            score += GameBalance.NemesisAINeighborBonus;
                            aiNeighborScore += GameBalance.NemesisAINeighborBonus;
                            aiNeighborCount++;
                        }
                    }
                }

                // Prefer established kingdoms (more realms)
                if (ai.realms != null)
                {
                    realmScore = ai.realms.Count * GameBalance.NemesisRealmCountBonus;
                    score += realmScore;
                }

                scores[ai.id] = score;
                LogVerbose($"Score {ai.Name}: {score:F1} (dist:{dist}, distScore:{distScore:F1}, power:{powerScore:F1}, aiNeighbors:{aiNeighborCount}x{GameBalance.NemesisAINeighborBonus}={aiNeighborScore:F1}, realms:{realmScore:F1}, humanPenalty:{neighborPenalty:F1})");
            }

            // Step 4: Greedy cluster expansion
            if (scores.Count == 0)
            {
                AIOverhaulPlugin.LogInfo("No AI kingdoms available for nemesis team.", LogCategory.Nemesis);
                return;
            }

            // Start with best-scored seed
            int seedId = scores.OrderByDescending(kv => kv.Value).First().Key;
            var selected = new List<int> { seedId };
            LogVerbose($"Seed: {game.GetKingdom(seedId)?.Name} (score {scores[seedId]:F1})");

            while (selected.Count < targetSize)
            {
                // Find best-scored direct neighbor of any selected kingdom that isn't already selected
                int bestCandidate = -1;
                float bestScore = float.MinValue;

                foreach (int selId in selected)
                {
                    Logic.Kingdom selK = game.GetKingdom(selId);
                    if (selK?.neighbors == null) continue;

                    foreach (var n in selK.neighbors)
                    {
                        if (!(n is Logic.Kingdom nk)) continue;
                        if (nk.is_player || nk.IsDefeated()) continue;
                        if (selected.Contains(nk.id)) continue;
                        if (!scores.ContainsKey(nk.id)) continue;

                        // Base score + cluster adjacency bonus
                        float candidateScore = scores[nk.id];
                        foreach (int existingId in selected)
                        {
                            Logic.Kingdom existing = game.GetKingdom(existingId);
                            if (existing?.neighbors == null) continue;
                            foreach (var en in existing.neighbors)
                            {
                                if (en is Logic.Kingdom enk && enk.id == nk.id)
                                {
                                    candidateScore += GameBalance.NemesisClusterAdjacencyBonus;
                                    break;
                                }
                            }
                        }

                        if (candidateScore > bestScore)
                        {
                            bestScore = candidateScore;
                            bestCandidate = nk.id;
                        }
                    }
                }

                if (bestCandidate < 0)
                {
                    LogVerbose($"Cluster expansion stopped: no more valid neighbors (have {selected.Count}/{targetSize})");
                    break;
                }
                LogVerbose($"Adding {game.GetKingdom(bestCandidate)?.Name} to cluster (score {bestScore:F1}, step {selected.Count + 1}/{targetSize})");
                selected.Add(bestCandidate);
            }

            // Step 5: Fallback - need at least MinTeamSize
            if (selected.Count < GameBalance.NemesisMinTeamSize)
            {
                AIOverhaulPlugin.LogInfo($"Could only select {selected.Count} kingdoms (need {GameBalance.NemesisMinTeamSize}). Skipping.", LogCategory.Nemesis);
                return;
            }

            // Step 6: Persist
            foreach (int id in selected)
            {
                NemesisKingdomIds.Add(id);
                Logic.Kingdom k = game.GetKingdom(id);
                if (k != null)
                {
                    k.SetVar(CampaignVarNames.NemesisTeam, new Value(1));
                    // Also add to EnhancedKingdomIds so they get all Enhanced AI behaviors
                    if (!AIOverhaulPlugin.EnhancedKingdomIds.Contains(id))
                        AIOverhaulPlugin.EnhancedKingdomIds.Add(id);
                }
            }

            IsInitialized = true;
            var selectedNames = selected.Select(id => game.GetKingdom(id)?.Name ?? id.ToString());
            AIOverhaulPlugin.LogInfo($"Selected nemesis team: {string.Join(", ", selectedNames)}", LogCategory.Nemesis);
            PersistToCampaign(game);
        }

        /// <summary>
        /// Detect coop players: find 2+ human players sharing the same team value.
        /// </summary>
        static List<Logic.Kingdom> DetectCoopPlayers(Game game)
        {
            var result = new List<Logic.Kingdom>();
            if (game.multiplayer == null || game.campaign == null) return result;

            // Gather all human player kingdoms and their team assignments
            var playerKingdoms = game.kingdoms.Where(k => k != null && k.is_player && !k.IsDefeated()).ToList();
            if (playerKingdoms.Count < 2) return result;

            // Try to detect team from campaign data: check if all players share the same team
            // In coop, players share a team value. We check campaign.playerDataPersistent for team info.
            var teamGroups = new Dictionary<int, List<Logic.Kingdom>>();

            foreach (var pk in playerKingdoms)
            {
                // Try to read team value from campaign player data
                int team = GetPlayerTeam(game, pk);
                if (!teamGroups.ContainsKey(team))
                    teamGroups[team] = new List<Logic.Kingdom>();
                teamGroups[team].Add(pk);
            }

            // Find the largest team with 2+ members
            foreach (var kvp in teamGroups)
            {
                LogVerbose($"Team {kvp.Key}: {string.Join(", ", kvp.Value.Select(k => k.Name))} ({kvp.Value.Count} players)");
                if (kvp.Value.Count >= 2 && kvp.Value.Count > result.Count)
                    result = kvp.Value;
            }

            return result;
        }

        /// <summary>
        /// Get the team number for a player kingdom from campaign data.
        /// Reads from the campaign "team" var on each player slot.
        /// Falls back to team 0 if not found (treats all players as same team).
        /// </summary>
        static int GetPlayerTeam(Game game, Logic.Kingdom playerKingdom)
        {
            if (game.campaign?.playerDataPersistent == null) return 0;

            for (int i = 0; i < game.campaign.playerDataPersistent.Length; i++)
            {
                var pd = game.campaign.playerDataPersistent[i];
                if (pd == null) continue;

                string kingdomName = game.campaign.GetKingdomName(i);
                if (!string.IsNullOrEmpty(kingdomName) && kingdomName == playerKingdom.Name)
                {
                    // Read team from campaign vars
                    Value teamVar = pd.GetVar(CampaignVarNames.Team);
                    if (teamVar.type == Value.Type.Int) return teamVar;
                    return 0;
                }
            }

            return 0; // Default team
        }

        // --- Restore from save/load ---

        public static void RestoreFromVars(Game game)
        {
            if (game == null || game.kingdoms == null) return;
            if (IsInitialized) return; // Already initialized this session

            // Try campaignData first — this is reliable on MP reload because campaignData is deserialized before gameplay starts
            if (game.campaign?.campaignData != null)
            {
                Value cdVal = game.campaign.campaignData.GetVar(CampaignVarNames.NemesisTeamIds);
                if (cdVal.is_string)
                {
                    string idsStr = cdVal.String();
                    if (!string.IsNullOrEmpty(idsStr))
                    {
                        var restored = new List<string>();
                        foreach (string token in idsStr.Split(','))
                        {
                            if (int.TryParse(token.Trim(), out int id))
                            {
                                Logic.Kingdom k = game.GetKingdom(id);
                                if (k != null && !k.IsDefeated())
                                {
                                    NemesisKingdomIds.Add(id);
                                    if (!AIOverhaulPlugin.EnhancedKingdomIds.Contains(id))
                                        AIOverhaulPlugin.EnhancedKingdomIds.Add(id);
                                    // Re-apply kingdom var for client sync
                                    k.SetVar(CampaignVarNames.NemesisTeam, new Value(1));
                                    restored.Add(k.Name);
                                }
                            }
                        }

                        if (NemesisKingdomIds.Count >= GameBalance.NemesisMinTeamSize)
                        {
                            HumanTeamKingdomIds.Clear();
                            var humanKingdoms = DetectCoopPlayers(game);
                            foreach (var hk in humanKingdoms)
                                HumanTeamKingdomIds.Add(hk.id);

                            IsInitialized = true;
                            AIOverhaulPlugin.LogInfo($"Restored nemesis team from campaignData: {string.Join(", ", restored)}", LogCategory.Nemesis);
                            return;
                        }
                        LogVerbose($"campaignData had {NemesisKingdomIds.Count} IDs but below min size, falling through to kingdom vars");
                        NemesisKingdomIds.Clear();
                    }
                }
            }

            // Fallback: scan kingdom vars (works for single-player saves or legacy saves without campaignData)
            int scanned = 0;
            var restoredFromVars = new List<string>();
            foreach (var k in game.kingdoms)
            {
                if (k == null || k.IsDefeated()) continue;
                scanned++;

                Value v = k.GetVar(CampaignVarNames.NemesisTeam);
                if (v.type == Value.Type.Int && (int)v == 1)
                {
                    NemesisKingdomIds.Add(k.id);
                    if (!AIOverhaulPlugin.EnhancedKingdomIds.Contains(k.id))
                        AIOverhaulPlugin.EnhancedKingdomIds.Add(k.id);
                    restoredFromVars.Add(k.Name);
                }
            }
            LogVerbose($"RestoreFromVars: scanned {scanned} kingdoms, found {restoredFromVars.Count} nemesis members");

            // Rebuild human team
            HumanTeamKingdomIds.Clear();
            var humanKingdomsFallback = DetectCoopPlayers(game);
            foreach (var hk in humanKingdomsFallback)
                HumanTeamKingdomIds.Add(hk.id);

            if (NemesisKingdomIds.Count >= GameBalance.NemesisMinTeamSize)
            {
                IsInitialized = true;
                AIOverhaulPlugin.LogInfo($"Restored nemesis team from save: {string.Join(", ", restoredFromVars)}", LogCategory.Nemesis);
                // Backfill campaignData so future reloads use the fast path
                PersistToCampaign(game);
            }
        }

        // --- Info ---

        public static string GetInfoString()
        {
            if (!IsInitialized || NemesisKingdomIds.Count == 0)
                return "No nemesis team active.";

            var game = AIOverhaulPlugin.CurrentGame;
            if (game == null) return "No game active.";

            var sb = new StringBuilder();
            sb.AppendLine("=== NEMESIS TEAM ===");

            float totalPower = 0f;
            foreach (int id in NemesisKingdomIds)
            {
                Logic.Kingdom k = game.GetKingdom(id);
                if (k == null) continue;

                float power = k.GetTotalPower();
                totalPower += power;
                string status = k.IsDefeated() ? "DEFEATED" : (k.wars != null && k.wars.Count > 0 ? $"AT WAR ({k.wars.Count})" : "Peace");

                // Check if at war with any human
                bool warWithHuman = false;
                foreach (int hid in HumanTeamKingdomIds)
                {
                    Logic.Kingdom hk = game.GetKingdom(hid);
                    if (hk != null && k.IsEnemy(hk)) { warWithHuman = true; break; }
                }

                string humanWarTag = warWithHuman ? " [VS HUMAN]" : "";
                sb.AppendLine($"  {k.Name} - STR:{power:F0} - {status}{humanWarTag}");
            }

            sb.AppendLine($"  TOTAL POWER: {totalPower:F0}");

            // Human team info
            sb.AppendLine("--- Human Team ---");
            foreach (int id in HumanTeamKingdomIds)
            {
                Logic.Kingdom k = game.GetKingdom(id);
                if (k != null)
                    sb.AppendLine($"  {k.Name} - STR:{k.GetTotalPower():F0}");
            }

            return sb.ToString();
        }

        // --- Clear ---

        public static void Clear()
        {
            NemesisKingdomIds.Clear();
            HumanTeamKingdomIds.Clear();
            IsInitialized = false;
        }

        static void PersistToCampaign(Game game)
        {
            if (game?.campaign?.campaignData == null || NemesisKingdomIds.Count == 0) return;
            string ids = string.Join(",", NemesisKingdomIds);
            game.campaign.campaignData.Set(CampaignVarNames.NemesisTeamIds, ids);
            LogVerbose($"Persisted nemesis team to campaignData: {ids}");
        }

        public static void ClearCampaignVars(Game game)
        {
            if (game?.campaign?.campaignData == null) return;
            game.campaign.campaignData.Set(CampaignVarNames.NemesisTeamIds, Value.Unknown);
            LogVerbose("Cleared nemesis team from campaignData");
        }
    }
}
