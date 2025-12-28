using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Logic;

public class Stat : IVars
{
	public class Def
	{
		public DT.Field field;

		public string name;

		public float base_value;

		public GlobalModifier base_mod;

		public float min_value = float.MinValue;

		public float max_value = float.MaxValue;

		public bool show_value = true;

		public bool show_plus_sign = true;

		public List<IncomeModifier.Def> income_mods;

		public ResourceType gives_resource;

		public DT.Field multiplier_field;

		public List<GlobalModifier> global_mods;

		public List<GlobalModifier> ref_mods;

		public static int total_lookups = 0;

		public static int total_calcs = 0;

		public int lookups;

		public int calcs;

		public int cache_hits;

		public int force_cache_hits;

		public long calc_ticks;

		public static long start_ticks = 0L;

		public static long start_game_frame = 0L;

		public static long start_game_updates = 1L;

		private static Vars tmp_vars = new Vars();

		public void ClearProfile()
		{
			total_lookups = (total_calcs = 0);
			lookups = (calcs = (cache_hits = (force_cache_hits = 0)));
			calc_ticks = 0L;
		}

		public Def(DT.Field f)
		{
			Load(f);
		}

		public void Load(DT.Field f)
		{
			income_mods = null;
			field = f;
			if (f == null)
			{
				global_mods = null;
				ref_mods = null;
				return;
			}
			name = f.key;
			if (f.value.obj_val is Expression)
			{
				base_mod = new GlobalModifier(f);
			}
			else
			{
				base_value = f.Float();
			}
			min_value = f.GetFloat("min", null, min_value);
			max_value = f.GetFloat("max", null, max_value);
			show_value = field.GetBool("show_value", null, show_value);
			show_plus_sign = field.GetBool("show_plus_sign", null, show_plus_sign);
			gives_resource = Resource.GetType(f.GetString("gives_resource"));
			multiplier_field = field.FindChild("multiplier");
			LoadGlobalMods(f);
		}

		private void LoadGlobalMods(DT.Field f)
		{
			global_mods = null;
			ref_mods = null;
			List<string> list = f.Keys();
			for (int i = 0; i < list.Count; i++)
			{
				string path = list[i];
				DT.Field field = f.FindChild(path);
				if (field == null || field.type != "mod")
				{
					continue;
				}
				GlobalModifier globalModifier = new GlobalModifier(field);
				if (globalModifier.own_stat != null || globalModifier.kingdom_stat != null)
				{
					if (ref_mods == null)
					{
						ref_mods = new List<GlobalModifier>();
					}
					ref_mods.Add(globalModifier);
				}
				else
				{
					if (global_mods == null)
					{
						global_mods = new List<GlobalModifier>();
					}
					global_mods.Add(globalModifier);
				}
			}
		}

		public void AddIncomeMod(IncomeModifier.Def mdef, Stats.Def stats, Stats.Def kingdom_stats)
		{
			if (income_mods == null)
			{
				income_mods = new List<IncomeModifier.Def>();
			}
			if (income_mods.Count > 0)
			{
				IncomeModifier.Def def = income_mods[0];
				if (def.location.parent.rt != mdef.location.parent.rt)
				{
					Game.Log(mdef.field.Path(include_file: true) + ": " + name + " gives multiple resources!", Game.LogType.Error);
				}
				if (def.perc != mdef.perc)
				{
					Game.Log(mdef.field.Path(include_file: true) + ": " + name + " used both as percentage and flat!", Game.LogType.Error);
				}
			}
			income_mods.Add(mdef);
			if (ref_mods == null)
			{
				return;
			}
			for (int i = 0; i < ref_mods.Count; i++)
			{
				GlobalModifier globalModifier = ref_mods[i];
				Stats.Def stats2;
				Def def2;
				if (globalModifier.kingdom_stat != null)
				{
					stats2 = kingdom_stats;
					def2 = kingdom_stats.FindStat(globalModifier.kingdom_stat);
				}
				else
				{
					stats2 = stats;
					def2 = stats.FindStat(globalModifier.own_stat);
				}
				def2?.AddIncomeMod(mdef, stats2, kingdom_stats);
			}
		}

		public ResourceType GetResource(bool incomes_only = false)
		{
			if (income_mods == null || income_mods.Count == 0)
			{
				if (!incomes_only)
				{
					return gives_resource;
				}
				return ResourceType.None;
			}
			IncomeModifier.Def def = income_mods[0];
			if (def.location?.parent == null)
			{
				return ResourceType.None;
			}
			return def.location.parent.rt;
		}

