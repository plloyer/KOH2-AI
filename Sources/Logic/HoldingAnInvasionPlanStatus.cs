namespace Logic;

public class HoldingAnInvasionPlanStatus : HoldingAPactStatus
{
	public HoldingAnInvasionPlanStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new HoldingAnInvasionPlanStatus(def);
	}
}
