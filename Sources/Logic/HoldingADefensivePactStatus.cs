namespace Logic;

public class HoldingADefensivePactStatus : HoldingAPactStatus
{
	public HoldingADefensivePactStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new HoldingADefensivePactStatus(def);
	}
}
