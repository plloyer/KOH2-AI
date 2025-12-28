using System.Collections.Generic;

namespace Logic;

public class ClericStudyAction : Action
{
	public ClericStudyAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ClericStudyAction(owner as Character, def);
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			character.SetMissionKingdom(base.target as Kingdom);
			character.SetDefaultStatus<OnStudyStatus>();
			base.Run();
		}
	}

	public override bool ValidateTarget(Object target)
	{
		Kingdom kingdom = target as Kingdom;
		if (kingdom.IsEnemy(own_kingdom))
		{
			return false;
		}
		if (kingdom.is_pagan)
		{
			return false;
		}
		return base.ValidateTarget(target);
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return null;
		}
		foreach (Kingdom neighbor in kingdom.neighbors)
		{
			AddTarget(ref targets, neighbor);
		}
		return targets;
	}
}
