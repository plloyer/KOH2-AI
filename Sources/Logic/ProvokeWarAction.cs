using System.Collections.Generic;

namespace Logic;

public class ProvokeWarAction : SpyPlot
{
	public ProvokeWarAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ProvokeWarAction(owner as Character, def);
	}

	public override List<Object> GetPossibleTargets()
	{
		Kingdom kingdom = base.own_character?.mission_kingdom;
		if (kingdom == null)
		{
			return null;
		}
		List<Object> targets = null;
		foreach (Kingdom neighbor in kingdom.neighbors)
		{
			AddTarget(ref targets, neighbor);
		}
		return targets;
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Kingdom kingdom))
		{
			return false;
		}
		Kingdom kingdom2 = base.own_character?.mission_kingdom;
		if (!War.CanStart(kingdom2, kingdom))
		{
			return false;
		}
		float relationship = kingdom2.GetRelationship(kingdom);
		float num = def.field.GetFloat("max_relationship", this, RelationUtils.Def.maxRelationship);
		if (relationship > num)
		{
			return false;
		}
		return true;
	}

	public override void Run()
	{
		Kingdom kingdom = base.own_character?.mission_kingdom;
		if (kingdom != null && target_kingdom != null && War.CanStart(kingdom, target_kingdom))
		{
			kingdom.StartWarWith(target_kingdom, War.InvolvementReason.SpyProvocation, "WarDeclaredMessage");
			base.own_character?.NotifyListeners("spy_provoke_war_success");
			base.Run();
		}
	}
}
