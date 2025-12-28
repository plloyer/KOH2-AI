using System.Collections.Generic;

namespace Logic;

public class Pause : IVars
{
	public class Request : IVars
	{
		public class Def
		{
			public DT.Field field;

			public bool allow_in_multiplayer;

			public DT.Field timeout_field;

			public DT.Field cooldown_mul_field;

			public string id => field?.key;

			public override string ToString()
			{
				return "Pause.Def(" + field?.Path(include_file: true) + ")";
			}
		}

		public Pause pause;

		public Def def;

		public int pid;

		public Time paused_time;

		public Time timeout_time;

		public string reason => def?.id;

		public Value GetVar(string key, IVars vars, bool as_value)
		{
			switch (key)
			{
			case "pause":
				return new Value(pause);
			case "game":
				return pause?.game;
			case "campaign_rules":
				return pause?.game?.rules;
			case "def":
				return new Value(def?.field);
			case "reason":
				return reason;
			case "pid":
				return pid;
			case "elapsed":
				return pause.time - paused_time;
			case "timeout":
				if (timeout_time == Time.Zero)
				{
					return Value.Null;
				}
				return timeout_time - paused_time;
			case "timeout_remaining":
				if (timeout_time == Time.Zero)
				{
					return Value.Null;
				}
				return timeout_time - pause.time;
			case "cooldwon_remaining":
			{
				if (pid <= 0)
				{
					return Value.Null;
				}
				Time time = pause.cooldowns[pid - 1];
				if (time <= pause.time)
				{
					return Value.Null;
				}
				return time - pause.time;
			}
			case "player_name":
			{
				if (pid <= 0)
				{
					return Value.Null;
				}
				string text2 = pause.game?.campaign?.GetPlayerName(pid - 1);
				if (!string.IsNullOrEmpty(text2))
				{
					return "#" + text2;
				}
				if (pause.debug && !pause.game.IsMultiplayer())
				{
					if (pid == 1)
					{
						return "#Player";
					}
					return $"#Player {pid}";
				}
				return Value.Null;
			}
			case "players":
			{
				List<string> list = new List<string>();
				Campaign campaign = pause.game?.campaign;
				if (campaign == null)
				{
					return Value.Null;
				}
				for (int i = 0; i < 6; i++)
				{
					string playerID = campaign.GetPlayerID(i);
					if (!string.IsNullOrEmpty(playerID) && !(playerID == THQNORequest.userId) && campaign.GetPlayerRuntimeState(i) > Campaign.RuntimeState.Lobby && !campaign.GetPlayerGameLoaded(i))
					{
						list.Add(campaign.GetPlayerName(playerID));
					}
				}
				string text = "";
				for (int j = 0; j < list.Count; j++)
				{
					text = ((j != 0) ? ((j != list.Count - 1 || list.Count < 2) ? (text + ", " + list[j]) : (text + " and " + list[j])) : (text + list[j]));
				}
				return text;
			}
			case "kingdom":
			{
				if (pid <= 0)
				{
					return Value.Null;
				}
				RemoteVars remoteVars = pause.game?.campaign?.GetRemoteVars(RemoteVars.DataType.PersistentPlayerData, pid - 1);
				if (remoteVars == null)
				{
					return Value.Null;
				}
				string name = remoteVars.GetVar("kingdom_name").String();
				Kingdom kingdom = pause.game?.GetKingdom(name);
				if (kingdom != null)
				{
					return kingdom;
				}
				if (pause.debug && !pause.game.IsMultiplayer() && pid == 1)
				{
					return pause.game.GetKingdom();
				}
				return Value.Null;
			}
			case "game_speed":
				if (pause?.game == null)
				{
					return Value.Null;
				}
				return (int)pause.game.speed;
			default:
			{
				if (pause == null)
				{
					return Value.Unknown;
				}
				Value var = pause.GetVar(key, vars, as_value);
				if (!var.is_unknown)
				{
					return var;
				}
				return Value.Unknown;
			}
			}
		}

		public override string ToString()
		{
			string text = reason;
			if (pid >= 0)
			{
				text += $", pid: {pid}";
			}
			if (pause != null && timeout_time > paused_time)
			{
				float num = timeout_time - paused_time;
				float num2 = pause.time - paused_time;
				text += $" ({num2:F1} / {num})";
			}
			return text;
		}
	}

	public Game game;

	public Dictionary<string, Request.Def> defs = new Dictionary<string, Request.Def>();

	public bool debug;

	public List<Request> requests = new List<Request>();

	public Request Resume;

	public Time[] cooldowns = new Time[6];

	public Time time => game.time_unscaled;

	public bool is_paused { get; private set; }

	public Pause(Game game)
	{
		this.game = game;
	}

