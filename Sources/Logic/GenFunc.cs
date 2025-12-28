using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logic;

public class Query : IVars
{
	public enum Mode
	{
		Invalid,
		Random,
		First,
		List,
		Count,
		Best,
		Var
	}

	public delegate void GenFunc(Query q);

	public Game game;

	public DT.Field field;

	public GenFunc func;

	public Serialization.ObjectType tid;

	public Mode mode;

	public bool optional;

	private static IVars tmp_vars;

	private static IVars tmp_out_vars;

	private static Value tmp_val;

	private static List<Value> tmp_results = new List<Value>();

	private static int tmp_count;

	private static Value tmp_best;

	private static float tmp_best_eval;

	private static List<float> tmp_weights = new List<float>();

	private static float tmp_total_weights;

	private static Dictionary<string, GenFunc> gen_funcs = null;

	public bool is_valid => mode != Mode.Invalid;

	public Query(Game game, DT.Field field)
	{
		this.game = game;
		this.field = field;
		if (field == null)
		{
			Game.Log("Null query field", Game.LogType.Error);
			return;
		}
		if (string.IsNullOrEmpty(field.key))
		{
			Game.Log(field.Path(include_file: true) + ": Empty query key", Game.LogType.Error);
			return;
		}
		string text = field.Type();
		if (string.IsNullOrEmpty(text))
		{
			Game.Log(field.Path(include_file: true) + ": Empty query type", Game.LogType.Error);
		}
		else if (!field.Value(null, calc_expression: false, as_value: false).is_valid)
		{
			Game.Log(field.Path(include_file: true) + ": Empty query value", Game.LogType.Error);
		}
		else
		{
			ResolveType(text);
		}
	}

	public Value GetValue(IVars vars)
	{
		if (!is_valid)
		{
			return Value.Unknown;
		}
		ClearTempValues();
		tmp_vars = vars;
		Value value = GetValue();
		ClearTempValues();
		return value;
	}

	private void ClearTempValues()
	{
		tmp_vars = null;
		tmp_results.Clear();
		tmp_count = 0;
		tmp_best = Value.Unknown;
		tmp_best_eval = 0f;
		tmp_weights.Clear();
		tmp_total_weights = 0f;
	}

	private Value GetValue()
	{
		if (mode == Mode.Var)
		{
			return field.Value(this);
		}
		if (func == null)
		{
			return Value.Unknown;
		}
		func(this);
		switch (mode)
		{
		case Mode.Random:
		{
			if (tmp_total_weights <= 0f || tmp_results.Count == 0)
			{
				return Value.Unknown;
			}
			float num = game.Random(0f, tmp_total_weights);
			for (int i = 0; i < tmp_results.Count; i++)
			{
				Value result = tmp_results[i];
				float num2 = tmp_weights[i];
				if (num2 >= num)
				{
					return result;
				}
				num -= num2;
			}
			int index = game.Random(0, tmp_results.Count);
			return tmp_results[index];
		}
		case Mode.First:
			if (tmp_results.Count == 0)
			{
				return Value.Unknown;
			}
			return tmp_results[0];
		case Mode.List:
			return new Value(new List<Value>(tmp_results));
		case Mode.Count:
			return tmp_count;
		case Mode.Best:
			return tmp_best;
		default:
			Game.Log($"Unsupported query mode: {mode}", Game.LogType.Error);
			return Value.Unknown;
		}
	}

	public bool AddResult(Value val)
	{
		tmp_val = val;
		float num = field.Float(this);
		tmp_val = Value.Unknown;
		if (num <= 0f)
		{
			return true;
		}
		switch (mode)
		{
		case Mode.Random:
			tmp_results.Add(val);
			tmp_weights.Add(num);
			tmp_total_weights += num;
			return true;
		case Mode.First:
			tmp_results.Add(val);
			return false;
		case Mode.List:
			tmp_results.Add(val);
			return true;
		case Mode.Count:
			tmp_count++;
			return true;
		case Mode.Best:
			if (num <= tmp_best_eval)
			{
				return true;
			}
			tmp_best = val;
			tmp_best_eval = num;
			return true;
		default:
			Game.Log($"Unsupported query mode: {mode}", Game.LogType.Error);
			return false;
		}
	}

	private static bool IsGenFunc(MethodInfo method)
	{
		if (method.ReturnType != typeof(void))
		{
			return false;
		}
		ParameterInfo[] parameters = method.GetParameters();
		if (parameters.Length != 1)
		{
			return false;
		}
		if (parameters[0].ParameterType != typeof(Query))
		{
			return false;
		}
		return true;
	}

