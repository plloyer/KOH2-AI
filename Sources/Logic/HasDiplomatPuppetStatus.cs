namespace Logic;

public class HasDiplomatPuppetStatus : HasPuppetStatus
{
	public HasDiplomatPuppetStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new HasDiplomatPuppetStatus(def);
	}

	public HasDiplomatPuppetStatus(Character puppet)
		: base(puppet)
	{
	}
}
