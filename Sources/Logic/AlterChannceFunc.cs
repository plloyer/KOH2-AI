using System;
using System.Collections.Generic;

namespace Logic;

public class OutcomeDef : IVars
{
	public delegate void AlterChannceFunc(OutcomeDef outcome, IVars vars);

	public string id;

	public DT.Field field;

	public OutcomeDef parent;

	public DT.Field default_field;

	public DT.Field condition_field;

	public DT.Field chance_field;

	public DT.Field rolls_field;

	public FadingModifier.Def mod_def;

	public int log;

	public bool upScaleOptions;

	public List<OutcomeDef> options;

	public List<OutcomeDef> alsos;

	public float chance;

	private static Vars tmp_vars = new Vars();

	public string type => field.Type();

	public string key => field.key;

	public OutcomeDef(Game game, DT.Field field, DT.Field defaults = null, OutcomeDef parent = null)
	{
		if (parent == null || parent.parent == null)
		{
			id = field.key;
		}
		else
		{
			id = parent.id + "." + field.key;
		}
		this.field = field;
		this.parent = parent;
		default_field = ((parent == null) ? defaults : defaults?.FindChild(field.key));
		string text = field.Type();
		object obj;
		switch (text)
		{
		default:
			obj = null;
			break;
		case "main":
		case "option":
		case "silent":
			obj = field;
			break;
		}
		chance_field = (DT.Field)obj;
		if (parent != null)
		{
			log = parent.log;
		}
		if (text == "mod")
		{
			mod_def = FadingModifier.Def.Load(game, field);
			if (mod_def != null && mod_def.duration == null && mod_def.fade_out_time == null)
			{
				Game.Log(field.Path(include_file: true) + ": Outcome stat modifier will stay forever", Game.LogType.Error);
			}
		}
		upScaleOptions = field.GetBool("upScaleOptions", null, upScaleOptions);
		List<string> list = field.Keys();
		for (int i = 0; i < list.Count; i++)
		{
			string text2 = list[i];
			DT.Field field2 = field.FindChild(text2);
			if (field2 == null)
			{
				continue;
			}
			switch (text2)
			{
			case "condition":
				condition_field = field2;
				continue;
			case "chance":
				chance_field = field2;
				continue;
			case "rolls":
				rolls_field = field2;
				continue;
			case "log":
				log = field2.Int();
				continue;
			case "message":
			case "bullet":
				continue;
			}
			switch (field2.Type())
			{
			case "main":
			case "option":
			case "silent":
				AddOutcome(game, ref options, field2, defaults);
				break;
			case "also":
			case "also_silent":
				AddOutcome(game, ref alsos, field2, defaults);
				break;
			case "mod":
			case "del_mod":
			case "status":
			case "trigger":
			case "set_var":
			case "del_var":
			case "log":
				AddOutcome(game, ref alsos, field2, defaults);
				break;
			}
		}
	}

	public void AddOutcome(Game game, ref List<OutcomeDef> outcomes, DT.Field f, DT.Field defaults)
	{
		OutcomeDef item = new OutcomeDef(game, f, defaults, this);
		if (outcomes == null)
		{
			outcomes = new List<OutcomeDef>();
		}
		outcomes.Add(item);
	}

	public override string ToString()
	{
		string text = field.Path();
		if (!string.IsNullOrEmpty(field.Type()))
		{
			text = field.Type() + " " + text;
		}
		if (chance_field != null && chance_field.value.is_valid)
		{
			text = ((!chance_field.value.is_number) ? (text + " = " + chance_field.value_str) : (text + " = " + chance_field.value_str + "%"));
		}
		if (options != null)
		{
			text = text + " {" + options.Count + "}";
		}
		if (alsos != null)
		{
			text = text + " +{" + alsos.Count + "}";
		}
		return text;
	}

	public void CalcChance(IVars vars, float def_val, List<OutcomeDef> forced_outcomes = null, AlterChannceFunc alter_chance = null)
	{
		int num = log;
		if (type == "log")
		{
			if (num <= 0)
			{
				chance = 0f;
				return;
			}
			num = 0;
		}
		if (forced_outcomes != null)
		{
			bool flag = false;
			for (int i = 0; i < forced_outcomes.Count; i++)
			{
				OutcomeDef outcomeDef = forced_outcomes[i];
				if (outcomeDef == null)
				{
					continue;
				}
				if (IsEquaTolOrParentOf(outcomeDef))
				{
					if (num >= 2)
					{
						Game.Log($"CalcChance({this}): forced", Game.LogType.Message);
					}
					chance = 100f;
					return;
				}
				if (IsSiblingOption(outcomeDef))
				{
					flag = true;
				}
			}
			if (flag)
			{
				if (num >= 2)
				{
					Game.Log($"CalcChance({this}): another option forced", Game.LogType.Message);
				}
				chance = 0f;
				return;
			}
		}
		if (condition_field != null && !condition_field.Bool(vars))
		{
			if (num >= 2)
			{
				Game.Log($"CalcChance({this}): condition failed", Game.LogType.Message);
			}
			chance = 0f;
			return;
		}
		if (chance_field == null)
		{
			chance = def_val;
		}
		else
		{
			chance = chance_field.Float(0, vars, def_val);
			if (chance < 0f && chance_field.value.obj_val is Expression)
			{
				if (num >= 2)
				{
					Game.Log($"CalcChance({this}): expression evaluated to negative value ({chance}), clamping to 0", Game.LogType.Message);
				}
				chance = 0f;
			}
		}
		if (alter_chance != null)
		{
			float num2 = chance;
			alter_chance(this, vars);
			if (num >= 2)
			{
				if (chance != num2)
				{
					Game.Log($"CalcChance({this}): chance = {chance} ({num2})", Game.LogType.Message);
				}
				else
				{
					Game.Log($"CalcChance({this}): chance = {chance}", Game.LogType.Message);
				}
			}
		}
		else if (num >= 2)
		{
			Game.Log($"CalcChance({this}): chance = {chance}", Game.LogType.Message);
		}
	}

	public OutcomeDef FindOption(string name)
	{
		if (options == null)
		{
			return null;
		}
		for (int i = 0; i < options.Count; i++)
		{
			OutcomeDef outcomeDef = options[i];
			if (outcomeDef.field.key == name)
			{
				return outcomeDef;
			}
		}
		return null;
	}

	public OutcomeDef DecideOption(Game game, IVars vars, List<OutcomeDef> forced_outcomes = null, AlterChannceFunc alter_chance = null)
	{
		if (options == null)
		{
			return null;
		}
		if (forced_outcomes != null)
		{
			bool flag = true;
			for (int i = 0; i < forced_outcomes.Count; i++)
			{
				OutcomeDef outcomeDef = forced_outcomes[i];
				if (outcomeDef == null)
				{
					if (i == 0)
					{
						flag = false;
					}
					continue;
				}
				if (flag && outcomeDef == this)
				{
					return null;
				}
				OutcomeDef outcomeDef2 = outcomeDef;
				while (outcomeDef2.parent != null)
				{
					if (options.Contains(outcomeDef2))
					{
						return outcomeDef2;
					}
					outcomeDef2 = outcomeDef2.parent;
				}
			}
		}
		float num = 0f;
		float num2 = 0f;
		int num3 = 0;
		int num4 = 0;
		for (int j = 0; j < options.Count; j++)
		{
			OutcomeDef outcomeDef3 = options[j];
			outcomeDef3.CalcChance(vars, -1f, null, alter_chance);
			if (outcomeDef3.chance > 0f && outcomeDef3.chance < 100f && outcomeDef3.type == "success" && vars is Action { own_kingdom: not null } action && action.own_kingdom.balance_factor_luck != 1f)
			{
				outcomeDef3.chance = 100f * (float)Math.Pow(0.01f * outcomeDef3.chance, 1f / action.own_kingdom.balance_factor_luck);
			}
			if (outcomeDef3.chance < 0f)
			{
				num4++;
				continue;
			}
			if (outcomeDef3.field.type == "main")
			{
				num += outcomeDef3.chance;
				continue;
			}
			num3++;
			num2 += outcomeDef3.chance;
		}
		float num5 = num + num2;
		float num6 = 1f;
		float num7 = 0f;
		if (num5 > 100f)
		{
			if (num >= 100f)
			{
				num6 = 0f;
			}
			else if (num2 > 0f)
			{
				num6 = (100f - num) / num2;
			}
		}
		else if (num4 > 0)
		{
			num7 = (100f - num5) / (float)num4;
		}
		else if (upScaleOptions && num5 < 100f && num2 > 0f)
		{
			num6 = 1f + (100f - num5) / num2;
		}
		if (num6 != 1f || num7 != 0f)
		{
			for (int k = 0; k < options.Count; k++)
			{
				OutcomeDef outcomeDef4 = options[k];
				if (outcomeDef4.chance < 0f)
				{
					outcomeDef4.chance = num7;
				}
				else if (!(outcomeDef4.field.type == "main"))
				{
					outcomeDef4.chance *= num6;
				}
			}
		}
		float num8 = game.Random(0f, 100f);
		OutcomeDef outcomeDef5 = null;
		for (int l = 0; l < options.Count; l++)
		{
			OutcomeDef outcomeDef6 = options[l];
			if (!(outcomeDef6.chance <= 0f))
			{
				if (outcomeDef5 == null || outcomeDef6.chance > outcomeDef5.chance)
				{
					outcomeDef5 = outcomeDef6;
				}
				if (num8 < outcomeDef6.chance)
				{
					return outcomeDef6;
				}
				num8 -= outcomeDef6.chance;
			}
		}
		if (num4 > 0 || num5 >= 100f)
		{
			return outcomeDef5;
		}
		return null;
	}

