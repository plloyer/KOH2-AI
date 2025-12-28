namespace Logic;

public class FreePuppetAction : FreePrisonerAction
{
	public FreePuppetAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public override bool ValidateTarget(Object target)
	{
		if (target == null || !target.IsValid())
		{
			return false;
		}
		if (target?.GetKingdom() != own_kingdom)
		{
			return false;
		}
		return true;
	}

	public new static Action Create(Object owner, Def def)
	{
		return new FreePuppetAction(owner as Kingdom, def);
	}
}
