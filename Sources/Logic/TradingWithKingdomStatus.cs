namespace Logic;

public class TradingWithKingdomStatus : Status
{
	public TradingWithKingdomStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new TradingWithKingdomStatus(def);
	}

	public override bool IsIdle()
	{
		return true;
	}
}
