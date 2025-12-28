using System.Collections.Generic;

namespace Logic;

public class SowDissentAction : SpyPlot
{
	public SowDissentAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new SowDissentAction(owner as Character, def);
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

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		List<Opinion> list = base.own_character?.mission_kingdom?.opinions?.opinions;
		if (list != null && list.Count > 0)
		{
			outcome_vars.Set("random_opinion", list[base.game.Random(0, list.Count)]?.def.field.key);
		}
	}

	public override void OnTick()
	{
		ApplyOutcomes();
	}
}
