using System;
using System.Collections;
using System.Reflection;

namespace Logic;

public class DefsContext : Expression.Context
{
	public DT dt;

	public Game game;

	public DT.Field field;

	public IVars vars;

	public static DefsContext temp_context = new DefsContext();

	public Value Call(object obj, string func_name, object[] args, bool as_value = true)
	{
		Value result = Value.Unknown;
		if (obj == null)
		{
			if (game != null && Reflection.Call(game, func_name, args, out result))
			{
				return Vars.ResolveValue(new Value(result), this.vars, as_value);
			}
			if (Reflection.Call(typeof(Game), func_name, args, out result))
			{
				return Vars.ResolveValue(new Value(result), this.vars, as_value);
			}
			if (args.Length == 1 && args[0] is string && game.vars.GetRaw(func_name).obj_val is Vars.Func1 func)
			{
				result = new Value(func((string)args[0]));
				return Vars.ResolveValue(new Value(result), this.vars, as_value);
			}
			if (this.vars == null)
			{
				return Value.Unknown;
			}
			obj = ((!(this.vars is Vars vars) || !vars.obj.is_object) ? this.vars : vars.obj.obj_val);
		}
		if (Reflection.Call(obj, func_name, args, out result))
		{
			return Vars.ResolveValue(new Value(result), this.vars, as_value);
		}
		return Value.Unknown;
	}

	public Value GetByIndex(object obj, object[] indicies, bool as_value = true)
	{
		try
		{
			if (obj == null)
			{
				return Value.Unknown;
			}
			Type type = obj.GetType();
			if (indicies.Length != 1)
			{
				MethodInfo method = type.GetMethod("get_Item");
				if (method != null)
				{
					object val = method.Invoke(obj, indicies);
					return Vars.ReflectedValue(obj, "indicies", new Value(val), vars, as_value);
				}
				return Value.Unknown;
			}
			if (indicies[0] == null)
			{
				return Value.Unknown;
			}
			bool flag = indicies[0] is int;
			int num = (flag ? ((int)indicies[0]) : (-1));
			if (obj is DT.Field field)
			{
				if (flag)
				{
					if (num < 0)
					{
						return Value.Unknown;
					}
					int num2 = field.NumValues();
					if (num >= num2)
					{
						return Value.Unknown;
					}
					return field.Value(num, vars, calc_expression: true, as_value);
				}
				return field.GetVar(indicies[0].ToString(), vars, as_value);
			}
			if (obj is IList list)
			{
				if (!flag || num < 0 || num >= list.Count)
				{
					return Value.Unknown;
				}
				return Vars.ResolveValue(new Value(list[num]), vars, as_value);
			}
			if (obj is IDictionary dictionary)
			{
				object key = indicies[0];
				if (dictionary.Contains(key))
				{
					return Vars.ResolveValue(new Value(dictionary[key]), vars, as_value);
				}
				return Value.Unknown;
			}
			if (obj is string text)
			{
				return text[Convert.ToInt32(indicies[0])].ToString();
			}
			if (Vars.ReflectionEnabled())
			{
				MethodInfo method2 = type.GetMethod("GetValue", new Type[1] { typeof(int) });
				if (method2 != null)
				{
					return Vars.ResolveValue(new Value(method2.Invoke(obj, indicies)), vars, as_value);
				}
			}
			return GetVar(obj, indicies[0].ToString(), as_value);
		}
		catch (Exception ex)
		{
			Game.Log("Exception in Defs.Context.GetByIndex(): " + ex.ToString(), Game.LogType.Error);
			return Value.Unknown;
		}
	}

	public Value GetVarPath(string path, bool as_value = true)
	{
		object obj = null;
		int num = 0;
		while (true)
		{
			int num2 = path.IndexOf('.', num);
			string var_name;
			if (num2 < 0)
			{
				var_name = ((num <= 0) ? path : path.Substring(num));
				return GetVar(obj, var_name, as_value);
			}
			var_name = path.Substring(num, num2 - num);
			obj = GetVar(obj, var_name, as_value: false).obj_val;
			if (obj == null)
			{
				break;
			}
			num = num2 + 1;
		}
		return Value.Unknown;
	}

	public Value GetVar(object obj, string var_name, bool as_value = true)
	{
		if (obj != null)
		{
			return Vars.GetExact(obj, var_name, vars, as_value);
		}
		switch (var_name)
		{
		case "game":
			return game;
		case "context_vars":
			return new Value(vars);
		case "field":
			return new Value(this.field);
		default:
			if (this.field != null)
			{
				DT.Field field = this.field.FindVarField(var_name, vars);
				if (field != null)
				{
					if (!as_value)
					{
						return new Value(field);
					}
					return field.Value(vars, calc_expression: true, as_value);
				}
			}
			if (vars != null)
			{
				Value exact = Vars.GetExact(vars, var_name, null, as_value);
				if (exact != Value.Unknown)
				{
					return exact;
				}
			}
			if (game != null)
			{
				Value exact = Vars.GetExact(game, var_name, vars, as_value);
				if (exact != Value.Unknown)
				{
					return exact;
				}
			}
			if (this.field != null)
			{
				DT.Field parent = this.field;
				while (parent != null && parent.type != "file")
				{
					DT.Field field2 = parent.FindChild(var_name);
					if (field2 != null)
					{
						if (!as_value)
						{
							return new Value(field2);
						}
						Value exact = field2.Value(vars);
						if (exact != Value.Unknown)
						{
							return exact;
						}
					}
					parent = parent.parent;
				}
			}
			if (dt != null)
			{
				DT.Field field3 = dt.Find(var_name);
				if (field3 != null)
				{
					if (!as_value)
					{
						return new Value(field3);
					}
					return field3.Value(vars);
				}
			}
			if (Vars.GetReflectionMode() != Vars.ReflectionMode.Disabled && ContainsUpperCaseEnglishLetter(var_name))
			{
				Reflection.TypeInfo typeInfo = Reflection.GetTypeInfo(var_name);
				if (typeInfo != null)
				{
					return new Value(typeInfo);
				}
			}
			return Value.Unknown;
		}
	}

	private static bool ContainsUpperCaseEnglishLetter(string s)
	{
		if (s == null)
		{
			return false;
		}
		foreach (char c in s)
		{
			if (c >= 'A' && c <= 'Z')
			{
				return true;
			}
		}
		return false;
	}

	public string Dump(string ident = "", string new_line = "\n")
	{
		string text = "";
		if (field != null)
		{
			text = text + ident + "Field: " + field.ToString();
		}
		if (this.vars != null)
		{
			if (text != "")
			{
				text += "\n";
			}
			text = text + ident + "Vars:";
			text = ((!(this.vars is Vars vars)) ? (text + " " + this.vars.ToString()) : (text + vars.Dump(new_line + ident + "    ", new_line + ident + "    ")));
		}
		return text;
	}
}
