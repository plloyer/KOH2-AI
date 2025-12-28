using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Logic;

public class Multiplayer : Object
{
	public enum Type
	{
		Client,
		ServerClient,
		Server
	}

	public enum NetworkTransportType
	{
		TCP,
		THQNO,
		Internal
	}

	public enum MessageId
	{
		CMD,
		DEBUG,
		UNIQUE_STRINGS,
		CONNECT,
		INCORRECT_CHANNEL,
		ACKNOWLEDGE,
		HEARTBEAT,
		DISCONNECT,
		FULL_GAME_STATE,
		LOAD_GAME,
		OBJ_EVENT,
		OBJ_STATE,
		OBJ_SUBSTATE,
		REQUEST_SEND,
		REQUEST_RESPONSE,
		SEND_DATA,
		JOIN_CAMPAIGN_REQUEST,
		CHAT_MESSAGE,
		BEGIN_GAME_STATE_DUMP,
		GAME_STATE_DUMP,
		END_GAME_STATE_DUMP,
		JOIN_CAMPAIGN_FAIL_RESPONSE,
		INTERUPT_COUNTDOWN,
		REQUEST_POLITICAL_DATA,
		POLITICAL_DATA
	}

	public class PlayerData
	{
		public Multiplayer owner;

		public string name;

		public string id;

		public string _kingdomName;

		public int team;

		public int pid;

		private int _kingdomId;

		public string gameState;

		public string kingdomName
		{
			get
			{
				return _kingdomName;
			}
			set
			{
				_kingdomName = value;
				_kingdomId = -1;
			}
		}

		public int kingdomId
		{
			get
			{
				if (_kingdomId <= 0)
				{
					if (string.IsNullOrEmpty(kingdomName))
					{
						return 0;
					}
					Game game = owner?.game;
					if (game == null)
					{
						return 0;
					}
					Kingdom kingdom = game.GetKingdom(kingdomName);
					if (kingdom != null)
					{
						_kingdomId = kingdom.id;
					}
				}
				return _kingdomId;
			}
		}

		public bool IsValid()
		{
			return !string.IsNullOrEmpty(id);
		}

		public void Read(Serialization.IReader reader, int idx)
		{
			name = reader.ReadRawStr("name_", idx);
			id = reader.ReadRawStr("id_", idx);
			team = reader.Read7BitSigned("team_", idx);
			pid = reader.Read7BitUInt("pid_", idx);
			kingdomName = reader.ReadStr("kingdomName_", idx);
		}

		public void Write(Serialization.IWriter writer, int idx)
		{
			writer.WriteRawStr(name, "name_", idx);
			writer.WriteRawStr(id, "id_", idx);
			writer.Write7BitSigned(team, "team_", idx);
			writer.Write7BitUInt(pid, "pid_", idx);
			writer.WriteStr(kingdomName, "kingdomName_", idx);
		}

		public override string ToString()
		{
			return $"PID: {pid}, player: {id} ({name})" + $", kingdom: {_kingdomId} ({_kingdomName})";
		}
	}

	public static class CurrentPlayers
	{
		private static List<PlayerData> currentPlayers = new List<PlayerData>();

		public static void Add(PlayerData playerData)
		{
			if (playerData == null)
			{
				Error("Calling CurrentPlayers.Add with null playerData");
				return;
			}
			if (playerData.pid == 0)
			{
				Error("Calling CurrentPlayers.Add with playerData with PID = 0");
				return;
			}
			lock (currentPlayers)
			{
				if (GetByPID(playerData.pid) == null)
				{
					currentPlayers.Add(playerData);
				}
				else
				{
					Error($"Calling CurrentPlayers.Add with playerData with already existing PID {playerData.pid}");
				}
			}
			if (playerData.owner != null && playerData.owner.game != null && playerData.kingdomName != null)
			{
				Kingdom kingdom = playerData.owner.game.GetKingdom(playerData.kingdomId);
				if (kingdom == null)
				{
					playerData.owner.game.Error($"Invalid player kingdom id {playerData.kingdomId} for player with pid {playerData.pid}. Multiplayer: {playerData.owner.ToString()}");
				}
				else
				{
					kingdom.SetAIState(enabled: false);
				}
			}
		}

		public static bool Remove(PlayerData playerData, bool isUngracefulDisconnecting = false)
		{
			bool flag = false;
			lock (currentPlayers)
			{
				return currentPlayers.Remove(playerData);
			}
		}

		public static void Clear()
		{
			lock (currentPlayers)
			{
				currentPlayers.Clear();
			}
		}

		public static int Count()
		{
			return currentPlayers.Count;
		}

		public static PlayerData GetByPID(int pid)
		{
			for (int i = 0; i < currentPlayers.Count; i++)
			{
				if (currentPlayers[i] != null && currentPlayers[i].pid == pid)
				{
					return currentPlayers[i];
				}
			}
			return null;
		}

		public static PlayerData GetByGUID(string id)
		{
			for (int i = 0; i < currentPlayers.Count; i++)
			{
				if (currentPlayers[i] != null && currentPlayers[i].id == id)
				{
					return currentPlayers[i];
				}
			}
			return null;
		}

		public static PlayerData GetByKingdom(string kingdomName)
		{
			for (int i = 0; i < currentPlayers.Count; i++)
			{
				if (currentPlayers[i] != null && currentPlayers[i].kingdomName == kingdomName)
				{
					return currentPlayers[i];
				}
			}
			return null;
		}

		public static PlayerData GetByKingdom(int kingdomId)
		{
			for (int i = 0; i < currentPlayers.Count; i++)
			{
				if (currentPlayers[i] != null && currentPlayers[i].kingdomId == kingdomId)
				{
					return currentPlayers[i];
				}
			}
			return null;
		}

		public static Kingdom GetKingdomByPID(int pid)
		{
			if (pid <= 0)
			{
				return null;
			}
			for (int i = 0; i < currentPlayers.Count; i++)
			{
				if (currentPlayers[i] != null && currentPlayers[i].pid == pid)
				{
					return (currentPlayers[i].owner?.game)?.GetKingdom(currentPlayers[i].kingdomId);
				}
			}
			return null;
		}

		public static Kingdom GetKingdomByGUID(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			for (int i = 0; i < currentPlayers.Count; i++)
			{
				if (currentPlayers[i] != null && currentPlayers[i].id == id)
				{
					return (currentPlayers[i].owner?.game)?.GetKingdom(currentPlayers[i].kingdomId);
				}
			}
			return null;
		}

		public static List<int> GetUniqueKingdomIDs()
		{
			List<int> list = new List<int>();
			for (int i = 0; i < currentPlayers.Count; i++)
			{
				if (currentPlayers[i] != null && !list.Contains(currentPlayers[i].kingdomId))
				{
					list.Add(currentPlayers[i].kingdomId);
				}
			}
			return list;
		}

		public static List<PlayerData> GetAll()
		{
			return currentPlayers;
		}

		public static void UpdateKingdom(int pid, string kingdomName)
		{
			PlayerData byPID = GetByPID(pid);
			if (byPID == null)
			{
				Warning($"UpdateKingdom called with pid {pid}. That player data in Multiplayer.CurrentPlayers is null!");
				return;
			}
			string kingdomName2 = byPID.kingdomName;
			byPID.kingdomName = kingdomName;
			if (byPID.owner == null || byPID.owner.game == null)
			{
				return;
			}
			Kingdom kingdom = byPID.owner.game.GetKingdom(kingdomName2);
			kingdom?.UpdateAIState();
			Kingdom kingdom2 = byPID.owner.game.GetKingdom(byPID.kingdomName);
			kingdom2?.SetAIState(enabled: false);
			if (kingdom2 == kingdom)
			{
				return;
			}
			kingdom?.NotifyListenersDelayed("players_changed", true, process_triggers: true, profile: true);
			kingdom2?.NotifyListenersDelayed("players_changed", null, process_triggers: true, profile: true);
			for (int i = 0; i < byPID.owner.game.kingdoms.Count; i++)
			{
				Kingdom kingdom3 = byPID.owner.game.kingdoms[i];
				if (kingdom3 != null && !kingdom3.IsDefeated() && kingdom3 != kingdom2 && kingdom3 != kingdom)
				{
					kingdom3.NotifyListenersDelayed("players_changed", null, process_triggers: true, profile: true);
				}
			}
			kingdom2?.OnChangedAnalytics("player_kingdom", kingdomName2, kingdomName);
		}

		public static bool KingdomIsPlayer(string kingdomName)
		{
			for (int i = 0; i < currentPlayers.Count; i++)
			{
				if (currentPlayers[i] != null && currentPlayers[i].kingdomName == kingdomName)
				{
					return true;
				}
			}
			return false;
		}
	}

	public enum Status
	{
		Offline,
		Listening,
		Connecting,
		Connected,
		Disconnecting,
		Disconnected,
		ShutDown
	}

	public enum ConnectionReason
	{
		Invalid,
		Meta,
		InGame
	}

	public enum LogLevel
	{
		None,
		Bytes,
		Messages,
		BytesAndMessages,
		Heartbeat,
		HeartbeatAndMessages,
		Objects,
		Custom
	}

	public class ConnectRequest : Coroutine.IResume
	{
		public Multiplayer multiplayer;

		public ConnectRequest(Multiplayer multiplayer)
		{
			this.multiplayer = multiplayer;
		}

		public bool Resume(Coroutine coro)
		{
			return multiplayer.IsOnline();
		}
	}

	public enum JoinReason
	{
		FromBrowser,
		FromInviteCode,
		FromOverlay
	}

	private struct SSAction
	{
		public System.Action action;

		public bool in_lock;

		public SSAction(System.Action action, bool in_lock)
		{
			this.action = action;
			this.in_lock = in_lock;
		}
	}

	private static int console_log_level = 1;

	private static int file_log_level = 3;

	public static int portKings = 4777;

	public static int portKings2 = 5777;

	public static NetworkTransportType NetworkType = NetworkTransportType.THQNO;

	public Type type;

	public const byte EVENT_BEGIN_OBJ = 0;

	public const byte EVENT_END_OBJ = 1;

	public const byte EVENT_START_OBJ = 2;

	public const byte EVENT_FINISH_OBJ = 3;

	public const byte EVENT_DELETE_OBJ = 4;

	public const byte EVENT_DEL_STATE = 5;

	public const byte EVENT_DEL_SUBSTATE = 6;

	public const byte EVENT_CUSTOM = 16;

	private readonly object StatusLock = new object();

	private Status status;

	private long status_time;

	public static MultiplayerSettings.Def multiplayer_settings;

	public List<Vars> reconnectingPlayersVars = new List<Vars>();

	public bool initial_waiting_finished;

	public int last_pid = 1;

	private bool isListening;

	public Serialization.ObjectsRegistry objects;

	public List<Multiplayer> allServerClients;

	public List<Multiplayer> joiningPlayers;

	public List<Multiplayer> joinedPlayers;

	public List<Multiplayer> leavingPlayers;

	private readonly object Lock = new object();

	public byte[] msg_tx_buf = new byte[AsyncReceiver.BufferSize];

	public Serialization.IWriter msg_writer;

	private Serialization.IReader reader;

	private List<Serialization.ObjectMessage> message_queue;

	private List<Object> send_objects = new List<Object>();

	public bool debug_headers;

	private UniqueStrings sentUniqueStrings;

	private int last_sent_ustrs = 2;

	public byte[] ustr_tx_buf = new byte[AsyncReceiver.BufferSize];

	private UniqueStrings receivedUniqueStrings = new UniqueStrings();

	private Acceptor acceptor;

	private Connection connection;

	public AsyncSender asyncSender;

	public AsyncReceiver asyncReceiver;

	public NetworkProfiler tx_profiler;

	public NetworkProfiler rx_profiler;

	private bool singleplayerNetworkProfilerEnabled;

	public const float SIGN_IN_RETRY_FREQ_MILLIS = 1000f;

	public static int HeartbeatReceiveTimeLimitInMilliseconds = 0;

	public static readonly int HeartbeatSendTimeLimitInMilliseconds = 1000;

	private static readonly object HeartbeatLock = new object();

	private static Dictionary<string, long> last_heartbeat_received = new Dictionary<string, long>();

	public static Stopwatch heartbeat_timer;

	private bool heartbeatRunning;

	public ConnectionReason connectionReason;

	public bool receivedFullState;

	public Multiplayer serverMultiplayer;

	public Stopwatch timer = Stopwatch.StartNew();

	public PlayerData playerData;

	public Chat chat;

	public ChatServer chatServer;

	public readonly LogLevel logLevel;

	public string serverSendLogPath;

	public string serverReceiveLogPath;

	public string clientOneSendLogPath;

	public string clientOneReceiveLogPath;

	public FileWriter sendLogWriter;

	public FileWriter receiveLogWriter;

	private static uint last_uid = 0u;

	public new uint uid;

	public static char player_handle_separator = '#';

	private IPEndPoint ep;

	private static bool error_on_sc_begin_message = false;

	private List<SSAction> ss_queue;

	private Thread ss_thread;

	private AutoResetEvent resume_ss_thread;

	public bool stopSSThread;

	public Multiplayer(Game game, Type type)
		: base(game)
	{
		reader = new BinarySerializeReader(game, OnMsgHeaderErrorCallback);
		uid = ++last_uid;
		this.type = type;
		playerData = new PlayerData();
		playerData.owner = this;
		playerData.id = THQNORequest.userId;
		playerData.name = THQNORequest.playerName;
		if (type != Type.ServerClient)
		{
			sentUniqueStrings = new UniqueStrings();
			objects = new Serialization.ObjectsRegistry();
			if (type == Type.Server)
			{
				allServerClients = new List<Multiplayer>();
				joiningPlayers = new List<Multiplayer>();
				joinedPlayers = new List<Multiplayer>();
				leavingPlayers = new List<Multiplayer>();
			}
			message_queue = new List<Serialization.ObjectMessage>(100000);
			tx_profiler = new NetworkProfiler(debug_headers);
			rx_profiler = new NetworkProfiler(debug_headers);
			StartSSThread();
		}
		chat = new Chat(this);
		if (type == Type.Server)
		{
			chatServer = new ChatServer(this);
		}
		if (multiplayer_settings == null)
		{
			multiplayer_settings = game.defs.GetBase<MultiplayerSettings.Def>();
		}
		if (HeartbeatReceiveTimeLimitInMilliseconds == 0)
		{
			HeartbeatReceiveTimeLimitInMilliseconds = multiplayer_settings.heartbeat_timeout;
		}
		THQNORequest.RegisterMultiplayer(this);
	}

	public void SetPid(int pid)
	{
		if (playerData == null)
		{
			Error($"SetPid(): multiplayer.playerData is null for mp: {this}");
			return;
		}
		playerData.pid = pid;
		if (objects == null)
		{
			Error($"SetPid(): multiplayer.objects is null for mp: {this}");
		}
		else
		{
			objects.SetPID(playerData.pid);
		}
	}

	public void OnMsgHeaderErrorCallback(string message)
	{
		NotifyListenersDelayed("show_network_error_window", message, process_triggers: false, profile: false);
	}

	public void StartLogFileWriters()
	{
		if (logLevel != LogLevel.None)
		{
			bool writeFromThread = logLevel == LogLevel.HeartbeatAndMessages;
			sendLogWriter = new FileWriter(clientOneSendLogPath, FileMode.Append, writeFromThread, base.Log);
			receiveLogWriter = new FileWriter(clientOneReceiveLogPath, FileMode.Append, writeFromThread, base.Log);
		}
	}

