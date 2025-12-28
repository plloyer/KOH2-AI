namespace Logic;

public class CancelSpyPlotAction : Action
{
	public CancelSpyPlotAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new CancelSpyPlotAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (!(character.cur_action is SpyPlot))
		{
			return "no_active_plot";
		}
		return "ok";
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (state != State.Inactive)
		{
			return base.GetVar(key, vars, as_value);
		}
		Action cur_action = base.own_character.cur_action;
		if (cur_action != null)
		{
			if (key == "action")
			{
				return cur_action;
			}
			Value var = cur_action.GetVar(key, vars, as_value);
			if (!var.is_unknown)
			{
				return var;
			}
		}
		return base.GetVar(key, vars, as_value);
	}
}
