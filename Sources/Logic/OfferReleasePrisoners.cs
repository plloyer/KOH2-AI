namespace Logic;

public class OfferReleasePrisoners : DemandReleasePrisoners
{
	public OfferReleasePrisoners(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferReleasePrisoners(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferReleasePrisoners(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return from;
	}

	public override Object GetTargetObj()
	{
		return to;
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
		return text;
	}
}
