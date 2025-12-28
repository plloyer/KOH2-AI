using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Logic;

public class Vars : BaseObject, IVars, ISetVar
{
	public class FullData : Data
	{
		private struct VarData
		{
			public string key;

			public Data data;

			public VarData(string key, Data data)
			{
				this.key = key;
				this.data = data;
			}

			public VarData(string key, Value value)
			{
				this.key = key;
				bool flag = value.IsSerializable();
				if (!flag)
				{
					Game.Log("Creating data for non-serializable value: " + key + ": " + value.ToString(), Game.LogType.Warning);
				}
				data = value.CreateData();
				if (data == null && flag)
				{
					Game.Log("Failed to create data for value " + key + ": " + value.ToString(), Game.LogType.Warning);
				}
			}

			public override string ToString()
			{
				return key + ": " + (data?.ToString() ?? "null");
			}
		}

		private List<VarData> data;

		public static FullData Create()
		{
			return new FullData();
		}

		public override string ToString()
		{
			return base.ToString() + "(" + ((data == null) ? "null" : data.Count.ToString()) + ")";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Vars vars))
			{
				return false;
			}
			int num = 0;
			if (!vars.obj.is_unknown)
			{
				num++;
			}
			if (vars.var2_name != null)
			{
				num++;
			}
			if (vars.dict != null)
			{
				num += vars.dict.Count;
			}
			data = new List<VarData>(num);
			if (!vars.obj.is_unknown)
			{
				data.Add(new VarData("obj", vars.obj));
			}
			if (vars.var2_name != null)
			{
				data.Add(new VarData(vars.var2_name, vars.var2));
			}
			if (vars.dict == null)
			{
				return true;
			}
			foreach (KeyValuePair<string, Value> item in vars.dict)
			{
				string key = item.Key;
				Value value = item.Value;
				data.Add(new VarData(key, value));
			}
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			int num = ((data != null) ? data.Count : 0);
			ser.Write7BitUInt(num, "count");
			for (int i = 0; i < num; i++)
			{
				VarData varData = data[i];
				ser.WriteStr(varData.key, "key", i);
				ser.WriteData(varData.data, "value", i);
			}
		}

		public override void Load(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				this.data = new List<VarData>(num);
				for (int i = 0; i < num; i++)
				{
					string key = ser.ReadStr("key", i);
					Data data = ser.ReadData("value", i);
					this.data.Add(new VarData(key, data));
				}
			}
		}

		public override object GetObject(Game game)
		{
			return new Vars();
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Vars vars))
			{
				return false;
			}
			if (data == null)
			{
				return true;
			}
			for (int i = 0; i < data.Count; i++)
			{
				VarData varData = data[i];
				Value val = ((varData.data == null) ? Value.Null : varData.data.GetValue(game));
				vars.Set(varData.key, val);
			}
			return true;
		}
	}

	public delegate object Func0();

	public delegate object Func1(string key);

	public delegate object Func2(IVars vars, string key);

	public enum ReflectionMode
	{
		Disabled,
		Log,
		Enabled
	}

	private struct GEStackItem
	{
		public object obj;

		public string key;

		public override string ToString()
		{
			return $"'{key}' from {obj}";
		}
	}

	public Value obj = Value.Unknown;

	private string var2_name;

	private Value var2 = Value.Unknown;

	private Dictionary<string, Value> dict;

	public static Type[] NoParams = new Type[0];

	public static Type[] GetNameKeyParams = new Type[2]
	{
		typeof(IVars),
		typeof(string)
	};

	private static ReflectionMode reflection_mode = ReflectionMode.Disabled;

	private static List<GEStackItem> GE_stack = new List<GEStackItem>(128);

	private static List<Vars> dump_stack = new List<Vars>();

	public override bool IsFullSerializable()
	{
		if (!obj.is_unknown && !obj.IsSerializable())
		{
			return false;
		}
		if (var2_name != null && !var2.IsSerializable())
		{
			return false;
		}
		if (dict == null)
		{
			return true;
		}
		foreach (KeyValuePair<string, Value> item in dict)
		{
			if (!item.Value.IsSerializable())
			{
				return false;
			}
		}
		return true;
	}

	public static DT.Field TestSerialization(object obj, Game game)
	{
		Data data = Data.Create(obj);
		DTSerializeWriter dTSerializeWriter = new DTSerializeWriter(new UniqueStrings());
		dTSerializeWriter.SetRootKey("test");
		dTSerializeWriter.WriteData(data, "obj");
		object obj2 = new DTSerializeReader(game, dTSerializeWriter.GetRoot()).ReadData("obj").GetValue(game).Object();
		Game.Log((obj2 as Vars)?.Dump() ?? obj2?.ToString(), Game.LogType.Message);
		return dTSerializeWriter.GetRoot();
	}

	public Vars()
	{
	}

	public Vars(Value obj)
	{
		this.obj = obj;
	}

	public Vars(object obj)
	{
		this.obj = new Value(obj);
	}

	public bool ContainsKey(string key)
	{
		if (key == "obj")
		{
			return true;
		}
		if (key == var2_name)
		{
			return true;
		}
		if (dict == null)
		{
			return false;
		}
		return dict.ContainsKey(key);
	}

	public Value GetRaw(string key)
	{
		if (key == "obj")
		{
			return obj;
		}
		if (key == var2_name)
		{
			return var2;
		}
		if (dict == null)
		{
			return Value.Unknown;
		}
		if (dict.TryGetValue(key, out var value))
		{
			return value;
		}
		return Value.Unknown;
	}

	public void Set<T>(string key, T val)
	{
		if (key == "obj")
		{
			obj = new Value(val);
			return;
		}
		if (key == var2_name || var2_name == null)
		{
			var2_name = key;
			var2 = new Value(val);
			return;
		}
		if (dict == null)
		{
			dict = new Dictionary<string, Value>();
		}
		dict[key] = new Value(val);
	}

	public void Del(string key)
	{
		if (key == "obj")
		{
			obj = Value.Unknown;
		}
		else if (key == var2_name)
		{
			if (dict == null || dict.Count == 0)
			{
				var2_name = null;
				var2 = Value.Unknown;
				return;
			}
			Dictionary<string, Value>.Enumerator enumerator = dict.GetEnumerator();
			enumerator.MoveNext();
			KeyValuePair<string, Value> current = enumerator.Current;
			var2_name = current.Key;
			var2 = current.Value;
			dict.Remove(var2_name);
		}
		else if (dict != null)
		{
			dict.Remove(key);
		}
	}

	public bool Empty()
	{
		if (obj.is_unknown && (var2_name == null || var2.is_unknown))
		{
			if (dict != null)
			{
				return dict.Count == 0;
			}
			return true;
		}
		return false;
	}

	public int Count()
	{
		int num = 0;
		if (!obj.is_unknown)
		{
			num++;
		}
		if (var2_name != null && !var2.is_unknown)
		{
			num++;
		}
		if (dict != null)
		{
			num += dict.Count;
		}
		return num;
	}

	public void Clear()
	{
		obj = Value.Unknown;
		var2_name = null;
		var2 = Value.Unknown;
		if (dict != null)
		{
			dict.Clear();
		}
	}

	public static bool ResolveFunc(out Value result, Value val, string key, IVars vars = null)
	{
		if (!val.is_object)
		{
			result = val;
			return false;
		}
		if (val.obj_val is Func0 func)
		{
			try
			{
				result = new Value(func());
			}
			catch (Exception ex)
			{
				Game.Log("Exception in ResolveFunc('" + key + "'): " + ex.ToString(), Game.LogType.Error);
				result = Value.Null;
			}
			return true;
		}
		if (val.obj_val is Func1 func2)
		{
			try
			{
				result = new Value(func2(key));
			}
			catch (Exception ex2)
			{
				Game.Log("Exception in ResolveFunc('" + key + "'): " + ex2.ToString(), Game.LogType.Error);
				result = Value.Null;
			}
			return true;
		}
		if (val.obj_val is Func2 func3)
		{
			try
			{
				result = new Value(func3(vars, key));
			}
			catch (Exception ex3)
			{
				Game.Log("Exception in ResolveFunc('" + key + "'): " + ex3.ToString(), Game.LogType.Error);
				result = Value.Null;
			}
			return true;
		}
		result = val;
		return false;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Value result = GetRaw(key);
		if (!result.is_unknown)
		{
			ResolveFunc(out result, result, key, this);
			if (key == "obj" && result.is_object)
			{
				Value exact = GetExact(result.obj_val, key, this, as_value);
				if (!exact.is_unknown)
				{
					return exact;
				}
			}
			return result;
		}
		if (obj.is_object)
		{
			result = GetExact(obj.obj_val, key, this, as_value);
			if (!result.is_unknown)
			{
				ResolveFunc(out result, result, key, this);
				return result;
			}
		}
		object obj_val = GetRaw("vars").obj_val;
		if (obj_val != null)
		{
			result = GetExact(obj_val, key, this, as_value);
			if (!result.is_unknown)
			{
				ResolveFunc(out result, result, key, this);
				return result;
			}
		}
		return Value.Unknown;
	}

	public void SetVar(string key, Value value)
	{
		if (value.is_unknown)
		{
			Del(key);
		}
		else
		{
			Set(key, value);
		}
	}

	public Value Get(string path, bool as_value = true)
	{
		int num = path.IndexOf('.');
		Value result;
		if (num < 0)
		{
			result = GetRaw(path);
			if (result != Value.Unknown)
			{
				ResolveFunc(out result, result, path, this);
				if (path == "obj" && result.is_object)
				{
					Value exact = GetExact(result.obj_val, path, this, as_value);
					if (!exact.is_unknown)
					{
						return exact;
					}
				}
				return result;
			}
			if (obj.is_object)
			{
				return GetExact(obj.obj_val, path, this, as_value);
			}
			return Value.Unknown;
		}
		if (num == 0)
		{
			return Value.Unknown;
		}
		string key = path.Substring(0, num);
		string path2 = path.Substring(num + 1);
		result = GetRaw(key);
		if (result.is_object)
		{
			return Get(result.obj_val, path2, this, as_value);
		}
		if (obj.is_object)
		{
			return Get(obj.obj_val, path, this, as_value);
		}
		return Value.Unknown;
	}

	public T Get<T>(string key, T def_val = default(T))
	{
		Value value = Get(key);
		if (!value.is_valid)
		{
			return def_val;
		}
		if (typeof(T) == typeof(bool))
		{
			return (T)(object)value.Bool();
		}
		object obj = value.Object();
		if (!typeof(T).IsAssignableFrom(obj.GetType()))
		{
			return def_val;
		}
		return (T)obj;
	}

	public static T Get<T>(IVars vars, string key, T def_val = null) where T : class
	{
		if (vars == null)
		{
			return def_val;
		}
		Value var = vars.GetVar(key);
		if (!var.is_valid)
		{
			return def_val;
		}
		Type c = var.CSType();
		if (!typeof(T).IsAssignableFrom(c))
		{
			return def_val;
		}
		return (T)var.Object();
	}

	public void EnumerateAll(Action<string, Value> action)
	{
		if (!obj.is_unknown)
		{
			action("obj", obj);
		}
		if (var2_name != null)
		{
			action(var2_name, var2);
		}
		if (dict == null)
		{
			return;
		}
		foreach (KeyValuePair<string, Value> item in dict)
		{
			string key = item.Key;
			Value value = item.Value;
			action(key, value);
		}
	}

	public static bool ReflectionEnabled()
	{
		return reflection_mode != ReflectionMode.Disabled;
	}

	public static ReflectionMode GetReflectionMode()
	{
		return reflection_mode;
	}

	public static ReflectionMode PushReflectionMode(ReflectionMode new_mode)
	{
		ReflectionMode result = reflection_mode;
		reflection_mode = new_mode;
		return result;
	}

	public static void PopReflectionMode(ReflectionMode old_mode)
	{
		reflection_mode = old_mode;
	}

	public static Value ResolveValue(Value val, IVars vars, bool as_value)
	{
		if (!as_value)
		{
			return val;
		}
		if (val.obj_val is DT.Field field)
		{
			return field.Value(vars);
		}
		if (val.obj_val is DT.SubValue subValue)
		{
			return subValue.value;
		}
		return val;
	}

	public static Value ReflectedValue(object obj, string key, Value value, IVars vars, bool as_value)
	{
		if (reflection_mode == ReflectionMode.Log)
		{
			Game.Log("Reflection: " + obj.GetType().FullName + "." + key + " = " + value.ToString(), Game.LogType.Warning);
		}
		if (!value.is_valid)
		{
			return value;
		}
		return ResolveValue(value, vars, as_value);
	}

	private static bool PushGE(object obj, string key)
	{
		for (int i = 0; i < GE_stack.Count; i++)
		{
			GEStackItem gEStackItem = GE_stack[i];
			if (gEStackItem.obj == obj && gEStackItem.key == key)
			{
				string text = $"Infinite loop while getting var '{key}' from {obj}:";
				for (int num = GE_stack.Count - 1; num > i; num--)
				{
					GEStackItem gEStackItem2 = GE_stack[num];
					text += $"\n  '{gEStackItem2.key}' from {gEStackItem2.obj}";
				}
				Game.Log(text, Game.LogType.Error);
				return false;
			}
		}
		GE_stack.Add(new GEStackItem
		{
			obj = obj,
			key = key
		});
		return true;
	}

	private static void PopGE()
	{
		GE_stack.RemoveAt(GE_stack.Count - 1);
	}

	public static Value GetExact(object obj, string key, IVars vars = null, bool as_value = true)
	{
		if (!PushGE(obj, key))
		{
			return Value.Unknown;
		}
		Value result;
		try
		{
			result = GetExactImpl(obj, key, vars, as_value);
		}
		catch (Exception ex)
		{
			Game.Log($"Error evaluating value '{key}' from object '{obj}', vars: '{vars}': {ex}", Game.LogType.Error);
			result = Value.Unknown;
		}
		PopGE();
		return result;
	}

	private static Value GetExactImpl(object obj, string key, IVars vars = null, bool as_value = true)
	{
		if (obj == null)
		{
			return Value.Null;
		}
		Value result;
		if (obj is IVars vars2)
		{
			result = vars2.GetVar(key, vars, as_value);
			if (!result.is_unknown)
			{
				return result;
			}
		}
		if (reflection_mode == ReflectionMode.Disabled)
		{
			return Value.Unknown;
		}
		using (Game.Profile("GetVar with reflection"))
		{
			if (obj is DT.Field)
			{
				return Value.Unknown;
			}
			if (ResolveFunc(out result, new Value(obj), key, vars))
			{
				return ReflectedValue(obj, key, result, vars, as_value);
			}
			if (obj is IList list)
			{
				switch (key)
				{
				case "Count":
				case "count":
					return list.Count;
				case "Empty":
				case "empty":
					return list.Count == 0;
				case "SingleItem":
				case "single_item":
					return list.Count == 1;
				case "MultipleItems":
				case "multiple_items":
					return list.Count > 1;
				default:
				{
					if (!int.TryParse(key, out var result2))
					{
						return Value.Unknown;
					}
					if (result2 >= list.Count)
					{
						return Value.Unknown;
					}
					try
					{
						return ReflectedValue(obj, key, new Value(list[result2]), vars, as_value);
					}
					catch (Exception ex)
					{
						Game.Log("Exception in GetExact('" + key + "'): " + ex.ToString(), Game.LogType.Error);
						return Value.Unknown;
					}
				}
				}
			}
			if (obj is IDictionary dictionary)
			{
				try
				{
					if (dictionary.Contains(key))
					{
						return ReflectedValue(obj, key, new Value(dictionary[key]), vars, as_value);
					}
					return Value.Unknown;
				}
				catch (Exception ex2)
				{
					Game.Log("Exception in GetExact('" + key + "'): " + ex2.ToString(), Game.LogType.Error);
					return Value.Unknown;
				}
			}
			Reflection.TypeInfo typeInfo = Reflection.GetTypeInfo(obj.GetType());
			Reflection.VarInfo varInfo = typeInfo.GetVarInfo(key);
			if (varInfo.is_valid)
			{
				result = varInfo.Extract(obj);
				if (!result.is_unknown)
				{
					return ReflectedValue(obj, key, result, vars, as_value);
				}
			}
			if (typeInfo.stats.is_valid)
			{
				object obj_val = typeInfo.stats.Extract(obj).obj_val;
				if (obj_val != null)
				{
					result = GetExact(obj_val, key, vars, as_value);
					if (!result.is_unknown)
					{
						return ReflectedValue(obj, key, result, vars, as_value);
					}
				}
			}
			if (typeInfo.def.is_valid)
			{
				object obj_val2 = typeInfo.def.Extract(obj).obj_val;
				if (obj_val2 != null)
				{
					result = GetExact(obj_val2, key, vars, as_value);
					if (!result.is_unknown)
					{
						return ReflectedValue(obj, key, result, vars, as_value);
					}
				}
			}
			if (typeInfo.field.is_valid)
			{
				object obj_val3 = typeInfo.field.Extract(obj).obj_val;
				if (obj_val3 != null)
				{
					result = GetExact(obj_val3, key, vars, as_value);
					if (!result.is_unknown)
					{
						return ReflectedValue(obj, key, result, vars, as_value);
					}
				}
			}
		}
		return Value.Unknown;
	}

	public static Value Get(object obj, string path, IVars vars = null, bool as_value = true)
	{
		while (true)
		{
			if (obj == null)
			{
				return Value.Unknown;
			}
			int num = path.IndexOf('.');
			if (num < 0)
			{
				return GetExact(obj, path, vars, as_value);
			}
			if (num == 0)
			{
				break;
			}
			string key = path.Substring(0, num);
			path = path.Substring(num + 1);
			obj = GetExact(obj, key, vars, as_value: false).obj_val;
		}
		return Value.Null;
	}

	public static Value Get(string path, IVars vars, bool as_value = true)
	{
		if (vars == null)
		{
			return Value.Unknown;
		}
		int num = path.IndexOf('.');
		if (num < 0)
		{
			return GetExact(vars, path, null, as_value);
		}
		if (num == 0)
		{
			return Value.Null;
		}
		string key = path.Substring(0, num);
		path = path.Substring(num + 1);
		Value exact = GetExact(vars, key, null, as_value: false);
		if (!exact.is_object)
		{
			return Value.Null;
		}
		return Get(exact.obj_val, path, vars, as_value);
	}

	public Vars Copy()
	{
		Vars vars = new Vars(obj);
		vars.var2_name = var2_name;
		vars.var2 = var2;
		if (dict != null)
		{
			vars.dict = new Dictionary<string, Value>(dict.Count);
			foreach (KeyValuePair<string, Value> item in dict)
			{
				string key = item.Key;
				Value value = item.Value;
				vars.dict.Add(key, value);
			}
		}
		return vars;
	}

	public void Modify(Vars vars)
	{
		vars?.EnumerateAll(delegate(string key, Value val)
		{
			Set(key, val);
		});
	}

	private static void AddVarField(List<DT.Field> lst, string key, Value value, DT.Field parent = null)
	{
		if (!value.is_unknown && key != null)
		{
			DT.Field field = new DT.Field(parent?.dt);
			field.key = key;
			if (value.obj_val is Vars vars)
			{
				vars.ToDT(field);
			}
			else
			{
				field.value = value;
			}
			parent?.AddChild(field);
			lst.Add(field);
		}
	}

	public List<DT.Field> ToDT(DT.Field parent = null)
	{
		int capacity = Count();
		if (parent != null && parent.children == null)
		{
			parent.children = new List<DT.Field>(capacity);
		}
		List<DT.Field> list = new List<DT.Field>(capacity);
		AddVarField(list, "obj", obj, parent);
		AddVarField(list, var2_name, var2, parent);
		if (dict != null)
		{
			foreach (KeyValuePair<string, Value> item in dict)
			{
				string key = item.Key;
				Value value = item.Value;
				AddVarField(list, key, value, parent);
			}
		}
		return list;
	}

	public static Vars FromDT(List<DT.Field> fields)
	{
		Vars vars = new Vars();
		if (fields == null)
		{
			return vars;
		}
		for (int i = 0; i < fields.Count; i++)
		{
			DT.Field field = fields[i];
			if (!string.IsNullOrEmpty(field.key))
			{
				if (field.children != null)
				{
					Vars val = FromDT(field);
					vars.Set(field.key, val);
				}
				else
				{
					vars.Set(field.key, field.value);
				}
			}
		}
		return vars;
	}

	public static Vars FromDT(DT.Field parent)
	{
		if (parent == null)
		{
			return null;
		}
		return FromDT(parent.children);
	}

	public string ToDTString()
	{
		List<DT.Field> list = ToDT();
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < list.Count; i++)
		{
			list[i].GetSaveString(stringBuilder);
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString();
	}

	public static Vars FromDTString(string dt_str)
	{
		return FromDT(new DT.Parser(null, dt_str).ReadFields(null));
	}

	public void SaveBinary(ref MemStream writer, UniqueStrings unique_strings = null, bool add_new_strings = true)
	{
		DT.Field.SaveBinaryFields(ToDT(), ref writer, unique_strings, add_new_strings);
	}

	public static Vars LoadBinary(ref MemStream reader, UniqueStrings unique_strings = null)
	{
		return FromDT(DT.Field.LoadBinaryFields(ref reader, unique_strings));
	}

	public string Dump(string prefix, string new_line, bool deep = false)
	{
		dump_stack.Add(this);
		string text = "";
		if (!obj.is_unknown)
		{
			text = text + "obj: " + obj.ToString();
		}
		if (var2_name != null)
		{
			if (text != "")
			{
				text += new_line;
			}
			text = text + var2_name + ": " + var2.ToString();
		}
		if (dict != null)
		{
			foreach (KeyValuePair<string, Value> item in dict)
			{
				string key = item.Key;
				Value value = item.Value;
				if (text != "")
				{
					text += new_line;
				}
				text = ((!deep || !(value.obj_val is Vars vars) || dump_stack.Contains(vars)) ? (text + key + ": " + value.ToString()) : (text + key + ":" + new_line + vars.Dump("  ", new_line + "  ", deep: true)));
			}
		}
		dump_stack.Remove(this);
		if (text == "")
		{
			return text;
		}
		return prefix + text;
	}

	public string Dump()
	{
		return ToString() + "\n" + Dump("", "\n");
	}

	public string DebugText()
	{
		return Dump("@", "{p}");
	}

	public override string ToString()
	{
		string text = "";
		if (!obj.is_unknown)
		{
			text = text + "obj: " + obj.ToString();
		}
		if (var2_name != null)
		{
			if (text != "")
			{
				text += ", ";
			}
			text = text + var2_name + ": " + var2.ToString();
		}
		text = "Vars(" + text + ")";
		if (dict != null)
		{
			text = text + "[" + dict.Count + "]";
		}
		return text;
	}
}