	public void LoadDefs()
	{
		defs.Clear();
		DT.Def def = game.dt.FindDef("PauseRequest");
		if (def == null)
		{
			Game.Log("PauseRequest base def not found", Game.LogType.Error);
			return;
		}
		debug = def.field.GetBool("debug");
		if (def.defs == null || def.defs.Count == 0)
		{
			Game.Log("No PauseRequest defs found", Game.LogType.Error);
			return;
		}
		for (int i = 0; i < def.defs.Count; i++)
		{
			DT.Def def2 = def.defs[i];
			Request.Def def3 = LoadDef(def2.field);
			defs.Add(def3.id, def3);
		}
	}

	private Request.Def LoadDef(DT.Field field)
	{
		return new Request.Def
		{
			field = field,
			allow_in_multiplayer = field.GetBool("allow_in_multiplayer"),
			timeout_field = field.FindChild("timeout"),
			cooldown_mul_field = field.FindChild("cooldown_mul")
		};
	}

	public Request.Def FindDef(string reason)
	{
		if (string.IsNullOrEmpty(reason))
		{
			return null;
		}
		if (defs.TryGetValue(reason, out var value))
		{
			return value;
		}
		return null;
	}

	public void Reset(bool apply = true)
	{
		requests.Clear();
		Resume = null;
		for (int i = 0; i < cooldowns.Length; i++)
		{
			cooldowns[i] = Time.Zero;
		}
		is_paused = false;
		if (apply)
		{
			Update();
		}
	}

	public void AddRequest(string reason, int pid = -2)
	{
		Request.Def def = FindDef(reason);
		if (def == null)
		{
			Game.Log("Unknown pause reason '" + reason + "' ignored", Game.LogType.Error);
		}
		else
		{
			if (!def.allow_in_multiplayer && game.IsMultiplayer() && Multiplayer.CurrentPlayers.Count() > 1)
			{
				return;
			}
			if (!game.IsAuthority() && !Game.isLoadingSaveGame && pid < 0)
			{
				game.SendEvent(new Game.GamePauseEvent(pause: true, reason));
				return;
			}
			pid = ResolvePID(pid);
			if (reason == "ManualPause" && pid > 0 && cooldowns[pid - 1] > this.time)
			{
				game.NotifyListeners("game_cant_pause_notification");
			}
			else if (FindRequestIdx(reason, pid) < 0)
			{
				Time time = this.time;
				Request request = new Request
				{
					def = def,
					pid = pid,
					pause = this,
					paused_time = time
				};
				float num = CalcTimeout(request);
				request.timeout_time = ((num < 0f) ? Time.Zero : (time + num));
				requests.Add(request);
				Update();
				game.SendState<Game.PauseState>();
			}
		}
	}

	public void RefreshResumeRequest(int pid = -2)
	{
		if (!game.IsAuthority() && !Game.isLoadingSaveGame && pid < 0)
		{
			game.SendEvent(new Game.GamePauseEvent(pause: true, "GameResumed"));
			return;
		}
		pid = ResolvePID(pid);
		Request.Def def = FindDef("GameResumed");
		Resume = new Request
		{
			def = def,
			pid = pid,
			pause = this
		};
		Update();
		game.SendState<Game.PauseState>();
	}

	private float CalcTimeout(Request req)
	{
		if (req?.def?.timeout_field == null)
		{
			return 0f;
		}
		if (req.pid < 0 && req.reason != "EoWPause")
		{
			return 0f;
		}
		return req.def.timeout_field.Float(req);
	}

	public void DelRequest(string reason, int pid = -2)
	{
		if (!game.IsAuthority())
		{
			game.SendEvent(new Game.GamePauseEvent(pause: false, reason));
			return;
		}
		pid = ResolvePID(pid);
		bool flag = false;
		int num = FindRequestIdx(reason, pid);
		if (num >= 0)
		{
			Request req = requests[num];
			RefreshCooldown(req);
			flag = true;
			requests.RemoveAt(num);
		}
		if (reason == "ManualPause" && DeleteTimedOutRequests())
		{
			flag = true;
		}
		if (requests.Count == 0)
		{
			RefreshResumeRequest(pid);
		}
		if (flag)
		{
			Update();
			game.SendState<Game.PauseState>();
		}
	}

	public void ToggleManualPause(int pid = -2)
	{
		if ((!HasRequest("EoWPause")) ? (requests.Count > 0) : (requests.Count > 1))
		{
			DelRequest("ManualPause", pid);
		}
		else
		{
			AddRequest("ManualPause", pid);
		}
	}

	private bool IsTimedOut(Request req)
	{
		if (req.timeout_time == Time.Zero)
		{
			return false;
		}
		if (time < req.timeout_time)
		{
			return false;
		}
		return true;
	}

	private bool DeleteTimedOutRequests()
	{
		bool result = false;
		for (int num = requests.Count - 1; num >= 0; num--)
		{
			Request req = requests[num];
			if (IsTimedOut(req))
			{
				RefreshCooldown(req);
				result = true;
				requests.RemoveAt(num);
			}
		}
		return result;
	}

