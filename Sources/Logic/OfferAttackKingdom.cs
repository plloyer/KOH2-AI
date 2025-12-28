namespace Logic;

public class OfferAttackKingdom : DemandAttackKingdom
{
	public OfferAttackKingdom(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferAttackKingdom(Kingdom from, Kingdom to, Kingdom kingdom)
		: base(from, to, kingdom)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferAttackKingdom(def, from, to);
	}

	public override string ValidateWithoutArgs()
	{
		if (!(from as Kingdom).IsEnemy(to as Kingdom))
		{
			return "in_war";
		}
		return base.ValidateWithoutArgs();
	}

	public override string Validate()
	{
		if (!(from as Kingdom).IsEnemy(to as Kingdom))
		{
			return "in_war";
		}
		return base.Validate();
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