	public override void OnUnloadMap()
	{
	}

	public override string ToString()
	{
		string text = ((base.obj_state == ObjState.Started) ? "" : $"[{base.obj_state}]");
		string text2 = type switch
		{
			Type.Server => "S", 
			Type.Client => "C", 
			Type.ServerClient => "SC", 
			_ => type.ToString(), 
		};
		string text3 = connectionReason switch
		{
			ConnectionReason.Invalid => "??", 
			ConnectionReason.Meta => "M", 
			ConnectionReason.InGame => "IG", 
			_ => connectionReason.ToString(), 
		};
		string text4 = $"[{uid}]{text}[{text2}#{text3}] {playerData?.name}: ";
		if (type == Type.Server)
		{
			text4 = text4 + "SCs: " + MPCnt(joinedPlayers, '\0') + MPCnt(joiningPlayers, '+') + MPCnt(leavingPlayers, '-') + ", ";
			text4 = ((!isListening) ? (text4 + "not listening") : (text4 + "listening on " + THQNORequest.userId + ":0"));
		}
		else
		{
			text4 += status;
			if (connection is THQNOConnection tHQNOConnection)
			{
				if (status == Status.Connecting || status == Status.Connected)
				{
					text4 += $" {THQNORequest.userId}:{tHQNOConnection.ownChannel} to {tHQNOConnection.GetTarget()}:{tHQNOConnection.targetChannel}";
				}
				else if (status == Status.Disconnected)
				{
					text4 += $" {THQNORequest.userId}:{tHQNOConnection.ownChannel} from {tHQNOConnection.GetTarget()}:{tHQNOConnection.targetChannel}";
				}
			}
		}
		return text4;
		static string MPCnt(List<Multiplayer> lst, char prefix)
		{
			if (lst == null || lst.Count == 0)
			{
				if (prefix != 0)
				{
					return "";
				}
				return "0";
			}
			if (prefix == '\0')
			{
				return lst.Count.ToString();
			}
			return $"{prefix}{lst.Count}";
		}
	}

	public static void SetLogLevel(int file_log_level, int console_log_level = -1)
	{
		Multiplayer.file_log_level = file_log_level;
		Multiplayer.console_log_level = ((console_log_level < 0) ? file_log_level : console_log_level);
	}

	public static bool LogEnabled(int level)
	{
		if (console_log_level < level)
		{
			return file_log_level >= level;
		}
		return true;
	}

	public static void Log(string msg, int level, Game.LogType type = Game.LogType.Message)
	{
		if (!LogEnabled(level) || msg.Contains("THQNODBG: C++ PlayerDataGetCustomDataResult callback success, result: 1, for user 2_0_0") || msg.Contains("PlayerDataGetCustomDataResultCallback"))
		{
			return;
		}
		if (console_log_level >= level)
		{
			Game.Log(msg, type);
		}
		if (file_log_level >= level)
		{
			switch (type)
			{
			case Game.LogType.Warning:
				msg = "Warning: " + msg;
				break;
			case Game.LogType.Error:
				msg = "ERROR: " + msg;
				break;
			}
			LogToFile(msg);
		}
	}

	public new static void Warning(string msg)
	{
		Log(msg, 1, Game.LogType.Warning);
	}

	public new static void Error(string msg)
	{
		Log(msg, 1, Game.LogType.Error);
	}

	public static string GetLogFilePath()
	{
		return System.IO.Path.Combine(Game.GetSavesRootDir(Game.SavesRoot.Root), "multiplayer.log");
	}

	public static void DeleteLogFile()
	{
		string logFilePath = GetLogFilePath();
		try
		{
			File.Delete(logFilePath);
		}
		catch
		{
		}
	}

