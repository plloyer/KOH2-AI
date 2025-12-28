namespace Logic;

public class DemandHelpWithRebels : OfferHelpWithRebels
{
	public DemandHelpWithRebels(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandHelpWithRebels(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandHelpWithRebels(def, from, to);
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
