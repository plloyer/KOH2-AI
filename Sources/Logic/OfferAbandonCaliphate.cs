namespace Logic;

public class OfferAbandonCaliphate : Offer
{
	public OfferAbandonCaliphate(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferAbandonCaliphate(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferAbandonCaliphate(def, from, to);
	}

	public override bool HasValidParent()
	{
		if (!base.HasValidParent())
		{
			return false;
		}
		if (parent == null && (from as Kingdom).IsEnemy(to as Kingdom))
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
		if (!(from as Kingdom).IsEnemy(to as Kingdom))
		{
			return "not_in_war";
		}
		if (!(GetSourceObj() as Kingdom).IsCaliphate())
		{
			return "_not_a_caliphate";
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
		if (!(from as Kingdom).IsEnemy(to as Kingdom))
		{
			return "not_in_war";
		}
		if (!(GetSourceObj() as Kingdom).IsCaliphate())
		{
			return "_not_a_caliphate";
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetSourceObj() as Kingdom;
		GetTargetObj();
		kingdom.actions.Find("AbandonCaliphateAction")?.Execute(kingdom);
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(OfferAbandonCaliphate)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(DemandAbandonCaliphate)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}
}
