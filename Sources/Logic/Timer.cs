namespace Logic;

public class Timer : Component, IVars
{
	public class Def
	{
		public DT.Field field;

		public RuleTimer.Def rtdef;

		public string name;

		public float initial_tick_min;

		public float initial_tick_max;

		public float secondary_tick_min;

		public float secondary_tick_max;

		public bool repeating;

		public int log;

		public int max_ticks;

		public bool Load(Game game, DT.Field f)
		{
			this.field = f;
			name = f.Path(include_file: false, unique: false, '_');
			repeating = f.GetBool("repeating", null, repeating);
			initial_tick_min = f.Float(0);
			initial_tick_max = f.Float(1, null, initial_tick_min);
			DT.Field field = f.FindChild("secondary_tick");
			if (field != null)
			{
				secondary_tick_min = field.Float(0);
				secondary_tick_max = field.Float(1, null, secondary_tick_min);
				repeating = true;
			}
			else
			{
				secondary_tick_min = initial_tick_min;
				secondary_tick_max = initial_tick_max;
			}
			max_ticks = f.GetInt("max_ticks", null, max_ticks);
			if (max_ticks > 0)
			{
				repeating = true;
			}
			log = f.GetInt("log");
			if (initial_tick_min <= 0f || initial_tick_max < initial_tick_min)
			{
				Game.Log(f.Path(include_file: true) + ": invalid timer value", Game.LogType.Error);
				return false;
			}
			if (field != null && (secondary_tick_min <= 0f || secondary_tick_max < secondary_tick_min))
			{
				Game.Log(field.Path(include_file: true) + ": invalid timer value", Game.LogType.Error);
				return false;
			}
			if (game.timer_defs.TryGetValue(name, out var value))
			{
				Game.Log(f.Path(include_file: true) + ": duplicated timer name '" + name + "':\n" + value.field.Path(include_file: true), Game.LogType.Error);
			}
			else
			{
				game.timer_defs.Add(name, this);
			}
			return true;
		}

		public static Def Find(Game game, string name)
		{
			if (!game.timer_defs.TryGetValue(name, out var value))
			{
				return null;
			}
			return value;
		}

		public float GetDuration(Game game, bool initial)
		{
			if (initial)
			{
				return game.Random(initial_tick_min, initial_tick_max);
			}
			return game.Random(secondary_tick_min, secondary_tick_max);
		}
	}

	public Def def;

	public Time start_time = Time.Zero;

	public float duration;

	public int tick;

	public bool send_state = true;

	public string name { get; private set; }

	public bool stopped => tick < 0;

	public Timer(Object obj, Def def)
		: base(obj)
	{
		this.def = def;
		name = def.name;
		float num = def.GetDuration(obj.game, initial: true);
		if (def.log >= 1)
		{
			Game.Log("Started " + ToString(), Game.LogType.Message);
		}
		Start(num);
		if (def?.rtdef != null)
		{
			def.rtdef.OnStart(this);
		}
		obj.NotifyListeners("timer_started", this);
	}

	public Timer(Object obj, string name, float duration, bool send_state = true)
		: base(obj)
	{
		this.name = name;
		this.send_state = send_state;
		Start(duration);
	}

	public void Start(float duration)
	{
		this.duration = duration;
		start_time = base.game.time;
		if (duration > 0f)
		{
			UpdateAfter(duration);
		}
		else
		{
			UpdateNextFrame();
		}
		if (obj.IsAuthority() && send_state)
		{
			obj.SendState<Object.TimersState>();
		}
	}

	public void Stop()
	{
		if (stopped)
		{
			return;
		}
		if (def != null && def.log >= 1)
		{
			Game.Log("Stopped " + ToString(), Game.LogType.Message);
		}
		tick = -1;
		obj.RemoveComponent(this);
		if (def != null)
		{
			obj.NotifyListeners("timer_stopped", this);
			if (def.rtdef != null)
			{
				def.rtdef.OnStop(this);
			}
		}
		OnDestroy();
		StopUpdating();
		if (obj.IsAuthority() && send_state)
		{
			obj.SendState<Object.TimersState>();
		}
	}

	public override void OnDestroy()
	{
		if (!stopped)
		{
			tick = -1;
		}
	}

	public float Elapsed()
	{
		if (start_time == Time.Zero)
		{
			return 0f;
		}
		return base.game.time - start_time;
	}

	public void GetProgress(out float cur, out float max)
	{
		cur = Elapsed();
		max = duration;
	}

	public static Timer Start(Object obj, string name, float duration, bool restart = false, bool send_state = true)
	{
		if (obj == null || name == null || duration < 0f)
		{
			Game.Log("Invalid " + duration + "s timer '" + (name ?? "null") + "' requested for " + (obj?.ToString() ?? "null"), Game.LogType.Error);
			return null;
		}
		obj.AssertAuthority();
		Timer timer = Find(obj, name);
		if (timer != null)
		{
			if (restart)
			{
				timer.Start(duration);
			}
			return timer;
		}
		return new Timer(obj, name, duration, send_state);
	}

	public static Timer Stop(Object obj, string name)
	{
		obj?.AssertAuthority();
		Timer timer = Find(obj, name);
		if (timer == null)
		{
			return null;
		}
		timer.Stop();
		return timer;
	}

	public static Timer Find(Object obj, string name)
	{
		if (obj == null || obj.components == null)
		{
			return null;
		}
		for (int i = 0; i < obj.components.Count; i++)
		{
			if (obj.components[i] is Timer timer && timer.name == name)
			{
				return timer;
			}
		}
		return null;
	}

	public override void OnUpdate()
	{
		if (!obj.IsAuthority())
		{
			return;
		}
		tick++;
		if (def != null && def.log >= 2)
		{
			Game.Log(ToString(), Game.LogType.Message);
		}
		obj.OnTimer(this);
		if (stopped)
		{
			return;
		}
		if (def != null)
		{
			if (def.rtdef != null)
			{
				def.rtdef.OnTick(this);
			}
			if (stopped)
			{
				return;
			}
			obj.NotifyListeners(name, this);
			if (stopped)
			{
				return;
			}
			if (def.repeating && (def.max_ticks <= 0 || tick < def.max_ticks))
			{
				float num = def.GetDuration(obj.game, initial: false);
				Start(num);
			}
		}
		if (!IsRegisteredForUpdate())
		{
			Stop();
		}
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "obj":
			return obj;
		case "def":
			return new Value(def);
		case "name":
			return name;
		case "tick":
			return tick;
		case "duration":
			return duration;
		case "elapsed":
			return Elapsed();
		default:
			if (def?.field != null)
			{
				return def.field.GetVar(key, vars, as_value);
			}
			return Value.Unknown;
		}
	}

	public override string ToString()
	{
		string text = ((def != null && def.max_ticks > 0) ? $" / {def.max_ticks}" : "");
		return $"{rtti.name} '{name}' of {obj}: {Elapsed()} / {duration}, tick {tick}{text}";
	}
}
