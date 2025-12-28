namespace Logic;

public class ImprovingOpinionsStatus : Status
{
	public ImprovingOpinionsStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new ImprovingOpinionsStatus(def);
	}
}
