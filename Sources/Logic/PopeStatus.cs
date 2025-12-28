namespace Logic;

public class PopeStatus : Status
{
	public PopeStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new PopeStatus(def);
	}
}
