namespace Logic;

public class OfferJoinInOffensivePact : DemandJoinInOffensivePact
{
	public OfferJoinInOffensivePact(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferJoinInOffensivePact(Kingdom from, Kingdom to, Pact pact)
		: base(from, to, pact)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferJoinInOffensivePact(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return from;
	}

	public override Object GetTargetObj()
	{
		return to;
	}

	public override bool IsWar(bool sender)
	{
		return sender;
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		if (vars.Get("diplomat_induced") != 1)
		{
			return "not_diplomat_induced";
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
		if (vars.Get("diplomat_induced") != 1)
		{
			return "not_diplomat_induced";
		}
		return text;
	}
}
