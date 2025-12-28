namespace Logic;

public class NoPaganBeliefStatus : Status
{
	public NoPaganBeliefStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new NoPaganBeliefStatus(def);
	}

	public override bool IsAutomatic()
	{
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "cost":
			return base.own_character.actions.Find("PromotePaganBeliefAction").GetCost();
		case "current_upkeep":
		{
			float num = base.own_character.game.religions.pagan.def.CalcPaganBliefsUpkeep(base.own_kingdom);
			if (num != 0f)
			{
				return new Value(num);
			}
			return Value.Null;
		}
		case "next_upkeep":
			return base.own_character.game.religions.pagan.def.CalcPaganBliefsUpkeep(base.own_kingdom, base.own_kingdom.pagan_beliefs.Count + 1);
		case "can_promote":
			return base.own_character.actions.Find("PromotePaganBeliefAction").Validate() == "ok";
		case "too_many_beliefs":
			return base.own_character.actions.Find("PromotePaganBeliefAction").Validate() == "_too_many_beliefs";
		case "belief":
		{
			Action action = base.own_character.actions.Find("PromotePaganBeliefAction");
			if (action == null)
			{
				return Value.Null;
			}
			return new Value(string.Concat("Pagan.", action.GetArg(0, null), ".name"));
		}
		case "is_preparing_to_promote":
			return base.own_character.actions.Find("PromotePaganBeliefAction").state == Action.State.Preparing;
		default:
			return base.GetVar(key, vars, as_value);
		}
	}

	public override void GetProgress(out float cur, out float max)
	{
		Action action = base.own_character.actions.Find("PromotePaganBeliefAction");
		if (action != null && action.is_active)
		{
			action.GetProgress(out cur, out max);
		}
		else
		{
			cur = (max = 0f);
		}
	}
}
