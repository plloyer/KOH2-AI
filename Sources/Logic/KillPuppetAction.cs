namespace Logic;

public class KillPuppetAction : KillPrisonerAction
{
	public KillPuppetAction(Kingdom owner, Def def)
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
		return new KillPuppetAction(owner as Kingdom, def);
	}
}
