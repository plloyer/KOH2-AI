using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Logic;

public class Expression
{
	public enum Type
	{
		Invalid,
		Constant,
		Variable,
		Operator,
		Precompiled
	}

	public interface Context
	{
		Value GetVar(object obj, string var_name, bool as_value = true);

		Value Call(object obj, string func_name, object[] args, bool as_value = true);

		Value GetByIndex(object obj, object[] indicies, bool as_value = true);

		string Dump(string ident = "", string new_line = "\n");
	}

	public delegate Value CompiledExpression(Context context, bool as_value);

	public class Operator
	{
		public delegate Value UnaryFunc(Value operand);

		public delegate Value BinaryFunc(Value operand1, Value operand2);

		public string name;

		public int priority;

		public UnaryFunc unary_func;

		public BinaryFunc binary_func;

		public static Dictionary<string, Operator> registry = new Dictionary<string, Operator>();

		public Operator(string name, int priority, BinaryFunc binary_func, UnaryFunc unary_func = null)
		{
			this.name = name;
			this.priority = priority;
			this.unary_func = unary_func;
			this.binary_func = binary_func;
			registry.Add(name, this);
		}

		public static Operator Get(string name)
		{
			Init();
			if (registry.TryGetValue(name, out var value))
			{
				return value;
			}
			return null;
		}

		public virtual Value Calc(Context context, List<Expression> operands, bool as_value)
		{
			if (operands == null || operands.Count == 0)
			{
				return Value.Unknown;
			}
			if (operands.Count == 1)
			{
				if (unary_func == null)
				{
					return Value.Unknown;
				}
				Value operand = operands[0].CalcInner(context);
				return unary_func(operand);
			}
			if (operands.Count == 2)
			{
				if (binary_func == null)
				{
					return Value.Unknown;
				}
				Value operand2 = operands[0].CalcInner(context);
				Value operand3 = operands[1].CalcInner(context);
				return binary_func(operand2, operand3);
			}
			return Value.Unknown;
		}

		public virtual System.Linq.Expressions.Expression GenerateLINQ(List<Expression> operands)
		{
			if (operands == null || operands.Count == 0)
			{
				return System.Linq.Expressions.Expression.Constant(Value.Unknown);
			}
			if (operands.Count == 1)
			{
				if (unary_func == null)
				{
					return System.Linq.Expressions.Expression.Constant(Value.Unknown);
				}
				System.Linq.Expressions.Expression lINQ = operands[0].GetLINQ();
				if (lINQ == null)
				{
					return null;
				}
				return System.Linq.Expressions.Expression.Call(unary_func.Method, lINQ);
			}
			if (operands.Count == 2)
			{
				if (binary_func == null)
				{
					return System.Linq.Expressions.Expression.Constant(Value.Unknown);
				}
				System.Linq.Expressions.Expression lINQ2 = operands[0].GetLINQ();
				if (lINQ2 == null)
				{
					return null;
				}
				System.Linq.Expressions.Expression lINQ3 = operands[1].GetLINQ();
				if (lINQ3 == null)
				{
					return null;
				}
				return System.Linq.Expressions.Expression.Call(binary_func.Method, lINQ2, lINQ3);
			}
			return System.Linq.Expressions.Expression.Constant(Value.Unknown);
		}

		public override string ToString()
		{
			return name;
		}

		public virtual string ToString(List<Expression> operands)
		{
			if (operands == null || operands.Count == 0)
			{
				return name;
			}
			if (operands.Count == 1)
			{
				return name + operands[0].ToString();
			}
			if (operands.Count == 2)
			{
				return operands[0].ToString() + " " + name + " " + operands[1].ToString();
			}
			string text = "";
			for (int i = 0; i < operands.Count; i++)
			{
				string text2 = operands[i].ToString();
				text = ((!(text == "")) ? (text + ", " + text2) : text2);
			}
			return name + "(" + text + ")";
		}
	}

	public class OperatorDot : Operator
	{
		public OperatorDot()
			: base(".", 2, null)
		{
		}

		public override Value Calc(Context context, List<Expression> operands, bool as_value)
		{
			if (operands.Count != 2)
			{
				return Value.Unknown;
			}
			if (operands[1].type != Type.Variable)
			{
				return Value.Unknown;
			}
			if (context == null)
			{
				return Value.Unknown;
			}
			Expression expression = operands[0];
			Expression expression2 = operands[1];
			Value value = expression.CalcInner(context, as_value: false);
			if (!value.is_object)
			{
				return expression2.result = Value.Unknown;
			}
			return expression2.result = context.GetVar(value.obj_val, expression2.value, as_value);
		}

		public override System.Linq.Expressions.Expression GenerateLINQ(List<Expression> operands)
		{
			if (operands.Count != 2)
			{
				return System.Linq.Expressions.Expression.Constant(Value.Unknown);
			}
			if (operands[1].type != Type.Variable)
			{
				return System.Linq.Expressions.Expression.Constant(Value.Unknown);
			}
			System.Linq.Expressions.Expression lINQ = operands[0].GetLINQ();
			return System.Linq.Expressions.Expression.Call(typeof(OperatorDot), "_GetVar", null, context_param, lINQ, System.Linq.Expressions.Expression.Constant((string)operands[1].value.obj_val), as_value_param);
		}

		private static Value _GetVar(Context context, Value obj, string var_name, bool as_value)
		{
			if (context == null)
			{
				return Value.Unknown;
			}
			if (!obj.is_object)
			{
				return Value.Unknown;
			}
			return context.GetVar(obj.obj_val, var_name, as_value);
		}

		public override string ToString(List<Expression> operands)
		{
			if (operands.Count != 2)
			{
				return base.ToString(operands);
			}
			return operands[0].ToString() + name + operands[1].ToString();
		}
	}

	public class OperatorBrackets : Operator
	{
		public OperatorBrackets()
			: base("()", -1, null)
		{
		}

		public override Value Calc(Context context, List<Expression> operands, bool as_value)
		{
			if (operands.Count != 1)
			{
				return Value.Unknown;
			}
			return operands[0].CalcInner(context, as_value);
		}

		public override System.Linq.Expressions.Expression GenerateLINQ(List<Expression> operands)
		{
			if (operands.Count != 1)
			{
				return System.Linq.Expressions.Expression.Constant(Value.Unknown);
			}
			return operands[0].GetLINQ();
		}

		public override string ToString(List<Expression> operands)
		{
			if (operands.Count != 1)
			{
				return base.ToString(operands);
			}
			return "(" + operands[0].ToString() + ")";
		}
	}

	public class OperatorCallFunction : Operator
	{
		public OperatorCallFunction()
			: base("FUNC", -1, null)
		{
		}

		public override Value Calc(Context context, List<Expression> operands, bool as_value)
		{
			if (operands.Count < 1)
			{
				return Value.Unknown;
			}
			Expression expression = operands[0];
			if (expression.type != Type.Variable)
			{
				return expression.result = Value.Unknown;
			}
			string text = expression.value;
			if (predefined_funcs.TryGetValue(text, out var value))
			{
				Value[] array = new Value[operands.Count - 1];
				for (int i = 1; i < operands.Count; i++)
				{
					array[i - 1] = operands[i].CalcInner(context);
				}
				return expression.result = value(context, array, as_value);
			}
			if (context == null)
			{
				return expression.result = Value.Unknown;
			}
			object[] array2 = new object[operands.Count - 1];
			for (int j = 1; j < operands.Count; j++)
			{
				array2[j - 1] = operands[j].CalcInner(context).Object();
			}
			return expression.result = context.Call(null, text, array2, as_value);
		}

		public override System.Linq.Expressions.Expression GenerateLINQ(List<Expression> operands)
		{
			if (operands.Count < 1)
			{
				return System.Linq.Expressions.Expression.Constant(Value.Unknown);
			}
			if (operands[0].type != Type.Variable)
			{
				return System.Linq.Expressions.Expression.Constant(Value.Unknown);
			}
			System.Linq.Expressions.Expression[] array = new System.Linq.Expressions.Expression[operands.Count - 1];
			for (int i = 1; i < operands.Count; i++)
			{
				System.Linq.Expressions.Expression lINQ = operands[i].GetLINQ();
				if (lINQ == null)
				{
					return null;
				}
				array[i - 1] = lINQ;
			}
			return System.Linq.Expressions.Expression.Call(typeof(OperatorCallFunction), "_Call", null, context_param, System.Linq.Expressions.Expression.Constant((string)operands[0].value), System.Linq.Expressions.Expression.NewArrayInit(typeof(Value), array), as_value_param);
		}

		private static Value _Call(Context context, string func_name, Value[] arguments, bool as_value)
		{
			if (predefined_funcs.TryGetValue(func_name, out var value))
			{
				return value(context, arguments, as_value);
			}
			if (context == null)
			{
				return Value.Unknown;
			}
			object[] array = new object[arguments.Length];
			for (int i = 0; i < arguments.Length; i++)
			{
				array[i] = arguments[i].Object();
			}
			return context.Call(null, func_name, array, as_value);
		}

		public override string ToString(List<Expression> operands)
		{
			if (operands.Count < 1)
			{
				return base.ToString(operands);
			}
			string text = operands[0].ToString() + "(";
			for (int i = 1; i < operands.Count; i++)
			{
				if (i > 1)
				{
					text += ", ";
				}
				text += operands[i].ToString();
			}
			return text + ")";
		}
	}

	public class OperatorCallMethod : Operator
	{
		public OperatorCallMethod()
			: base("METHOD", -1, null)
		{
		}

		public override Value Calc(Context context, List<Expression> operands, bool as_value)
		{
			if (operands.Count < 2)
			{
				return Value.Unknown;
			}
			Expression expression = operands[1];
			if (expression.type != Type.Variable)
			{
				return expression.result = Value.Unknown;
			}
			if (context == null)
			{
				return expression.result = Value.Unknown;
			}
			Value value = operands[0].CalcInner(context, as_value: false);
			if (!value.is_object)
			{
				return expression.result = Value.Unknown;
			}
			object[] array = new object[operands.Count - 2];
			for (int i = 2; i < operands.Count; i++)
			{
				array[i - 2] = operands[i].CalcInner(context).Object();
			}
			return expression.result = context.Call(value.obj_val, operands[1].value, array, as_value);
		}

		public override System.Linq.Expressions.Expression GenerateLINQ(List<Expression> operands)
		{
			if (operands.Count < 2)
			{
				return System.Linq.Expressions.Expression.Constant(Value.Unknown);
			}
			if (operands[1].type != Type.Variable)
			{
				return System.Linq.Expressions.Expression.Constant(Value.Unknown);
			}
			System.Linq.Expressions.Expression lINQ = operands[0].GetLINQ();
			if (lINQ == null)
			{
				return null;
			}
			System.Linq.Expressions.Expression[] array = new System.Linq.Expressions.Expression[operands.Count - 2];
			for (int i = 2; i < operands.Count; i++)
			{
				if (operands[i].GetLINQ() == null)
				{
					return null;
				}
				array[i - 2] = operands[i].GetLINQ();
			}
			return System.Linq.Expressions.Expression.Call(typeof(OperatorCallMethod), "_Call", null, context_param, lINQ, System.Linq.Expressions.Expression.Constant((string)operands[1].value), System.Linq.Expressions.Expression.NewArrayInit(typeof(Value), array), as_value_param);
		}

		private static Value _Call(Context context, Value obj, string func_name, Value[] arguments, bool as_value)
		{
			if (context == null || !obj.is_object)
			{
				return Value.Unknown;
			}
			object[] array = new object[arguments.Length];
			for (int i = 0; i < arguments.Length; i++)
			{
				array[i] = arguments[i].Object();
			}
			return context.Call(obj.Object(), func_name, array, as_value);
		}

		public override string ToString(List<Expression> operands)
		{
			if (operands.Count < 2)
			{
				return base.ToString(operands);
			}
			string text = operands[0].ToString() + "." + operands[1].ToString() + "(";
			for (int i = 2; i < operands.Count; i++)
			{
				if (i > 2)
				{
					text += ", ";
				}
				text += operands[i].ToString();
			}
			return text + ")";
		}
	}

	public class OperatorIndex : Operator
	{
		public OperatorIndex()
			: base("[]", -1, null)
		{
		}

		public override Value Calc(Context context, List<Expression> operands, bool as_value)
		{
			if (operands.Count < 2)
			{
				return Value.Unknown;
			}
			if (context == null)
			{
				return Value.Unknown;
			}
			Value value = operands[0].CalcInner(context, as_value: false);
			if (!value.is_object)
			{
				return Value.Unknown;
			}
			object[] array = new object[operands.Count - 1];
			for (int i = 1; i < operands.Count; i++)
			{
				array[i - 1] = operands[i].CalcInner(context).Object();
			}
			return context.GetByIndex(value.obj_val, array, as_value);
		}

		public override System.Linq.Expressions.Expression GenerateLINQ(List<Expression> operands)
		{
			if (operands.Count < 2)
			{
				return System.Linq.Expressions.Expression.Constant(Value.Unknown);
			}
			System.Linq.Expressions.Expression lINQ = operands[0].GetLINQ();
			if (lINQ == null)
			{
				return null;
			}
			System.Linq.Expressions.Expression[] array = new System.Linq.Expressions.Expression[operands.Count - 1];
			for (int i = 1; i < operands.Count; i++)
			{
				if (operands[i].GetLINQ() == null)
				{
					return null;
				}
				array[i - 1] = operands[i].GetLINQ();
			}
			return System.Linq.Expressions.Expression.Call(typeof(OperatorIndex), "_Index", null, context_param, lINQ, System.Linq.Expressions.Expression.NewArrayInit(typeof(Value), array), as_value_param);
		}

		private static Value _Index(Context context, Value obj, Value[] arguments, bool as_value)
		{
			if (context == null || !obj.is_object)
			{
				return Value.Unknown;
			}
			object[] array = new object[arguments.Length];
			for (int i = 0; i < arguments.Length; i++)
			{
				array[i] = arguments[i].Object();
			}
			return context.GetByIndex(obj.Object(), array, as_value);
		}

		public override string ToString(List<Expression> operands)
		{
			if (operands.Count < 2)
			{
				return base.ToString(operands);
			}
			string text = operands[0].ToString() + "[";
			for (int i = 1; i < operands.Count; i++)
			{
				if (i > 1)
				{
					text += ", ";
				}
				text += operands[i].ToString();
			}
			return text + "]";
		}
	}

	public class OperatorDump : Operator
	{
		public OperatorDump()
			: base("??", 15, null)
		{
		}

		public override Value Calc(Context context, List<Expression> operands, bool as_value)
		{
			if (operands.Count != 1)
			{
				return Value.Unknown;
			}
			if (context == null)
			{
				return Value.Unknown;
			}
			Expression expression = operands[0];
			Value value = expression.CalcInner(context, as_value);
			string text = $"Calculated '{expression}' -> {value}";
			if (context != null)
			{
				text = text + "\nContext:\n" + context.Dump("  ");
			}
			text = text + "\n" + expression.Dump();
			Game.Log(text, Game.LogType.Message);
			return value;
		}

		public override System.Linq.Expressions.Expression GenerateLINQ(List<Expression> operands)
		{
			if (operands.Count != 1)
			{
				return System.Linq.Expressions.Expression.Constant(Value.Unknown);
			}
			return operands[0].GetLINQ();
		}

		public override string ToString(List<Expression> operands)
		{
			if (operands.Count != 1)
			{
				return base.ToString(operands);
			}
			return "??" + operands[0].ToString();
		}
	}

	public class OperatorLog : Operator
	{
		public OperatorLog()
			: base("?", 3, null)
		{
		}

		public override Value Calc(Context context, List<Expression> operands, bool as_value)
		{
			if (operands.Count != 1)
			{
				return Value.Unknown;
			}
			if (context == null)
			{
				return Value.Unknown;
			}
			Expression expression = operands[0];
			Value value = expression.CalcInner(context, as_value);
			Game.Log($"'{expression}' -> {value}", Game.LogType.Message);
			return value;
		}

		public override System.Linq.Expressions.Expression GenerateLINQ(List<Expression> operands)
		{
			if (operands.Count != 1)
			{
				return System.Linq.Expressions.Expression.Constant(Value.Unknown);
			}
			return operands[0].GetLINQ();
		}

		public override string ToString(List<Expression> operands)
		{
			if (operands.Count != 1)
			{
				return base.ToString(operands);
			}
			return "?" + operands[0].ToString();
		}
	}

