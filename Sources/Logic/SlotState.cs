using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Logic;

public class Campaign : RemoteVars.IListener, RemoteVars.IValidator, IVars
{
	public enum State
	{
		Empty,
		Created,
		Started,
		Closed
	}

	public enum SlotState
	{
		Empty,
		Joined,
		Eliminated,
		Kicked,
		Left,
		Reserved,
		Closed
	}

	public enum RuntimeState
	{
		None,
		Offline,
		Lobby,
		PreparingToStart,
		StartingGame,
		Playing,
		PreparingToMigrate,
		ReadyToMigrate
	}

	public enum ReasonForCopying
	{
		NewCampaign,
		RevertMinorVictoryConditionFromSave
	}

	public const int MAX_PLAYERS = 6;

	public int idx;

	public State state;

	private bool is_multiplayer;

	public string id;

	public RemoteVars campaignData;

	public RemoteVars[] playerDataPersistent;

	public RemoteVars[] playerDataNonPersistent;

	public RemoteVars chatData;

	public string[] playerIDs;

	public List<string> player_kingdoms;

	public List<int> kingdom_teams;

	public string thqno_lobby_id = string.Empty;

	public Vars[] cloned_player_vars;

	public bool is_published;

	public string requested_political_data_from_player_id;

	public int requested_political_data_session_time;

	public static string single_player_id = "single_player";

	public static bool copying_settings = false;

	public List<RemoteVars.IListener> var_listeners = new List<RemoteVars.IListener>();

	public static DT.Field campaign_vars_def = null;

	public static DT.Field singleplayer_campaign_vars_def = null;

	private static List<string> tmp_on_change_reset_processed = new List<string>();

	private static int in_on_change_reset_processed = 0;

	private static int[] team_sizes = new int[3];

	public Campaign(int idx)
	{
		this.idx = idx;
		CreateVars();
	}

	public void Destroy()
	{
		state = State.Empty;
		DestroyVars();
	}

	private void CreateVars()
	{
		campaignData = new RemoteVars(this, RemoteVars.DataType.CampaignData, 0);
		campaignData.AddListener(this);
		campaignData.AddValidator(this);
		playerDataPersistent = new RemoteVars[6];
		playerDataNonPersistent = new RemoteVars[6];
		for (int i = 0; i < 6; i++)
		{
			playerDataPersistent[i] = new RemoteVars(this, RemoteVars.DataType.PersistentPlayerData, i);
			playerDataPersistent[i].AddListener(this);
			playerDataPersistent[i].AddValidator(this);
			playerDataNonPersistent[i] = new RemoteVars(this, RemoteVars.DataType.NonPersistentPlayerData, i);
			playerDataNonPersistent[i].AddListener(this);
			playerDataNonPersistent[i].AddValidator(this);
		}
		chatData = new RemoteVars(this, RemoteVars.DataType.ChatData, 0);
		chatData.AddListener(this);
	}

	public void Reset()
	{
		ClearVars();
		state = State.Empty;
		id = null;
		cloned_player_vars = null;
		thqno_lobby_id = string.Empty;
		is_published = false;
		kingdom_teams = null;
		player_kingdoms = null;
	}

	private void ClearVars()
	{
		if (campaignData == null)
		{
			return;
		}
		campaignData.vars.Clear();
		for (int i = 0; i < 6; i++)
		{
			if (playerIDs != null)
			{
				playerIDs[i] = null;
			}
			playerDataPersistent[i].vars.Clear();
			playerDataNonPersistent[i].vars.Clear();
		}
		chatData.vars.Clear();
	}

	public void DestroyVars()
	{
		campaignData?.DelListener(this);
		campaignData?.DelValidator(this);
		for (int i = 0; i < 6; i++)
		{
			if (playerDataPersistent != null)
			{
				RemoteVars obj = playerDataNonPersistent[i];
				obj?.DelListener(this);
				obj?.DelValidator(this);
			}
			if (playerDataNonPersistent != null)
			{
				RemoteVars obj2 = playerDataNonPersistent[i];
				obj2?.DelListener(this);
				obj2?.DelValidator(this);
			}
		}
		chatData?.DelListener(this);
		campaignData = null;
		playerDataPersistent = null;
		playerDataNonPersistent = null;
		chatData = null;
	}

	public void SetState(State state)
	{
		this.state = state;
		campaignData.Set("state", state.ToString());
	}

	public void SetMultiplayer(bool is_multiplayer)
	{
		this.is_multiplayer = is_multiplayer;
	}

	public string GetOwnerID()
	{
		return campaignData?.vars?.Get<string>("owner_id");
	}

	public bool IsOwner()
	{
		if (!IsMultiplayerCampaign())
		{
			return true;
		}
		return GetOwnerID() == THQNORequest.userId;
	}

	public string GetOwnerName()
	{
		return PlayerInfo.GetPlayerName(GetOwnerID());
	}

	public void SetOwnerID(string owner_id, bool send_data_changed = true)
	{
		campaignData?.Set("owner_id", owner_id, send_data_changed);
	}

	public string CalcHostID()
	{
		if (!IsMultiplayerCampaign())
		{
			return single_player_id;
		}
		if (playerDataNonPersistent == null)
		{
			LogError("CalcHostID() called with null playerDataNonPersistent");
			return null;
		}
		if (state == State.Empty)
		{
			return null;
		}
		RuntimeState runtimeState = GetRuntimeState();
		if (runtimeState < RuntimeState.Lobby)
		{
			return null;
		}
		int latestSaveSessionTime = GetLatestSaveSessionTime();
		string result = null;
		int num = -1;
		for (int i = 0; i < 6; i++)
		{
			string playerID = GetPlayerID(i);
			if (string.IsNullOrEmpty(playerID))
			{
				continue;
			}
			RuntimeState playerRuntimeState = GetPlayerRuntimeState(i);
			if (playerRuntimeState < RuntimeState.Lobby)
			{
				continue;
			}
			if (runtimeState > RuntimeState.Lobby)
			{
				if (playerRuntimeState <= RuntimeState.Lobby)
				{
					continue;
				}
				if ((string)GetVar(RemoteVars.DataType.NonPersistentPlayerData, i, "host_id") == playerID)
				{
					return playerID;
				}
			}
			else if (!HasLatestSave(i, latestSaveSessionTime))
			{
				continue;
			}
			int num2 = GetVar(RemoteVars.DataType.GlobalPersistentPlayerData, i, "spec_score");
			if (num2 > num)
			{
				result = playerID;
				num = num2;
			}
		}
		return result;
	}

