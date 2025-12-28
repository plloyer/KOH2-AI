namespace Logic;

public class IdleThroneRoom : Status
{
	public IdleThroneRoom(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new IdleThroneRoom(def);
	}

	public override bool IsAutomatic()
	{
		return true;
	}
}
