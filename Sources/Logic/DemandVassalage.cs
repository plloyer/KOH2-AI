namespace Logic;

public class DemandVassalage : OfferVassalage
{
	public DemandVassalage(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandVassalage(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandVassalage(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
	}
}
