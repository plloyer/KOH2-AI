namespace Logic;

public class AcceptRansomAction : PrisonAction
{
	public AcceptRansomAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AcceptRansomAction(owner as Kingdom, def);
	}

	public bool ValidateOffer(Offer offer, Object target)
	{
		if (offer == null)
		{
			return false;
		}
		if (!(offer is OfferRansomPrisoner))
		{
			return false;
		}
		if (offer.args == null || offer.args.Count < 1 || offer.args[0].obj_val != target)
		{
			return false;
		}
		return true;
	}

	private Offer GetOffer(Object target)
	{
		Offer offer = target?.GetKingdom().GetOutgoingOfferTo(own_kingdom);
		if (ValidateOffer(offer, target))
		{
			return offer;
		}
		if (offer?.args != null && offer.args.Count >= 1 && ValidateOffer(offer.args[0].obj_val as Offer, target))
		{
			return offer.args[0].obj_val as Offer;
		}
		return null;
	}

	public override bool ValidateTarget(Object target)
	{
		if (GetOffer(target) == null)
		{
			return false;
		}
		return base.ValidateTarget(target);
	}

	public override void Run()
	{
		base.target.GetKingdom().GetOutgoingOfferTo(own_kingdom)?.Answer("accept");
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "offer")
		{
			return GetOffer(base.target);
		}
		return base.GetVar(key, vars, as_value);
	}
}