	public class PrecompiledDescr
	{
		public string name;

		public CompiledExpression func;

		public PrecompiledDescr(string name, CompiledExpression func)
		{
			this.name = name;
			this.func = func;
		}
	}

	public delegate Value PredefinedFunc(Context context, Value[] args, bool as_value);

	public class TestContext : Context
	{
		public static object a = new object();

		public Value Call(object obj, string func_name, object[] args, bool as_value = true)
		{
			if (obj == null)
			{
				if (func_name.Length > 1 && func_name[0] == 'F' && func_name.Substring(1) == args.Length.ToString())
				{
					return "F" + args.Length;
				}
				return Value.Unknown;
			}
			if (func_name.Length > 1 && func_name[0] == 'M' && func_name.Substring(1) == args.Length.ToString())
			{
				return "M" + args.Length;
			}
			return Value.Unknown;
		}

		public Value GetByIndex(object obj, object[] indicies, bool as_value = true)
		{
			if (obj == null)
			{
				return Value.Unknown;
			}
			string text = "";
			for (int i = 1; i < indicies.Length; i++)
			{
				text += ",";
			}
			return "[" + text + "]";
		}

		public Value GetVar(object obj, string var_name, bool as_value = true)
		{
			if (obj == null)
			{
				if (var_name == "a")
				{
					return new Value(a);
				}
				return Value.Unknown;
			}
			if (var_name == "x" || var_name == "y")
			{
				return var_name;
			}
			return Value.Unknown;
		}

		public string Dump(string ident = "", string new_line = "\n")
		{
			return ident + "TestContext";
		}
	}

	public Type type;

	public Value value = Value.Unknown;

	public List<Expression> operands;

	public Value result = Value.Unknown;

	public static int num_calcs = 0;

	private static bool in_calc = false;

	public static bool compile_enabled = false;

	public System.Linq.Expressions.Expression linq_expression;

	public CompiledExpression compiled;

	private static ParameterExpression context_param = System.Linq.Expressions.Expression.Parameter(typeof(Context), "context");

	private static ParameterExpression as_value_param = System.Linq.Expressions.Expression.Parameter(typeof(bool), "as_value");

	private static bool initted = false;

	public static Dictionary<string, PrecompiledDescr> precompiled = new Dictionary<string, PrecompiledDescr>();

	public static Dictionary<string, PredefinedFunc> predefined_funcs = new Dictionary<string, PredefinedFunc>();

	public static TestContext test_context = new TestContext();

	private static Kingdom[] temp_enemy_kingdoms = new Kingdom[3];

	public Expression()
	{
	}

	public Expression(Type type, Value value)
	{
		this.type = type;
		this.value = value;
	}

	public Expression(int val)
	{
		type = Type.Constant;
		value = val;
	}

	public Expression(float val)
	{
		type = Type.Constant;
		value = val;
	}

	public Expression(string val)
	{
		type = Type.Constant;
		value = val;
	}

	public Expression(Operator op, List<Expression> operands = null)
	{
		type = Type.Operator;
		value = new Value(op);
		this.operands = operands;
	}

	public Expression(Value val)
	{
		if (val.obj_val is Expression expression)
		{
			type = expression.type;
			value = expression.value;
			operands = expression.operands;
		}
		else
		{
			type = Type.Constant;
			value = val;
		}
	}

	public Expression(string op, Value operand)
	{
		type = Type.Operator;
		value = new Value(Operator.Get(op) ?? Operator.Get("null"));
		operands = new List<Expression>
		{
			new Expression(operand)
		};
	}

	public Expression(Value operand1, string op, Value operand2)
	{
		type = Type.Operator;
		value = new Value(Operator.Get(op) ?? Operator.Get("null"));
		operands = new List<Expression>
		{
			new Expression(operand1),
			new Expression(operand2)
		};
	}

	public static bool IsBlank(char c)
	{
		if (c != ' ')
		{
			return c == '\t';
		}
		return true;
	}

	public static void SkipBlanks(string text, ref int idx)
	{
		while (idx < text.Length && IsBlank(text[idx]))
		{
			idx++;
		}
	}

	public static Value ReadNumber(string text, ref int idx)
	{
		int i = idx;
		if (idx < text.Length && text[idx] == '-')
		{
			i++;
		}
		for (; i < text.Length && char.IsDigit(text[i]); i++)
		{
		}
		if (i > idx && (i >= text.Length || text[i] != '.'))
		{
			if (!int.TryParse(text.Substring(idx, i - idx), out var num))
			{
				return Value.Unknown;
			}
			idx = i;
			return num;
		}
		if (i < text.Length && text[i] == '.')
		{
			for (i++; i < text.Length && char.IsDigit(text[i]); i++)
			{
			}
			if (i > idx + 1)
			{
				DT.ParseFloat(text.Substring(idx, i - idx), out var f);
				idx = i;
				return f;
			}
		}
		return Value.Unknown;
	}

	public static string ReadQuotedString(string text, ref int idx)
	{
		if (idx >= text.Length)
		{
			return null;
		}
		char c = text[idx];
		if (c != '"' && c != '\'')
		{
			return null;
		}
		int i;
		for (i = idx + 1; i < text.Length && text[i] != c; i++)
		{
		}
		if (i >= text.Length)
		{
			return null;
		}
		string text2 = text.Substring(idx + 1, i - idx - 1);
		idx = i + 1;
		return text2;
	}

	public static string ReadIdentifier(string text, ref int idx)
	{
		if (idx >= text.Length)
		{
			return null;
		}
		char c = text[idx];
		if (c != '_' && !char.IsLetter(c))
		{
			return null;
		}
		int i;
		for (i = idx + 1; i < text.Length; i++)
		{
			c = text[i];
			if (c != '_' && !char.IsLetterOrDigit(c))
			{
				break;
			}
		}
		string text2 = text.Substring(idx, i - idx);
		idx = i;
		return text2;
	}

	private static Expression ReadOperand(string text, ref int idx)
	{
		if (idx >= text.Length)
		{
			return null;
		}
		if (text[idx] == '#')
		{
			idx++;
			string text2 = ReadIdentifier(text, ref idx);
			if (text2 == null)
			{
				return null;
			}
			if (!precompiled.TryGetValue(text2, out var val))
			{
				return null;
			}
			return new Expression(Type.Precompiled, new Value(val));
		}
		if (text[idx] == '(')
		{
			int num = idx;
			idx++;
			Expression expression = ReadExpression(text, ref idx);
			SkipBlanks(text, ref idx);
			if (expression == null || idx >= text.Length || text[idx] != ')')
			{
				idx = num;
				return null;
			}
			idx++;
			return new Expression(Operator.Get("()"), new List<Expression> { expression });
		}
		string text3 = ReadQuotedString(text, ref idx);
		if (text3 != null)
		{
			return new Expression(Type.Constant, text3);
		}
		Value value = ReadNumber(text, ref idx);
		if (value.is_valid)
		{
			return new Expression(Type.Constant, value);
		}
		string text4 = ReadIdentifier(text, ref idx);
		return text4 switch
		{
			null => null, 
			"true" => new Expression(Type.Constant, true), 
			"false" => new Expression(Type.Constant, false), 
			"null" => new Expression(Type.Constant, Value.Null), 
			"unknown" => new Expression(Type.Constant, Value.Unknown), 
			_ => new Expression(Type.Variable, text4), 
		};
	}

	private static Operator ReadOperator(string text, ref int idx)
	{
		if (idx >= text.Length)
		{
			return null;
		}
		if (text[idx] == '[')
		{
			idx++;
			return Operator.Get("[]");
		}
		Operator obj = null;
		foreach (KeyValuePair<string, Operator> item in Operator.registry)
		{
			_ = item.Key;
			Operator obj2 = item.Value;
			if (obj2.priority < 0)
			{
				continue;
			}
			int length = obj2.name.Length;
			if (idx + length > text.Length || (obj != null && obj.name.Length >= length))
			{
				continue;
			}
			bool flag = true;
			for (int i = 0; i < length; i++)
			{
				if (text[idx + i] != obj2.name[i])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				obj = obj2;
			}
		}
		if (obj != null)
		{
			idx += obj.name.Length;
		}
		return obj;
	}

	private static void ReadCallArgs(string text, ref int idx, List<Expression> args)
	{
		while (true)
		{
			Expression expression = ReadExpression(text, ref idx);
			if (expression != null)
			{
				args.Add(expression);
				SkipBlanks(text, ref idx);
				if (idx < text.Length && text[idx] == ',')
				{
					idx++;
					continue;
				}
				break;
			}
			break;
		}
	}

	public static Expression ReadExpression(string text, ref int idx, int priority = int.MaxValue)
	{
		SkipBlanks(text, ref idx);
		Expression expression = ReadOperand(text, ref idx);
		while (true)
		{
			SkipBlanks(text, ref idx);
			if (idx >= text.Length)
			{
				break;
			}
			int num = idx;
			if (text[idx] == '(')
			{
				if (expression == null || (expression.type != Type.Variable && (expression.type != Type.Operator || !(expression.value.obj_val is OperatorDot))))
				{
					break;
				}
				idx++;
				List<Expression> list = new List<Expression>();
				ReadCallArgs(text, ref idx, list);
				SkipBlanks(text, ref idx);
				if (idx >= text.Length || text[idx] != ')')
				{
					idx = num;
					break;
				}
				idx++;
				if (expression.type == Type.Variable)
				{
					list.Insert(0, expression);
					expression = new Expression(Operator.Get("FUNC"), list);
				}
				else
				{
					list.Insert(0, expression.operands[1]);
					list.Insert(0, expression.operands[0]);
					expression = new Expression(Operator.Get("METHOD"), list);
				}
				continue;
			}
			Operator obj = ReadOperator(text, ref idx);
			if (obj == null)
			{
				break;
			}
			if (obj is OperatorIndex)
			{
				List<Expression> list2 = new List<Expression>();
				ReadCallArgs(text, ref idx, list2);
				SkipBlanks(text, ref idx);
				if (idx >= text.Length || text[idx] != ']')
				{
					idx = num;
					break;
				}
				idx++;
				list2.Insert(0, expression);
				expression = new Expression(obj, list2);
				continue;
			}
			if ((expression != null || obj.unary_func == null) && (obj.priority > priority || (obj.priority == priority && obj.binary_func != null && (priority <= 2 || (priority >= 4 && priority <= 14) || priority == 17))))
			{
				idx = num;
				break;
			}
			if (obj is OperatorDot)
			{
				SkipBlanks(text, ref idx);
				string text2 = ReadIdentifier(text, ref idx);
				if (text2 == null)
				{
					idx = num;
					break;
				}
				expression = new Expression(obj, new List<Expression>
				{
					expression,
					new Expression(Type.Variable, text2)
				});
				continue;
			}
			Expression expression2 = ReadExpression(text, ref idx, obj.priority);
			if (expression2 == null)
			{
				idx = num;
				break;
			}
			expression = ((expression != null) ? new Expression(obj, new List<Expression> { expression, expression2 }) : new Expression(obj, new List<Expression> { expression2 }));
		}
		return expression;
	}

	public static Expression Parse(string text, bool allow_compilation = true)
	{
		Init();
		int idx = 0;
		Expression expression = ReadExpression(text, ref idx);
		if (idx < text.Length)
		{
			return new Expression();
		}
		if (allow_compilation)
		{
			expression.Compile();
		}
		return expression;
	}

	public Value Calc(Context context, bool as_value = true)
	{
		if (compiled != null && compile_enabled)
		{
			num_calcs++;
			try
			{
				return compiled(context, as_value);
			}
			catch (Exception ex)
			{
				Error("Error evaluating compiled '" + ToString() + "': " + ex.ToString());
				return Value.Unknown;
			}
		}
		if (in_calc)
		{
			return CalcInner(context, as_value);
		}
		num_calcs++;
		in_calc = true;
		try
		{
			CalcInner(context, as_value);
		}
		catch (Exception ex2)
		{
			Error("Error evaluating '" + ToString() + "': " + ex2.ToString());
		}
		in_calc = false;
		return result;
	}

	private Value CalcInner(Context context, bool as_value = true)
	{
		result = Value.Unknown;
		switch (type)
		{
		case Type.Constant:
			result = value;
			break;
		case Type.Variable:
			if (context != null && value.type == Value.Type.String)
			{
				result = context.GetVar(null, (string)value.obj_val, as_value);
			}
			break;
		case Type.Operator:
			result = ((Operator)value.obj_val).Calc(context, operands, as_value);
			break;
		case Type.Precompiled:
			result = ((PrecompiledDescr)value.obj_val).func(context, as_value);
			break;
		}
		return result;
	}

	public System.Linq.Expressions.Expression GetLINQ()
	{
		if (linq_expression == null)
		{
			linq_expression = GenerateLINQ();
		}
		return linq_expression;
	}

	public System.Linq.Expressions.Expression GenerateLINQ()
	{
		switch (type)
		{
		case Type.Invalid:
			return System.Linq.Expressions.Expression.Constant(Value.Unknown);
		case Type.Constant:
			return System.Linq.Expressions.Expression.Constant(value);
		case Type.Variable:
			if (value.type != Value.Type.String)
			{
				return System.Linq.Expressions.Expression.Constant(Value.Unknown);
			}
			return System.Linq.Expressions.Expression.Call(typeof(Expression), "_GetVar", null, context_param, System.Linq.Expressions.Expression.Constant((string)value.obj_val), as_value_param);
		case Type.Operator:
			return ((Operator)value.obj_val).GenerateLINQ(operands);
		case Type.Precompiled:
			return System.Linq.Expressions.Expression.Call(((PrecompiledDescr)value.obj_val).func.Method, context_param, as_value_param);
		default:
			return null;
		}
	}

	private static Value _GetVar(Context context, string var_name, bool as_value)
	{
		return context?.GetVar(null, var_name, as_value) ?? Value.Unknown;
	}

	public bool Compile()
	{
		if (!compile_enabled)
		{
			return false;
		}
		try
		{
			if (type == Type.Precompiled)
			{
				compiled = ((PrecompiledDescr)value.obj_val).func;
				return compiled != null;
			}
			System.Linq.Expressions.Expression lINQ = GetLINQ();
			if (lINQ == null)
			{
				return false;
			}
			LambdaExpression lambdaExpression = System.Linq.Expressions.Expression.Lambda(typeof(CompiledExpression), lINQ, context_param, as_value_param);
			compiled = (CompiledExpression)lambdaExpression.Compile();
			return compiled != null;
		}
		catch (Exception ex)
		{
			Game.Log("Error compiling expression '" + ToString() + "': " + ex.ToString(), Game.LogType.Error);
			return false;
		}
	}

	public override string ToString()
	{
		try
		{
			if (!value.is_valid)
			{
				return value.ToString();
			}
			switch (type)
			{
			case Type.Constant:
				if (value.type == Value.Type.Int)
				{
					return value.int_val.ToString();
				}
				if (value.type == Value.Type.Float)
				{
					return DT.FloatToStr(value.float_val);
				}
				return value.ToString();
			case Type.Variable:
				return value;
			case Type.Operator:
				return ((Operator)value.obj_val).ToString(operands);
			case Type.Precompiled:
				return "#" + ((PrecompiledDescr)value.obj_val).name;
			}
		}
		catch (Exception ex)
		{
			return "<invalid>:" + ex.ToString();
		}
		return "<invalid>";
	}

	public string Dump(string ident = "", string new_line = "\n")
	{
		if (type != Type.Operator)
		{
			return ident + ToString() + " -> " + result.ToString();
		}
		Operator obj = (Operator)value.obj_val;
		string text = ident + "Operator " + obj.name;
		if (operands != null)
		{
			text += "(";
			for (int i = 0; i < operands.Count; i++)
			{
				Expression expression = operands[i];
				if (i != 0)
				{
					text += ", ";
				}
				text = text + expression.ToString() + " -> " + expression.result.ToString();
			}
			text += ")";
		}
		text = text + " -> " + result.ToString();
		if (operands != null)
		{
			ident += "    ";
			for (int j = 0; j < operands.Count; j++)
			{
				Expression expression2 = operands[j];
				text = text + new_line + expression2.Dump(ident, new_line);
			}
		}
		return text;
	}

	public static void Error(string text)
	{
		Game.Log("Expression error: " + text, Game.LogType.Error);
	}

	public static int bool2int(bool b)
	{
		if (!b)
		{
			return 0;
		}
		return 1;
	}

