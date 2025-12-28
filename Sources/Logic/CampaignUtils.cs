using System;
using System.Collections.Generic;
using System.IO;

namespace Logic;

public static class CampaignUtils
{
	public static string DEFAULT_PLAYER_ID = "player_id";

	public static void PickRandomKingdoms(Game game, Campaign campaign, DT.Field rule)
	{
		if (game == null || game.campaign == null || rule == null || !rule.key.Contains("random"))
		{
			return;
		}
		string text = campaign.GetVar("main_goal");
		bool flag = (int)campaign.GetVar("kingdom_size") == 0;
		int num = campaign.GetVar("realms_goal");
		IsFFA(campaign);
		bool flag2 = rule.key.Contains("province");
		int[] perTeamPlayerCount = GetPerTeamPlayerCount(campaign);
		int activePlayerCount = GetActivePlayerCount(campaign);
		int num2 = int.MaxValue;
		WeightedRandom<Kingdom> weightedRandom = new WeightedRandom<Kingdom>(game.kingdoms.Count);
		if (text == "HaveXRealms")
		{
			int num3 = 1;
			for (int i = 0; i < perTeamPlayerCount.Length; i++)
			{
				if (num3 < perTeamPlayerCount[i])
				{
					num3 = perTeamPlayerCount[i];
				}
			}
			num2 = (int)Math.Max(1f, (float)num / (float)num3 / 2f);
		}
		while (true)
		{
			weightedRandom.Clear();
			if (flag2)
			{
				for (int j = 0; j < game.realms.Count; j++)
				{
					Kingdom kingdom = game.GetKingdom(game.realms[j].name);
					if (kingdom != null && IsPlayerEligible(game, kingdom))
					{
						weightedRandom.AddOption(kingdom, 1f);
					}
				}
			}
			else
			{
				int num4 = num2 / 2;
				for (int k = 0; k < game.kingdoms.Count; k++)
				{
					Kingdom kingdom2 = game.kingdoms[k];
					if (kingdom2.realms.Count != 0 && IsPlayerEligible(game, kingdom2) && (!flag || kingdom2.realms.Count <= num2))
					{
						int val = Math.Max(0, num4 - Math.Abs(num4 - kingdom2.realms.Count));
						weightedRandom.AddOption(kingdom2, Math.Max(1, val));
					}
				}
			}
			if (weightedRandom.options.Count >= activePlayerCount)
			{
				break;
			}
			if (num2 < num - 1)
			{
				num2++;
				continue;
			}
			game.Log($"Campaign is for {activePlayerCount} players," + $" but the map only has {weightedRandom.options.Count} possible kingdoms to pick from!");
			return;
		}
		for (int l = 0; l < 6; l++)
		{
			if (game.campaign.IsInCampaign(l))
			{
				Kingdom kingdom3 = weightedRandom.Choose(null, del_option: true);
				if (kingdom3 != null)
				{
					Game.Log($"Player index: {l} kingdom {kingdom3}({kingdom3.realms.Count})", Game.LogType.Message);
					game.campaign.SetPlayerKingdomName(l, kingdom3.Name, null);
				}
			}
		}
		game.teams.Evaluate();
	}

	private static int[] GetPerTeamPlayerCount(Campaign campaign)
	{
		int[] array = new int[GetTeamsCount(campaign)];
		for (int i = 0; i < 6; i++)
		{
			int team = GetTeam(campaign, i);
			if (team >= 0 && campaign.IsInCampaign(i))
			{
				array[team]++;
			}
		}
		return array;
	}

	private static int GetActivePlayerCount(Campaign campaign)
	{
		int num = 0;
		for (int i = 0; i < 6; i++)
		{
			campaign.GetSlotState(i);
			if (campaign.IsInCampaign(i))
			{
				num++;
			}
		}
		return num;
	}

