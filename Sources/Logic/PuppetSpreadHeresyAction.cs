using System;
using System.Collections.Generic;

namespace Logic;

public class PuppetSpreadHeresyAction : PuppetPlot
{
	private List<Realm> realms = new List<Realm>();

	public PuppetSpreadHeresyAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetSpreadHeresyAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (puppet.GetKingdom().is_pagan)
		{
			return "is_pagan";
		}
		if (puppet.GetArmy()?.rebel != null)
		{
			return "is_rebel";
		}
		return base.ValidatePuppet(puppet);
	}

	private void CalcRealms()
	{
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		int num = base.game.Random(0, mission_kingdom.realms.Count);
		int val = base.game.Random(1, def.field.GetInt("max_realms") + 1);
		val = Math.Min(val, (int)((float)mission_kingdom.realms.Count * def.field.GetFloat("max_realms_perc") / 100f));
		val = Math.Max(1, val);
		realms?.Clear();
		for (int i = 0; i < mission_kingdom.realms.Count; i++)
		{
			Realm realm = mission_kingdom.realms[(num + i) % mission_kingdom.realms.Count];
			if (!realm.IsOccupied() && !realm.is_pagan)
			{
				realms.Add(realm);
				val--;
				if (val <= 0)
				{
					break;
				}
			}
		}
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "success")
		{
			CalcRealms();
			return true;
		}
		return base.ApplyEarlyOutcome(outcome);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("realms", realms);
	}

	public override void Run()
	{
		for (int i = 0; i < realms.Count; i++)
		{
			realms[i].SpreadHeresy(100f);
		}
		base.Run();
	}
}
