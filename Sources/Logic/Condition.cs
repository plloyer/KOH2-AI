using System.Collections.Generic;

namespace Logic;

public class Condition : BaseObject, IVars
{
	public DT.Field def;

	public string def_value;

	public List<Condition> children;

	public static Condition Load(DT.Field def)
	{
		if (def == null)
		{
			return null;
		}
		return new Condition
		{
			def = def
		};
	}

	public Value GetValue(IVars vars)
	{
		return def.Value(vars);
	}

	public Value GetAnd(IVars vars)
	{
		return Value.Unknown;
	}

	public Value GetOr(IVars vars)
	{
		return Value.Unknown;
	}

	public Value GetSum(IVars vars)
	{
		return Value.Unknown;
	}

	public Value GetMul(IVars vars)
	{
		return Value.Unknown;
	}

	public Value GetMin(IVars vars)
	{
		return Value.Unknown;
	}

	public Value GetMax(IVars vars)
	{
		return Value.Unknown;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return Value.Unknown;
	}

	public override string ToString()
	{
		return "condition: " + def.ToString();
	}
}
