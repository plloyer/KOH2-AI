namespace Logic;

public class HelpTheWeakStatus : Status
{
	public HelpTheWeakStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new HelpTheWeakStatus(def);
	}
}
