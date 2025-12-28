using System.Collections.Generic;

namespace Logic;

public class ReligionSpreadAction : CharacterOpportunity
{
	public ReligionSpreadAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ReligionSpreadAction(owner as Character, def);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Realm realm))
		{
			return false;
		}
		if (realm.religion == own_kingdom.religion)
		{
			return false;
		}
		if (realm.IsOccupied())
		{
			return false;
		}
		if (!own_kingdom.externalBorderRealms.Contains(realm))
		{
			return false;
		}
		return base.ValidateTarget(target);
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		for (int i = 0; i < own_kingdom.externalBorderRealms.Count; i++)
		{
			AddTarget(ref targets, own_kingdom.externalBorderRealms[i]);
		}
		return targets;
	}

	public override void Run()
	{
		(base.target as Realm)?.SetReligion(own_kingdom.religion);
		base.Run();
	}
}
