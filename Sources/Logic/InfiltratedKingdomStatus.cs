namespace Logic;

public class InfiltratedKingdomStatus : Status
{
	public InfiltratedKingdomStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new InfiltratedKingdomStatus(def);
	}

	public override bool IsIdle()
	{
		return true;
	}
}
