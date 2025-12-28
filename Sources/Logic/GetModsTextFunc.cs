using System;
using System.Collections.Generic;

namespace Logic;

public class Opinion : BaseObject, IVars
{
	public class Def : Logic.Def
	{
		public delegate string GetModsTextFunc(Opinion opinion);

		public float min_value = -10f;

		public float max_value = 10f;

		public DT.Field initial_value;

		public int history_size = 10;

		public List<StatModifier.Def> mods;

		public static List<Def> all;

		public static GetModsTextFunc get_mods_text;

		public override bool Load(Game game)
		{
			if (IsBase())
			{
				all = null;
			}
			DT.Field field = base.field;
			DT.Field field2 = field.FindChild("value_range");
			if (field2 != null)
			{
				min_value = field2.Float(0);
				max_value = field2.Float(1);
			}
			initial_value = field.FindChild("initial_value");
			history_size = field.GetInt("history_size", null, history_size);
			LoadMods(game);
			return true;
		}

		private void LoadMods(Game game)
		{
			mods = null;
			List<DT.Field> list = base.field.FindChild("stat_mods")?.Children();
			if (list == null)
			{
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				DT.Field field = list[i];
				if (field.Type() != "mod")
				{
					continue;
				}
				StatModifier.Def def = new StatModifier.Def();
				if (def.Load(game, field))
				{
					if (mods == null)
					{
						mods = new List<StatModifier.Def>();
					}
					mods.Add(def);
				}
			}
		}

		public override bool Validate(Game game)
		{
			if (IsBase())
			{
				all = game.defs.GetDefs<Def>();
			}
			ValidateMods(game);
			return true;
		}

		private void ValidateMods(Game game)
		{
			if (mods == null)
			{
				return;
			}
			Stats.Def def = game.defs.Get<Stats.Def>("KingdomStats");
			for (int i = 0; i < mods.Count; i++)
			{
				StatModifier.Def def2 = mods[i];
				if (!def.HasStat(def2.stat_name))
				{
					Game.Log(def2.field.Path(include_file: true) + ": Unknown Kingdom stat: " + base.id + "." + def2.stat_name, Game.LogType.Error);
					mods.RemoveAt(i);
					i--;
				}
			}
		}
	}

	public struct Change
	{
		public Time time;

		public float amount;

		public string reason;

		public Vars vars;

		public override string ToString()
		{
			return $"{amount} ({reason})";
		}
	}

	public class RefData : Data
	{
		public int kingdom_id;

		public string name;

		public static RefData Create()
		{
			return new RefData();
		}