	public List<OutcomeDef> DecideOutcomes(Game game, IVars vars, List<OutcomeDef> forced_outcomes = null, AlterChannceFunc alter_chance = null)
	{
		List<OutcomeDef> list = new List<OutcomeDef>();
		AddOutcomes(list, game, vars, forced_outcomes, alter_chance);
		ValidateOutcomes(list, game, vars);
		return list;
	}

	public void AddOutcomes(List<OutcomeDef> outcomes, Game game, IVars vars, List<OutcomeDef> forced_outcomes = null, AlterChannceFunc alter_chance = null)
	{
		OutcomeDef outcomeDef = DecideOption(game, vars, forced_outcomes, alter_chance);
		if (outcomeDef != null)
		{
			outcomeDef.AddOutcomes(outcomes, game, vars, forced_outcomes, alter_chance);
		}
		else if (parent != null)
		{
			outcomes.Add(this);
		}
		if (alsos == null)
		{
			return;
		}
		for (int i = 0; i < alsos.Count; i++)
		{
			OutcomeDef outcomeDef2 = alsos[i];
			outcomeDef2.CalcChance(vars, 100f, forced_outcomes, alter_chance);
			if (!(outcomeDef2.chance < 100f) || !(game.Random(0f, 100f) >= outcomeDef2.chance))
			{
				outcomeDef2.AddOutcomes(outcomes, game, vars, forced_outcomes, alter_chance);
			}
		}
	}

	private void ValidateOutcomes(List<OutcomeDef> outcomes, Game game, IVars vars)
	{
		for (int i = 0; i < outcomes.Count; i++)
		{
			OutcomeDef outcomeDef = outcomes[i];
			if (!outcomeDef.Validate(game, vars))
			{
				if (outcomeDef.log >= 2)
				{
					Game.Log($"validate failed: {outcomeDef}", Game.LogType.Message);
				}
				outcomes.RemoveAt(i);
				i--;
			}
		}
	}

	public static List<OutcomeDef> UniqueOutcomes(List<OutcomeDef> outcomes)
	{
		List<OutcomeDef> list = new List<OutcomeDef>();
		for (int i = 0; i < outcomes.Count; i++)
		{
			OutcomeDef outcome = outcomes[i];
			AddUniqueOutcomes(list, outcome);
		}
		return list;
	}

	public static void AddUniqueOutcomes(List<OutcomeDef> unique, OutcomeDef outcome)
	{
		if (outcome?.parent != null && !unique.Contains(outcome))
		{
			AddUniqueOutcomes(unique, outcome.parent);
			unique.Add(outcome);
		}
	}

	public static Vars PrecalculateValues(List<OutcomeDef> unique_outcomes, Game game, IVars vars, Vars set_to_vars = null)
	{
		if (unique_outcomes == null)
		{
			return null;
		}
		Vars vars2 = null;
		for (int i = 0; i < unique_outcomes.Count; i++)
		{
			OutcomeDef outcomeDef = unique_outcomes[i];
			Value val = outcomeDef.CalcValue(game, vars, check_precalculated: false);
			if (!val.is_unknown)
			{
				if (vars2 == null)
				{
					vars2 = new Vars();
				}
				vars2.Set(outcomeDef.id, val);
			}
		}
		if (vars2 == null)
		{
			return null;
		}
		set_to_vars?.Set("outcome_values", vars2);
		return vars2;
	}

	public Value GetPrecalculatedValue(IVars vars)
	{
		if (vars == null)
		{
			return Value.Unknown;
		}
		Vars vars2 = vars.GetVar("outcome_values").Get<Vars>();
		if (vars2 == null)
		{
			return Value.Unknown;
		}
		Value var = vars2.GetVar(id);
		if (var.is_unknown)
		{
			return Value.Unknown;
		}
		return var;
	}

	public OutcomeDef Root()
	{
		OutcomeDef outcomeDef = this;
		while (outcomeDef.parent != null)
		{
			outcomeDef = outcomeDef.parent;
		}
		return outcomeDef;
	}

	public bool IsEquaTolOrParentOf(OutcomeDef outcome)
	{
		while (outcome != null)
		{
			if (outcome == this)
			{
				return true;
			}
			outcome = outcome.parent;
		}
		return false;
	}

	public bool IsSiblingOption(OutcomeDef outcome)
	{
		if (parent != outcome.parent)
		{
			return false;
		}
		if (field?.type != "option" && field.type != "main" && field.type != "silent")
		{
			return false;
		}
		if (outcome.field?.type != "option" && outcome.field.type != "main" && outcome.field.type != "silent")
		{
			return false;
		}
		return true;
	}