	public string GetHostID(bool skip_local_player, bool mute_warrning = false)
	{
		string text = null;
		for (int i = 0; i < 6; i++)
		{
			string playerID = GetPlayerID(i);
			if (playerID == null || (skip_local_player && playerID == THQNORequest.userId))
			{
				continue;
			}
			RemoteVars remoteVars = GetRemoteVars(RemoteVars.DataType.NonPersistentPlayerData, playerID);
			if (remoteVars != null)
			{
				text = remoteVars.GetVar("host_id").String();
				if (!string.IsNullOrEmpty(text))
				{
					break;
				}
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			if (!mute_warrning)
			{
				Game.Log($"GetHostID couldn't extract host id for campaign {this}", Game.LogType.Error);
			}
			return null;
		}
		return text;
	}

	public string GetPlayerID(int idx, bool ignore_left_players = true)
	{
		if (idx < 0 || idx >= 6)
		{
			return null;
		}
		if (playerDataPersistent == null)
		{
			return null;
		}
		RemoteVars remoteVars = playerDataPersistent[idx];
		if (remoteVars?.vars == null)
		{
			return null;
		}
		string text = remoteVars.vars.Get<string>("id");
		if (string.IsNullOrEmpty(text))
		{
			if (ignore_left_players)
			{
				return null;
			}
		}
		else if (!ignore_left_players)
		{
			return text;
		}
		if (playerIDs == null)
		{
			return null;
		}
		string text2 = playerIDs[idx];
		if (string.IsNullOrEmpty(text2))
		{
			return null;
		}
		if (ignore_left_players && text != text2)
		{
			return null;
		}
		return text2;
	}

	public void SetPlayerID(int idx, string id, bool send_data_changed = true)
	{
		if (idx < 0 || idx >= 6)
		{
			return;
		}
		if (playerIDs == null)
		{
			playerIDs = new string[6];
		}
		playerIDs[idx] = id;
		if (campaignData == null)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < 6; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(",");
			}
			string value = playerIDs[i];
			stringBuilder.Append(value);
		}
		campaignData.Set("players", stringBuilder.ToString(), send_data_changed);
	}

	private void LoadPlayerIDs(string txt)
	{
		if (string.IsNullOrEmpty(txt))
		{
			playerIDs = null;
			return;
		}
		if (playerIDs == null)
		{
			playerIDs = new string[6];
		}
		int i = 0;
		int num = 0;
		for (; i < 6; i++)
		{
			if (num >= txt.Length)
			{
				break;
			}
			int num2 = txt.IndexOf(',', num);
			if (num2 < 0)
			{
				num2 = txt.Length;
			}
			string text = txt.Substring(num, num2 - num).Trim();
			playerIDs[i] = text;
			num = num2 + 1;
		}
		while (i < 6)
		{
			playerIDs[i++] = null;
		}
	}

	public string GetPlayerName(int player_idx)
	{
		string playerID = GetPlayerID(player_idx, ignore_left_players: false);
		return GetPlayerName(playerID);
	}

	public string GetPlayerName(string player_id)
	{
		return PlayerInfo.GetPlayerName(player_id);
	}

	public string ValidatePlayerJoin(string id, Multiplayer.JoinReason join_reason)
	{
		if (GetPlayerJoinIdx(id) == -1)
		{
			return "join_no_free_slot";
		}
		if (state < State.Started && (bool)GetVar(RemoteVars.DataType.NonPersistentPlayerData, GetLocalPlayerID(), "start_countdown"))
		{
			return "join_in_countdown";
		}
		if (join_reason == Multiplayer.JoinReason.FromBrowser && !string.IsNullOrEmpty(THQNORequest.currentlyEnteredTHQNOLobbyId))
		{
			THQNORequest lobbyType = THQNORequest.GetLobbyType(THQNORequest.currentlyEnteredTHQNOLobbyId);
			if (lobbyType != null && (Common.LobbyType)lobbyType.result.obj_val != Common.LobbyType.Public)
			{
				return "lobby_not_public";
			}
		}
		return "ok";
	}

	public int GetPlayerJoinIdx(string id)
	{
		if (playerIDs == null)
		{
			playerIDs = new string[6];
		}
		int num = -1;
		int num2 = -1;
		int num3 = -1;
		int num4 = -1;
		for (int i = 0; i < playerIDs.Length; i++)
		{
			string playerID = GetPlayerID(i, ignore_left_players: false);
			SlotState slotState = GetSlotState(i);
			if (playerID == id)
			{
				return i;
			}
			switch (slotState)
			{
			case SlotState.Empty:
				if (num2 < 0)
				{
					num2 = i;
				}
				break;
			case SlotState.Left:
				if (num3 < 0)
				{
					num3 = i;
				}
				break;
			case SlotState.Kicked:
				if (num4 < 0)
				{
					num4 = i;
				}
				break;
			default:
				continue;
			}
			if (num < 0)
			{
				num = i;
			}
		}
		if (num2 >= 0)
		{
			return num2;
		}
		if (num3 >= 0)
		{
			return num3;
		}
		if (num4 >= 0)
		{
			return num4;
		}
		return num;
	}

	public void JoinPlayer(string id, bool send_data_changed = true)
	{
		int playerJoinIdx = GetPlayerJoinIdx(id);
		if (playerJoinIdx < 0)
		{
			LogError("Couldn't find a free player slot for campaign " + id);
			return;
		}
		SetPlayerID(playerJoinIdx, id, send_data_changed);
		SetVar(RemoteVars.DataType.PersistentPlayerData, playerJoinIdx, "id", id, send_data_changed: false);
		SetSlotState(playerJoinIdx, SlotState.Joined, send_data_changed);
	}

	public DateTime GetCreationTime()
	{
		string text = campaignData?.vars?.Get<string>("creation_time");
		if (string.IsNullOrEmpty(text))
		{
			return DateTime.Now;
		}
		try
		{
			return DateTime.Parse(text);
		}
		catch
		{
		}
		return DateTime.Now;
	}

	public void SetCreationTime(DateTime creation_time)
	{
		string val = creation_time.ToString("O");
		campaignData?.vars?.Set("creation_time", val);
	}

	public int GetLatestSaveSessionTime(int player_index)
	{
		return GetVar(RemoteVars.DataType.PersistentPlayerData, player_index, "latest_save_session_time").Int(-1);
	}

	public int GetLatestSaveSessionTime()
	{
		int num = -1;
		for (int i = 0; i < 6; i++)
		{
			int latestSaveSessionTime = GetLatestSaveSessionTime(i);
			if (latestSaveSessionTime > num)
			{
				num = latestSaveSessionTime;
			}
		}
		return num;
	}

	public bool HasLatestSave(int player_index, int campaign_latest = -2)
	{
		int latestSaveSessionTime = GetLatestSaveSessionTime(player_index);
		if (campaign_latest == -2)
		{
			campaign_latest = GetLatestSaveSessionTime();
		}
		if (latestSaveSessionTime < 0 && campaign_latest >= 0)
		{
			return false;
		}
		int num = latestSaveSessionTime - campaign_latest;
		if (num < -10 || num > 10)
		{
			return false;
		}
		return true;
	}

	public int GetOwnerIndex()
	{
		string ownerID = GetOwnerID();
		if (string.IsNullOrEmpty(ownerID))
		{
			return -1;
		}
		return GetPlayerIndex(ownerID);
	}

	public void CreateNew(string name, string owner_id, string owner_name, bool isMultiplayer)
	{
		Reset();
		Vars vars = new Vars();
		vars.Set("name", name);
		SetMultiplayer(isMultiplayer);
		id = NewGUID();
		vars.Set("id", id);
		vars.Set("is_multiplayer", IsMultiplayerCampaign());
		vars.Set("owner_id", owner_id);
		state = State.Created;
		vars.Set("state", state.ToString());
		vars.Set("host_message", "");
		ModManager modManager = ModManager.Get();
		if (modManager != null)
		{
			vars.Set("mod_id", modManager.GetActiveModsSaveString());
		}
		DT.Field varsDef = GetVarsDef();
		if (varsDef != null && varsDef.children != null)
		{
			campaignData.vars = vars;
			for (int i = 0; i < varsDef.children.Count; i++)
			{
				string key = varsDef.children[i].key;
				if (!string.IsNullOrEmpty(key))
				{
					Value defaultValue = GetDefaultValue(key);
					if (defaultValue.is_valid)
					{
						vars.Set(key, defaultValue);
					}
				}
			}
		}
		campaignData.SetAll(vars, send_data_changed: false);
		SetCreationTime(DateTime.Now);
		SetPlayerID(0, owner_id, send_data_changed: false);
		Vars vars2 = new Vars();
		vars2.Set("id", owner_id);
		vars2.Set("version", 1);
		playerDataPersistent[0].SetAll(vars2, send_data_changed: false);
		SetSlotState(0, SlotState.Joined, send_data_changed: false);
	}

	public void SwitchToGrandCampaign()
	{
		campaignData?.Set("main_goal", "None");
	}

	public void CopySettings(Campaign from_campaign, ReasonForCopying reasonForCopying)
	{
		DT.Field field = from_campaign?.GetVarsDef();
		if (field?.children == null)
		{
			return;
		}
		bool flag = !from_campaign.IsMultiplayerCampaign();
		bool flag2 = !IsMultiplayerCampaign();
		for (int i = 0; i < field.children.Count; i++)
		{
			DT.Field field2 = field.children[i];
			string key = field2.key;
			if (string.IsNullOrEmpty(key) || (reasonForCopying == ReasonForCopying.RevertMinorVictoryConditionFromSave && !ShouldCopyWhenRevertingVictoryConditionFromSave(field2)))
			{
				continue;
			}
			Value val = from_campaign.campaignData.vars.Get(key);
			if (!val.is_unknown && ValidateValue(key, val))
			{
				copying_settings = true;
				try
				{
					campaignData.Set(key, val);
				}
				catch (Exception ex)
				{
					LogError(ex.ToString());
				}
				copying_settings = false;
			}
		}
		if (flag || flag2)
		{
			SwitchToGrandCampaign();
		}
	}

	public void CopyPlayerData(Campaign from_campaign)
	{
		if (from_campaign?.playerDataPersistent == null)
		{
			return;
		}
		bool flag = !from_campaign.IsMultiplayerCampaign();
		bool flag2 = !IsMultiplayerCampaign();
		int localPlayerIndex = from_campaign.GetLocalPlayerIndex(ignore_left_players: false);
		for (int i = 0; i < from_campaign.playerDataPersistent.Length; i++)
		{
			if (flag2 && i != localPlayerIndex)
			{
				continue;
			}
			RemoteVars remoteVars = from_campaign.playerDataPersistent[i];
			string text = remoteVars?.GetPlayerID();
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			Value var = remoteVars.GetVar("kingdom_name");
			Value var2 = remoteVars.GetVar("origin_realm");
			Value var3 = remoteVars.GetVar("team");
			int num = ((!(flag || flag2)) ? GetPlayerJoinIdx(text) : 0);
			if (num >= 0)
			{
				RemoteVars remoteVars2 = playerDataPersistent[num];
				if (num > 0)
				{
					remoteVars2.Set("id", text);
				}
				remoteVars2.Set("kingdom_name", var, send_data_changed: false);
				remoteVars2.Set("origin_realm", var2, send_data_changed: false);
				remoteVars2.Set("team", var3, send_data_changed: false);
				if (num > 0)
				{
					SetSlotState(num, SlotState.Reserved, send_data_changed: false);
				}
			}
		}
		SetPlayerKingdomsFromCampaign(from_campaign);
	}

	public void SetFromSave(string save_id)
	{
		if (string.IsNullOrEmpty(save_id))
		{
			campaignData?.Set("from_save_id", Value.Unknown);
			campaignData?.Set("player_kingdoms", Value.Unknown);
			campaignData?.Set("kingdom_teams", Value.Unknown);
			player_kingdoms = null;
			kingdom_teams = null;
		}
		else
		{
			campaignData?.Set("from_save_id", save_id);
		}
		int value = ((!string.IsNullOrEmpty(save_id)) ? 1 : 0);
		THQNORequest.SetLobbyIntData(thqno_lobby_id, "is_from_save", value);
	}

	public void SetLocallyPlayerKingdomsFromCampaign(string txt)
	{
		if (string.IsNullOrEmpty(txt))
		{
			player_kingdoms = null;
			return;
		}
		if (player_kingdoms == null)
		{
			player_kingdoms = new List<string>();
		}
		player_kingdoms.Clear();
		int num = 0;
		while (num < txt.Length)
		{
			int num2 = txt.IndexOf(',', num);
			if (num2 < 0)
			{
				num2 = txt.Length;
			}
			string item = txt.Substring(num, num2 - num).Trim();
			player_kingdoms.Add(item);
			num = num2 + 1;
		}
	}

	public void SetLocallyKingdomTeamsFromCampaign(string txt)
	{
		if (string.IsNullOrEmpty(txt))
		{
			kingdom_teams = null;
			return;
		}
		if (kingdom_teams == null)
		{
			kingdom_teams = new List<int>();
		}
		kingdom_teams.Clear();
		int num = 0;
		while (num < txt.Length)
		{
			int num2 = txt.IndexOf(',', num);
			if (num2 < 0)
			{
				num2 = txt.Length;
			}
			int item = Convert.ToInt32(txt.Substring(num, num2 - num).Trim());
			kingdom_teams.Add(item);
			num = num2 + 1;
		}
	}

	public void SetPlayerKingdomsFromCampaign(Campaign campaign, bool send_data_changed = true)
	{
		if (campaign == null)
		{
			LogError("SetPlayerKingdomsFromCampaign() called with null campaign");
			return;
		}
		if (player_kingdoms == null)
		{
			player_kingdoms = new List<string>();
		}
		if (kingdom_teams == null)
		{
			kingdom_teams = new List<int>();
		}
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		for (int i = 0; i < 6; i++)
		{
			string kingdomName = campaign.GetKingdomName(i, raw: true);
			if (!string.IsNullOrEmpty(kingdomName))
			{
				player_kingdoms.Add(kingdomName);
				if (i > 0)
				{
					stringBuilder.Append(",");
				}
				stringBuilder.Append(kingdomName);
				int num = campaign.GetVar(RemoteVars.DataType.PersistentPlayerData, i, "team").Int();
				kingdom_teams.Add(num);
				if (i > 0)
				{
					stringBuilder2.Append(",");
				}
				stringBuilder2.Append(num);
			}
		}
		string text = stringBuilder.ToString();
		if (string.IsNullOrEmpty(text))
		{
			LogWarning($"Couldn't extract any player kingdoms from campaign {campaign}");
		}
		string text2 = stringBuilder2.ToString();
		if (string.IsNullOrEmpty(text2))
		{
			LogWarning($"Couldn't extract any player kingdom teams from campaign {campaign}");
		}
		campaignData.Set("player_kingdoms", text, send_data_changed);
		campaignData.Set("kingdom_teams", text2, send_data_changed);
	}

	public int GetTeamForKingdomName(string kingdom_name)
	{
		if (player_kingdoms == null)
		{
			LogError("GetTeamForKingdomName() called with null player_kingdoms");
			return -1;
		}
		if (kingdom_teams == null)
		{
			LogError("GetTeamForKingdomName() called with null kingdom_teams");
			return -1;
		}
		if (string.IsNullOrEmpty(kingdom_name))
		{
			LogError("GetTeamForKingdomName() called with empty kingdom name");
			return -1;
		}
		int num = player_kingdoms.IndexOf(kingdom_name);
		if (num < 0 || num > kingdom_teams.Count - 1)
		{
			LogError($"GetTeamForKingdomName() called with player_kingdom_index {num}, while kingdom_teams has a count of {kingdom_teams.Count}");
			return -1;
		}
		return kingdom_teams[num];
	}

	private void ValidatePlayerTeams()
	{
		if (CampaignUtils.IsAutoTeams(this))
		{
			return;
		}
		int teamsCount = CampaignUtils.GetTeamsCount(this);
		for (int i = 0; i < 6; i++)
		{
			int num = GetVar(RemoteVars.DataType.PersistentPlayerData, i, "team");
			if (num < 0 || num >= teamsCount)
			{
				SetVar(RemoteVars.DataType.PersistentPlayerData, i, "team", teamsCount - 1);
			}
		}
	}

	public bool IsKingdomValid(string kingdom_name)
	{
		if (string.IsNullOrEmpty(kingdom_name))
		{
			return false;
		}
		if (player_kingdoms != null && IsCustomFromSave())
		{
			return player_kingdoms.Contains(kingdom_name);
		}
		return true;
	}

	public string GetFromSaveID(bool check_started)
	{
		if (check_started && state >= State.Started)
		{
			return null;
		}
		return campaignData.GetVar("from_save_id").String();
	}

	public bool IsFromSave()
	{
		return !string.IsNullOrEmpty(GetFromSaveID(check_started: true));
	}

	public void SwapPlayers(int player_index_1, int player_index_2, bool send_data_changed = true)
	{
		if (player_index_1 != player_index_2 && player_index_1 >= 0 && player_index_1 < 6 && player_index_2 >= 0 && player_index_2 < 6)
		{
			if (playerIDs != null)
			{
				string text = playerIDs[player_index_1];
				string text2 = playerIDs[player_index_2];
				SetPlayerID(player_index_1, text2, send_data_changed: false);
				SetPlayerID(player_index_2, text, send_data_changed);
			}
			if (playerDataPersistent != null)
			{
				RemoteVars obj = playerDataPersistent[player_index_1];
				RemoteVars remoteVars = playerDataPersistent[player_index_2];
				Vars new_vars = obj?.vars;
				Vars new_vars2 = remoteVars?.vars;
				obj?.SetAll(new_vars2, send_data_changed);
				remoteVars?.SetAll(new_vars, send_data_changed);
			}
			if (playerDataNonPersistent != null)
			{
				RemoteVars obj2 = playerDataNonPersistent[player_index_1];
				RemoteVars remoteVars2 = playerDataNonPersistent[player_index_2];
				Vars new_vars3 = obj2?.vars;
				Vars new_vars4 = remoteVars2?.vars;
				obj2?.SetAll(new_vars4, send_data_changed);
				remoteVars2?.SetAll(new_vars3, send_data_changed);
			}
		}
	}

	public DT.Field GetVarsDef()
	{
		if (IsMultiplayerCampaign())
		{
			return campaign_vars_def;
		}
		return singleplayer_campaign_vars_def;
	}

	public static string NewGUID()
	{
		return Guid.NewGuid().ToString();
	}

	public bool IsMultiplayerCampaign()
	{
		return is_multiplayer;
	}

	public static string EncodePlayerIndexAsId(int player_index)
	{
		return $"#{player_index}";
	}

	public static bool DecodePlayerIDAsIndex(string player_id, out int player_index)
	{
		player_index = -1;
		if (string.IsNullOrEmpty(player_id))
		{
			return false;
		}
		if (player_id[0] != '#')
		{
			return false;
		}
		if (!int.TryParse(player_id.Substring(1), out player_index))
		{
			return false;
		}
		return true;
	}

	public string GetLocalPlayerID()
	{
		if (!IsMultiplayerCampaign())
		{
			return single_player_id;
		}
		return THQNORequest.userId;
	}

	public bool IsGrandCampaign()
	{
		Value var = GetVar("main_goal");
		if (var.is_unknown)
		{
			return false;
		}
		return var == (Value)"None";
	}

	public bool IsPlayerControlledKingdom(Kingdom kingdom)
	{
		return GetPlayerIndex(kingdom) != -1;
	}

	public int GetPlayerIndex(string player_id, bool ignore_left_players = true)
	{
		if (DecodePlayerIDAsIndex(player_id, out var player_index))
		{
			return player_index;
		}
		if (playerDataPersistent == null || playerIDs == null || string.IsNullOrEmpty(player_id))
		{
			return -1;
		}
		for (int i = 0; i < 6; i++)
		{
			if (GetPlayerID(i, ignore_left_players) == player_id)
			{
				return i;
			}
		}
		return -1;
	}

	public int GetPlayerIndex(Kingdom kingdom)
	{
		string text = kingdom?.Name;
		if (playerDataPersistent == null || string.IsNullOrEmpty(text))
		{
			return -1;
		}
		for (int i = 0; i < 6; i++)
		{
			if (GetKingdomName(i) == text)
			{
				return i;
			}
		}
		return -1;
	}

	public int GetPlayerKingdomCoAIndex(string player_id)
	{
		string mapName = GetMapName();
		string period = GetPeriod();
		string kingdomName = GetKingdomName(player_id);
		return CoAMapping.GetCoAIndex(mapName, period, kingdomName);
	}

	public int GetPlayerKingdomCoAIndexByKingdomName(string kingdom_name)
	{
		string mapName = GetMapName();
		string period = GetPeriod();
		return CoAMapping.GetCoAIndex(mapName, period, kingdom_name);
	}

	public int GetLocalPlayerIndex(bool ignore_left_players = true)
	{
		if (!IsMultiplayerCampaign())
		{
			return 0;
		}
		return GetPlayerIndex(THQNORequest.userId, ignore_left_players);
	}

	public int GetIndexInPlayers(string player_id)
	{
		if (string.IsNullOrEmpty(player_id))
		{
			return -2;
		}
		if (DecodePlayerIDAsIndex(player_id, out var player_index))
		{
			return player_index;
		}
		if (playerIDs == null)
		{
			return -3;
		}
		for (int i = 0; i < playerIDs.Length; i++)
		{
			if (playerIDs[i] == player_id)
			{
				return i;
			}
		}
		return -1;
	}

	public int GetIndexInPlayerData(string player_id)
	{
		if (string.IsNullOrEmpty(player_id))
		{
			return -3;
		}
		if (DecodePlayerIDAsIndex(player_id, out var player_index))
		{
			return player_index;
		}
		if (playerDataPersistent == null)
		{
			return -2;
		}
		for (int i = 0; i < 6; i++)
		{
			if (playerDataPersistent[i].GetPlayerID() == player_id)
			{
				return i;
			}
		}
		return -1;
	}

	public int GetLocalPlayerIndexInData()
	{
		if (!IsMultiplayerCampaign())
		{
			return 0;
		}
		return GetIndexInPlayerData(THQNORequest.userId);
	}

	public int GetNumPlayers(bool include_reserved = false)
	{
		int num = 0;
		for (int i = 0; i < 6; i++)
		{
			SlotState slotState = GetSlotState(i);
			if (IsInCampaign(slotState) || (slotState == SlotState.Reserved && include_reserved))
			{
				num++;
			}
		}
		return num;
	}

	public int GetMaxPlayers()
	{
		int num = 0;
		for (int i = 0; i < 6; i++)
		{
			if (GetSlotState(i) == SlotState.Closed)
			{
				num++;
			}
		}
		return 6 - num;
	}

	public bool CanPlayerBeInvited(int player_index)
	{
		return GetVar(RemoteVars.DataType.PersistentPlayerData, player_index, "can_be_invited");
	}

	public SlotState GetSlotState(int player_index)
	{
		if (Enum.TryParse<SlotState>(GetVar(RemoteVars.DataType.PersistentPlayerData, player_index, "slot_state").String("Empty"), out var result))
		{
			return result;
		}
		return SlotState.Empty;
	}

	public SlotState GetSlotState(string player_id)
	{
		int playerIndex = GetPlayerIndex(player_id);
		return GetSlotState(playerIndex);
	}

	public void SetSlotState(int player_index, SlotState slot_state, bool send_data_changed = true)
	{
		SetVar(RemoteVars.DataType.PersistentPlayerData, player_index, "slot_state", slot_state.ToString(), send_data_changed);
	}

	public bool IsInCampaign(SlotState slot_state)
	{
		if (slot_state != SlotState.Joined)
		{
			return slot_state == SlotState.Eliminated;
		}
		return true;
	}

	public bool IsInCampaign(int player_index)
	{
		SlotState slotState = GetSlotState(player_index);
		return IsInCampaign(slotState);
	}

	public bool IsInCampaign(string player_id)
	{
		int playerIndex = GetPlayerIndex(player_id);
		return IsInCampaign(playerIndex);
	}

	public RemoteVars GetRemoteVars(RemoteVars.DataType data_type, string player_id)
	{
		if (string.IsNullOrEmpty(player_id))
		{
			return null;
		}
		int indexInPlayers = GetIndexInPlayers(player_id);
		RemoteVars remoteVars = GetRemoteVars(data_type, indexInPlayers);
		if (remoteVars != null)
		{
			return remoteVars;
		}
		if (data_type != RemoteVars.DataType.PersistentPlayerData && data_type != RemoteVars.DataType.NonPersistentPlayerData)
		{
			return null;
		}
		remoteVars = GetRemoteVars(data_type, indexInPlayers);
		if (remoteVars != null)
		{
			return remoteVars;
		}
		return null;
	}

	public RemoteVars GetRemoteVars(RemoteVars.DataType data_type, int player_index)
	{
		switch (data_type)
		{
		case RemoteVars.DataType.CampaignData:
			return campaignData;
		case RemoteVars.DataType.PersistentPlayerData:
			if (playerDataPersistent == null)
			{
				return null;
			}
			if (player_index < 0 || player_index >= 6)
			{
				return null;
			}
			return playerDataPersistent[player_index];
		case RemoteVars.DataType.NonPersistentPlayerData:
			if (playerDataNonPersistent == null)
			{
				return null;
			}
			if (player_index < 0 || player_index >= 6)
			{
				return null;
			}
			return playerDataNonPersistent[player_index];
		case RemoteVars.DataType.AllPersistentData:
			return null;
		case RemoteVars.DataType.GlobalPersistentPlayerData:
		{
			string playerID = GetPlayerID(player_index);
			if (string.IsNullOrEmpty(playerID))
			{
				return null;
			}
			return PlayerInfo.Get(playerID, create: false)?.persistent_global_vars;
		}
		default:
			return null;
		}
	}

	public RemoteVars GetLocalPlayerRemoteVars(RemoteVars.DataType data_type)
	{
		int localPlayerIndex = GetLocalPlayerIndex();
		return GetRemoteVars(data_type, localPlayerIndex);
	}

	public int GetVersion()
	{
		if (campaignData == null)
		{
			return -1;
		}
		return campaignData.GetVersion();
	}

	public int GetPlayerDataVersion(int player_index)
	{
		return GetRemoteVars(RemoteVars.DataType.PersistentPlayerData, player_index)?.GetVersion() ?? (-1);
	}

	public int GetPlayerDataVersion(string player_id)
	{
		return GetRemoteVars(RemoteVars.DataType.PersistentPlayerData, player_id)?.GetVersion() ?? (-1);
	}

	public Value GetVar(RemoteVars.DataType data_type, int player_index, string key)
	{
		return GetRemoteVars(data_type, player_index)?.GetVar(key) ?? Value.Unknown;
	}

	public Value GetVar(RemoteVars.DataType data_type, string player_id, string key)
	{
		return GetRemoteVars(data_type, player_id)?.GetVar(key) ?? Value.Unknown;
	}

	public Value GetLocalPlayerVar(RemoteVars.DataType data_type, string key)
	{
		return GetLocalPlayerRemoteVars(data_type)?.GetVar(key) ?? Value.Unknown;
	}

	public bool SetVar(RemoteVars.DataType data_type, int player_index, string key, Value val, bool send_data_changed = true)
	{
		RemoteVars remoteVars = GetRemoteVars(data_type, player_index);
		if (remoteVars == null)
		{
			return false;
		}
		remoteVars.Set(key, val, send_data_changed);
		return true;
	}

	public bool SetVar(RemoteVars.DataType data_type, string player_id, string key, Value val, bool send_data_changed = true)
	{
		RemoteVars remoteVars = GetRemoteVars(data_type, player_id);
		if (remoteVars == null)
		{
			return false;
		}
		remoteVars.Set(key, val, send_data_changed);
		return true;
	}

	public bool SetLocalPlayerVar(RemoteVars.DataType data_type, string key, Value val, bool send_data_changed = true)
	{
		RemoteVars localPlayerRemoteVars = GetLocalPlayerRemoteVars(data_type);
		if (localPlayerRemoteVars == null)
		{
			return false;
		}
		localPlayerRemoteVars.vars.Set("id", GetLocalPlayerID());
		localPlayerRemoteVars.Set(key, val, send_data_changed);
		return true;
	}

	public void AddVarsListener(RemoteVars.IListener listener)
	{
		var_listeners.Add(listener);
	}

	public void DelVarsListener(RemoteVars.IListener listener)
	{
		var_listeners.Remove(listener);
	}

	public void OnVarChanged(RemoteVars vars, string key, Value old_val, Value new_val)
	{
		for (int i = 0; i < var_listeners.Count; i++)
		{
			var_listeners[i].OnVarChanged(vars, key, old_val, new_val);
		}
		if (vars.data_type == RemoteVars.DataType.ChatData)
		{
			return;
		}
		switch (key)
		{
		case "players":
			LoadPlayerIDs(new_val.String());
			break;
		case "state":
		{
			if (Enum.TryParse<State>(new_val.String(), out var result))
			{
				state = result;
			}
			else
			{
				state = State.Empty;
			}
			break;
		}
		case "start_period":
			if (!copying_settings)
			{
				SetFromSave(null);
			}
			break;
		case "player_kingdoms":
			SetLocallyPlayerKingdomsFromCampaign(new_val.String());
			break;
		case "kingdom_teams":
			SetLocallyKingdomTeamsFromCampaign(new_val.String());
			break;
		case "team_size":
			ValidatePlayerTeams();
			break;
		}
		if (vars.data_type == RemoteVars.DataType.CampaignData && IsAuthority())
		{
			ProcessOnChangeReset(key);
		}
	}

	public void ValidatePlayersReady(Game game)
	{
		for (int i = 0; i < 6; i++)
		{
			if (!string.IsNullOrEmpty(ValidateReady(i, game, check_host: false)))
			{
				SetPlayerReady(i, ready: false);
			}
		}
	}

	public bool ValidateVarChange(RemoteVars vars, string key, Value old_val, ref Value new_val)
	{
		if (vars.data_type == RemoteVars.DataType.CampaignData && IsAuthority() && !ValidateValue(key, new_val))
		{
			return false;
		}
		return true;
	}

	public static string MultiplayerRootDir()
	{
		return Game.GetSavesRootDir(Game.SavesRoot.Multi);
	}

	public string Dir()
	{
		string savesRootDir = Game.GetSavesRootDir((!IsMultiplayerCampaign()) ? Game.SavesRoot.Single : Game.SavesRoot.Multi);
		if (string.IsNullOrEmpty(id))
		{
			LogError("Empty campaign id");
			return null;
		}
		return new DirectoryInfo(System.IO.Path.Combine(savesRootDir, id)).FullName;
	}

	public string Dir(string subfolder)
	{
		if (string.IsNullOrEmpty(subfolder))
		{
			return null;
		}
		string path = Dir();
		path = System.IO.Path.Combine(path, subfolder);
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		return path;
	}

	public bool Load(string path = null)
	{
		string text = path;
		if (text == null)
		{
			text = Dir();
			if (text == null)
			{
				return false;
			}
		}
		try
		{
			string txt = File.ReadAllText(text + "/campaign.def");
			LoadFromDTString(txt);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public void Save(string path = null)
	{
		if (string.IsNullOrEmpty(path))
		{
			path = Dir();
			if (path == null)
			{
				return;
			}
			Directory.CreateDirectory(path);
		}
		string dTSaveString = GetDTSaveString();
		File.WriteAllText(path + "/campaign.def", dTSaveString);
	}

	public Vars GetSaveVars()
	{
		Vars vars = new Vars();
		vars.Modify(campaignData?.vars);
		vars.Set("id", id);
		vars.Set("is_multiplayer", IsMultiplayerCampaign());
		if (playerDataPersistent != null)
		{
			for (int i = 0; i < playerDataPersistent.Length; i++)
			{
				Vars vars2 = playerDataPersistent[i]?.vars;
				if (vars2 != null && vars2.Count() != 0)
				{
					vars.Set($"player{i}", vars2);
				}
			}
		}
		else
		{
			LogWarning($"{this} player_vars is null on def save!");
		}
		return vars;
	}

	public string GetDTSaveString()
	{
		return GetSaveVars().ToDTString();
	}

	public void LoadFromDTString(string txt)
	{
		Vars vars = Vars.FromDTString(txt);
		if (vars != null)
		{
			LoadFromVars(vars);
		}
	}

	public void LoadFromVars(Vars vars)
	{
		string text = vars.Get<string>("id");
		if (string.IsNullOrEmpty(id))
		{
			id = text;
		}
		else if (text != id)
		{
			LogWarning("Changed campaign id from " + id + " to " + text + "!");
			id = text;
		}
		is_multiplayer = vars.Get<bool>("is_multiplayer", IsMultiplayerCampaign());
		string txt = vars.Get<string>("players");
		LoadPlayerIDs(txt);
		if (!Enum.TryParse<State>(vars.Get<string>("state"), out state))
		{
			state = State.Empty;
		}
		for (int i = 0; i < 6; i++)
		{
			string key = $"player{i}";
			Vars vars2 = vars.Get<Vars>(key);
			vars.Del(key);
			if (vars2 != null)
			{
				playerDataPersistent[i].vars = vars2;
			}
		}
		campaignData.vars = vars;
	}

	public void DeleteSavedGame()
	{
		if (string.IsNullOrEmpty(id))
		{
			return;
		}
		string text = Dir();
		if (text != null)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(text);
			if (directoryInfo.Exists)
			{
				directoryInfo.Delete(recursive: true);
			}
		}
	}

	public void Load(Serialization.IReader reader, bool server)
	{
		id = reader.ReadStr("id");
		bool multiplayer = reader.ReadBool("is_multiplayer");
		SetMultiplayer(multiplayer);
		Vars vars = Data.RestoreObject<Vars>(reader.ReadData("vars"), null);
		if (vars != null)
		{
			campaignData.vars = vars;
			OnVarChanged(campaignData, "state", Value.Unknown, vars.GetVar("state"));
		}
		for (int i = 0; i < 6; i++)
		{
			Vars vars2 = Data.RestoreObject<Vars>(reader.ReadData("player", i), null);
			if (vars2 != null && !string.IsNullOrEmpty(vars2.Get("id").String()))
			{
				playerDataPersistent[i].vars = vars2;
			}
		}
	}

	public void Save(Serialization.IWriter writer)
	{
		writer.WriteStr(id, "id");
		writer.WriteBool(IsMultiplayerCampaign(), "is_multiplayer");
		writer.WriteData(Data.CreateFull(campaignData?.vars), "vars");
		for (int i = 0; i < 6; i++)
		{
			Vars obj = ((playerDataPersistent == null) ? null : playerDataPersistent[i].vars);
			writer.WriteData(Data.CreateFull(obj), "player", i);
		}
	}

	public bool WriteTHQNOCampaignData(DTSerializeWriter writer)
	{
		if (writer == null)
		{
			return false;
		}
		writer.WriteRawStr(id, "campaign_id");
		writer.WriteRawStr(DateTime.Now.ToUniversalTime().ToString(), "timestamp");
		int val = 0;
		if (playerDataPersistent != null)
		{
			val = playerDataPersistent.Length;
		}
		writer.Write7BitUInt(val, "players_count");
		if (playerDataPersistent != null)
		{
			for (int i = 0; i < playerDataPersistent.Length; i++)
			{
				string val2 = playerDataPersistent[i].GetVar("id");
				string kingdomName = GetKingdomName(i);
				writer.WriteRawStr(val2, "player_id", i);
				writer.WriteRawStr(kingdomName, "kingdom_name", i);
			}
		}
		return true;
	}

	public Value GetVar(string key, IVars _vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "in_editor":
			return false;
		case "owner_name":
		{
			string ownerName = GetOwnerName();
			if (string.IsNullOrEmpty(ownerName))
			{
				return Value.Null;
			}
			return "#" + ownerName;
		}
		case "custom_campaign_name":
			return GetCustomNameLocalized();
		case "kingdom_name":
		{
			string kingdomName = GetKingdomName();
			if (string.IsNullOrEmpty(kingdomName))
			{
				return Value.Null;
			}
			return "tn_" + kingdomName;
		}
		default:
			if (campaignData != null)
			{
				Value var = campaignData.GetVar(key);
				if (!var.is_unknown)
				{
					return var;
				}
			}
			return GetDefaultValue(key);
		}
	}

	public DT.Field GetVarDef(string key)
	{
		return GetVarsDef()?.FindChild(key);
	}

	public DT.Field GetVarOptions(string key)
	{
		return GetVarDef(key)?.FindChild("options", campaignData);
	}

	public Value GetOptionValue(DT.Field f)
	{
		if (f.value.is_valid)
		{
			return f.value;
		}
		if (int.TryParse(f.key, out var result))
		{
			return result;
		}
		if (DT.ParseFloat(f.key, out var f2))
		{
			return f2;
		}
		return f.key;
	}

	public DT.Field FindMatchingOption(string key)
	{
		DT.Field varDef = GetVarDef(key);
		if (varDef == null)
		{
			return null;
		}
		DT.Field field = varDef.FindChild("options", this);
		if (field == null || field.children == null)
		{
			return null;
		}
		Value var = GetVar(key);
		for (int i = 0; i < field.children.Count; i++)
		{
			DT.Field field2 = field.children[i];
			if (!string.IsNullOrEmpty(field2.key) && GetOptionValue(field2) == var)
			{
				return field2;
			}
		}
		return null;
	}

	public bool CheckCondition(string key)
	{
		return GetVarDef(key)?.GetBool("condition", this, def_val: true) ?? true;
	}

	public bool CheckOptionCondition(DT.Field of)
	{
		if (of == null)
		{
			return true;
		}
		if (of.FindChild("condition") == null)
		{
			return true;
		}
		return of.GetBool("condition", this);
	}

	public Value GetDefaultValue(string key)
	{
		DT.Field varDef = GetVarDef(key);
		if (varDef == null)
		{
			return Value.Unknown;
		}
		if (!CheckCondition(key))
		{
			return Value.Unknown;
		}
		if (varDef.value.is_valid)
		{
			return varDef.value;
		}
		DT.Field field = varDef.FindChild("options", this);
		if (field == null || field.children == null)
		{
			return Value.Unknown;
		}
		for (int i = 0; i < field.children.Count; i++)
		{
			DT.Field field2 = field.children[i];
			if (!(field2.type != "default"))
			{
				return GetOptionValue(field2);
			}
		}
		return Value.Unknown;
	}

	public void SetToDefaultValue(string key)
	{
		Value defaultValue = GetDefaultValue(key);
		campaignData?.Set(key, defaultValue);
	}

	public bool ValidateValue(string key, Value val)
	{
		DT.Field varDef = GetVarDef(key);
		if (varDef == null)
		{
			return false;
		}
		if (!CheckCondition(key))
		{
			return false;
		}
		DT.Field field = varDef.FindChild("options", this);
		if (field != null)
		{
			if (field.children == null)
			{
				return false;
			}
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				if (!string.IsNullOrEmpty(field2.key) && GetOptionValue(field2) == val)
				{
					return true;
				}
			}
			return false;
		}
		return true;
	}

	public void ProcessOnChangeReset(string key)
	{
		DT.Field varDef = GetVarDef(key);
		if (varDef == null)
		{
			return;
		}
		DT.Field field = varDef.FindChild("on_change_reset");
		if (field == null || field.children == null)
		{
			return;
		}
		in_on_change_reset_processed++;
		for (int i = 0; i < field.children.Count; i++)
		{
			DT.Field field2 = field.children[i];
			if (!string.IsNullOrEmpty(field2.key) && !tmp_on_change_reset_processed.Contains(field2.key))
			{
				tmp_on_change_reset_processed.Add(field2.key);
				Value defaultValue = GetDefaultValue(field2.key);
				campaignData.Set(field2.key, defaultValue);
				if (!defaultValue.is_unknown)
				{
					ProcessOnChangeReset(field2.key);
				}
			}
		}
		in_on_change_reset_processed--;
		if (in_on_change_reset_processed == 0)
		{
			tmp_on_change_reset_processed.Clear();
		}
	}

	public string ValidateSetup(Game game)
	{
		if (string.IsNullOrEmpty(GetMapName()))
		{
			return "no_map";
		}
		if (state < State.Created && IsMultiplayerCampaign())
		{
			return "not_created";
		}
		string text = ValidateLocalPlayerReady(game);
		if (text != null)
		{
			return text;
		}
		string authorityPlayerID = GetAuthorityPlayerID();
		int num = 0;
		bool flag = CampaignUtils.IsAutoTeams(this);
		if (!flag)
		{
			num = CampaignUtils.GetTeamsCount(this);
			for (int i = 0; i < team_sizes.Length; i++)
			{
				team_sizes[i] = 0;
			}
		}
		Value var = GetVar(RemoteVars.DataType.NonPersistentPlayerData, GetLocalPlayerID(), "start_countdown");
		for (int j = 0; j < 6; j++)
		{
			string playerID = GetPlayerID(j);
			if (string.IsNullOrEmpty(playerID) || GetSlotState(j) != SlotState.Joined)
			{
				continue;
			}
			if (!flag)
			{
				int team = CampaignUtils.GetTeam(this, j);
				if (team < 0 || team >= num)
				{
					return "bad_teams";
				}
				team_sizes[team]++;
			}
			if (IsMultiplayerCampaign() && GetPlayerRuntimeState(j) == RuntimeState.Offline)
			{
				if (!GetPlayerAllowOffline(j))
				{
					return "not_online";
				}
			}
			else if (!var)
			{
				if (playerID != authorityPlayerID && !IsPlayerReady(j))
				{
					return "not_ready";
				}
				text = ValidateReady(j, game, check_host: false);
				if (text != null)
				{
					return "not_ready";
				}
			}
		}
		if (!flag)
		{
			int teamMaxSize = CampaignUtils.GetTeamMaxSize(this);
			for (int k = 0; k < num; k++)
			{
				int num2 = team_sizes[k];
				if (num2 <= 0)
				{
					return "bad_teams";
				}
				if (num2 > teamMaxSize)
				{
					return "bad_teams";
				}
			}
		}
		if (is_multiplayer && GetNumPlayers() < 2 && !Game.CheckCheatLevel(Game.CheatLevel.High, "start_game_num_players", warning: false))
		{
			return "not_enough_players_new_game";
		}
		if (IsMultiplayerCampaign())
		{
			bool flag2 = true;
			string text2 = null;
			for (int l = 0; l < 6; l++)
			{
				string playerID2 = GetPlayerID(l);
				if (!string.IsNullOrEmpty(playerID2) && GetPlayerRuntimeState(playerID2) > RuntimeState.Offline)
				{
					string text3 = GetVar(RemoteVars.DataType.NonPersistentPlayerData, playerID2, "mod_id");
					if (flag2)
					{
						text2 = text3;
						flag2 = false;
					}
					else if (text2 != text3)
					{
						return "client_version_mismatch";
					}
				}
			}
			flag2 = true;
			string text4 = null;
			for (int m = 0; m < 6; m++)
			{
				string playerID3 = GetPlayerID(m);
				if (!string.IsNullOrEmpty(playerID3) && GetPlayerRuntimeState(playerID3) > RuntimeState.Offline)
				{
					string text5 = GetVar(RemoteVars.DataType.NonPersistentPlayerData, playerID3, "client_version");
					if (flag2)
					{
						text4 = text5;
						flag2 = false;
					}
					else if (text4 != text5)
					{
						return "client_version_mismatch";
					}
				}
			}
		}
		return null;
	}

	public int GetPlayerIndexForKingdom(string kingdom_name, int ignore_player_index = -1)
	{
		if (string.IsNullOrEmpty(kingdom_name))
		{
			return -2;
		}
		for (int i = 0; i < 6; i++)
		{
			if (i != ignore_player_index && GetKingdomName(i) == kingdom_name)
			{
				return i;
			}
		}
		return -1;
	}

	public string ValidateLocalPlayerReady(Game game, bool check_host = true)
	{
		int localPlayerIndex = GetLocalPlayerIndex();
		return ValidateReady(localPlayerIndex, game, check_host);
	}

	public string ValidateReady(int player_index, Game game, bool check_host = true)
	{
		if (game == null)
		{
			return "no_game";
		}
		if (player_index < 0)
		{
			return "invalid_player_index";
		}
		if (IsMultiplayerCampaign() && string.IsNullOrEmpty(GetPlayerID(player_index)))
		{
			return "no_player_id";
		}
		string kingdomName = GetKingdomName(player_index);
		if (string.IsNullOrEmpty(kingdomName) && !CampaignUtils.IsRandomKingdomRuleActive(this))
		{
			return "no_kingdom";
		}
		if (check_host && string.IsNullOrEmpty(CalcHostID()))
		{
			return "no_host";
		}
		if (!CampaignUtils.IsAutoTeams(this))
		{
			int team = CampaignUtils.GetTeam(this, player_index);
			if (team < 0)
			{
				return "no_team";
			}
			int teamsCount = CampaignUtils.GetTeamsCount(this);
			if (team >= teamsCount)
			{
				return "no_team";
			}
		}
		if (state == State.Started)
		{
			return null;
		}
		bool flag = kingdomName == "random";
		if (!flag && GetPlayerIndexForKingdom(kingdomName, player_index) >= 0)
		{
			return "duplicate_kingdom";
		}
		Kingdom kingdom = (flag ? null : game.GetKingdom(kingdomName));
		string text = CampaignUtils.GetSelectedOption(this, "pick_kingdom")?.key;
		if (text == "kingdom")
		{
			if (!CampaignUtils.IsPlayerEligible(game, kingdom))
			{
				return "invalid_kingdom";
			}
			if (kingdom?.realms == null || kingdom.realms.Count == 0)
			{
				return "invalid_kingdom";
			}
			string originRealmName = GetOriginRealmName(player_index);
			Realm realm = game.GetRealm(originRealmName);
			if (!kingdom.realms.Contains(realm))
			{
				return "invalid_origin_realm";
			}
		}
		if (text == "province" && !CampaignUtils.IsPlayerEligible(game, kingdom))
		{
			return "invalid_kingdom";
		}
		if ((string)GetVar("main_goal") == "HaveXRealms")
		{
			int num = GetVar("kingdom_size");
			bool num2 = num == 0;
			bool flag2 = !CampaignUtils.IsFFA(this);
			int num3 = GetVar("realms_goal");
			int num4 = ((!flag2) ? (kingdom?.realms.Count ?? 0) : CampaignUtils.GetTeamProvinceCount(game, this, player_index));
			if (num2 && num4 >= num3)
			{
				if (!flag2)
				{
					return "invalid_kingdom_size";
				}
				return "invalid_kingdom_team_size";
			}
			if (num >= num3)
			{
				if (!flag2)
				{
					return "invalid_kingdom_size";
				}
				return "invalid_kingdom_team_size";
			}
		}
		return null;
	}

	public bool IsVarChangeableAfterStart(DT.Field vf)
	{
		return vf?.GetBool("changeable_after_start", this) ?? false;
	}

	public bool IsVarChangeableAfterCopy(DT.Field vf)
	{
		if (vf == null)
		{
			return false;
		}
		if (!IsFromSave())
		{
			LogError("IsVarChangeableAfterCopy() called for campaign that is not created from save");
			return false;
		}
		if (IsGrandCampaign())
		{
			if (vf.key == "main_goal")
			{
				return false;
			}
			if (vf.key == "team_size")
			{
				return true;
			}
		}
		return vf.GetBool("changeable_after_copy", this);
	}

	public bool IsRuleOptionChangable(string rule_key, string option_key)
	{
		if (IsFromSave() && IsMultiplayerCampaign() && rule_key == "main_goal")
		{
			Value var = GetVar("main_goal");
			if (var.is_unknown || !var.is_string)
			{
				LogError("Cannot extract current campaigns' main_goal!");
				return false;
			}
			string text = var.String();
			if (option_key == "None" || option_key == text)
			{
				return true;
			}
			return false;
		}
		return true;
	}

	public bool CanChangeTeam(string player_id)
	{
		if (string.IsNullOrEmpty(player_id))
		{
			LogError("CanChangeTeam() called with invalid player_id");
			return false;
		}
		if (!IsFromSave())
		{
			return true;
		}
		if (!IsGrandCampaign())
		{
			return false;
		}
		return true;
	}

	public bool CanChangeTeam(int player_idx)
	{
		if (player_idx < 0 || player_idx > 6)
		{
			LogError($"CanChangeTeam() called with invalid player_idx: {player_idx}");
			return false;
		}
		if (!IsFromSave())
		{
			return true;
		}
		if (!IsGrandCampaign())
		{
			return false;
		}
		return true;
	}

	public List<string> GetWhitelistKingdoms()
	{
		if (state >= State.Started && (player_kingdoms == null || player_kingdoms.Count == 0))
		{
			if (player_kingdoms == null)
			{
				player_kingdoms = new List<string>(6);
			}
			for (int i = 0; i < 6; i++)
			{
				string text = GetVar(RemoteVars.DataType.PersistentPlayerData, i, "kingdom_name");
				if (!string.IsNullOrEmpty(text))
				{
					player_kingdoms.Add(text);
				}
			}
		}
		return player_kingdoms;
	}

	public bool ShouldCopyWhenRevertingVictoryConditionFromSave(DT.Field vf)
	{
		return vf?.GetBool("copy_when_reverting_victory_condition_from_save", this) ?? false;
	}

	public bool IsCustomFromSave()
	{
		if (IsFromSave() && !IsGrandCampaign())
		{
			return state == State.Created;
		}
		return false;
	}

	public static Campaign CreateSinglePlayerCampaign(string map_name, string period)
	{
		Campaign campaign = new Campaign(0);
		if (Game.isComingFromTitle)
		{
			campaign.id = NewGUID();
		}
		else
		{
			campaign.id = "Editor-" + map_name;
		}
		campaign.SetOwnerID(single_player_id, send_data_changed: false);
		DT.Field varsDef = campaign.GetVarsDef();
		if (varsDef != null && varsDef.children != null)
		{
			for (int i = 0; i < varsDef.children.Count; i++)
			{
				string key = varsDef.children[i].key;
				if (!string.IsNullOrEmpty(key))
				{
					Value defaultValue = campaign.GetDefaultValue(key);
					if (defaultValue.is_valid)
					{
						campaign.campaignData.vars.Set(key, defaultValue);
					}
				}
			}
		}
		campaign.SetCreationTime(DateTime.Now);
		if (!string.IsNullOrEmpty(map_name))
		{
			campaign.SetMapName(map_name, send_data_changed: false);
			if (!string.IsNullOrEmpty(period))
			{
				campaign.SetPeriod(period, send_data_changed: false);
			}
		}
		campaign.SetState(State.Created);
		campaign.SetSlotState(0, SlotState.Joined, send_data_changed: false);
		campaign.SetVar(RemoteVars.DataType.PersistentPlayerData, 0, "id", single_player_id, send_data_changed: false);
		campaign.SetPlayerID(0, single_player_id, send_data_changed: false);
		return campaign;
	}

	public string GetMapName()
	{
		if (campaignData == null)
		{
			return null;
		}
		Value var = campaignData.GetVar("map_size");
		if (var.is_unknown || var.is_unknown || !var.is_string)
		{
			return null;
		}
		return var.String()?.ToLowerInvariant();
	}

	public void SetMapName(string map_name, bool send_data_changed = true)
	{
		campaignData?.Set("map_size", map_name, send_data_changed);
	}

	public string GetPeriod()
	{
		if (campaignData == null)
		{
			return null;
		}
		Value var = campaignData.GetVar("start_period");
		if (var.is_unknown || var.is_unknown || !var.is_string)
		{
			return null;
		}
		return var.String();
	}

	public void SetPeriod(string period, bool send_data_changed = true)
	{
		campaignData?.Set("start_period", period, send_data_changed);
	}

	public string GetCustomName()
	{
		return campaignData?.vars?.Get<string>("name");
	}

	public string GetCustomNameLocalized()
	{
		string customName = GetCustomName();
		if (string.IsNullOrEmpty(customName))
		{
			return null;
		}
		return "#" + customName;
	}

	public string GetNameKey()
	{
		if (IsMultiplayerCampaign())
		{
			return "TitleScreen.Multiplayer.campaign_name";
		}
		string customNameLocalized = GetCustomNameLocalized();
		if (!string.IsNullOrEmpty(customNameLocalized))
		{
			return "@{noparse}{" + customNameLocalized + "}{/noparse}";
		}
		string kingdomName = GetKingdomName();
		if (!string.IsNullOrEmpty(kingdomName))
		{
			return "tn_" + kingdomName;
		}
		return "#No name";
	}

	public string GetNameNonLocalized()
	{
		string text = campaignData?.vars?.Get<string>("name");
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		string kingdomName = GetKingdomName();
		if (!string.IsNullOrEmpty(kingdomName))
		{
			return kingdomName;
		}
		return "No name";
	}

	public string GetKingdomName(bool raw = false)
	{
		int localPlayerIndexInData = GetLocalPlayerIndexInData();
		return GetKingdomName(localPlayerIndexInData, raw);
	}

	public string GetKingdomName(string player_id, bool raw = false)
	{
		int indexInPlayerData = GetIndexInPlayerData(player_id);
		return GetKingdomName(indexInPlayerData, raw);
	}

	public string GetKingdomName(int player_index, bool raw = false)
	{
		if (!raw && state < State.Started && CampaignUtils.IsRandomKingdomRuleActive(this))
		{
			return "random";
		}
		Value var = GetVar(RemoteVars.DataType.PersistentPlayerData, player_index, "kingdom_name");
		if (state < State.Started && CampaignUtils.GetSelectedOption(this, "pick_kingdom")?.key == "province")
		{
			var = GetVar(RemoteVars.DataType.PersistentPlayerData, player_index, "origin_realm");
		}
		return var.String();
	}

	public string GetOriginRealmName(int player_index)
	{
		return GetVar(RemoteVars.DataType.PersistentPlayerData, player_index, "origin_realm").String();
	}

	public void SetPlayerKingdomName(int player_index, string kingdom_name, string origin_realm, bool send_data_changed = true)
	{
		RemoteVars remoteVars = GetRemoteVars(RemoteVars.DataType.PersistentPlayerData, player_index);
		if (remoteVars != null)
		{
			remoteVars.Set("origin_realm", origin_realm, send_data_changed: false);
			remoteVars.Set("kingdom_name", kingdom_name, send_data_changed);
			SetPlayerKingdomTeam(player_index, kingdom_name);
		}
	}

	public void SetLocalPlayerKingdomName(string kingdom_name, string origin_realm, bool send_data_changed = true)
	{
		int localPlayerIndex = GetLocalPlayerIndex();
		SetPlayerKingdomName(localPlayerIndex, kingdom_name, origin_realm, send_data_changed);
	}

	public void SetPlayerKingdomTeam(int player_index, string kingdom_name)
	{
		if (player_index < 0 || player_index > 6)
		{
			LogError($"SetPlayerKingdomTeam() called for player index {player_index}");
		}
		else if (IsCustomFromSave() && !string.IsNullOrEmpty(kingdom_name))
		{
			int teamForKingdomName = GetTeamForKingdomName(kingdom_name);
			if (teamForKingdomName > -1)
			{
				CampaignUtils.SetTeam(this, player_index, teamForKingdomName);
			}
		}
	}

	public int GetCoAIndex()
	{
		string mapName = GetMapName();
		string period = GetPeriod();
		string kingdomName = GetKingdomName();
		return CoAMapping.GetCoAIndex(mapName, period, kingdomName);
	}

	public bool GetPlayerAllowOffline(int player_index)
	{
		return GetVar(RemoteVars.DataType.PersistentPlayerData, player_index, "allow_offline").Bool();
	}

	public void SetLocalPlayerAllowOffline(bool allow_offline, bool send_data_changed = true)
	{
		SetLocalPlayerVar(RemoteVars.DataType.PersistentPlayerData, "allow_offline", allow_offline, send_data_changed);
	}

	public RuntimeState GetPlayerRuntimeState(int player_index)
	{
		if (Enum.TryParse<RuntimeState>(GetVar(RemoteVars.DataType.NonPersistentPlayerData, player_index, "runtime_state").String("Offline"), out var result))
		{
			return result;
		}
		return RuntimeState.Offline;
	}

	public RuntimeState GetPlayerRuntimeState(string player_id)
	{
		int playerIndex = GetPlayerIndex(player_id);
		if (playerIndex < 0)
		{
			return RuntimeState.Offline;
		}
		return GetPlayerRuntimeState(playerIndex);
	}

	public RuntimeState GetLocalPlayerRuntimeState()
	{
		int localPlayerIndex = GetLocalPlayerIndex();
		return GetPlayerRuntimeState(localPlayerIndex);
	}

	public RuntimeState GetRuntimeState()
	{
		RuntimeState runtimeState = RuntimeState.Offline;
		for (int i = 0; i < 6; i++)
		{
			RuntimeState playerRuntimeState = GetPlayerRuntimeState(i);
			if (playerRuntimeState > runtimeState)
			{
				runtimeState = playerRuntimeState;
			}
		}
		return runtimeState;
	}

	public bool IsEveryoneOfflineOrPreparingToMigrate()
	{
		for (int i = 0; i < 6; i++)
		{
			RuntimeState playerRuntimeState = GetPlayerRuntimeState(i);
			if (playerRuntimeState != RuntimeState.Offline && playerRuntimeState != RuntimeState.PreparingToMigrate)
			{
				return false;
			}
		}
		return true;
	}

	public bool IsEveryoneOfflineOrReadyToMigrate()
	{
		for (int i = 0; i < 6; i++)
		{
			RuntimeState playerRuntimeState = GetPlayerRuntimeState(i);
			if (playerRuntimeState != RuntimeState.Offline && playerRuntimeState < RuntimeState.PreparingToStart)
			{
				return false;
			}
		}
		return true;
	}

	public string GetAuthorityPlayerID()
	{
		if (!is_multiplayer)
		{
			return GetLocalPlayerID();
		}
		if (state < State.Started)
		{
			return GetOwnerID();
		}
		string text = CalcHostID();
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return null;
	}

	public bool IsAuthority()
	{
		return GetAuthorityPlayerID() == GetLocalPlayerID();
	}

	public bool IsPlayerReady(int player_index)
	{
		if (player_index == GetPlayerIndex(GetAuthorityPlayerID()))
		{
			return true;
		}
		return GetVar(RemoteVars.DataType.NonPersistentPlayerData, player_index, "ready").Bool();
	}

	public bool IsLocalPlayerReady()
	{
		int localPlayerIndex = GetLocalPlayerIndex();
		return IsPlayerReady(localPlayerIndex);
	}

	public void SetLocalPlayerReady(bool ready, bool send_data_changed = true)
	{
		SetLocalPlayerVar(RemoteVars.DataType.NonPersistentPlayerData, "ready", ready, send_data_changed);
	}

	public void SetPlayerReady(int index, bool ready, bool send_data_changed = true)
	{
		SetVar(RemoteVars.DataType.NonPersistentPlayerData, index, "ready", ready, send_data_changed);
	}

	private void UnreadyAllPlayersOnVarChange(RemoteVars vars, string key, Value old_val, Value new_val)
	{
		if (vars.data_type == RemoteVars.DataType.NonPersistentPlayerData && vars.IsAuthority() && key == "runtime_state")
		{
			if (vars.campaign.GetPlayerRuntimeState(vars.player_idx) == RuntimeState.Offline)
			{
				vars.Set("ready", Value.Unknown);
			}
		}
		else
		{
			if (!vars.IsPersistent() || string.IsNullOrEmpty(key) || old_val == new_val)
			{
				return;
			}
			switch (key)
			{
			case "state":
				return;
			case "allow_offline":
				return;
			}
			for (int i = 0; i < 6; i++)
			{
				if (!string.IsNullOrEmpty(GetPlayerID(i)))
				{
					RemoteVars remoteVars = GetRemoteVars(RemoteVars.DataType.NonPersistentPlayerData, i);
					if (remoteVars != null && remoteVars.GetVar("ready").Bool())
					{
						remoteVars.Set("ready", false);
					}
				}
			}
		}
	}

	public bool GetPlayerGameLoaded(int player_index)
	{
		return GetVar(RemoteVars.DataType.NonPersistentPlayerData, player_index, "game_loaded").Bool();
	}

	public bool GetLocalPlayerGameLoaded()
	{
		int localPlayerIndex = GetLocalPlayerIndex();
		return GetPlayerGameLoaded(localPlayerIndex);
	}

	public void SetLocalPlayerGameLoaded(bool game_loaded, bool send_data_changed = true)
	{
		SetLocalPlayerVar(RemoteVars.DataType.NonPersistentPlayerData, "game_loaded", game_loaded, send_data_changed);
	}

	public void UpdatePlayersAI(Game game)
	{
		for (int i = 0; i < 6; i++)
		{
			string kingdomName = GetKingdomName(i);
			if (!string.IsNullOrEmpty(kingdomName))
			{
				game.GetKingdom(kingdomName)?.UpdateAIState(is_player: true);
			}
		}
	}

	public void SetModID(string mod_id, bool send_data_changed = true)
	{
		campaignData?.Set("mod_id", mod_id, send_data_changed);
	}

	public string GetModID()
	{
		return GetVar("mod_id").String();
	}

	public override string ToString()
	{
		string text = (IsMultiplayerCampaign() ? "MP" : "SP");
		return $"[{text}] [{idx}] [{state}] [{GetRuntimeState()}]" + "'" + GetNameNonLocalized() + "' (" + GetMapName() + "/" + GetPeriod() + "/" + GetKingdomName() + "): " + id;
	}

	public string Dump()
	{
		string text = ToString();
		text = text + "\nAuthority: " + GetAuthorityPlayerID() + ", Host: " + CalcHostID();
		for (int i = 0; i < 6; i++)
		{
			string playerID = GetPlayerID(i, ignore_left_players: false);
			if (!string.IsNullOrEmpty(playerID))
			{
				SlotState slotState = GetSlotState(i);
				string text2 = "";
				string kingdomName = GetKingdomName(i);
				int team = CampaignUtils.GetTeam(this, i);
				text2 = $"kingdom: {kingdomName}, team: {team}, ";
				Value var = GetVar(RemoteVars.DataType.NonPersistentPlayerData, i, "runtime_state");
				Value var2 = GetVar(RemoteVars.DataType.NonPersistentPlayerData, i, "ready");
				text += $"\n  {i}: [{slotState}] {playerID} ({text2}runtime state: {var}, ready: {var2}";
			}
		}
		text = text + "\nVars:\n" + campaignData?.Dump("  ");
		for (int j = 0; j < 6; j++)
		{
			RemoteVars remoteVars = GetRemoteVars(RemoteVars.DataType.PersistentPlayerData, j);
			if (remoteVars?.vars == null || remoteVars.vars.Empty())
			{
				remoteVars = null;
			}
			RemoteVars remoteVars2 = GetRemoteVars(RemoteVars.DataType.NonPersistentPlayerData, j);
			if (remoteVars2?.vars == null || remoteVars2.vars.Empty())
			{
				remoteVars2 = null;
			}
			if (remoteVars != null || remoteVars2 != null)
			{
				text += $"\nPlayer {j}:";
				if (remoteVars != null)
				{
					text = text + "\n" + remoteVars.Dump("  ");
				}
				if (remoteVars2 != null)
				{
					text = text + "\n----\n" + remoteVars2.Dump("  ");
				}
			}
		}
		return text;
	}

	public static void Log(string msg)
	{
		msg = DateTime.Now.ToString("HH:mm:ss.fff: ") + msg;
		Debug.Log(msg);
	}

	public static void LogWarning(string msg)
	{
		msg = DateTime.Now.ToString("HH:mm:ss.fff: ") + msg;
		Debug.LogWarning(msg);
	}

	public static void LogError(string msg)
	{
		msg = DateTime.Now.ToString("HH:mm:ss.fff: ") + msg;
		Debug.LogError(msg);
	}
}
