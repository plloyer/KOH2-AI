namespace Logic;

public class BreakRoyalTies : Offer
{
	public BreakRoyalTies(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public BreakRoyalTies(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new BreakRoyalTies(def, from, to);
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		if (!(from as Kingdom).GetRoyalMarriage(to as Kingdom))
		{
			return "not_married";
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
		if (!(from as Kingdom).GetRoyalMarriage(to as Kingdom))
		{
			return "not_married";
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom obj = GetSourceObj() as Kingdom;
		Kingdom k = GetTargetObj() as Kingdom;
		obj.UnsetStance(k, RelationUtils.Stance.Marriage);
	}
}
