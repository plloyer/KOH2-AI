namespace Logic;

public class HasRoyalBloodStatus : Status
{
	public HasRoyalBloodStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new HasRoyalBloodStatus(def);
	}
}