		public override string ToString()
		{
			return base.ToString() + "(" + name + " of " + kingdom_id + ")";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Opinion opinion))
			{
				return false;
			}
			kingdom_id = opinion.kingdom.id;
			name = opinion.def.id;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kingdom_id, "kingdom");
			ser.WriteStr(name, "name");
		}

		public override void Load(Serialization.IReader ser)
		{
			kingdom_id = ser.Read7BitUInt("kingdom");
			name = ser.ReadStr("name");
		}

		public override object GetObject(Game game)
		{
			Kingdom kingdom = game.GetKingdom(kingdom_id);
			if (kingdom?.opinions == null)
			{
				return null;
			}
			return kingdom.opinions.Find(name);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Opinion opinion))
			{
				return false;
			}
			if (opinion.def.id != name)
			{
				return false;
			}
			return true;
		}
	}

	public class FullData : RefData
	{
		public struct Change
		{
			public float dt;

			public float amount;

			public string reason;

			public Data vars;
		}

		public float value;

		public List<Change> changes;

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Opinion opinion))
			{
				return false;
			}
			base.InitFrom(obj);
			value = opinion.value;
			if (opinion.changes != null)
			{
				changes = new List<Change>(opinion.changes.Count);
				Time time = opinion.game.time;
				for (int i = 0; i < opinion.changes.Count; i++)
				{
					Opinion.Change change = opinion.changes[i];
					Change item = new Change
					{
						dt = time - change.time,
						amount = change.amount,
						reason = change.reason,
						vars = Data.CreateFull(change.vars)
					};
					changes.Add(item);
				}
			}
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteFloat(value, "value");
			int num = ((changes != null) ? changes.Count : 0);
			ser.Write7BitUInt(num, "changes");
			for (int i = 0; i < num; i++)
			{
				Change change = changes[i];
				ser.WriteFloat(change.dt, "change_dt", i);
				ser.WriteFloat(change.amount, "change_amount", i);
				ser.WriteStr(change.reason, "change_reason", i);
				ser.WriteData(change.vars, "change_vars", i);
			}
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			value = ser.ReadFloat("value");
			int num = ser.Read7BitUInt("changes");
			if (num > 0)
			{
				changes = new List<Change>(num);
			}
			for (int i = 0; i < num; i++)
			{
				float dt = ser.ReadFloat("change_dt", i);
				float amount = ser.ReadFloat("change_amount", i);
				string reason = ser.ReadStr("change_reason", i);
				Data vars = ser.ReadData("change_vars", i);
				changes.Add(new Change
				{
					dt = dt,
					amount = amount,
					reason = reason,
					vars = vars
				});
			}
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Opinion opinion))
			{
				return false;
			}
			if (opinion.def.id != name)
			{
				Game.Log("Attempting to apply " + name + " data to " + opinion.ToString(), Game.LogType.Error);
				return false;
			}
			base.ApplyTo(obj, game);
			opinion.DelMods();
			opinion.value = value;
			opinion.AddMods();
			if (changes != null)
			{
				opinion.changes = new List<Opinion.Change>(changes.Count);
				_ = game.time;
				for (int i = 0; i < changes.Count; i++)
				{
					Change change = changes[i];
					Opinion.Change item = new Opinion.Change
					{
						time = game.time - change.dt,
						amount = change.amount,
						reason = change.reason,
						vars = Data.RestoreObject<Vars>(change.vars, game)
					};
					opinion.changes.Add(item);
				}
			}
			else
			{
				opinion.changes = null;
			}
			opinion.kingdom.NotifyListeners("opinion_changed", opinion);
			return true;
		}
	}

	public class StatModifier : Stat.Modifier, IVars, IListener
	{
		public class Def : IVars
		{
			public DT.Field field;

			public string stat_name;

			public int num_values;

			public Type type;

			public DT.Field condition;

			public List<string> recalc_on;

			public bool is_const;

			public bool round;

			public bool Load(Game game, DT.Field field)
			{
				this.field = field;
				stat_name = field.key;
				num_values = field.NumValues();
				if (num_values <= 0 || num_values > 3)
				{
					Game.Log(field.Path(include_file: true) + ": Invalid number of opinion stat modifier values", Game.LogType.Error);
					return false;
				}
				if (field.FindChild("perc") != null)
				{
					type = Type.Perc;
				}
				else if (field.FindChild("base") != null)
				{
					type = Type.Base;
				}
				else if (field.FindChild("unscaled") != null)
				{
					type = Type.Unscaled;
				}
				condition = field.FindChild("condition");
				recalc_on = null;
				round = field.GetBool("round");
				DT.Field field2 = field.FindChild("recalc_on");
				if (field2 != null)
				{
					recalc_on = new List<string>();
					List<DT.Field> list = field2.Children();
					if (list != null)
					{
						for (int i = 0; i < list.Count; i++)
						{
							DT.Field field3 = list[i];
							if (!string.IsNullOrEmpty(field3.key))
							{
								recalc_on.Add(field3.key);
							}
						}
					}
				}
				is_const = CalcIsConst();
				return true;
			}

			private bool CalcIsConst()
			{
				if (recalc_on != null)
				{
					return true;
				}
				if (condition != null)
				{
					is_const = false;
				}
				num_values = field.NumValues();
				for (int i = 0; i < num_values; i++)
				{
					if (field.Value(i, null, calc_expression: false).obj_val is Expression)
					{
						return false;
					}
				}
				return true;
			}

			public float CalcValue(Opinion opinion, out bool active)
			{
				float num = 0f;
				if (condition != null)
				{
					active = condition.Bool(opinion);
					if (!active)
					{
						return num;
					}
				}
				else
				{
					active = true;
				}
				switch (num_values)
				{
				case 1:
					num = field.Float(opinion);
					break;
				case 2:
				{
					float value2 = opinion.value;
					float min_value = opinion.def.min_value;
					float max_value = opinion.def.max_value;
					float num7 = field.Float(0, opinion);
					float num8 = field.Float(1, opinion);
					num = num7 + (value2 - min_value) / (max_value - min_value) * (num8 - num7);
					break;
				}
				case 3:
				{
					float value = opinion.value;
					float num2 = opinion.def.min_value;
					float num3 = opinion.def.max_value;
					float num4 = field.Float(0, opinion);
					float num5 = field.Float(1, opinion);
					float num6 = field.Float(2, opinion);
					if (value <= 0f)
					{
						num3 = 0f;
						num6 = num5;
					}
					else
					{
						num2 = 0f;
						num4 = num5;
					}
					num = num4 + (value - num2) / (num3 - num2) * (num6 - num4);
					break;
				}
				}
				if (round)
				{
					return (float)Math.Truncate(num);
				}
				return num;
			}

			public Value GetVar(string key, IVars vars = null, bool as_value = true)
			{
				switch (key)
				{
				case "min_val":
					return field.Int(0);
				case "mid_val":
					return num_values switch
					{
						1 => field.Int(0), 
						2 => (field.Int(0) + field.Int(1)) / 2, 
						3 => field.Int(1), 
						_ => field.Int(), 
					};
				case "max_val":
					if (num_values <= 1)
					{
						return field.Int(0);
					}
					return field.Int(num_values - 1);
				case "stat_name":
					return "KingdomStats." + stat_name + ".name";
				default:
					return Value.Unknown;
				}
			}

			public override string ToString()
			{
				return field.Path() + " = '" + field.ValueStr() + "'";
			}
		}

		public Def def;

		public Opinion opinion;

		public StatModifier(Def def, Opinion opinion)
		{
			this.def = def;
			this.opinion = opinion;
			type = def.type;
		}

		public override DT.Field GetField()
		{
			return def.field;
		}

		public override DT.Field GetNameField()
		{
			return opinion.def.field;
		}

		public override bool IsConst()
		{
			return def.is_const;
		}

		public override float CalcValue(Stats stats, Stat stat)
		{
			bool active;
			return def.CalcValue(opinion, out active);
		}

		public override void OnActivate(Stats stats, Stat stat, bool from_state = false)
		{
			base.OnActivate(stats, stat, from_state);
			if (def.recalc_on != null && opinion.kingdom != null)
			{
				opinion.kingdom.AddListener(this);
			}
		}

		public override void OnDeactivate(Stats stats, Stat stat)
		{
			base.OnDeactivate(stats, stat);
			if (def.recalc_on != null && opinion.kingdom != null)
			{
				opinion.kingdom.DelListener(this);
			}
		}

		public void OnMessage(object obj, string message, object param)
		{
			Stat stat = base.stat;
			if (stat != null && def.recalc_on.IndexOf(message) >= 0)
			{
				float num = CalcValue(stat.stats, stat);
				if (num != value)
				{
					stat.DelModifier(this, notify_changed: false);
					value = num;
					stat.AddModifier(this);
				}
			}
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			if (!(key == "value"))
			{
				if (key == "active")
				{
					def.CalcValue(opinion, out var active);
					return active;
				}
				Value var = opinion.GetVar(key, vars, as_value);
				if (!var.is_unknown)
				{
					return var;
				}
				return Value.Unknown;
			}
			bool active2;
			return def.CalcValue(opinion, out active2);
		}

		public override string ToString()
		{
			bool active;
			float num = def.CalcValue(opinion, out active);
			string arg = (active ? "" : " (inactive)");
			return $"{def} -> {num}{arg}";
		}
	}

	public Def def;

	public Opinions opinions;

	public float value;

	public List<StatModifier> mods;

	public List<Change> changes;

	public Kingdom kingdom => opinions?.kingdom;

	public Game game => opinions?.game;

	public int index => opinions.opinions.IndexOf(this);

	public Opinion(Def def, Opinions opinions)
	{
		this.def = def;
		this.opinions = opinions;
		if (def.initial_value != null)
		{
			value = def.initial_value.Float(this);
		}
		CreateMods();
		AddMods();
	}

	public void Modify(float amount, string reason)
	{
		float num = (float)Math.Round(value + amount);
		if (num > def.max_value)
		{
			num = def.max_value;
		}
		else if (num < def.min_value)
		{
			num = def.min_value;
		}
		if (def.history_size > 0)
		{
			if (changes == null)
			{
				changes = new List<Change>();
			}
			changes.Insert(0, new Change
			{
				time = game.time,
				amount = amount,
				reason = reason
			});
			while (changes.Count > def.history_size)
			{
				changes.RemoveAt(changes.Count - 1);
			}
		}
		if (num != value)
		{
			DelMods();
			value = num;
			AddMods();
			kingdom.SendSubstate<Kingdom.OpinionsState.OpinionState>(index);
		}
		kingdom.stability.SpecialEvent(think_rebel: false);
		kingdom.InvalidateIncomes();
		kingdom.NotifyListeners("opinion_changed", this);
	}

	public void Set(float amount, string reason)
	{
		float num = (float)Math.Min(def.max_value, Math.Max(def.min_value, Math.Round(amount)));
		changes = new List<Change>();
		changes.Insert(0, new Change
		{
			time = game.time,
			amount = amount,
			reason = reason
		});
		CreateMods();
		value = num;
		kingdom.SendSubstate<Kingdom.OpinionsState.OpinionState>(index);
		kingdom.stability.SpecialEvent(think_rebel: false);
		kingdom.InvalidateIncomes();
		kingdom.NotifyListeners("opinion_changed", this);
	}

	private void CreateMods()
	{
		if (def.mods != null)
		{
			mods = new List<StatModifier>();
			for (int i = 0; i < def.mods.Count; i++)
			{
				StatModifier item = new StatModifier(def.mods[i], this);
				mods.Add(item);
			}
		}
	}

	private void AddMods()
	{
		if (mods != null)
		{
			Kingdom kingdom = this.kingdom;
			for (int i = 0; i < mods.Count; i++)
			{
				StatModifier statModifier = mods[i];
				kingdom.stats.AddModifier(statModifier.def.stat_name, statModifier);
			}
		}
	}

	private void DelMods()
	{
		if (mods != null)
		{
			_ = kingdom;
			for (int i = 0; i < mods.Count; i++)
			{
				StatModifier statModifier = mods[i];
				statModifier.stat?.DelModifier(statModifier, notify_changed: false);
			}
		}
	}

	public override bool IsRefSerializable()
	{
		return true;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "id":
			return def.id;
		case "value":
			return value;
		case "min_value":
			return def.min_value;
		case "max_value":
			return def.max_value;
		case "is_max":
			return value >= def.max_value;
		case "kingdom":
			return kingdom;
		case "opinions":
			return opinions;
		case "def":
			return def;
		case "is_christian":
			return kingdom != null && kingdom.is_christian;
		case "is_catholic":
			return kingdom != null && kingdom.is_catholic;
		case "is_orthodox":
			return kingdom != null && kingdom.is_orthodox;
		case "is_muslim":
			return kingdom != null && kingdom.is_muslim;
		case "is_pagan":
			return kingdom != null && kingdom.is_pagan;
		case "time_since_last_changed":
			if (changes == null || changes.Count == 0)
			{
				return Value.Null;
			}
			return game.time - changes[0].time;
		case "last_change_amount":
			if (changes == null || changes.Count == 0)
			{
				return Value.Null;
			}
			return changes[0].amount;
		case "last_change_reason":
			if (changes == null || changes.Count == 0)
			{
				return Value.Null;
			}
			return changes[0].reason;
		case "mods_text":
			if (Def.get_mods_text == null)
			{
				return Value.Null;
			}
			return Def.get_mods_text(this);
		default:
		{
			Value var = def.field.GetVar(key, vars ?? this, as_value);
			if (!var.is_unknown)
			{
				return var;
			}
			if (kingdom != null)
			{
				var = kingdom.GetVar(key, vars ?? this, as_value);
				if (!var.is_unknown)
				{
					return var;
				}
			}
			return Value.Unknown;
		}
		}
	}

	public override string ToString()
	{
		return $"{def.id} of {kingdom}: {value}";
	}
}