		public bool HasMultipler()
		{
			if (income_mods != null)
			{
				return true;
			}
			if (multiplier_field != null)
			{
				return true;
			}
			return false;
		}

		public float CalcMultiplier(Realm r, Object obj = null)
		{
			if (income_mods == null)
			{
				if (multiplier_field == null)
				{
					return -2f;
				}
				tmp_vars.Set("obj", obj);
				tmp_vars.Set("realm", r);
				return multiplier_field.Float(tmp_vars, -3f);
			}
			float num = -1f;
			for (int i = 0; i < income_mods.Count; i++)
			{
				IncomeModifier.Def def = income_mods[i];
				float num2 = def.CalcMultiplier(r);
				if (num2 < 0f)
				{
					continue;
				}
				if (num < 0f)
				{
					num = 0f;
				}
				if (def.location.type == IncomeLocation.Type.Building)
				{
					if (num < num2)
					{
						num = num2;
					}
				}
				else
				{
					num += num2;
				}
			}
			return num;
		}

		public float CalcMultiplier(Kingdom k)
		{
			if (k == null)
			{
				return -3f;
			}
			if (income_mods == null)
			{
				if (multiplier_field == null)
				{
					return -2f;
				}
				tmp_vars.Set("obj", k);
				return multiplier_field.Float(tmp_vars);
			}
			float num = -1f;
			for (int i = 0; i < income_mods.Count; i++)
			{
				float num2 = income_mods[i].CalcMultiplier(k);
				if (!(num2 < 0f))
				{
					if (num < 0f)
					{
						num = 0f;
					}
					num += num2;
				}
			}
			return num;
		}