	public static bool ChangeGamePoliticalData(Game game, Campaign campaign, PoliticalData political_data)
	{
		if (game == null || campaign == null || game.campaign?.id != campaign.id || game.state != Game.State.InLobby)
		{
			return false;
		}
		if (game.realms == null)
		{
			Game.Log($"Game ({game.state}) has no realms", Game.LogType.Warning);
			return false;
		}
		string mapName = campaign.GetMapName();
		string period = campaign.GetPeriod();
		string fromSaveID = campaign.GetFromSaveID(check_started: true);
		if (game.map_name == mapName && game.map_period == period && game.map_from_save_id == fromSaveID && (political_data == null || game.political_data_session_time >= political_data.session_time))
		{
			return false;
		}
		using (Game.Profile("ChangeGamePoliticalData"))
		{
			game.map_name = mapName;
			game.map_period = period;
			game.map_from_save_id = fromSaveID;
			game.political_data_session_time = political_data?.session_time ?? 0;
			int i = 0;
			for (int count = game.realms.Count; i < count; i++)
			{
				game.realms[i].Destroy();
			}
			game.realms = null;
			int j = 0;
			for (int count2 = game.kingdoms.Count; j < count2; j++)
			{
				game.kingdoms[j].Destroy();
			}
			game.kingdoms = null;
			game.LoadRealms(game.map_name, game.map_period);
			game.LoadKingdoms(game.map_name, game.map_period);
			game.LoadRealmIDMap(game.map_name);
			ExtractKingdomReligions(game);
			political_data?.ApplyTo(game);
		}
		return true;
	}

	public static void SetCampaignVarGameLoaded(Game game)
	{
		if (game != null && game.campaign != null && !game.campaign.GetLocalPlayerGameLoaded())
		{
			game.campaign.SetLocalPlayerGameLoaded(game_loaded: true);
		}
	}

	public static void ClearGameLoadedCampaignVars(Game game)
	{
		if (game == null || game.campaign == null)
		{
			return;
		}
		for (int i = 0; i < 6; i++)
		{
			if (game.campaign.playerDataNonPersistent[i].GetVar("game_loaded").Bool())
			{
				game.campaign.playerDataNonPersistent[i].Set("game_loaded", false);
			}
		}
	}

	public static List<RemoteVars> GetAllAwaitedPlayers(Game game)
	{
		List<RemoteVars> list = new List<RemoteVars>();
		if (game == null || game.campaign == null)
		{
			Game.Log("GetAllAwaitedPlayers() called with null game or null campaign", Game.LogType.Error);
			return list;
		}
		Campaign campaign = game.campaign;
		for (int i = 0; i < 6; i++)
		{
			string playerID = campaign.GetPlayerID(i);
			if (!string.IsNullOrEmpty(playerID) && !(playerID == THQNORequest.userId) && campaign.GetPlayerRuntimeState(i) > Campaign.RuntimeState.Lobby && !campaign.GetPlayerGameLoaded(i))
			{
				list.Add(campaign.playerDataPersistent[i]);
			}
		}
		return list;
	}

	public static bool AreAllJoinedPlayersInGame(Game game)
	{
		return GetAllAwaitedPlayers(game).Count <= 0;
	}

	public static bool IsPlayerLoaded(Game game, string player_id)
	{
		if (game == null || game.campaign == null)
		{
			Game.Log("IsPlayerLoaded() called with null game or null campaign", Game.LogType.Error);
			return true;
		}
		int playerIndex = game.campaign.GetPlayerIndex(player_id);
		if (playerIndex < 0)
		{
			Game.Log("Cannot find player_index for player with id " + player_id, Game.LogType.Error);
			return true;
		}
		return game.campaign.GetPlayerGameLoaded(playerIndex);
	}

	public static bool IsPlayerLoaded(Game game, int player_index)
	{
		if (game == null || game.campaign == null)
		{
			return false;
		}
		if (player_index < 0)
		{
			return false;
		}
		return game.campaign.GetPlayerGameLoaded(player_index);
	}

	public static void ExtractKingdomReligions(Game game)
	{
		if (game == null || game.religions == null)
		{
			return;
		}
		using (Game.Profile("ExtractKingdomReligions"))
		{
			for (int i = 0; i < game.kingdoms.Count; i++)
			{
				Kingdom kingdom = game.kingdoms[i];
				string text = kingdom.csv_field?.GetString("Religion");
				if (string.IsNullOrEmpty(text))
				{
					text = kingdom.def?.GetString("religion");
				}
				SetKingdomReligion(kingdom, text);
			}
		}
	}

