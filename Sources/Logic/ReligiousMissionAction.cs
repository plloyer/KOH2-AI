using System.Collections.Generic;

namespace Logic;

public class ReligiousMissionAction : CharacterOpportunity
{
	public ReligiousMissionAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ReligiousMissionAction(owner as Character, def);
	}

	public override bool ValidateTarget(Object target)
	{
		Realm realm = target as Realm;
		if (realm == base.game.religions.catholic.hq_realm && base.game.religions.catholic.hq_kingdom.IsDefeated())
		{
			return false;
		}
		if (realm == base.game.religions.catholic.holy_lands_realm && !realm.GetKingdom().is_christian)
		{
			return false;
		}
		if (realm == base.game.religions.orthodox.hq_realm && !realm.GetKingdom().is_christian)
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
		AddTarget(ref targets, base.game.religions.catholic.hq_realm);
		AddTarget(ref targets, base.game.religions.catholic.holy_lands_realm);
		AddTarget(ref targets, base.game.religions.orthodox.hq_realm);
		return targets;
	}
}
