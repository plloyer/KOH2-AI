namespace Logic;

public class DemandJoinInDefensivePact : DemandJoinInOffensivePact
{
	public DemandJoinInDefensivePact(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandJoinInDefensivePact(Kingdom from, Kingdom to, Pact pact)
		: base(from, to, pact)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandJoinInDefensivePact(def, from, to);
	}

	protected override void InitType()
	{
		type = Pact.Type.Defensive;
	}
}