	public static void SetKingdomReligion(Kingdom k, string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return;
		}
		string text = "";
		int num = name.IndexOf('.');
		if (num >= 0)
		{
			text = name.Substring(num + 1);
			name = name.Substring(0, num);
		}
		k.religion = k.game.religions.Get(name);
		if (k.religion == null)
		{
			Game.Log("Invalid religion specified for kingdom " + k.Name + ": " + name, Game.LogType.Error);
		}
		switch (text)
		{
		case "I":
			if (!(name != "Orthodox"))
			{
				k.subordinated = false;
			}
			break;
		case "E":
			if (!(name != "Catholic"))
			{
				k.excommunicated = true;
			}
			break;
		case "C":
			if (k.is_muslim)
			{
				k.caliphate = true;
			}
			break;
		}
		Religion.RefreshModifiers(k);
	}

	public static DT.Field GetSelectedOption(Campaign Campaign, string rule_key)
	{
		DT.Field varsDef = Campaign.GetVarsDef();
		if (varsDef == null || varsDef.children == null)
		{
			return null;
		}
		DT.Field field = varsDef.FindChild(rule_key);
		if (field == null)
		{
			return null;
		}
		return GetSelectedOption(Campaign, field);
	}

	public static DT.Field GetSelectedOption(Campaign Campaign, DT.Field Rule)
	{
		if (Campaign == null)
		{
			return null;
		}
		if (Campaign.campaignData == null)
		{
			return null;
		}
		if (Rule == null)
		{
			return null;
		}
		Value var = Campaign.campaignData.GetVar(Rule.key);
		DT.Field varOptions = Campaign.GetVarOptions(Rule.key);
		if (varOptions == null)
		{
			return null;
		}
		for (int i = 0; i < varOptions.children.Count; i++)
		{
			DT.Field field = varOptions.children[i];
			if (!string.IsNullOrEmpty(field.key))
			{
				Value optionValue = Campaign.GetOptionValue(field);
				if (optionValue.is_valid && optionValue == var)
				{
					return field;
				}
			}
		}
		return null;
	}

	public static DT.Field GetDefaultOrValidOption(Campaign Campaign, string rule_key)
	{
		DT.Field varsDef = Campaign.GetVarsDef();
		if (varsDef == null || varsDef.children == null)
		{
			return null;
		}
		DT.Field field = varsDef.FindChild(rule_key);
		if (field == null)
		{
			return null;
		}
		return GetDefaultOrValidOption(Campaign, field);
	}

	public static DT.Field GetDefaultOrValidOption(Campaign Campaign, DT.Field Rule)
	{
		Value defaultValue = Campaign.GetDefaultValue(Rule.key);
		DT.Field varOptions = Campaign.GetVarOptions(Rule.key);
		for (int i = 0; i < varOptions.children.Count; i++)
		{
			DT.Field field = varOptions.children[i];
			if (!string.IsNullOrEmpty(field.key))
			{
				if (defaultValue.is_unknown)
				{
					return field;
				}
				Value optionValue = Campaign.GetOptionValue(field);
				if (optionValue.is_valid && optionValue == defaultValue)
				{
					return field;
				}
			}
		}
		return null;
	}

	public static void ClearMultiplayerRegistry(Multiplayer multiplayer)
	{
		if (multiplayer == null)
		{
			return;
		}
		Dictionary<Serialization.ObjectType, Serialization.ObjectsRegistry.ObjectTypeRegistry> registries = multiplayer.objects.registries;
		List<Object> list = new List<Object>();
		foreach (KeyValuePair<Serialization.ObjectType, Serialization.ObjectsRegistry.ObjectTypeRegistry> item in registries)
		{
			if (item.Key == Serialization.ObjectType.Game || item.Key == Serialization.ObjectType.Religions)
			{
				continue;
			}
			foreach (Object value in item.Value.objects.Values)
			{
				list.Add(value);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			multiplayer.objects.Del(list[i]);
		}
	}

	public static void ClearSingleplayerLogic(Game game, bool clearGameMultiplayer = true)
	{
		if (game.campaign != null && !game.campaign.IsMultiplayerCampaign())
		{
			game.campaign.Destroy();
			game.campaign = null;
		}
		game.ai.enabled = true;
		if (game.multiplayer != null)
		{
			ClearMultiplayerRegistry(game.multiplayer);
			if (clearGameMultiplayer)
			{
				game.DestroyMultiplayer();
			}
		}
		UnloadMap(game);
	}

	public static void InitForSingleplayer(Game game)
	{
		if (game == null)
		{
			Game.Log("Game is null in InitForSingleplayer", Game.LogType.Error);
			return;
		}
		ClearSingleplayerLogic(game);
		game.campaign = Campaign.CreateSinglePlayerCampaign(game.map_name, game.map_period);
		Serialization.SetKingdomsVersion();
	}

	public static void PopulateDummyGame(Game game, string map_name)
	{
		game.state = Game.State.InLobby;
		game.ai.enabled = false;
		game.map_name = map_name;
		game.map_period = null;
		game.map_from_save_id = null;
		game.mapLoaded = false;
		game.ResolvePeriod();
		game.cultures = new Cultures(game);
		game.cultures.def.dt_def = game.dt.FindDef("Cultures");
		game.cultures.def.Load(game);
		game.cultures.LoadDefaults(map_name);
		game.LoadRealms(map_name, game.map_period);
		game.LoadKingdoms(map_name, game.map_period);
		game.LoadRealmIDMap(game.map_name);
		ExtractKingdomReligions(game);
	}

	public static Value GetPreferredCampaignOption(Campaign campaign, string id)
	{
		DT.Field varOptions = campaign.GetVarOptions(id);
		if (varOptions != null)
		{
			DT.Field field = null;
			bool flag = false;
			for (int i = 0; i < varOptions.children.Count; i++)
			{
				DT.Field field2 = varOptions.children[i];
				if (!string.IsNullOrEmpty(field2.key))
				{
					if (field == null)
					{
						field = field2;
					}
					if (field2.type == "prefered")
					{
						flag = true;
						return campaign.GetOptionValue(field2);
					}
				}
			}
			if (field != null && !flag)
			{
				return campaign.GetOptionValue(field);
			}
		}
		return Value.Null;
	}

	public static void EnforceSingleplayerDefaults(Campaign campaign)
	{
		if (campaign == null)
		{
			return;
		}
		if (campaign.IsMultiplayerCampaign())
		{
			Campaign.LogWarning($"Attempting to EnforceSingleplayerDefaults() on a multiplayer campaign: {campaign}");
			return;
		}
		if (GetSelectedOption(campaign, "pick_kingdom")?.key == "unknown")
		{
			Value preferredCampaignOption = GetPreferredCampaignOption(campaign, "pick_kingdom");
			if (preferredCampaignOption != Value.Null)
			{
				campaign.campaignData.Set("pick_kingdom", preferredCampaignOption);
			}
		}
		string text = THQNORequest.userId;
		if (string.IsNullOrEmpty(text))
		{
			text = DEFAULT_PLAYER_ID;
		}
		string text2 = THQNORequest.playerName;
		if (string.IsNullOrEmpty(text2))
		{
			text2 = "Player";
		}
		campaign.playerDataPersistent[0].Set("id", text);
		campaign.playerDataPersistent[0].Set("name", text2);
	}

	public static void UnloadMap(Game game)
	{
		game.map_name = null;
		game.mapLoaded = false;
		game.realms = null;
		game.game_rules = null;
		if (game.rules != null)
		{
			game.rules.Unload();
		}
		if (game.path_finding != null)
		{
			game.path_finding.Destroy();
			game.path_finding = null;
		}
		game.terrain_types = null;
		game.realm_id_map = null;
		game.heights.Dispose();
		game.passability.Dispose();
		game.world_size = Point.Invalid;
		game.cur_obj = game.first_object;
		while (game.cur_obj != null)
		{
			Object cur_obj = game.cur_obj;
			game.cur_obj = game.cur_obj.next_in_game;
			cur_obj.OnUnloadMap();
		}
	}

	public static void TurnToFFA(Campaign campaign)
	{
		if (campaign != null && campaign.campaignData != null)
		{
			if (campaign.ValidateValue("team_size", 1))
			{
				campaign.campaignData.Set("team_size", 1);
			}
			_ = campaign.playerDataPersistent;
		}
	}

	public static string GetOwnKingdomName(Campaign campaign)
	{
		string text = null;
		if (!string.IsNullOrEmpty(THQNORequest.userId))
		{
			for (int i = 0; i < 6; i++)
			{
				if (campaign.GetPlayerID(i) == THQNORequest.userId)
				{
					text = campaign.GetKingdomName(i);
					break;
				}
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			string text2 = campaign.Dir();
			System.IO.Path.GetFileName(text2);
			if (!new FileInfo(text2 + "/savegame.txt").Exists)
			{
				return null;
			}
			List<DT.Field> list = DT.Parser.ReadFile(null, text2 + "/savegame.txt");
			if (list == null || list.Count != 1)
			{
				return null;
			}
			DT.Field field = list[0];
			if (field.type != "save")
			{
				return null;
			}
			if (string.IsNullOrEmpty(field.String()))
			{
				return null;
			}
			text = field.GetString("kingdom");
		}
		return text;
	}

	public static void ResetTimeLimits(Campaign campaign)
	{
		if (campaign != null && campaign.campaignData != null)
		{
			campaign.SetToDefaultValue("time_limit");
		}
	}

	public static void GetKingdoms(Game game, List<Kingdom> kList)
	{
		if (game == null)
		{
			return;
		}
		if (kList == null)
		{
			kList = new List<Kingdom>();
		}
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom != null && IsPlayerEligible(game, kingdom) && !kingdom.IsDefeated())
			{
				kList.Add(kingdom);
			}
		}
	}

	public static bool IsPlayerEligible(Game game, Kingdom kingdom)
	{
		if (game == null)
		{
			return false;
		}
		if (kingdom == null)
		{
			return false;
		}
		DT.Field field = game.dt.Find("Maps." + game.map_name);
		if (field == null)
		{
			return true;
		}
		DT.Field field2 = field.FindChild("forbiden_kingdoms");
		if (field2 == null)
		{
			return true;
		}
		int num = field2.NumValues();
		for (int i = 0; i < num; i++)
		{
			Value value = field2.Value(i);
			if ((Value)kingdom.Name == value)
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsBlackListedKingdom(Campaign campaign, Kingdom kingdom)
	{
		if (campaign == null)
		{
			return false;
		}
		if (kingdom == null)
		{
			return false;
		}
		if (campaign.state < Campaign.State.Started && campaign.IsGrandCampaign())
		{
			return false;
		}
		List<string> whitelistKingdoms = campaign.GetWhitelistKingdoms();
		if (whitelistKingdoms == null || whitelistKingdoms.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < whitelistKingdoms.Count; i++)
		{
			if (whitelistKingdoms[i] == kingdom.Name)
			{
				return false;
			}
		}
		return true;
	}

	public static void GetRealmKingdoms(Game game, List<Kingdom> kList)
	{
		if (game == null)
		{
			return;
		}
		if (kList == null)
		{
			kList = new List<Kingdom>();
		}
		for (int i = 0; i < game.realms.Count; i++)
		{
			Realm realm = game.realms[i];
			if (realm != null && !realm.IsSeaRealm() && game.kingdoms_by_name.TryGetValue(realm.name, out var value))
			{
				kList.Add(value);
			}
		}
	}

	public static bool IsHistoricalKingdom(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		if (k.realms == null)
		{
			return false;
		}
		return k.realms.Count > 0;
	}

	public static bool IsRealmKingdom(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		Game game = k.game;
		if (game == null)
		{
			return false;
		}
		if (game.realms == null)
		{
			return false;
		}
		for (int i = 0; i < game.realms.Count; i++)
		{
			Realm realm = game.realms[i];
			if (realm != null && !realm.IsSeaRealm() && realm.name == k.Name)
			{
				return true;
			}
		}
		return false;
	}

	public static Realm GetRealmOfRealmKingdom(Kingdom k)
	{
		if (k == null)
		{
			return null;
		}
		Game game = k.game;
		if (game == null)
		{
			return null;
		}
		if (game.realms == null)
		{
			return null;
		}
		for (int i = 0; i < game.realms.Count; i++)
		{
			Realm realm = game.realms[i];
			if (realm != null && realm.name == k.Name)
			{
				return realm;
			}
		}
		return null;
	}

	public static int GetTeam(Campaign campaign, int player_index)
	{
		if (campaign == null)
		{
			return -3;
		}
		if (player_index < 0)
		{
			return -2;
		}
		int num = campaign.GetVar("team_size").Int(1);
		if (num <= 1)
		{
			return player_index;
		}
		if (num == 5)
		{
			return 0;
		}
		return campaign.GetVar(RemoteVars.DataType.PersistentPlayerData, player_index, "team").Int();
	}

	public static void SetTeam(Campaign campaign, int player_index, int team)
	{
		campaign?.SetVar(RemoteVars.DataType.PersistentPlayerData, player_index, "team", team);
	}

	public static bool IsAutoTeams(Campaign Campaign)
	{
		if (Campaign == null)
		{
			return true;
		}
		int num = Campaign.GetVar("team_size").Int(1);
		if (num > 1)
		{
			return num == 5;
		}
		return true;
	}

	public static bool IsFFA(Campaign Campaign)
	{
		if (Campaign == null)
		{
			return true;
		}
		Value var = Campaign.GetVar("team_size");
		if (var.is_unknown)
		{
			return true;
		}
		return var == 1;
	}

	public static bool IsCoop(Campaign Campaign)
	{
		if (Campaign == null)
		{
			return false;
		}
		Value var = Campaign.GetVar("team_size");
		if (var.is_unknown)
		{
			return false;
		}
		return var == 5;
	}

	public static int GetTeamsCount(Campaign campaign)
	{
		return campaign.GetVar("team_size").Int(1) switch
		{
			2 => 2, 
			3 => 3, 
			5 => 1, 
			_ => 6, 
		};
	}

	public static int GetTeamMaxSize(Campaign campaign)
	{
		return campaign.GetVar("team_size").Int(1) switch
		{
			2 => 5, 
			3 => 4, 
			5 => 6, 
			_ => 1, 
		};
	}

	public static int GetPlayerIndex(Campaign campaign, string player_id)
	{
		if (campaign?.playerDataPersistent == null)
		{
			return -1;
		}
		for (int i = 0; i < campaign.playerDataPersistent.Length; i++)
		{
			if (campaign.playerDataPersistent[i].GetVar("id").String() == player_id)
			{
				return i;
			}
		}
		return -1;
	}

	public static bool GetAllowOffline(Campaign campaign, string player_id)
	{
		if (campaign == null)
		{
			return false;
		}
		int playerIndex = GetPlayerIndex(campaign, player_id);
		if (playerIndex == -1)
		{
			return false;
		}
		return campaign.playerDataPersistent[playerIndex].GetVar("allow_offline");
	}

	public static bool IsRandomKingdomRuleActive(Campaign campaign)
	{
		if (campaign?.GetVarsDef() == null)
		{
			return false;
		}
		if (campaign.state >= Campaign.State.Started)
		{
			return false;
		}
		DT.Field rule = campaign.GetVarsDef().FindChild("pick_kingdom");
		DT.Field field = GetSelectedOption(campaign, rule);
		if (field == null)
		{
			field = GetDefaultOrValidOption(campaign, rule);
		}
		if (field.key == "random_kingdom")
		{
			return true;
		}
		if (field.key == "random_province")
		{
			return true;
		}
		return false;
	}

	public static int GetKingdomsOwningPlayerIndex(Campaign campaign, Kingdom k)
	{
		if (campaign == null)
		{
			return -1;
		}
		if (k == null)
		{
			return -1;
		}
		if (campaign?.playerDataPersistent == null)
		{
			return -1;
		}
		for (int i = 0; i < campaign.playerDataPersistent.Length; i++)
		{
			if (campaign.GetKingdomName(i) == k.Name)
			{
				return i;
			}
		}
		return -1;
	}

	public static int GetTeamProvinceCount(Game game, Campaign campaign, int player_index)
	{
		if (game == null)
		{
			return -1;
		}
		if (campaign == null)
		{
			return -1;
		}
		if (player_index < 0 || player_index >= 6)
		{
			return -1;
		}
		int num = campaign.GetVar("kingdom_size");
		if (num != 0)
		{
			return num;
		}
		int num2 = 0;
		int team = GetTeam(campaign, player_index);
		for (int i = 0; i < 6; i++)
		{
			int team2 = GetTeam(campaign, i);
			if (team == team2)
			{
				Kingdom kingdom = game.GetKingdom(campaign.GetKingdomName(i));
				if (kingdom != null)
				{
					num2 += kingdom.realms.Count;
				}
			}
		}
		return num2;
	}
}
