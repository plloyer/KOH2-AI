namespace Logic;

public class ExpandTradeAction : MerchantOpportunity
{
	public ExpandTradeAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ExpandTradeAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.trade_level >= character.GetMaxTradeLevel())
		{
			return "at_max_level";
		}
		return base.Validate(quick_out);
	}

	public override void Prepare()
	{
		base.Prepare();
		own_kingdom?.InvalidateIncomes();
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			character.IncreaseTradeLevel();
			base.Run();
		}
	}

	private float CalcCommerceCost()
	{
		float num = base.game.economy.CalcCommerceForTrader(base.own_character);
		return base.game.economy.CalcCommerceForTrader(base.own_character, null, base.own_character.trade_level + 1) - num;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "name":
			return base.GetVar(key, vars, as_value);
		case "next_level":
			return $"Economy.trade.level_{base.own_character.trade_level + 1}.name";
		case "commerce_cost":
			return CalcCommerceCost();
		default:
		{
			Value tradeLevelValue = base.game.economy.GetTradeLevelValue(key, own_kingdom, base.target as Kingdom, base.own_character, base.own_character.trade_level + 1);
			if (!tradeLevelValue.is_unknown)
			{
				return tradeLevelValue;
			}
			return base.GetVar(key, vars, as_value);
		}
		}
	}
}
