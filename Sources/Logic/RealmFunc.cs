using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logic;

public class IncomeModifier : IListener
{
	public enum Type
	{
		Stat,
		Func
	}

	public delegate float KingdomFunc(Kingdom k, IncomeModifier mod);

	public delegate float RealmFunc(Realm r, IncomeModifier mod);

	public delegate float SettlementFunc(Settlement s, IncomeModifier mod);

	public class Def
	{
		public DT.Field field;

		public IncomeLocation.Def location;

		public Type type;

		public bool flat;

		public bool perc;

		public string mul;

		public bool governor;

		public string category;

		public KingdomFunc kingdom_func;

		public RealmFunc realm_func;

		public SettlementFunc settlement_func;

		public List<string> recalc_on;

		public bool Load(DT.Field field, IncomeLocation.Def location, Game game, Stats.Def stats, Stats.Def kingdom_stats)
		{
			this.field = field;
			this.location = location;
			string text = field.Type();
			switch (text)
			{
			case "stat":
				type = Type.Stat;
				break;
			case "kingdom_stat":
				type = Type.Stat;
				stats = kingdom_stats;
				break;
			case "func":
				type = Type.Func;
				break;
			default:
				Game.Log(field.Path(include_file: true) + ": Unknown income modidier type: '" + text + "'", Game.LogType.Error);
				return false;
			}
			DT.Field field2 = field.FindChild("perc");
			if (field2 != null)
			{
				perc = field2.Bool(null, def_val: true);
			}
			else
			{
				DT.Field field3 = field.FindChild("flat");
				if (field3 != null)
				{
					flat = field3.Bool(null, def_val: true);
				}
				else if (field.key.EndsWith("_perc", StringComparison.Ordinal))
				{
					Game.Log($"{field.Path(include_file: true)}: Looks like percentage modifier, did you forget to add '{'{'} perc {'}'}'? Use 'perc = false' to mute this warning.", Game.LogType.Warning);
				}
			}
			if (type == Type.Stat && stats != null)
			{
				Stat.Def def = stats.FindStat(field.key);
				if (def == null)
				{
					Game.Log(field.Path(include_file: true) + ": Unknown stat: " + stats.id + "." + field.key, Game.LogType.Error);
					return false;
				}
				def.AddIncomeMod(this, stats, kingdom_stats);
			}
			if (!ResolveFunc(game))
			{
				return false;
			}
			recalc_on = null;
			DT.Field field4 = field.FindChild("recalc_on");
			if (field4 != null)
			{
				recalc_on = new List<string>();
				List<DT.Field> list = field4.Children();
				if (list != null)
				{
					for (int i = 0; i < list.Count; i++)
					{
						DT.Field field5 = list[i];
						if (!string.IsNullOrEmpty(field5.key))
						{
							recalc_on.Add(field5.key);
						}
					}
				}
			}
			mul = field.GetString("mul", null, null);
			governor = field.key.IndexOf("governor", StringComparison.OrdinalIgnoreCase) >= 0;
			governor = field.GetBool("governor", null, governor);
			category = field.GetString("category", null, field.key);
			return true;
		}

		private bool ResolveFunc(Game game)
		{
			if (type != Type.Func)
			{
				return true;
			}
			System.Type[] types;
			switch (location.type)
			{
			case IncomeLocation.Type.Kingdom:
				types = kingdom_func_params;
				break;
			case IncomeLocation.Type.Realm:
				types = realm_func_params;
				break;
			case IncomeLocation.Type.Settlement:
				types = settlement_func_params;
				break;
			default:
				Game.Log($"{this.field.Path(include_file: true)}: {location.type} modifiers do not support C# functions", Game.LogType.Error);
				return false;
			}
			string text = this.field.String(null, this.field.key);
			MethodInfo method = typeof(Economy).GetMethod(text, types);
			if (method == null || method.ReturnType != typeof(float) || !method.IsStatic || !method.IsPublic)
			{
				Game.Log(this.field.Path(include_file: true) + ": Unknown C# function: " + text, Game.LogType.Error);
				return false;
			}
			if (text == "CalcStatusResources")
			{
				DT.Field field = this.field.GetRef("status");
				if (field != null)
				{
					game.defs.Get<Status.Def>(field.Path()).invalidate_incomes = true;
				}
			}
			else if (text == "CalcActionResources")
			{
				DT.Field field2 = this.field.GetRef("action");
				if (field2 != null)
				{
					game.defs.Get<Action.Def>(field2.Path()).invalidate_incomes = true;
				}
			}
			switch (location.type)
			{
			case IncomeLocation.Type.Kingdom:
				kingdom_func = Delegate.CreateDelegate(typeof(KingdomFunc), method) as KingdomFunc;
				break;
			case IncomeLocation.Type.Realm:
				realm_func = Delegate.CreateDelegate(typeof(RealmFunc), method) as RealmFunc;
				break;
			case IncomeLocation.Type.Settlement:
				settlement_func = Delegate.CreateDelegate(typeof(SettlementFunc), method) as SettlementFunc;
				break;
			}
			return true;
		}

