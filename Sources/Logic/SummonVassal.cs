namespace Logic;

public class SummonVassal : DemandAttackKingdom
{
	public SummonVassal(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public SummonVassal(Kingdom from, Kingdom to, Kingdom kingdom)
		: base(from, to, kingdom)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new SummonVassal(def, from, to);
	}

	public override bool HasValidParent()
	{
		if (!base.HasValidParent())
		{
			return false;
		}
		if (CountSimilarOffersInParent() > 0)
		{
			return false;
		}
		return true;
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = from as Kingdom;
		if ((to as Kingdom).sovereignState != kingdom)
		{
			return "not_vassal";
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
		Kingdom kingdom = from as Kingdom;
		if ((to as Kingdom).sovereignState != kingdom)
		{
			return "not_vassal";
		}
		return text;
	}

	public override void OnAccept()
	{
		Kingdom provoker = GetTargetObj() as Kingdom;
		Kingdom obj = GetSourceObj() as Kingdom;
		Kingdom arg = GetArg<Kingdom>(0);
		obj.StartWarWith(arg, War.InvolvementReason.VassalSummoned, "SummonVassalMessage", provoker);
	}
}
