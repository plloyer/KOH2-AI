namespace Logic;

public class SignTrade : Offer
{
	public SignTrade(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public SignTrade(Kingdom from, Kingdom to, params Value[] args)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new SignTrade(def, from, to);
	}

	public override bool HasValidParent()
	{
		if (!base.HasValidParent())
		{
			return false;
		}
		if (CountSimilarOffersInParent() > 0)
		{
			return false;
		}
		return true;
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (parent == null && kingdom.IsEnemy(kingdom2))
		{
			return "in_war";
		}
		if (kingdom.HasTradeAgreement(kingdom2))
		{
			return "already_trading";
		}
		float tradeLevelFloat = base.game.economy.GetTradeLevelFloat("commerce", kingdom, kingdom2, null, 0);
		if (kingdom.GetAvailableCommerce() < tradeLevelFloat)
		{
			return "_insufficient_commerse_proposer";
		}
		if (kingdom2.GetAvailableCommerce() < tradeLevelFloat)
		{
			return "_insufficient_commerse_other";
		}
		return text;
	}

	public override string Validate()
	{
		string text = base.Validate();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (parent == null && kingdom.IsEnemy(kingdom2))
		{
			return "in_war";
		}
		if (kingdom.HasTradeAgreement(kingdom2))
		{
			return "already_trading";
		}
		float tradeLevelFloat = base.game.economy.GetTradeLevelFloat("commerce", kingdom, kingdom2, null, 0);
		if (kingdom.GetAvailableCommerce() < tradeLevelFloat)
		{
			return "_insufficient_commerce_proposer";
		}
		if (kingdom2.GetAvailableCommerce() < tradeLevelFloat)
		{
			return "_insufficient_commerce_other";
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		kingdom.SetStance(kingdom2, RelationUtils.Stance.Trade);
		Vars vars = new Vars();
		vars.SetVar("kingdom_a", kingdom);
		vars.SetVar("kingdom_b", kingdom2);
		kingdom.game.BroadcastRadioEvent("TradeSignedMessage", vars);
	}
}
