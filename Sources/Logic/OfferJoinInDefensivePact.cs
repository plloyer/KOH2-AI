namespace Logic;

public class OfferJoinInDefensivePact : DemandJoinInDefensivePact
{
	public OfferJoinInDefensivePact(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferJoinInDefensivePact(Kingdom from, Kingdom to, Pact pact)
		: base(from, to, pact)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferJoinInDefensivePact(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return from;
	}

	public override Object GetTargetObj()
	{
		return to;
	}
}
