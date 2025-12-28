namespace Logic;

public class FadingModifier : Stat.Modifier
{
	public class FullData : Data
	{
		public int umid;

		public string stat_name;

		public string def;

		public NID source;

		public NID target;

		public float tgt_value;

		public float fade_in_time;

		public float duration;

		public float fade_out_time;

		public int max_stacks;

		public int state;

		public float delta_state_time;

		public static FullData Create()
		{
			return new FullData();
		}

		public override object GetObject(Game game)
		{
			return new FadingModifier(game, null);
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is FadingModifier fadingModifier))
			{
				return false;
			}
			umid = fadingModifier.umid;
			stat_name = fadingModifier.stat?.def?.field?.key ?? "";
			def = fadingModifier?.def?.id;
			source = fadingModifier.source;
			target = fadingModifier.target;
			tgt_value = fadingModifier.tgt_value;
			fade_in_time = fadingModifier.fade_in_time;
			duration = fadingModifier.duration;
			fade_out_time = fadingModifier.fade_out_time;
			max_stacks = fadingModifier.max_stacks;
			state = (int)fadingModifier.state;
			delta_state_time = fadingModifier.state_time - (fadingModifier.stat.owner as Object).game.time;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(umid, "umid");
			ser.WriteStr(stat_name, "stat_name");
			ser.WriteStr(def, "def_id");
			ser.WriteNID(source, "source");
			ser.WriteNID(target, "target");
			ser.WriteFloat(tgt_value, "tgt_value");
			ser.WriteFloat(fade_in_time, "fade_in_time");
			ser.WriteFloat(duration, "duration");
			ser.WriteFloat(fade_out_time, "fade_out_time");
			ser.Write7BitUInt(max_stacks, "max_stacks");
			ser.Write7BitUInt(state, "state");
			ser.WriteFloat(delta_state_time, "delta_state_time");
		}

		public override void Load(Serialization.IReader ser)
		{
			umid = ser.Read7BitUInt("umid");
			stat_name = ser.ReadStr("stat_name");
			def = ser.ReadStr("def_id");
			source = ser.ReadNID("source");
			target = ser.ReadNID("target");
			tgt_value = ser.ReadFloat("tgt_value");
			fade_in_time = ser.ReadFloat("fade_in_time");
			duration = ser.ReadFloat("duration");
			fade_out_time = ser.ReadFloat("fade_out_time");
			max_stacks = ser.Read7BitUInt("max_stacks");
			state = ser.Read7BitUInt("state");
			delta_state_time = ser.ReadFloat("delta_state_time");
		}

		public virtual Def GetDef(Game game, string defName)
		{
			return game.defs.Get<Def>(defName);
		}

		public virtual bool ApplyMod(FadingModifier mod, Stats stats, bool is_existing)
		{
			Game game = mod.game;
			mod.umid = umid;
			mod.def = GetDef(game, def);
			mod.source = source.GetObj(game);
			mod.target = target.GetObj(game);
			mod.tgt_value = tgt_value;
			mod.fade_in_time = fade_in_time;
			mod.duration = duration;
			mod.fade_out_time = fade_out_time;
			mod.max_stacks = max_stacks;
			mod.state = (State)state;
			mod.state_time = game.time + delta_state_time;
			if (!is_existing)
			{
				stats.AddModifier(stat_name, mod, from_state: true);
			}
			else if (mod.stat.def.field.key != stat_name)
			{
				Game.Log("Applying different stat to a fading modifier, current = " + mod.stat.def.field.key + ", but it should be" + stat_name, Game.LogType.Error);
				return false;
			}
			return true;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			Stats stats = (obj as Object)?.GetStats();
			if (stats == null)
			{
				return false;
			}
			FadingModifier fadingModifier = stats.GetFadingModifier(umid);
			bool is_existing = false;
			if (fadingModifier == null)
			{
				fadingModifier = GetObject(game) as FadingModifier;
			}
			else
			{
				is_existing = true;
			}
			return ApplyMod(fadingModifier, stats, is_existing);
		}
	}

	public class Def : Logic.Def
	{
		public string stat_name = "";

		public DT.Field value;

		public DT.Field fade_in_time;

		public DT.Field duration;

		public DT.Field fade_out_time;

		public DT.Field max_stacks;

		public string stack_group;

		public float CalcValue(IVars vars)
		{
			return value.Float(vars);
		}

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			stat_name = field.GetString("stat", null, field.key);
			value = field.FindChild("value") ?? field;
			fade_in_time = field.FindChild("fade_in_time");
			fade_out_time = field.FindChild("fade_out_time");
			duration = field.FindChild("duration");
			max_stacks = field.FindChild("max_stacks");
			stack_group = field.GetString("stack_group", null, null);
			return true;
		}

		public static Def NewDef()
		{
			return new Def();
		}

		public static Def Load(Game game, DT.Field f)
		{
			if (f == null)
			{
				return null;
			}
			Def def = NewDef();
			def.dt_def = new DT.Def
			{
				path = f.Path(),
				field = f
			};
			def.dt_def.def = def;
			f.def = def.dt_def;
			if (!def.Load(game))
			{
				return null;
			}
			if (game?.defs?.registries[typeof(Def)]?.defs != null)
			{
				game.defs.registries[typeof(Def)].defs[f.Path()] = def;
			}
			return def;
		}
	}

	public enum State
	{
		FadeIn,
		Active,
		FadeOut,
		Expired
	}

	public int umid;

	public Game game;

	public Def def;

	public Object source;

	public Object target;

	public float tgt_value;

	public float fade_in_time;

	public float duration;

	public float fade_out_time;

	public int max_stacks;

	public State state;

	public Time state_time;

	public virtual Data GetFullData()
	{
		FullData fullData = FullData.Create();
		if (fullData.InitFrom(this))
		{
			return fullData;
		}
		return null;
	}

	public static FadingModifier Add(Object obj, Def def, IVars vars = null)
	{
		if (def == null)
		{
			return null;
		}
		Stats stats = obj?.GetStats();
		if (stats == null)
		{
			return null;
		}
		Stat stat = stats.Find(def.stat_name);
		if (stat == null)
		{
			return null;
		}
		FadingModifier fadingModifier = new FadingModifier(obj.game, def, null, null, vars);
		stat.AddModifier(fadingModifier);
		return fadingModifier;
	}

	public FadingModifier(Game game, Def def, Object source = null, Object target = null, IVars vars = null)
	{
		this.game = game;
		this.def = def;
		this.source = source;
		this.target = target;
		if (def != null)
		{
			if (def.fade_in_time != null)
			{
				fade_in_time = def.fade_in_time.Float(vars);
			}
			if (def.fade_out_time != null)
			{
				fade_out_time = def.fade_out_time.Float(vars);
			}
			duration = ((fade_out_time > 0f) ? 0f : (-1f));
			if (def.duration != null)
			{
				duration = def.duration.Float(vars, duration);
			}
			if (def.max_stacks != null)
			{
				max_stacks = def.max_stacks.Int(vars);
			}
		}
		else
		{
			duration = -1f;
		}
		if (fade_in_time > 0f)
		{
			state = State.FadeIn;
			value = 0f;
		}
		else if (duration <= 0f && fade_out_time > 0f)
		{
			state = State.FadeOut;
		}
		else
		{
			state = State.Active;
		}
		tgt_value = def?.CalcValue(vars) ?? 0f;
		state_time = game.time;
	}

	public override DT.Field GetField()
	{
		return def.field;
	}

	public override string GetStackGroup()
	{
		return def.stack_group;
	}

	public override int MaxStacks()
	{
		return max_stacks;
	}

	public override string ToString()
	{
		return base.ToString();
	}

	private float GetStateDuration()
	{
		return state switch
		{
			State.FadeIn => fade_in_time, 
			State.Active => duration, 
			State.FadeOut => fade_out_time, 
			_ => -1f, 
		};
	}

	public float UpdateState()
	{
		float num = game.time - state_time;
		float stateDuration;
		while (true)
		{
			stateDuration = GetStateDuration();
			if (stateDuration < 0f && (state == State.Active || state == State.Expired))
			{
				return 1f;
			}
			if (num < stateDuration)
			{
				break;
			}
			state++;
			state_time += stateDuration;
			num -= stateDuration;
		}
		return num / stateDuration;
	}

	public override float CalcValue(Stats stats, Stat stat)
	{
		float num = tgt_value;
		float num2 = UpdateState();
		return state switch
		{
			State.FadeIn => num * num2, 
			State.Active => num, 
			State.FadeOut => num * (1f - num2), 
			_ => float.NaN, 
		};
	}

	public override void OnActivate(Stats stats, Stat stat, bool from_state = false)
	{
		Object obj = stats.owner as Object;
		value = CalcValue(stats, stat);
		stats.AddCacheFadingModifier(this);
		if (!from_state)
		{
			umid = ++stats.last_ufmid;
			if (obj.AssertAuthority())
			{
				obj.SendSubstate<Object.FadingModifiersState.FadingModifierState>(umid);
			}
		}
	}

	public override void OnDeactivate(Stats stats, Stat stat)
	{
		Object obj = stats.owner as Object;
		if (stats.DelCacheFadingModifier(this) && obj.IsAuthority())
		{
			obj.SendEvent(new Object.DelFadingModifierEvent(umid));
		}
	}
}
