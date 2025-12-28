namespace Logic;

public class HasMarshalPuppetStatus : HasPuppetStatus
{
	public HasMarshalPuppetStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new HasMarshalPuppetStatus(def);
	}

	public HasMarshalPuppetStatus(Character puppet)
		: base(puppet)
	{
	}
}
