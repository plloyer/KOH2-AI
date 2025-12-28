namespace Logic;

public class InPrisonStatus : Status
{
	public InPrisonStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new InPrisonStatus(def);
	}

	public override bool IsAutomatic()
	{
		return true;
	}
}
