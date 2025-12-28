using System;
using System.Collections.Generic;

namespace Logic;

public class PuppetPreachAction : PuppetPlot
{
	private List<Realm> realms;

	public PuppetPreachAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetPreachAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (puppet.GetArmy()?.rebel != null)
		{
			return "is_rebel";
		}
		if (base.own_character.mission_kingdom == null)
		{
			return "not_on_a_mission";
		}
		if (base.own_character.mission_kingdom.religion == own_kingdom.religion)
		{
			return "same_religion";
		}
		bool flag = false;
		for (int i = 0; i < base.own_character.mission_kingdom.realms.Count; i++)
		{
			if (base.own_character.mission_kingdom.realms[i].religion != own_kingdom.religion && !base.own_character.mission_kingdom.realms[i].IsOccupied())
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return "no_possible_targets";
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
			if (!realm.IsOccupied() && realm.religion != own_kingdom.religion)
			{
				if (realms == null)
				{
					realms = new List<Realm>();
				}
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
			realms[i].SetReligion(own_kingdom.religion);
		}
		base.Run();
	}
}