		public float CalcMultiplier(Kingdom k)
		{
			if (perc)
			{
				return -200f;
			}
			if (location == null)
			{
				return -20f;
			}
			if (k == null)
			{
				return -100f;
			}
			if (location.type == IncomeLocation.Type.Kingdom)
			{
				if (!string.IsNullOrEmpty(mul))
				{
					return k.GetVar(mul).Float();
				}
				return -50f;
			}
			float num = -1f;
			for (int i = 0; i < k.realms.Count; i++)
			{
				Realm r = k.realms[i];
				float num2 = CalcMultiplier(r, in_kingdom: true);
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

		public float CalcMultiplier(Realm r, bool in_kingdom = false)
		{
			if (perc)
			{
				Game.Log($"{this}: Calculating multiplier for percentage income mod, this will probably lead to wrong results!", Game.LogType.Error);
			}
			if (r == null)
			{
				return -10f;
			}
			float num = -1f;
			if (!string.IsNullOrEmpty(mul))
			{
				num = r.GetVar(mul).Float();
				if (num == 0f)
				{
					return 0f;
				}
			}
			if (location == null)
			{
				return -20f;
			}
			if (location.type == IncomeLocation.Type.Realm)
			{
				if (in_kingdom && num < 0f)
				{
					return 1f;
				}
				return num;
			}
			if (location.type == IncomeLocation.Type.Settlement)
			{
				if (location.field.key == "Town")
				{
					if (in_kingdom && num < 0f)
					{
						return 1f;
					}
					return num;
				}
				int num2 = 0;
				for (int i = 0; i < r.settlements.Count; i++)
				{
					if (r.settlements[i].MatchType(location.field.key))
					{
						num2++;
					}
				}
				if (num2 == 0)
				{
					return 0f;
				}
				if (num < 0f)
				{
					return num2;
				}
				return num * (float)num2;
			}
			if (location.type == IncomeLocation.Type.Building)
			{
				if (r.castle == null)
				{
					return -30f;
				}
				Building.Def def = r.game.defs.Find<Building.Def>(location.field.key);
				if (def == null)
				{
					return -4f;
				}
				if (!r.castle.HasWorkingBuilding(def))
				{
					return 0f;
				}
				if (num < 0f)
				{
					return 1f;
				}
				return num;
			}
			return -100f;
		}

		public override string ToString()
		{
			return field?.Path(include_file: true) ?? "";
		}
	}

	private static System.Type[] kingdom_func_params = new System.Type[2]
	{
		typeof(Kingdom),
		typeof(IncomeModifier)
	};

	private static System.Type[] realm_func_params = new System.Type[2]
	{
		typeof(Realm),
		typeof(IncomeModifier)
	};

	private static System.Type[] settlement_func_params = new System.Type[2]
	{
		typeof(Settlement),
		typeof(IncomeModifier)
	};

	public Def def;

	public IncomeLocation location;

	public Stat stat;

	public float stat_value = float.NaN;

	public float multiplier = float.NaN;

	public float value = float.NaN;

	public int valid;

	public static IncomeModifier Create(Def def, IncomeLocation location)
	{
		if (def == null || location == null)
		{
			return null;
		}
		IncomeModifier incomeModifier = new IncomeModifier();
		incomeModifier.def = def;
		incomeModifier.location = location;
		incomeModifier.ResolveStat();
		return incomeModifier;
	}

	public void ResolveStat()
	{
		stat?.DelListener(this);
		if (def.field.type == "stat")
		{
			stat = location.parent.stats.Find(def.field.key);
		}
		else if (def.field.type == "kingdom_stat")
		{
			stat = location.parent.kingdom_stats.Find(def.field.key);
		}
		stat?.AddListener(this);
	}

	public void ResolveKingdomStats()
	{
		if (def.field.type == "kingdom_stat")
		{
			ResolveStat();
		}
	}

	public void Calc(bool governed)
	{
		if (def.governor && !governed)
		{
			stat_value = float.NaN;
			multiplier = float.NaN;
			value = 0f;
			valid = 1;
			return;
		}
		valid = ((def.recalc_on != null) ? 1 : 0);
		Realm realm2;
		if (stat != null)
		{
			stat_value = stat.CalcValue();
			if (def.recalc_on == null)
			{
				valid = stat.ConstLevel();
			}
		}
		else if (def.kingdom_func != null)
		{
			if (!(location.parent.obj is Kingdom k))
			{
				stat_value = float.NaN;
				multiplier = float.NaN;
				value = 0f;
				valid = 3;
				return;
			}
			stat_value = def.kingdom_func(k, this);
		}
		else
		{
			if (def.realm_func != null)
			{
				IVars obj = location.parent.obj;
				if (obj == null)
				{
					goto IL_0166;
				}
				if (!(obj is Realm realm))
				{
					if (!(obj is Settlement settlement))
					{
						if (!(obj is Building building))
						{
							goto IL_0166;
						}
						realm2 = building.castle?.GetRealm();
					}
					else
					{
						realm2 = settlement.GetRealm();
					}
				}
				else
				{
					realm2 = realm;
				}
				goto IL_0168;
			}
			if (def.settlement_func == null)
			{
				stat_value = float.NaN;
				multiplier = float.NaN;
				value = 0f;
				valid = 3;
				return;
			}
			if (!(location.parent.obj is Settlement s))
			{
				stat_value = float.NaN;
				multiplier = float.NaN;
				value = 0f;
				valid = 3;
				return;
			}
			stat_value = def.settlement_func(s, this);
		}
		goto IL_0246;
		IL_0166:
		realm2 = null;
		goto IL_0168;
		IL_0168:
		if (realm2 != null)
		{
			stat_value = def.realm_func(realm2, this);
			goto IL_0246;
		}
		stat_value = float.NaN;
		multiplier = float.NaN;
		value = 0f;
		valid = 3;
		return;
		IL_0246:
		value = stat_value;
		if (stat_value != 0f && def.mul != null)
		{
			multiplier = location.parent.obj.GetVar(def.mul).Float();
			value *= multiplier;
		}
	}

	public void OnMessage(object obj, string message, object param)
	{
		valid = 0;
		location?.parent?.Invalidate();
	}

	public bool MatchCategory(string category)
	{
		if (category == "GOVERNOR")
		{
			return def.governor;
		}
		if (category == def.category)
		{
			return true;
		}
		if (def.field.key == category)
		{
			return true;
		}
		return false;
	}

	public string Val2Str()
	{
		string text = DT.FloatToStr(value, 3);
		if (def.mul != null)
		{
			text = DT.FloatToStr(stat_value, 3) + " x " + DT.FloatToStr(multiplier, 3) + " (" + def.mul + ") = " + text;
		}
		if (def.perc)
		{
			text += "%";
		}
		return text;
	}

	public override string ToString()
	{
		return $"[{Incomes.ValidStr(valid)}] {def.type} {def.field.key}: {Val2Str()}";
	}

	public string Dump(bool verbose, string prefix, string new_line)
	{
		string text = prefix + ToString();
		if (stat == null)
		{
			return text;
		}
		new_line += "    ";
		text += new_line;
		new_line += "    ";
		return text + stat.Dump(new_line, verbose);
	}

	public string Dump()
	{
		return Dump(verbose: false, "", "\n");
	}
}
