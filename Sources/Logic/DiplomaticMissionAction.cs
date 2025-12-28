using System.Collections.Generic;

namespace Logic;

public class DiplomaticMissionAction : CharacterOpportunity
{
	public DiplomaticMissionAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomaticMissionAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (own_kingdom.is_catholic && base.game.religions.catholic.hq_kingdom.IsDefeated())
		{
			return "papacy_destroyed";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		Realm realm = target as Realm;
		if (own_kingdom.is_catholic && realm != base.game.religions.catholic.hq_realm)
		{
			return false;
		}
		if (own_kingdom.is_orthodox && realm != base.game.religions.orthodox.hq_realm)
		{
			return false;
		}
		if (own_kingdom.IsEnemy(realm.GetKingdom()) || own_kingdom == realm.GetKingdom())
		{
			return false;
		}
		return base.ValidateTarget(target);
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		AddTarget(ref targets, base.game.religions.catholic.hq_realm);
		AddTarget(ref targets, base.game.religions.orthodox.hq_realm);
		return targets;
	}
}