		public Value CalcMultiplier(IVars vars)
		{
			if (vars == null)
			{
				return Value.Null;
			}
			if (!HasMultipler())
			{
				return Value.Null;
			}
			Realm realm = Vars.Get<Realm>(vars, "realm");
			if (realm != null)
			{
				float num = CalcMultiplier(realm);
				if (num < 0f)
				{
					return Value.Null;
				}
				return num;
			}
			if (vars is Kingdom k)
			{
				float num2 = CalcMultiplier(k);
				if (num2 < 0f)
				{
					return Value.Null;
				}
				return num2;
			}
			return Value.Null;
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ForceCached : IDisposable
	{
		public ForceCached(string reason)
		{
			if (!disable_force_cache_optimisations)
			{
				force_cache_depth++;
				if (force_cache_depth <= 1)
				{
					force_cache_reason = reason;
					force_cache_version = ++force_caches;
				}
			}
		}

		public void Dispose()
		{
			if (!disable_force_cache_optimisations)
			{
				force_cache_depth--;
				if (force_cache_depth <= 0)
				{
					force_cache_reason = null;
					force_cache_version = 0;
				}
			}
		}
	}

	public class Modifier
	{
		public enum Type
		{
			Base,
			Perc,
			Unscaled
		}

		public float value;

		public Type type;

		public const float NoValue = float.NaN;

		public Stat stat;

		public object owner
		{
			get
			{
				if (stat != null)
				{
					return stat.owner;
				}
				return null;
			}
		}

		public Modifier()
		{
		}

		public Modifier(float value, Type type = Type.Base)
		{
			this.value = value;
			this.type = type;
		}

		public virtual DT.Field GetField()
		{
			return null;
		}

		public virtual DT.Field GetNameField()
		{
			DT.Field field = GetField().FindChild("mod_name");
			if (field != null)
			{
				return field;
			}
			return null;
		}

		public virtual int MaxStacks()
		{
			return 0;
		}

		public virtual string GetStackGroup()
		{
			return null;
		}

		public virtual void OnActivate(Stats stats, Stat stat, bool from_state = false)
		{
		}

		public virtual void OnDeactivate(Stats stats, Stat stat)
		{
		}

		public static void Apply(float value, Type type, ref float add, ref float perc, ref float base_add)
		{
			if (value != 0f && !float.IsNaN(value))
			{
				switch (type)
				{
				case Type.Unscaled:
					add += value;
					break;
				case Type.Perc:
					perc += value;
					break;
				case Type.Base:
					base_add += value;
					break;
				}
			}
		}

		public static void Revert(float value, Type type, ref float add, ref float perc, ref float base_add)
		{
			if (value != 0f && !float.IsNaN(value))
			{
				switch (type)
				{
				case Type.Unscaled:
					add -= value;
					break;
				case Type.Perc:
					perc -= value;
					break;
				case Type.Base:
					base_add -= value;
					break;
				}
			}
		}

		public void Apply(ref float add, ref float perc, ref float base_add)
		{
			Apply(value, type, ref add, ref perc, ref base_add);
		}

		public void Revert(ref float add, ref float perc, ref float base_add)
		{
			Revert(value, type, ref add, ref perc, ref base_add);
		}

		public virtual bool IsConst()
		{
			return false;
		}

		public virtual float CalcValue(Stats stats, Stat stat)
		{
			return value;
		}

		public static string ToString(float value, Type type)
		{
			string text = ((value >= 0f) ? ("+" + Val2Str(value)) : ("-" + Val2Str(0f - value)));
			switch (type)
			{
			case Type.Perc:
				text += "%";
				break;
			case Type.Base:
				text = "(" + text + ")";
				break;
			}
			return text;
		}

		public string ConstStr()
		{
			if (this is GlobalModifier)
			{
				return "*GMOD";
			}
			if (stat == null)
			{
				return "!stat";
			}
			if (stat.var_mods != null && stat.var_mods.Contains(this))
			{
				return "*func";
			}
			return "const";
		}

		public override string ToString()
		{
			string text = ToString(value, type);
			DT.Field field = GetField();
			return string.Concat(str3: (field == null) ? ("(" + GetType().Name + ")" + text) : (field.Path() + ": " + text), str0: "[", str1: ConstStr(), str2: "] mod ");
		}
	}

	public class GlobalModifier : Modifier
	{
		public DT.Field field;

		public string var_name;

		public string kingdom_stat;

		public string own_stat;

		public Vars vars;

		public DT.Field mul_field;

		public DT.Field condition_field;

		public static Stat cur_stat;

		public static GlobalModifier cur_gmod;

		public GlobalModifier(DT.Field field)
		{
			this.field = field;
			vars = new Vars();
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
			mul_field = field.FindChild("mul");
			condition_field = field.FindChild("condition");
			if (field.value.is_string)
			{
				var_name = field.value.String();
			}
			else if (field.value.obj_val is Expression { type: Expression.Type.Variable } expression)
			{
				var_name = expression.value.String();
			}
			if (var_name == null)
			{
				return;
			}
			if (field.key.StartsWith("kingdom", StringComparison.Ordinal))
			{
				if (field.dt?.Find("KingdomStats." + var_name) != null)
				{
					kingdom_stat = var_name;
				}
				else if (field.value.is_string)
				{
					Game.Log(field.Path(include_file: true) + ": Kingdom stat '" + var_name + "' not found", Game.LogType.Error);
				}
			}
			else if ((field.parent?.parent)?.FindChild(var_name) != null)
			{
				own_stat = var_name;
			}
			else if (field.value.is_string)
			{
				Game.Log(field.Path(include_file: true) + ": Stat '" + var_name + "' not found", Game.LogType.Error);
			}
		}

		public override DT.Field GetField()
		{
			return field;
		}

		private Value CalcFieldValue(DT.Field field, Stats stats, Stat stat, Value def_val = default(Value))
		{
			if (field == null)
			{
				return def_val;
			}
			Vars.ReflectionMode old_mode = Vars.PushReflectionMode(Vars.ReflectionMode.Disabled);
			vars.obj = new Value(stats.owner);
			Stat stat2 = cur_stat;
			GlobalModifier globalModifier = cur_gmod;
			cur_stat = stat;
			cur_gmod = this;
			Value result = field.Value(vars);
			cur_stat = stat2;
			cur_gmod = globalModifier;
			Vars.PopReflectionMode(old_mode);
			return result;
		}

		public float CalcMultiplier(Stats stats, Stat stat)
		{
			if (condition_field != null && !CalcFieldValue(condition_field, stats, stat).Bool())
			{
				return 0f;
			}
			if (mul_field != null)
			{
				return CalcFieldValue(mul_field, stats, stat).Float();
			}
			return 1f;
		}

		public override float CalcValue(Stats stats, Stat stat)
		{
			float num = CalcMultiplier(stats, stat);
			if (num == 0f)
			{
				return 0f;
			}
			if (var_name != null)
			{
				Vars.ReflectionMode old_mode = Vars.PushReflectionMode(Vars.ReflectionMode.Disabled);
				Value exact = Vars.GetExact(stats.owner, var_name);
				Vars.PopReflectionMode(old_mode);
				return exact.Float() * num;
			}
			return CalcFieldValue(field, stats, stat).Float() * num;
		}
	}

	public struct Factor
	{
		public Stat stat;

		public Modifier mod;

		public float value;

		public override string ToString()
		{
			if (mod == null)
			{
				return stat.ToString();
			}
			return mod.ToString();
		}
	}

	public Stats stats;

	public Def def;

	public List<Modifier> all_mods;

	public List<Modifier> var_mods;

	public List<IListener> listeners;

	public float const_add;

	public float const_perc;

	public float const_base_add;

	public bool cache_valid;

	public float base_value;

	public float add;

	public float perc;

	public float base_add;

	public float value;

	public static string force_cache_reason;

	public static int force_cache_depth;

	public static int force_cache_version;

	public static int force_caches;

	public int cache_version;

	private static List<Factor> tmp_factors = new List<Factor>();

	public static bool disable_const_optimisations = false;

	public static bool disable_force_cache_optimisations = false;

	public object owner
	{
		get
		{
			if (stats != null)
			{
				return stats.owner;
			}
			return null;
		}
	}

	public List<Factor> GetFactors(bool include_inactive = false)
	{
		ClearTmpFactors();
		AddTmpFactor(this, include_inactive);
		AddTmpFactorMods(include_inactive);
		return GetTmpFactors();
	}

	public static void ClearTmpFactors()
	{
		tmp_factors.Clear();
	}

	public static List<Factor> GetTmpFactors()
	{
		return tmp_factors;
	}

	public static void AddTmpFactor(Stats stats, StatName stat_name)
	{
		Stat stat = stats?.Find(stat_name);
		if (stat != null)
		{
			float num = stat.base_value;
			Factor item = new Factor
			{
				stat = stat,
				mod = null,
				value = num
			};
			tmp_factors.Add(item);
		}
	}

	public static void AddTmpFactor(Stat stat, bool include_inactive)
	{
		float num = stat.base_value;
		Factor item = new Factor
		{
			stat = stat,
			mod = null,
			value = num
		};
		tmp_factors.Add(item);
	}

	public static void AddTmpFactor(Stat stat, Modifier mod, bool include_inactive)
	{
		float num = mod.CalcValue(stat.stats, stat);
		if (num != 0f || include_inactive)
		{
			Factor item = new Factor
			{
				stat = stat,
				mod = mod,
				value = num
			};
			tmp_factors.Add(item);
		}
	}

	public static void AddTmpFactorMods(Stat stat, bool include_inactive)
	{
		if (stat?.all_mods != null)
		{
			for (int i = 0; i < stat.all_mods.Count; i++)
			{
				Modifier modifier = stat.all_mods[i];
				if (modifier is StatRefModifier statRefModifier)
				{
					AddTmpFactorMods(statRefModifier.tgt_stat, include_inactive);
				}
				else
				{
					AddTmpFactor(stat, modifier, include_inactive);
				}
			}
		}
		if (stat?.def?.global_mods != null)
		{
			for (int j = 0; j < stat.def.global_mods.Count; j++)
			{
				GlobalModifier mod = stat.def.global_mods[j];
				AddTmpFactor(stat, mod, include_inactive);
			}
		}
	}

	public static void AddTmpFactorMods(bool include_inactive)
	{
		int count = tmp_factors.Count;
		for (int i = 0; i < count; i++)
		{
			Factor factor = tmp_factors[i];
			if (factor.mod == null)
			{
				AddTmpFactorMods(factor.stat, include_inactive);
			}
		}
	}

	public static string Val2Str(float val)
	{
		return DT.FloatToStr(val, 3);
	}

	public static string ModsToString(float base_value, float add, float perc, float base_add)
	{
		string text = "";
		if (perc != 0f || add != 0f)
		{
			text += "(";
		}
		text += Val2Str(base_value);
		text = ((!(base_add > 0f)) ? (text + " - " + Val2Str(0f - base_add)) : (text + " + " + Val2Str(base_add)));
		if (perc != 0f || add != 0f)
		{
			text += ")";
		}
		if (perc != 0f)
		{
			text = text + " * " + Val2Str(100f + perc) + "%";
		}
		if (add > 0f)
		{
			text = text + " + " + Val2Str(add);
		}
		else if (add < 0f)
		{
			text = text + " - " + Val2Str(0f - add);
		}
		return text;
	}

	public override string ToString()
	{
		string text = "[" + ConstStr() + "] " + def.name + " = " + Val2Str(value);
		if (add != 0f || perc != 0f || base_add != 0f)
		{
			text = text + " = " + ModsToString(base_value, add, perc, base_add);
		}
		if (all_mods != null && all_mods.Count > 0)
		{
			text = text + ", mods: " + all_mods.Count;
		}
		if (def.ref_mods != null)
		{
			text = text + ", rmods: " + def.ref_mods.Count;
		}
		if (def.global_mods != null)
		{
			text = text + ", gmods: " + def.global_mods.Count;
		}
		return text;
	}

	private void AddDump(ref string smods, Modifier mod, string new_line, bool verbose)
	{
		if (smods != "")
		{
			smods += new_line;
		}
		smods += "    ";
		smods += mod.ToString();
		if (mod is StatRefModifier { tgt_stat: { } tgt_stat })
		{
			new_line += "        ";
			smods += new_line;
			smods += tgt_stat.Dump(new_line, verbose);
		}
	}

	public string Dump(string new_line, bool verbose)
	{
		string text = ToString();
		string smods = "";
		if (all_mods != null)
		{
			for (int i = 0; i < all_mods.Count; i++)
			{
				Modifier modifier = all_mods[i];
				if (verbose || modifier.value != 0f)
				{
					AddDump(ref smods, modifier, new_line, verbose);
				}
			}
		}
		if (def.global_mods != null)
		{
			for (int j = 0; j < def.global_mods.Count; j++)
			{
				GlobalModifier globalModifier = def.global_mods[j];
				if (verbose || globalModifier.value != 0f)
				{
					AddDump(ref smods, globalModifier, new_line, verbose);
				}
			}
		}
		if (smods != "")
		{
			text = text + new_line + smods;
		}
		return text;
	}

	public string Dump()
	{
		return Dump("\n", verbose: false);
	}

	public string DumpAll()
	{
		return Dump("\n", verbose: true);
	}

	public string DebugText()
	{
		return "#" + Dump();
	}

	public Stat(Stats stats, Def def)
	{
		this.stats = stats;
		this.def = def;
	}

	public void SetKingdom(Kingdom k)
	{
		if (all_mods == null)
		{
			return;
		}
		for (int i = 0; i < all_mods.Count; i++)
		{
			if (all_mods[i] is StatRefModifier statRefModifier)
			{
				statRefModifier.SetKingdom(k);
			}
		}
	}

	public void AddListener(IListener listener)
	{
		if (listener == null)
		{
			return;
		}
		if (listeners == null)
		{
			listeners = new List<IListener>();
		}
		for (int i = 0; i < listeners.Count; i++)
		{
			IListener listener2 = listeners[i];
			if (listener2 == listener)
			{
				return;
			}
			if (listener2 == null)
			{
				listeners[i] = listener;
				return;
			}
		}
		listeners.Add(listener);
	}

	public void DelListener(IListener listener)
	{
		if (listeners != null && listener != null)
		{
			int num = listeners.IndexOf(listener);
			if (num >= 0)
			{
				listeners[num] = null;
			}
		}
	}

	public void AddRefMods()
	{
		if (def.ref_mods != null)
		{
			for (int i = 0; i < def.ref_mods.Count; i++)
			{
				StatRefModifier mod = new StatRefModifier(def.ref_mods[i]);
				AddModifier(mod);
			}
		}
	}

	public void DelRefMods()
	{
		if (all_mods == null)
		{
			return;
		}
		for (int num = all_mods.Count - 1; num >= 0; num--)
		{
			Modifier modifier = all_mods[num];
			if (modifier is StatRefModifier)
			{
				DelModifier(modifier, notify_changed: false);
			}
		}
	}

	public void AddModifier(Modifier mod, bool from_state = false)
	{
		if (mod.stat != null)
		{
			Game.Log("Attempting add already assigned modifier\nStat: " + ToString() + "\nMod: " + mod.ToString() + "\nMod stat: " + Object.ToString(mod.stat), Game.LogType.Error);
			return;
		}
		DelStacks(mod, notify_changed: false);
		cache_valid = false;
		if (all_mods == null)
		{
			all_mods = new List<Modifier>();
		}
		all_mods.Add(mod);
		mod.stat = this;
		if (mod.IsConst())
		{
			mod.value = mod.CalcValue(stats, this);
			mod.Apply(ref const_add, ref const_perc, ref const_base_add);
		}
		else
		{
			if (var_mods == null)
			{
				var_mods = new List<Modifier>();
			}
			var_mods.Add(mod);
		}
		mod.OnActivate(stats, this, from_state);
		stats.NotifyChanged(this);
	}

	public void DelModifier(Modifier mod, bool notify_changed = true)
	{
		if (mod.stat != this)
		{
			Game.Log("Attempting to delete non-existing modifier\nStat: " + ToString() + "\nMod: " + mod.ToString() + "\nMod stat: " + Object.ToString(mod.stat), Game.LogType.Error);
			return;
		}
		cache_valid = false;
		mod.stat = null;
		int num = all_mods.IndexOf(mod);
		if (num >= 0)
		{
			all_mods.RemoveAt(num);
			int num2 = ((var_mods == null) ? (-1) : var_mods.IndexOf(mod));
			if (num2 >= 0)
			{
				var_mods.RemoveAt(num2);
			}
			else
			{
				mod.Revert(ref const_add, ref const_perc, ref const_base_add);
			}
		}
		else
		{
			Game.Log("Modifier not found\nStat: " + ToString() + "\nMod: " + mod.ToString() + "\nMod stat: " + Object.ToString(mod.stat), Game.LogType.Error);
		}
		mod.OnDeactivate(stats, this);
		if (notify_changed)
		{
			stats.NotifyChanged(this);
		}
	}

	public void RefreshModifier(Modifier mod, bool notify_changed = true)
	{
		if (var_mods == null || !var_mods.Contains(mod))
		{
			DelModifier(mod, notify_changed: false);
			AddModifier(mod, notify_changed);
		}
	}

	public static bool MatchStackGroup(Modifier mod, string stack_group, DT.Field def_field)
	{
		if (mod == null)
		{
			return false;
		}
		if (mod.GetStackGroup() != stack_group)
		{
			return false;
		}
		if (stack_group != null)
		{
			return true;
		}
		if (mod.GetField() != def_field)
		{
			return false;
		}
		return true;
	}

	public int NumStacks(string stack_group, DT.Field def_field = null)
	{
		if (all_mods == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < all_mods.Count; i++)
		{
			if (MatchStackGroup(all_mods[i], stack_group, def_field))
			{
				num++;
			}
		}
		return num;
	}

	public int NumStacks(Modifier mod)
	{
		string stackGroup = mod.GetStackGroup();
		DT.Field field = ((stackGroup == null) ? mod.GetField() : null);
		if (stackGroup == null && field == null)
		{
			return 0;
		}
		return NumStacks(stackGroup, field);
	}

	public int DelStacks(string stack_group)
	{
		return DelStacks(stack_group, null, 0);
	}

	private int DelStacks(string stack_group, DT.Field def_field, int max_count, bool notify_changed = true)
	{
		if (all_mods == null)
		{
			return 0;
		}
		int num = 0;
		for (int num2 = all_mods.Count - 1; num2 >= 0; num2--)
		{
			Modifier mod = all_mods[num2];
			if (MatchStackGroup(mod, stack_group, def_field) && --max_count < 0)
			{
				num++;
				DelModifier(mod, notify_changed: false);
			}
		}
		if (num > 0 && notify_changed)
		{
			stats.NotifyChanged(this);
		}
		return num;
	}

	private int DelStacks(Modifier mod, bool notify_changed = true)
	{
		int num = mod.MaxStacks();
		if (num <= 0)
		{
			return 0;
		}
		string stackGroup = mod.GetStackGroup();
		DT.Field field = ((stackGroup == null) ? mod.GetField() : null);
		if (stackGroup == null && field == null)
		{
			Game.Log($"Attempting to remove stacks for mod without def: {mod}", Game.LogType.Error);
			return 0;
		}
		int num2 = DelStacks(stackGroup, field, num - 1, notify_changed: false);
		if (num2 > 0 && notify_changed)
		{
			stats.NotifyChanged(this);
		}
		return num2;
	}

	public bool IsConst()
	{
		if (def.base_mod == null && def.global_mods == null)
		{
			if (var_mods != null)
			{
				return var_mods.Count == 0;
			}
			return true;
		}
		return false;
	}

	public int ConstLevel()
	{
		if (def.base_mod != null || (var_mods != null && var_mods.Count > 0))
		{
			return 0;
		}
		if (def.global_mods != null)
		{
			return 0;
		}
		if (def.ref_mods != null)
		{
			return 1;
		}
		return 2;
	}

	public string ConstStr()
	{
		if (def.base_mod != null)
		{
			return "VBASE";
		}
		if (var_mods != null && var_mods.Count > 0)
		{
			return "VMODS";
		}
		if (def.global_mods != null)
		{
			return "GMODS";
		}
		if (def.ref_mods != null)
		{
			return "const";
		}
		return "CONST";
	}

	public float CalcValue(bool forced = false)
	{
		if (disable_const_optimisations)
		{
			forced = true;
		}
		if (cache_valid && !forced)
		{
			def.cache_hits++;
			return value;
		}
		if (force_cache_version != 0 && cache_version == force_cache_version)
		{
			def.force_cache_hits++;
			return value;
		}
		cache_version = force_cache_version;
		def.calcs++;
		Def.total_calcs++;
		long elapsedTicks = Game.prof_timer.ElapsedTicks;
		if (def.base_mod != null)
		{
			base_value = def.base_mod.CalcValue(stats, this);
		}
		else
		{
			base_value = def.base_value;
		}
		List<Modifier> list;
		if (forced)
		{
			add = 0f;
			perc = 0f;
			base_add = 0f;
			list = all_mods;
		}
		else
		{
			add = const_add;
			perc = const_perc;
			base_add = const_base_add;
			list = var_mods;
		}
		bool flag = false;
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				Modifier modifier = list[i];
				float num = modifier.value;
				modifier.value = modifier.CalcValue(stats, this);
				if (forced && modifier.value != num && (var_mods == null || !var_mods.Contains(modifier)))
				{
					Game.Log($"Constant modifier changed from {num} to {modifier.value}: {modifier}", Game.LogType.Error);
				}
				if (float.IsNaN(modifier.value))
				{
					flag = true;
					DelModifier(modifier, notify_changed: false);
					i--;
				}
				else
				{
					modifier.Apply(ref add, ref perc, ref base_add);
				}
			}
		}
		if (def.global_mods != null)
		{
			for (int j = 0; j < def.global_mods.Count; j++)
			{
				GlobalModifier globalModifier = def.global_mods[j];
				globalModifier.value = globalModifier.CalcValue(stats, this);
				if (globalModifier.value != 0f && !float.IsNaN(globalModifier.value))
				{
					globalModifier.Apply(ref add, ref perc, ref base_add);
				}
			}
		}
		value = (base_value + base_add) * (100f + perc) / 100f + add;
		if (value < def.min_value)
		{
			value = def.min_value;
		}
		else if (value > def.max_value)
		{
			value = def.max_value;
		}
		cache_valid = IsConst();
		def.calc_ticks += Game.prof_timer.ElapsedTicks - elapsedTicks;
		if (flag)
		{
			stats.NotifyChanged(this);
		}
		return value;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "value":
			return CalcValue();
		case "base":
			return def.base_value;
		case "base_add":
			CalcValue();
			return base_add;
		case "perc":
			CalcValue();
			return perc;
		case "add":
			CalcValue();
			return add;
		case "min":
			return def.min_value;
		case "max":
			return def.max_value;
		case "factors":
			return new Value(GetFactors());
		case "all_factors":
			return new Value(GetFactors(include_inactive: true));
		default:
			if (all_mods != null)
			{
				for (int i = 0; i < all_mods.Count; i++)
				{
					Modifier modifier = all_mods[i];
					if (modifier.GetField()?.key == key)
					{
						if (as_value)
						{
							return modifier.value;
						}
						return new Value(modifier);
					}
				}
			}
			if (def?.global_mods != null)
			{
				for (int j = 0; j < def.global_mods.Count; j++)
				{
					GlobalModifier globalModifier = def.global_mods[j];
					if (globalModifier.GetField()?.key == key)
					{
						if (!as_value)
						{
							return new Value(globalModifier);
						}
						float num = globalModifier.CalcValue(stats, this);
						if (float.IsNaN(globalModifier.value))
						{
							return 0f;
						}
						return num;
					}
				}
			}
			return Value.Unknown;
		}
	}
}
