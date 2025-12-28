namespace Logic;

public class OngoingActionStatus : Status
{
	public OngoingActionStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new OngoingActionStatus(def);
	}

	public override bool IsAutomatic()
	{
		return true;
	}

	public override bool IsDead()
	{
		return GetAction()?.IsDead() ?? false;
	}

	public override void GetProgress(out float cur, out float max)
	{
		Action action = GetAction();
		if (action == null)
		{
			cur = (max = 0f);
		}
		else
		{
			action.GetProgress(out cur, out max);
		}
	}

	public Action GetAction()
	{
		if (owner == null)
		{
			return null;
		}
		return owner.GetComponent<Actions>()?.current;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Action action = GetAction();
		if (key == "action")
		{
			return action;
		}
		if (action != null)
		{
			Value var = action.GetVar(key, vars, as_value);
			if (!var.is_unknown)
			{
				return var;
			}
		}
		return base.GetVar(key, vars, as_value);
	}
}
