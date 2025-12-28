using System.Collections.Generic;

namespace Logic;

public class EnlightmentMissionAction : CharacterOpportunity
{
	public EnlightmentMissionAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new EnlightmentMissionAction(owner as Character, def);
	}

	public override bool ValidateTarget(Object target)
	{
		Realm realm = target as Realm;
		if (!realm.GetKingdom().is_christian)
		{
			return false;
		}
		if (own_kingdom.IsEnemy(realm.GetKingdom()) || realm.GetKingdom() == own_kingdom)
		{
			return false;
		}
		return base.ValidateTarget(target);
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		AddTarget(ref targets, base.game.religions.orthodox.hq_realm);
		return targets;
	}
}
