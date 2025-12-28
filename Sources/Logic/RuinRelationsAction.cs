using System.Collections.Generic;

namespace Logic;

public class RuinRelationsAction : SpyPlot
{
	public RuinRelationsAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new RuinRelationsAction(owner as Character, def);
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

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "owner_escaped")
		{
			Cancel();
		}
		return base.ApplyOutcome(outcome);
	}

	public override void OnTick()
	{
		ApplyOutcomes();
	}
}
