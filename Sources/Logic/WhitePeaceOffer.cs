namespace Logic;

public class WhitePeaceOffer : Offer
{
	public WhitePeaceOffer(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public WhitePeaceOffer(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new WhitePeaceOffer(def, from, to);
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
		if (!(from as Kingdom).IsEnemy(to as Kingdom))
		{
			return "not_in_war";
		}
		if (!War.CanStop(from as Kingdom, to as Kingdom))
		{
			return "_cant_stop_war";
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
		if (!War.CanStop(from as Kingdom, to as Kingdom))
		{
			text = "_cant_stop_war";
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom.FindWarWith(kingdom2) == null)
		{
			base.game.Log("Signing peace without having active war object: " + kingdom.Name + " --> " + kingdom.GetWarStance(kingdom2).ToString() + " <-- " + kingdom2.Name);
		}
		kingdom.EndWarWith(kingdom2, null, "white_peace");
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(PeaceOfferTribute)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(WhitePeaceOffer)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(PeaceDemandTribute)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}
}