	public static bool tobool(Value val)
	{
		if (!val.is_valid)
		{
			return false;
		}
		if (val.type == Value.Type.Int)
		{
			return val.int_val != 0;
		}
		if (val.type == Value.Type.Float)
		{
			return val.float_val != 0f;
		}
		if (val.type == Value.Type.String)
		{
			string text = (string)val.obj_val;
			if (text != "")
			{
				return text != "false";
			}
			return false;
		}
		return true;
	}

	private static Value Plus(Value a)
	{
		if (a.type == Value.Type.Int || a.type == Value.Type.Float)
		{
			return a;
		}
		return Value.Unknown;
	}

	private static Value Plus(Value a, Value b)
	{
		if (a.type == Value.Type.Int)
		{
			if (b.type == Value.Type.Int)
			{
				return a.int_val + b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return (float)a.int_val + b.float_val;
			}
			if (b.type == Value.Type.String)
			{
				return a.int_val + (string)b.obj_val;
			}
		}
		else if (a.type == Value.Type.Float)
		{
			if (b.type == Value.Type.Int)
			{
				return a.float_val + (float)b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return a.float_val + b.float_val;
			}
			if (b.type == Value.Type.String)
			{
				return a.float_val + (string)b.obj_val;
			}
		}
		else if (a.type == Value.Type.String)
		{
			if (b.is_null)
			{
				return a;
			}
			if (b.type == Value.Type.Int)
			{
				return (string)a.obj_val + b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return (string)a.obj_val + b.float_val;
			}
			if (b.type == Value.Type.String)
			{
				return (string)a.obj_val + (string)b.obj_val;
			}
		}
		return Value.Unknown;
	}

	private static Value Minus(Value a)
	{
		if (a.type == Value.Type.Int)
		{
			return -a.int_val;
		}
		if (a.type == Value.Type.Float)
		{
			return 0f - a.float_val;
		}
		return Value.Unknown;
	}

	private static Value Minus(Value a, Value b)
	{
		if (a.type == Value.Type.Int)
		{
			if (b.type == Value.Type.Int)
			{
				return a.int_val - b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return (float)a.int_val - b.float_val;
			}
		}
		else if (a.type == Value.Type.Float)
		{
			if (b.type == Value.Type.Int)
			{
				return a.float_val - (float)b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return a.float_val - b.float_val;
			}
		}
		return Value.Unknown;
	}

	private static Value Mul(Value a, Value b)
	{
		if (a.type == Value.Type.Int)
		{
			if (b.type == Value.Type.Int)
			{
				return a.int_val * b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return (float)a.int_val * b.float_val;
			}
		}
		else if (a.type == Value.Type.Float)
		{
			if (b.type == Value.Type.Int)
			{
				return a.float_val * (float)b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return a.float_val * b.float_val;
			}
		}
		return Value.Unknown;
	}

	private static Value Div(Value a, Value b)
	{
		if (a.type == Value.Type.Int)
		{
			if (b.type == Value.Type.Int)
			{
				return (float)a.int_val / (float)b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return (float)a.int_val / b.float_val;
			}
		}
		else if (a.type == Value.Type.Float)
		{
			if (b.type == Value.Type.Int)
			{
				return a.float_val / (float)b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return a.float_val / b.float_val;
			}
		}
		return Value.Unknown;
	}

	private static Value Pow(Value a, Value b)
	{
		if (a.type == Value.Type.Int)
		{
			if (b.type == Value.Type.Int)
			{
				return (float)Math.Pow(a.int_val, b.int_val);
			}
			if (b.type == Value.Type.Float)
			{
				return (float)Math.Pow(a.int_val, b.float_val);
			}
		}
		else if (a.type == Value.Type.Float)
		{
			if (b.type == Value.Type.Int)
			{
				return (float)Math.Pow(a.float_val, b.int_val);
			}
			if (b.type == Value.Type.Float)
			{
				return (float)Math.Pow(a.float_val, b.float_val);
			}
		}
		return Value.Unknown;
	}

	private static Value Mod(Value a, Value b)
	{
		if (a.type == Value.Type.Int)
		{
			if (b.type == Value.Type.Int)
			{
				return a.int_val % b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return (float)a.int_val % b.float_val;
			}
		}
		else if (a.type == Value.Type.Float)
		{
			if (b.type == Value.Type.Int)
			{
				return a.float_val % (float)b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return a.float_val % b.float_val;
			}
		}
		return Value.Unknown;
	}

	private static Value Eq(Value a, Value b)
	{
		return a == b;
	}

	private static Value NEq(Value a, Value b)
	{
		return a != b;
	}

	private static Value Gt(Value a, Value b)
	{
		if (a.type == Value.Type.Int)
		{
			if (b.type == Value.Type.Int)
			{
				return a.int_val > b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return (float)a.int_val > b.float_val;
			}
		}
		else if (a.type == Value.Type.Float)
		{
			if (b.type == Value.Type.Int)
			{
				return a.float_val > (float)b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return a.float_val > b.float_val;
			}
		}
		return Value.Unknown;
	}

	private static Value GtEq(Value a, Value b)
	{
		if (a == b)
		{
			return true;
		}
		return Gt(a, b);
	}

	private static Value Lt(Value a, Value b)
	{
		if (a.type == Value.Type.Int)
		{
			if (b.type == Value.Type.Int)
			{
				return a.int_val < b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return (float)a.int_val < b.float_val;
			}
		}
		else if (a.type == Value.Type.Float)
		{
			if (b.type == Value.Type.Int)
			{
				return a.float_val < (float)b.int_val;
			}
			if (b.type == Value.Type.Float)
			{
				return a.float_val < b.float_val;
			}
		}
		return Value.Unknown;
	}

	private static Value LtEq(Value a, Value b)
	{
		if (a == b)
		{
			return true;
		}
		return Lt(a, b);
	}

	private static Value Not(Value a)
	{
		if (!a.is_valid)
		{
			return true;
		}
		if (a.type == Value.Type.Int)
		{
			return a.int_val == 0;
		}
		if (a.type == Value.Type.Float)
		{
			return a.float_val == 0f;
		}
		if (a.type == Value.Type.String)
		{
			return (string)a.obj_val == "";
		}
		return false;
	}

	private static Value NotNot(Value a)
	{
		if (!a.is_valid)
		{
			return false;
		}
		if (a.type == Value.Type.Int)
		{
			return a.int_val != 0;
		}
		if (a.type == Value.Type.Float)
		{
			return a.float_val != 0f;
		}
		if (a.type == Value.Type.String)
		{
			return (string)a.obj_val != "";
		}
		return true;
	}

	private static Value Or(Value a, Value b)
	{
		if (!a)
		{
			return b;
		}
		return a;
	}

	private static Value And(Value a, Value b)
	{
		if (!a)
		{
			return a;
		}
		return b;
	}

	public static Value max(Context context, Value[] args, bool as_value)
	{
		if (args.Length < 1)
		{
			return Value.Unknown;
		}
		Value value = args[0];
		if (!value.is_number)
		{
			return Value.Unknown;
		}
		float num = value.Float();
		for (int i = 1; i < args.Length; i++)
		{
			Value value2 = args[i];
			if (!value2.is_number)
			{
				return Value.Unknown;
			}
			float num2 = value2.Float();
			if (num2 > num)
			{
				value = value2;
				num = num2;
			}
		}
		return value;
	}

	public static Value min(Context context, Value[] args, bool as_value)
	{
		if (args.Length < 1)
		{
			return Value.Unknown;
		}
		Value value = args[0];
		if (!value.is_number)
		{
			return Value.Unknown;
		}
		float num = value.Float();
		for (int i = 1; i < args.Length; i++)
		{
			Value value2 = args[i];
			if (!value2.is_number)
			{
				return Value.Unknown;
			}
			float num2 = value2.Float();
			if (num2 < num)
			{
				value = value2;
				num = num2;
			}
		}
		return value;
	}

