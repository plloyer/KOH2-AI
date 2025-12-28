namespace Logic;

public class OfferRansomPrisoner : AskForPrisonerRansom
{
	public OfferRansomPrisoner(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferRansomPrisoner(Kingdom from, Kingdom to, Character target, int ransom_default = 0)
		: base(from, to, target, ransom_default)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferRansomPrisoner(def, from, to);
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
