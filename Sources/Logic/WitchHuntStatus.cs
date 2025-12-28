namespace Logic;

public class WitchHuntStatus : Status
{
	private Action a;

	public WitchHuntStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new WitchHuntStatus(def);
	}

	public override bool IsAutomatic()
	{
		return true;
	}

	private Action GetAction()
	{
		if (a == null || a.own_character != base.own_character)
		{
			a = base.own_character?.actions?.Find("WitchHuntAction");
		}
		return a;
	}

	public override void GetProgress(out float cur, out float max)
	{
		Action action = GetAction();
		if (action != null && action.is_active)
		{
			action.GetProgress(out cur, out max);
		}
		else
		{
			cur = (max = 0f);
		}
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return GetAction()?.GetVar(key, vars, as_value) ?? base.GetVar(key, vars, as_value);
	}
}
