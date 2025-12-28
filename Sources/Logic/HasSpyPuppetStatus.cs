namespace Logic;

public class HasSpyPuppetStatus : HasPuppetStatus
{
	public HasSpyPuppetStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new HasSpyPuppetStatus(def);
	}

	public HasSpyPuppetStatus(Character puppet)
		: base(puppet)
	{
	}
}
