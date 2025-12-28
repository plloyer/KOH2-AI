using System.Collections.Generic;

namespace Logic;

public class PacifyRebelsAction : CharacterOpportunity
{
	public PacifyRebelsAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PacifyRebelsAction(owner as Character, def);
	}

	public override bool ValidateTarget(Object target)
	{
		Character character = target as Character;
		Rebellion rebellion = character?.GetArmy()?.rebel?.rebellion;
		if (rebellion == null)
		{
			return false;
		}
		if (!character.IsAlive())
		{
			return false;
		}
		if (rebellion.leader.character != character)
		{
			return false;
		}
		if (!own_kingdom.rebellions.Contains(rebellion))
		{
			return false;
		}
		if (rebellion.GetOriginRealm().GetKingdom() != own_kingdom)
		{
			return false;
		}
		return true;
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return null;
		}
		for (int i = 0; i < kingdom.rebellions.Count; i++)
		{
			AddTarget(ref targets, kingdom.rebellions[i].leader.character);
		}
		return targets;
	}

	public override void Run()
	{
		Rebellion rebellion = ((base.target as Character)?.GetArmy()?.rebel)?.rebellion;
		if (rebellion != null)
		{
			rebellion.Disband();
			base.Run();
		}
	}
}
