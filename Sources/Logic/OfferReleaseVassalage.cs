namespace Logic;

public class OfferReleaseVassalage : DemandReleaseVassalage
{
	public OfferReleaseVassalage(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferReleaseVassalage(Kingdom from, Kingdom to, Kingdom vassal)
		: base(from, to, vassal)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferReleaseVassalage(def, from, to);
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
		return "false";
	}

	public override string Validate()
	{
		return "false";
	}
}
