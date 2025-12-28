namespace Logic;

public class RebelStatus : Status
{
	public RebelStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new RebelStatus(def);
	}

	public override bool IsAutomatic()
	{
		return true;
	}
}
