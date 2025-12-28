namespace Logic;

public class EscapedStatus : Status
{
	public EscapedStatus(Def def = null)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new EscapedStatus(def);
	}

	public override bool IsDead()
	{
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "reason")
		{
			return "escaped";
		}
		return base.GetVar(key, vars, as_value);
	}
}
