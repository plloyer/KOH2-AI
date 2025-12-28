using System;

namespace Logic;

public class EstablishTradeRouteAction : Action
{
	public EstablishTradeRouteAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new EstablishTradeRouteAction(owner as Character, def);
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			character.GetKingdom().CreateTradeRoute(base.target as Kingdom);
			base.Run();
		}
	}

	public override void FillPossibleTargetVars(Vars vars)
	{
		Resource resource = GetCost(base.target);
		if (resource == null)
		{
			resource = new Resource();
		}
		resource[ResourceType.Gold] = (int)Math.Round(resource[ResourceType.Gold]);
		vars.Set("cost", resource);
		vars.Set("profit", (float)Math.Round(base.own_character.GetKingdom().GetTradeRouteProfit(base.target as Kingdom)));
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Kingdom kingdom = base.own_character.GetKingdom();
		Object obj = base.target;
		if (obj == null && vars != null && vars != this && key != "target")
		{
			obj = vars.GetVar("target").obj_val as Kingdom;
		}
		switch (key)
		{
		case "distance":
			if (obj == null)
			{
				return Value.Unknown;
			}
			return own_kingdom.DistanceToKingdom(obj as Kingdom);
		case "square_distance":
			if (obj == null)
			{
				return Value.Unknown;
			}
			return (float)Math.Sqrt(own_kingdom.DistanceToKingdom(obj as Kingdom));
		case "is_exclusive":
			if (obj == null)
			{
				return Value.Unknown;
			}
			return false;
		case "have_marriage":
			if (obj == null)
			{
				return Value.Unknown;
			}
			return kingdom.GetRoyalMarriage(obj as Kingdom);
		case "traderoute_capacity":
			if (obj == null)
			{
				return Value.Unknown;
			}
			return kingdom.GetTradeRouteCommerseCapacityDrain(obj as Kingdom);
		case "relationship":
			if (obj == null)
			{
				return Value.Unknown;
			}
			return own_kingdom.GetRelationship(obj as Kingdom);
		case "knight_trade_modifiers":
			return 1;
		case "merchant_class_level":
			return base.own_character.GetClassLevel();
		default:
			return base.GetVar(key, vars, as_value);
		}
	}
}
