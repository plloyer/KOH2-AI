namespace Logic;

public class NotGoverningStatus : Status
{
	public NotGoverningStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new NotGoverningStatus(def);
	}

	public override bool IsAutomatic()
	{
		return true;
	}

	public override void GetProgress(out float cur, out float max)
	{
		if (base.own_character.IsPrepairngToGovern(out var a))
		{
			a.GetProgress(out cur, out max);
		}
		else
		{
			cur = (max = 0f);
		}
	}
}