	public bool Match(string outcome_key, string search_key, bool wildcard)
	{
		if (!wildcard)
		{
			return outcome_key == search_key;
		}
		return outcome_key.IndexOf(search_key, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public OutcomeDef Find(string path, bool wildcard = false, char delimiter = '.')
	{
		int num = path.IndexOf(delimiter);
		if (num >= 0)
		{
			string path2 = path.Substring(num + 1);
			string path3 = path.Substring(0, num);
			OutcomeDef outcomeDef = Find(path3, wildcard, delimiter);
			if (outcomeDef != null)
			{
				outcomeDef = outcomeDef.Find(path2, wildcard, delimiter);
				if (outcomeDef != null)
				{
					return outcomeDef;
				}
			}
			if (!wildcard)
			{
				return null;
			}
		}
		if (num < 0)
		{
			if (options != null)
			{
				for (int i = 0; i < options.Count; i++)
				{
					OutcomeDef outcomeDef2 = options[i];
					if (Match(outcomeDef2.key, path, wildcard))
					{
						return outcomeDef2;
					}
				}
			}
			if (alsos != null)
			{
				for (int j = 0; j < alsos.Count; j++)
				{
					OutcomeDef outcomeDef3 = alsos[j];
					if (Match(outcomeDef3.key, path, wildcard))
					{
						return outcomeDef3;
					}
				}
			}
		}
		if (!wildcard)
		{
			return null;
		}
		if (options != null)
		{
			for (int k = 0; k < options.Count; k++)
			{
				OutcomeDef outcomeDef4 = options[k].Find(path, wildcard: true, delimiter);
				if (outcomeDef4 != null)
				{
					return outcomeDef4;
				}
			}
		}
		if (alsos != null)
		{
			for (int l = 0; l < alsos.Count; l++)
			{
				OutcomeDef outcomeDef5 = alsos[l].Find(path, wildcard: true, delimiter);
				if (outcomeDef5 != null)
				{
					return outcomeDef5;
				}
			}
		}
		return null;
	}

	public List<OutcomeDef> Parse(string str, bool must_exist = true)
	{
		List<OutcomeDef> list = null;
		int i = 0;
		int length = str.Length;
		bool flag = true;
		if (str.StartsWith("*", StringComparison.Ordinal))
		{
			flag = false;
			for (i++; i < length && str[i] == ' '; i++)
			{
			}
		}
		while (i < length)
		{
			int num = str.IndexOf(',', i);
			if (num < 0)
			{
				num = length;
			}
			string text = str.Substring(i, num - i);
			OutcomeDef outcomeDef = Find(text, wildcard: true);
			if (outcomeDef != null)
			{
				if (list == null)
				{
					list = new List<OutcomeDef>();
				}
				list.Add(outcomeDef);
			}
			else if (must_exist)
			{
				Game.Log("Unknown outcome: " + field.Path(include_file: true) + "." + text, Game.LogType.Error);
			}
			for (i = num + 1; i < length && str[i] == ' '; i++)
			{
			}
		}
		if (list != null && !flag)
		{
			list.Insert(0, null);
		}
		return list;
	}

	public DT.Field FindValue(IVars vars, bool parents, params string[] keys)
	{
		for (int i = 0; i < keys.Length; i++)
		{
			string text = keys[i];
			DT.Field field = this.field;
			if (text.StartsWith("parent.", StringComparison.Ordinal))
			{
				field = parent?.field;
				if (field == null)
				{
					continue;
				}
				text = text.Substring(7);
			}
			else if (text.StartsWith("default.", StringComparison.Ordinal))
			{
				field = default_field;
				if (field == null)
				{
					continue;
				}
				text = text.Substring(8);
			}
			if (text == "value")
			{
				if (field.value.is_valid)
				{
					return field;
				}
				continue;
			}
			DT.Field field2 = field.FindChild(text, vars);
			if (field2 != null && !field2.Value(null, calc_expression: false, as_value: false).is_unknown)
			{
				return field2;
			}
		}
		if (parents && parent != null)
		{
			DT.Field field3 = parent.FindValue(vars, parents: true, keys);
			if (field3 != null)
			{
				return field3;
			}
		}
		return null;
	}

	public static DT.Field FindValue(List<OutcomeDef> outcomes, Vars vars, bool parents, params string[] keys)
	{
		for (int i = 0; i < outcomes.Count; i++)
		{
			OutcomeDef outcomeDef = outcomes[i];
			vars.Set("outcome", outcomeDef);
			DT.Field field = outcomeDef.FindValue(vars, parents, keys);
			if (field != null)
			{
				return field;
			}
		}
		return null;
	}

	public static DT.Field FindMessageDef(List<OutcomeDef> outcomes, Vars vars)
	{
		return FindValue(outcomes, vars, true, "message.def", "default.message.def")?.Ref(vars);
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "key":
			return this.key;
		case "id":
			return id;
		case "field":
			return Vars.ResolveValue(new Value(field), vars, as_value);
		case "value":
			return CalcValue(null, vars);
		case "parent":
			return new Value(parent);
		case "default":
			return Vars.ResolveValue(new Value(default_field), vars, as_value);
		case "chance":
			return Vars.ResolveValue(new Value(chance_field), vars, as_value);
		case "rolls":
			return Vars.ResolveValue(new Value(rolls_field), vars, as_value);
		case "options":
			return new Value(options);
		case "alsos":
			return new Value(alsos);
		case "src_kingdom":
		case "source_kingdom":
			as_value = true;
			break;
		case "tgt_kingdom":
		case "target_kingdom":
			as_value = true;
			break;
		}
		Value var = field.GetVar(key, vars, as_value);
		if (!var.is_unknown)
		{
			return var;
		}
		if (vars != null)
		{
			var = vars.GetVar(key, null, as_value);
			if (!var.is_unknown)
			{
				return var;
			}
		}
		if (default_field != null)
		{
			var = default_field.GetVar(key, vars, as_value);
			if (!var.is_unknown)
			{
				return var;
			}
		}
		return Value.Unknown;
	}

	private float GetFloat(IVars vars, string key)
	{
		return GetVar(key, vars).Float();
	}

	private string GetString(IVars vars, string key)
	{
		return GetVar(key, vars).String();
	}

	public T GetObj<T>(IVars vars, string key, bool as_value = true) where T : class
	{
		return GetVar(key, vars, as_value).Get<T>();
	}

	public bool ApplyMod(Game game, IVars vars)
	{
		if (mod_def == null)
		{
			return false;
		}
		Object obj = GetObj<Object>(vars, "owner");
		if (obj == null)
		{
			obj = GetObj<Object>(vars, "target");
		}
		if (obj == null)
		{
			return false;
		}
		if (FadingModifier.Add(obj, mod_def, vars) == null)
		{
			return false;
		}
		return true;
	}

	public bool ValidateDelMod(IVars vars)
	{
		Object obj = GetObj<Object>(vars, "owner");
		if (obj == null)
		{
			obj = GetObj<Object>(vars, "target");
		}
		if (obj == null)
		{
			return false;
		}
		Stats stats = obj.GetStats();
		if (stats == null)
		{
			return false;
		}
		Stat stat = stats.Find(field.key);
		if (stat == null)
		{
			return false;
		}
		string text = field.String(vars);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (stat.NumStacks(text) <= 0)
		{
			return false;
		}
		return true;
	}

	public bool ApplyDelMod(Game game, IVars vars)
	{
		Object obj = GetObj<Object>(vars, "owner");
		if (obj == null)
		{
			obj = GetObj<Object>(vars, "target");
		}
		if (obj == null)
		{
			return false;
		}
		Stats stats = obj.GetStats();
		if (stats == null)
		{
			return false;
		}
		Stat stat = stats.Find(field.key);
		if (stat == null)
		{
			return false;
		}
		string text = field.String(vars);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		return stat.DelStacks(text) > 0;
	}

	public bool ApplyStatus(Game game, IVars vars)
	{
		Object obj = GetObj<Object>(vars, "owner");
		if (obj == null)
		{
			obj = GetObj<Object>(vars, "target");
		}
		if (obj == null)
		{
			return false;
		}
		if (obj.AddStatus(field.key) == null)
		{
			return false;
		}
		return true;
	}

	public bool ApplyTrigger(Game game, IVars vars)
	{
		Object obj = GetObj<Object>(vars, "target");
		if (obj == null)
		{
			obj = GetObj<Object>(vars, "owner");
		}
		if (obj == null)
		{
			return false;
		}
		object param = GetVar("param", vars).Object();
		obj.NotifyListeners(field.key, param);
		return true;
	}

	public bool ApplySetVar(Game game, IVars vars, Value precalculated_value = default(Value))
	{
		ISetVar obj = GetObj<ISetVar>(vars, "owner");
		if (obj == null)
		{
			obj = GetObj<ISetVar>(vars, "target");
		}
		if (obj == null)
		{
			return false;
		}
		Value value = CalcValue(game, vars, precalculated_value);
		obj.SetVar(field.key, value);
		return true;
	}

	public bool ApplyDelVar(Game game, IVars vars)
	{
		ISetVar obj = GetObj<ISetVar>(vars, "owner");
		if (obj == null)
		{
			obj = GetObj<ISetVar>(vars, "target");
		}
		if (obj == null)
		{
			return false;
		}
		obj.SetVar(field.key, Value.Unknown);
		return true;
	}

	public bool ApplyLog(Game game, IVars vars)
	{
		if (log <= 0)
		{
			return true;
		}
		Value value = field.Value(vars);
		Game.Log($"{this}: {key} = {value}", Game.LogType.Message);
		return true;
	}

	public bool Validate(Game game, IVars vars)
	{
		string text = type;
		if (text == "del_mod")
		{
			return ValidateDelMod(vars);
		}
		switch (key)
		{
		case "rel_change":
		case "rel_change2":
		case "rel_change3":
		case "rel_change4":
		case "rel_change5":
		case "casus_beli":
		case "rel_change_executed_prisoner":
		case "rel_change_released_prisoner":
		{
			Kingdom obj11 = GetObj<Kingdom>(vars, "src_kingdom");
			Kingdom obj12 = GetObj<Kingdom>(vars, "tgt_kingdom");
			if (obj11 == null || obj12 == null || obj11 == obj12)
			{
				return false;
			}
			if (obj11.type != Kingdom.Type.Regular)
			{
				return false;
			}
			if (obj12.type != Kingdom.Type.Regular)
			{
				return false;
			}
			return true;
		}
		case "refresh_target_army_food":
		case "refresh_target_army_speed":
			return GetObj<Army>(vars, "target") != null;
		case "try_resolve_land_end":
		{
			Character obj2 = GetObj<Character>(vars, "target");
			if (obj2 != null && obj2.GetArmy().rebel != null)
			{
				return obj2.GetArmy().rebel.rebellion.GetComponent<RebellionIndependence>().GetIndependenceKingdom() != null;
			}
			return true;
		}
		case "disorganize_target_army":
		{
			Army army5 = GetObj<Army>(vars, "target");
			if (army5 == null)
			{
				army5 = GetObj<Character>(vars, "target")?.GetArmy();
			}
			if (army5 == null)
			{
				return false;
			}
			if (army5.FindStatus<ArmyDisorginizedStatus>() != null)
			{
				return false;
			}
			if (army5.GetKingdom().GetStat(Stats.ks_disorganized_state_disable) > 0f)
			{
				return false;
			}
			return true;
		}
		case "undisorganize_target_army":
		{
			Army army3 = GetObj<Army>(vars, "target");
			if (army3 == null)
			{
				army3 = GetObj<Character>(vars, "target")?.GetArmy();
			}
			if (army3 == null)
			{
				return false;
			}
			if (army3.FindStatus<ArmyDisorginizedStatus>() == null)
			{
				return false;
			}
			return true;
		}
		case "starve_target_army":
		{
			Army army4 = GetObj<Army>(vars, "target");
			if (army4 == null)
			{
				army4 = GetObj<Character>(vars, "target")?.GetArmy();
			}
			if (army4 == null)
			{
				return false;
			}
			if (army4.FindStatus<ArmyStarvingStatus>() != null)
			{
				return false;
			}
			return true;
		}
		case "unstarve_target_army":
		{
			Army army2 = GetObj<Army>(vars, "target");
			if (army2 == null)
			{
				army2 = GetObj<Character>(vars, "target")?.GetArmy();
			}
			if (army2 == null)
			{
				return false;
			}
			if (army2.FindStatus<ArmyStarvingStatus>() == null)
			{
				return false;
			}
			return true;
		}
		case "turn_target_into_rebel":
		{
			Army army = GetObj<Army>(vars, "target");
			Character character = army?.leader;
			if (army == null)
			{
				character = GetObj<Character>(vars, "target");
				army = character?.GetArmy();
			}
			if (army?.rebel != null)
			{
				return false;
			}
			if (character == null)
			{
				return false;
			}
			if (character != null && character.IsKing())
			{
				return false;
			}
			if (character != null && character.IsCrusader())
			{
				return false;
			}
			Realm realm = army?.realm_in;
			if (realm == null)
			{
				realm = GetObj<Realm>(vars, "realm");
			}
			if (realm == null)
			{
				realm = character?.ChooseRebelRealm();
			}
			if (realm == null || realm.IsSeaRealm())
			{
				return false;
			}
			return true;
		}
		case "try_spawn_rebellion":
			return GetObj<Realm>(vars, "target")?.rebellionRisk.CanRebel() ?? false;
		case "remove_disorder":
			return GetObj<Realm>(vars, "target")?.IsDisorder() ?? false;
		case "set_disorder":
		{
			Realm obj13 = GetObj<Realm>(vars, "target");
			if (obj13 == null)
			{
				return false;
			}
			if (obj13.IsDisorder())
			{
				return false;
			}
			if (obj13.controller is Rebellion)
			{
				return false;
			}
			return true;
		}
		case "give_gold":
		case "give_books":
		case "give_piety":
		{
			Kingdom obj8 = GetObj<Kingdom>(vars, "kingdom");
			if (obj8 == null)
			{
				obj8 = GetObj<Kingdom>(vars, "src_kingdom");
			}
			if (obj8 == null)
			{
				obj8 = GetObj<Kingdom>(vars, "tgt_kingdom");
			}
			if (obj8 == null)
			{
				return false;
			}
			text = key;
			if (!(text == "give_books"))
			{
				if (text == "give_piety")
				{
					return obj8.resources[ResourceType.Piety] < obj8.GetStat(Stats.ks_max_piety);
				}
				return true;
			}
			return obj8.resources[ResourceType.Books] < obj8.GetStat(Stats.ks_max_books);
		}
		case "convert_province_religion":
		{
			Realm obj14 = GetObj<Realm>(vars, "realm");
			if (obj14 == null)
			{
				return false;
			}
			Religion obj15 = GetObj<Religion>(vars, "religion");
			if (obj15 == null)
			{
				return false;
			}
			if (obj14.religion == obj15)
			{
				return false;
			}
			return true;
		}
		case "prestige":
			if (vars == null)
			{
				return false;
			}
			if (GetObj<Kingdom>(vars, "src_kingdom") == null)
			{
				return false;
			}
			return true;
		case "target_prestige":
			if (vars == null)
			{
				return false;
			}
			if (GetObj<Kingdom>(vars, "tgt_kingdom") == null)
			{
				return false;
			}
			return true;
		case "fame":
			if (vars == null)
			{
				return false;
			}
			if (GetObj<Kingdom>(vars, "src_kingdom") == null)
			{
				return false;
			}
			return true;
		case "target_fame":
			if (vars == null)
			{
				return false;
			}
			if (GetObj<Kingdom>(vars, "tgt_kingdom") == null)
			{
				return false;
			}
			return true;
		case "unlock_achievement":
		{
			Kingdom obj3 = GetObj<Kingdom>(vars, "kingdom");
			if (obj3 == null)
			{
				obj3 = GetObj<Kingdom>(vars, "src_kingdom");
			}
			if (obj3 == null)
			{
				obj3 = GetObj<Kingdom>(vars, "tgt_kingdom");
			}
			if (obj3 == null || obj3.IsDefeated())
			{
				return false;
			}
			if (!field.Value(vars).is_string)
			{
				return false;
			}
			return true;
		}
		case "achievement_proggress":
		{
			Kingdom obj9 = GetObj<Kingdom>(vars, "kingdom");
			if (obj9 == null)
			{
				obj9 = GetObj<Kingdom>(vars, "src_kingdom");
			}
			if (obj9 == null)
			{
				obj9 = GetObj<Kingdom>(vars, "tgt_kingdom");
			}
			if (obj9 == null || obj9.IsDefeated())
			{
				return false;
			}
			if (!field.Value(vars).is_string)
			{
				return false;
			}
			if (field.FindChild("amount") == null)
			{
				return false;
			}
			return true;
		}
		case "change_religion_clerics":
			if (GetObj<Kingdom>(vars, "target") == null)
			{
				return false;
			}
			return true;
		case "spawn_rebellion_on_change_religion":
		{
			Kingdom obj6 = GetObj<Kingdom>(vars, "target");
			Religion obj7 = GetObj<Religion>(vars, "old_religion");
			if (obj6 == null || obj7 == null)
			{
				return false;
			}
			return true;
		}
		case "excommunicate":
		{
			Kingdom obj10 = GetObj<Kingdom>(vars, "kingdom");
			if (obj10 == null)
			{
				obj10 = GetObj<Kingdom>(vars, "src_kingdom");
			}
			if (obj10 == null)
			{
				obj10 = GetObj<Kingdom>(vars, "tgt_kingdom");
			}
			if (obj10 == null || obj10.IsDefeated())
			{
				return false;
			}
			if (!obj10.is_catholic)
			{
				return false;
			}
			if (obj10.excommunicated)
			{
				return false;
			}
			Catholic catholic2 = game.religions.catholic;
			if (obj10 == catholic2.hq_kingdom)
			{
				return false;
			}
			return true;
		}
		case "pope_leave":
		{
			Catholic catholic = game.religions.catholic;
			if (catholic.head_kingdom == catholic.hq_kingdom)
			{
				return false;
			}
			return true;
		}
		case "destroy_target":
			return GetObj<Object>(vars, "target")?.IsValid() ?? false;
		case "join_rebel":
		{
			Character obj4 = GetObj<Character>(vars, "target");
			Kingdom obj5 = GetObj<Kingdom>(vars, "kingdom");
			if (obj4 == null || !obj4.IsRebel() || obj5 == null)
			{
				return false;
			}
			return true;
		}
		case "rel_change_with_orthodox_constantinople":
		case "rel_change_with_independent_orthodox":
		{
			Kingdom obj = GetObj<Kingdom>(vars, "src_kingdom");
			if (obj == null)
			{
				return false;
			}
			if (obj.type != Kingdom.Type.Regular)
			{
				return false;
			}
			return true;
		}
		default:
			return true;
		}
	}

	public Value CalcValue(Game game, IVars vars, bool check_precalculated = true)
	{
		if (check_precalculated)
		{
			Value precalculatedValue = GetPrecalculatedValue(vars);
			if (!precalculatedValue.is_unknown)
			{
				return precalculatedValue;
			}
		}
		Value result = GetVar("amount", vars);
		if (result.is_unknown && (type == "also" || type == "also_silent" || type == "set_var"))
		{
			result = ((!(vars is Vars vars2) || !(vars2.obj.obj_val is Event obj)) ? field.Value(vars) : field.Value(obj.param as Action));
		}
		if (result.is_unknown && check_precalculated && type != "also_silent")
		{
			Game.Log($"{this}: Could not resolve value", Game.LogType.Warning);
		}
		return result;
	}

	public bool Apply(Game game, IVars vars)
	{
		if (log >= 1 && type != "log")
		{
			Game.Log($"outcome: {this}, chance: {chance}", Game.LogType.Message);
		}
		if (key[0] == '_')
		{
			return true;
		}
		switch (type)
		{
		case "mod":
			return ApplyMod(game, vars);
		case "del_mod":
			return ApplyDelMod(game, vars);
		case "status":
			return ApplyStatus(game, vars);
		case "trigger":
			return ApplyTrigger(game, vars);
		case "set_var":
			return ApplySetVar(game, vars);
		case "del_var":
			return ApplyDelVar(game, vars);
		case "log":
			return ApplyLog(game, vars);
		default:
			switch (key)
			{
			case "owner_executed":
			{
				Character obj18 = GetObj<Character>(vars, "owner");
				if (obj18 == null)
				{
					obj18 = GetObj<Character>(vars, "own_character");
					if (obj18 == null)
					{
						return false;
					}
				}
				Kingdom prison_kingdom = obj18.prison_kingdom;
				if (prison_kingdom == null)
				{
					return false;
				}
				if (!(prison_kingdom.actions.Find("KillPrisonerAction") is KillPrisonerAction killPrisonerAction))
				{
					return false;
				}
				killPrisonerAction.target = obj18;
				killPrisonerAction.Run();
				return true;
			}
			case "owner_revealed":
				return true;
			case "owner_escaped":
			{
				Character obj39 = GetObj<Character>(vars, "owner");
				if (obj39 == null)
				{
					obj39 = GetObj<Character>(vars, "own_character");
					if (obj39 == null)
					{
						return false;
					}
				}
				if (obj39.prison_kingdom != null)
				{
					obj39.Imprison(null);
					return true;
				}
				obj39.cur_action?.Cancel();
				Action action2 = obj39.actions?.Find("RecallAction");
				if (action2 == null || !action2.Execute(null))
				{
					Game.Log(id + ": Failed to execute RecallAction of " + obj39.ToString(), Game.LogType.Warning);
					obj39.Recall();
				}
				return true;
			}
			case "owner_killed":
			{
				Character obj28 = GetObj<Character>(vars, "owner");
				if (obj28 == null)
				{
					obj28 = GetObj<Character>(vars, "own_character");
					if (obj28 == null)
					{
						return false;
					}
				}
				string obj29 = GetObj<string>(vars, "reason");
				if (string.IsNullOrEmpty(obj29))
				{
					obj28.Die(new DeadStatus("killed_in_action", obj28));
				}
				else
				{
					obj28.Die(new DeadStatus(obj29, obj28));
				}
				return true;
			}
			case "owner_imprisoned":
			{
				Character obj19 = GetObj<Character>(vars, "owner");
				if (obj19 == null)
				{
					obj19 = GetObj<Character>(vars, "own_character");
					if (obj19 == null)
					{
						return false;
					}
				}
				if (obj19.prison_kingdom != null)
				{
					return true;
				}
				Kingdom kingdom2 = obj19.mission_kingdom;
				if (kingdom2 == null)
				{
					kingdom2 = GetObj<Kingdom>(vars, "tgt_kingdom");
				}
				if (kingdom2 != null)
				{
					obj19.Imprison(kingdom2, recall: true, send_state: true, "owner_imprisoned");
					return true;
				}
				return false;
			}
			case "rel_change":
			case "rel_change2":
			case "rel_change3":
			case "rel_change4":
			case "rel_change5":
			{
				Kingdom obj32 = GetObj<Kingdom>(vars, "src_kingdom");
				Kingdom obj33 = GetObj<Kingdom>(vars, "tgt_kingdom");
				if (obj32 == null || obj33 == null || obj32 == obj33)
				{
					return false;
				}
				Value val2 = CalcValue(game, vars);
				obj32.AddRelationModifier(obj33, val2, vars);
				return true;
			}
			case "rel_change_neighbor_kingdoms":
			{
				Kingdom obj50 = GetObj<Kingdom>(vars, "src_kingdom");
				if (obj50 == null)
				{
					return false;
				}
				Value val3 = CalcValue(game, vars);
				float num19 = this.field.GetFloat("max_dist", vars, -1f);
				if (num19 > 0f)
				{
					for (int num20 = 0; num20 < game.kingdoms.Count; num20++)
					{
						Kingdom k2 = game.kingdoms[num20];
						if (!((float)obj50.DistanceToKingdom(k2) > num19))
						{
							obj50.AddRelationModifier(k2, val3, vars);
						}
					}
				}
				else
				{
					foreach (Kingdom neighbor in obj50.neighbors)
					{
						obj50.AddRelationModifier(neighbor, val3, vars);
					}
				}
				return true;
			}
			case "rel_change_with_kingdoms":
			case "rel_change_with_kingdoms2":
			case "rel_change_with_kingdoms3":
			{
				Kingdom obj7 = GetObj<Kingdom>(vars, "src_kingdom");
				if (obj7 == null)
				{
					return false;
				}
				DT.Field field = this.field.FindChild("filter");
				if (field == null)
				{
					return false;
				}
				if (!(field.Value(null, calc_expression: false, as_value: false).obj_val is Expression))
				{
					return false;
				}
				Value val = CalcValue(game, vars);
				tmp_vars.Clear();
				tmp_vars.obj = new Value(vars);
				for (int i = 0; i < game.kingdoms.Count; i++)
				{
					Kingdom kingdom = game.kingdoms[i];
					if (kingdom != obj7 && !kingdom.IsDefeated())
					{
						tmp_vars.Set("k", kingdom);
						if (field.Bool(tmp_vars))
						{
							obj7.AddRelationModifier(kingdom, val, vars);
						}
					}
				}
				return true;
			}
			case "casus_beli":
			{
				Kingdom obj35 = GetObj<Kingdom>(vars, "src_kingdom");
				Kingdom obj36 = GetObj<Kingdom>(vars, "tgt_kingdom");
				if (obj35 == null || obj36 == null)
				{
					return false;
				}
				return true;
			}
			case "crown_authority":
			{
				Kingdom obj2 = GetObj<Kingdom>(vars, "src_kingdom");
				if (obj2 == null)
				{
					return false;
				}
				Value value2 = CalcValue(game, vars);
				if (value2.type == Value.Type.String)
				{
					obj2.GetCrownAuthority().AddModifier(value2.String(), vars);
				}
				else if (value2.type == Value.Type.Int)
				{
					int num = value2.Int();
					if (num == 0)
					{
						return false;
					}
					obj2.GetCrownAuthority().ChangeValue(num);
				}
				return true;
			}
			case "target_crown_authority":
			{
				Kingdom obj12 = GetObj<Kingdom>(vars, "tgt_kingdom");
				if (obj12 == null)
				{
					return false;
				}
				Value value5 = CalcValue(game, vars);
				if (value5.type == Value.Type.String)
				{
					obj12.GetCrownAuthority().AddModifier(value5.String(), vars);
				}
				else if (value5.type == Value.Type.Int)
				{
					int num5 = value5.Int();
					if (num5 == 0)
					{
						return false;
					}
					obj12.GetCrownAuthority().ChangeValue(num5);
				}
				return true;
			}
			case "opinion_change":
			{
				Kingdom obj55 = GetObj<Kingdom>(vars, "kingdom");
				if (obj55 == null)
				{
					obj55 = GetObj<Kingdom>(vars, "src_kingdom");
				}
				if (obj55 == null)
				{
					obj55 = GetObj<Kingdom>(vars, "tgt_kingdom");
				}
				if (obj55 == null)
				{
					return false;
				}
				string name = GetString(vars, "opinion");
				Opinion opinion = obj55.opinions?.Find(name);
				if (opinion == null)
				{
					return false;
				}
				float num24 = CalcValue(game, vars).Float();
				if (num24 == 0f)
				{
					return true;
				}
				string reason = GetString(vars, "reason");
				opinion.Modify(num24, reason);
				return true;
			}
			case "prevent_opinion_drop":
				return true;
			case "give_gold":
			case "give_books":
			case "give_piety":
			{
				Kingdom obj16 = GetObj<Kingdom>(vars, "kingdom");
				if (obj16 == null)
				{
					obj16 = GetObj<Kingdom>(vars, "src_kingdom");
				}
				if (obj16 == null)
				{
					obj16 = GetObj<Kingdom>(vars, "tgt_kingdom");
				}
				if (obj16 == null)
				{
					return false;
				}
				float num7 = CalcValue(game, vars).Float();
				if (num7 == 0f)
				{
					return false;
				}
				ResourceType rt;
				switch (key)
				{
				case "give_gold":
					rt = ResourceType.Gold;
					break;
				case "give_books":
					rt = ResourceType.Books;
					num7 = Math.Min(num7, obj16.GetStat(Stats.ks_max_books) - obj16.resources[rt]);
					break;
				case "give_piety":
					rt = ResourceType.Piety;
					num7 = Math.Min(num7, obj16.GetStat(Stats.ks_max_piety) - obj16.resources[rt]);
					break;
				default:
					return false;
				}
				KingdomAI.Expense.Category category = KingdomAI.Expense.Category.Economy;
				obj16.AddResources(category, rt, num7);
				return true;
			}
			case "convert_province_religion":
			{
				Realm obj41 = GetObj<Realm>(vars, "realm");
				if (obj41 == null)
				{
					return false;
				}
				Religion obj42 = GetObj<Religion>(vars, "religion");
				if (obj42 == null)
				{
					return false;
				}
				if (obj41.religion == obj42)
				{
					return false;
				}
				obj41.SetReligion(obj42);
				return true;
			}
			case "prestige":
			{
				if (vars == null)
				{
					return false;
				}
				Kingdom obj30 = GetObj<Kingdom>(vars, "src_kingdom");
				if (obj30 == null)
				{
					return false;
				}
				Value value9 = CalcValue(game, vars);
				if (value9.is_string)
				{
					obj30.AddPrestigeModifier(value9);
				}
				else
				{
					obj30.AddPrestige(value9);
				}
				return true;
			}
			case "target_prestige":
			{
				if (vars == null)
				{
					return false;
				}
				Kingdom obj34 = GetObj<Kingdom>(vars, "tgt_kingdom");
				if (obj34 == null)
				{
					return false;
				}
				Value value10 = CalcValue(game, vars);
				if (value10.is_string)
				{
					obj34.AddPrestigeModifier(value10);
				}
				else
				{
					obj34.AddPrestige(value10);
				}
				return true;
			}
			case "fame":
			{
				if (vars == null)
				{
					return false;
				}
				Kingdom obj22 = GetObj<Kingdom>(vars, "src_kingdom");
				if (obj22 == null)
				{
					return false;
				}
				Value value8 = CalcValue(game, vars);
				if (value8.is_string)
				{
					obj22.AddFameModifier(value8);
				}
				else
				{
					obj22.AddFame(value8);
				}
				return true;
			}
			case "target_fame":
			{
				if (vars == null)
				{
					return false;
				}
				Kingdom obj48 = GetObj<Kingdom>(vars, "tgt_kingdom");
				if (obj48 == null)
				{
					return false;
				}
				Value value12 = CalcValue(game, vars);
				if (value12.is_string)
				{
					obj48.AddFameModifier(value12);
				}
				else
				{
					obj48.AddFame(value12);
				}
				return true;
			}
			case "unlock_achievement":
			{
				if (Game.isLoadingSaveGame)
				{
					return true;
				}
				Kingdom obj5 = GetObj<Kingdom>(vars, "kingdom");
				if (obj5 == null)
				{
					obj5 = GetObj<Kingdom>(vars, "src_kingdom");
				}
				if (obj5 == null)
				{
					obj5 = GetObj<Kingdom>(vars, "tgt_kingdom");
				}
				if (obj5 == null || obj5.IsDefeated())
				{
					return false;
				}
				if (!obj5.is_player)
				{
					return true;
				}
				Value value4 = this.field.Value(vars);
				if (!value4.is_string)
				{
					return false;
				}
				string text = value4.String();
				if (obj5.is_local_player)
				{
					obj5.game.stats.SetAchievement(text);
					return true;
				}
				Event obj6 = new Event(obj5, "unlock_achievement", text, this.field, notify_listeners: false);
				obj6.send_to_kingdoms = new List<int> { obj5.id };
				obj5.FireEvent(obj6);
				return true;
			}
			case "achievement_proggress":
			{
				if (Game.isLoadingSaveGame)
				{
					return false;
				}
				Kingdom obj13 = GetObj<Kingdom>(vars, "kingdom");
				if (obj13 == null)
				{
					obj13 = GetObj<Kingdom>(vars, "src_kingdom");
				}
				if (obj13 == null)
				{
					obj13 = GetObj<Kingdom>(vars, "tgt_kingdom");
				}
				if (obj13 == null || obj13.IsDefeated())
				{
					return false;
				}
				if (!obj13.is_player)
				{
					return true;
				}
				Value value6 = this.field.Value(vars);
				if (!value6.is_string)
				{
					return false;
				}
				string text2 = value6.String();
				Value value7 = CalcValue(game, vars);
				if (value7.type != Value.Type.Int)
				{
					return false;
				}
				int num6 = value7.Int();
				if (num6 == 0)
				{
					return true;
				}
				if (obj13.is_local_player)
				{
					obj13.game.stats.IncIntStat(text2, num6);
					return true;
				}
				Event obj14 = new Event(obj13, "achievement_proggress", text2, this.field, notify_listeners: false);
				obj14.vars = new Vars();
				obj14.vars.Set("val", num6);
				obj14.send_to_kingdoms = new List<int> { obj13.id };
				obj13.FireEvent(obj14);
				return true;
			}
			case "no_money":
				return true;
			case "rel_change_executed_prisoner":
			{
				Kingdom obj51 = GetObj<Kingdom>(vars, "src_kingdom");
				Kingdom obj52 = GetObj<Kingdom>(vars, "tgt_kingdom");
				if (GetObj<Character>(vars, "target") == null)
				{
					GetObj<Character>(vars, "arg0");
				}
				if (obj51 != obj52 && obj52.type == Kingdom.Type.Regular)
				{
					obj51.AddRelationModifier(obj52, "rel_executed_prisoner", null, GetFloat(vars, "relationship_execute"));
				}
				return true;
			}
			case "rel_change_released_prisoner":
			{
				Kingdom obj43 = GetObj<Kingdom>(vars, "src_kingdom");
				Kingdom obj44 = GetObj<Kingdom>(vars, "tgt_kingdom");
				if (GetObj<Character>(vars, "target") == null)
				{
					GetObj<Character>(vars, "arg0");
				}
				if (obj43 != obj44 && obj44.type == Kingdom.Type.Regular)
				{
					obj43.AddRelationModifier(obj44, "rel_released_prisoner", null, GetFloat(vars, "relationship_release"));
				}
				return true;
			}
			case "crusade_try_establish_new_kingdom":
				return game.religions.catholic.crusade?.TryEstablishKingdom() ?? false;
			case "war_score":
			{
				Character obj25 = GetObj<Character>(vars, "c2");
				Character obj26 = GetObj<Character>(vars, "c1");
				if (obj25 == null || obj26 == null)
				{
					return true;
				}
				Kingdom kingdom3 = obj25.GetKingdom();
				Kingdom kingdom4 = obj26.GetKingdom();
				War war = kingdom3.FindWarWith(kingdom4);
				if (war == null)
				{
					return true;
				}
				string obj27 = GetObj<string>(vars, "activity");
				if (string.IsNullOrEmpty(obj27))
				{
					return false;
				}
				war.AddActivity(obj27, kingdom3, kingdom4);
				return true;
			}
			case "kill_target_character":
			{
				Character obj23 = GetObj<Character>(vars, "target");
				string obj24 = GetObj<string>(vars, "reason");
				if (string.IsNullOrEmpty(obj24))
				{
					obj23.Die();
				}
				else
				{
					obj23.Die(null, obj24);
				}
				return true;
			}
			case "refresh_target_army_speed":
				GetObj<Army>(vars, "target").SetWorldSpeed();
				return true;
			case "refresh_target_army_food":
				GetObj<Army>(vars, "target").RecalcSuppliesRate();
				return true;
			case "try_resolve_land_end":
			{
				Character obj57 = GetObj<Character>(vars, "target");
				if (obj57 != null && obj57.GetArmy().rebel != null)
				{
					obj57.GetArmy().rebel.rebellion.GetComponent<RebellionIndependence>().DeclareIndependence();
				}
				return true;
			}
			case "disorganize_target_army":
			{
				Army army3 = GetObj<Army>(vars, "target");
				if (army3 == null)
				{
					army3 = GetObj<Character>(vars, "target")?.GetArmy();
				}
				if (army3 == null)
				{
					return false;
				}
				ArmyDisorginizedStatus armyDisorginizedStatus = new ArmyDisorginizedStatus(army3.game.defs.Get<Status.Def>("ArmyDisorginizedStatus"));
				if (armyDisorginizedStatus != null)
				{
					army3.AddStatus(armyDisorginizedStatus);
				}
				army3.leader?.NotifyListeners("army_disorganized");
				return true;
			}
			case "undisorganize_target_army":
			{
				Army army = GetObj<Army>(vars, "target");
				if (army == null)
				{
					army = GetObj<Character>(vars, "target")?.GetArmy();
				}
				if (army == null)
				{
					return false;
				}
				army.DelStatus<ArmyDisorginizedStatus>();
				army.leader?.NotifyListeners("army_no_longer_disorganized");
				return true;
			}
			case "starve_target_army":
			{
				Army army5 = GetObj<Army>(vars, "target");
				if (army5 == null)
				{
					army5 = GetObj<Character>(vars, "target")?.GetArmy();
				}
				if (army5 == null)
				{
					return false;
				}
				ArmyStarvingStatus armyStarvingStatus = new ArmyStarvingStatus(army5.game.defs.Get<Status.Def>("ArmyStarvingStatus"));
				if (armyStarvingStatus != null)
				{
					army5.AddStatus(armyStarvingStatus);
				}
				army5.NotifyListeners("army_starving");
				return true;
			}
			case "unstarve_target_army":
			{
				Army army4 = GetObj<Army>(vars, "target");
				if (army4 == null)
				{
					army4 = GetObj<Character>(vars, "target")?.GetArmy();
				}
				if (army4 == null)
				{
					return false;
				}
				army4.DelStatus<ArmyStarvingStatus>();
				army4.NotifyListeners("army_no_longer_starving");
				return true;
			}
			case "become_rebel":
			case "turn_target_into_rebel":
			{
				Army obj15 = GetObj<Army>(vars, "target");
				Character character2 = obj15?.leader;
				if (character2 == null)
				{
					character2 = GetObj<Character>(vars, "target");
				}
				if (character2 == null)
				{
					return false;
				}
				Realm realm2 = obj15?.realm_in;
				if (obj15 == null)
				{
					realm2 = GetObj<Realm>(vars, "realm");
				}
				string text3 = GetString(vars, "rebel_type");
				if (text3 == null)
				{
					text3 = "GeneralRebels";
				}
				return character2.TurnIntoRebel(text3, GetString(vars, "spawn_condition"), null, realm2) != null;
			}
			case "try_spawn_loyalists":
			{
				Realm obj8 = GetObj<Realm>(vars, "target");
				if (obj8 != null)
				{
					obj8.rebellionRisk.TryRebel();
					return true;
				}
				Kingdom obj9 = GetObj<Kingdom>(vars, "target");
				Kingdom obj10 = GetObj<Kingdom>(vars, "loyalists_kingdom");
				if (obj9 == null || obj10 == null)
				{
					return false;
				}
				int num2 = this.field.GetInt("max_realms", null, 1);
				int num3 = 0;
				int num4 = game.Random(0, obj9.realms.Count);
				for (int j = 0; j < obj9.realms.Count; j++)
				{
					if (num3 >= num2)
					{
						break;
					}
					Realm realm = obj9.realms[(j + num4) % obj9.realms.Count];
					if (realm.pop_majority.kingdom == obj10)
					{
						num3 += (realm.rebellionRisk.TryRebel() ? 1 : 0);
					}
				}
				return true;
			}
			case "try_spawn_rebels":
			case "try_spawn_rebellion":
			{
				Realm obj53 = GetObj<Realm>(vars, "target");
				if (obj53 != null)
				{
					obj53.rebellionRisk.TryRebel();
					return true;
				}
				Kingdom obj54 = GetObj<Kingdom>(vars, "target");
				if (obj54 != null)
				{
					int maxRebelsToSpawn = obj54.GetMaxRebelsToSpawn();
					int num21 = 0;
					int num22 = game.Random(0, obj54.realms.Count);
					for (int num23 = 0; num23 < obj54.realms.Count; num23++)
					{
						if (num21 >= maxRebelsToSpawn)
						{
							break;
						}
						Realm realm5 = obj54.realms[(num23 + num22) % obj54.realms.Count];
						num21 += (realm5.rebellionRisk.TryRebel() ? 1 : 0);
					}
					return true;
				}
				return false;
			}
			case "spawn_rebellion_on_change_religion":
			{
				Kingdom obj46 = GetObj<Kingdom>(vars, "target");
				Religion obj47 = GetObj<Religion>(vars, "old_religion");
				DT.Field field2 = this.field.FindChild("chance_crown_athoirty");
				float[] array = new float[3]
				{
					field2.Value(0),
					field2.Value(1),
					field2.Value(2)
				};
				CrownAuthority crownAuthority = obj46.GetCrownAuthority();
				int value11 = crownAuthority.GetValue();
				float num9 = ((value11 < 0) ? obj46.game.Map(value11, crownAuthority.Min(), 0f, array[0], array[1]) : obj46.game.Map(value11, 0f, crownAuthority.Max(), array[1], array[2]));
				float num10 = GetFloat(vars, "chance_neighbor_to_old_religion");
				float num11 = GetFloat(vars, "chance_neighbor_to_split");
				float num12 = GetFloat(vars, "chance_loyalists");
				List<Realm> list = new List<Realm>();
				if (obj46 != null)
				{
					int num13 = this.field.GetInt("max_realms", null, 1);
					int num14 = game.Random(0, obj46.realms.Count);
					for (int l = 0; l < num13; l++)
					{
						Realm realm3 = obj46.realms[(l + num14) % obj46.realms.Count];
						if (realm3.religion != obj47 || realm3.castle.governor != null)
						{
							continue;
						}
						float num15 = num9;
						for (int m = 0; m < realm3.logicNeighborsRestricted.Count; m++)
						{
							Realm realm4 = realm3.logicNeighborsRestricted[m];
							if (realm4.GetKingdom() != obj46 && realm4.GetKingdom().religion == realm3.religion)
							{
								num15 += num10;
								break;
							}
						}
						for (int n = 0; n < realm3.logicNeighborsRestricted.Count; n++)
						{
							Realm item = realm3.logicNeighborsRestricted[n];
							if (list.Contains(item))
							{
								num15 += num11;
								break;
							}
						}
						for (int num16 = 0; num16 < realm3.rebellions.Count; num16++)
						{
							Rebellion rebellion = realm3.rebellions[num16];
							if (rebellion.IsLoyalist() && rebellion.GetLoyalTo().religion == realm3.religion)
							{
								num15 += num12;
							}
						}
						if ((float)obj46.game.Random(0, 100) < num15)
						{
							realm3.rebellionRisk.ForceRebel();
							if (!list.Contains(realm3))
							{
								list.Add(realm3);
							}
						}
					}
					return true;
				}
				return false;
			}
			case "remove_disorder":
			{
				Realm obj37 = GetObj<Realm>(vars, "target");
				if (obj37 == null)
				{
					return false;
				}
				obj37.SetDisorder(value: false);
				return true;
			}
			case "set_disorder":
			{
				Realm obj31 = GetObj<Realm>(vars, "target");
				if (obj31 == null)
				{
					return false;
				}
				obj31.SetDisorder(value: true);
				return true;
			}
			case "add_realm_population":
			{
				Realm obj38 = GetObj<Realm>(vars, "target");
				if (obj38 == null)
				{
					return false;
				}
				int num8 = CalcValue(game, vars).Int();
				if (num8 == 0)
				{
					return false;
				}
				if (num8 > 0)
				{
					return obj38.castle.population.AddVillagers(num8, Population.Type.Worker);
				}
				return obj38.castle.population.RemoveVillagers(-num8, Population.Type.Worker);
			}
			case "excommunicate":
			{
				Kingdom obj3 = GetObj<Kingdom>(vars, "kingdom");
				if (obj3 == null)
				{
					obj3 = GetObj<Kingdom>(vars, "src_kingdom");
				}
				if (obj3 == null)
				{
					obj3 = GetObj<Kingdom>(vars, "tgt_kingdom");
				}
				if (obj3 == null || obj3.IsDefeated())
				{
					return false;
				}
				if (!obj3.is_catholic)
				{
					return false;
				}
				if (obj3.excommunicated)
				{
					return false;
				}
				Catholic catholic2 = game.religions.catholic;
				if (obj3 == catholic2.hq_kingdom)
				{
					return false;
				}
				if (obj3 == catholic2.head_kingdom)
				{
					catholic2.PopeLeave();
				}
				obj3.excommunicated = true;
				Catholic.CheckCardinalTitles(obj3);
				Religion.RefreshModifiers(obj3);
				obj3.FireEvent("excommunicated", null);
				return true;
			}
			case "pope_leave":
			{
				Catholic catholic = game.religions.catholic;
				if (catholic.head_kingdom == catholic.hq_kingdom)
				{
					return false;
				}
				catholic.PopeLeave();
				return true;
			}
			case "change_religion_clerics":
			{
				Kingdom obj56 = GetObj<Kingdom>(vars, "target");
				if (obj56 == null)
				{
					return false;
				}
				obj56.religion.ApplyReligionChangeClericOutcomes(obj56);
				return true;
			}
			case "morale":
			{
				if (vars == null)
				{
					return false;
				}
				Kingdom obj49 = GetObj<Kingdom>(vars, "target");
				if (obj49 == null)
				{
					return false;
				}
				float num17 = CalcValue(game, vars).Float();
				if (num17 != 0f)
				{
					for (int num18 = 0; num18 < obj49.armies.Count; num18++)
					{
						obj49.armies[num18].morale.AddTemporaryMorale(num17);
					}
				}
				return true;
			}
			case "negative_kingdom_food_penalty":
			{
				Object obj45 = GetObj<Object>(vars, "src_kingdom");
				if (obj45 == null)
				{
					return false;
				}
				obj45.NotifyListeners("negative_kingdom_food_penalty");
				return true;
			}
			case "destroy_target":
			{
				Object obj40 = GetObj<Object>(vars, "target");
				if (obj40 == null)
				{
					return false;
				}
				obj40.Destroy();
				return true;
			}
			case "join_rebel":
			{
				Character obj20 = GetObj<Character>(vars, "target");
				Kingdom obj21 = GetObj<Kingdom>(vars, "kingdom");
				if (obj20 == null || obj21 == null)
				{
					return false;
				}
				Army army2 = obj20.GetArmy();
				if (army2 != null)
				{
					obj21.JoinArmy(army2, joinCourt: true);
				}
				else
				{
					obj20.SetKingdom(obj21.id);
					if (obj20.IsInSpecialCourt(obj21))
					{
						obj21.AddCourtMember(obj20);
					}
					RoyalFamily royalFamily = obj20.GetKingdom()?.royalFamily;
					if (royalFamily != null && royalFamily.Children.Contains(obj20))
					{
						obj20.SetTitle("Prince");
						if (royalFamily.Heir == null)
						{
							royalFamily.SetHeir(obj20);
						}
					}
				}
				return true;
			}
			case "lose_autocephaly":
			{
				Kingdom obj17 = GetObj<Kingdom>(vars, "kingdom");
				if (obj17 == null)
				{
					return false;
				}
				obj17.game.religions.orthodox.SetSubordinated(obj17, subordinated: true);
				return true;
			}
			case "activate_witch_hunt":
			{
				Kingdom obj11 = GetObj<Kingdom>(vars, "kingdom");
				if (obj11 == null || !obj11.IsAuthority())
				{
					return false;
				}
				for (int k = 0; k < obj11.court.Count; k++)
				{
					Character character = obj11.court[k];
					if (character != null && character.IsSpy())
					{
						Action action = character.actions?.Find("WitchHuntAction");
						if (action != null)
						{
							WitchHuntAction.from_event = true;
							character.actions.TryActivateOpportunity(action);
							WitchHuntAction.from_event = false;
						}
					}
				}
				return true;
			}
			case "rel_change_with_orthodox_constantinople":
			{
				Kingdom obj4 = GetObj<Kingdom>(vars, "kingdom");
				if (obj4 == null)
				{
					obj4 = GetObj<Kingdom>(vars, "src_kingdom");
				}
				if (obj4 == null)
				{
					return false;
				}
				Value value3 = CalcValue(game, vars);
				obj4.AddRelationModifier(obj4.game.religions.orthodox.hq_kingdom, "rel_change_with_orthodox_constantinople", null, value3);
				return true;
			}
			case "rel_change_with_independent_orthodox":
			{
				Kingdom obj = GetObj<Kingdom>(vars, "kingdom");
				if (obj == null)
				{
					obj = GetObj<Kingdom>(vars, "src_kingdom");
				}
				if (obj == null)
				{
					return false;
				}
				Value value = CalcValue(game, vars);
				foreach (Kingdom kingdom5 in obj.game.kingdoms)
				{
					if (!kingdom5.IsDefeated() && kingdom5.is_orthodox && !kingdom5.subordinated && kingdom5 != obj)
					{
						obj.AddRelationModifier(kingdom5, "rel_change_with_independent_orthodox", null, value);
					}
				}
				return true;
			}
			case "AI_leader":
			case "own_leader":
			case "another_player_leader":
			case "default":
				return true;
			default:
				return false;
			}
		}
	}
}