	private void RefreshCooldown(Request req)
	{
		if (req.pid <= 0 || req?.def.cooldown_mul_field == null)
		{
			return;
		}
		float num = req.def.cooldown_mul_field.Float(req);
		if (num != 0f)
		{
			Time obj = this.time;
			float num2 = obj - req.paused_time;
			if (num2 > 300f)
			{
				num2 = 300f;
			}
			float num3 = num2 * num;
			Time time = obj + num3;
			Time time2 = cooldowns[req.pid - 1];
			if (time > time2)
			{
				cooldowns[req.pid - 1] = time;
			}
		}
	}

	public void Update()
	{
		is_paused = requests.Count > 0;
		game.NotifyListeners("game_pause_changed");
	}

	public int FindRequestIdx(string reason, int pid = -2)
	{
		for (int i = 0; i < requests.Count; i++)
		{
			Request request = requests[i];
			if (!(request.reason != reason))
			{
				if (pid <= -2)
				{
					return i;
				}
				if (request.pid == pid)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public Request FindRequest(string reason, int pid = -2)
	{
		int num = FindRequestIdx(reason, pid);
		if (num < 0)
		{
			return null;
		}
		return requests[num];
	}

	public bool HasRequest(string reason, int pid = -2)
	{
		if (FindRequestIdx(reason, pid) < 0)
		{
			return false;
		}
		return true;
	}

	public bool CanPause(int pid = -2)
	{
		return CalcCannotPauseTime(pid) <= 0f;
	}

	public bool CanUnpause(int pid = -2)
	{
		return CalcCannotUnpauseTime(pid) <= 0f;
	}

	public float CalcCannotPauseTime(int pid = -2)
	{
		pid = ResolvePID(pid);
		if (pid <= 0)
		{
			return 0f;
		}
		Time time = cooldowns[pid - 1];
		if (time <= this.time)
		{
			return 0f;
		}
		return time - this.time;
	}

	public float CalcCannotUnpauseTime(int pid = -2)
	{
		pid = ResolvePID(pid);
		if (pid <= 0)
		{
			return 0f;
		}
		float num = 0f;
		Time time = this.time;
		for (int i = 0; i < requests.Count; i++)
		{
			Request request = requests[i];
			if (!CanUnpause(request, pid))
			{
				if (request.timeout_time == Time.Zero)
				{
					return float.PositiveInfinity;
				}
				float num2 = request.timeout_time - time;
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		return num;
	}

	private bool CanUnpause(Request req, int pid)
	{
		if (IsTimedOut(req))
		{
			return true;
		}
		if (req.pid == pid && req.reason == "ManualPause")
		{
			return true;
		}
		return false;
	}

	public bool IsMultiplayer()
	{
		if (debug)
		{
			return true;
		}
		return game.IsMultiplayer();
	}

	public int ResolvePID(int pid)
	{
		if (pid >= 0)
		{
			return pid;
		}
		if (!IsMultiplayer())
		{
			return -1;
		}
		return game?.multiplayer?.playerData?.pid ?? (-1);
	}

	public int NumActiveCooldowns()
	{
		Time time = this.time;
		int num = 0;
		for (int i = 0; i < cooldowns.Length; i++)
		{
			if (cooldowns[i] > time)
			{
				num++;
			}
		}
		return num;
	}

	public Value GetVar(string key, IVars vars, bool as_value)
	{
		switch (key)
		{
		case "game":
			return game;
		case "campaign_rules":
			return game?.rules;
		case "is_multiplayer":
			return IsMultiplayer();
		case "debug":
			return debug;
		case "can_pause":
			return CanPause();
		case "can_unpause":
			return CanUnpause();
		case "is_paused":
			return is_paused;
		case "cannot_pause_time":
		{
			float num2 = CalcCannotPauseTime();
			if (float.IsInfinity(num2))
			{
				return Value.Null;
			}
			if (num2 <= 0f)
			{
				return Value.Null;
			}
			return num2;
		}
		case "cannot_unpause_time":
		{
			float num = CalcCannotUnpauseTime();
			if (num <= 0f)
			{
				return Value.Null;
			}
			return num;
		}
		default:
			return Value.Unknown;
		}
	}

	public override string ToString()
	{
		return $"Requests: {requests.Count}, Cooldowns: {NumActiveCooldowns()}";
	}

	public string Dump()
	{
		string text = ToString();
		for (int i = 0; i < requests.Count; i++)
		{
			Request arg = requests[i];
			text += $"\n    {arg}";
		}
		Time time = this.time;
		bool flag = false;
		for (int j = 0; j < cooldowns.Length; j++)
		{
			Time time2 = cooldowns[j];
			if (!(time2 <= time))
			{
				if (!flag)
				{
					flag = true;
					text += "\nCooldowns:";
				}
				text += $"\n    {j + 1}: {time2 - time:F1}";
			}
		}
		return text;
	}
}
