namespace Logic;

public class GoverningStatus : Status
{
	public GoverningStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new GoverningStatus(def);
	}

	public override bool IsAutomatic()
	{
		return true;
	}
}
