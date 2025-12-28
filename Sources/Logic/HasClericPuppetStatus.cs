namespace Logic;

public class HasClericPuppetStatus : HasPuppetStatus
{
	public HasClericPuppetStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new HasClericPuppetStatus(def);
	}

	public HasClericPuppetStatus(Character puppet)
		: base(puppet)
	{
	}
}
