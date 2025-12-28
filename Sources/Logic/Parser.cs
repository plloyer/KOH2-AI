using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Logic;

public class DT : IVars
{
	public class Field : IVars
	{
		public enum Flags
		{
			StartsAtSameLine = 1,
			OpenBraceAtSameLine = 2,
			ClosingBraceAtSameLine = 4,
			PercentValue = 8,
			SlashedList = 0x10,
			HasCases = 0x20,
			HasVars = 0x40,
			ResolvingSwitchValue = 0x80,
			DontSave = 0x100
		}

		private struct CalcValueStack
		{
			public Field field;

			public object val;
		}

		public int line;

		public Flags flags;

		public DT dt;

		public Def def;

		public Field parent;

		public Field based_on;

		public Field extends;

		public List<Field> extensions;

		public string type = "";

		public string key = "";

		public string ignored;

		public string base_path;

		public string value_str = "";

		public Value value = Logic.Value.Unknown;

		public string comment1 = "";

		public string comment2 = "";

		public string comment3 = "";

		public List<Field> children;

		public Dictionary<string, Field> children_by_key;

		public Field cur_case;

		private static Random sm_random = new Random();

		private static List<CalcValueStack> calc_value_stack = new List<CalcValueStack>();

		public DefsContext context
		{
			get
			{
				if (dt != null)
				{
					return dt.context;
				}
				return null;
			}
		}

		public bool is_root
		{
			get
			{
				if (parent != null)
				{
					return parent.type == "file";
				}
				return true;
			}
		}

		public Field(DT dt)
		{
			this.dt = dt;
		}

		public override string ToString()
		{
			string text = Path(include_file: true);
			if (type != "")
			{
				text = type + " " + text;
			}
			if (!string.IsNullOrEmpty(base_path))
			{
				text = text + " : " + base_path;
			}
			if (value_str != "")
			{
				text = text + " = " + value_str;
			}
			if (value.is_valid)
			{
				text = text + " -> " + value.ToString();
			}
			if (children != null)
			{
				text = text + " {" + children.Count + "}";
			}
			return text;
		}

		public Field Mod(Field other)
		{
			if (other == null)
			{
				return this;
			}
			if (other.type == "delete")
			{
				if (ModManager.IsPureDelete(other))
				{
					if (is_root)
					{
						dt.roots.Remove(key);
					}
					parent?.DelChild(this);
					return this;
				}
				children = null;
				children_by_key = null;
				extends = null;
			}
			if (type != "file")
			{
				if (other.type != "" && other.type != "delete" && other.type != "extend")
				{
					type = other.type;
				}
				if (other.base_path != null)
				{
					base_path = other.base_path;
					based_on = other.based_on;
				}
				if (!string.IsNullOrEmpty(other.value_str))
				{
					value_str = other.value_str;
					value = other.value;
				}
			}
			if (other.children != null)
			{
				foreach (Field child in other.children)
				{
					Field field = FindChild(child.key);
					if (field != null)
					{
						field.Mod(child);
					}
					else if (!ModManager.IsPureDelete(child))
					{
						Field field2 = AddChild(child.key);
						if (child.FileField() == FileField())
						{
							field2.line = child.line;
						}
						bool num = child.type == "delete";
						if (num)
						{
							child.type = "";
						}
						field2.Mod(child);
						if (num)
						{
							child.type = "delete";
						}
					}
				}
			}
			return this;
		}

		public Field FileField()
		{
			Field field = this;
			while (field.parent != null)
			{
				field = field.parent;
			}
			if (!(field.type == "file"))
			{
				return null;
			}
			return field;
		}

		public string FilePath()
		{
			Field field = this;
			while (field.parent != null)
			{
				field = field.parent;
			}
			if (!(field.type == "file"))
			{
				return "";
			}
			return Unquote(field.value_str);
		}

		public string FileName()
		{
			Field field = this;
			while (field.parent != null)
			{
				field = field.parent;
			}
			if (!(field.type == "file"))
			{
				return "";
			}
			return field.key;
		}

		public string Type()
		{
			if (type != "")
			{
				return type;
			}
			if (based_on == null)
			{
				return "";
			}
			return based_on.Type();
		}

		public string Path(bool include_file = false, bool unique = false, char delimiter = '.')
		{
			if (parent == null && type == "file")
			{
				if (!include_file)
				{
					return key;
				}
				return Unquote(value_str);
			}
			string text = Key(unique);
			for (Field field = parent; field != null; field = field.parent)
			{
				if (field.parent == null && field.type == "file")
				{
					if (include_file)
					{
						text = Unquote(field.value_str) + "(" + line + "):" + text;
					}
					break;
				}
				text = field.Key(unique) + delimiter + text;
			}
			return text;
		}

		public int UniqueIndex()
		{
			if (parent == null)
			{
				return 0;
			}
			int num = 1;
			for (int i = 0; i < parent.children.Count; i++)
			{
				Field field = parent.children[i];
				if (field == this)
				{
					return num;
				}
				if (field.key == key)
				{
					num++;
				}
			}
			return 0;
		}

		public string Key(bool unique = false)
		{
			if (!unique)
			{
				return key;
			}
			int num = UniqueIndex();
			if (num < 2)
			{
				return key;
			}
			return key + "#" + num;
		}

		public string Comment1()
		{
			if (comment1 != "")
			{
				return comment1;
			}
			if (based_on == null)
			{
				return "";
			}
			return based_on.Comment1();
		}

		public string Comment2()
		{
			if (comment2 != "")
			{
				return comment2;
			}
			if (based_on == null)
			{
				return "";
			}
			return based_on.Comment2();
		}

		public string Comment3()
		{
			if (comment3 != "")
			{
				return comment3;
			}
			if (based_on == null)
			{
				return "";
			}
			return based_on.Comment3();
		}

		public string ValueStr()
		{
			if (value_str != "")
			{
				return value_str;
			}
			if (based_on == null)
			{
				return "";
			}
			return based_on.ValueStr();
		}

		public string ValueStr(int idx)
		{
			if (idx < 0)
			{
				return Logic.Value.Unknown;
			}
			if (Value(null, calc_expression: false, as_value: false).obj_val is List<SubValue> list)
			{
				if (idx >= list.Count)
				{
					return Logic.Value.Unknown;
				}
				return list[idx].value_str;
			}
			if (idx != 0)
			{
				return Logic.Value.Unknown;
			}
			return ValueStr();
		}

		private bool PushCalcValue(Field f, object val)
		{
			if (!MainThreadUpdates.IsMainThread())
			{
				return true;
			}
			for (int i = 0; i < calc_value_stack.Count; i++)
			{
				CalcValueStack calcValueStack = calc_value_stack[i];
				if (calcValueStack.field == f && calcValueStack.val == val)
				{
					return false;
				}
			}
			calc_value_stack.Add(new CalcValueStack
			{
				field = f,
				val = val
			});
			return true;
		}

		private void PopCalcValue(Field f, object val)
		{
			if (!MainThreadUpdates.IsMainThread())
			{
				return;
			}
			if (calc_value_stack.Count < 1)
			{
				Game.Log("Calc Value Stack messed up, may be a multithreaded issue?", Game.LogType.Error);
				return;
			}
			CalcValueStack calcValueStack = calc_value_stack[calc_value_stack.Count - 1];
			if (calcValueStack.field != f || calcValueStack.val != val)
			{
				Game.Log("Calc Value Stack messed up, may be a multithreaded issue?", Game.LogType.Error);
			}
			else
			{
				calc_value_stack.RemoveAt(calc_value_stack.Count - 1);
			}
		}

		public Value CalcValue(Value value, DefsContext context, IVars vars, bool as_value)
		{
			if (!value.is_object)
			{
				return value;
			}
			if (!PushCalcValue(this, value.obj_val))
			{
				return Logic.Value.Error("<<infinite loop>>");
			}
			Value result;
			try
			{
				result = _CalcValue(value, context, vars, as_value);
			}
			catch (Exception arg)
			{
				Game.Log($"Error evaluating {this}: {arg}", Game.LogType.Error);
				result = Logic.Value.Unknown;
			}
			PopCalcValue(this, value.obj_val);
			return result;
		}

		private Value _CalcValue(Value value, DefsContext context, IVars vars, bool as_value)
		{
			if (value.obj_val is Field field)
			{
				if (!as_value)
				{
					return value;
				}
				return field.CalcValue(field.value, context, vars, as_value);
			}
			if (value.obj_val is Expression expression)
			{
				if (context == null)
				{
					context = DefsContext.temp_context;
				}
				Field field2 = context.field;
				IVars vars2 = context.vars;
				context.field = this;
				context.vars = vars;
				Value result = expression.Calc(context, as_value);
				context.field = field2;
				context.vars = vars2;
				return result;
			}
			return value;
		}

		public bool Bool(IVars vars = null, bool def_val = false)
		{
			return DT.Bool(Value(vars), def_val);
		}

		public bool Bool(int idx, IVars vars = null, bool def_val = false)
		{
			return DT.Bool(Value(idx, vars), def_val);
		}

		public bool RandomBool(IVars vars = null, bool def_val = false)
		{
			return DT.Bool(RandomValue(vars), def_val);
		}

		public int Int(IVars vars = null, int def_val = 0)
		{
			return DT.Int(Value(vars), def_val);
		}

		public int Int(int idx, IVars vars = null, int def_val = 0)
		{
			return DT.Int(Value(idx, vars), def_val);
		}

		public int RandomInt(IVars vars = null, int def_val = 0)
		{
			return DT.Int(RandomValue(vars), def_val);
		}

		public float Float(IVars vars = null, float def_val = 0f)
		{
			return DT.Float(Value(vars), def_val);
		}

		public float Float(int idx, IVars vars = null, float def_val = 0f)
		{
			return DT.Float(Value(idx, vars), def_val);
		}

		public float RandomFloat(IVars vars = null, float def_val = 0f)
		{
			return DT.Float(RandomValue(vars), def_val);
		}

		public Point Point(IVars vars = null)
		{
			return DT.Point(Value(vars));
		}

		public Point Point(int idx, IVars vars = null)
		{
			return DT.Point(Value(idx, vars));
		}

		public Point RandomPoint(IVars vars = null)
		{
			return DT.Point(RandomValue(vars));
		}

		public string String(IVars vars = null, string def_val = "")
		{
			return DT.String(Value(vars), def_val);
		}

		public string String(int idx, IVars vars = null, string def_val = "")
		{
			return DT.String(Value(idx, vars), def_val);
		}

		public string RandomString(IVars vars = null, string def_val = "")
		{
			return DT.String(RandomValue(vars), def_val);
		}

		public Field Ref(IVars vars = null, bool calc_expression = true, bool recursive = true)
		{
			Field field = Value(vars, calc_expression, as_value: false).Get<Field>();
			if (field == null || !recursive)
			{
				return field;
			}
			Field field2 = field.Ref(vars, calc_expression, recursive);
			if (field2 != null)
			{
				return field2;
			}
			return field;
		}

		public Value Value(IVars vars = null, bool calc_expression = true, bool as_value = true)
		{
			if (calc_expression && vars != null)
			{
				Field field = ResolveCase(vars, recursive: false);
				if (field != null && field != this)
				{
					Field field2 = field.FindChild("value", vars);
					if (field2 != null)
					{
						return field2.Value(vars, calc_expression, as_value);
					}
				}
			}
			if (value.is_valid)
			{
				if (calc_expression)
				{
					return CalcValue(value, context, vars, as_value);
				}
				if (!as_value)
				{
					return value;
				}
				if (value.obj_val is Field field3)
				{
					return field3.Value(vars, calc_expression: false);
				}
				return value;
			}
			if (value_str != "")
			{
				return Logic.Value.Null;
			}
			if (based_on == null)
			{
				return Logic.Value.Unknown;
			}
			Value result = based_on.Value(null, calc_expression: false, as_value);
			if (!calc_expression)
			{
				return result;
			}
			return CalcValue(result, context, vars, as_value);
		}

