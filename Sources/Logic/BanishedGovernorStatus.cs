namespace Logic;

public class BanishedGovernorStatus : Status
{
	public Castle castle;

	public BanishedGovernorStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new BanishedGovernorStatus(def);
	}

	public override bool IsAutomatic()
	{
		return true;
	}
}
