namespace Logic;

public class DemandChangeReligion : OfferChangeReligion
{
	public DemandChangeReligion(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandChangeReligion(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandChangeReligion(def, from, to);
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