		public Value Value(int idx, IVars vars = null, bool calc_expression = true, bool as_value = true)
		{
			if (idx < 0)
			{
				return Logic.Value.Unknown;
			}
			if (calc_expression && vars != null)
			{
				Field field = ResolveCase(vars, recursive: false);
				if (field != null && field != this)
				{
					Field field2 = field.FindChild("value", vars);
					if (field2 != null)
					{
						return field2.Value(idx, vars, calc_expression, as_value);
					}
				}
			}
			Value result = Value(vars, calc_expression);
			if (result.obj_val is List<SubValue> list)
			{
				if (idx >= list.Count)
				{
					return Logic.Value.Unknown;
				}
				result = list[idx].value;
				if (!calc_expression)
				{
					return result;
				}
				return CalcValue(result, context, vars, as_value);
			}
			if (idx != 0)
			{
				return Logic.Value.Unknown;
			}
			if (!as_value)
			{
				result = Value(vars, calc_expression, as_value: false);
			}
			_ = result.is_valid;
			return result;
		}

		public static Random SetSeed(int seed)
		{
			Random result = sm_random;
			sm_random = new Random(seed);
			return result;
		}

		public static void RestoreSeed(Random rnd)
		{
			if (rnd != null)
			{
				sm_random = rnd;
			}
		}

		public Value RandomValue(IVars vars = null, bool calc_expression = true, bool as_value = true)
		{
			if (calc_expression && vars != null)
			{
				Field field = ResolveCase(vars, recursive: false);
				if (field != null && field != this)
				{
					Field field2 = field.FindChild("value", vars);
					if (field2 != null)
					{
						return field2.RandomValue(vars, calc_expression, as_value);
					}
				}
			}
			int num = NumValues();
			switch (num)
			{
			case 0:
				return Logic.Value.Unknown;
			case 1:
				return Value(0, vars, calc_expression, as_value);
			default:
			{
				int idx = sm_random.Next(0, num);
				return Value(idx, vars, calc_expression, as_value);
			}
			}
		}

		public int NumValues()
		{
			Value value = Value(null, calc_expression: false, as_value: false);
			if (!value.is_valid)
			{
				return 0;
			}
			if (value.obj_val is List<SubValue> list)
			{
				return list.Count;
			}
			return 1;
		}

		public void ListChildren(Field parent, List<Field> lst, bool allow_base, bool allow_extensions)
		{
			if (allow_base && based_on != null)
			{
				based_on.ListChildren(parent, lst, allow_base: true, allow_extensions);
			}
			if (children != null)
			{
				for (int i = 0; i < children.Count; i++)
				{
					Field field = children[i];
					if (!(field.key == ""))
					{
						Field item = parent.VirtualChild(field);
						int num = FindIdx(lst, field.key);
						if (num >= 0)
						{
							lst.RemoveAt(num);
						}
						lst.Add(item);
					}
				}
			}
			if (allow_extensions && extensions != null)
			{
				for (int j = 0; j < extensions.Count; j++)
				{
					extensions[j].ListChildren(parent, lst, allow_base, allow_extensions: true);
				}
			}
			static int FindIdx(List<Field> fields, string key)
			{
				if (fields == null)
				{
					return -1;
				}
				for (int k = 0; k < fields.Count; k++)
				{
					if (fields[k].key == key)
					{
						return k;
					}
				}
				return -1;
			}
		}

		public List<Field> Children()
		{
			if (based_on == null)
			{
				return children;
			}
			List<Field> list = new List<Field>();
			ListChildren(this, list, allow_base: true, allow_extensions: true);
			return list;
		}

		public Field VirtualChild(Field child)
		{
			if (child == null)
			{
				return null;
			}
			for (Field field = child.parent; field != null; field = field.parent)
			{
				if (field == this)
				{
					return child;
				}
			}
			if (!(child.value.obj_val is Expression))
			{
				return child;
			}
			return new Field(child.dt)
			{
				parent = this,
				key = child.key,
				based_on = child
			};
		}

		public Field FindChild(string path, IVars vars = null, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			if (string.IsNullOrEmpty(path))
			{
				return null;
			}
			if (allow_switches && HasCases())
			{
				find_had_cases = true;
			}
			if (allow_switches && vars != null)
			{
				Field field = ResolveCase(vars, recursive: false);
				if (field != null && field != this)
				{
					Field field2 = field.FindChild(path, vars, allow_base, allow_extensions, allow_switches, delimiter);
					if (field2 != null)
					{
						return field2;
					}
				}
			}
			int num = ((delimiter == ' ') ? (-1) : path.IndexOf('.'));
			string text;
			string text2;
			if (num < 0)
			{
				text = path;
				text2 = "";
			}
			else
			{
				text = path.Substring(0, num);
				text2 = path.Substring(num + 1);
			}
			if (children != null)
			{
				Field field3 = null;
				if (children_by_key != null)
				{
					children_by_key.TryGetValue(text, out field3);
				}
				else
				{
					for (int i = 0; i < children.Count; i++)
					{
						Field field4 = children[i];
						if (!(field4.key != text) && !(field4.type == "extend"))
						{
							field3 = field4;
							break;
						}
					}
				}
				if (field3 != null)
				{
					if (text2 == "")
					{
						return VirtualChild(field3);
					}
					Field field5 = field3.FindChild(text2, vars, allow_base, allow_extensions, allow_switches, delimiter);
					if (field5 != null)
					{
						return VirtualChild(field5);
					}
				}
			}
			if (extensions != null && allow_extensions)
			{
				for (int num2 = extensions.Count - 1; num2 >= 0; num2--)
				{
					Field field6 = extensions[num2].FindChild(path, vars, allow_base, allow_extensions: true, allow_switches, delimiter);
					if (field6 != null)
					{
						return VirtualChild(field6);
					}
				}
			}
			if (!allow_base || based_on == null)
			{
				return null;
			}
			return VirtualChild(based_on.FindChild(path, vars, allow_base: true, allow_extensions, allow_switches, delimiter));
		}

		public Field FindOrAddChild(string path, IVars vars = null, bool allowBase = true, bool allowExtensions = true, bool allowSwitches = true, char delimiter = '.', int line = 0)
		{
			if (string.IsNullOrEmpty(path))
			{
				return null;
			}
			int num = ((delimiter == ' ') ? (-1) : path.IndexOf('.'));
			if (num < 0)
			{
				Field field = FindChild(path, vars, allowBase, allowExtensions, allowSwitches, delimiter);
				if (field == null)
				{
					field = AddChild(path);
					field.line = line;
				}
				return field;
			}
			string path2 = path.Substring(0, num);
			string path3 = path.Substring(num + 1);
			Field field2 = FindChild(path2, vars, allowBase, allowExtensions, allowSwitches, delimiter);
			if (field2 == null)
			{
				field2 = AddChild(path2);
				field2.line = line;
			}
			return field2.FindOrAddChild(path3, vars, allowBase, allowExtensions, allowSwitches, delimiter, line);
		}

		public bool HasCases(bool allow_base = true, bool allow_extensions = true)
		{
			if ((flags & Flags.HasCases) != 0)
			{
				return true;
			}
			if (allow_extensions && extensions != null)
			{
				for (int i = 0; i < extensions.Count; i++)
				{
					if (extensions[i].HasCases(allow_base: false, allow_extensions: false))
					{
						return true;
					}
				}
			}
			if (allow_base && based_on != null && based_on.HasCases(allow_base, allow_extensions))
			{
				return true;
			}
			return false;
		}

		public bool IsInCase()
		{
			for (Field field = parent; field != null; field = field.parent)
			{
				if (field.Type() == "case")
				{
					return true;
				}
			}
			return false;
		}

		public bool HasVars(bool allow_base = true, bool allow_extensions = true)
		{
			if ((flags & Flags.HasVars) != 0)
			{
				return true;
			}
			if (allow_extensions && extensions != null)
			{
				for (int i = 0; i < extensions.Count; i++)
				{
					if (extensions[i].HasVars(allow_base: false, allow_extensions: false))
					{
						return true;
					}
				}
			}
			if (allow_base && based_on != null && based_on.HasVars(allow_base, allow_extensions))
			{
				return true;
			}
			if (parent != null && parent.type != "file" && parent.HasVars(allow_base, allow_extensions))
			{
				return true;
			}
			return false;
		}

		public Value GetSwitchValue(IVars vars)
		{
			flags |= Flags.ResolvingSwitchValue;
			Field field = FindChild("switch_value");
			if (field != null)
			{
				Value value = field.Value(null, calc_expression: false, as_value: false);
				if (value.obj_val is Expression)
				{
					Value result = field.CalcValue(value, context, vars, as_value: true);
					flags &= (Flags)(-129);
					return result;
				}
				if (value.is_string)
				{
					Value result2 = vars?.GetVar(value.String()) ?? Logic.Value.Unknown;
					flags &= (Flags)(-129);
					return result2;
				}
			}
			if (vars == null)
			{
				flags &= (Flags)(-129);
				return Logic.Value.Unknown;
			}
			Value var = vars.GetVar("switch_value");
			flags &= (Flags)(-129);
			return var;
		}

		public static bool MatchCase(Field case_field, IVars vars, Value switch_value)
		{
			Value value = case_field.Value(null, calc_expression: false, as_value: false);
			if (value.is_valid)
			{
				return case_field.CalcValue(value, case_field.context, vars, as_value: true).Bool();
			}
			if (case_field.key == "default")
			{
				return true;
			}
			if (case_field.key == "null")
			{
				return switch_value.is_null;
			}
			if (float.TryParse(case_field.key, out var result))
			{
				return switch_value.Match(result);
			}
			string text = case_field.key;
			Value value2 = Logic.Value.Unknown;
			bool flag = !switch_value.is_unknown;
			Field field = case_field.FindVarField(text);
			if (field != null)
			{
				bool flag2 = field.Type() == "bool";
				if (flag2 || flag)
				{
					value2 = field.Value(vars);
					if (flag2)
					{
						return value2.Bool();
					}
				}
			}
			if (value2.is_unknown && flag && vars != null)
			{
				value2 = vars.GetVar(text);
			}
			if (value2.is_unknown)
			{
				field = case_field.FindChild(text);
				if (field != null)
				{
					bool flag3 = field.Type() == "bool";
					if (flag3 || flag)
					{
						value2 = field.Value(vars);
						if (flag3)
						{
							return value2.Bool();
						}
					}
				}
			}
			if (value2.is_unknown && case_field.dt != null)
			{
				field = case_field.dt.FindInParents(case_field, case_field.key);
				if (field != null && field.Type() != "case")
				{
					bool flag4 = field.Type() == "bool";
					if (flag4 || flag)
					{
						value2 = field.Value(vars);
						if (flag4)
						{
							return value2.Bool();
						}
					}
				}
			}
			if (!flag)
			{
				return false;
			}
			if (value2.is_unknown || value2.is_null)
			{
				value2 = text;
			}
			if (value2.Match(switch_value))
			{
				return true;
			}
			if (((Value)text).Match(switch_value))
			{
				return true;
			}
			return false;
		}

		public Field ResolveCase(IVars vars, bool recursive)
		{
			if ((flags & Flags.ResolvingSwitchValue) != 0)
			{
				return this;
			}
			if (cur_case != null)
			{
				if (!recursive)
				{
					return cur_case;
				}
				return cur_case.ResolveCase(vars, recursive);
			}
			if (!HasCases())
			{
				return this;
			}
			Value switchValue = GetSwitchValue(vars);
			List<string> list = Keys();
			for (int i = 0; i < list.Count; i++)
			{
				string path = list[i];
				Field field = FindChild(path);
				if (field.Type() != "case")
				{
					continue;
				}
				cur_case = field;
				if (!MatchCase(field, vars, switchValue))
				{
					cur_case = null;
					continue;
				}
				if (!recursive)
				{
					cur_case = null;
					return field;
				}
				Field result = field.ResolveCase(vars, recursive: true);
				cur_case = null;
				return result;
			}
			return null;
		}