	public static void LogToFile(string msg)
	{
		try
		{
			File.AppendAllText(GetLogFilePath(), "---------- " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff:") + "\r\n" + msg + "\r\n");
		}
		catch
		{
		}
	}

	public string GetProjectName()
	{
		if (playerData.name == null || !playerData.name.Contains("_Kings2"))
		{
			return "Kings";
		}
		return "Kings2";
	}

	public string GetTarget()
	{
		if (connection == null)
		{
			return string.Empty;
		}
		return connection.GetTarget();
	}

	public string GetHandle()
	{
		if (NetworkType == NetworkTransportType.THQNO)
		{
			string target = GetTarget();
			if (string.IsNullOrEmpty(target))
			{
				return string.Empty;
			}
			return target + player_handle_separator.ToString() + connectionReason;
		}
		_ = NetworkType;
		return string.Empty;
	}

	public static string GetPlayerHandle(string playerId, ConnectionReason connectionReason)
	{
		if (string.IsNullOrEmpty(playerId))
		{
			Error("GetPlayerHandle() called with empty playerId");
			return null;
		}
		if (connectionReason == ConnectionReason.Invalid)
		{
			Error("GetPlayerHandle() called with invalid connection reason");
			return null;
		}
		return playerId + player_handle_separator + connectionReason;
	}

	public static string GetPlayerId(string playerHandle)
	{
		if (string.IsNullOrEmpty(playerHandle))
		{
			Error("GetPlayerId() called with empty playerHandle");
			return null;
		}
		int num = playerHandle.IndexOf(player_handle_separator);
		if (num < 0)
		{
			Error("GetPlayerId() called with invalid player handle " + playerHandle);
			return null;
		}
		return playerHandle.Substring(0, num);
	}

	public static ConnectionReason GetConnectionReason(string playerHandle)
	{
		if (string.IsNullOrEmpty(playerHandle))
		{
			Error("GetConnectionReason() called with empty playerHandle");
			return ConnectionReason.Invalid;
		}
		int num = playerHandle.IndexOf(player_handle_separator);
		if (num < 0)
		{
			Error("GetConnectionReason() called with invalid player handle " + playerHandle);
			return ConnectionReason.Invalid;
		}
		string text = playerHandle.Substring(num + 1);
		ConnectionReason result = ConnectionReason.Invalid;
		Enum.TryParse<ConnectionReason>(text, out result);
		if (result == ConnectionReason.Invalid)
		{
			Error("Couldn't extract connection reason from enum for value " + text);
		}
		return result;
	}

	private string GetEndPoint()
	{
		if (type != Type.Client)
		{
			Error("Attempting to call GetEndPoint() from multiplayer of type " + type);
			return null;
		}
		return ep?.ToString();
	}

	public bool IsOnline()
	{
		if (type == Type.Server)
		{
			if (joinedPlayers.Count <= 0)
			{
				return leavingPlayers.Count > 0;
			}
			return true;
		}
		if (connection == null)
		{
			return false;
		}
		return connection.IsConnected();
	}

	public bool IsConnected()
	{
		if (type == Type.Server)
		{
			if (joiningPlayers.Count <= 0 && joinedPlayers.Count <= 0)
			{
				return leavingPlayers.Count > 0;
			}
			return true;
		}
		if (connection == null)
		{
			return false;
		}
		if (NetworkType == NetworkTransportType.TCP)
		{
			return connection.IsConnected();
		}
		if (NetworkType == NetworkTransportType.THQNO)
		{
			return status == Status.Connected;
		}
		return false;
	}

	public Multiplayer GetPlayerByKingdomId(int kingdomId)
	{
		if (type != Type.Server)
		{
			Error("Attempting to call GetPlayerByKingdomId() from multiplayer of type " + type);
			return null;
		}
		for (int i = 0; i < joiningPlayers.Count; i++)
		{
			if (joiningPlayers[i].playerData.kingdomId == kingdomId)
			{
				return joiningPlayers[i];
			}
		}
		for (int j = 0; j < joinedPlayers.Count; j++)
		{
			if (joinedPlayers[j].playerData.kingdomId == kingdomId)
			{
				return joinedPlayers[j];
			}
		}
		for (int k = 0; k < leavingPlayers.Count; k++)
		{
			if (leavingPlayers[k].playerData.kingdomId == kingdomId)
			{
				return leavingPlayers[k];
			}
		}
		Error($"No player with kingdom id {kingdomId} found!");
		return null;
	}

	public Multiplayer GetPlayerByPid(int pid)
	{
		if (type != Type.Server)
		{
			Error("Attempting to call GetPlayerByPid() from multiplayer of type " + type);
			return null;
		}
		for (int i = 0; i < joiningPlayers.Count; i++)
		{
			if (joiningPlayers[i].playerData.pid == pid)
			{
				return joiningPlayers[i];
			}
		}
		for (int j = 0; j < joinedPlayers.Count; j++)
		{
			if (joinedPlayers[j].playerData.pid == pid)
			{
				return joinedPlayers[j];
			}
		}
		for (int k = 0; k < leavingPlayers.Count; k++)
		{
			if (leavingPlayers[k].playerData.pid == pid)
			{
				return leavingPlayers[k];
			}
		}
		return null;
	}

	public Kingdom GetPlayerKingdom()
	{
		return game.GetKingdom(playerData.kingdomId);
	}

	public void StartGame(bool new_game, string map_name = null, string fullPath = null)
	{
		if (game.state == Game.State.LoadingMap)
		{
			return;
		}
		if (new_game && string.IsNullOrEmpty(map_name))
		{
			Error("Start New Game called with invalid parameters. map_name: " + map_name);
			return;
		}
		if (game != null)
		{
			game.state = Game.State.LoadingMap;
		}
		if (!string.IsNullOrEmpty(THQNORequest.currentlyEnteredTHQNOLobbyId))
		{
			Coroutine.Start("THQNORequest.LeaveCurrentLobby", THQNORequest.LeaveCurrentLobbyCoro());
		}
		if (game != null && game.campaign != null && game.campaign.state != Campaign.State.Started)
		{
			game.campaign.SetState(Campaign.State.Started);
		}
		if (type == Type.Server && game != null && game.campaign != null && game.campaign.IsMultiplayerCampaign())
		{
			CampaignUtils.ClearGameLoadedCampaignVars(game);
		}
		List<string> parameters = new List<string>
		{
			new_game.ToString(),
			map_name,
			fullPath
		};
		Coroutine.Start("StartGame", coro());
		IEnumerator coro()
		{
			if (!new_game && !string.IsNullOrEmpty(fullPath))
			{
				game.NotifyListeners("loading_saved_game", (game.state == Game.State.Running) ? "in_game" : "main_menu");
			}
			yield return null;
			game.multiplayer.NotifyListeners("show_loading_screen", new_game);
			yield return null;
			game.multiplayer.NotifyListenersDelayed("game_started", parameters, process_triggers: false, profile: false);
		}
	}

	public bool CheckSenderAuthority(Serialization.ObjectMessage msg)
	{
		return true;
	}

	public void UpdatePlayerInCurrentPlayers(int pid, string kingdomName)
	{
		if (type == Type.ServerClient)
		{
			Error("Attempting to call UpdateCurrentPlayers() from multiplayer of type " + type);
			return;
		}
		if (pid == 0)
		{
			Warning($"Updating current players with pid {pid}");
		}
		CurrentPlayers.UpdateKingdom(pid, kingdomName);
		game.teams.Evaluate();
		game.SendState<Game.CurrentPlayersState>();
		NotifyListenersDelayed("players_changed", null, process_triggers: true, profile: true);
	}

	public void AddJoiningPlayer(Multiplayer multiplayer)
	{
		if (type != Type.Server)
		{
			Error("Attempting to call AddJoiningPlayer() from multiplayer of type " + type);
			return;
		}
		lock (Lock)
		{
			if (!joiningPlayers.Contains(multiplayer) && !joinedPlayers.Contains(multiplayer) && !leavingPlayers.Contains(multiplayer))
			{
				joiningPlayers.Add(multiplayer);
			}
		}
	}

	public bool AddLeavingPlayer(Multiplayer multiplayer)
	{
		if (type != Type.Server)
		{
			Error("Attempting to call AddLeavingPlayer() from multiplayer of type " + type);
			return false;
		}
		lock (Lock)
		{
			if (!leavingPlayers.Contains(multiplayer))
			{
				leavingPlayers.Add(multiplayer);
				return true;
			}
		}
		return false;
	}

	private void ClearUniqueStrings()
	{
		sentUniqueStrings.Clear();
		receivedUniqueStrings.Clear();
		last_sent_ustrs = 2;
	}

	public void StartServer(int port = -1)
	{
		if (type != Type.Server)
		{
			Error("Attempting to call StartServer() from multiplayer of type " + type);
			return;
		}
		if (isListening)
		{
			Error($"StartServer called on an already listening Server multiplayer {this}");
			return;
		}
		int port2 = port;
		if (port < 0)
		{
			port2 = portKings;
			if (game != null)
			{
				string projectName = GetProjectName();
				if (projectName == null)
				{
					Error("Project name is null!");
				}
				else if (projectName.Equals("Kings2"))
				{
					port2 = portKings2;
				}
			}
		}
		if (NetworkType == NetworkTransportType.TCP)
		{
			acceptor = new TCPAcceptor(port2, this);
		}
		else if (NetworkType == NetworkTransportType.THQNO)
		{
			acceptor = new THQNOAcceptor(this);
		}
		if (NetworkType == NetworkTransportType.THQNO && !THQNORequest.connected && !THQNORequest.signed_in)
		{
			Error($"Calling StartServer() while using THQNO with THQNORequest.connected: {THQNORequest.connected}, THQNORequest.signed_inL {THQNORequest.signed_in}");
			return;
		}
		SetStatus(Status.Listening);
		acceptor.StartAccepting();
		isListening = true;
		if (logLevel != LogLevel.None)
		{
			bool writeFromThread = logLevel == LogLevel.HeartbeatAndMessages;
			sendLogWriter = new FileWriter(serverSendLogPath, FileMode.Append, writeFromThread);
		}
	}

	public void StopServer()
	{
		if (LogEnabled(2))
		{
			Log("Stopping server", 2);
		}
		if (type != Type.Server)
		{
			Error("Attempting to call StopServer() from multiplayer of type " + type);
			return;
		}
		Disconnect(gotDisconnectedByPeer: false, isUngraceful: false, destroy: true);
		isListening = false;
		if (acceptor != null)
		{
			acceptor.CleanUp();
		}
	}

	public Multiplayer OnAccept(Connection connection, ConnectionReason connectionReason)
	{
		if (type != Type.Server)
		{
			Error("Attempting to call OnAccept() from multiplayer of type " + type);
			return null;
		}
		if (!isListening && (type != Type.Server || this.connectionReason != ConnectionReason.InGame))
		{
			Warning($"Ignored {connectionReason} connection request");
			return null;
		}
		if (connectionReason != this.connectionReason)
		{
			if (connectionReason != ConnectionReason.InGame)
			{
				Warning($"Ignored {connectionReason} connection request");
				return null;
			}
			if (game?.multiplayer == null)
			{
				Warning($"Ignored {connectionReason} connection request since there is no in-game multiplayer");
				return null;
			}
			if (game.multiplayer == this || game.multiplayer.type != Type.Server || game.multiplayer.connectionReason != connectionReason)
			{
				Warning($"Ignored {connectionReason} connection request since game in-game multiplayer is not valid: {game.multiplayer}");
				return null;
			}
			if (game.campaign == null || !game.campaign.IsMultiplayerCampaign())
			{
				Warning($"Ignored {connectionReason} connection request since game campaign is not multiplayer: {game.campaign}");
				return null;
			}
			if (LogEnabled(2))
			{
				Log($"Forwarding {connectionReason} connection request to {game.multiplayer}", 2);
			}
			return game.multiplayer.OnAccept(connection, connectionReason);
		}
		Multiplayer multiplayer = new Multiplayer(game, Type.ServerClient);
		multiplayer.serverMultiplayer = this;
		multiplayer.debug_headers = debug_headers;
		multiplayer.sentUniqueStrings = sentUniqueStrings;
		multiplayer.tx_profiler = tx_profiler;
		multiplayer.rx_profiler = rx_profiler;
		multiplayer.ss_thread = ss_thread;
		multiplayer.ss_queue = ss_queue;
		multiplayer.resume_ss_thread = resume_ss_thread;
		multiplayer.connectionReason = connectionReason;
		multiplayer.playerData.id = connection.GetTarget();
		FileWriter logFileWriter = null;
		if (logLevel != LogLevel.None)
		{
			bool writeFromThread = logLevel == LogLevel.HeartbeatAndMessages;
			multiplayer.sendLogWriter = new FileWriter(serverSendLogPath, FileMode.Append, writeFromThread);
		}
		if (logLevel == LogLevel.Bytes || logLevel == LogLevel.BytesAndMessages)
		{
			logFileWriter = multiplayer.sendLogWriter;
		}
		multiplayer.connection = connection;
		connection.multiplayer = multiplayer;
		multiplayer.asyncSender = new AsyncSender(multiplayer.connection, logFileWriter);
		multiplayer.asyncSender.profiler = tx_profiler;
		FileWriter logFileWriter2 = null;
		if (logLevel != LogLevel.None)
		{
			bool writeFromThread2 = logLevel == LogLevel.HeartbeatAndMessages;
			multiplayer.receiveLogWriter = new FileWriter(serverReceiveLogPath, FileMode.Append, writeFromThread2);
		}
		if (logLevel == LogLevel.Bytes || logLevel == LogLevel.BytesAndMessages)
		{
			logFileWriter2 = multiplayer.receiveLogWriter;
		}
		multiplayer.asyncReceiver = new AsyncReceiver(multiplayer.connection, logFileWriter2, logLevel == LogLevel.Bytes);
		multiplayer.asyncReceiver.profiler = rx_profiler;
		multiplayer.asyncReceiver.Receive();
		allServerClients.Add(multiplayer);
		NotifyListenersDelayed("client_connected", multiplayer, process_triggers: false, profile: false);
		multiplayer.OnConnect();
		return multiplayer;
	}

	public void HandlePlayerDisconnection(Multiplayer multiplayer)
	{
		if (type != Type.Server)
		{
			Error("Attempting to call OnPlayerDisconnect() from multiplayer of type " + type);
		}
		else
		{
			ServerRemovePlayer(multiplayer);
		}
	}

	public void SendCmd(string cmd)
	{
		QueueSS(delegate
		{
			BeginMessage(MessageId.CMD);
			msg_writer.WriteRawStr(cmd, "cmd");
			SendMessage(isObjectMessage: false);
		});
	}

	public void SendDebug(string message)
	{
		QueueSS(delegate
		{
			BeginMessage(MessageId.DEBUG);
			msg_writer.WriteRawStr(message, "message");
			SendMessage(isObjectMessage: false);
		});
	}

	public ConnectRequest Connect(string serverId = "", ConnectionReason connectionReason = ConnectionReason.Meta, bool isRemote = false)
	{
		if (type != Type.Client)
		{
			Error("Attempting to call Connect() from multiplayer of type " + type);
			return new ConnectRequest(this);
		}
		if (IsOnline() || status == Status.Connecting)
		{
			return new ConnectRequest(this);
		}
		this.connectionReason = connectionReason;
		SetStatus(Status.Connecting);
		if (NetworkType == NetworkTransportType.TCP)
		{
			string ip = "";
			int port = -1;
			if (!string.IsNullOrEmpty(serverId))
			{
				ExtractAddressFromServerId(serverId, out ip, out port);
			}
			try
			{
				IPAddress address = ((!(ip == "")) ? IPAddress.Parse(ip) : IPAddress.Parse("127.0.0.1"));
				int port2 = portKings;
				if (port < 0)
				{
					if (isRemote)
					{
						if (GetProjectName().Equals("Kings"))
						{
							port2 = portKings2;
						}
						else if (GetProjectName().Equals("Kings2"))
						{
							port2 = portKings;
						}
					}
					else if (GetProjectName().Equals("Kings2"))
					{
						port2 = portKings2;
					}
				}
				else
				{
					port2 = port;
				}
				ep = new IPEndPoint(address, port2);
			}
			catch (Exception ex)
			{
				Error("Invalid IP Address: " + ex.Message);
				SetStatus(Status.Disconnected);
				return new ConnectRequest(this);
			}
			if (LogEnabled(2))
			{
				Log(string.Concat("Connecting to ", ep.Address, ":", ep.Port, "..."), 2);
			}
			connection = new TCPConnection(Connection.ConnectionType.Client, null, ep);
		}
		else if (NetworkType == NetworkTransportType.THQNO)
		{
			if (connection != null)
			{
				Error($"{this}: already has a connection on Connect: {connection}");
				connection.multiplayer = null;
			}
			if (THQNOAcceptor.lastChannelUsed < 0)
			{
				THQNOAcceptor.lastChannelUsed = 0;
			}
			THQNOAcceptor.lastChannelUsed++;
			connection = new THQNOConnection(Connection.ConnectionType.Client, serverId, THQNOAcceptor.lastChannelUsed, (int)THQNOAcceptor.ServerChannel);
		}
		connection.multiplayer = this;
		if (logLevel == LogLevel.Bytes || logLevel == LogLevel.BytesAndMessages)
		{
			asyncSender = new AsyncSender(connection, sendLogWriter);
		}
		else
		{
			asyncSender = new AsyncSender(connection);
		}
		asyncSender.profiler = tx_profiler;
		if (logLevel == LogLevel.Bytes || logLevel == LogLevel.BytesAndMessages)
		{
			asyncReceiver = new AsyncReceiver(connection, receiveLogWriter, logLevel == LogLevel.Bytes);
		}
		else
		{
			asyncReceiver = new AsyncReceiver(connection);
		}
		asyncReceiver.profiler = rx_profiler;
		if (LogEnabled(2))
		{
			Log($"{this}: Starting a new connection", 2);
		}
		connection.Connect();
		return new ConnectRequest(this);
	}

	private void ExtractAddressFromServerId(string serverId, out string ip, out int port)
	{
		if (string.IsNullOrEmpty(serverId))
		{
			ip = "";
			port = -1;
			return;
		}
		int num = serverId.IndexOf('#');
		if (num >= 0)
		{
			serverId = serverId.Substring(0, num);
		}
		string[] array = serverId.Split(':');
		ip = array[0];
		if (array.Length < 2)
		{
			port = -1;
		}
		else if (!int.TryParse(array[1], out port))
		{
			Error("Error while parsing port!");
			port = -1;
		}
	}

	public void OnQuit()
	{
		if (type == Type.Server)
		{
			StopServer();
		}
		else
		{
			Disconnect(gotDisconnectedByPeer: false, isUngraceful: false, destroy: true);
		}
		if (type != Type.ServerClient && ss_thread != null)
		{
			ss_thread.Join(500);
		}
	}

	public void ShutDown()
	{
		if (type == Type.ServerClient)
		{
			serverMultiplayer?.allServerClients.Remove(this);
		}
		if (IsConnected() || status == Status.Connecting)
		{
			Disconnect(gotDisconnectedByPeer: false, isUngraceful: false, destroy: true);
		}
		else
		{
			if (status == Status.ShutDown)
			{
				return;
			}
			SetStatus(Status.ShutDown);
			QueueSS(delegate
			{
				Thread.Sleep(100);
				if (type != Type.ServerClient)
				{
					stopSSThread = true;
				}
				if (type != Type.Server)
				{
					connection?.Close();
				}
			});
			if (playerData != null)
			{
				playerData.owner = null;
			}
			THQNORequest.UnregisterMultiplayer(this);
			sendLogWriter?.Dispose();
			receiveLogWriter?.Dispose();
			if (IsValid())
			{
				Destroy();
			}
		}
	}

	protected override void OnDestroy()
	{
		if (status != Status.ShutDown && game != null && !game.IsQuitting())
		{
			Error("Multiplayer was Destroy()-ed directly, use Shutdown() instead");
			ShutDown();
		}
		base.OnDestroy();
	}

	private void OnDisconnectFinished(bool destroy)
	{
		if (destroy)
		{
			ShutDown();
		}
		else
		{
			SetStatus(Status.Disconnected);
		}
	}

	public void Disconnect(bool gotDisconnectedByPeer = false, bool isUngraceful = false, bool destroy = false)
	{
		if (isUngraceful)
		{
			THQNORequest.CheckOnlineServices();
		}
		Status status = this.status;
		if (status >= Status.Disconnecting)
		{
			Warning($"{this}: Disconnect already called");
			return;
		}
		SetStatus(Status.Disconnecting);
		if (type == Type.Server)
		{
			for (int i = 0; i < joiningPlayers.Count; i++)
			{
				Multiplayer multiplayer = joiningPlayers[i];
				if (multiplayer != null && multiplayer.IsValid())
				{
					multiplayer.ShutDown();
				}
			}
			for (int j = 0; j < joinedPlayers.Count; j++)
			{
				Multiplayer multiplayer2 = joinedPlayers[j];
				if (multiplayer2 != null && multiplayer2.IsValid())
				{
					multiplayer2.ShutDown();
				}
			}
			for (int k = 0; k < leavingPlayers.Count; k++)
			{
				Multiplayer multiplayer3 = leavingPlayers[k];
				if (multiplayer3 != null && multiplayer3.IsValid())
				{
					multiplayer3.ShutDown();
				}
			}
			joiningPlayers.Clear();
			joinedPlayers.Clear();
			leavingPlayers.Clear();
			OnDisconnectFinished(destroy);
		}
		else if (type == Type.Client)
		{
			Vars disconnectionVars = new Vars();
			string val = (gotDisconnectedByPeer ? "host" : "you");
			disconnectionVars.Set("disconnectionType", val);
			disconnectionVars.Set("isUngraceful", isUngraceful);
			if (game != null && game.state == Game.State.Running)
			{
				string text = string.Empty;
				_ = string.Empty;
				if (!gotDisconnectedByPeer)
				{
					text = playerData.name;
					_ = playerData.kingdomName;
				}
				else
				{
					string text2 = game?.campaign?.GetHostID(skip_local_player: false);
					PlayerData byGUID = CurrentPlayers.GetByGUID(text2);
					if (byGUID != null)
					{
						text = byGUID.name;
						_ = byGUID.kingdomName;
					}
					else
					{
						Error("Could not find host's player data for id: " + text2);
					}
				}
				disconnectionVars.Set("target", "#" + text);
				disconnectionVars.Set("kingdom", game.GetKingdom(playerData.kingdomName));
				disconnectionVars.Set("player_id", playerData.id);
				disconnectionVars.Set("pid", playerData.pid);
			}
			Game.fullGameStateReceived = false;
			if (gotDisconnectedByPeer)
			{
				lock (connection)
				{
					if (connection.IsConnected() || status == Status.Connecting)
					{
						if (LogEnabled(2))
						{
							Log($"{this} got disconnected by server", 2);
						}
						connection.OnDisconnected();
					}
				}
				StopHeartbeat();
				SetStatus(Status.Disconnected);
				MainThreadUpdates.Perform(delegate
				{
					NotifyListeners("client_disconnected", disconnectionVars, may_trigger: false, profile: false);
					Vars vars3 = new Vars(this);
					vars3.Set("multiplayer", this);
					vars3.Set("is_ungraceful", isUngraceful);
					NotifyListeners("connection_dropped", vars3, may_trigger: false, profile: false);
					OnDisconnectFinished(destroy);
				});
			}
			else
			{
				if (!connection.IsConnected() && status != Status.Connecting)
				{
					return;
				}
				if (NetworkType == NetworkTransportType.THQNO)
				{
					bool send_disconnect_message = status == Status.Connected;
					QueueSS(delegate
					{
						if (send_disconnect_message)
						{
							if (LogEnabled(2))
							{
								Log($"{this}: Sending DISCONNECT message", 2);
							}
							BeginMessage(MessageId.DISCONNECT);
							SendMessage(isObjectMessage: false);
						}
						MainThreadUpdates.Perform(delegate
						{
							if (LogEnabled(2))
							{
								Log($"{this} disconnected", 2);
							}
							connection.OnDisconnected();
							StopHeartbeat();
							SetStatus(Status.Disconnected);
							NotifyListeners("client_disconnected", disconnectionVars, may_trigger: false, profile: false);
							Vars vars3 = new Vars(this);
							vars3.Set("multiplayer", this);
							NotifyListeners("connection_dropped", vars3, may_trigger: false, profile: false);
							OnDisconnectFinished(destroy);
						});
					});
				}
				else
				{
					if (LogEnabled(2))
					{
						Log($"{this} disconnected", 2);
					}
					connection.OnDisconnected();
					NotifyListenersDelayed("client_disconnected", disconnectionVars, process_triggers: false, profile: false);
					Vars vars = new Vars(this);
					vars.Set("multiplayer", this);
					NotifyListenersDelayed("connection_dropped", vars, process_triggers: false, profile: false);
					OnDisconnectFinished(destroy);
				}
			}
		}
		else
		{
			if (SinglePlayerNetworkProfilerEnabled())
			{
				return;
			}
			Vars disconnectionVars2 = new Vars();
			string val2 = ((!gotDisconnectedByPeer) ? "you" : "anotherPlayer");
			disconnectionVars2.Set("disconnectionType", val2);
			disconnectionVars2.Set("isUngraceful", isUngraceful);
			disconnectionVars2.Set("target", "#" + playerData.name);
			disconnectionVars2.Set("kingdom", game.GetKingdom(playerData.kingdomName));
			disconnectionVars2.Set("player_id", playerData.id);
			disconnectionVars2.Set("pid", playerData.pid);
			serverMultiplayer.NotifyListenersDelayed("players_changed", null, process_triggers: false, profile: false);
			if (NetworkType == NetworkTransportType.THQNO)
			{
				QueueSS(delegate
				{
					if (!gotDisconnectedByPeer)
					{
						if (LogEnabled(2))
						{
							Log($"{this}: Sending DISCONNECT message", 2);
						}
						BeginMessage(MessageId.DISCONNECT);
						SendMessage(isObjectMessage: false);
					}
					MainThreadUpdates.Perform(delegate
					{
						lock (connection)
						{
							if (connection.IsConnected())
							{
								if (LogEnabled(2))
								{
									Log($"{this} disconnected", 2);
								}
								string handle = GetHandle();
								(serverMultiplayer.acceptor as THQNOAcceptor)?.RemoveConnection(handle);
								connection.OnDisconnected();
								SetStatus(Status.Disconnected);
							}
						}
						StopHeartbeat();
						serverMultiplayer.NotifyListeners("client_disconnected", disconnectionVars2, may_trigger: false, profile: false);
						Vars vars3 = new Vars(this);
						vars3.Set("multiplayer", this);
						serverMultiplayer.NotifyListeners("connection_dropped", vars3, may_trigger: false, profile: false);
						OnDisconnectFinished(destroy);
					});
				});
			}
			else
			{
				if (connection.IsConnected())
				{
					if (LogEnabled(2))
					{
						Log($"{this} disconnected", 2);
					}
					connection.OnDisconnected();
				}
				NotifyListenersDelayed("client_disconnected", disconnectionVars2, process_triggers: false, profile: false);
				Vars vars2 = new Vars(this);
				vars2.Set("multiplayer", this);
				serverMultiplayer.NotifyListenersDelayed("connection_dropped", vars2, process_triggers: false, profile: false);
				OnDisconnectFinished(destroy);
			}
			if (CurrentPlayers.Remove(playerData, isUngraceful))
			{
				game.SendState<Game.CurrentPlayersState>();
			}
		}
	}

	public bool IsReadyToDisconnect()
	{
		lock (ss_queue)
		{
			if (ss_queue == null || ss_queue.Count == 0)
			{
				return true;
			}
			return false;
		}
	}

	public void OnReceive(MemStream stream)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnReceive() from multiplayer of type " + type);
			return;
		}
		if (NetworkType == NetworkTransportType.THQNO)
		{
			ResetHertbeatReceivedTimer();
		}
		if (!(reader is BinarySerializeReader binarySerializeReader))
		{
			Error("Critical: OnReceive reader is not a BinarySerializeReader!");
			return;
		}
		binarySerializeReader.Init(stream, debug_headers);
		binarySerializeReader.unique_strings = receivedUniqueStrings;
		MessageId messageId = (MessageId)binarySerializeReader.ReadByte(null);
		if (!IsValid())
		{
			return;
		}
		if (receiveLogWriter != null)
		{
			if (logLevel == LogLevel.Messages)
			{
				receiveLogWriter.WriteLine(messageId.ToString());
			}
			else if (logLevel == LogLevel.BytesAndMessages || logLevel == LogLevel.HeartbeatAndMessages)
			{
				receiveLogWriter.WriteLine(" " + messageId);
			}
		}
		lock (Lock)
		{
			Connection obj = connection;
			if (obj != null && obj.status == Connection.Status.Connecting && messageId != MessageId.ACKNOWLEDGE && messageId != MessageId.INCORRECT_CHANNEL && messageId != MessageId.HEARTBEAT)
			{
				Error($"{this} receiving message {messageId} while it's Connection is in status {connection.status}!");
				return;
			}
			switch (messageId)
			{
			case MessageId.CMD:
				OnCmd(binarySerializeReader);
				break;
			case MessageId.DEBUG:
				OnDebug(binarySerializeReader);
				break;
			case MessageId.UNIQUE_STRINGS:
				OnUniqueString(binarySerializeReader);
				break;
			case MessageId.INCORRECT_CHANNEL:
				OnIncorrectChannel(binarySerializeReader);
				break;
			case MessageId.ACKNOWLEDGE:
				OnAcknowledge(binarySerializeReader);
				break;
			case MessageId.HEARTBEAT:
				OnHeartbeat(binarySerializeReader);
				break;
			case MessageId.DISCONNECT:
				OnDisconnect(binarySerializeReader);
				break;
			case MessageId.FULL_GAME_STATE:
				OnFullGameState(binarySerializeReader);
				break;
			case MessageId.LOAD_GAME:
				OnLoadGame(binarySerializeReader);
				break;
			case MessageId.OBJ_EVENT:
				OnObjEvent(binarySerializeReader);
				break;
			case MessageId.OBJ_STATE:
				OnObjState(binarySerializeReader);
				break;
			case MessageId.OBJ_SUBSTATE:
				OnObjSubstate(binarySerializeReader);
				break;
			case MessageId.REQUEST_SEND:
				Request.OnRequest(this, binarySerializeReader);
				break;
			case MessageId.REQUEST_RESPONSE:
				Request.OnResponse(this, binarySerializeReader);
				break;
			case MessageId.SEND_DATA:
				OnSendData(binarySerializeReader);
				break;
			case MessageId.JOIN_CAMPAIGN_REQUEST:
				OnJoinRequest(binarySerializeReader);
				break;
			case MessageId.JOIN_CAMPAIGN_FAIL_RESPONSE:
				OnJoinRequestFailResponse(binarySerializeReader);
				break;
			case MessageId.INTERUPT_COUNTDOWN:
				OnInteruptCountdown(binarySerializeReader);
				break;
			case MessageId.CHAT_MESSAGE:
				OnChatMessage(binarySerializeReader);
				break;
			case MessageId.BEGIN_GAME_STATE_DUMP:
				OnBeginGameStateDump(binarySerializeReader);
				break;
			case MessageId.GAME_STATE_DUMP:
				OnGameStateDump(binarySerializeReader);
				break;
			case MessageId.END_GAME_STATE_DUMP:
				OnEndGameStateDump(binarySerializeReader);
				break;
			case MessageId.REQUEST_POLITICAL_DATA:
				OnRequestPoliticalData(binarySerializeReader);
				break;
			case MessageId.POLITICAL_DATA:
				OnPoliticalData(binarySerializeReader);
				break;
			default:
				Error($"Unhandled message: {messageId.ToString()} ({(byte)messageId})");
				return;
			}
			if (binarySerializeReader.Position() != binarySerializeReader.Length())
			{
				Error($"ObjectMessage not completely read: {messageId.ToString()} ({(byte)messageId}) --- read {binarySerializeReader.Position()} bytes, {binarySerializeReader.Length()} total");
			}
		}
	}

	public void OnNetworkConnect(int ownChannel = -1)
	{
		if (type != Type.Client)
		{
			Error("Attempting to call OnConnect() from multiplayer of type " + type);
			return;
		}
		asyncReceiver.Receive();
		connection.status = Connection.Status.Connecting;
		QueueSS(delegate
		{
			if (LogEnabled(2))
			{
				Log($"{this}: sending CONNECT message", 2);
			}
			BeginMessage(MessageId.CONNECT);
			msg_writer.WriteByte((byte)connectionReason, null);
			if (NetworkType == NetworkTransportType.THQNO)
			{
				msg_writer.Write7BitUInt(ownChannel, null);
			}
			SendMessage(isObjectMessage: false);
		});
	}

	public void SendFullGameStateMessage()
	{
		if (type != Type.ServerClient)
		{
			Error("Attempting to call SendFullGameStateSent() from multiplayer of type " + type);
			return;
		}
		if (LogEnabled(2))
		{
			Log($"{this} finished sending full game state", 2);
		}
		QueueSS(delegate
		{
			BeginMessage(MessageId.FULL_GAME_STATE);
			SendMessage(isObjectMessage: false);
		});
	}

	public void SendLoadGame(string mapName, string period)
	{
		if (type == Type.Client)
		{
			Error("Attempting to call SendLoadGame() from multiplayer of type " + type);
		}
		else if (type == Type.Server)
		{
			for (int i = 0; i < joiningPlayers.Count; i++)
			{
				joiningPlayers[i].SendLoadGame(mapName, period);
			}
			for (int j = 0; j < joinedPlayers.Count; j++)
			{
				joinedPlayers[j].SendLoadGame(mapName, period);
			}
			MoveAllPlayersToJoiningPlayers();
		}
		else
		{
			QueueSS(delegate
			{
				BeginMessage(MessageId.LOAD_GAME);
				msg_writer.WriteStr(mapName, "mapName");
				msg_writer.WriteStr(period, "period");
				SendMessage(isObjectMessage: false);
			});
		}
	}

	private void MoveAllPlayersToJoiningPlayers()
	{
		lock (Lock)
		{
			joiningPlayers.AddRange(joinedPlayers);
			joinedPlayers.Clear();
		}
	}

	private void OnCmd(Serialization.IReader reader)
	{
		reader.ReadRawStr("cmd");
	}

	private void OnDebug(Serialization.IReader reader)
	{
		string text = reader.ReadRawStr("message");
		if (text == "test_p2p")
		{
			if (LogEnabled(2))
			{
				Log($"{this} received a THQNO TEST P2P message after ungraceful disconnect!", 2);
			}
		}
		else if (LogEnabled(2))
		{
			Log($"{this} received a DEBUG message: {text}", 2);
		}
	}

	private void OnUniqueString(Serialization.IReader reader)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnUniqueString() from multiplayer of type " + type);
			return;
		}
		int num = reader.Read7BitUInt(null);
		int num2 = reader.Read7BitUInt(null);
		int num3 = receivedUniqueStrings.Count - num;
		List<string> list = new List<string>();
		for (int i = 0; i < num3; i++)
		{
			list.Add(receivedUniqueStrings.Get(i + num));
		}
		int count = receivedUniqueStrings.Count;
		for (int j = 0; j < num2; j++)
		{
			string text = reader.ReadRawStr(null);
			int num4 = j + num;
			if (num4 >= count)
			{
				receivedUniqueStrings.Register(text);
			}
			else
			{
				receivedUniqueStrings.Replace(num4, text);
			}
		}
		if (num >= count)
		{
			return;
		}
		for (int k = 0; k < num3; k++)
		{
			int num5 = k + num;
			if (list[k] != receivedUniqueStrings.Get(num5))
			{
				Warning($"{this}: Unique strings merged. Messed up {num5}: '{list[k]}' to '{receivedUniqueStrings.Get(num5)}'");
			}
		}
	}

	private void OnConnect()
	{
		if (type != Type.ServerClient)
		{
			Error("Attempting to call OnConnect() from multiplayer of type " + type);
			return;
		}
		connection.status = Connection.Status.Connecting;
		SetStatus(Status.Connecting);
		QueueSS(delegate
		{
			if (LogEnabled(2))
			{
				Log($"{this} sending ACKNOWLEDGE message", 2);
			}
			BeginMessage(MessageId.ACKNOWLEDGE);
			if (NetworkType == NetworkTransportType.THQNO)
			{
				int ownChannel = (connection as THQNOConnection).ownChannel;
				msg_writer.Write7BitSigned(ownChannel, null);
			}
			msg_writer.WriteBool(debug_headers, null);
			SendMessage(isObjectMessage: false);
		});
		StartHeartbeat();
	}

	private void OnIncorrectChannel(Serialization.IReader reader)
	{
		if (type != Type.Client)
		{
			Error("Attempting to call OnIncorrectChannel() from multiplayer of type " + type);
			return;
		}
		int num = reader.Read7BitUInt(null);
		string target = connection.GetTarget();
		if (LogEnabled(2))
		{
			Log($"{this} received IncorrectChannel message with channel id to use {num} from player {target}", 2);
		}
		connection.OnDisconnected();
		if (num > THQNOAcceptor.lastChannelUsed)
		{
			THQNOAcceptor.lastChannelUsed = num;
		}
		if (THQNOAcceptor.lastChannelUsed < 0)
		{
			THQNOAcceptor.lastChannelUsed = 0;
		}
		THQNOAcceptor.lastChannelUsed++;
		connection = new THQNOConnection(Connection.ConnectionType.Client, target, THQNOAcceptor.lastChannelUsed, (int)THQNOAcceptor.ServerChannel);
		connection.multiplayer = this;
		if (logLevel == LogLevel.Bytes || logLevel == LogLevel.BytesAndMessages)
		{
			asyncSender = new AsyncSender(connection, sendLogWriter);
		}
		else
		{
			asyncSender = new AsyncSender(connection);
		}
		asyncSender.profiler = tx_profiler;
		if (logLevel == LogLevel.Bytes || logLevel == LogLevel.BytesAndMessages)
		{
			asyncReceiver = new AsyncReceiver(connection, receiveLogWriter, logLevel == LogLevel.Bytes);
		}
		else
		{
			asyncReceiver = new AsyncReceiver(connection);
		}
		asyncReceiver.profiler = rx_profiler;
		if (LogEnabled(2))
		{
			Log($"Starting another connection because of incorrect channel, from multiplayer {this} and will be listening on channel {THQNOAcceptor.lastChannelUsed}", 2);
		}
		connection.Connect();
	}

	private void OnAcknowledge(Serialization.IReader reader)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnAcknowledge() from multiplayer of type " + type);
		}
		else if (type == Type.Client)
		{
			if (NetworkType == NetworkTransportType.THQNO)
			{
				int num = reader.Read7BitSigned(null);
				if (num < 0 || num == THQNOAcceptor.ServerChannel)
				{
					Error($"{this} received invalid target channel {num} on Acknowledge message");
				}
				(connection as THQNOConnection)?.SetTargetChannel(num);
				if (LogEnabled(2))
				{
					Log($"{this} received Acknowledge message from the Server with channel to use {num}", 2);
				}
			}
			debug_headers = reader.ReadBool(null);
			connection.status = Connection.Status.Connected;
			connection.OnConnected();
			SetStatus(Status.Connected);
			NotifyListenersDelayed("connection_established", this, process_triggers: false, profile: false);
			QueueSS(delegate
			{
				if (LogEnabled(2))
				{
					Log($"{this}: sending ACKNOWLEDGE message", 2);
				}
				BeginMessage(MessageId.ACKNOWLEDGE);
				SendMessage(isObjectMessage: false);
				connection.CloseHandshakeChannel();
			});
			StartHeartbeat();
		}
		else
		{
			connection.status = Connection.Status.Connected;
			connection.OnConnected();
			SetStatus(Status.Connected);
			serverMultiplayer.NotifyListenersDelayed("connection_established", this, process_triggers: false, profile: false);
			if (connectionReason == ConnectionReason.Invalid)
			{
				Error($"OnAcknowledge() for multiplayer with invalid connection type: {this}");
			}
			if (connectionReason == ConnectionReason.InGame)
			{
				serverMultiplayer.AddJoiningPlayer(this);
			}
		}
	}

	private void OnHeartbeat(Serialization.IReader reader)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnHeartbeat() from multiplayer of type " + type);
		}
		else if (NetworkType != NetworkTransportType.THQNO)
		{
			Error($"Received heartbeat while using network layer type: {NetworkType}");
		}
		else if (HeartbeatReceiveTimeLimitInMilliseconds == 0)
		{
			Log($"Received heartbeat from {this} while this machine has heartbeat disabled - starting heartbeats!", 2);
			HeartbeatReceiveTimeLimitInMilliseconds = multiplayer_settings.heartbeat_timeout;
			StartHeartbeat();
		}
	}

	private void OnDisconnect(Serialization.IReader reader)
	{
		if (LogEnabled(2))
		{
			Log($"{this}: received DISCONNECT message", 2);
		}
		if (type == Type.Server)
		{
			Error("Attempting to call OnDisconnect() from multiplayer of type " + type);
			return;
		}
		if (type == Type.ServerClient)
		{
			serverMultiplayer?.HandlePlayerDisconnection(this);
		}
		Disconnect(gotDisconnectedByPeer: true, isUngraceful: false, destroy: true);
	}

	private void OnFullGameState(Serialization.IReader reader)
	{
		if (type != Type.Client)
		{
			Error("Attempting to call OnFullGameState() from multiplayer of type " + type);
		}
		else
		{
			game.OnFullGameStateReceived(isServer: false);
		}
	}

	private void OnLoadGame(Serialization.IReader reader)
	{
		if (type != Type.Client)
		{
			Error("Attempting to call OnLoadGame() from multiplayer of type " + type);
			return;
		}
		string mapName = reader.ReadStr("mapName");
		reader.ReadStr("period");
		Game.isLoadingSaveGameForClientOnly = true;
		if (string.IsNullOrEmpty(mapName))
		{
			Error("Invalid map name!");
			return;
		}
		MainThreadUpdates.Schedule(delegate
		{
			StartGame(new_game: false, mapName);
		});
	}

	public void StartHeartbeat(bool send_one = true)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnHeartbeat() from multiplayer of type " + type);
		}
		else if (NetworkType != NetworkTransportType.THQNO)
		{
			Error($"Attempting to StartHearbeat while network layer type {NetworkType}");
		}
		else
		{
			if (HeartbeatReceiveTimeLimitInMilliseconds <= 0)
			{
				return;
			}
			if (heartbeatRunning)
			{
				Warning($"Heartbeat already running for {this}");
				return;
			}
			lock (HeartbeatLock)
			{
				if (heartbeat_timer == null)
				{
					heartbeat_timer = Stopwatch.StartNew();
				}
			}
			ResetHertbeatReceivedTimer();
			heartbeatRunning = true;
			if (logLevel == LogLevel.Heartbeat || logLevel == LogLevel.HeartbeatAndMessages)
			{
				sendLogWriter.WriteLine($"{this}: Starting heartbeat");
			}
			if (send_one)
			{
				QueueSS(delegate
				{
					BeginMessage(MessageId.HEARTBEAT);
					SendMessage(isObjectMessage: false);
				});
			}
		}
	}

	public void StopHeartbeat()
	{
		if (logLevel == LogLevel.Heartbeat || logLevel == LogLevel.HeartbeatAndMessages)
		{
			receiveLogWriter.WriteLine($"{this}: Stopping heartbeat");
		}
		if (type == Type.Server)
		{
			Error("Attempting to call StopHeartbeat() from multiplayer of type " + type);
		}
		else if (NetworkType != NetworkTransportType.THQNO)
		{
			Error($"Attempting to StopHeartbeat while using network layer type {NetworkType}");
		}
		else
		{
			heartbeatRunning = false;
		}
	}

	public void ServerRemovePlayer(Multiplayer multiplayer, bool isUngraceful = false)
	{
		if (type != Type.Server)
		{
			Error("Attempting to call RemovePlayer() from multiplayer of type " + type);
			return;
		}
		bool num = CurrentPlayers.Remove(multiplayer.playerData, isUngraceful);
		AddLeavingPlayer(multiplayer);
		if (num)
		{
			game.SendState<Game.CurrentPlayersState>();
		}
	}

	public void DevCheatUngracefulDisconnect()
	{
		if (type == Type.Server)
		{
			for (int i = 0; i < allServerClients.Count; i++)
			{
				allServerClients[i]?.DevCheatUngracefulDisconnect();
			}
		}
		else if (connection == null)
		{
			Warning($"DevCheatUngracefulDisconnect() called with null connection for {this}");
		}
		else
		{
			(connection as THQNOConnection).devCheatStop = true;
		}
	}

	public void AddObj(Object obj)
	{
		if (type == Type.ServerClient)
		{
			Error("Attempting to add object " + obj.ToString() + " to multiplayer " + ToString() + ". Cannot add objects to multiplayer of type " + type);
			return;
		}
		Serialization.ObjectTypeInfo objectTypeInfo = Serialization.ObjectTypeInfo.Get(obj);
		if (objectTypeInfo == null)
		{
			return;
		}
		int num = obj.GetNid(generateNid: false);
		if (num != 0)
		{
			Object obj2 = objects.Get(objectTypeInfo.tid, num);
			if (obj2 != null)
			{
				if (obj2 != obj)
				{
					Error("Overwriting existing nid: " + obj2.ToString() + " <- " + obj.ToString());
				}
				return;
			}
		}
		objects.Add(objectTypeInfo.tid, obj);
		if (obj.GetFlag(16u))
		{
			if (IsOnline())
			{
				SendObj(obj);
			}
			else
			{
				obj.ClrFlag(16u);
			}
		}
	}

	public void SendObj(Object obj)
	{
		if (type == Type.ServerClient)
		{
			Error("Attempting to send object " + obj.ToString() + " from multiplayer " + ToString() + ". Cannot send objects from multiplayer of type " + type);
		}
		else if (!obj.IsAuthority())
		{
			if (type == Type.Server && isListening)
			{
				send_objects.Add(obj);
			}
		}
		else if (!obj.destroyed)
		{
			NID nid = obj;
			QueueSS(delegate
			{
				BeginMessage(MessageId.OBJ_EVENT);
				msg_writer.WriteMessageHeader(0, nid);
				SendMessage(isObjectMessage: true);
			});
			send_objects.Add(obj);
		}
	}

	public void FinishObj(Object obj)
	{
		if (game == null)
		{
			return;
		}
		if (type == Type.ServerClient)
		{
			Error("Attempting to finish object " + obj.ToString() + " from multiplayer " + ToString() + ". Cannot finish objects from multiplayer of type " + type);
		}
		else
		{
			if (obj.GetNid(generateNid: false) == 0 || game.state == Game.State.Quitting)
			{
				return;
			}
			if (Serialization.ObjectTypeInfo.Get(obj) == null)
			{
				Error("Attempting to finish object of unknown type: " + obj.ToString());
			}
			else if (obj.IsAuthority())
			{
				NID nid = obj;
				QueueSS(delegate
				{
					BeginMessage(MessageId.OBJ_EVENT);
					msg_writer.WriteMessageHeader(3, nid);
					SendMessage(isObjectMessage: true);
				});
			}
		}
	}

	public void DelObj(Object obj)
	{
		if (game == null)
		{
			return;
		}
		if (type == Type.ServerClient)
		{
			Error("Attempting to delete object " + obj.ToString() + " from multiplayer " + ToString() + ". Cannot delete objects from multiplayer of type " + type);
		}
		else
		{
			if (obj.GetNid(generateNid: false) == 0 || game.state == Game.State.Quitting)
			{
				return;
			}
			Serialization.ObjectTypeInfo objectTypeInfo = Serialization.ObjectTypeInfo.Get(obj);
			if (objectTypeInfo == null)
			{
				Error("Attempting to delete object of unknown type: " + obj.ToString());
				return;
			}
			if (obj.IsAuthority())
			{
				NID nid = obj;
				QueueSS(delegate
				{
					BeginMessage(MessageId.OBJ_EVENT);
					msg_writer.WriteMessageHeader(4, nid);
					SendMessage(isObjectMessage: true);
				});
			}
			objects.Del(objectTypeInfo.tid, obj);
		}
	}

	private void SendUniqueStrings()
	{
		if (sentUniqueStrings == null)
		{
			return;
		}
		int num = sentUniqueStrings.Count - last_sent_ustrs;
		if (num <= 0)
		{
			return;
		}
		MemStream memStream = new MemStream(ustr_tx_buf);
		memStream.WriteByte(2);
		memStream.Write7BitUInt(last_sent_ustrs);
		memStream.Write7BitUInt(num);
		while (last_sent_ustrs < sentUniqueStrings.Count)
		{
			string s = sentUniqueStrings.Get(last_sent_ustrs++);
			memStream.WriteString(s);
		}
		try
		{
			asyncSender.Send(ustr_tx_buf, memStream.Position);
		}
		catch (Exception ex)
		{
			Error("Send Error: " + ex.ToString());
		}
	}

	public void Send(byte[] data, int len)
	{
		if (type == Type.Server)
		{
			Error("Attempting to send data from multiplayer " + ToString() + ". Cannot send data from multiplayer of type " + type);
		}
		else
		{
			if (!SinglePlayerNetworkProfilerEnabled() && !IsOnline())
			{
				return;
			}
			if (len > 0 && len < AsyncReceiver.BufferSize)
			{
				if (status != Status.Connecting)
				{
					SendUniqueStrings();
				}
				try
				{
					asyncSender.Send(data, len);
					return;
				}
				catch (Exception ex)
				{
					Error("Send Error: " + ex.ToString());
					return;
				}
			}
			Error($"Attempting to send {len} bytes");
		}
	}

	public void EnqueueMessage(Serialization.ObjectMessage message)
	{
		lock (Lock)
		{
			message_queue.Add(message);
		}
	}

	public void SendAllObjects(List<Multiplayer> toPlayers)
	{
		if (type == Type.Client || type == Type.ServerClient)
		{
			Error("Attempting to call SendAllObjects() from multiplayer of type " + type);
			return;
		}
		lock (Lock)
		{
			if (toPlayers == null || toPlayers.Count == 0)
			{
				return;
			}
		}
		Game.BeginProfileSection("Generate all states");
		List<Serialization.ObjectStates> states = GenerateAllStates();
		Game.EndProfileSection("Generate all states");
		ClearSendObjects();
		if (LogEnabled(2))
		{
			Log($"{this} started sending full game state to all players", 2);
		}
		Game.BeginProfileSection("Send all states");
		SendStates(states, toPlayers, full_state: true);
		Game.EndProfileSection("Send all states");
		lock (Lock)
		{
			for (int i = 0; i < toPlayers.Count; i++)
			{
				toPlayers[i].game = game;
				toPlayers[i].receivedFullState = true;
				toPlayers[i].SendFullGameStateMessage();
			}
		}
	}

	public void BeginMessage(MessageId id)
	{
		if (type == Type.ServerClient && error_on_sc_begin_message)
		{
			Error("BeginMessage() called for ServerClient multiplayer");
		}
		if (sendLogWriter != null)
		{
			if (logLevel == LogLevel.Messages)
			{
				sendLogWriter.WriteLine("Sending " + id);
			}
			else if (logLevel == LogLevel.BytesAndMessages || logLevel == LogLevel.HeartbeatAndMessages)
			{
				sendLogWriter.WriteLine("Sending " + id.ToString() + " ");
			}
		}
		if (msg_writer != null)
		{
			Error("BeginMessage reentrant call");
			return;
		}
		msg_writer = new BinarySerializeWriter(sentUniqueStrings, new MemStream(msg_tx_buf), debug_headers);
		msg_writer.WriteByte((byte)id, null);
	}

	public void DiscardMessage()
	{
		if (msg_writer != null)
		{
			msg_writer.Close();
			msg_writer = null;
		}
	}

	public void SendMessage(List<Multiplayer> players)
	{
		if (msg_writer == null)
		{
			Error("SendMessage called with no message created");
			return;
		}
		for (int i = 0; i < players.Count; i++)
		{
			players[i].Send(msg_tx_buf, msg_writer.Position());
		}
		DiscardMessage();
	}

	public void SendMessage(bool isObjectMessage, Multiplayer skipPlayer = null, bool sendToJoiningPlayers = false, bool sendToJoiningAndJoinedPlayers = false)
	{
		if (skipPlayer != null && type == Type.Client)
		{
			Error("Attempting to send message to a particular player from multiplayer of type " + type);
		}
		else if (msg_writer == null)
		{
			Error("SendMessage called with no message created");
		}
		else if (type == Type.Server)
		{
			List<Multiplayer> list = ((!sendToJoiningPlayers) ? joinedPlayers : joiningPlayers);
			if (sendToJoiningAndJoinedPlayers)
			{
				list = new List<Multiplayer>();
				for (int i = 0; i < joiningPlayers.Count; i++)
				{
					list.Add(joiningPlayers[i]);
				}
				for (int j = 0; j < joinedPlayers.Count; j++)
				{
					list.Add(joinedPlayers[j]);
				}
			}
			for (int k = 0; k < list.Count; k++)
			{
				if (skipPlayer != list[k] && (!isObjectMessage || list[k].receivedFullState))
				{
					list[k].Send(msg_tx_buf, msg_writer.Position());
				}
			}
			DiscardMessage();
		}
		else
		{
			Send(msg_tx_buf, msg_writer.Position());
			DiscardMessage();
		}
	}

	public void SendState<T>(Object obj) where T : Serialization.ObjectState
	{
		if (!IsOnline() || obj.GetNid(generateNid: false) == 0)
		{
			return;
		}
		System.Type typeFromHandle = typeof(T);
		Serialization.ObjectTypeInfo objectTypeInfo = Serialization.ObjectTypeInfo.Get(obj);
		byte value;
		if (objectTypeInfo == null)
		{
			Error("Attempting to send " + typeFromHandle.ToString() + " for object of unknown type: " + obj.ToString());
		}
		else if (!objectTypeInfo.state_ids.TryGetValue(typeFromHandle, out value))
		{
			Error("Attempting to send unknown state " + typeFromHandle.ToString() + " for " + obj.ToString());
		}
		else
		{
			if (obj.GetFlag(16u))
			{
				return;
			}
			Serialization.ObjectState state = Serialization.CreateState(Serialization.GetStateAttr(value, objectTypeInfo));
			state.id = value;
			state.obj_ti = objectTypeInfo;
			state.nid = obj.GetNid();
			state.InitFrom(obj);
			SendObjects();
			QueueSS(delegate
			{
				BeginMessage(MessageId.OBJ_STATE);
				state.Write(msg_writer);
				SendMessage(isObjectMessage: true);
				if (state.substates != null)
				{
					for (int i = 0; i < state.substates.Count; i++)
					{
						Serialization.ObjectSubstate objectSubstate = state.substates[i];
						BeginMessage(MessageId.OBJ_SUBSTATE);
						objectSubstate.Write(msg_writer);
						SendMessage(isObjectMessage: true);
					}
				}
			});
		}
	}

	public void SendSubstate<T>(Object obj, int idx) where T : Serialization.ObjectSubstate
	{
		if (!IsOnline() || obj.GetNid(generateNid: false) == 0)
		{
			return;
		}
		System.Type typeFromHandle = typeof(T);
		System.Type declaringType = typeFromHandle.DeclaringType;
		Serialization.ObjectTypeInfo objectTypeInfo = Serialization.ObjectTypeInfo.Get(obj);
		if (objectTypeInfo == null)
		{
			Error("Attempting to send " + typeFromHandle.ToString() + " for object of unknown type: " + obj.ToString());
			return;
		}
		if (!objectTypeInfo.state_ids.TryGetValue(declaringType, out var value))
		{
			Error("Attempting to send unknown substate " + typeFromHandle.ToString() + " for " + obj.ToString());
			return;
		}
		if (!objectTypeInfo.states_by_id.TryGetValue(value, out var value2))
		{
			Error("Attempting to send unknown substate " + typeFromHandle.ToString() + " for " + obj.ToString());
			return;
		}
		if (!value2.substate_ids.TryGetValue(typeFromHandle, out var value3))
		{
			Error("Attempting to send unknown substate " + typeFromHandle.ToString() + " for " + obj.ToString());
			return;
		}
		if (!value2.substates_by_id.TryGetValue(value3, out var value4))
		{
			Error("Attempting to send unknown substate " + typeFromHandle.ToString() + " for " + obj.ToString());
			return;
		}
		Serialization.ObjectSubstate substate = Serialization.CreateSubstate(value4);
		substate.id = value;
		substate.nid = obj.GetNid();
		substate.obj_ti = objectTypeInfo;
		substate.substate_id = value3;
		substate.substate_index = idx;
		substate.InitFrom(obj);
		SendObjects();
		QueueSS(delegate
		{
			BeginMessage(MessageId.OBJ_SUBSTATE);
			substate.Write(msg_writer);
			SendMessage(isObjectMessage: true);
		});
	}

	public void SendObjEvent(Object obj, Serialization.ObjectEvent objEvent)
	{
		if (!IsOnline() || obj.GetNid(generateNid: false) == 0)
		{
			return;
		}
		System.Type type = objEvent.rtti.type;
		Serialization.ObjectTypeInfo objectTypeInfo = Serialization.ObjectTypeInfo.Get(obj);
		if (objectTypeInfo == null)
		{
			Error("Attempting to send " + type.ToString() + " for object of unknown type: " + obj.ToString());
			return;
		}
		if (!objectTypeInfo.event_ids.TryGetValue(type, out var value))
		{
			Error("Attempting to send unknown event " + type.ToString() + " for " + obj.ToString());
			return;
		}
		objEvent.id = value;
		objEvent.obj_ti = objectTypeInfo;
		objEvent.nid = obj.GetNid();
		QueueSS(delegate
		{
			BeginMessage(MessageId.OBJ_EVENT);
			objEvent.Write(msg_writer);
			SendMessage(isObjectMessage: true);
		});
	}

	public void SendObjEventToAllClients(Object obj, Serialization.ObjectEvent objEvent)
	{
		if (this.type != Type.Server)
		{
			Error("Attempting to send object event to all clients from multiplayer of type " + this.type);
		}
		else
		{
			if (!IsOnline() || obj.GetNid(generateNid: false) == 0)
			{
				return;
			}
			System.Type type = objEvent.rtti.type;
			Serialization.ObjectTypeInfo objectTypeInfo = Serialization.ObjectTypeInfo.Get(obj);
			if (objectTypeInfo == null)
			{
				Error("Attempting to send " + type.ToString() + " for object of unknown type: " + obj.ToString());
				return;
			}
			if (!objectTypeInfo.event_ids.TryGetValue(type, out var value))
			{
				Error("Attempting to send unknown event " + type.ToString() + " for " + obj.ToString());
				return;
			}
			objEvent.id = value;
			objEvent.obj_ti = objectTypeInfo;
			objEvent.nid = obj.GetNid();
			QueueSS(delegate
			{
				BeginMessage(MessageId.OBJ_EVENT);
				objEvent.Write(msg_writer);
				SendMessage(isObjectMessage: true);
			});
		}
	}

	public void SendObjEventToKingdom(Object obj, Serialization.ObjectEvent objEvent, int kingdomId)
	{
		if (type != Type.Server)
		{
			Error("Attempting to send object event to a kingdom from multiplayer of type " + type);
		}
		else
		{
			GetPlayerByKingdomId(kingdomId)?.SendObjEvent(obj, objEvent);
		}
	}

	public void SendObjEventToPlayer(Object obj, Serialization.ObjectEvent objEvent, int pid)
	{
		if (type == Type.Server)
		{
			GetPlayerByPid(pid)?.SendObjEvent(obj, objEvent);
		}
	}

	private void SetStatus(Status status)
	{
		if (LogEnabled(2))
		{
			Log($"{this}: new status: {status}", 2);
		}
		if (type == Type.Server && status == Status.Connecting)
		{
			Error($"Status cannot be set to {status} for multiplayer of type {type.ToString()}");
			return;
		}
		lock (StatusLock)
		{
			this.status = status;
			status_time = timer.ElapsedMilliseconds;
		}
	}

	public Status GetMultiplayerStatus()
	{
		return status;
	}

	private void OnObjState(Serialization.IReader reader)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnObjState() from multiplayer of type " + type);
			return;
		}
		Serialization.ObjectState objectState = Serialization.ObjectState.Read(reader);
		if (objectState != null)
		{
			if (type == Type.Client)
			{
				EnqueueMessage(objectState);
			}
			else if (type == Type.ServerClient && game?.multiplayer != null)
			{
				objectState.sender = this;
				game.multiplayer.EnqueueMessage(objectState);
			}
		}
	}

	private void OnObjEvent(Serialization.IReader reader)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnObjEvent() from multiplayer of type " + type);
			return;
		}
		Serialization.ObjectEvent objectEvent = Serialization.ObjectEvent.Read(reader);
		if (objectEvent != null)
		{
			if (type == Type.Client)
			{
				EnqueueMessage(objectEvent);
			}
			else if (type == Type.ServerClient && game?.multiplayer != null)
			{
				objectEvent.sender = this;
				game.multiplayer.EnqueueMessage(objectEvent);
			}
		}
	}

	private void OnObjSubstate(Serialization.IReader reader)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnObjSubstate() from multiplayer of type " + type);
			return;
		}
		Serialization.ObjectSubstate objectSubstate = Serialization.ObjectSubstate.Read(reader);
		if (objectSubstate != null)
		{
			if (type == Type.Client)
			{
				EnqueueMessage(objectSubstate);
			}
			else if (type == Type.ServerClient && game?.multiplayer != null)
			{
				objectSubstate.sender = this;
				game.multiplayer.EnqueueMessage(objectSubstate);
			}
		}
	}

	private void OnObjectState(Serialization.ObjectState state)
	{
		if (type == Type.ServerClient)
		{
			Error("Attempting to call OnObjectState() from multiplayer of type " + type);
			return;
		}
		Object obj = objects.Get(state.obj_ti.tid, state.nid);
		if (obj == null)
		{
			Error("Received " + state.ToString() + " for unknown object");
		}
		else if (obj.state_acc != null)
		{
			obj.state_acc.Set(state.id, state);
		}
		else
		{
			state.ApplyTo(obj);
		}
	}

	private void OnObjectSubstate(Serialization.ObjectSubstate substate)
	{
		if (type == Type.ServerClient)
		{
			Error("Attempting to call OnObjectSubstate() from multiplayer of type " + type);
			return;
		}
		Object obj = objects.Get(substate.obj_ti.tid, substate.nid);
		if (obj == null)
		{
			Error("Received " + substate.ToString() + " for unknown object");
		}
		else if (obj.state_acc != null)
		{
			obj.state_acc.Add(substate);
		}
		else
		{
			substate.ApplyTo(obj);
		}
	}

	private void OnObjectEvent(Serialization.ObjectEvent evt)
	{
		if (type == Type.ServerClient)
		{
			Error("Attempting to call OnObjectEvent() from multiplayer of type " + type);
			return;
		}
		Object obj = objects.Get(evt.obj_ti.tid, evt.nid);
		if (evt.id == 0)
		{
			if (obj == null)
			{
				obj = evt.obj_ti.CreateObject(this);
				if (obj == null)
				{
					return;
				}
				obj.SetNid(evt.nid, update_registry: false);
				objects.Add(evt.obj_ti.tid, obj);
			}
			if (logLevel == LogLevel.Custom)
			{
				receiveLogWriter?.WriteLine("BEGIN OBJ: " + obj);
			}
			if (logLevel == LogLevel.Objects)
			{
				receiveLogWriter?.WriteLine("BEGIN " + obj);
			}
			if (obj.state_acc == null)
			{
				obj.state_acc = new Serialization.ObjectStates(obj);
			}
			else
			{
				Error("Received EVENT_BEGIN_OBJ for already accumulating object " + obj.ToString());
			}
		}
		else if (obj == null)
		{
			Error("Received " + evt.ToString() + " for unknown object");
		}
		else if (evt.id == 1)
		{
			if (obj.state_acc == null)
			{
				Error("Received EVENT_END_OBJ for non-accumulating object " + obj.ToString());
				if (logLevel == LogLevel.Custom)
				{
					receiveLogWriter?.WriteLine("Received EVENT_END_OBJ for non-accumulating object  " + obj);
				}
				return;
			}
			obj.state_acc.LoadObject(obj);
			obj.state_acc = null;
			if (logLevel == LogLevel.Custom)
			{
				receiveLogWriter?.WriteLine("END OBJ: " + obj);
			}
			if (logLevel == LogLevel.Objects)
			{
				receiveLogWriter?.WriteLine("END " + obj);
			}
		}
		else if (evt.id == 2)
		{
			if (obj.state_acc != null)
			{
				obj.state_acc.LoadObject(obj);
				obj.state_acc = null;
			}
			if (type == Type.Client && !obj.started)
			{
				obj.Start();
			}
			if (logLevel == LogLevel.Custom)
			{
				receiveLogWriter?.WriteLine("START OBJ: " + obj);
			}
			if (logLevel == LogLevel.Objects)
			{
				receiveLogWriter?.WriteLine("START " + obj);
			}
		}
		else if (evt.id == 3)
		{
			obj.Finish();
		}
		else if (evt.id == 4)
		{
			objects.Del(evt.obj_ti.tid, obj);
			obj.Destroy(forceDestroyInstantly: true);
		}
		else
		{
			evt.ApplyTo(obj);
		}
	}

	private void OnObjectMessage(Serialization.ObjectMessage msg)
	{
		if (msg == null)
		{
			return;
		}
		if (type == Type.ServerClient)
		{
			Error("Attempting to call OnObjectMessage() from multiplayer of type " + type);
		}
		else if (msg is Serialization.ObjectState objectState)
		{
			if (type != Type.Client)
			{
				if (objectState.sender == null)
				{
					Error("Invalid state sender");
				}
				else
				{
					Error($"Sender {objectState.sender.ToString()} with pid {objectState.sender.playerData.pid} is not authority for {objectState.ToString()}");
				}
			}
			else
			{
				OnObjectState(objectState);
			}
		}
		else if (msg is Serialization.ObjectEvent objectEvent)
		{
			if (type == Type.Client)
			{
				OnObjectEvent(objectEvent);
			}
			else if (objectEvent.id >= 16)
			{
				Object obj = game.multiplayer.objects.Get(objectEvent.obj_ti.tid, objectEvent.nid);
				if (obj == null)
				{
					Error("Received object event " + objectEvent.ToString() + " for unknown object!");
				}
				else if (!CheckSenderAuthority(objectEvent))
				{
					Error($"Sender {objectEvent.sender.ToString()} with pid {objectEvent.sender.playerData.pid} is not authority for {objectEvent.ToString()}");
				}
				else if (obj.IsAuthority())
				{
					OnObjectEvent(objectEvent);
				}
				else
				{
					Error("No authority found for obj: " + obj.ToString());
				}
			}
			else
			{
				OnObjectEvent(objectEvent);
			}
		}
		else if (msg is Serialization.ObjectSubstate objectSubstate)
		{
			if (type != Type.Client)
			{
				if (objectSubstate.sender == null)
				{
					Error("Invalid substate sender");
				}
				else
				{
					Error($"Sender {objectSubstate.sender.ToString()} with pid {objectSubstate.sender.playerData.pid} is not authority for {objectSubstate.ToString()}");
				}
			}
			else
			{
				OnObjectSubstate(objectSubstate);
			}
		}
		else
		{
			Error("Received object message which is not state, substate or event: " + msg.ToString());
		}
	}

	public void SendData(string campaign_id, int data_type, string player_id, string data, int data_id)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call SendData() from multiplayer of type " + type);
			return;
		}
		QueueSS(delegate
		{
			BeginMessage(MessageId.SEND_DATA);
			msg_writer.WriteStr(campaign_id, "campaign_id");
			msg_writer.Write7BitUInt(data_type, "data_type");
			msg_writer.WriteStr(player_id, "player_id");
			msg_writer.WriteRawStr(data, "data");
			msg_writer.Write7BitUInt(data_id, "data_id");
			SendMessage(isObjectMessage: false);
		});
	}

	private void OnSendData(Serialization.IReader reader)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnSendData() from multiplayer of type " + type);
			return;
		}
		string text = reader.ReadStr("campaign_id");
		int num = reader.Read7BitUInt("data_type");
		string text2 = reader.ReadStr("player_id");
		string text3 = reader.ReadRawStr("data");
		int num2 = reader.Read7BitUInt("data_id");
		Value[] param = new Value[6]
		{
			text,
			connection.GetTarget(),
			num,
			text2,
			text3,
			num2
		};
		if (string.IsNullOrEmpty(playerData.id))
		{
			Error("Invalid playerData.id in OnSendData()");
		}
		NotifyListenersDelayed("data_changed", param, process_triggers: false, profile: false);
	}

	public void SendJoinCampaignRequest(string campaign_id, JoinReason joinReason)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call SendJoinRequest() from multiplayer of type " + type);
			return;
		}
		if (LogEnabled(2))
		{
			Log($"{this}: Sending Join request for campaign {campaign_id}", 2);
		}
		QueueSS(delegate
		{
			BeginMessage(MessageId.JOIN_CAMPAIGN_REQUEST);
			msg_writer.WriteStr(campaign_id, "campaign_id");
			msg_writer.WriteByte((byte)joinReason, "join_reason");
			SendMessage(isObjectMessage: false);
		});
	}

	private void OnJoinRequest(Serialization.IReader reader)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnJoinRequest() from multiplayer of type " + type);
			return;
		}
		string text = reader.ReadStr("campaign_id");
		byte b = reader.ReadByte("join_reason");
		if (LogEnabled(2))
		{
			Log("Received Join request for campaign " + text, 2);
		}
		Value[] param = new Value[3]
		{
			text,
			connection.GetTarget(),
			b
		};
		if (string.IsNullOrEmpty(playerData.id))
		{
			Error("Invalid playerData.id in OnJoinRequest()");
		}
		NotifyListenersDelayed("join_request", param, process_triggers: false, profile: false);
	}

	public void SendJoinCampaignRequestFailResponse(string response)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call SendJoinCampaignRequestFailResponse() from multiplayer of type " + type);
			return;
		}
		if (LogEnabled(2))
		{
			Log($"{this}: Sending Join fail response {response}", 2);
		}
		QueueSS(delegate
		{
			BeginMessage(MessageId.JOIN_CAMPAIGN_FAIL_RESPONSE);
			msg_writer.WriteStr(response, "response");
			SendMessage(isObjectMessage: false);
		});
	}

	public void SendInteruptCountdown(string campaign_id, string by_player_id)
	{
		if (type == Type.Client)
		{
			Error("Attempting to call SendInteruptCountdown() from multiplayer of type " + type);
			return;
		}
		if (LogEnabled(2))
		{
			Log($"{this}: Sending interupt of cooldown response {by_player_id}", 2);
		}
		QueueSS(delegate
		{
			BeginMessage(MessageId.INTERUPT_COUNTDOWN);
			msg_writer.WriteStr(campaign_id, "campaign_id");
			msg_writer.WriteStr(by_player_id, "by_player_id");
			SendMessage(isObjectMessage: false);
		});
	}

	private void OnJoinRequestFailResponse(Serialization.IReader reader)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnJoinRequest() from multiplayer of type " + type);
			return;
		}
		string text = reader.ReadStr("response");
		if (LogEnabled(2))
		{
			Log("Received Join request fail reponse " + text, 2);
		}
		Value[] param = new Value[2]
		{
			text,
			default(Value)
		};
		if (string.IsNullOrEmpty(playerData.id))
		{
			Error("Invalid playerData.id in OnJoinRequestFailResponse()");
		}
		NotifyListenersDelayed("join_request_fail_response", param, process_triggers: false, profile: false);
	}

	private void OnInteruptCountdown(Serialization.IReader reader)
	{
		string text = reader.ReadStr("campaign_id");
		string text2 = reader.ReadStr("by_player_id");
		if (LogEnabled(2))
		{
			Log("Received interupt countdown by player " + text2, 2);
		}
		Value[] param = new Value[2] { text, text2 };
		if (string.IsNullOrEmpty(playerData.id))
		{
			Error("Invalid playerData.id in OnInteruptCountdown()");
		}
		NotifyListenersDelayed("interupt_countdown", param, process_triggers: false, profile: false);
	}

	public void SendChatMessage(string senderPlayerId, string message, Chat.Channel channel, string whipserTargetId, string campaignId)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call SendChatMessage() from multiplayer of type " + type);
			return;
		}
		if (playerData == null)
		{
			Error($"Player data is null in multiplayer {this}");
			return;
		}
		if (LogEnabled(2))
		{
			Log($"{this} is sending {channel} chat message", 2);
		}
		QueueSS(delegate
		{
			BeginMessage(MessageId.CHAT_MESSAGE);
			Chat.WriteChatMessage(msg_writer, senderPlayerId, message, channel, whipserTargetId, campaignId);
			SendMessage(isObjectMessage: false);
		});
	}

	public void BroadcastChatMessage(List<Multiplayer> recipients, string senderPlayerId, string message, Chat.Channel channel, bool includeServer, string whipserTargetId, bool isLobbyChat, string campaignId)
	{
		if (type != Type.Server)
		{
			Error("Attempting to call BroadcastChatMessage() from multiplayer of type " + type);
			return;
		}
		if (includeServer)
		{
			chat.ReceivedChatMessage(game.campaign, senderPlayerId, message, channel, whipserTargetId);
		}
		if (recipients != null && recipients.Count != 0)
		{
			QueueSS(delegate
			{
				BeginMessage(MessageId.CHAT_MESSAGE);
				Chat.WriteChatMessage(msg_writer, senderPlayerId, message, channel, whipserTargetId, campaignId);
				SendMessage(recipients);
			});
		}
	}

	private void OnChatMessage(Serialization.IReader reader)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnChatMessage() from multiplayer of type " + type);
			return;
		}
		Chat.ReadChatMessage(reader, out var senderPlayerId, out var message, out var channel, out var whisperTargetId, out var campaignId);
		if (channel == Chat.Channel.Lobby)
		{
			Vars messageVars = Chat.GetMessageVars(campaignId, senderPlayerId, message, channel, whisperTargetId);
			NotifyListenersDelayed("lobby_chat_received", messageVars, process_triggers: false, profile: true);
		}
		else if (type == Type.Client)
		{
			if (channel != Chat.Channel.Lobby || (!string.IsNullOrEmpty(campaignId) && !(campaignId != game?.campaign?.id)))
			{
				if (LogEnabled(2))
				{
					Log($"{this} received a chat message from player with id {senderPlayerId}", 2);
				}
				chat.ReceivedChatMessage(game.campaign, senderPlayerId, message, channel, whisperTargetId);
			}
		}
		else
		{
			if (LogEnabled(2))
			{
				Log($"{this} received a chat message from player with id {senderPlayerId}", 2);
			}
			serverMultiplayer.chatServer.RouteMessage(senderPlayerId, message, channel, whisperTargetId, campaignId);
		}
	}

	public void SendBeginGameStateDump(byte[] checksum)
	{
		if (type == Type.Server)
		{
			for (int i = 0; i < joinedPlayers.Count; i++)
			{
				joinedPlayers[i].SendBeginGameStateDump(checksum);
			}
		}
		else if (type == Type.ServerClient)
		{
			BeginMessage(MessageId.BEGIN_GAME_STATE_DUMP);
			msg_writer.WriteBytes(checksum, "checksum");
			SendMessage(isObjectMessage: false);
		}
		else
		{
			BeginMessage(MessageId.BEGIN_GAME_STATE_DUMP);
			SendMessage(isObjectMessage: false);
		}
	}

	private void OnBeginGameStateDump(Serialization.IReader reader)
	{
		if (type == Type.Server)
		{
			Error("Attempting to call OnBeginGameStateDump() from multiplayer of type " + type);
		}
		else if (type == Type.Client)
		{
			byte[] serversChecksum = reader.ReadBytes("checksum");
			if (game == null)
			{
				Error("Game is null");
			}
			else
			{
				game.DumpGameStateClient(serversChecksum);
			}
		}
		else
		{
			playerData.gameState = string.Empty;
		}
	}

	public void SendGameStateDump()
	{
		if (type != Type.Client)
		{
			Error("Attempting to call SendGameStateDump() from multiplayer of type " + type);
			return;
		}
		SendBeginGameStateDump(null);
		string gameState = playerData.gameState;
		List<string> list = SplitMessageToChunks(gameState);
		for (int i = 0; i < list.Count; i++)
		{
			BeginMessage(MessageId.GAME_STATE_DUMP);
			msg_writer.WriteRawStr(list[i], "game_state_dump");
			SendMessage(isObjectMessage: false);
		}
		SendEndGameStateDump();
	}

	public void OnGameStateDump(Serialization.IReader reader)
	{
		if (type != Type.ServerClient)
		{
			Error("Attempting to call OnGameStateDump() from multiplayer of type " + type);
			return;
		}
		string text = reader.ReadRawStr("game_state_dump");
		playerData.gameState += text;
	}

	public void SendEndGameStateDump()
	{
		if (type != Type.Client)
		{
			Error("Attempting to call SendEndGameStateDump() from multiplayer of type " + type);
			return;
		}
		BeginMessage(MessageId.END_GAME_STATE_DUMP);
		SendMessage(isObjectMessage: false);
	}

	private void OnEndGameStateDump(Serialization.IReader reader)
	{
		if (type != Type.ServerClient)
		{
			Error("Attempting to call OnEndGameStateDump() from multiplayer of type " + type);
			return;
		}
		MainThreadUpdates.Perform(delegate
		{
			if (LogEnabled(2))
			{
				Log($"Game state dump received from player {this}", 2);
			}
			game.DumpGameStateToFile(playerData, Game.GameStateDumpFileType.Client);
		});
	}

	public void RequestPoliticalData(string campaign_id)
	{
		Log("Sending REQUEST_POLITICAL_DATA for campaign " + campaign_id, 2);
		QueueSS(delegate
		{
			BeginMessage(MessageId.REQUEST_POLITICAL_DATA);
			msg_writer.WriteRawStr(campaign_id, "campaign_id");
			SendMessage(isObjectMessage: false);
		});
	}

	private void OnRequestPoliticalData(Serialization.IReader reader)
	{
		string text = reader.ReadRawStr("campaign_id");
		Log("Received REQUEST_POLITICAL_DATA for campaign " + text, 2);
		NotifyListenersDelayed("on_request_political_data", text, process_triggers: false, profile: false);
	}

	public void SendPoliticalData(string campaign_id, PoliticalData pd)
	{
		Log($"Sending POLITICAL_DATA for campaign {campaign_id}: {pd}", 2);
		QueueSS(delegate
		{
			BeginMessage(MessageId.POLITICAL_DATA);
			msg_writer.WriteRawStr(campaign_id, "campaign_id");
			pd.WriteBody(msg_writer);
			SendMessage(isObjectMessage: false);
		});
	}

	private void OnPoliticalData(Serialization.IReader reader)
	{
		string text = reader.ReadRawStr("campaign_id");
		PoliticalData politicalData = new PoliticalData();
		politicalData.ReadBody(reader);
		Log($"Received POLITICAL_DATA for campaign {text}: {politicalData}", 2);
		NotifyListenersDelayed("on_political_data", new object[2] { text, politicalData }, process_triggers: false, profile: false);
	}

	private List<string> SplitMessageToChunks(string input)
	{
		if (Encoding.Unicode.GetByteCount(input) <= AsyncReceiver.BufferSize)
		{
			return new List<string> { input };
		}
		List<string> result = new List<string>();
		SplitMessageToChunks(input, result);
		return result;
	}

	private void SplitMessageToChunks(string input, List<string> result)
	{
		int num = input.Length / 2;
		string text = input.Substring(0, num);
		if (Encoding.Unicode.GetByteCount(text) > AsyncReceiver.BufferSize)
		{
			SplitMessageToChunks(text, result);
		}
		else
		{
			result.Add(text);
		}
		string text2 = input.Substring(num);
		if (Encoding.Unicode.GetByteCount(text2) > AsyncReceiver.BufferSize)
		{
			SplitMessageToChunks(text2, result);
		}
		else
		{
			result.Add(text2);
		}
	}

	private void AddPlayersToGame(List<Multiplayer> multiplayers)
	{
		if (type != Type.Server)
		{
			Error("Attempting to call AddPlayersToGame() from multiplayer of type " + type);
		}
		else if (multiplayers != null && game != null && game.campaign != null)
		{
			for (int i = 0; i < multiplayers.Count; i++)
			{
				AddPlayerToGame(multiplayers[i], game.campaign);
			}
		}
	}

	private void AddPlayerToGame(Multiplayer multiplayer, Campaign campaign)
	{
		if (type != Type.Server)
		{
			Error("Attempting to call AddPlayerToGame() from multiplayer of type " + type);
			return;
		}
		if (campaign == null)
		{
			Error("AddPlayerToGame() called with null campaign");
			return;
		}
		if (multiplayer == null)
		{
			Error("AddPlayerToGame() called with null multiplayer");
			return;
		}
		if (multiplayer.playerData == null)
		{
			Error("AddPlayerToGame() called with multiplayer with no player data: " + multiplayer.ToString());
			return;
		}
		int playerIndex = campaign.GetPlayerIndex(multiplayer.playerData.id);
		if (playerIndex < 0)
		{
			Error($"AddPlayerToGame cannot extract player index for player {multiplayer}. Player data id: {multiplayer.playerData.id}");
			return;
		}
		string playerID = campaign.GetPlayerID(playerIndex);
		if (string.IsNullOrEmpty(playerID))
		{
			Error($"AddPlayerToGame cannot extract player id for player {multiplayer}");
			return;
		}
		PlayerData playerData = new PlayerData
		{
			id = playerID,
			name = PlayerInfo.GetPlayerName(playerID),
			kingdomName = campaign.GetKingdomName(playerIndex),
			team = CampaignUtils.GetTeam(campaign, playerIndex)
		};
		multiplayer.playerData = playerData;
		multiplayer.playerData.pid = playerIndex + 1;
		multiplayer.playerData.owner = multiplayer;
		if (game.multiplayer.initial_waiting_finished)
		{
			Vars vars = new Vars();
			vars.Set("player_id", multiplayer.playerData.id);
			if (multiplayer.playerData.kingdomName != null)
			{
				vars.Set("kingdom", multiplayer.playerData.kingdomName);
			}
			if (!string.IsNullOrEmpty(multiplayer.playerData.name))
			{
				vars.Set("target", "#" + multiplayer.playerData.name);
			}
			vars.Set("pid", multiplayer.playerData.pid);
			game.multiplayer.reconnectingPlayersVars.Add(vars);
		}
		CurrentPlayers.Add(multiplayer.playerData);
	}

	private void UpdateConnection()
	{
		if (type != Type.Client)
		{
			Error("Attempting to call UpdateConnection() from multiplayer of type " + type);
		}
		else if (connection != null && !IsConnected() && status == Status.Connecting && timer.ElapsedMilliseconds - status_time > multiplayer_settings.connect_to_player_timeout)
		{
			if (LogEnabled(2))
			{
				Log($"{this}: Connecting timed out", 2);
			}
			ShutDown();
		}
	}

	private void UpdateJoiningPlayers()
	{
		if (type != Type.Server)
		{
			Error("Attempting to call UpdateJoiningPlayers() from multiplayer of type " + type);
		}
		else if (connectionReason != ConnectionReason.InGame)
		{
			Error("Attempting to call UpdateJoiningPlayers() from multiplayer with connection reason " + connectionReason);
		}
		else
		{
			if (game?.kingdoms == null)
			{
				return;
			}
			List<Multiplayer> list = new List<Multiplayer>();
			lock (Lock)
			{
				if (joiningPlayers == null || joiningPlayers.Count == 0)
				{
					return;
				}
				if (!singleplayerNetworkProfilerEnabled)
				{
					for (int i = 0; i < joiningPlayers.Count; i++)
					{
						if (joiningPlayers[i].playerData.id == playerData.id)
						{
							joiningPlayers.RemoveAt(i);
							i--;
							continue;
						}
						if (game.GetKingdom(joiningPlayers[i].playerData.kingdomId) == null)
						{
							string kingdomName = game.campaign.GetKingdomName(joiningPlayers[i].playerData.id);
							if (game.GetKingdom(kingdomName) == null)
							{
								Error($"No kingdom set in playerData or in campaign for player {joiningPlayers[i]}");
								continue;
							}
						}
						list.Add(joiningPlayers[i]);
						joiningPlayers.RemoveAt(i);
						i--;
					}
				}
			}
			if (list.Count != 0)
			{
				AddPlayersToGame(list);
				game.teams.Evaluate();
				if (game.state == Game.State.Running)
				{
					game.SendState<Game.CurrentPlayersState>();
				}
				SendAllObjects(list);
				lock (Lock)
				{
					joinedPlayers.AddRange(list);
				}
				game.ValidatePlayersAndKingdoms();
			}
		}
	}

	private void UpdateMessageQueue()
	{
		if (type == Type.ServerClient)
		{
			Error("Attempting to call UpdateMessageQueue() from multiplayer of type " + type);
			return;
		}
		lock (Lock)
		{
			if (message_queue.Count <= 0)
			{
				return;
			}
		}
		while (game == null || game.state != Game.State.LoadingMap)
		{
			Serialization.ObjectMessage objectMessage;
			lock (Lock)
			{
				if (message_queue.Count <= 0)
				{
					break;
				}
				objectMessage = message_queue[0];
				message_queue.RemoveAt(0);
			}
			try
			{
				if (logLevel == LogLevel.Custom)
				{
					receiveLogWriter?.WriteLine("UpdateMessageQueue() called");
				}
				OnObjectMessage(objectMessage);
			}
			catch (Exception ex)
			{
				Error("Error handling message " + objectMessage.ToString() + ": " + ex.ToString());
			}
		}
	}

	private void UpdateLeavingPlayers()
	{
		if (type == Type.ServerClient)
		{
			Error("Attempting to call UpdateLeavingPlayers() from multiplayer of type " + type);
		}
		else if (connectionReason != ConnectionReason.InGame)
		{
			Error("Attempting to call UpdateLeavingPlayers() from multiplayer with connection reason " + connectionReason);
		}
		else if (type == Type.Server && leavingPlayers != null && leavingPlayers.Count != 0)
		{
			int num;
			for (num = 0; num < leavingPlayers.Count; num++)
			{
				joiningPlayers.Remove(leavingPlayers[num]);
				joinedPlayers.Remove(leavingPlayers[num]);
				leavingPlayers[num].ShutDown();
				leavingPlayers.RemoveAt(num);
				num--;
			}
		}
	}

	private static long MillisecondsSinceLastHeartbeat(string player_id)
	{
		if (string.IsNullOrEmpty(player_id))
		{
			return long.MaxValue;
		}
		lock (HeartbeatLock)
		{
			if (heartbeat_timer == null)
			{
				return long.MaxValue;
			}
			long elapsedMilliseconds = heartbeat_timer.ElapsedMilliseconds;
			if (!last_heartbeat_received.TryGetValue(player_id, out var value))
			{
				return long.MaxValue;
			}
			return elapsedMilliseconds - value;
		}
	}

	private long MillisecondsSinceLastHeartbeat()
	{
		return MillisecondsSinceLastHeartbeat(connection?.GetTarget());
	}

	private static void ResetHertbeatReceivedTimer(string player_id)
	{
		if (string.IsNullOrEmpty(player_id))
		{
			return;
		}
		lock (HeartbeatLock)
		{
			if (heartbeat_timer != null)
			{
				long elapsedMilliseconds = heartbeat_timer.ElapsedMilliseconds;
				last_heartbeat_received[player_id] = elapsedMilliseconds;
			}
		}
	}

	private void ResetHertbeatReceivedTimer()
	{
		ResetHertbeatReceivedTimer(connection?.GetTarget());
	}

	public void UpdateHeartbeat()
	{
		if (!heartbeatRunning)
		{
			if (HeartbeatReceiveTimeLimitInMilliseconds <= 0 || !IsConnected())
			{
				return;
			}
			StartHeartbeat();
		}
		if (type == Type.Server)
		{
			Error("Attempting to call UpdateHeartbeat() from multiplayer of type " + type);
			return;
		}
		if (NetworkType != NetworkTransportType.THQNO)
		{
			Error("Attempting to call UpdateHeartbeat() using network transport layer type " + NetworkType);
			return;
		}
		if (HeartbeatReceiveTimeLimitInMilliseconds <= 0)
		{
			StopHeartbeat();
			return;
		}
		long num = MillisecondsSinceLastHeartbeat();
		if (num >= HeartbeatReceiveTimeLimitInMilliseconds)
		{
			if (LogEnabled(2))
			{
				Log($"{this} is ungraceful disconnecting. Elapsed {num}ms.", 2);
			}
			if (type == Type.ServerClient)
			{
				serverMultiplayer?.CheckAndReSignInToTHQNO();
			}
			else if (type == Type.Client)
			{
				CheckAndReSignInToTHQNO();
			}
			StopHeartbeat();
			HandleUngracefulDisconnect();
		}
	}

	private void UpdateWaitingForPlayers()
	{
		if (connectionReason != ConnectionReason.InGame)
		{
			Error("Attempting to call UpdateWaitingForPlayers() from multiplayer with connection reason " + connectionReason);
		}
		else if (!game.multiplayer.initial_waiting_finished && CampaignUtils.AreAllJoinedPlayersInGame(game))
		{
			game.multiplayer.initial_waiting_finished = true;
			game.pause.DelRequest("WaitingForPlayersPause");
		}
	}

	private void UpdateReconnectingPlayers()
	{
		if (connectionReason != ConnectionReason.InGame)
		{
			Error("Attempting to call UpdateReconnectingPlayers() from multiplayer with connection reason " + connectionReason);
		}
		else
		{
			if (reconnectingPlayersVars == null || reconnectingPlayersVars.Count == 0)
			{
				return;
			}
			for (int num = reconnectingPlayersVars.Count - 1; num >= 0; num--)
			{
				Vars vars = reconnectingPlayersVars[num];
				if (vars != null)
				{
					string player_id = vars.Get("player_id");
					if (CampaignUtils.IsPlayerLoaded(game, player_id))
					{
						game.FireEvent("player_connected", vars);
						NotifyListenersDelayed("players_changed", null, process_triggers: false, profile: false);
						reconnectingPlayersVars.RemoveAt(num);
					}
				}
			}
		}
	}

	private void CheckAndReSignInToTHQNO()
	{
		if (type == Type.ServerClient)
		{
			Error("Attempting to call CheckTHQNOConnectionAndReconnect() from multiplayer of type " + type);
		}
		else
		{
			Coroutine.Start("ReSignInToTHQNO", ReSignInToTHQNO());
		}
		IEnumerator ReSignInToTHQNO()
		{
			Coroutine.OnFinish(delegate(Value result, string err)
			{
				if (!string.IsNullOrEmpty(err))
				{
					game?.multiplayer?.NotifyListeners("reconnect_fail", err);
				}
			});
			if (!THQNORequest.signed_in)
			{
				if (THQNORequest.platformType != Common.PlatformType.THQNO && THQNORequest.platformType != Common.PlatformType.NoDRM)
				{
					while (!THQNORequest.signed_in)
					{
						yield return THQNORequest.SignInPlatformCoro();
						if (!THQNORequest.signed_in || !THQNORequest.networkingAvailable)
						{
							Error($"SignInPlatformCoro Fail. signed_in: Result: {THQNORequest.signed_in}, networkingAvailable: {THQNORequest.networkingAvailable}. Attempting again.");
						}
					}
				}
				else
				{
					string credentialsFilePath = THQNORequest.Utility.GetCredentialsFilePath();
					if (!File.Exists(credentialsFilePath))
					{
						string text = "ReSignInToTHQNO failed. Credentials file: \"" + credentialsFilePath + "\" does not exist.";
						Error(text);
						yield return Coroutine.Return(Value.Null, text);
					}
					while (!THQNORequest.signed_in)
					{
						yield return THQNORequest.SignInWithCredentialsFile();
						SignInResultCD signInResultCD = ((Coroutine.Result.obj_val == null) ? default(SignInResultCD) : ((SignInResultCD)Coroutine.Result.obj_val));
						if ((signInResultCD.result != Common.APIResult.Success || !signInResultCD.networkingAvaliable) && signInResultCD.result != Common.APIResult.SignIn_AlreadySignedIn)
						{
							Warning($"SignIn fail. Result: {signInResultCD.result}. Attempting again.");
						}
						else if (LogEnabled(2))
						{
							Log("THQNO ReSignedIn successfully.", 2);
						}
						yield return new Coroutine.WaitForMillis(1000f);
					}
				}
				if (LogEnabled(2))
				{
					Log("CheckAndReSignInToTHQNO() completed.", 2);
				}
			}
		}
	}

	public void HandleUngracefulDisconnect()
	{
		if (logLevel == LogLevel.Heartbeat || logLevel == LogLevel.HeartbeatAndMessages)
		{
			long num = MillisecondsSinceLastHeartbeat();
			sendLogWriter.WriteLine($"{this}: Ungraceful disconnecting. Elapsed {num}ms");
		}
		game?.OnQuitGameAnalytics("ungraceful_disconnect");
		Disconnect(gotDisconnectedByPeer: true, isUngraceful: true);
		StopHeartbeat();
	}

	private void UpdateHeartbeats()
	{
		if (NetworkType != NetworkTransportType.THQNO)
		{
			Error("Attempting to call UpdateHeartbeats() using network transport layer type " + NetworkType);
		}
		else if (type == Type.Server)
		{
			if (allServerClients == null)
			{
				Error($"Attempting to call UpdateHeartbeats() but allServerClients is null for {this}");
				return;
			}
			for (int num = allServerClients.Count - 1; num >= 0; num--)
			{
				if (allServerClients[num].status >= Status.Disconnecting)
				{
					allServerClients.RemoveAt(num);
				}
				else
				{
					allServerClients[num].UpdateHeartbeat();
				}
			}
		}
		else if (type == Type.ServerClient && !IsConnected())
		{
			serverMultiplayer?.allServerClients?.Remove(this);
		}
		else
		{
			UpdateHeartbeat();
		}
	}

	public void ClearSendObjects()
	{
		for (int i = 0; i < send_objects.Count; i++)
		{
			send_objects[i].ClrFlag(16u);
		}
		send_objects.Clear();
	}

	public List<Serialization.ObjectStates> GenerateStates()
	{
		if (send_objects == null)
		{
			return null;
		}
		List<Serialization.ObjectStates> list = new List<Serialization.ObjectStates>(send_objects.Count);
		for (int i = 0; i < send_objects.Count; i++)
		{
			Object obj = send_objects[i];
			if (!obj.destroyed)
			{
				Serialization.ObjectStates objectStates = new Serialization.ObjectStates(obj);
				try
				{
					objectStates.GenerateStates(obj);
				}
				catch (Exception ex)
				{
					Error("GenerateStates() exception: " + ex.ToString());
				}
				list.Add(objectStates);
			}
		}
		return list;
	}

	public List<Serialization.ObjectStates> GenerateAllStates()
	{
		List<Serialization.ObjectStates> list = new List<Serialization.ObjectStates>(send_objects.Count);
		for (Serialization.ObjectType objectType = Serialization.ObjectType.Game; objectType < Serialization.ObjectType.COUNT; objectType++)
		{
			foreach (KeyValuePair<int, Object> @object in objects.Registry(objectType).objects)
			{
				Object value = @object.Value;
				if (!value.destroyed)
				{
					Serialization.ObjectStates objectStates = new Serialization.ObjectStates(value);
					try
					{
						objectStates.GenerateStates(value);
					}
					catch (Exception ex)
					{
						Error("GenerateStates() exception: " + ex.ToString());
					}
					list.Add(objectStates);
				}
			}
		}
		return list;
	}

	public bool SinglePlayerNetworkProfilerEnabled()
	{
		if (type == Type.Server)
		{
			return singleplayerNetworkProfilerEnabled;
		}
		if (type == Type.ServerClient)
		{
			if (serverMultiplayer != null)
			{
				return serverMultiplayer.singleplayerNetworkProfilerEnabled;
			}
			return false;
		}
		return false;
	}

	public void StartSSThread()
	{
		ss_queue = new List<SSAction>();
		resume_ss_thread = new AutoResetEvent(initialState: false);
		ss_thread = new Thread(SSThread);
		ss_thread.Start();
	}

	private void SSThread()
	{
		Thread.CurrentThread.Name = "Serialize and Send";
		while (!stopSSThread)
		{
			System.Action action;
			bool in_lock;
			lock (ss_queue)
			{
				if (ss_queue.Count > 0)
				{
					SSAction sSAction = ss_queue[0];
					ss_queue.RemoveAt(0);
					action = sSAction.action;
					in_lock = sSAction.in_lock;
				}
				else
				{
					action = null;
					in_lock = false;
				}
			}
			if (action != null)
			{
				ExecuteSSAction(action, in_lock);
				continue;
			}
			CheckAndSendHeartbeat();
			resume_ss_thread.WaitOne(HeartbeatSendTimeLimitInMilliseconds);
		}
	}

	private void ExecuteSSAction(System.Action action, bool in_lock)
	{
		if (in_lock)
		{
			lock (Lock)
			{
				try
				{
					action();
					return;
				}
				catch (Exception ex)
				{
					Error("Error during serialize and send: " + ex.ToString());
					return;
				}
			}
		}
		try
		{
			action();
		}
		catch (Exception ex2)
		{
			Error("Error during serialize and send: " + ex2.ToString());
		}
	}

	private void CheckAndSendHeartbeat()
	{
		if (HeartbeatReceiveTimeLimitInMilliseconds <= 0 || (type != Type.Server && !IsConnected()))
		{
			return;
		}
		if (type == Type.Server)
		{
			for (int i = 0; i < allServerClients.Count; i++)
			{
				allServerClients[i]?.CheckAndSendHeartbeat();
			}
		}
		else if (connection == null)
		{
			Error($"CheckToSendHeartbeat() but connection is null for {this}");
		}
		else if (heartbeat_timer != null && heartbeat_timer.ElapsedMilliseconds - connection.last_send_time >= HeartbeatSendTimeLimitInMilliseconds)
		{
			BeginMessage(MessageId.HEARTBEAT);
			SendMessage(isObjectMessage: false);
		}
	}

	public void QueueSS(System.Action action, bool in_lock = true)
	{
		if (ss_queue == null)
		{
			ExecuteSSAction(action, in_lock);
			return;
		}
		lock (ss_queue)
		{
			ss_queue.Add(new SSAction(action, in_lock));
			resume_ss_thread.Set();
		}
	}

	public void SendStates(List<Serialization.ObjectStates> states, List<Multiplayer> players, bool full_state)
	{
		List<Multiplayer> players_copy = new List<Multiplayer>(players);
		QueueSS(delegate
		{
			if (full_state)
			{
				for (int i = 0; i < states.Count; i++)
				{
					Serialization.ObjectStates objectStates = states[i];
					lock (Lock)
					{
						BeginMessage(MessageId.OBJ_EVENT);
						msg_writer.WriteMessageHeader(0, objectStates.obj_nid);
						SendMessage(players_copy);
					}
				}
			}
			for (int j = 0; j < states.Count; j++)
			{
				foreach (KeyValuePair<byte, Serialization.ObjectState> state in states[j].states)
				{
					_ = state.Key;
					Serialization.ObjectState value = state.Value;
					lock (Lock)
					{
						BeginMessage(MessageId.OBJ_STATE);
						value.Write(msg_writer);
						SendMessage(players_copy);
					}
					if (value.substates != null)
					{
						for (int k = 0; k < value.substates.Count; k++)
						{
							Serialization.ObjectSubstate objectSubstate = value.substates[k];
							lock (Lock)
							{
								BeginMessage(MessageId.OBJ_SUBSTATE);
								objectSubstate.Write(msg_writer);
								SendMessage(players_copy);
							}
						}
					}
				}
			}
			for (int l = 0; l < states.Count; l++)
			{
				Serialization.ObjectStates objectStates2 = states[l];
				lock (Lock)
				{
					BeginMessage(MessageId.OBJ_EVENT);
					if (objectStates2.started)
					{
						msg_writer.WriteMessageHeader(2, objectStates2.obj_nid);
					}
					else
					{
						msg_writer.WriteMessageHeader(1, objectStates2.obj_nid);
					}
					SendMessage(players_copy);
				}
			}
		}, in_lock: false);
	}

	public void SendStartObj(Object obj)
	{
		if (IsOnline() && obj.IsAuthority())
		{
			NID nid = obj;
			QueueSS(delegate
			{
				BeginMessage(MessageId.OBJ_EVENT);
				msg_writer.WriteMessageHeader(2, nid);
				SendMessage(isObjectMessage: true);
			});
		}
	}

	public void SendObjects()
	{
		if (send_objects.Count <= 0)
		{
			return;
		}
		if (type != Type.Server)
		{
			Error("Attempting to SendObjects() from multiplayer of type " + type);
			ClearSendObjects();
			return;
		}
		lock (Lock)
		{
			if (joinedPlayers == null || joinedPlayers.Count == 0)
			{
				return;
			}
		}
		Game.BeginProfileSection("Generate states");
		List<Serialization.ObjectStates> states = GenerateStates();
		Game.EndProfileSection("Generate states");
		ClearSendObjects();
		Game.BeginProfileSection("Send states");
		SendStates(states, joinedPlayers, full_state: false);
		Game.EndProfileSection("Send states");
	}

	public override void OnUpdate()
	{
		if (type == Type.Client)
		{
			if (status == Status.Connecting)
			{
				UpdateConnection();
			}
		}
		else if (type == Type.Server && connectionReason == ConnectionReason.InGame && game.mapLoaded)
		{
			UpdateJoiningPlayers();
			if (game.campaign != null && game.campaign.IsMultiplayerCampaign())
			{
				UpdateWaitingForPlayers();
				UpdateReconnectingPlayers();
			}
		}
		if (IsOnline())
		{
			if (type == Type.Client && game != null)
			{
				if ((Game.isComingFromTitle && game.state == Game.State.Running) || (!Game.isComingFromTitle && game.mapLoaded))
				{
					UpdateMessageQueue();
				}
			}
			else
			{
				UpdateMessageQueue();
			}
		}
		if (connectionReason == ConnectionReason.InGame)
		{
			UpdateLeavingPlayers();
		}
		SendObjects();
		if (NetworkType == NetworkTransportType.THQNO)
		{
			UpdateHeartbeats();
		}
	}
}