	private static void FillGenFuncs()
	{
		if (gen_funcs != null)
		{
			return;
		}
		gen_funcs = new Dictionary<string, GenFunc>();
		MethodInfo[] methods = typeof(Query).GetMethods(BindingFlags.Static | BindingFlags.Public);
		foreach (MethodInfo methodInfo in methods)
		{
			if (IsGenFunc(methodInfo))
			{
				GenFunc value;
				try
				{
					value = (GenFunc)Delegate.CreateDelegate(typeof(GenFunc), methodInfo);
				}
				catch (Exception ex)
				{
					Game.Log("Error creating delegate for " + methodInfo.ToString() + ": " + ex.ToString(), Game.LogType.Error);
					continue;
				}
				gen_funcs.Add(methodInfo.Name, value);
			}
		}
	}

	private bool ExtractPrefix(ref string s, string prefix)
	{
		if (!s.StartsWith(prefix, StringComparison.Ordinal))
		{
			return false;
		}
		s = s.Substring(prefix.Length);
		return true;
	}

	private bool ExtractSuffix(ref string s, string suffix)
	{
		if (!s.EndsWith(suffix, StringComparison.Ordinal))
		{
			return false;
		}
		s = s.Substring(0, s.Length - suffix.Length);
		return true;
	}

	private bool ResolveType(string type)
	{
		if (ExtractSuffix(ref type, "?"))
		{
			optional = true;
		}
		if (type == "var")
		{
			mode = Mode.Var;
			return true;
		}
		if (ExtractPrefix(ref type, "random_"))
		{
			mode = Mode.Random;
		}
		else if (ExtractPrefix(ref type, "first_"))
		{
			mode = Mode.First;
		}
		else if (ExtractSuffix(ref type, "_list"))
		{
			mode = Mode.List;
		}
		else if (ExtractSuffix(ref type, "_count"))
		{
			mode = Mode.Count;
		}
		else
		{
			if (!ExtractPrefix(ref type, "best_"))
			{
				Game.Log(field.Path(include_file: true) + ": Unknown query mode: '" + type + "'", Game.LogType.Error);
				mode = Mode.Invalid;
				return false;
			}
			mode = Mode.Best;
		}
		tid = Serialization.GetTID(type);
		if (tid != Serialization.ObjectType.COUNT)
		{
			func = ObjectType;
			return true;
		}
		FillGenFuncs();
		if (!gen_funcs.TryGetValue(type, out func))
		{
			Game.Log(field.Path(include_file: true) + ": Unknown query type: '" + type + "'", Game.LogType.Error);
			mode = Mode.Invalid;
			return false;
		}
		return true;
	}

	public static List<Query> LoadAll(Game game, DT.Field f)
	{
		List<DT.Field> list = f?.Children();
		if (list == null)
		{
			return null;
		}
		List<Query> list2 = null;
		for (int i = 0; i < list.Count; i++)
		{
			DT.Field field = list[i];
			if (string.IsNullOrEmpty(field.key))
			{
				continue;
			}
			Query query = new Query(game, field);
			if (query.is_valid)
			{
				if (list2 == null)
				{
					list2 = new List<Query>(list.Count);
				}
				list2.Add(query);
			}
		}
		return list2;
	}

