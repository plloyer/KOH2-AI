namespace Logic;

public class HoldAFeastAction : Action
{
	public HoldAFeastAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new HoldAFeastAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (!CheckCost(base.target))
		{
			return "no_money";
		}
		return base.Validate(quick_out);
	}

	public override void Run()
	{
		own_kingdom?.NotifyListeners("hold_feast");
	}
}
