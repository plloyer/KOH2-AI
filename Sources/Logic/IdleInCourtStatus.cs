namespace Logic;

public class IdleInCourtStatus : Status
{
	public IdleInCourtStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new IdleInCourtStatus(def);
	}

	public override bool IsAutomatic()
	{
		return true;
	}

	public override bool IsIdle()
	{
		return true;
	}
}
