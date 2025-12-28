namespace Logic;

public class OfferChangeReligion : Offer
{
	public OfferChangeReligion(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferChangeReligion(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferChangeReligion(def, from, to);
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

	protected virtual string ValidateKingdomReligions()
	{
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (!kingdom.IsEnemy(kingdom2))
		{
			return "not_in_war";
		}
		if (!(kingdom.religion is Pagan))
		{
			return "_old_religion_not_pagan";
		}
		if (kingdom2.religion is Pagan)
		{
			return "_new_religion_is_pagan";
		}
		return "ok";
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		string text2 = ValidateKingdomReligions();
		if (ShouldReturn(text2))
		{
			return text2;
		}
		if (text2 != "ok")
		{
			text = text2;
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
		string text2 = ValidateKingdomReligions();
		if (ShouldReturn(text2))
		{
			return text2;
		}
		if (text2 != "ok")
		{
			text = text2;
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		kingdom.SetReligion(kingdom2.religion);
		Vars vars = new Vars();
		vars.SetVar("kingdom_a", kingdom);
		vars.SetVar("new_religion", kingdom2.religion.name);
		base.game.BroadcastRadioEvent("KingdomChangedReligionMessage", vars);
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(OfferChangeReligion)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(DemandChangeReligion)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "religion")
		{
			return (GetTargetObj() as Kingdom).religion;
		}
		return base.GetVar(key, vars, as_value);
	}
}
