namespace Logic;

public class DemandAbandonCaliphate : OfferAbandonCaliphate
{
	public DemandAbandonCaliphate(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandAbandonCaliphate(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandAbandonCaliphate(def, from, to);
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
