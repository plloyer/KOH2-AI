using System.Collections.Generic;

namespace Logic;

public class CloseTradeRouteAction : Action
{
	public enum Type
	{
		Manual,
		Auto
	}

	public Type type;

	private float modifier;

	public CloseTradeRouteAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new CloseTradeRouteAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		return "ok";
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "success")
		{
			modifier = KingdomAndKingdomRelation.GetValueOfModifier(base.game, (type == Type.Manual) ? "rel_break_trade_manual" : "rel_break_trade");
			return true;
		}
		return base.ApplyEarlyOutcome(outcome);
	}

	public override void Run()
	{
		own_kingdom.CloseTradeRoute(base.target as Kingdom, isManual: true);
	}

	public override List<int> GetSendToKingdoms()
	{
		return new List<int>(2)
		{
			(base.target as Kingdom)?.id ?? 0,
			(base.owner as Kingdom)?.id ?? 0
		};
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "rel_change_amount"))
		{
			if (key == "type")
			{
				return type.ToString();
			}
			return base.GetVar(key, vars, as_value);
		}
		return modifier;
	}
}