	public static bool FillVars(Vars out_vars, List<Query> queries, IVars vars, bool stop_on_invalid = true)
	{
		if (queries == null)
		{
			return true;
		}
		tmp_out_vars = out_vars;
		bool result = true;
		for (int i = 0; i < queries.Count; i++)
		{
			Query query = queries[i];
			Value value = query.GetValue(vars);
			if (!value.is_valid && !query.optional)
			{
				result = false;
				if (stop_on_invalid)
				{
					break;
				}
			}
			out_vars.Set(query.field.key, value);
		}
		tmp_out_vars = null;
		return result;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == this.field.key)
		{
			return tmp_val;
		}
		switch (key)
		{
		case "obj":
			return tmp_val;
		case "field":
			return new Value(this.field);
		case "vars":
			return new Value(tmp_vars);
		default:
		{
			DT.Field field = this.field?.FindChild(key, this);
			if (field != null)
			{
				if (!as_value)
				{
					return new Value(field);
				}
				Value result = field.Value(vars ?? this, calc_expression: true, as_value);
				if (!result.is_unknown)
				{
					return result;
				}
			}
			IVars vars2 = tmp_val.Get<IVars>();
			if (vars2 != null)
			{
				Value result = vars2.GetVar(key, vars ?? this, as_value);
				if (!result.is_unknown)
				{
					return result;
				}
			}
			if (tmp_out_vars != null)
			{
				Value result = tmp_out_vars.GetVar(key, vars, as_value);
				if (!result.is_unknown)
				{
					return result;
				}
			}
			if (tmp_vars != null)
			{
				Value result = tmp_vars.GetVar(key, vars, as_value);
				if (!result.is_unknown)
				{
					return result;
				}
			}
			return Value.Unknown;
		}
		}
	}

	public Value GetParamValue(params string[] keys)
	{
		foreach (string key in keys)
		{
			Value var = GetVar(key);
			if (!var.is_unknown)
			{
				return var;
			}
		}
		return Value.Unknown;
	}

	public bool GetBool(params string[] keys)
	{
		return GetParamValue(keys).Bool();
	}

	public float GetInt(params string[] keys)
	{
		return GetParamValue(keys).Int();
	}

	public float GetFloat(params string[] keys)
	{
		return GetParamValue(keys).Float();
	}

	public string GetString(params string[] keys)
	{
		return GetParamValue(keys).String();
	}

	public T GetObj<T>(params string[] keys) where T : class
	{
		return GetParamValue(keys).Get<T>();
	}

	public override string ToString()
	{
		if (field == null)
		{
			return "Query(null)";
		}
		return "Query(" + field.Type() + " " + field.key + " = '" + field.ValueStr() + "')";
	}

	public static void ObjectType(Query q)
	{
		if (!q.game.multiplayer.objects.registries.TryGetValue(q.tid, out var value))
		{
			return;
		}
		foreach (KeyValuePair<int, Object> @object in value.objects)
		{
			Object value2 = @object.Value;
			q.AddResult(value2);
		}
	}

	public Kingdom GetKingdom()
	{
		return GetObj<Kingdom>(new string[3] { "kingdom", "tgt_kingdom", "src_kingdom" });
	}

	public Object GetOwner()
	{
		return GetObj<Object>(new string[1] { "owner" });
	}

	public static void realm(Query q)
	{
		if (q.GetString("kingdom") == "any")
		{
			for (int i = 0; i < q.game.landRealmsCount; i++)
			{
				Realm realm = q.game.realms[i];
				if (!q.AddResult(realm))
				{
					break;
				}
			}
			return;
		}
		Kingdom kingdom = q.GetKingdom();
		if (kingdom == null)
		{
			return;
		}
		for (int j = 0; j < kingdom.realms.Count; j++)
		{
			Realm realm2 = kingdom.realms[j];
			if (!q.AddResult(realm2))
			{
				break;
			}
		}
	}

	public static void court_member(Query q)
	{
		Kingdom kingdom = q.GetKingdom();
		if (kingdom == null)
		{
			return;
		}
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character != null && !q.AddResult(character))
			{
				break;
			}
		}
	}

	public static void royal_child(Query q)
	{
		Kingdom kingdom = q.GetKingdom();
		if (kingdom == null || kingdom.royalFamily == null)
		{
			return;
		}
		for (int i = 0; i < kingdom.royalFamily.Children.Count; i++)
		{
			Character character = kingdom.royalFamily.Children[i];
			if (character != null && !q.AddResult(character))
			{
				break;
			}
		}
	}

	public static void royal_member(Query q)
	{
		Kingdom kingdom = q.GetKingdom();
		if (kingdom == null || kingdom.royalFamily == null || (kingdom.royalFamily.Sovereign != null && !q.AddResult(kingdom.royalFamily.Sovereign)) || (kingdom.royalFamily.Spouse != null && !q.AddResult(kingdom.royalFamily.Spouse)))
		{
			return;
		}
		for (int i = 0; i < kingdom.royalFamily.Children.Count; i++)
		{
			Character character = kingdom.royalFamily.Children[i];
			if (character != null && !q.AddResult(character))
			{
				break;
			}
		}
	}

	public static void army(Query q)
	{
		Kingdom kingdom = q.GetKingdom();
		if (kingdom == null)
		{
			return;
		}
		for (int i = 0; i < kingdom.armies.Count; i++)
		{
			Army army = kingdom.armies[i];
			if (army != null && !q.AddResult(army))
			{
				break;
			}
		}
	}

	public static void rebellion(Query q)
	{
		Kingdom k = q.GetKingdom();
		if (k == null)
		{
			return;
		}
		int num = (int)q.GetInt("distance");
		if (num < 1)
		{
			num = -1;
		}
		q.game.RealmWave(k.realms, num, delegate(Realm r, List<Realm> rStart, int depth, object param, ref bool push_neighbors, ref bool stop)
		{
			if (r.kingdom_id != k.id)
			{
				for (int i = 0; i < r.rebellions.Count; i++)
				{
					q.AddResult(r.rebellions[i]);
				}
			}
		}, false, use_logic_neighbors: true);
	}

	public static void kingdom(Query q)
	{
		List<Kingdom> list = q.game?.kingdoms;
		if (list == null)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Kingdom kingdom = list[i];
			if (kingdom != null && !kingdom.IsDefeated() && !q.AddResult(kingdom))
			{
				break;
			}
		}
	}

	public static void Test(Game game)
	{
		DT.Field field = game.dt.Find("TestQueries");
		if (field != null)
		{
			List<Query> list = LoadAll(game, field);
			Vars vars = new Vars();
			vars.Set("kingdom", game.GetKingdom());
			Vars vars2 = new Vars();
			string dump = string.Format(arg1: FillVars(vars2, list, vars, stop_on_invalid: false) ? "OK" : "FAILED", format: "Queries: {0}, {1}:", arg0: list.Count);
			vars2.EnumerateAll(delegate(string key, Value val)
			{
				dump = dump + "\n-- " + key + ": " + Object.Dump(val);
			});
			Game.Log(dump, Game.LogType.Message);
		}
	}
}
