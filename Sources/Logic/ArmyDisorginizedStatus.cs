namespace Logic;

public class ArmyDisorginizedStatus : Status
{
	public ArmyDisorginizedStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new ArmyDisorginizedStatus(def);
	}
}
