namespace Logic;

public class ArmyStarvingStatus : Status
{
	public ArmyStarvingStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new ArmyStarvingStatus(def);
	}
}
