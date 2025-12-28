namespace Logic;

public class WidowedStatus : MarriedStatus
{
	public WidowedStatus(Def def)
		: base(def)
	{
	}

	public WidowedStatus(Marriage marriage)
		: base(marriage)
	{
	}

	public new static Status Create(Def def)
	{
		return new WidowedStatus(def);
	}

	public new static WidowedStatus Create(Marriage marriage)
	{
		return new WidowedStatus(marriage);
	}

	protected override void OnDestroy()
	{
		if (marriage != null && marriage.IsValid())
		{
			marriage.Destroy();
		}
		base.OnDestroy();
	}
}
