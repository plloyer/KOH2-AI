namespace Logic;

public class BreakTrade : Offer
{
	public BreakTrade(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public BreakTrade(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new BreakTrade(def, from, to);
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
		if (!ShouldSkip(text))
		{
			Kingdom obj = GetSourceObj() as Kingdom;
			Kingdom item = GetTargetObj() as Kingdom;
			if (!obj.tradeAgreementsWith.Contains(item))
			{
				return "_not_trading";
			}
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
		if (!ShouldSkip(text))
		{
			Kingdom obj = GetSourceObj() as Kingdom;
			Kingdom item = GetTargetObj() as Kingdom;
			if (!obj.tradeAgreementsWith.Contains(item))
			{
				text = "_not_trading";
			}
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom obj = GetSourceObj() as Kingdom;
		Kingdom k = GetTargetObj() as Kingdom;
		obj.UnsetStance(k, RelationUtils.Stance.Trade);
	}
}
