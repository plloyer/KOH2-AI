namespace Logic;

public class MissingStatus : Status
{
	public MissingStatus(Def def = null)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new MissingStatus(def);
	}

	public override bool IsDead()
	{
		return true;
	}
}
