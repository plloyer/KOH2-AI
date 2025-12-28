namespace Logic;

public class SignExclusiveTrade : Offer
{
	public SignExclusiveTrade(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public SignExclusiveTrade(Kingdom from, Kingdom to, params Value[] args)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new SignExclusiveTrade(def, from, to);
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
		if (kingdom.IsEnemy(kingdom2))
		{
			return "in_war";
		}
		if (!kingdom.HasTradeAgreement(kingdom2))
		{
			return "already_trading";
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
		if (kingdom.IsEnemy(kingdom2))
		{
			return "in_war";
		}
		if (!kingdom.HasTradeAgreement(kingdom2))
		{
			return "already_trading";
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		GetSourceObj();
		GetTargetObj();
	}
}
