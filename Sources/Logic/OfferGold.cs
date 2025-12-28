namespace Logic;

public class OfferGold : DemandGold
{
	public OfferGold(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferGold(Kingdom from, Kingdom to, int amount)
		: base(from, to, amount)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferGold(def, from, to);
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