		public string GetValueStr(string key, string def_val = "", bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			Field field = FindChild(key, null, allow_base, allow_extensions);
			if (field == null)
			{
				return def_val;
			}
			return field.ValueStr();
		}

		public bool GetBool(string key, IVars vars = null, bool def_val = false, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			return FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter)?.Bool(vars, def_val) ?? def_val;
		}

		public List<string> GetListOfStrings(string key)
		{
			if (!(GetValue(key).obj_val is List<SubValue> { Count: not 0 } list))
			{
				return null;
			}
			List<string> list2 = new List<string>();
			foreach (SubValue item in list)
			{
				list2.Add(item.value.String());
			}
			return list2;
		}

		public bool GetRandomBool(string key, IVars vars = null, bool def_val = false, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			return FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter)?.RandomBool(vars, def_val) ?? def_val;
		}

		public int GetInt(string key, IVars vars = null, int def_val = 0, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			return FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter)?.Int(vars, def_val) ?? def_val;
		}

		public int GetRandomInt(string key, IVars vars = null, int def_val = 0, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			return FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter)?.RandomInt(vars, def_val) ?? def_val;
		}

		public float GetFloat(string key, IVars vars = null, float def_val = 0f, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			return FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter)?.Float(vars, def_val) ?? def_val;
		}

		public float GetRandomFloat(string key, IVars vars = null, float def_val = 0f, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			return FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter)?.RandomFloat(vars, def_val) ?? def_val;
		}

		public Point GetPoint(string key, IVars vars = null, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			return FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter)?.Point(vars) ?? Logic.Point.Invalid;
		}

		public Point GetRandomPoint(string key, IVars vars = null, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			return FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter)?.RandomPoint(vars) ?? Logic.Point.Invalid;
		}

		public string GetString(string key, IVars vars = null, string def_val = "", bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			Field field = FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter);
			if (field == null)
			{
				return def_val;
			}
			return field.String(vars, def_val);
		}

		public string GetRandomString(string key, IVars vars = null, string def_val = "", bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			Field field = FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter);
			if (field == null)
			{
				return def_val;
			}
			return field.RandomString(vars, def_val);
		}

		public Field GetRef(string key, IVars vars = null, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			return FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter)?.Ref(vars);
		}

		public Value GetValue(string key, IVars vars = null, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			return FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter)?.Value(vars) ?? Logic.Value.Unknown;
		}

		public Value GetValue(int idx, string key, IVars vars = null, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			return FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter)?.Value(idx, vars) ?? Logic.Value.Unknown;
		}

		public Value GetRandomValue(string key, IVars vars = null, bool allow_base = true, bool allow_extensions = true, bool allow_switches = true, char delimiter = '.')
		{
			return FindChild(key, vars, allow_base, allow_extensions, allow_switches, delimiter)?.RandomValue(vars) ?? Logic.Value.Unknown;
		}

		public Field FindVarField(string key, IVars vars = null)
		{
			if (!HasVars())
			{
				return null;
			}
			Field field = FindChild("vars");
			if (field != null)
			{
				Field field2 = field.FindChild(key, vars);
				if (field2 != null)
				{
					return field2;
				}
			}
			Field field3 = parent;
			while (field3 != null && field3.type != "file")
			{
				if (field3.key == "vars")
				{
					return null;
				}
				field = field3.FindChild("vars");
				if (field != null)
				{
					Field field4 = field.FindChild(key, vars);
					if (field4 != null)
					{
						return field4;
					}
				}
				field3 = field3.parent;
			}
			return null;
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			switch (key)
			{
			case "FIELD":
				return new Value(this);
			case "TYPE":
				return Type();
			case "KEY":
				return Key();
			case "PATH":
				return Path(include_file: true);
			case "BASE_PATH":
				return base_path;
			case "BASED_ON":
				return new Value(based_on);
			case "VALUE_STR":
				return value_str;
			case "VALUE":
				return Value(vars);
			case "CHILDREN":
				return new Value(Children());
			case "DT_DEF":
				return new Value(def);
			case "DEF":
				return def?.def;
			case "COMMENT1":
				return comment1;
			case "COMMENT2":
				return comment2;
			case "COMMENT3":
				return comment3;
			default:
			{
				Field field = FindVarField(key, vars);
				if (field != null)
				{
					if (!as_value)
					{
						return new Value(field);
					}
					return field.Value(vars, calc_expression: true, as_value);
				}
				Field field2 = FindChild(key, vars);
				if (field2 != null)
				{
					if (!as_value)
					{
						return new Value(field2);
					}
					return field2.Value(vars, calc_expression: true, as_value);
				}
				Value value = Value(vars, calc_expression: true, as_value: false);
				if (value.is_object)
				{
					return Vars.Get(value.obj_val, key, vars, as_value);
				}
				return Logic.Value.Unknown;
			}
			}
		}

		public Field SetValue(string key, string value_str, object value = null)
		{
			Field field = FindChild(key, null, allow_base: false, allow_extensions: true, allow_switches: false, ' ');
			if (field == null)
			{
				field = AddChild(key);
			}
			field.value_str = value_str;
			field.value = new Value(value);
			return field;
		}

		public Field SetValue(string key, Value value)
		{
			Field field = FindChild(key, null, allow_base: false, allow_extensions: true, allow_switches: false, ' ');
			if (field == null)
			{
				field = AddChild(key);
			}
			field.value_str = "";
			field.value = value;
			return field;
		}

		public void BuildChildrenIndex()
		{
			if (children_by_key != null)
			{
				return;
			}
			children_by_key = new Dictionary<string, Field>(children.Count);
			for (int i = 0; i < children.Count; i++)
			{
				Field field = children[i];
				if (!(field.key == "") && !(field.type == "extend"))
				{
					children_by_key[field.key] = field;
				}
			}
		}

		public void AddChild(Field child)
		{
			if (child == null)
			{
				return;
			}
			if (children == null)
			{
				children = new List<Field>();
			}
			children.Add(child);
			child.parent = this;
			if (child.key != "" && child.type != "extend")
			{
				if (children_by_key != null)
				{
					children_by_key[child.key] = child;
				}
				else if (children.Count > 8 && type != "file")
				{
					BuildChildrenIndex();
				}
			}
			if (based_on != null && child.based_on == null && !string.IsNullOrEmpty(child.key))
			{
				child.based_on = based_on.FindChild(child.key);
			}
		}

		public Field AddChild(string key)
		{
			Field field = new Field(dt);
			field.key = key;
			AddChild(field);
			return field;
		}

		public bool DelChild(Field cf)
		{
			if (cf.parent == this)
			{
				cf.parent = null;
			}
			if (children_by_key != null)
			{
				children_by_key.Remove(cf.key);
			}
			if (children == null)
			{
				return false;
			}
			return children.Remove(cf);
		}

		public Field CreateChild(string path, char delimiter = '.')
		{
			int num = ((delimiter == ' ') ? (-1) : path.IndexOf('.'));
			string path2;
			string text;
			if (num >= 0)
			{
				path2 = path.Substring(0, num);
				text = path.Substring(num + 1);
			}
			else
			{
				path2 = path;
				text = null;
			}
			Field field = FindChild(path2, null, allow_base: false, allow_extensions: true, allow_switches: false, ' ');
			if (field == null)
			{
				field = AddChild(path2);
			}
			if (text == null)
			{
				return field;
			}
			return field.CreateChild(text);
		}

		public bool IsBasedOn(Field field)
		{
			for (Field field2 = this; field2 != null; field2 = field2.based_on)
			{
				if (field2 == field)
				{
					return true;
				}
			}
			return false;
		}

		public Field BaseRoot()
		{
			Field field = this;
			while (field.based_on != null)
			{
				field = field.based_on;
			}
			return field;
		}

		public void ListKeys(List<string> keys, bool allow_base, bool allow_extensions)
		{
			if (allow_base && based_on != null)
			{
				based_on.ListKeys(keys, allow_base: true, allow_extensions);
			}
			if (children != null)
			{
				for (int i = 0; i < children.Count; i++)
				{
					Field field = children[i];
					if (!(field.key == "") && !keys.Contains(field.key))
					{
						keys.Add(field.key);
					}
				}
			}
			if (allow_extensions && extensions != null)
			{
				for (int j = 0; j < extensions.Count; j++)
				{
					extensions[j].ListKeys(keys, allow_base, allow_extensions: true);
				}
			}
		}

		public List<string> Keys(bool allow_base = true, bool allow_extensions = true)
		{
			List<string> list = new List<string>();
			ListKeys(list, allow_base, allow_extensions);
			return list;
		}

		public string GetSaveString(int indent = 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			GetSaveString(stringBuilder, indent);
			return stringBuilder.ToString();
		}

		public void GetSaveString(StringBuilder sb, int indent = 0)
		{
			for (int i = 0; i < indent; i++)
			{
				sb.Append("  ");
			}
			if (!string.IsNullOrEmpty(type))
			{
				sb.Append(type + " ");
			}
			if (!string.IsNullOrEmpty(key))
			{
				sb.Append(key);
			}
			if (!string.IsNullOrEmpty(base_path))
			{
				sb.Append(" : " + base_path);
			}
			switch (value.type)
			{
			case Logic.Value.Type.Int:
				sb.Append($" = {value.int_val}");
				break;
			case Logic.Value.Type.Float:
				sb.Append(" = " + FloatToStr(value.float_val));
				break;
			case Logic.Value.Type.String:
				sb.Append(" = " + Enquote(value.String()));
				break;
			case Logic.Value.Type.Object:
				sb.Append($"// = {value.obj_val}");
				break;
			}
			if (children != null)
			{
				sb.Append("\n");
				for (int j = 0; j < indent; j++)
				{
					sb.Append("  ");
				}
				sb.Append("{\n");
				for (int k = 0; k < children.Count; k++)
				{
					children[k].GetSaveString(sb, indent + 1);
					sb.Append("\n");
				}
				for (int l = 0; l < indent; l++)
				{
					sb.Append("  ");
				}
				sb.Append("}");
			}
		}

		private static void WriteString(string str, ref MemStream writer, UniqueStrings unique_strings, bool add_new_strings)
		{
			int uid = 0;
			unique_strings?.GetInfo(str, add_new_strings, out uid);
			writer.Write7BitUInt(uid);
			if (uid == 0)
			{
				writer.WriteString(str);
			}
		}

		private static string ReadString(ref MemStream reader, UniqueStrings unique_strings)
		{
			int num = reader.Read7BitUInt();
			if (num == 0)
			{
				return reader.ReadString();
			}
			return unique_strings?.Get(num);
		}

		public void SaveBinary(ref MemStream writer, UniqueStrings unique_strings = null, bool add_new_strings = true)
		{
			byte b = (byte)value.type;
			if (!string.IsNullOrEmpty(type))
			{
				b |= 0x80;
			}
			if (children != null)
			{
				b |= 0x40;
			}
			if (value.type == Logic.Value.Type.Int && value.int_val < 0)
			{
				b |= 0x20;
			}
			writer.WriteByte(b);
			if (!string.IsNullOrEmpty(type))
			{
				WriteString(type, ref writer, unique_strings, add_new_strings);
			}
			WriteString(key, ref writer, unique_strings, add_new_strings);
			switch (value.type)
			{
			case Logic.Value.Type.Int:
				writer.Write7BitUInt(Math.Abs(value.int_val));
				break;
			case Logic.Value.Type.Float:
				writer.WriteFloat(value.float_val);
				break;
			case Logic.Value.Type.String:
				WriteString(value.String(), ref writer, unique_strings, add_new_strings);
				break;
			}
			if (children != null)
			{
				writer.Write7BitUInt(children.Count);
				for (int i = 0; i < children.Count; i++)
				{
					children[i].SaveBinary(ref writer, unique_strings, add_new_strings);
				}
			}
		}

		public static void SaveBinaryFields(List<Field> fields, ref MemStream writer, UniqueStrings unique_strings = null, bool add_new_strings = true)
		{
			writer.Write7BitUInt(fields.Count);
			for (int i = 0; i < fields.Count; i++)
			{
				fields[i].SaveBinary(ref writer, unique_strings, add_new_strings);
			}
		}

		public static Field LoadBinaryField(ref MemStream reader, UniqueStrings unique_strings = null)
		{
			Field field = new Field(null);
			byte b = reader.ReadByte();
			if ((b & 0x80) != 0)
			{
				field.type = ReadString(ref reader, unique_strings);
			}
			field.key = ReadString(ref reader, unique_strings);
			switch ((Value.Type)(b & 0xF))
			{
			case Logic.Value.Type.Null:
				field.value = Logic.Value.Null;
				break;
			case Logic.Value.Type.Int:
				field.value = reader.Read7BitUInt();
				break;
			case Logic.Value.Type.Float:
				field.value = reader.ReadFloat();
				break;
			case Logic.Value.Type.String:
				field.value = ReadString(ref reader, unique_strings);
				break;
			}
			if ((b & 0x20) != 0)
			{
				field.value.int_val = -field.value.int_val;
			}
			if ((b & 0x40) == 0)
			{
				return field;
			}
			int num = reader.Read7BitUInt();
			field.children = new List<Field>(num);
			for (int i = 0; i < num; i++)
			{
				Field child = LoadBinaryField(ref reader, unique_strings);
				field.AddChild(child);
			}
			return field;
		}

		public static List<Field> LoadBinaryFields(ref MemStream reader, UniqueStrings unique_strings = null)
		{
			int num = reader.Read7BitUInt();
			List<Field> list = new List<Field>(num);
			for (int i = 0; i < num; i++)
			{
				Field item = LoadBinaryField(ref reader, unique_strings);
				list.Add(item);
			}
			return list;
		}
	}

	public class Def
	{
		public string path = "";

		public Field field;

		public Logic.Def def;

		public List<Def> defs;

		public override string ToString()
		{
			return path;
		}
	}

	public class SubValue
	{
		public string value_str = "";

		public Value value = Value.Unknown;

		public string comment1 = "";

		public string comment2 = "";

		public string comment3 = "";

		public override string ToString()
		{
			return value_str + " = " + value.ToString();
		}
	}

	public delegate bool SkipFileFunc(string path, string name, bool is_directory);

	public class Parser
	{
		private DT m_dt;

		public string m_str;

		public int m_ofs;

		public int m_len;

		public int m_line;

		public bool m_skipped_NL;

		public static bool allow_multi_line_quotted_strings;

		public Parser(DT dt, string str, int ofs = 0, int len = -1)
		{
			m_dt = dt;
			m_str = str;
			m_ofs = ofs;
			m_line = 1;
			if (len < 0)
			{
				m_len = str?.Length ?? 0;
			}
			else
			{
				m_len = len;
			}
			if (m_ofs < 0)
			{
				m_ofs = 0;
			}
			else if (m_ofs > m_len)
			{
				m_ofs = m_len;
			}
		}

		public Field ReadField(Field parent)
		{
			SkipSLBlanks();
			if (IsAtEnd())
			{
				return null;
			}
			if (IsAt('}'))
			{
				return null;
			}
			Field field = new Field(m_dt);
			if (!m_skipped_NL)
			{
				field.flags |= Field.Flags.StartsAtSameLine;
			}
			field.parent = parent;
			field.line = m_line;
			field.type = ReadKey();
			SkipSLBlanks();
			field.key = ReadKey();
			if (field.key == "" && field.type != "")
			{
				field.key = field.type;
				field.type = "";
			}
			SkipSLBlanks();
			int ofs = m_ofs;
			while (!IsAtEOL() && !IsAt(':') && !IsAt('=') && !IsAt(';') && !IsAt('{') && !IsAt('}') && !IsAtComment())
			{
				Skip();
			}
			if (m_ofs != ofs)
			{
				field.ignored = m_str.Substring(ofs, m_ofs - ofs);
			}
			else
			{
				field.ignored = null;
			}
			if (IsAt(':'))
			{
				Skip();
				SkipSLBlanks();
				field.base_path = ReadBase();
			}
			if (IsAt('='))
			{
				Skip();
				SkipSLBlanks();
				field.value_str = ReadValue(field, out field.value, ";{}", allow_slashed_lists: true);
			}
			else
			{
				field.value_str = "";
			}
			bool flag = IsAt('{');
			if (flag)
			{
				field.flags |= Field.Flags.OpenBraceAtSameLine;
				Skip();
				SkipSLBlanks();
			}
			bool flag2 = IsAt(';');
			if (flag2)
			{
				Skip();
				SkipSLBlanks();
				m_skipped_NL = false;
			}
			field.comment1 = ReadComment();
			SkipNL();
			SkipSLBlanks();
			if (!flag && !flag2 && IsAt('{'))
			{
				if (!m_skipped_NL)
				{
					field.flags |= Field.Flags.OpenBraceAtSameLine;
				}
				flag = true;
				Skip();
				SkipSLBlanks();
				field.comment2 = ReadComment();
				SkipNL();
				SkipSLBlanks();
			}
			if (flag)
			{
				field.children = new List<Field>();
				ReadFields(field);
				if (IsAt('}'))
				{
					if (!m_skipped_NL)
					{
						field.flags |= Field.Flags.ClosingBraceAtSameLine;
					}
					Skip();
					SkipSLBlanks();
					field.comment3 = ReadComment();
					SkipNL();
					SkipSLBlanks();
				}
			}
			return field;
		}

		public char Char(int iRelOfs = 0)
		{
			int num = m_ofs + iRelOfs;
			if (num >= 0 && num < m_len)
			{
				return m_str[num];
			}
			return '\0';
		}

		public static bool IsSLBlank(char c)
		{
			if (c != ' ')
			{
				return c == '\t';
			}
			return true;
		}

		public static bool IsNL(char c)
		{
			if (c != '\r')
			{
				return c == '\n';
			}
			return true;
		}

		public static bool IsBlank(char c)
		{
			if (!IsSLBlank(c))
			{
				return IsNL(c);
			}
			return true;
		}

		public bool IsAtEnd()
		{
			return m_ofs >= m_len;
		}

		public bool IsAt(char c)
		{
			return Char() == c;
		}

		public bool IsAtEOL()
		{
			if (!IsAtEnd())
			{
				return IsNL(Char());
			}
			return true;
		}

		public bool IsAtComment()
		{
			if (m_ofs + 1 >= m_len)
			{
				return false;
			}
			char c = m_str[m_ofs];
			if (c != '/')
			{
				return false;
			}
			if (m_str[m_ofs + 1] != c)
			{
				return false;
			}
			return true;
		}

		public void Skip()
		{
			m_ofs++;
			if (m_ofs > m_len)
			{
				m_ofs = m_len;
			}
		}

		public void SkipBlanks()
		{
			bool skipped_NL = false;
			while (true)
			{
				SkipSLBlanks();
				SkipNL();
				if (!m_skipped_NL)
				{
					break;
				}
				skipped_NL = true;
			}
			m_skipped_NL = skipped_NL;
		}

		public void SkipSLBlanks()
		{
			while (IsSLBlank(Char()))
			{
				Skip();
			}
		}

		public void SkipNL()
		{
			char c = Char();
			if (!IsNL(c))
			{
				m_skipped_NL = false;
				return;
			}
			Skip();
			if (Char() == 23 - c)
			{
				Skip();
			}
			m_line++;
			m_skipped_NL = true;
		}

		public string ReadKey()
		{
			int ofs = m_ofs;
			while (!IsAtEOL() && !IsBlank(Char()) && !IsAt(':') && !IsAt('=') && !IsAt(';') && !IsAt('{') && !IsAt('}') && !IsAtComment())
			{
				Skip();
			}
			string text = ((ofs == m_ofs) ? "" : m_str.Substring(ofs, m_ofs - ofs));
			UniqueStrings uniqueStrings = m_dt?.unique_strings;
			if (uniqueStrings != null)
			{
				text = uniqueStrings.Resolve(text);
			}
			return text;
		}

		public string ReadBase()
		{
			int ofs = m_ofs;
			while (!IsAtEOL() && !IsAt('=') && !IsAt(';') && !IsAt('{') && !IsAt('}') && !IsAtComment())
			{
				Skip();
			}
			string text = ((ofs == m_ofs) ? "" : m_str.Substring(ofs, m_ofs - ofs).TrimEnd());
			UniqueStrings uniqueStrings = m_dt?.unique_strings;
			if (uniqueStrings != null)
			{
				text = uniqueStrings.Resolve(text);
			}
			return text;
		}

		public string ReadQuotedString()
		{
			int ofs = m_ofs;
			char c = Char();
			if (c != '"' && c != '\'')
			{
				return null;
			}
			Skip();
			while (allow_multi_line_quotted_strings || !IsAtEOL())
			{
				bool num = IsAt(c);
				Skip();
				if (num)
				{
					if (!allow_multi_line_quotted_strings || !IsAt(c))
					{
						break;
					}
					Skip();
				}
			}
			return m_str.Substring(ofs, m_ofs - ofs);
		}

		public string ReadMultiLineString(Field f, out Value value)
		{
			value = Value.Unknown;
			if (!IsAt('$') || Char(1) != '[')
			{
				return null;
			}
			int ofs = m_ofs;
			m_ofs += 2;
			string text = "";
			while (true)
			{
				SkipBlanks();
				while (ReadComment() != "")
				{
					SkipBlanks();
				}
				if (IsAt(']'))
				{
					Skip();
					string result = m_str.Substring(ofs, m_ofs - ofs);
					value = text;
					return result;
				}
				string text2 = ReadQuotedString();
				if (text2 == null)
				{
					break;
				}
				text += Unquote(text2);
			}
			if (f?.dt != null)
			{
				f.dt.Error(f, "Missing ']'");
			}
			else
			{
				Game.Log($"{f}: Missing ']'", Game.LogType.Error);
			}
			return null;
		}

		public string ReadValueList(Field f, out Value value)
		{
			value = Value.Unknown;
			if (!IsAt('['))
			{
				return null;
			}
			int ofs = m_ofs;
			Skip();
			List<SubValue> list = new List<SubValue>();
			int ofs2;
			while (true)
			{
				ofs2 = m_ofs;
				SkipBlanks();
				while (ReadComment() != "")
				{
					SkipBlanks();
				}
				if (IsAtEnd())
				{
					if (f?.dt != null)
					{
						f.dt.Error(f, "Missing ']'");
					}
					else
					{
						Game.Log($"{f}: Missing ']'", Game.LogType.Error);
					}
					return null;
				}
				if (IsAt(']'))
				{
					break;
				}
				SubValue subValue = new SubValue();
				if (m_ofs > ofs2)
				{
					subValue.comment1 = m_str.Substring(ofs2, m_ofs - ofs2);
				}
				subValue.value_str = ReadValue(f, out subValue.value, ",]");
				ofs2 = m_ofs;
				subValue.comment2 = ReadComment();
				list.Add(subValue);
				if (IsAt(','))
				{
					Skip();
					ofs2 = m_ofs;
					SkipSLBlanks();
					subValue.comment2 = ReadComment();
				}
				else if (!IsAt(']') && !IsAtEOL())
				{
					if (f?.dt != null)
					{
						f.dt.Error(f, "Missing ',' or ']'");
					}
					else
					{
						Game.Log($"{f}: Missing ',' or ']'", Game.LogType.Error);
					}
				}
				if (subValue.comment2 != "")
				{
					SkipNL();
				}
				else if (m_ofs > ofs2)
				{
					subValue.comment3 = m_str.Substring(ofs2, m_ofs - ofs2);
				}
			}
			if (m_ofs > ofs2 && list.Count > 0)
			{
				list[list.Count - 1].comment3 += m_str.Substring(ofs2, m_ofs - ofs2);
			}
			Skip();
			string result = m_str.Substring(ofs, m_ofs - ofs);
			value = new Value(list);
			return result;
		}

		public bool IsAtEndOfField(string delimiters)
		{
			if (!IsAtEOL() && !IsAtComment())
			{
				return delimiters.IndexOf(Char()) >= 0;
			}
			return true;
		}

		public void CheckAtEndOfField(Field f, string delimiters)
		{
			SkipSLBlanks();
			if (!IsAtEndOfField(delimiters) && f.dt != null)
			{
				f.dt.Error(f, "Unterminated field");
			}
		}

		public string ReadValue(Field f, out Value value, string delimiters, bool allow_slashed_lists = false)
		{
			value = Value.Unknown;
			string text = ReadQuotedString();
			if (text != null)
			{
				CheckAtEndOfField(f, delimiters);
				value = Unquote(text);
				return text;
			}
			string text2 = ReadMultiLineString(f, out value);
			if (text2 != null)
			{
				CheckAtEndOfField(f, delimiters);
				return text2;
			}
			string text3 = ReadValueList(f, out value);
			if (text3 != null)
			{
				CheckAtEndOfField(f, delimiters);
				return text3;
			}
			int ofs = m_ofs;
			while (!IsAtEndOfField(delimiters))
			{
				Skip();
			}
			int num = m_ofs;
			while (num > ofs && IsSLBlank(m_str[num - 1]))
			{
				num--;
			}
			if (ofs == num)
			{
				return "";
			}
			string text4 = m_str.Substring(ofs, num - ofs);
			value = ResolveValue(f, text4, allow_expressions: false, allow_slashed_lists);
			return text4;
		}

		public string ReadComment()
		{
			if (!IsAtComment())
			{
				return "";
			}
			int ofs = m_ofs;
			while (!IsAtEOL())
			{
				Skip();
			}
			return m_str.Substring(ofs, m_ofs - ofs);
		}

		public bool CheckConditionalDirective(Field f)
		{
			if (f.key != "#if")
			{
				return true;
			}
			Expression expression = Expression.Parse(f.value_str);
			if (expression == null || expression.type == Expression.Type.Invalid)
			{
				return false;
			}
			warn_slow_find = false;
			Value value = expression.Calc(f.dt?.context);
			warn_slow_find = true;
			return value.Bool();
		}

		public List<Field> ReadFields(Field parent, bool allow_conditional_directive = false)
		{
			List<Field> list = parent?.children;
			while (!IsAtEnd() && !IsAt('}'))
			{
				Field field = ReadField(parent);
				if (field == null)
				{
					break;
				}
				if (allow_conditional_directive)
				{
					allow_conditional_directive = false;
					if (!CheckConditionalDirective(field))
					{
						return null;
					}
				}
				if (list == null)
				{
					list = new List<Field>();
					if (parent != null)
					{
						parent.children = list;
					}
				}
				if (parent != null)
				{
					parent.AddChild(field);
				}
				else
				{
					list.Add(field);
				}
			}
			return list;
		}

		public static List<Field> ReadFile(DT dt, string path, Field parent = null)
		{
			if (path == null)
			{
				return null;
			}
			string text = ReadTextFile(path);
			if (text == null)
			{
				return null;
			}
			Checksum.FeedChecksum(text);
			return new Parser(dt, text).ReadFields(parent, parent != null);
		}

		public static Field LoadFieldFromFile(string path, string key)
		{
			Field field = LoadDefFile(null, path);
			if (field?.children == null)
			{
				return null;
			}
			if (ModManager.IsLoadingMod())
			{
				ExpandShortFormKeys(field);
			}
			for (int i = 0; i < field.children.Count; i++)
			{
				Field field2 = field.children[i];
				if (field2.key == key)
				{
					return field2;
				}
			}
			return null;
		}
	}

	public List<Field> files = new List<Field>();

	public Dictionary<string, Field> roots = new Dictionary<string, Field>();

	private Dictionary<string, Def> defs = new Dictionary<string, Def>();

	public List<string> errors = new List<string>();

	public DefsContext context;

	public int num_reloads;

	public UniqueStrings unique_strings;

	public static List<Field> tmp_fields_list = new List<Field>(64);

	public static readonly NumberFormatInfo number_format = new NumberFormatInfo
	{
		NumberDecimalSeparator = ".",
		NumberGroupSeparator = "",
		PositiveInfinitySymbol = "PositiveInfinity"
	};

	public static bool warn_slow_find = true;

	public static bool find_had_cases = false;

	private static List<string> tmp_ignore_keys = new List<string>();

	private static StringBuilder tmp_sb = new StringBuilder(128);

	public static bool import_forms_as_languages = false;

	public static string debug_load_text_key = null;

	public DT()
	{
		context = new DefsContext();
		context.dt = this;
		unique_strings = new UniqueStrings();
	}

	public void Reset()
	{
		files = new List<Field>();
		errors = new List<string>();
		foreach (KeyValuePair<string, Def> def in defs)
		{
			Def value = def.Value;
			value.field = null;
			value.defs = null;
		}
		roots.Clear();
		if (unique_strings != null)
		{
			unique_strings.Clear();
		}
		num_reloads++;
	}

	public static bool Convert(Value val, ref bool res)
	{
		if (!val.is_valid)
		{
			return false;
		}
		if (val.type == Value.Type.Int)
		{
			switch (val.int_val)
			{
			case 0:
				res = false;
				return true;
			case 1:
				res = true;
				return true;
			default:
				res = true;
				return false;
			}
		}
		if (val.type == Value.Type.String)
		{
			switch ((string)val.obj_val)
			{
			case "false":
			case "0":
				res = false;
				return true;
			case "true":
			case "1":
				res = true;
				return true;
			default:
				res = false;
				return false;
			}
		}
		res = val.Bool();
		return false;
	}

	public static bool Convert(Value val, ref int res)
	{
		if (!val.is_valid)
		{
			return false;
		}
		if (val.type == Value.Type.Int)
		{
			res = val.int_val;
			return true;
		}
		if (val.type == Value.Type.Float)
		{
			res = (int)val.float_val;
			return (float)res == val.float_val;
		}
		if (val.type == Value.Type.String)
		{
			if (!int.TryParse((string)val.obj_val, out var result))
			{
				return false;
			}
			res = result;
			return true;
		}
		return false;
	}

	public static bool Convert(Value val, ref float res)
	{
		if (!val.is_valid)
		{
			return false;
		}
		if (val.type == Value.Type.Int)
		{
			res = val.int_val;
			return true;
		}
		if (val.type == Value.Type.Float)
		{
			res = val.float_val;
			return true;
		}
		if (val.type == Value.Type.String)
		{
			if (!ParseFloat((string)val.obj_val, out var f))
			{
				return false;
			}
			res = f;
			return true;
		}
		return false;
	}

	public static bool Convert(Value val, ref Point res)
	{
		if (val.obj_val == null)
		{
			return false;
		}
		if (val.obj_val is Point)
		{
			res = (Point)val.obj_val;
			return true;
		}
		if (val.type == Value.Type.String)
		{
			if (!Logic.Point.TryParse((string)val.obj_val, out var pt))
			{
				return false;
			}
			res = pt;
			return true;
		}
		return false;
	}

	public static bool Convert(Value val, ref PPos res)
	{
		if (val.obj_val == null)
		{
			return false;
		}
		if (val.obj_val is PPos)
		{
			res = (PPos)val.obj_val;
			return true;
		}
		if (val.type == Value.Type.String)
		{
			if (!PPos.TryParse((string)val.obj_val, out var pt))
			{
				return false;
			}
			res = pt;
			return true;
		}
		return false;
	}

	public static bool Convert(Value val, ref string res)
	{
		if (!val.is_valid)
		{
			return false;
		}
		if (val.type == Value.Type.String)
		{
			res = (string)val.obj_val;
			return true;
		}
		if (val.type == Value.Type.Int)
		{
			res = val.int_val.ToString();
			return true;
		}
		if (val.type == Value.Type.Float)
		{
			res = FloatToStr(val.float_val);
			return true;
		}
		return false;
	}

	public static bool Bool(Value val, bool def_val = false)
	{
		bool res = def_val;
		Convert(val, ref res);
		return res;
	}

	public static int Int(Value val, int def_val = 0)
	{
		int res = def_val;
		Convert(val, ref res);
		return res;
	}

	public static float Float(Value val, float def_val = 0f)
	{
		float res = def_val;
		Convert(val, ref res);
		return res;
	}

	public static Point Point(Value val)
	{
		Point res = Logic.Point.Invalid;
		Convert(val, ref res);
		return res;
	}

	public static string String(Value val, string def_val = null)
	{
		string res = def_val;
		Convert(val, ref res);
		return res;
	}

	public static Value GetValue(Field field, IVars vars = null)
	{
		return field?.Value(vars) ?? Value.Unknown;
	}

	public static Value GetRandomValue(Field field, IVars vars = null)
	{
		return field?.RandomValue(vars) ?? Value.Unknown;
	}

	public Value GetValue(string path, IVars vars = null)
	{
		return GetValue(Find(path, vars), vars);
	}

	public Value GetRandomValue(string path, IVars vars = null)
	{
		return GetRandomValue(Find(path, vars), vars);
	}

	public Value GetValue(string def_id, string key, IVars vars = null)
	{
		Def def = FindDef(def_id);
		if (def == null || def.field == null)
		{
			return Value.Unknown;
		}
		return def.field.GetValue(key, vars);
	}

	public Value GetRandomValue(string def_id, string key, IVars vars = null)
	{
		Def def = FindDef(def_id);
		if (def == null || def.field == null)
		{
			return Value.Unknown;
		}
		return def.field.GetRandomValue(key, vars);
	}

	public static bool GetBool(Field field, IVars vars = null, bool def_val = false)
	{
		return field?.Bool(vars, def_val) ?? def_val;
	}

	public static bool GetRandomBool(Field field, IVars vars = null, bool def_val = false)
	{
		return field?.RandomBool(vars, def_val) ?? def_val;
	}

	public bool GetBool(string path, IVars vars = null, bool def_val = false)
	{
		return GetBool(Find(path, vars), vars, def_val);
	}

	public bool GetRandomBool(string path, IVars vars = null, bool def_val = false)
	{
		return GetRandomBool(Find(path, vars), vars, def_val);
	}

	public bool GetBool(string def_id, string key, IVars vars = null, bool def_val = false)
	{
		Def def = FindDef(def_id);
		if (def == null || def.field == null)
		{
			return def_val;
		}
		return def.field.GetBool(key, vars, def_val);
	}

	public bool GetRandomBool(string def_id, string key, IVars vars = null, bool def_val = false)
	{
		Def def = FindDef(def_id);
		if (def == null || def.field == null)
		{
			return def_val;
		}
		return def.field.GetRandomBool(key, vars, def_val);
	}

	public static int GetInt(Field field, IVars vars = null, int def_val = 0)
	{
		return field?.Int(vars, def_val) ?? def_val;
	}

	public static int GetRandomInt(Field field, IVars vars = null, int def_val = 0)
	{
		return field?.RandomInt(vars, def_val) ?? def_val;
	}

	public int GetInt(string path, IVars vars = null, int def_val = 0)
	{
		return GetInt(Find(path, vars), vars, def_val);
	}

	public int GetRandomInt(string path, IVars vars = null, int def_val = 0)
	{
		return GetRandomInt(Find(path, vars), vars, def_val);
	}

	public int GetInt(string def_id, string key, IVars vars = null, int def_val = 0)
	{
		Def def = FindDef(def_id);
		if (def == null || def.field == null)
		{
			return def_val;
		}
		return def.field.GetInt(key, vars, def_val);
	}

	public int GetRandomInt(string def_id, string key, IVars vars = null, int def_val = 0)
	{
		Def def = FindDef(def_id);
		if (def == null || def.field == null)
		{
			return def_val;
		}
		return def.field.GetRandomInt(key, vars, def_val);
	}

	public static float GetFloat(Field field, IVars vars = null, float def_val = 0f)
	{
		return field?.Float(vars, def_val) ?? def_val;
	}

	public static float GetRandomFloat(Field field, IVars vars = null, float def_val = 0f)
	{
		return field?.RandomFloat(vars, def_val) ?? def_val;
	}

	public float GetFloat(string path, IVars vars = null, float def_val = 0f)
	{
		return GetFloat(Find(path, vars), vars, def_val);
	}

	public float GetRandomFloat(string path, IVars vars = null, float def_val = 0f)
	{
		return GetRandomFloat(Find(path, vars), vars, def_val);
	}

	public float GetFloat(string def_id, string key, IVars vars = null, float def_val = 0f)
	{
		Def def = FindDef(def_id);
		if (def == null || def.field == null)
		{
			return def_val;
		}
		return def.field.GetFloat(key, vars, def_val);
	}

	public float GetRandomFloat(string def_id, string key, IVars vars = null, float def_val = 0f)
	{
		Def def = FindDef(def_id);
		if (def == null || def.field == null)
		{
			return def_val;
		}
		return def.field.GetRandomFloat(key, vars, def_val);
	}

	public static Point GetPoint(Field field, IVars vars = null)
	{
		return field?.Point(vars) ?? Logic.Point.Invalid;
	}

	public static Point GetRandomPoint(Field field, IVars vars = null)
	{
		return field?.RandomPoint(vars) ?? Logic.Point.Invalid;
	}

	public Point GetPoint(string path, IVars vars = null)
	{
		return GetPoint(Find(path, vars), vars);
	}

	public Point GetRandomPoint(string path, IVars vars = null)
	{
		return GetRandomPoint(Find(path, vars), vars);
	}

	public Point GetPoint(string def_id, string key, IVars vars = null)
	{
		Def def = FindDef(def_id);
		if (def == null || def.field == null)
		{
			return Logic.Point.Invalid;
		}
		return def.field.GetPoint(key, vars);
	}

	public Point GetRandomPoint(string def_id, string key, IVars vars = null)
	{
		Def def = FindDef(def_id);
		if (def == null || def.field == null)
		{
			return Logic.Point.Invalid;
		}
		return def.field.GetRandomPoint(key, vars);
	}

	public static string GetString(Field field, IVars vars = null, string def_val = "")
	{
		if (field == null)
		{
			return def_val;
		}
		return field.String(vars, def_val);
	}

	public static string GetRandomString(Field field, IVars vars = null, string def_val = "")
	{
		if (field == null)
		{
			return def_val;
		}
		return field.RandomString(vars, def_val);
	}

	public string GetString(string path, IVars vars = null, string def_val = "")
	{
		return GetString(Find(path, vars), vars, def_val);
	}

	public string GetRandomString(string path, IVars vars = null, string def_val = "")
	{
		return GetRandomString(Find(path, vars), vars, def_val);
	}

	public string GetString(string def_id, string key, IVars vars = null, string def_val = "")
	{
		Def def = FindDef(def_id);
		if (def == null || def.field == null)
		{
			return def_val;
		}
		return def.field.GetString(key, vars, def_val);
	}

	public string GetRandomString(string def_id, string key, IVars vars = null, string def_val = "")
	{
		Def def = FindDef(def_id);
		if (def == null || def.field == null)
		{
			return def_val;
		}
		return def.field.GetRandomString(key, vars, def_val);
	}

	public static bool IsQuoted(string s)
	{
		if (s == null)
		{
			return false;
		}
		if (s.Length < 2)
		{
			return false;
		}
		char c = s[0];
		if (c != '"' && c != '\'')
		{
			return false;
		}
		if (s[s.Length - 1] != c)
		{
			return false;
		}
		return true;
	}

	public static string Unquote(string s)
	{
		if (!IsQuoted(s))
		{
			return s;
		}
		return s.Substring(1, s.Length - 2).Replace("''", "\"");
	}

	public static string Enquote(string s)
	{
		if (IsQuoted(s))
		{
			return s;
		}
		if (s != null && (s.StartsWith("(", StringComparison.Ordinal) || s.StartsWith("[", StringComparison.Ordinal) || s.StartsWith("$[", StringComparison.Ordinal)))
		{
			return s;
		}
		return "\"" + s.Replace("\"", "''") + "\"";
	}

	private void Error(Field field, string text, Field field2 = null)
	{
		string text2 = "";
		if (field != null)
		{
			text2 = text2 + field.FilePath() + "(" + field.line + "): " + field.Path() + ": ";
		}
		text2 += text;
		if (field2 != null)
		{
			text2 = text2 + "\n" + field2.FilePath() + "(" + field2.line + "): " + field2.Path();
		}
		errors.Add(text2);
	}

	public void ResolveExtend(Field field)
	{
		if (field.type != "extend")
		{
			return;
		}
		if (field.base_path != null)
		{
			Error(field, "'extend' type field has base: '" + field.base_path + "'");
			field.base_path = null;
		}
		if (field.value_str != "")
		{
			Error(field, "'extend' type field has value: " + field.value_str);
		}
		field.extends = Find(field.key);
		if (field.extends == null)
		{
			Error(field, "Could not find extend target");
			return;
		}
		if (field.extends.extensions == null)
		{
			field.extends.extensions = new List<Field>();
		}
		field.extends.extensions.Add(field);
	}

	public void AddDef(Field field)
	{
		if (field.def != null || field.Type() != "def")
		{
			return;
		}
		string text = field.Path();
		if (string.IsNullOrEmpty(text))
		{
			Game.Log(field.Path(include_file: true) + ": def field with no key", Game.LogType.Error);
			return;
		}
		field.def = FindDef(text);
		if (field.def == null)
		{
			field.def = new Def();
			field.def.path = text;
			defs.Add(text, field.def);
		}
		field.def.field = field;
		Field field2 = field;
		while (field2.based_on != null)
		{
			field2 = field2.based_on;
			ResolveBase(field2);
		}
		if (field2 == field)
		{
			return;
		}
		if (field2.def == null)
		{
			AddDef(field2);
			if (field2.def == null)
			{
				Error(field, "Def is not based on def", field2);
				return;
			}
		}
		if (field2.def.defs == null)
		{
			field2.def.defs = new List<Def>();
		}
		field2.def.defs.Add(field.def);
	}

	public Field FindInParents(Field field, string key)
	{
		Field parent = field.parent;
		while (parent != null && parent.type != "file")
		{
			Field field2 = parent.FindChild(key);
			if (field2 != null && field2 != field)
			{
				return field2;
			}
			for (Field based_on = parent.based_on; based_on != null; based_on = based_on.based_on)
			{
				field2 = based_on.FindChild(key);
				if (field2 != null)
				{
					return field2;
				}
			}
			parent = parent.parent;
		}
		return Find(key);
	}

	private void ResolveBase(Field field)
	{
		if (field.based_on != null || field.base_path == "" || field.base_path == "null")
		{
			return;
		}
		if (field.base_path != null)
		{
			field.based_on = FindInParents(field, field.base_path);
			if (field.based_on == null)
			{
				Error(field, "Cannot find base field: " + field.base_path);
				return;
			}
			tmp_fields_list.Clear();
			for (Field field2 = field; field2 != null; field2 = field2.based_on)
			{
				if (tmp_fields_list.IndexOf(field2) >= 0)
				{
					field.based_on = null;
					Error(field, "Cyclic DT reference: " + field.base_path);
					break;
				}
				tmp_fields_list.Add(field2);
			}
		}
		else
		{
			if (field.key == "")
			{
				return;
			}
			Field field3 = field.parent;
			if (field3 == null)
			{
				return;
			}
			if (field3.extends != null)
			{
				field3 = field3.extends;
				if (!string.IsNullOrEmpty(field3.base_path) && field3.based_on == null)
				{
					ResolveBase(field3);
				}
			}
			if (field3.based_on != null)
			{
				field.based_on = field3.based_on.FindChild(field.key);
			}
			if (field.based_on != null || !(field.type != "case") || !(field3.Type() == "case"))
			{
				return;
			}
			Field parent = field3.parent;
			while (parent != null && parent.type != "file")
			{
				Field field4 = parent.FindChild(field.key);
				if (field4 != null)
				{
					field.based_on = field4;
					break;
				}
				if (!(parent.Type() != "case"))
				{
					parent = parent.parent;
					continue;
				}
				break;
			}
		}
	}

	public static bool ParseFloat(string s, out float f)
	{
		return float.TryParse(s, NumberStyles.Float, number_format, out f);
	}

	public static bool ParseInt(string s, out int i)
	{
		return int.TryParse(s, NumberStyles.Integer, number_format, out i);
	}

	public static bool ParseBool(string s, out bool b)
	{
		return bool.TryParse(s, out b);
	}

	public static float ParseFloat(string s, float def_val = 0f)
	{
		if (ParseFloat(s, out var f))
		{
			return f;
		}
		return def_val;
	}

	public static string FloatToStr(float f, int max_precision = int.MaxValue)
	{
		if (max_precision != int.MaxValue)
		{
			if (max_precision < 0)
			{
				max_precision = ((!(f > 10f) && !(f < -10f)) ? 1 : 0);
			}
			f = (float)Math.Round(f, max_precision, MidpointRounding.AwayFromZero);
		}
		return f.ToString(CultureInfo.InvariantCulture);
	}

	public static float Round(float val)
	{
		val = ((!(val > 10f) && !(val < -10f)) ? ((float)Math.Round(val, 1, MidpointRounding.AwayFromZero)) : ((float)Math.Round(val, MidpointRounding.AwayFromZero)));
		return val;
	}

	public static Field ResolveReference(Field field, string path)
	{
		while (true)
		{
			if (field == null)
			{
				return null;
			}
			Field field2 = field.FindChild(path);
			if (field2 != null)
			{
				return field2;
			}
			if (field.dt != null && field.type == "file")
			{
				break;
			}
			field = field.parent;
		}
		return field.dt.Find(path);
	}

	public static List<SubValue> ResolveSlashedList(Field field, string sval)
	{
		int num = sval.IndexOf('/');
		if (num < 0)
		{
			return null;
		}
		List<SubValue> list = new List<SubValue>();
		int num2 = 0;
		int length = sval.Length;
		while (true)
		{
			SubValue subValue = new SubValue();
			subValue.value_str = sval.Substring(num2, num - num2).Trim();
			subValue.value = ResolveValue(field, subValue.value_str, allow_expressions: false);
			list.Add(subValue);
			if (num == length)
			{
				break;
			}
			num2 = num + 1;
			num = sval.IndexOf('/', num2);
			if (num < 0)
			{
				num = length;
			}
		}
		return list;
	}

	public static Value ResolveValue(Field field, string sval, bool allow_expressions, bool allow_slashed_lists = false)
	{
		if (string.IsNullOrEmpty(sval))
		{
			return Value.Unknown;
		}
		if (IsQuoted(sval))
		{
			return Unquote(sval);
		}
		switch (sval)
		{
		case "null":
			return Value.Null;
		case "false":
			return 0;
		case "true":
			return 1;
		default:
		{
			if (int.TryParse(sval, out var result))
			{
				return result;
			}
			if (ParseFloat(sval, out var f))
			{
				return f;
			}
			bool flag = sval.StartsWith("(", StringComparison.Ordinal) || sval.StartsWith("#", StringComparison.Ordinal) || sval.StartsWith("?", StringComparison.Ordinal);
			if (!flag && field != null && field.Type() == "color")
			{
				return sval;
			}
			if (Logic.Point.TryParse(sval, out var pt))
			{
				return new Value(pt);
			}
			if (!flag && allow_slashed_lists)
			{
				List<SubValue> list = ResolveSlashedList(field, sval);
				if (list != null)
				{
					field.flags |= Field.Flags.SlashedList;
					return new Value(list);
				}
			}
			if (!allow_expressions)
			{
				return Value.Unknown;
			}
			if (flag)
			{
				Expression expression = Expression.Parse(sval, allow_compilation: false);
				if (expression != null && expression.type != Expression.Type.Invalid)
				{
					if (expression.type == Expression.Type.Operator && expression.operands.Count == 1 && expression.value.obj_val == Expression.Operator.Get("()"))
					{
						expression = expression.operands[0];
					}
					expression.Compile();
					return new Value(expression);
				}
				if (field != null && field.dt != null)
				{
					field.dt.Error(field, "Invalid expression: '" + sval + "'");
				}
				return Value.Unknown;
			}
			Field field2 = ResolveReference(field, sval);
			if (field2 != null)
			{
				return new Value(field2);
			}
			if (field != null && field.dt != null)
			{
				field.dt.Error(field, "Cannot resolve value: '" + sval + "'");
			}
			return Value.Unknown;
		}
		}
	}

	public void PostProcess1(Field field)
	{
		ResolveExtend(field);
		if (field.children != null)
		{
			for (int i = 0; i < field.children.Count; i++)
			{
				Field field2 = field.children[i];
				PostProcess1(field2);
			}
		}
	}

	public void AddChildrenToRoots(Field file)
	{
		for (int i = 0; i < file.children.Count; i++)
		{
			Field field = file.children[i];
			if (!(field.key == "") && !(field.key == "#if") && !(field.type == "extend"))
			{
				if (roots.TryGetValue(field.key, out var value))
				{
					Error(field, "Duplicated DT key", value);
				}
				else
				{
					roots.Add(field.key, field);
				}
			}
		}
	}

	private void DetectDuplicateKeys(Field field)
	{
		if (field.parent == null)
		{
			return;
		}
		Field field2 = ((field.extends != null) ? field.extends : field);
		for (int i = 0; i < field.children.Count; i++)
		{
			Field field3 = field.children[i];
			if (!(field3.key == ""))
			{
				Field field4 = field2.FindChild(field3.key, null, allow_base: false, allow_extensions: true, allow_switches: false, ' ');
				if (field3 != field4)
				{
					Error(field3, "Duplicated DT key", field4);
				}
			}
		}
	}

	private void PostProcess2(Field field)
	{
		if (field.parent != null && field.parent.type != "file")
		{
			if (field.type == "case")
			{
				field.parent.flags |= Field.Flags.HasCases;
			}
			else if (field.key == "vars")
			{
				field.parent.flags |= Field.Flags.HasVars;
			}
		}
		ResolveBase(field);
		AddDef(field);
		if (field.children != null)
		{
			for (int i = 0; i < field.children.Count; i++)
			{
				Field field2 = field.children[i];
				PostProcess2(field2);
			}
			DetectDuplicateKeys(field);
		}
	}

	public static void ResolveValue(Field field, bool recursive = false)
	{
		if (field.value.is_unknown)
		{
			field.value = ResolveValue(field, field.value_str, allow_expressions: true, allow_slashed_lists: true);
		}
		if (field.value.obj_val is List<SubValue> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				SubValue subValue = list[i];
				if (subValue.value.is_unknown)
				{
					subValue.value = ResolveValue(field, subValue.value_str, allow_expressions: true);
				}
			}
		}
		if (recursive && field.children != null)
		{
			for (int j = 0; j < field.children.Count; j++)
			{
				ResolveValue(field.children[j], recursive);
			}
		}
	}

	private void ValidateTextValue(Field f)
	{
		if (f.value.type != Value.Type.Object)
		{
			return;
		}
		if (!(f.value.obj_val is List<SubValue> list))
		{
			Error(f, $"Invalid text value: '{f.value_str}' -> {f.value}");
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			SubValue subValue = list[i];
			if (subValue.value.type == Value.Type.Object)
			{
				Error(f, $"Invalid text sub-value {i}: '{subValue.value_str}' -> {subValue.value}");
			}
		}
	}

	private void ValidateTextField(Field f)
	{
		if (f.Type() != "text")
		{
			return;
		}
		ValidateTextValue(f);
		if (f.children == null)
		{
			return;
		}
		for (int i = 0; i < f.children.Count; i++)
		{
			Field field = f.children[i];
			if (field.Type() != "")
			{
				Error(field, "Text form field has type: " + field.Type());
			}
			if (field.children != null)
			{
				Error(field, "Text form has children");
			}
			ValidateTextValue(field);
		}
	}

	private void PostProcess3(Field field)
	{
		ResolveValue(field);
		if (field.children != null)
		{
			for (int i = 0; i < field.children.Count; i++)
			{
				Field field2 = field.children[i];
				PostProcess3(field2);
			}
		}
		ValidateTextField(field);
	}

	private void PostProcessModFiles(Field file)
	{
		int num = file.children.Count;
		for (int i = 0; i < num; i++)
		{
			Field field = file.children[i];
			if (field.key == "")
			{
				continue;
			}
			if (roots.TryGetValue(field.key, out var value))
			{
				value.Mod(field);
				file.DelChild(field);
				i--;
				num--;
				continue;
			}
			if (field.type == "delete")
			{
				if (ModManager.IsPureDelete(field))
				{
					file.DelChild(field);
					i--;
					num--;
					continue;
				}
				field.type = "";
			}
			Field field2 = file.AddChild(field.key);
			field2.line = field.line;
			roots.Add(field.key, field2);
			field2.Mod(field);
			file.DelChild(field);
			i--;
			num--;
		}
		files.Add(file);
	}

	public void PostProcess()
	{
		foreach (Field file in files)
		{
			PostProcess1(file);
		}
		List<Mod> list = ModManager.Get()?.GetActiveMods();
		if (list != null)
		{
			foreach (Mod item in list)
			{
				foreach (Field def_file in (ModManager.LoadingMod = item).def_files)
				{
					PostProcessModFiles(def_file);
				}
				ModManager.LoadingMod = null;
			}
		}
		for (int i = 0; i < files.Count; i++)
		{
			Field field = files[i];
			PostProcess2(field);
		}
		for (int j = 0; j < files.Count; j++)
		{
			Field field2 = files[j];
			PostProcess3(field2);
		}
		tmp_fields_list.Clear();
	}

	public void PostProcessFile(Field file)
	{
		PostProcess1(file);
		PostProcess2(file);
		PostProcess3(file);
	}

	public Field Find(string path, IVars vars = null)
	{
		find_had_cases = false;
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		if (roots != null)
		{
			int num = path.IndexOf('.');
			string key;
			string text;
			if (num > 0)
			{
				key = path.Substring(0, num);
				text = path.Substring(num + 1);
			}
			else
			{
				key = path;
				text = null;
			}
			if (!roots.TryGetValue(key, out var value))
			{
				return null;
			}
			if (text == null)
			{
				return value;
			}
			return value.FindChild(text, vars);
		}
		if (warn_slow_find)
		{
			Game.Log("Using slow version of DT.Find()", Game.LogType.Warning);
		}
		for (int num2 = files.Count - 1; num2 >= 0; num2--)
		{
			Field field = files[num2].FindChild(path, vars);
			if (field != null)
			{
				return field;
			}
		}
		return null;
	}

	public Def FindDef(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		Def value = null;
		defs.TryGetValue(path, out value);
		return value;
	}

	public static Field LoadDefFile(DT dt, string path)
	{
		Field field = new Field(dt);
		field.type = "file";
		field.key = System.IO.Path.GetFileName(path);
		field.value_str = Enquote(path);
		field.children = Parser.ReadFile(dt, path, field);
		if (field.children == null)
		{
			return null;
		}
		dt?.AddFile(field);
		return field;
	}

	public Field LoadDefFile(string path)
	{
		return LoadDefFile(this, path);
	}

	public static bool ExpandShortFormKey(Field field)
	{
		if (field == null || string.IsNullOrEmpty(field.key))
		{
			return false;
		}
		ExpandShortFormKeys(field.children);
		int num = field.key.IndexOf('.');
		if (num < 0)
		{
			return false;
		}
		int line = field.line;
		string type = field.type;
		string key = field.key;
		string base_path = field.base_path;
		Field based_on = field.based_on;
		string value_str = field.value_str;
		Value value = field.value;
		List<Field> children = field.children;
		Field field2;
		bool result;
		if (field.parent != null)
		{
			field2 = field.parent.FindOrAddChild(key, null, allowBase: true, allowExtensions: true, allowSwitches: true, '.', line);
			result = true;
		}
		else
		{
			string text = field.key.Substring(0, num);
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			string text2 = field.key.Substring(num + 1);
			if (string.IsNullOrEmpty(text2))
			{
				return false;
			}
			field.type = "";
			field.key = text;
			field.base_path = null;
			field.based_on = null;
			field.value = Value.Unknown;
			field.value_str = string.Empty;
			field.children = null;
			field2 = field.FindOrAddChild(text2, null, allowBase: true, allowExtensions: true, allowSwitches: true, '.', line);
			result = false;
		}
		field2.line = line;
		field2.type = type;
		field2.base_path = base_path;
		field2.based_on = based_on;
		field2.value_str = value_str;
		field2.value = value;
		field2.children = children;
		if (children != null)
		{
			for (int i = 0; i < children.Count; i++)
			{
				children[i].parent = field2;
			}
		}
		return result;
	}

	public void AddFile(Field file)
	{
		if (file.children != null)
		{
			if (ModManager.IsLoadingMod())
			{
				ExpandShortFormKeys(file);
				ModManager.Get();
				ModManager.LoadingMod?.def_files.Add(file);
			}
			else
			{
				files.Add(file);
				AddChildrenToRoots(file);
			}
		}
	}

	public static void ExpandShortFormKeys(Field file)
	{
		ExpandShortFormKeys(file?.children);
	}

	public static void ExpandShortFormKeys(List<Field> fields)
	{
		if (fields == null)
		{
			return;
		}
		int num = fields.Count;
		for (int i = 0; i < num; i++)
		{
			Field field = fields[i];
			if (!ExpandShortFormKey(field))
			{
				continue;
			}
			if (field.parent != null)
			{
				if (fields == field.parent.children)
				{
					i--;
					num--;
				}
				field.parent.DelChild(field);
			}
			else
			{
				fields.RemoveAt(i);
				i--;
				num--;
			}
		}
	}

	public static Field ParseCSV(DT dt, string path, string text, char delimiter = '\0', string value_key = "<value>", string empty_value = "")
	{
		Table table = Table.FromString(text, delimiter, path);
		if (table == null)
		{
			return null;
		}
		delimiter = table.delimiter;
		if (table.NumCols < 1 || table.NumRows < 1)
		{
			return null;
		}
		string text2 = table.Get(0, 0);
		string text3 = text2;
		for (int i = 1; i < table.NumCols; i++)
		{
			string text4 = table.Get(0, i);
			text3 = text3 + delimiter + text4;
		}
		Field field = new Field(dt);
		field.type = "file";
		field.key = System.IO.Path.GetFileName(path);
		field.value_str = Enquote(path);
		field.comment1 = text3;
		field.children = new List<Field>();
		dt?.AddFile(field);
		if (text2.IndexOf('*') < 0)
		{
			text2 += "_*";
		}
		for (int j = 1; j < table.NumRows; j++)
		{
			string text5 = table.Get(j, 0);
			Field field2 = null;
			if (text5.StartsWith("//", StringComparison.Ordinal))
			{
				field2 = new Field(dt);
				field2.comment1 = text5;
			}
			else if (text5 == "")
			{
				field2 = new Field(dt);
			}
			else
			{
				string str = text2.Replace("*", text5);
				field2 = new Parser(dt, str).ReadField(field);
			}
			if (field2 == null)
			{
				continue;
			}
			field2.line = j + 1;
			field2.comment2 = text5;
			field.AddChild(field2);
			field2.children = new List<Field>();
			for (int k = 1; k < table.NumCols; k++)
			{
				text5 = table.Get(0, k);
				string text6 = table.Get(j, k);
				if (text6 == empty_value)
				{
					text6 = "";
				}
				Value value;
				if (dt == null)
				{
					value = ((!string.IsNullOrEmpty(text6)) ? ((Value)text6) : Value.Unknown);
				}
				else
				{
					value = ResolveValue(field2, text6, allow_expressions: false);
					if (value.is_unknown && !string.IsNullOrEmpty(text6))
					{
						value = Unquote(text6);
					}
				}
				if (text5 == value_key)
				{
					field2.value_str = text6;
					field2.value = value;
					continue;
				}
				Field field3 = new Field(dt);
				field3.line = j + 1;
				field3.key = text5;
				field3.value_str = text6;
				field3.value = value;
				field2.AddChild(field3);
			}
		}
		return field;
	}

	public static string ReadTextFile(string path)
	{
		string result = null;
		try
		{
			using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
			result = streamReader.ReadToEnd();
		}
		catch (FileNotFoundException)
		{
		}
		catch (DirectoryNotFoundException)
		{
		}
		catch (Exception ex3)
		{
			Game.Log("Error reading " + path + ": " + ex3.Message, Game.LogType.Error);
		}
		return result;
	}

	public static Field LoadCSVFile(DT dt, string path, char delimiter = '\0', string value_key = "<value>", string empty_value = "")
	{
		string text = ReadTextFile(path);
		if (dt != null)
		{
			Checksum.FeedChecksum(text);
		}
		if (text == null)
		{
			return null;
		}
		return ParseCSV(dt, path, text, delimiter, value_key, empty_value);
	}

	public static void PostProcessCSVTexts(Field root)
	{
		if (root.children.Count != 0)
		{
			Field default_row = null;
			Field field = root.children[0];
			if (field.comment2 == "default")
			{
				default_row = field;
				root.children.RemoveAt(0);
			}
			for (int i = 0; i < root.children.Count; i++)
			{
				PostProcessCSVTextField(root.children[i], default_row);
			}
		}
	}

	private static void ReplaceFormVars(Field field, Field child, List<string> ignore_keys = null)
	{
		if (child == field)
		{
			return;
		}
		if (field?.key == debug_load_text_key || field?.parent?.key == debug_load_text_key)
		{
			Game.Log($"Replacing form vars: {field}", Game.LogType.Error);
		}
		if (ignore_keys == null)
		{
			tmp_ignore_keys.Clear();
			ignore_keys = tmp_ignore_keys;
		}
		string key = child.key;
		if (ignore_keys.IndexOf(key) >= 0)
		{
			return;
		}
		ignore_keys.Add(key);
		string value_str = child.value_str;
		tmp_sb.Clear();
		StringBuilder stringBuilder = tmp_sb;
		int num = 0;
		int num2 = 0;
		while (num2 < value_str.Length)
		{
			int num3 = value_str.IndexOf('{', num2);
			if (num3 < 0)
			{
				break;
			}
			int num4 = value_str.IndexOf('}', num3);
			if (num4 < 0)
			{
				break;
			}
			key = value_str.Substring(num3 + 1, num4 - num3 - 1);
			Field field2 = ((key == "base") ? field : field.FindChild(key));
			if (field2 == null)
			{
				num2 = num4 + 1;
				continue;
			}
			ReplaceFormVars(field, field2, ignore_keys);
			string value_str2 = field2.value_str;
			if (num3 > num)
			{
				stringBuilder.Append(value_str, num, num3 - num);
			}
			stringBuilder.Append(value_str2);
			num = (num2 = num4 + 1);
		}
		if (num != 0)
		{
			if (num < value_str.Length)
			{
				stringBuilder.Append(value_str, num, value_str.Length - num);
			}
			child.value_str = stringBuilder.ToString();
			child.value = child.value_str;
		}
	}

	private static void PostProcessCSVTextField(Field field, Field default_row)
	{
		if (field?.key == debug_load_text_key || field?.parent?.key == debug_load_text_key)
		{
			Game.Log($"post-processing {field}", Game.LogType.Message);
		}
		if (field.base_path != null)
		{
			field.key = field.key + ":" + field.base_path;
			field.base_path = null;
		}
		if (field.type != "")
		{
			Game.Log(field.Path(include_file: true) + " has type '" + field.type + "' - spaces in the key are not allowed", Game.LogType.Error);
		}
		if (field.key == "" || field.children == null)
		{
			return;
		}
		if (default_row != null)
		{
			for (int i = 0; i < field.children.Count; i++)
			{
				Field field2 = field.children[i];
				if (!(field2.key == "") && field2.value_str == "")
				{
					field2.value_str = default_row.GetValueStr(field2.key);
					field2.value = Value.Null;
				}
			}
		}
		if (!import_forms_as_languages)
		{
			bool flag = false;
			for (int j = 0; j < field.children.Count; j++)
			{
				Field field3 = field.children[j];
				if (field3.key == "")
				{
					continue;
				}
				if (field3.key[0] == ':')
				{
					string text = field3.key.Substring(1);
					if (field.FindChild(text) != null)
					{
						Game.Log(field.Path(include_file: true) + " has duplicated forms: '" + text + "' and ':" + text + "'", Game.LogType.Error);
					}
					field3.key = text;
					if (field.children_by_key != null)
					{
						field.children_by_key = null;
						flag = true;
					}
				}
				ReplaceFormVars(field, field3);
			}
			if (flag)
			{
				field.BuildChildrenIndex();
			}
		}
		for (int k = 0; k < field.children.Count; k++)
		{
			Field field4 = field.children[k];
			if (!(field4.key == "") && !field4.value.is_valid)
			{
				field4.value = ResolveValue(field4, field4.value_str, allow_expressions: false);
				if (!field4.value.is_valid && !string.IsNullOrEmpty(field4.value_str))
				{
					field4.value = field4.value_str;
				}
			}
		}
	}

	private static int ReadVersion(string s, int idx)
	{
		int num = 0;
		while (idx < s.Length)
		{
			char c = s[idx];
			if (c < '0' || c > '9')
			{
				break;
			}
			num = 10 * num + c - 48;
			idx++;
		}
		return num;
	}

	public static int CompareFileNames(string fn1, string fn2)
	{
		StringComparer invariantCultureIgnoreCase = StringComparer.InvariantCultureIgnoreCase;
		int num = fn1.IndexOf(';');
		int num2 = fn2.IndexOf(';');
		if (num < 0 && num2 < 0)
		{
			return invariantCultureIgnoreCase.Compare(fn1, fn2);
		}
		if (num < 0)
		{
			return -1;
		}
		if (num2 < 0)
		{
			return 1;
		}
		int num3 = ReadVersion(fn1, num + 1);
		int value = ReadVersion(fn2, num2 + 1);
		int num4 = num3.CompareTo(value);
		if (num4 != 0)
		{
			return num4;
		}
		return invariantCultureIgnoreCase.Compare(fn1, fn2);
	}

	public static int CompareFileNames(FileInfo a, FileInfo b)
	{
		return CompareFileNames(a.Name, b.Name);
	}

	public static int CompareFileNames(DirectoryInfo a, DirectoryInfo b)
	{
		return CompareFileNames(a.Name, b.Name);
	}

	public static List<Field> ReadMapsDef(DT dt, string relativePath, Field parent = null)
	{
		List<Field> list = Parser.ReadFile(dt, Game.maps_path + relativePath, parent);
		ModManager modManager = ModManager.Get();
		if (modManager == null)
		{
			return list;
		}
		foreach (Mod activeMod in modManager.GetActiveMods())
		{
			if (!activeMod.has_maps)
			{
				continue;
			}
			ModManager.LoadingMod = activeMod;
			List<Field> list2 = Parser.ReadFile(dt, activeMod.maps_path + relativePath, parent);
			ModManager.LoadingMod = null;
			if (list2 == null)
			{
				continue;
			}
			ExpandShortFormKeys(list2);
			foreach (Field dtmElem in list2)
			{
				Field field = list.Find((Field dtElem) => dtElem.key == dtmElem.key);
				if (field != null)
				{
					field.Mod(dtmElem);
				}
				else
				{
					list.Add(dtmElem);
				}
			}
		}
		return list;
	}

	public static string GetMapFileFullPath(string relativePath)
	{
		string result = Game.maps_path + relativePath;
		ModManager modManager = ModManager.Get();
		if (modManager != null)
		{
			List<Mod> activeMods = modManager.GetActiveMods();
			for (int num = activeMods.Count - 1; num >= 0; num--)
			{
				Mod mod = activeMods[num];
				if (mod.has_maps)
				{
					string text = mod.maps_path + relativePath;
					if (new FileInfo(text).Exists)
					{
						return text;
					}
				}
			}
		}
		return result;
	}

	public static Field ReadMapsCsv(DT dt, string relativePath, char delimiter = '\0', string valueKey = "<value>", string emptyValue = "")
	{
		Field field = LoadCSVFile(dt, Game.maps_path + relativePath, delimiter, valueKey, emptyValue);
		ModManager modManager = ModManager.Get();
		if (modManager == null)
		{
			return field;
		}
		foreach (Mod activeMod in modManager.GetActiveMods())
		{
			if (activeMod.has_maps)
			{
				ModManager.LoadingMod = activeMod;
				Field field2 = LoadCSVFile(dt, activeMod.maps_path + relativePath, delimiter, valueKey, emptyValue);
				ModManager.LoadingMod = null;
				if (field2 != null)
				{
					field.Mod(field2);
				}
			}
		}
		return field;
	}

	public void LoadDirInternal(DirectoryInfo di, string path, SkipFileFunc skip_func = null)
	{
		if (!di.Exists)
		{
			Game.Log("Failed to find directory: " + path, Game.LogType.Warning);
			return;
		}
		FileInfo[] array = di.GetFiles();
		Array.Sort(array, CompareFileNames);
		foreach (FileInfo fileInfo in array)
		{
			if (fileInfo.Extension.Equals(".def", StringComparison.OrdinalIgnoreCase))
			{
				string text = fileInfo.Name.ToLowerInvariant();
				if (skip_func == null || !skip_func(path, text, is_directory: false))
				{
					string path2 = path + text;
					LoadDefFile(path2);
				}
			}
		}
		foreach (FileInfo fileInfo2 in array)
		{
			if (fileInfo2.Extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = fileInfo2.Name.ToLowerInvariant();
				if (skip_func == null || !skip_func(path, text2, is_directory: false))
				{
					string path3 = path + text2;
					LoadCSVFile(this, path3);
				}
			}
		}
		DirectoryInfo[] directories = di.GetDirectories();
		Array.Sort(directories, CompareFileNames);
		foreach (DirectoryInfo directoryInfo in directories)
		{
			string text3 = directoryInfo.Name.ToLowerInvariant();
			if (skip_func == null || !skip_func(path, text3, is_directory: true))
			{
				string path4 = path + text3 + "/";
				LoadDirInternal(directoryInfo, path4, skip_func);
			}
		}
	}

	public void LoadDir(string path, SkipFileFunc skip_func = null)
	{
		path = FS.EnforceDirectorySeparator(path);
		DirectoryInfo di = new DirectoryInfo(path);
		LoadDirInternal(di, path, skip_func);
	}

	public void LoadModDir(Mod mod, string path, SkipFileFunc skipFunc = null)
	{
		if (!string.IsNullOrEmpty(path))
		{
			ModManager.LoadingMod = mod;
			LoadDir(path, skipFunc);
			ModManager.LoadingMod = null;
		}
	}

	public void LoadActiveMods(SkipFileFunc skip_map_def = null)
	{
		ModManager modManager = ModManager.Get();
		if (modManager == null)
		{
			return;
		}
		foreach (Mod activeMod in modManager.GetActiveMods())
		{
			if (activeMod.has_defs)
			{
				LoadModDir(activeMod, activeMod.defs_path);
			}
			if (activeMod.has_maps)
			{
				LoadModDir(activeMod, activeMod.maps_path, skip_map_def);
			}
		}
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Field field = Find(key);
		if (field == null)
		{
			return Value.Unknown;
		}
		if (!as_value)
		{
			return new Value(field);
		}
		return field.Value();
	}
}