	public static Value abs(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 1)
		{
			return Value.Unknown;
		}
		Value value = args[0];
		if (value.type == Value.Type.Int)
		{
			if (value.int_val >= 0)
			{
				return value;
			}
			return -value.int_val;
		}
		if (value.type == Value.Type.Float)
		{
			if (value.float_val >= 0f)
			{
				return value;
			}
			return 0f - value.float_val;
		}
		return Value.Unknown;
	}

	public static Value clamp(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 3)
		{
			return Value.Unknown;
		}
		if (!args[0].is_number || !args[1].is_number || !args[2].is_number)
		{
			return Value.Unknown;
		}
		float num = args[0].Float();
		float num2 = args[1].Float();
		float num3 = args[2].Float();
		if (num < num2)
		{
			return args[1];
		}
		if (num > num3)
		{
			return args[2];
		}
		return args[0];
	}

	public static Value map(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 5)
		{
			return Value.Unknown;
		}
		if (!args[0].is_number || !args[1].is_number || !args[2].is_number || !args[3].is_number || !args[4].is_number)
		{
			return Value.Unknown;
		}
		float num = args[0].Float();
		float num2 = args[1].Float();
		float num3 = args[2].Float();
		float num4 = args[3].Float();
		float num5 = args[4].Float();
		if (num3 == num2 || num5 == num4)
		{
			return num4;
		}
		return num4 + (num - num2) / (num3 - num2) * (num5 - num4);
	}

	public static Value map_clamp(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 5)
		{
			return Value.Unknown;
		}
		if (!args[0].is_number || !args[1].is_number || !args[2].is_number || !args[3].is_number || !args[4].is_number)
		{
			return Value.Unknown;
		}
		float num = args[0].Float();
		float num2 = args[1].Float();
		float num3 = args[2].Float();
		float num4 = args[3].Float();
		float num5 = args[4].Float();
		if (num3 <= num2)
		{
			return num4;
		}
		if (num < num2)
		{
			num = num2;
		}
		if (num > num3)
		{
			num = num3;
		}
		return num4 + (num - num2) / (num3 - num2) * (num5 - num4);
	}

	public static Value map_clamp_pow(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 6)
		{
			return Value.Unknown;
		}
		if (!args[0].is_number || !args[1].is_number || !args[2].is_number || !args[3].is_number || !args[4].is_number || !args[5].is_number)
		{
			return Value.Unknown;
		}
		float num = args[0].Float();
		float num2 = args[1].Float();
		float num3 = args[2].Float();
		float num4 = args[3].Float();
		float num5 = args[4].Float();
		float num6 = args[5].Float();
		if (num3 <= num2)
		{
			return num4;
		}
		if (num < num2)
		{
			num = num2;
		}
		if (num > num3)
		{
			num = num3;
		}
		return num4 + (num5 - num4) * (float)Math.Pow((num - num2) / (num3 - num2), num6);
	}

	public static Value map3_clamp(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 7)
		{
			return Value.Unknown;
		}
		if (!args[0].is_number || !args[1].is_number || !args[2].is_number || !args[3].is_number || !args[4].is_number || !args[5].is_number || !args[6].is_number)
		{
			return Value.Unknown;
		}
		float num = args[0].Float();
		float num2 = args[1].Float();
		float num3 = args[2].Float();
		float num4 = args[3].Float();
		float num5 = args[4].Float();
		float num6 = args[5].Float();
		float num7 = args[6].Float();
		if (num4 <= num2)
		{
			return num5;
		}
		if (num < num2)
		{
			num = num2;
		}
		if (num > num4)
		{
			num = num4;
		}
		if (num < num3)
		{
			num4 = num3;
			num7 = num6;
		}
		else
		{
			num2 = num3;
			num5 = num6;
		}
		if (num4 == num2)
		{
			return num5;
		}
		return num5 + (num - num2) / (num4 - num2) * (num7 - num5);
	}

	public static Value round(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 1 && args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!args[0].is_number)
		{
			return Value.Unknown;
		}
		float num = args[0].Float();
		float num2 = ((args.Length > 1) ? args[1].Float(1f) : 1f);
		if (num2 <= 0f)
		{
			num2 = 1f;
		}
		return (float)Math.Round(num / num2) * num2;
	}

	public static Value ceil(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 1 && args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!args[0].is_number)
		{
			return Value.Unknown;
		}
		float num = args[0].Float();
		float num2 = ((args.Length > 1) ? args[1].Float(1f) : 1f);
		if (num2 <= 0f)
		{
			num2 = 1f;
		}
		return (float)Math.Ceiling(num / num2) * num2;
	}

	public static Value floor(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 1 && args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!args[0].is_number)
		{
			return Value.Unknown;
		}
		float num = args[0].Float();
		float num2 = ((args.Length > 1) ? args[1].Float(1f) : 1f);
		if (num2 <= 0f)
		{
			num2 = 1f;
		}
		return (float)Math.Floor(num / num2) * num2;
	}

	public static Value sqr(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 1)
		{
			return Value.Unknown;
		}
		if (!args[0].is_number)
		{
			return Value.Unknown;
		}
		float num = args[0].Float();
		return num * num;
	}

	public static Value sqrt(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 1)
		{
			return Value.Unknown;
		}
		if (!args[0].is_number)
		{
			return Value.Unknown;
		}
		return (float)Math.Sqrt(args[0].Float());
	}

	public static void Init()
	{
		if (!initted)
		{
			initted = true;
			InitOperators();
			InitPrecompiledExpressions();
		}
	}

	public static void InitOperators()
	{
		new OperatorDot();
		new OperatorBrackets();
		new OperatorCallFunction();
		new OperatorCallMethod();
		new OperatorIndex();
		new OperatorDump();
		new OperatorLog();
		new Operator("null", -1, null);
		new Operator("+", 6, Plus, Plus);
		new Operator("-", 6, Minus, Minus);
		new Operator("*", 5, Mul);
		new Operator("/", 5, Div);
		new Operator("^", 11, Pow);
		new Operator("%", 5, Mod);
		new Operator("=", 9, Eq);
		new Operator("==", 9, Eq);
		new Operator("!=", 9, NEq);
		new Operator(">", 8, Gt);
		new Operator(">=", 8, GtEq);
		new Operator("<", 8, Lt);
		new Operator("<=", 8, LtEq);
		new Operator("!", 3, null, Not);
		new Operator("not", 3, null, Not);
		new Operator("!!", 3, null, NotNot);
		new Operator("||", 14, Or);
		new Operator("or", 14, Or);
		new Operator("&&", 13, And);
		new Operator("and", 13, And);
	}

	public static void InitPrecompiledExpressions()
	{
		MethodInfo[] methods = typeof(Expression).GetMethods();
		foreach (MethodInfo methodInfo in methods)
		{
			if (!methodInfo.IsStatic || !methodInfo.IsPublic || methodInfo.ReturnType != typeof(Value))
			{
				continue;
			}
			ParameterInfo[] parameters = methodInfo.GetParameters();
			if (parameters.Length == 3 && parameters[0].ParameterType == typeof(Context) && parameters[1].ParameterType == typeof(Value[]) && parameters[2].ParameterType == typeof(bool))
			{
				try
				{
					if (Delegate.CreateDelegate(typeof(PredefinedFunc), methodInfo) is PredefinedFunc predefinedFunc)
					{
						predefined_funcs.Add(methodInfo.Name, predefinedFunc);
					}
				}
				catch
				{
					Game.Log("Failed to register predefined func: " + methodInfo.Name, Game.LogType.Error);
				}
			}
			else
			{
				if (parameters.Length != 2 || !(parameters[0].ParameterType == typeof(Context)) || !(parameters[1].ParameterType == typeof(bool)))
				{
					continue;
				}
				try
				{
					if (Delegate.CreateDelegate(typeof(CompiledExpression), methodInfo) is CompiledExpression func)
					{
						precompiled.Add(methodInfo.Name, new PrecompiledDescr(methodInfo.Name, func));
					}
				}
				catch
				{
					Game.Log("Failed to register precompiled expression: " + methodInfo.Name, Game.LogType.Error);
				}
			}
		}
	}

	public static void Test()
	{
		string text = "a.x + a.y";
		Expression expression = Parse(text, allow_compilation: false);
		if (expression == null || expression.type == Type.Invalid)
		{
			Game.Log("Failed to parse: " + text, Game.LogType.Error);
			return;
		}
		Value value = Value.Unknown;
		int num = 1000000;
		Stopwatch stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < num; i++)
		{
			value = expression.Calc(test_context);
		}
		long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
		Game.Log(expression.ToString() + " -> " + value.ToString() + ", " + elapsedMilliseconds + "ms", Game.LogType.Message);
		if (!expression.Compile())
		{
			Game.Log("Failed to compile: " + text, Game.LogType.Warning);
			return;
		}
		stopwatch = Stopwatch.StartNew();
		for (int j = 0; j < num; j++)
		{
			value = expression.Calc(test_context);
		}
		elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
		Game.Log("Compiled " + expression.ToString() + " -> " + value.ToString() + ", " + elapsedMilliseconds + "ms", Game.LogType.Message);
	}

	public static void UnitTest()
	{
		UnitTest("2 + 3", 5);
		UnitTest("2.5 + 3", 5.5f);
		UnitTest("2 + 3.5", 5.5f);
		UnitTest("2.5 + 3.5", 6f);
		UnitTest("'Hell' + 'o'", "Hello");
		UnitTest("'Hell' + 0", "Hell0");
		UnitTest("'Val: ' + 2.5", "Val: 2.5");
		UnitTest("4 + 'orba'", "4orba");
		UnitTest("2.5 + 'f'", "2.5f");
		UnitTest("+2", 2);
		UnitTest("+2.5", 2.5f);
		UnitTest("2 - 3", -1);
		UnitTest("2.5 - 3", -0.5f);
		UnitTest("2 - 3.5", -1.5f);
		UnitTest("2.5 - 3.5", -1f);
		UnitTest("-(2)", -2);
		UnitTest("-(-2)", 2);
		UnitTest("-(2.5)", -2.5f);
		UnitTest("2 * 3", 6);
		UnitTest("2.5 * 3", 7.5f);
		UnitTest("2 * 3.5", 7f);
		UnitTest("2.5 * 3.5", 8.75f);
		UnitTest("3 / 2", 1);
		UnitTest("3.5 / 2", 1.75f);
		UnitTest("3 / 2.5", 1.2f);
		UnitTest("3 % 2", 1);
		UnitTest("3.5 % 2", 1.5f);
		UnitTest("3 % 2.5", 0.5f);
		UnitTest("2 = 2", true);
		UnitTest("2 = 3", false);
		UnitTest("2 = 2.0", true);
		UnitTest("2 = 2.5", false);
		UnitTest("2.5 = 2.5", true);
		UnitTest("2.5 = 3.5", false);
		UnitTest("'2' = '2'", true);
		UnitTest("2 = '2'", false);
		UnitTest("2 != 2", false);
		UnitTest("2 != 3", true);
		UnitTest("2 != 2.0", false);
		UnitTest("2 != 2.5", true);
		UnitTest("2.5 != 2.5", false);
		UnitTest("2.5 != 3.5", true);
		UnitTest("'2' != '2'", false);
		UnitTest("2 != '2'", true);
		UnitTest("2 > 2", false);
		UnitTest("2 > 2.5", false);
		UnitTest("2.5 > 2", true);
		UnitTest("2.5 > 3.5", false);
		UnitTest("2 >= 2", true);
		UnitTest("2 >= 2.5", false);
		UnitTest("2.5 >= 2", true);
		UnitTest("2.5 >= 3.5", false);
		UnitTest("2 < 2", false);
		UnitTest("2 < 2.5", true);
		UnitTest("2.5 < 2", false);
		UnitTest("2.5 < 3.5", true);
		UnitTest("2 <= 2", true);
		UnitTest("2 <= 2.5", true);
		UnitTest("2.5 <= 2", false);
		UnitTest("2.5 <= 3.5", true);
		UnitTest("!0", true);
		UnitTest("!x", true);
		UnitTest("!1", false);
		UnitTest("!2", false);
		UnitTest("!1.5", false);
		UnitTest("!0.0", true);
		UnitTest("!'x'", false);
		UnitTest("2 || 0", 2);
		UnitTest("2 || 3", 2);
		UnitTest("0 || 2.5", 2.5f);
		UnitTest("0.0 || 2.5", 2.5f);
		UnitTest("x || 'x'", "x");
		UnitTest("2 && 0", 0);
		UnitTest("2 && 3", 3);
		UnitTest("0 && 2.5", 0);
		UnitTest("0.0 && 2.5", 0f);
		UnitTest("x && 'x'", Value.Unknown);
		UnitTest("a.x", "x");
		UnitTest("x.y", Value.Unknown);
		UnitTest("a.x + a.y", "xy");
		UnitTest("F0()", "F0");
		UnitTest("F1(x)", "F1");
		UnitTest("F2(x,y)", "F2");
		UnitTest("F(x)", Value.Unknown);
		UnitTest("a.M0()", "M0");
		UnitTest("a.M1(x)", "M1");
		UnitTest("a.M2(x,y)", "M2");
		UnitTest("a.M(x)", Value.Unknown);
		UnitTest("a[]", Value.Unknown);
		UnitTest("a[1]", "[]");
		UnitTest("a[1,2]", "[,]");
		UnitTest("#TestPrecompiled", "TestPrecompiled");
		UnitTest("#TestPrecompiled + 'Expression'", "TestPrecompiledExpression");
	}

	public static Value TestPrecompiled(Context context, bool as_value)
	{
		return "TestPrecompiled";
	}

	public static void UnitTest(string expr_str, Value result)
	{
		Expression expression = Parse(expr_str, allow_compilation: false);
		if (expression == null || expression.type == Type.Invalid)
		{
			Game.Log("Failed to parse: " + expr_str, Game.LogType.Error);
			return;
		}
		Value value = expression.Calc(test_context);
		if (value != result)
		{
			Game.Log("FAILED: " + expression.ToString() + " -> " + value.ToString() + " != " + result.ToString(), Game.LogType.Error);
		}
		else
		{
			if (!compile_enabled)
			{
				return;
			}
			if (!expression.Compile())
			{
				Game.Log("Failed to compile: " + expr_str, Game.LogType.Error);
				return;
			}
			value = expression.Calc(test_context);
			if (value != result)
			{
				Game.Log("FAILED(compiled): " + expression.ToString() + " -> " + value.ToString() + " != " + result.ToString(), Game.LogType.Error);
			}
		}
	}

	public static Value RndF(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2 && args.Length != 3)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		float num = args[0].Float();
		float num2 = args[1].Float();
		float num3 = defsContext.game.Random(num, num2);
		if (args.Length == 3)
		{
			float num4 = args[2].Float();
			if (num4 > 0f)
			{
				num3 /= num4;
				num3 = (float)Math.Round(num3);
				num3 *= num4;
				if (num3 > num2)
				{
					num3 = num2;
				}
			}
		}
		return num3;
	}

	public static Value RndI(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2 && args.Length != 3)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		int num = args[0].Int();
		int num2 = args[1].Int();
		int num3 = defsContext.game.Random(num, num2 + 1);
		if (args.Length == 3)
		{
			int num4 = args[2].Int();
			if (num4 > 0)
			{
				num3 /= num4;
				num3 *= num4;
				if (num3 > num2)
				{
					num3 = num2;
				}
			}
		}
		return num3;
	}

	public static Value RndItem(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 1)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		IList list = args[0].Get<IList>();
		if (list == null)
		{
			return Value.Unknown;
		}
		int count = list.Count;
		if (count <= 0)
		{
			return Value.Unknown;
		}
		int index = defsContext.game.Random(0, count);
		return new Value(list[index]);
	}

	public static Value Chance(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 1)
		{
			return Value.Unknown;
		}
		float num = args[0].Float();
		if (num <= 0f)
		{
			return false;
		}
		if (num >= 100f)
		{
			return true;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		return defsContext.game.Random(0f, 100f) <= num;
	}

	public static Value IntervalIdx(Context context, Value[] args, bool as_value)
	{
		if (args.Length < 2)
		{
			return Value.Unknown;
		}
		Value value = args[0];
		if (!value.is_number)
		{
			return Value.Unknown;
		}
		float num = value.Float();
		int num2 = -1;
		for (int i = 1; i < args.Length; i++)
		{
			Value value2 = args[i];
			if (!value2.is_number)
			{
				return Value.Unknown;
			}
			float num3 = value2.Float();
			if (num >= num3)
			{
				num2 = i - 1;
			}
		}
		return num2;
	}

	public static Value Match(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2 && args.Length != 3)
		{
			return Value.Unknown;
		}
		string s = args[0].String();
		string pattern = args[1].String();
		bool case_insensitive = false;
		if (args.Length > 2)
		{
			case_insensitive = args[2].Bool();
		}
		return Game.Match(s, pattern, case_insensitive);
	}

	public static Value If(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 3 || !args[0].is_number)
		{
			return Value.Unknown;
		}
		if (args[0] != 0)
		{
			return args[1];
		}
		return args[2];
	}

	public static Value MapCA(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 3 && args.Length != 4)
		{
			return Value.Unknown;
		}
		Kingdom kingdom = args[0].Get<Kingdom>();
		if (kingdom == null)
		{
			return Value.Unknown;
		}
		CrownAuthority crownAuthority = kingdom.GetCrownAuthority();
		if (crownAuthority == null)
		{
			return Value.Unknown;
		}
		float num = crownAuthority.GetValue();
		float num2 = crownAuthority.Min();
		float num3 = crownAuthority.Max();
		float num4;
		float num5;
		if (args.Length == 3)
		{
			num4 = args[1].Float();
			num5 = args[2].Float();
		}
		else if (num < 0f)
		{
			num3 = 0f;
			num4 = args[1].Float();
			num5 = args[2].Float();
		}
		else
		{
			if (!(num > 0f))
			{
				return args[2].Float();
			}
			num2 = 0f;
			num4 = args[2].Float();
			num5 = args[3].Float();
		}
		if (num3 == num2)
		{
			return num4;
		}
		return num4 + (num - num2) / (num3 - num2) * (num5 - num4);
	}

	public static Value MapCL(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 3)
		{
			return Value.Unknown;
		}
		Character character = args[0].Get<Character>();
		if (character == null)
		{
			return Value.Unknown;
		}
		int classLevel = character.GetClassLevel();
		float num = 0f;
		float num2 = character.GetMaxClassLevel();
		float num3 = args[1].Float();
		float num4 = args[2].Float();
		return num3 + ((float)classLevel - num) / (num2 - num) * (num4 - num3);
	}

	public static Value MapRelationship(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 4 && args.Length != 5)
		{
			return Value.Unknown;
		}
		Kingdom kingdom = args[0].Get<Kingdom>();
		if (kingdom == null)
		{
			return Value.Unknown;
		}
		Kingdom kingdom2 = args[1].Get<Kingdom>();
		if (kingdom2 == null)
		{
			return Value.Unknown;
		}
		float relationship = kingdom.GetRelationship(kingdom2);
		float num = -1000f;
		float num2 = 1000f;
		float num3;
		float num4;
		if (args.Length == 4)
		{
			num3 = args[2].Float();
			num4 = args[3].Float();
		}
		else if (relationship < 0f)
		{
			num2 = 0f;
			num3 = args[2].Float();
			num4 = args[3].Float();
		}
		else
		{
			if (!(relationship > 0f))
			{
				return args[3].Float();
			}
			num = 0f;
			num3 = args[3].Float();
			num4 = args[4].Float();
		}
		if (num2 == num)
		{
			return num3;
		}
		return num3 + (relationship - num) / (num2 - num) * (num4 - num3);
	}

	public static Value MaxSSumsBlend(Context context, Value[] args, bool as_value)
	{
		if (args.Length < 3)
		{
			return Value.Unknown;
		}
		Kingdom kingdom = args[0].Get<Kingdom>();
		Kingdom kingdom2 = args[1].Get<Kingdom>();
		float num = args[2].Float();
		float bias = args[3].Float();
		if (num == 0f)
		{
			return 0;
		}
		if (kingdom == null || kingdom2 == null || num < 0f || num > (float)kingdom.DiplomaticGoldLevels())
		{
			return Value.Unknown;
		}
		float num2 = 100f;
		if (args.Length > 4)
		{
			num2 = args[4].Float();
		}
		return kingdom.MaxSSumsBlend(kingdom2, num, bias, num2);
	}

	public static Value SSum(Context context, Value[] args, bool as_value)
	{
		if (args.Length < 2)
		{
			return Value.Unknown;
		}
		Kingdom kingdom = args[0].Get<Kingdom>();
		float num = args[1].Float();
		if (num == 0f)
		{
			return 0;
		}
		if (kingdom == null || num < 0f || num > (float)kingdom.DiplomaticGoldLevels())
		{
			return Value.Unknown;
		}
		float num2 = 100f;
		if (args.Length > 2)
		{
			num2 = args[2].Float();
		}
		return kingdom.GetSSum(num, num2);
	}

	public static Value pdv(Context context, Value[] args, bool as_value)
	{
		if (args.Length < 1)
		{
			return Value.Unknown;
		}
		Game game = (context as DefsContext)?.game;
		if (game == null)
		{
			return Value.Unknown;
		}
		int num = ((game.rules == null) ? 1 : game.rules.ai_difficulty);
		if (num >= args.Length)
		{
			return args[^1];
		}
		return args[num];
	}

	public static Value pdv_pow(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 3 || !args[0].is_number || !args[1].is_number || !args[2].is_number)
		{
			return Value.Unknown;
		}
		Game game = (context as DefsContext)?.game;
		if (game == null)
		{
			return Value.Unknown;
		}
		float num = 1f;
		float num2 = 0f;
		float num3 = 1f;
		if (game.rules != null)
		{
			num = game.rules.ai_difficulty;
			num2 = game.rules.min_ai_difficulty;
			num3 = game.rules.max_ai_difficulty;
		}
		Value value = args[0];
		Value value2 = args[1];
		Value value3 = args[2];
		float num4 = (float)Math.Pow((num - num2) / (num3 - num2), (float)value3);
		return (float)value + (float)((int)value2 - (int)value) * num4;
	}

	public static Value GetRule(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		Object obj = args[0].Get<Object>();
		if (obj == null)
		{
			return Value.Unknown;
		}
		string ruleName = args[1].String();
		return obj.GetComponent<ObjRules>()?.rules?.Find((GameRule r) => r.def.field.key == ruleName);
	}

	public static Value CheckRecentRule(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		Object obj = args[0].Get<Object>();
		if (obj == null)
		{
			return Value.Unknown;
		}
		string ruleName = args[1].String();
		GameRule gameRule = obj.GetComponent<ObjRules>()?.rules?.Find((GameRule r) => r.def.field.key == ruleName);
		if (gameRule == null)
		{
			return false;
		}
		bool flag = gameRule.def.instant || gameRule.IsActive();
		return flag;
	}

	public static Value SumAllEnemyIdleSpies(Context context, bool as_vale)
	{
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = ((var.obj_val as Vars)?.obj.obj_val as Action)?.own_character;
		if (character == null || character.mission_kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		Kingdom mission_kingdom = character.mission_kingdom;
		for (int i = 0; i < mission_kingdom.court.Count; i++)
		{
			Character character2 = mission_kingdom.court[i];
			if (character2 != null && character2.IsSpy())
			{
				num += (float)character2.GetClassLevel() * (character2.IsKing() ? 1f : defsContext.field.GetFloat("mod_non_king_class_level", null, 1f));
			}
		}
		num = Math.Max(defsContext.field.GetFloat("min"), num);
		num = Math.Min(defsContext.field.GetFloat("max"), num);
		return num * defsContext.field.GetFloat("mod_result", null, 1f);
	}

	public static Value SumAllEnemyRebelLevels(Context context, bool as_vale)
	{
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = ((var.obj_val as Vars)?.obj.obj_val as Action)?.own_character;
		if (character == null || character.mission_kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		Kingdom mission_kingdom = character.mission_kingdom;
		for (int i = 0; i < mission_kingdom.armies_in.Count; i++)
		{
			Rebel rebel = mission_kingdom.armies_in[i].rebel;
			if (rebel != null)
			{
				num += (float)rebel.character.GetClassLevel();
			}
		}
		return num * defsContext.field.GetFloat("mod_result", null, 1f);
	}

	public static Value OurMarriedPrincessesForTheirKingdom(Context context, bool as_vale)
	{
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = ((var.obj_val as Vars)?.obj.obj_val as Action)?.own_character;
		if (character == null || character.mission_kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		Kingdom mission_kingdom = character.mission_kingdom;
		for (int i = 0; i < mission_kingdom.marriages.Count; i++)
		{
			Marriage marriage = mission_kingdom.marriages[i];
			if (marriage != null && marriage.wife != null && marriage.husband != null && marriage.kingdom_husband == character.mission_kingdom && marriage.kingdom_wife == character.GetKingdom())
			{
				num += (marriage.husband.IsKing() ? defsContext.field.GetFloat("modKing") : defsContext.field.GetFloat("modPrince"));
			}
		}
		return num;
	}

	public static Value TheirMarriedPrincessesForOurKingdom(Context context, bool as_vale)
	{
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = ((var.obj_val as Vars)?.obj.obj_val as Action)?.own_character;
		if (character == null || character.mission_kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		Kingdom mission_kingdom = character.mission_kingdom;
		for (int i = 0; i < mission_kingdom.marriages.Count; i++)
		{
			Marriage marriage = mission_kingdom.marriages[i];
			if (marriage != null && marriage.wife != null && marriage.husband != null && marriage.kingdom_husband == character.GetKingdom() && marriage.kingdom_wife == character.mission_kingdom)
			{
				num += (marriage.husband.IsKing() ? defsContext.field.GetFloat("modKing") : defsContext.field.GetFloat("modPrince"));
			}
		}
		return num;
	}

	public static Value KingdomsSizeDifference(Context context, bool as_vale)
	{
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = ((var.obj_val as Vars)?.obj.obj_val as Action)?.own_character;
		if (character == null || character.mission_kingdom == null)
		{
			return 0;
		}
		float num = character.GetKingdom().realms.Count;
		float num2 = character.mission_kingdom.realms.Count;
		float num3 = defsContext.field.GetFloat("min");
		float num4 = defsContext.field.GetFloat("max");
		float num5 = defsContext.field.GetFloat("timesLarger", null, 1f) - 1f;
		float num6 = num / num2;
		if (num2 > num)
		{
			num6 = -1f / num6;
		}
		num6 -= (float)Math.Sign(num6);
		return (float)map(null, new Value[5]
		{
			num6,
			0f - num5,
			num5,
			num3,
			num4
		}, as_value: true);
	}

	public static Value GetImportGoodDef(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Character character))
		{
			return Value.Unknown;
		}
		string good_name = args[1].String();
		return character.game.economy.GetGoodDef(good_name);
	}

	public static Value GetImportCommerceUpkeep(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Character character))
		{
			return Value.Unknown;
		}
		string name = args[1].String();
		object obj = context.GetVar(null, "context_vars").Object();
		float discount = 0f;
		object obj2 = obj;
		if (obj2 == null)
		{
			goto IL_00bf;
		}
		if (!(obj2 is LucrativeImportAction lucrativeImportAction))
		{
			if (!(obj2 is Opportunity opportunity))
			{
				goto IL_00bf;
			}
			discount = opportunity.action.def.field.GetFloat("discount");
		}
		else
		{
			discount = lucrativeImportAction.def.field.GetFloat("discount");
		}
		goto IL_00ca;
		IL_00ca:
		return character.CalcImportedGoodCommerce(name, discount);
		IL_00bf:
		Game.Log("Failed to retrieve discount!", Game.LogType.Error);
		goto IL_00ca;
	}

	public static Value GetImportGoldUpkeep(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Character character))
		{
			return Value.Unknown;
		}
		string name = args[1].String();
		return character.CalcImportedGoodGoldUpkeep(name);
	}

	public static Value CalcMoraleInOwnRealm(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Army army = character.GetArmy();
		if (army?.morale == null)
		{
			return 0;
		}
		return army.morale.morale_in_own_realm;
	}

	public static Value CalcMoraleInAlliedRealm(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Army army = character.GetArmy();
		if (army?.morale == null)
		{
			return 0;
		}
		return army.morale.morale_in_allied_realm;
	}

	public static Value CalcMoraleInNeutralRealm(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Army army = character.GetArmy();
		if (army?.morale == null)
		{
			return 0;
		}
		return army.morale.morale_in_neutral_realm;
	}

	public static Value CalcMoraleInEnemyRealm(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Army army = character.GetArmy();
		if (army?.morale == null)
		{
			return 0;
		}
		return army.morale.morale_in_enemy_realm;
	}

	public static Value CalcMoraleEnemyRealmKeeps(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Vars vars = var.Get<Vars>();
		Character character = vars?.Get<Character>("obj");
		Morale morale = null;
		Realm realm = null;
		IRelationCheck obj = null;
		if (character != null)
		{
			Army army = character.GetArmy();
			if (army != null)
			{
				morale = army.morale;
				realm = army.realm_in;
				obj = army.GetStanceObj();
			}
		}
		else
		{
			realm = vars?.Get<Realm>("obj");
			if (realm != null)
			{
				morale = realm.castle.morale;
				obj = realm.GetStanceObj();
			}
		}
		if (morale == null || realm == null)
		{
			return 0;
		}
		float num = 0f;
		for (int i = 0; i < realm.settlements.Count; i++)
		{
			Settlement settlement = realm.settlements[i];
			if (settlement.IsActiveSettlement() && settlement.type == "Keep" && settlement.IsEnemy(obj))
			{
				num += morale.def.morale_per_enemy_keep;
			}
		}
		return num;
	}

	public static Value CalcMoraleFriendlyRealmKeeps(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Vars vars = var.Get<Vars>();
		Character character = vars?.Get<Character>("obj");
		Morale morale = null;
		Realm realm = null;
		IRelationCheck obj = null;
		if (character != null)
		{
			Army army = character.GetArmy();
			if (army != null)
			{
				morale = army.morale;
				realm = army.realm_in;
				obj = army.GetStanceObj();
			}
		}
		else
		{
			realm = vars?.Get<Realm>("obj");
			if (realm != null)
			{
				morale = realm.castle.morale;
				obj = realm.GetStanceObj();
			}
		}
		if (morale == null || realm == null)
		{
			return 0;
		}
		float num = 0f;
		for (int i = 0; i < realm.settlements.Count; i++)
		{
			Settlement settlement = realm.settlements[i];
			if (settlement.IsActiveSettlement() && settlement.type == "Keep" && !settlement.IsEnemy(obj))
			{
				num += morale.def.morale_per_friendly_keep;
			}
		}
		return num;
	}

	public static Value CalcMoraleKingdomFood(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Army army = character.GetArmy();
		Morale morale = army?.morale;
		if (morale == null)
		{
			return 0;
		}
		Kingdom kingdom = army.GetKingdom();
		float num = 0f;
		if (kingdom.type == Kingdom.Type.Regular)
		{
			float num2 = Game.map(kingdom.GetSufficentFoodMod(), kingdom.sufficient_food_min, kingdom.sufficient_food_max, morale.def.starvation_kingdom_morale_penalty, 0f);
			num2 = (float)Math.Ceiling(num2);
			num += 0f - num2;
		}
		return num;
	}

	public static Value CalcMoraleFreeOccupied(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Army army = character.GetArmy();
		Morale morale = army?.morale;
		Realm realm = army?.realm_in;
		if (morale == null || realm == null)
		{
			return 0;
		}
		Kingdom kingdom = army.GetKingdom();
		Kingdom kingdom2 = realm.GetKingdom();
		if (kingdom2 == kingdom)
		{
			return 0;
		}
		if (realm.pop_majority.kingdom == kingdom && kingdom.HasStance(kingdom2, RelationUtils.Stance.War))
		{
			return kingdom.GetStat(Stats.ks_army_morale_vs_occupators);
		}
		return 0;
	}

	public static Value CalcMoraleRestorePapacy(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Army army = character.GetArmy();
		Morale obj = army?.morale;
		Realm realm = army?.realm_in;
		if (obj == null || realm == null)
		{
			return 0;
		}
		Kingdom kingdom = army.GetKingdom();
		float num = 0f;
		if (army.battle != null && army.battle.defender_kingdom != null && army.battle.attacker_kingdom != null)
		{
			int num2 = ((!army.battle.attackers.Contains(army)) ? army.battle.attacker_kingdom.id : army.battle.defender_kingdom.id);
			Game game = army.game;
			if (game?.religions?.catholic?.hq_realm == null)
			{
				return Value.Unknown;
			}
			if (game.religions.catholic.hq_realm.kingdom_id != game.religions.catholic.hq_kingdom.id && game.religions.catholic.hq_realm.kingdom_id == num2)
			{
				num += kingdom.GetStat(Stats.ks_army_morale_restoring_papacy);
			}
		}
		return num;
	}

	public static Value CalcMoraleKingWarfareAbility(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Vars vars = var.Get<Vars>();
		Character character = vars?.Get<Character>("obj");
		Kingdom kingdom = null;
		kingdom = ((character == null) ? (vars?.Get<Realm>("obj"))?.GetKingdom() : character.GetArmy()?.GetKingdom());
		if (kingdom == null)
		{
			return 0;
		}
		int num = kingdom?.GetKing()?.GetKingAbility("Warfare") ?? 0;
		return 0f + (float)num;
	}

	public static Value CalcRebellionRankMorale(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Rebel rebel = character.GetArmy()?.rebel;
		if (rebel?.def == null)
		{
			return 0;
		}
		float num = 0f;
		if (rebel.IsLeader())
		{
			Rebellion rebellion = rebel.rebellion;
			if (rebellion == null)
			{
				return num;
			}
			num += (float)rebel.army.leader.GetClassLevel() * rebel.army.morale.def.rebel_leader_level_morale_mod;
			for (int i = 0; i < rebellion.rebels.Count; i++)
			{
				Rebel rebel2 = rebellion.rebels[i];
				if (rebel2 != rebel)
				{
					num = ((!rebel2.IsGeneral()) ? (num + rebel.army.morale.def.rebel_leader_morale_per_supporter) : (num + rebel.army.morale.def.rebel_leader_morale_per_lieutenant));
				}
			}
		}
		else
		{
			Rebellion rebellion2 = rebel.rebellion;
			if (rebellion2 == null)
			{
				return num;
			}
			if (rebellion2?.leader?.army?.leader != null)
			{
				num += (float)rebellion2.leader.army.leader.GetClassLevel() * rebel.army.morale.def.rebel_supporter_leader_level_morale_mod;
			}
			for (int j = 0; j < rebellion2.rebels.Count; j++)
			{
				if (rebellion2.rebels[j] != rebel)
				{
					num += rebel.army.morale.def.rebel_supporter_morale_per_army;
				}
			}
		}
		return num;
	}

	public static Value CalcRebellionFamousMorale(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Rebel rebel = character.GetArmy()?.rebel;
		Rebellion rebellion = rebel?.rebellion;
		if (rebel?.def == null || rebellion == null || !rebellion.IsFamous())
		{
			return 0;
		}
		float num = 0f;
		num = (rebel.IsLeader() ? rebel.army.morale.def.rebel_famous_morale_leader : ((!rebel.IsGeneral()) ? rebel.army.morale.def.rebel_famous_morale : rebel.army.morale.def.rebel_famous_morale_lieutenant));
		return num;
	}

	public static Value CalcRebelOccupiedRealmsMorale(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Rebel rebel = character.GetArmy()?.rebel;
		if (rebel == null)
		{
			return 0;
		}
		float num = 0f;
		Rebellion rebellion = rebel.rebellion;
		if (rebellion?.occupiedRealms == null)
		{
			return num;
		}
		num += (float)rebellion.occupiedRealms.Count * rebel.army.morale.def.rebel_morale_per_occupied_province;
		return num;
	}

	public static Value CalcCampingMorale(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Army army = character.GetArmy();
		if (army == null)
		{
			return 0;
		}
		float num = 0f;
		if (army.isCamping)
		{
			num += army.morale.def.morale_if_camping;
		}
		return num;
	}

	public static Value CalcMoraleWarBonus(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		Kingdom kingdom = null;
		Kingdom kingdom2 = null;
		if (character != null)
		{
			Army army = character.GetArmy();
			if (army?.battle != null)
			{
				if (army.battle.attackers.Contains(army))
				{
					kingdom = army.battle.attacker_kingdom;
					kingdom2 = army.battle.defender_kingdom;
				}
				else
				{
					kingdom2 = army.battle.attacker_kingdom;
					kingdom = army.battle.defender_kingdom;
				}
			}
		}
		if (kingdom == null || kingdom2 == null)
		{
			return 0;
		}
		return War.GetBonus(kingdom, kingdom2, "army_morale");
	}

	public static Value CalcMoraleNavigationSkill(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Army army = character.GetArmy();
		if (army == null)
		{
			return 0;
		}
		if (!army.is_in_water)
		{
			return false;
		}
		return character.GetStat(Stats.cs_army_morale_in_sea);
	}

	public static Value CalcMoralePopMajorityBonus(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Army army = character.GetArmy();
		if (army == null)
		{
			return 0;
		}
		Morale morale = army.morale;
		Realm realm_in = army.realm_in;
		if (realm_in == null)
		{
			return 0;
		}
		Kingdom kingdom = army.GetKingdom();
		Kingdom kingdom2 = realm_in.GetKingdom();
		if (kingdom2 == null || (kingdom2.GetStance(kingdom) & RelationUtils.Stance.War) == 0)
		{
			return 0;
		}
		if (realm_in.pop_majority.kingdom == kingdom)
		{
			return morale.def.morale_same_pop_majority;
		}
		return 0;
	}

	public static Value CalcMoraleJihad(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Character character = var.Get<Vars>()?.Get<Character>("obj");
		if (character == null)
		{
			return 0;
		}
		Army army = character.GetArmy();
		Realm realm = army?.realm_in;
		if (realm == null)
		{
			return 0;
		}
		Kingdom kingdom = army.GetKingdom();
		if (kingdom?.jihad == null)
		{
			return 0;
		}
		Kingdom kingdom2 = realm.GetKingdom();
		if (kingdom == null || kingdom2 == null)
		{
			return 0;
		}
		if (!kingdom.IsEnemy(kingdom2) && !kingdom.IsOwnStance(kingdom2))
		{
			return 0;
		}
		if (!kingdom.IsCaliphate())
		{
			return 0;
		}
		return War.GetBonus(kingdom, kingdom.jihad.GetEnemyLeader(kingdom), "army_morale");
	}

	public static Value BattleGetEnemy(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Battle battle))
		{
			return Value.Unknown;
		}
		MapObject mapObject = args[1].Object() as Army;
		if (mapObject == null)
		{
			mapObject = args[1].Object() as Settlement;
		}
		if (mapObject == null)
		{
			mapObject = (args[1].Object() as Character).GetArmy();
		}
		if (mapObject == null)
		{
			mapObject = (args[1].Object() as Rebel).army;
		}
		if (mapObject == null)
		{
			return Value.Unknown;
		}
		if (battle.settlement == mapObject)
		{
			return battle.attacker;
		}
		for (int i = 0; i < battle.defenders.Count; i++)
		{
			if (battle.defenders[i] == mapObject)
			{
				return battle.attacker;
			}
		}
		for (int j = 0; j < battle.attackers.Count; j++)
		{
			if (battle.attackers[j] == mapObject)
			{
				return battle.defender;
			}
		}
		return Value.Unknown;
	}

	public static Value CalcMarshSpawnCondition(Context context, bool as_value)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Battle battle = var.Get<Battle>();
		if (battle == null)
		{
			return 0;
		}
		if (battle.is_siege || battle.type == Battle.Type.Naval)
		{
			return 0;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < battle.def.tt_grid_height; i++)
		{
			for (int j = 0; j < battle.def.tt_grid_width; j++)
			{
				switch (battle.tt_grid[j, i])
				{
				case TerrainType.River:
					num++;
					continue;
				case TerrainType.Ocean:
					continue;
				}
				bool flag = false;
				for (int k = -1; k <= 1; k++)
				{
					for (int l = -1; l <= 1; l++)
					{
						if (l != 0 || k != 0)
						{
							int num3 = j + l;
							int num4 = i + k;
							if (num3 >= 0 && num3 < battle.def.tt_grid_width && num4 >= 0 && num4 < battle.def.tt_grid_height && battle.tt_grid[num3, num4] == TerrainType.Ocean)
							{
								flag = true;
								break;
							}
						}
					}
					if (flag)
					{
						break;
					}
				}
				if (flag)
				{
					num2++;
				}
			}
		}
		return num * 10 + num2 * 10;
	}

	public static Value CalcRiverSpawnCondition(Context context, bool as_value)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Battle battle = var.Get<Battle>();
		if (battle == null)
		{
			return 0;
		}
		if (battle.is_siege || battle.type == Battle.Type.Naval)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < battle.def.tt_grid_height; i++)
		{
			for (int j = 0; j < battle.def.tt_grid_width; j++)
			{
				if (battle.tt_grid[j, i] == TerrainType.River)
				{
					num++;
				}
			}
		}
		return (num >= 3) ? 100 : 0;
	}

	public static Value CalcOceanCoastSpawnCondition(Context context, bool as_value)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Battle battle = var.Get<Battle>();
		if (battle == null)
		{
			return 0;
		}
		if (battle.is_siege || battle.type == Battle.Type.Naval)
		{
			return 0;
		}
		int num = 0;
		int num2 = battle.def.tt_grid_height * battle.def.tt_grid_width;
		for (int i = 0; i < battle.def.tt_grid_height; i++)
		{
			for (int j = 0; j < battle.def.tt_grid_width; j++)
			{
				if (battle.tt_grid[j, i] == TerrainType.Ocean)
				{
					num++;
				}
			}
		}
		return ((float)num >= (float)num2 * 0.33f) ? 100 : 0;
	}

	public static Value CalcWoodsSpawnCondition(Context context, bool as_value)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Battle battle = var.Get<Battle>();
		if (battle == null)
		{
			return 0;
		}
		if (battle.is_siege || battle.type == Battle.Type.Naval)
		{
			return 0;
		}
		int num = 0;
		int num2 = battle.def.tt_grid_height * battle.def.tt_grid_width;
		for (int i = 0; i < battle.def.tt_grid_height; i++)
		{
			for (int j = 0; j < battle.def.tt_grid_width; j++)
			{
				if (battle.tt_grid[j, i] == TerrainType.Forest)
				{
					num++;
				}
			}
		}
		return ((float)num >= (float)num2 * 0.25f && (float)num < (float)num2 * 0.5f) ? 100 : 0;
	}

	public static Value CalcDeepForestSpawnCondition(Context context, bool as_value)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Battle battle = var.Get<Battle>();
		if (battle == null)
		{
			return 0;
		}
		if (battle.is_siege || battle.type == Battle.Type.Naval)
		{
			return 0;
		}
		int num = 0;
		int num2 = battle.def.tt_grid_height * battle.def.tt_grid_width;
		for (int i = 0; i < battle.def.tt_grid_height; i++)
		{
			for (int j = 0; j < battle.def.tt_grid_width; j++)
			{
				if (battle.tt_grid[j, i] == TerrainType.Forest)
				{
					num++;
				}
			}
		}
		return ((float)num >= (float)num2 * 0.5f) ? 100 : 0;
	}

	public static Value CalcHillsSpawnCondition(Context context, bool as_value)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Battle battle = var.Get<Battle>();
		if (battle == null)
		{
			return 0;
		}
		if (battle.is_siege || battle.type == Battle.Type.Naval)
		{
			return 0;
		}
		int num = 0;
		int num2 = 0;
		int num3 = battle.def.tt_grid_height * battle.def.tt_grid_width;
		for (int i = 0; i < battle.def.tt_grid_height; i++)
		{
			for (int j = 0; j < battle.def.tt_grid_width; j++)
			{
				TerrainType num4 = battle.tt_grid[j, i];
				if (num4 == TerrainType.Hills)
				{
					num++;
				}
				if (num4 == TerrainType.Mountains)
				{
					num2++;
				}
			}
		}
		return ((float)(num + num2) >= (float)num3 * 0.19f && (float)num2 < (float)num3 * 0.19f) ? 100 : 0;
	}

	public static Value CalcMountainsSpawnCondition(Context context, bool as_value)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Battle battle = var.Get<Battle>();
		if (battle == null)
		{
			return 0;
		}
		if (battle.is_siege || battle.type == Battle.Type.Naval)
		{
			return 0;
		}
		int num = 0;
		int num2 = battle.def.tt_grid_height * battle.def.tt_grid_width;
		for (int i = 0; i < battle.def.tt_grid_height; i++)
		{
			for (int j = 0; j < battle.def.tt_grid_width; j++)
			{
				if (battle.tt_grid[j, i] == TerrainType.Mountains)
				{
					num++;
				}
			}
		}
		return ((float)num >= (float)num2 * 0.19f) ? 100 : 0;
	}

	public static Value CalcMoraleOutnumbered(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		int battle_side = squad.battle_side;
		BattleSimulation simulation = squad.simulation;
		float attack_power;
		float attack_power2;
		if (battle_side == 0)
		{
			attack_power = simulation.attacker_totals.attack_power;
			attack_power2 = simulation.defender_totals.attack_power;
		}
		else
		{
			attack_power = simulation.defender_totals.attack_power;
			attack_power2 = simulation.attacker_totals.attack_power;
		}
		float ratio = attack_power / (attack_power + attack_power2);
		return simulation.def.GetMoraleOutnumbered(ratio);
	}

	public static Value CalcMoraleVsOtherReligion(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		for (int i = 0; i < temp_enemy_kingdoms.Length; i++)
		{
			temp_enemy_kingdoms[i] = null;
		}
		MapObject mapObject = null;
		Battle battle = squad.simulation.battle;
		if (squad.army != null)
		{
			mapObject = squad.army;
		}
		else if (squad.garrison != null)
		{
			mapObject = squad.garrison.settlement;
		}
		if (mapObject == null)
		{
			return 0;
		}
		float num = 0f;
		Kingdom kingdom = mapObject.GetKingdom();
		Religion religion = kingdom.religion;
		if (religion == null)
		{
			return num;
		}
		float num2 = -1f;
		if (battle != null && battle.defender_kingdom != null && battle.attacker_kingdom != null)
		{
			if (squad.battle_side == 0)
			{
				temp_enemy_kingdoms[0] = battle.defender_kingdom;
				if (battle.defenders.Count > 0)
				{
					temp_enemy_kingdoms[1] = battle.defenders[0]?.GetKingdom();
				}
				if (battle.defenders.Count > 1)
				{
					temp_enemy_kingdoms[2] = battle.defenders[1]?.GetKingdom();
				}
			}
			else
			{
				if (battle.attackers.Count > 0)
				{
					temp_enemy_kingdoms[0] = battle.attackers[0]?.GetKingdom();
				}
				if (battle.attackers.Count > 1)
				{
					temp_enemy_kingdoms[1] = battle.attackers[1]?.GetKingdom();
				}
			}
			for (int j = 0; j < temp_enemy_kingdoms.Length; j++)
			{
				Religion religion2 = temp_enemy_kingdoms[j]?.religion;
				if (religion2 == null)
				{
					return num;
				}
				if (religion.DistTo(religion2) >= 2)
				{
					if (num2 == -1f)
					{
						num2 = kingdom.GetStat(Stats.ks_army_morale_vs_other_religion);
					}
					num = Math.Max(num, num2);
				}
			}
		}
		return num;
	}

	public static Value CalcMoraleRebels(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		int battle_side = squad.battle_side;
		BattleSimulation simulation = squad.simulation;
		Army army = squad.army;
		if (army != null && army.rebel != null)
		{
			return squad.def.bonus_enemy_rebels_morale.GetValue(simulation.battle, 1 - battle_side, null, null, use_battle_bonuses: false);
		}
		return 0;
	}

	public static Value CalcMoraleLosses(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		return (squad.damage - squad.initial_damage) * squad.simulation.def.morale_from_casualties;
	}

	public static Value CalcMoraleInitial(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		float num = 0f;
		for (int i = 0; i < squad.simulation.def.morale_factors.Count; i++)
		{
			if (squad.simulation.def.morale_factors[i].can_be_initial)
			{
				float num2 = squad.permanent_morale_factors[i];
				num += num2;
			}
		}
		return squad.initial_morale - num;
	}

	public static Value CalcMoraleFlankedLeft(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		Squad squad2 = squad.squad;
		if (squad2 == null)
		{
			return 0;
		}
		float left_threat = squad2.left_threat;
		if (left_threat < 0f)
		{
			return 0;
		}
		return squad.simulation.def.GetMoraleFlanked(left_threat);
	}

	public static Value CalcMoraleSupportedLeft(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		Squad squad2 = squad.squad;
		if (squad2 == null)
		{
			return 0;
		}
		float left_threat = squad2.left_threat;
		if (left_threat > 0f)
		{
			return 0;
		}
		return squad.simulation.def.GetMoraleFlanked(left_threat);
	}

	public static Value CalcMoraleFlankedRight(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		Squad squad2 = squad.squad;
		if (squad2 == null)
		{
			return 0;
		}
		float right_threat = squad2.right_threat;
		if (right_threat < 0f)
		{
			return 0;
		}
		return squad.simulation.def.GetMoraleFlanked(right_threat);
	}

	public static Value CalcMoraleSupportedRight(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		Squad squad2 = squad.squad;
		if (squad2 == null)
		{
			return 0;
		}
		float right_threat = squad2.right_threat;
		if (right_threat > 0f)
		{
			return 0;
		}
		return squad.simulation.def.GetMoraleFlanked(right_threat);
	}

	public static Value CalcMoraleFlankedRear(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		Squad squad2 = squad.squad;
		if (squad2 == null)
		{
			return 0;
		}
		float rear_threat = squad2.rear_threat;
		if (rear_threat < 0f)
		{
			return 0;
		}
		return squad.simulation.def.GetMoraleFlankedRear(rear_threat);
	}

	public static Value CalcMoraleSupportedRear(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		Squad squad2 = squad.squad;
		if (squad2 == null)
		{
			return 0;
		}
		float rear_threat = squad2.rear_threat;
		if (rear_threat > 0f)
		{
			return 0;
		}
		return squad.simulation.def.GetMoraleFlankedRear(rear_threat);
	}

	public static Value CalcMoraleCapturePoints(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		if (squad.squad == null)
		{
			return 0;
		}
		Battle battle = squad.simulation?.battle;
		if (battle?.capture_points == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < battle.capture_points.Count; i++)
		{
			CapturePoint capturePoint = battle.capture_points[i];
			if (capturePoint.battle_side == squad.battle_side && capturePoint.def.count_victory)
			{
				num++;
			}
		}
		BattleSimulation.Totals totals = ((squad.battle_side != 0) ? squad.simulation.defender_totals : squad.simulation.attacker_totals);
		return num - totals.starting_capture_point_count;
	}

	public static Value CalcMoraleShrinkFormation(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		BattleSimulation.Squad squad = var.Get<BattleSimulation.Squad>();
		if (squad == null)
		{
			return 0;
		}
		Squad squad2 = squad.squad;
		if (squad2 == null)
		{
			return 0;
		}
		return squad2.spacing == Squad.Spacing.Shrinken;
	}

	public static Value CalcArmySizeRatio(Context context, bool as_value)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Army army = context.GetVar(null, "context_vars").Get<Army>();
		if (army == null)
		{
			return Value.Unknown;
		}
		Battle battle = army.battle;
		if (battle == null)
		{
			return Value.Unknown;
		}
		int battle_side = army.battle_side;
		int num = CalcNumTroops(battle, battle_side);
		int num2 = CalcNumTroops(battle, 1 - battle_side);
		return (float)num / (float)num2;
	}

	private static int CalcNumTroops(Battle b, int side)
	{
		return CalcNumTroops(b.GetArmies(side));
	}

	private static int CalcNumTroops(List<Army> armies)
	{
		if (armies == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < armies.Count; i++)
		{
			Army a = armies[i];
			num += CalcNumTroops(a);
		}
		return num;
	}

	private static int CalcNumTroops(Army a)
	{
		int num = 0;
		for (int i = 0; i < a.units.Count; i++)
		{
			Unit unit = a.units[i];
			num += (int)unit.GetVar("num_troops");
		}
		return num;
	}

	public static Value GetSkillRank(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Character character))
		{
			return Value.Unknown;
		}
		string skill_name = args[1].String();
		if (string.IsNullOrEmpty(skill_name))
		{
			return Value.Unknown;
		}
		return character.GetSkillRank(skill_name);
	}

	public static Value IsPuppetOf(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		Character character = args[0].Object() as Character;
		Character character2 = args[1].Object() as Character;
		if (character == null || character2 == null || character2.puppets == null)
		{
			return Value.Unknown;
		}
		return character2.puppets.Contains(character);
	}

	public static Value SumRealmsMaxTradePower(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Kingdom kingdom = (var.obj_val as Vars).Get<Kingdom>("obj");
		if (kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			if (!kingdom.realms[i].IsOccupied() && !kingdom.realms[i].IsDisorder())
			{
				num += kingdom.realms[i].GetCommerce();
			}
		}
		return num;
	}

	public static Value InfluenceFromGovernedKeeps(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Kingdom kingdom = (var.obj_val as Vars).Get<Kingdom>("obj");
		if (kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			int num2 = 0;
			Character character = kingdom.court[i];
			if (character == null || character.governed_castle == null)
			{
				continue;
			}
			Realm realm = character.governed_castle.GetRealm();
			if (realm == null || realm.IsDisorder())
			{
				continue;
			}
			float stat = character.GetStat(Stats.cs_influence_per_governed_keep);
			if (stat == 0f)
			{
				continue;
			}
			for (int j = 0; j < realm.settlements.Count; j++)
			{
				Settlement settlement = realm.settlements[j];
				if (settlement.IsActiveSettlement() && settlement.type == "Keep")
				{
					num2++;
				}
			}
			num += (float)num2 * stat;
		}
		return num;
	}

	public static Value CultureFromGovernedMonasteries(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Kingdom kingdom = (var.obj_val as Vars).Get<Kingdom>("obj");
		if (kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			int num2 = 0;
			Character character = kingdom.court[i];
			if (character == null || character.governed_castle == null)
			{
				continue;
			}
			Realm realm = character.governed_castle.GetRealm();
			if (realm == null || realm.IsDisorder())
			{
				continue;
			}
			float stat = character.GetStat(Stats.cs_culture_per_governed_monastery);
			if (stat == 0f)
			{
				continue;
			}
			for (int j = 0; j < realm.settlements.Count; j++)
			{
				Settlement settlement = realm.settlements[j];
				if (settlement.IsActiveSettlement() && settlement.def.piety_type != null && Religion.MatchPietyType(settlement.def.piety_type, kingdom))
				{
					num2++;
				}
			}
			num += (float)num2 * stat;
		}
		return num;
	}

	public static Value GetRelationship(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Kingdom kingdom))
		{
			return Value.Unknown;
		}
		if (!(args[1].Object() is Kingdom k))
		{
			return Value.Unknown;
		}
		return kingdom.GetRelationship(k);
	}

	public static Value ReligionDistance(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		Religion religion = GetReligion(args[0]);
		if (religion == null)
		{
			return Value.Unknown;
		}
		Religion religion2 = GetReligion(args[1]);
		if (religion2 == null)
		{
			return Value.Unknown;
		}
		return religion.DistTo(religion2);
		Religion GetReligion(Value val)
		{
			object obj_val = val.obj_val;
			if (obj_val != null)
			{
				if (obj_val is Religion religion3)
				{
					return religion3;
				}
				if (obj_val is Kingdom kingdom)
				{
					return kingdom.religion;
				}
				if (obj_val is Realm realm)
				{
					return realm.religion;
				}
				if (obj_val is Settlement settlement)
				{
					return settlement.GetRealm()?.religion;
				}
				if (obj_val is Object obj)
				{
					return obj.GetKingdom()?.religion;
				}
				if (obj_val is string text)
				{
					string name = text;
					return ((context as DefsContext)?.game)?.religions?.Get(name);
				}
			}
			return null;
		}
	}

	public static Value CultureDistance(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		Game game = (context as DefsContext)?.game;
		if (game?.cultures == null)
		{
			return Value.Unknown;
		}
		string text = GetCulture(args[0]);
		if (text == null)
		{
			return Value.Unknown;
		}
		string text2 = GetCulture(args[1]);
		if (text2 == null)
		{
			return Value.Unknown;
		}
		return game.cultures.Dist(text, text2);
		string GetCulture(Value val)
		{
			object obj_val = val.obj_val;
			if (obj_val != null)
			{
				if (obj_val is Kingdom kingdom)
				{
					return kingdom.culture;
				}
				if (obj_val is Realm realm)
				{
					return realm.culture;
				}
				if (obj_val is Settlement settlement)
				{
					return settlement.GetRealm()?.culture;
				}
				if (obj_val is Object obj)
				{
					return obj.GetKingdom()?.culture;
				}
				if (obj_val is string text3)
				{
					string culture = text3;
					if (!game.cultures.IsValid(culture))
					{
						return null;
					}
					return culture;
				}
			}
			return null;
		}
	}

	public static Value HaveMarriage(Context context, Value[] args, bool as_value)
	{
		if (args.Length < 2 || args.Length > 4)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Kingdom kingdom))
		{
			return Value.Unknown;
		}
		if (!(args[1].Object() is Kingdom kingdom2))
		{
			return Value.Unknown;
		}
		if (!kingdom.GetRoyalMarriage(kingdom2))
		{
			return false;
		}
		int num = 0;
		if (args.Length > 2)
		{
			num = args[2].Int();
		}
		int num2 = 0;
		if (args.Length > 3)
		{
			num2 = args[3].Int();
		}
		for (int i = 0; i < kingdom.marriages.Count; i++)
		{
			Marriage marriage = kingdom.marriages[i];
			if (marriage == null || marriage.wife == null || marriage.husband == null || (marriage.kingdom_wife != kingdom && marriage.kingdom_wife != kingdom2) || (num == 0 && marriage.kingdom_husband != kingdom && marriage.kingdom_husband != kingdom2) || (num == 1 && marriage.kingdom_husband != kingdom) || (num == 2 && marriage.kingdom_husband != kingdom2))
			{
				continue;
			}
			switch (num2)
			{
			case 0:
				return true;
			case 1:
				if (marriage.husband.IsKing())
				{
					return true;
				}
				break;
			}
			if (num2 == 2 && marriage.husband.IsPrince())
			{
				return true;
			}
		}
		return false;
	}

	public static Value GetLowAvgArmyMorale(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 1)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Kingdom kingdom))
		{
			return Value.Unknown;
		}
		float num = 0f;
		for (int i = 0; i < kingdom.armies.Count; i++)
		{
			Army army = kingdom.armies[i];
			num += army.GetMorale();
		}
		num /= (float)kingdom.armies.Count;
		return num;
	}

	public static Value GetProConThresholdRatio(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 3)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Kingdom our_kingdom))
		{
			return Value.Unknown;
		}
		if (!(args[1].Object() is Kingdom their_kingdom))
		{
			return Value.Unknown;
		}
		string text = args[2].String();
		if (text == null)
		{
			return Value.Unknown;
		}
		ProsAndCons prosAndCons = ProsAndCons.Get(text, our_kingdom, their_kingdom);
		prosAndCons.Calc();
		return prosAndCons.ratio;
	}

	public static Value IsNeighbor(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		Kingdom kingdom = args[0].Object() as Kingdom;
		Realm realm = args[0].Object() as Realm;
		if (kingdom == null && realm == null)
		{
			return Value.Unknown;
		}
		Kingdom kingdom2 = args[1].Object() as Kingdom;
		Realm realm2 = args[1].Object() as Realm;
		if (kingdom2 == null && realm2 == null)
		{
			return Value.Unknown;
		}
		if (kingdom != null)
		{
			if (kingdom2 != null)
			{
				return kingdom.neighbors.Contains(kingdom2);
			}
			return kingdom.externalBorderRealms.Contains(realm2);
		}
		if (kingdom2 != null)
		{
			return kingdom2.externalBorderRealms.Contains(realm);
		}
		return realm.logicNeighborsRestricted.Contains(realm2);
	}

	public static Value HaveTradeAgreement(Context context, Value[] args, bool as_value)
	{
		if (args.Length < 2 || args.Length > 4)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Kingdom kingdom))
		{
			return Value.Unknown;
		}
		if (!(args[1].Object() is Kingdom k))
		{
			return Value.Unknown;
		}
		return kingdom.HasTradeAgreement(k);
	}

	public static Value GetProvincesNumWithReligion(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Kingdom kingdom))
		{
			return Value.Unknown;
		}
		Religion religion = args[1].Object() as Religion;
		if (religion == null)
		{
			string name = args[1].String();
			if (string.IsNullOrEmpty(name))
			{
				return Value.Unknown;
			}
			religion = kingdom.game.religions.Get(name);
			if (religion == null)
			{
				return Value.Unknown;
			}
		}
		int num = 0;
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			if (kingdom.realms[i].religion == religion)
			{
				num++;
			}
		}
		return num;
	}

	public static Value GetNeighborsNumWithReligion(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Kingdom kingdom))
		{
			return Value.Unknown;
		}
		Religion religion = args[1].Object() as Religion;
		if (religion == null)
		{
			string name = args[1].String();
			if (string.IsNullOrEmpty(name))
			{
				return Value.Unknown;
			}
			religion = kingdom.game.religions.Get(name);
			if (religion == null)
			{
				return Value.Unknown;
			}
		}
		int num = 0;
		foreach (Kingdom neighbor in kingdom.neighbors)
		{
			if (neighbor.religion == religion)
			{
				num++;
			}
		}
		return num;
	}

	public static Value HasResource(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Kingdom kingdom))
		{
			return Value.Unknown;
		}
		string tag = args[1].String();
		if (string.IsNullOrEmpty(tag))
		{
			return Value.Unknown;
		}
		return kingdom.GetRealmTag(tag) != 0;
	}

	public static Value HasTradition(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Kingdom kingdom))
		{
			return Value.Unknown;
		}
		string id = args[1].String();
		if (string.IsNullOrEmpty(id))
		{
			return Value.Unknown;
		}
		return kingdom.HasTradition(id);
	}

	public static Value IsInCourt(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		Kingdom kingdom = args[0].Object() as Kingdom;
		Character character = args[1].Object() as Character;
		if (kingdom == null || character == null)
		{
			return Value.Unknown;
		}
		bool flag = kingdom.court.Contains(character);
		if (!flag)
		{
			flag = kingdom.special_court.Contains(character);
		}
		return flag;
	}

	public static Value IsRoyalRelative(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		Kingdom kingdom = args[0].Object() as Kingdom;
		Character character = args[1].Object() as Character;
		if (kingdom == null || character == null)
		{
			return Value.Unknown;
		}
		return kingdom.royalFamily.IsRelative(character);
	}

	public static Value CanDeclareWar(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		Kingdom kingdom = args[0].Object() as Kingdom;
		Kingdom kingdom2 = args[1].Object() as Kingdom;
		if (kingdom == null || kingdom2 == null)
		{
			return Value.Unknown;
		}
		return War.CanStart(kingdom, kingdom2);
	}

	public static Value IsGoodProduced(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		Kingdom kingdom = args[0].Object() as Kingdom;
		string text = args[1].Object() as string;
		if (kingdom == null || text == null)
		{
			return Value.Unknown;
		}
		kingdom.UpdateRealmTags();
		Resource.Def def;
		return kingdom.goods_produced.TryGetValue(text, out def);
	}

	public static Value HasStatus(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Object obj))
		{
			return Value.Unknown;
		}
		string id = args[1].String();
		return obj.FindStatus(obj.game.defs.Get<Status.Def>(id)) != null;
	}

	public static Value AreEnemies(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Object obj))
		{
			return Value.Unknown;
		}
		if (!(args[1].Object() is Object obj2))
		{
			return Value.Unknown;
		}
		return obj.IsEnemy(obj2);
	}

	public static Value AreNeutral(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Object obj))
		{
			return Value.Unknown;
		}
		if (!(args[1].Object() is Object obj2))
		{
			return Value.Unknown;
		}
		return obj.IsNeutral(obj2);
	}

	public static Value AreAllies(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Object obj))
		{
			return Value.Unknown;
		}
		if (!(args[1].Object() is Object obj2))
		{
			return Value.Unknown;
		}
		return obj.IsAlly(obj2);
	}

	public static Value HaveNAP(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Object obj))
		{
			return Value.Unknown;
		}
		if (!(args[1].Object() is Object obj2))
		{
			return Value.Unknown;
		}
		return obj.HasStance(obj2, RelationUtils.Stance.NonAggression);
	}

	public static Value AreAlliesOrOwn(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Object obj))
		{
			return Value.Unknown;
		}
		if (!(args[1].Object() is Object obj2))
		{
			return Value.Unknown;
		}
		return obj.IsAllyOrOwn(obj2);
	}

	public static Value AreOwn(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Object obj))
		{
			return Value.Unknown;
		}
		if (!(args[1].Object() is Object obj2))
		{
			return Value.Unknown;
		}
		return obj.IsOwnStance(obj2);
	}

	public static Value GetWar(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		Kingdom kingdom = (args[0].Object() as Object)?.GetKingdom();
		if (kingdom == null)
		{
			return Value.Unknown;
		}
		Kingdom kingdom2 = (args[1].Object() as Object)?.GetKingdom();
		if (kingdom2 == null)
		{
			return Value.Unknown;
		}
		War war = kingdom.FindWarWith(kingdom2);
		if (war == null)
		{
			return Value.Null;
		}
		return war;
	}

	public static Value IsAttackerInWar(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		Kingdom kingdom = (args[0].Object() as Object)?.GetKingdom();
		if (kingdom == null)
		{
			return Value.Unknown;
		}
		Kingdom kingdom2 = (args[1].Object() as Object)?.GetKingdom();
		if (kingdom2 == null)
		{
			return Value.Unknown;
		}
		War war = kingdom.FindWarWith(kingdom2);
		if (war?.attackers == null)
		{
			return false;
		}
		if (war.attackers.Contains(kingdom))
		{
			return true;
		}
		return false;
	}

	public static Value IsDefenderInWar(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(context is DefsContext defsContext))
		{
			return Value.Unknown;
		}
		if (defsContext.game == null)
		{
			return Value.Unknown;
		}
		Kingdom kingdom = (args[0].Object() as Object)?.GetKingdom();
		if (kingdom == null)
		{
			return Value.Unknown;
		}
		Kingdom kingdom2 = (args[1].Object() as Object)?.GetKingdom();
		if (kingdom2 == null)
		{
			return Value.Unknown;
		}
		War war = kingdom.FindWarWith(kingdom2);
		if (war?.defenders == null)
		{
			return false;
		}
		if (war.defenders.Contains(kingdom))
		{
			return true;
		}
		return false;
	}

	public static Value CombineStats(Context context, Value[] args, bool as_value)
	{
		if (args.Length < 2)
		{
			return Value.Unknown;
		}
		Object obj = args[0].Get<Object>();
		if (obj == null)
		{
			return Value.Unknown;
		}
		DT.Field field = args[1].Get<DT.Field>();
		float num = 0f;
		if (args.Length > 2)
		{
			num = args[2].Float();
		}
		List<DT.Field> list = field?.Children();
		if (list == null)
		{
			if (field == null)
			{
				DefsContext defsContext = context as DefsContext;
				if (defsContext?.field != null)
				{
					Game.Log(defsContext.field.Path(include_file: true) + ": CombineStats: Unresolved field ... try adding '.FIELD' to the 2nd parameter", Game.LogType.Warning);
				}
				else
				{
					Game.Log("CombineStats: Unresolved field", Game.LogType.Warning);
				}
			}
			return num;
		}
		Kingdom kingdom = obj.GetKingdom();
		Stats stats = obj.GetStats();
		Stats stats2 = kingdom?.GetStats();
		float num2 = 0f;
		float num3 = 0f;
		for (int i = 0; i < list.Count; i++)
		{
			DT.Field field2 = list[i];
			string text = field2.Key();
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			Stats stats3 = ((!text.StartsWith("ks_", StringComparison.Ordinal)) ? stats : stats2);
			if (stats3 == null)
			{
				continue;
			}
			Stat stat = stats3.Find(field2.key);
			if (stat?.def == null)
			{
				continue;
			}
			float num4 = stat.CalcValue();
			if (stat.def.HasMultipler())
			{
				Realm r = ((!(obj is Realm realm)) ? ((!(obj is Character character)) ? null : character.governed_castle?.GetRealm()) : realm);
				float num5 = stat.def.CalcMultiplier(r);
				if (num5 == 0f)
				{
					continue;
				}
				if (num5 > 0f)
				{
					num4 *= num5;
				}
			}
			if (field2.FindChild("perc")?.Bool(null, def_val: true) ?? field2.key.EndsWith("_perc", StringComparison.Ordinal))
			{
				num3 += num4;
			}
			else
			{
				num2 += num4;
			}
		}
		return (num + num2) * (1f + num3 * 0.01f);
	}

	public static Value CalcKInfluenceDiplomat(Context context, bool as_vale)
	{
		if (!(Stat.GlobalModifier.cur_stat.owner is Character character) || !character.IsDiplomat())
		{
			return 0f;
		}
		return character.GetClassLevel() + 10;
	}

	public static Value GetMercenaryUnitGoldSum(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 1)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is Mercenary { army: var army } mercenary))
		{
			return Value.Unknown;
		}
		Resource resource = new Resource();
		for (int i = 0; i < army.units.Count; i++)
		{
			Unit unit = army.units[i];
			if (unit != null)
			{
				resource.Add(mercenary.GetUnitCost(unit), 1f);
			}
		}
		return resource[ResourceType.Gold];
	}

	public static Value KeepsLocalAuthority(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = (var.obj_val as Vars).Get<Realm>("obj");
		if (realm == null)
		{
			return 0;
		}
		Kingdom kingdom = realm.GetKingdom();
		if (kingdom == null)
		{
			return 0;
		}
		float stat = kingdom.GetStat(Stats.ks_keeps_local_stability_bonus, must_exist: false);
		int num = 0;
		for (int i = 0; i < realm.settlements.Count; i++)
		{
			if (realm.settlements[i].type == "Keep")
			{
				num++;
			}
		}
		return (float)num * stat;
	}

	public static Value LocalAuthorityFromOwnNeighbors(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = (var.obj_val as Vars).Get<Realm>("obj");
		if (realm == null)
		{
			return 0;
		}
		if (realm.GetKingdom() == null)
		{
			return 0;
		}
		float num = 0f;
		for (int i = 0; i < realm.logicNeighborsAll.Count; i++)
		{
			Realm realm2 = realm.logicNeighborsAll[i];
			if (realm2.logicNeighborsRestricted.Contains(realm) && realm2.kingdom_id == realm.kingdom_id)
			{
				num += realm2.GetStat(Stats.rs_stability_to_own_neighbors);
				if (realm2?.castle?.governor != null)
				{
					num += realm2.GetStat(Stats.rs_governor_local_authority_per_keep_in_realm_and_neighbors) * (float)realm2.GetSettlementCount("Keep", "realm");
					num += realm2.GetStat(Stats.rs_governor_local_authority_in_realm_and_neighbors);
				}
			}
		}
		return num;
	}

	public static Value HappinessFromNeighboringGovernors(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = (var.obj_val as Vars).Get<Realm>("obj");
		if (realm == null)
		{
			return 0;
		}
		float num = 0f;
		for (int i = 0; i < realm.logicNeighborsAll.Count; i++)
		{
			Character character = realm.logicNeighborsAll[i]?.castle?.governor;
			if (character != null && realm.IsOwnStance(character))
			{
				num += character.GetStat(Stats.cs_happiness_boost_neighbors);
			}
		}
		return num;
	}

	public static Value RebelSquadSizePerc(Context context, bool as_vale)
	{
		if (!(Stat.GlobalModifier.cur_stat.owner is Character character) || !character.IsRebel())
		{
			return 0f;
		}
		return (character.GetArmy()?.rebel)?.GetSquadSizePerc() ?? 0f;
	}

	public static Value CalcHappinessSpawnCondition(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Realm>();
		if (realm == null)
		{
			return 0;
		}
		float stat = realm.GetStat(Stats.rs_happiness, must_exist: false);
		return Math.Max((stat < 0f) ? (0f - stat) : 0f, 0f);
	}

	public static Value CalcTaxesSpawnCondition(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		if (var.Get<Realm>() == null)
		{
			return 0;
		}
		return Math.Max(0f, 0f);
	}

	public static Value CalcLoyalistsSpawnCondition(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Realm>();
		if (realm == null)
		{
			return 0;
		}
		realm.GetKingdom();
		return Math.Max(realm.GetRebellionRisk("hunger") / realm.GetTotalRebellionRisk() * 100f, 0f);
	}

	public static Value CalcReligionSpawnCondition(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Realm>();
		if (realm == null)
		{
			return 0;
		}
		return Math.Max(realm.GetRebellionRisk("dead_king") / realm.GetTotalRebellionRisk() * 100f, 0f);
	}

	public static Value CalcWarSupportSpawnCondition(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Realm>();
		if (realm == null)
		{
			return 0;
		}
		KingdomStability kingdomStability = realm.GetKingdom()?.stability;
		if (kingdomStability == null)
		{
			return 0;
		}
		return kingdomStability.GetStability("rebel_leaders");
	}

	public static Value RebellionWarSupportRisk(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Kingdom kingdom = var.Get<Realm>().GetKingdom();
		if (kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		foreach (War war in kingdom.wars)
		{
			_ = war;
			num += 5f;
		}
		num = Math.Max(num, 0f);
		return num;
	}

	public static Value CalcDynasticSpawnCondition(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Realm>();
		if (realm == null)
		{
			return 0;
		}
		return Math.Max(realm.GetStat(Stats.rs_rebellion_risk_dynastic, must_exist: false), 0f);
	}

	public static Value CrownAuthoritySpawnCondition(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Realm>();
		if (realm == null)
		{
			return 0;
		}
		KingdomStability kingdomStability = realm.GetKingdom()?.stability;
		if (kingdomStability == null)
		{
			return 0;
		}
		return Math.Max(kingdomStability.GetStability("crown_authority"), 0f);
	}

	public static Value StabilityFromHunger(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Kingdom kingdom = var.Get<Vars>().Get<Kingdom>("obj");
		if (kingdom == null)
		{
			return 0;
		}
		float sufficentFoodMod = kingdom.GetSufficentFoodMod();
		float num = Game.map_clamp(kingdom.realms.Count, kingdom.sufficient_food_min_provinces, kingdom.sufficient_food_max_provinces, kingdom.min_provinces_hunger_stability_penalty_perc, 100f);
		return (int)Math.Round((float)Math.Floor(Game.map_clamp(rmin: kingdom.stability.def.max_risk_hunger * num / 100f, val: sufficentFoodMod, vmin: kingdom.sufficient_food_min, vmax: kingdom.sufficient_food_max, rmax: 0f)));
	}

	public static Value StabilityFromCrownAuthority(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Kingdom kingdom = var.Get<Vars>().Get<Kingdom>("obj");
		if (kingdom == null)
		{
			return 0;
		}
		CrownAuthority crownAuthority = kingdom.GetCrownAuthority();
		if (crownAuthority == null)
		{
			return 0;
		}
		return crownAuthority.GetStabilityFromCA();
	}

	public static Value StabilityFromRebelLevels(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Kingdom kingdom = var.Get<Vars>().Get<Kingdom>("obj");
		if (kingdom == null)
		{
			return 0;
		}
		KingdomStability.Def def = kingdom.stability.def;
		float num = 0f;
		for (int i = 0; i < kingdom.rebellions.Count; i++)
		{
			Rebellion rebellion = kingdom.rebellions[i];
			for (int j = 0; j < rebellion.rebels.Count; j++)
			{
				Rebel rebel = rebellion.rebels[j];
				Character character = rebel?.army?.leader;
				if (character != null)
				{
					if (rebel.IsLeader())
					{
						int classLevel = character.GetClassLevel();
						num += (float)classLevel * def.risk_global_leader_level;
					}
					else
					{
						num += def.risk_global_rebel;
					}
				}
			}
		}
		return num;
	}

	public static Value StabilityFromReligiousDifferences(Context context, bool as_value)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Kingdom kingdom = var.Get<Vars>().Get<Kingdom>("obj");
		if (kingdom == null)
		{
			return 0;
		}
		float stability_differences_added_provinces = kingdom.stability.def.stability_differences_added_provinces;
		int count = kingdom.realms.Count;
		float num = (float)count + stability_differences_added_provinces;
		Religion religion = kingdom.religion;
		if (religion == null)
		{
			return 0;
		}
		if (kingdom.game.state == Game.State.LoadingMap)
		{
			return 0;
		}
		float num2 = 0f;
		for (int i = 0; i < count; i++)
		{
			Religion religion2 = kingdom.realms[i].religion;
			num2 += (float)religion.DistTo(religion2);
		}
		float stability_differences_min_perc = kingdom.stability.def.stability_differences_min_perc;
		float stability_differences_religion_max = kingdom.stability.def.stability_differences_religion_max;
		if (kingdom.is_pagan)
		{
			float stability_pagan_tolerance_modifier = kingdom.stability.def.stability_pagan_tolerance_modifier;
			num2 *= stability_pagan_tolerance_modifier;
		}
		float val = 100f * num2 / num;
		return -1f * Game.map_clamp(val, stability_differences_min_perc, 100f, 0f, stability_differences_religion_max);
	}

	public static Value StabilityFromCultureDifferences(Context context, bool as_value)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Kingdom kingdom = var.Get<Vars>().Get<Kingdom>("obj");
		if (kingdom == null)
		{
			return 0;
		}
		float stability_differences_added_provinces = kingdom.stability.def.stability_differences_added_provinces;
		int count = kingdom.realms.Count;
		float num = (float)count + stability_differences_added_provinces;
		string culture = kingdom.culture;
		float num2 = 0f;
		for (int i = 0; i < count; i++)
		{
			string culture2 = kingdom.realms[i].pop_majority.kingdom?.culture;
			num2 += (float)kingdom.game.cultures.Dist(culture, culture2);
		}
		float stability_differences_min_perc = kingdom.stability.def.stability_differences_min_perc;
		float stability_differences_culture_max = kingdom.stability.def.stability_differences_culture_max;
		if (kingdom.is_pagan)
		{
			float stability_pagan_tolerance_modifier = kingdom.stability.def.stability_pagan_tolerance_modifier;
			num2 *= stability_pagan_tolerance_modifier;
		}
		float val = 100f * num2 / num;
		return -1f * Game.map_clamp(val, stability_differences_min_perc, 100f, 0f, stability_differences_culture_max);
	}

	public static Value StabilityFromOwnSpies(Context context, bool as_value)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Kingdom kingdom = var.Get<Vars>().Get<Kingdom>("obj");
		if (kingdom == null || kingdom.court == null)
		{
			return 0;
		}
		float num = 0f;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character == null || !character.IsSpy() || character.governed_castle == null)
			{
				continue;
			}
			Realm realm = character.governed_castle.GetRealm();
			for (int j = 0; j < realm.settlements.Count; j++)
			{
				Settlement settlement = realm.settlements[j];
				if (settlement.type == "Keep")
				{
					num += character.GetStat(Stats.cs_spy_governor_stability_per_keep);
				}
				if (settlement.type == "Village")
				{
					num += character.GetStat(Stats.cs_spy_governor_stability_per_village);
				}
			}
		}
		return (int)num;
	}

	public static Value StabilityFromHelpTheWeak(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Kingdom kingdom = var.Get<Vars>().Get<Kingdom>("obj");
		if (kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character != null && character.IsCleric() && character.FindStatus<HelpTheWeakStatus>() != null)
			{
				num += character.GetStat(Stats.cs_help_the_weak_cleric_effect) * (float)character.GetClassLevel() / 3f;
				num += character.GetStat(Stats.cs_help_the_weak_stability);
			}
		}
		return (float)Math.Ceiling(num);
	}

	public static Value RebellionRiskArmyPresence(Context context, bool as_vale)
	{
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Vars>().Get<Realm>("obj");
		if (realm == null || realm.castle == null || realm.castle.garrison == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < realm.castle.garrison.units.Count; i++)
		{
			if (realm.castle.garrison.units[i] != null)
			{
				num += 1 + realm.castle.garrison.units[i].level;
			}
		}
		if (realm.castle.army != null)
		{
			for (int j = 0; j < realm.castle.army.units.Count; j++)
			{
				if (realm.castle.army.units[j] != null)
				{
					num += 1 + realm.castle.army.units[j].level;
				}
			}
		}
		return (int)Math.Round((float)num * realm.rebellionRisk.def.risk_army_presence_multiplier);
	}

	public static Value RebellionRiskReligionTension(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Vars>().Get<Realm>("obj");
		if (realm?.religion == null)
		{
			return 0;
		}
		Kingdom kingdom = realm.GetKingdom();
		if (kingdom == null)
		{
			return 0;
		}
		return -(int)Math.Round(realm.religion.def.ReligiousTension(kingdom.religion) * realm.rebellionRisk.def.risk_religios_differences_multiplier);
	}

	public static Value RebellionRiskCultureTension(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Vars>().Get<Realm>("obj");
		if (realm == null)
		{
			return 0;
		}
		Kingdom kingdom = realm.GetKingdom();
		if (kingdom == null)
		{
			return 0;
		}
		int num = realm.game.cultures.Dist(realm.pop_majority.kingdom?.culture, kingdom.culture);
		RebellionRisk.Def def = realm.rebellionRisk.def;
		float num2 = 0f;
		if (num == 0)
		{
			num2 = 0f;
		}
		if (num == 1)
		{
			num2 = def.risk_same_culture_family;
		}
		if (num == 2)
		{
			num2 = def.risk_different_culture_family;
		}
		num2 += kingdom.GetStat(Stats.ks_nationalism_rebel_risk);
		num2 *= 1f - kingdom.GetStat(Stats.ks_reduce_culture_tension_perc);
		num2 *= def.risk_cultural_differences_multiplier;
		num2 = (int)Math.Round(num2);
		return num2;
	}

	public static Value RebellionRiskRebelOccupations(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Vars>().Get<Realm>("obj");
		if (realm == null)
		{
			return 0;
		}
		Kingdom kingdom = realm.GetKingdom();
		if (kingdom == null)
		{
			return 0;
		}
		RebellionRisk.Def def = realm.rebellionRisk.def;
		if (realm.IsOccupied() && realm.controller is Rebellion)
		{
			return def.risk_occupied_by_rebels;
		}
		float num = 0f;
		for (int i = 0; i < realm.logicNeighborsRestricted.Count; i++)
		{
			Realm realm2 = realm.logicNeighborsRestricted[i];
			if (realm2.IsOccupied() && realm2.controller is Rebellion rebellion)
			{
				num = Math.Min(num, (rebellion.loyal_to == kingdom.id) ? def.risk_nearby_occupied_by_our_loyalists : def.risk_nearby_occupied_by_rebels);
			}
		}
		return num;
	}

	public static Value RebellionRiskRebelArmies(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Vars>().Get<Realm>("obj");
		if (realm == null)
		{
			return 0;
		}
		realm.GetKingdom();
		RebellionRisk.Def def = realm.rebellionRisk.def;
		float num = realm.rebellionRisk.risk_from_rebel_armies(realm);
		if (realm.rebellions.Count > 0)
		{
			num += def.risk_rebellion_zone;
		}
		for (int i = 0; i < realm.logicNeighborsRestricted.Count; i++)
		{
			num += realm.rebellionRisk.risk_from_rebel_armies(realm.logicNeighborsRestricted[i]) * def.risk_rebel_armies_in_neigbors_multiplier;
		}
		return num;
	}

	public static Value RebellionRiskDisloyalPop(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Vars>().Get<Realm>("obj");
		if (realm == null)
		{
			return 0;
		}
		Kingdom kingdom = realm.GetKingdom();
		RebellionRisk.Def def = realm.rebellionRisk.def;
		Kingdom kingdom2 = realm.pop_majority.kingdom;
		float strength = realm.pop_majority.strength;
		if (kingdom2 == kingdom)
		{
			return 0f;
		}
		return Game.map_clamp(strength, 50f, 100f, 0f, def.risk_disloyal_population);
	}

	public static Value RebellionRiskSacked(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		Realm realm = var.Get<Vars>().Get<Realm>("obj");
		if (realm?.castle == null)
		{
			return 0;
		}
		RebellionRisk.Def def = realm.rebellionRisk.def;
		float num = 0f;
		if (realm.castle.sacked && !realm.castle.quick_recovery)
		{
			num += def.risk_sacked;
		}
		return num;
	}

	public static Value RelOfferGoldPerm(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Offer offer = context.GetVar(null, "obj").Get<Offer>();
		if (offer == null)
		{
			return 0;
		}
		Value var = context.GetVar(null, "field");
		if (var.is_null)
		{
			return 0;
		}
		DT.Field field = var.Get<DT.Field>();
		if (field == null)
		{
			return 0;
		}
		Game game = offer.game;
		Kingdom kingdom = offer.GetSourceObj() as Kingdom;
		Kingdom kingdom2 = offer.GetTargetObj() as Kingdom;
		Value arg = offer.GetArg(0);
		float trasuryRatioWith = kingdom.GetTrasuryRatioWith(kingdom2);
		DT.Field field2 = field.parent?.FindChild("donate_gold_rel_amount");
		DT.Field field3 = field.parent?.FindChild("donate_gold_perm_perc");
		DT.Field field4 = field.parent?.FindChild("donate_gold_min_treasury_mul");
		DT.Field field5 = field.parent?.FindChild("donate_gold_max_treasury_mul");
		DT.Field field6 = field.parent?.FindChild("donate_gold_to_Papacy_perm_mul");
		if (field2 == null || field3 == null || field4 == null || field5 == null || field6 == null)
		{
			return 0;
		}
		float num = field2.Value((int)arg - 1).Float();
		float num2 = field3.Value((int)arg - 1).Float();
		Value value = field4.Value(0);
		float num3 = game.Map(rmax: field5.Value(0), v: trasuryRatioWith, vmin: 0f, vmax: 1f, rmin: value);
		float num4 = num * num2 * num3;
		if (kingdom2.IsPapacy())
		{
			num4 *= (float)field6.Value(0);
		}
		return num4;
	}

	public static Value RelOfferGoldTemp(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Offer offer = context.GetVar(null, "obj").Get<Offer>();
		if (offer == null)
		{
			return 0;
		}
		Value var = context.GetVar(null, "field");
		if (var.is_null)
		{
			return 0;
		}
		DT.Field field = var.Get<DT.Field>();
		if (field == null)
		{
			return 0;
		}
		Game game = offer.game;
		Kingdom kingdom = offer.GetSourceObj() as Kingdom;
		Kingdom kingdom2 = offer.GetTargetObj() as Kingdom;
		Value arg = offer.GetArg(0);
		float trasuryRatioWith = kingdom.GetTrasuryRatioWith(kingdom2);
		DT.Field field2 = field.parent?.FindChild("donate_gold_rel_amount");
		DT.Field field3 = field.parent?.FindChild("donate_gold_perm_perc");
		DT.Field field4 = field.parent?.FindChild("donate_gold_min_treasury_mul");
		DT.Field field5 = field.parent?.FindChild("donate_gold_max_treasury_mul");
		DT.Field field6 = field.parent?.FindChild("donate_gold_to_Papacy_temp_mul");
		if (field2 == null || field3 == null || field4 == null || field5 == null || field6 == null)
		{
			return 0;
		}
		float num = field2.Value((int)arg - 1).Float();
		float num2 = field3.Value((int)arg - 1).Float();
		Value value = field4.Value(0);
		float num3 = game.Map(rmax: field5.Value(0), v: trasuryRatioWith, vmin: 0f, vmax: 1f, rmin: value);
		float num4 = num * num3 * (1f - num2);
		if (kingdom2.IsPapacy())
		{
			num4 *= (float)field6.Value(0);
		}
		return num4;
	}

	public static Value TradeExpeditionProfit(Context context, bool as_vale)
	{
		if (!(context is DefsContext))
		{
			return Value.Unknown;
		}
		Value var = context.GetVar(null, "context_vars");
		if (var.is_null)
		{
			return 0;
		}
		TradeExpeditionStatus tradeExpeditionStatus = (var.obj_val as Vars)?.Get<Character>("obj")?.FindStatus<TradeExpeditionStatus>();
		if (tradeExpeditionStatus == null)
		{
			return 0;
		}
		Value var2 = context.GetVar(null, "field");
		if (var2.is_null)
		{
			return 0;
		}
		DT.Field field = var2.Get<DT.Field>();
		if (field == null)
		{
			return 0;
		}
		Character own_character = tradeExpeditionStatus.own_character;
		Kingdom own_kingdom = tradeExpeditionStatus.own_kingdom;
		float num = field.GetFloat("base");
		float num2 = field.GetFloat("class_level_mod");
		float num3 = field.GetFloat("gold_colonies_per_commerce_mod");
		float maxCommerce = own_kingdom.GetMaxCommerce();
		float num4 = 1f + own_kingdom.GetStat(Stats.ks_profit_from_colonies_perc) / 100f;
		return (num + (float)own_character.GetClassLevel() * num2 + maxCommerce * num3) * num4;
	}

	public static Value GetEnemyLeader(Context context, Value[] args, bool as_value)
	{
		if (args.Length != 2)
		{
			return Value.Unknown;
		}
		if (!(args[0].Object() is War war))
		{
			return Value.Unknown;
		}
		if (!(args[1].Object() is Kingdom k))
		{
			return Value.Unknown;
		}
		return war.GetEnemyLeader(k);
	}
}
