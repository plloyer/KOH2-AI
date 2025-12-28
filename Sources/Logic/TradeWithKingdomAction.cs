namespace Logic;

public class TradeWithKingdomAction : Action
{
	public TradeWithKingdomAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new TradeWithKingdomAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character?.GetArmy() != null)
		{
			return "leading_army";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!base.ValidateTarget(target))
		{
			return false;
		}
		if (!(target is Kingdom kingdom))
		{
			return false;
		}
		Kingdom kingdom2 = base.own_character.GetKingdom();
		if (kingdom.GetMerchantFrom(kingdom2) != null)
		{
			return false;
		}
		if (kingdom2.GetMerchantGoingTo(kingdom) != null)
		{
			return false;
		}
		return true;
	}

	public override void Prepare()
	{
		base.Prepare();
		OnMissionChangedAnalytics();
		own_kingdom?.InvalidateIncomes();
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			character.SetMissionKingdom(base.target as Kingdom);
			character.SetTradeLevel(character.GetMinTradeLevelOnMission());
			character.SetDefaultStatus<TradingWithKingdomStatus>();
			own_kingdom?.InvalidateIncomes();
			base.Run();
		}
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "name"))
		{
			if (key == "owner")
			{
				return base.owner;
			}
			Object obj = GetTarget(vars);
			Value tradeLevelValue = base.game.economy.GetTradeLevelValue(key, own_kingdom, obj as Kingdom, base.own_character, 1);
			if (!tradeLevelValue.is_unknown)
			{
				return tradeLevelValue;
			}
			tradeLevelValue = base.GetVar(key, vars, as_value);
			if (!tradeLevelValue.is_unknown)
			{
				return tradeLevelValue;
			}
			return Value.Unknown;
		}
		return base.GetVar(key, vars, as_value);
	}
}
