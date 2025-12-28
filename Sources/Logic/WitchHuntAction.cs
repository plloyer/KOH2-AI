using System.Collections.Generic;

namespace Logic;

public class WitchHuntAction : SpyPlot
{
	public static bool from_event = false;

	private static List<Character> spies = new List<Character>(3);

	public WitchHuntAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new WitchHuntAction(owner as Character, def);
	}

	public void CatchSpies()
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return;
		}
		spies.Clear();
		_ = kingdom.foreigners.Count;
		for (int num = kingdom.foreigners.Count - 1; num >= 0; num--)
		{
			Character character = kingdom.foreigners[num];
			if (character.IsSpy() && character != null)
			{
				Action action = character.actions?.Find("WitchHuntRevealAction");
				if (action != null)
				{
					action.Execute(null);
					if (character.mission_kingdom != own_kingdom)
					{
						spies.Add(character);
						if (spies.Count >= def.field.GetInt("max_caught_spies"))
						{
							break;
						}
					}
				}
			}
		}
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "success")
		{
			CatchSpies();
			return true;
		}
		return base.ApplyEarlyOutcome(outcome);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		List<Character> list = null;
		List<Character> list2 = null;
		List<Character> list3 = null;
		for (int i = 0; i < spies.Count; i++)
		{
			Character character = spies[i];
			if (character.IsDead())
			{
				if (list3 == null)
				{
					list3 = new List<Character>();
				}
				list3.Add(character);
			}
			else if (character.prison_kingdom == own_kingdom)
			{
				if (list == null)
				{
					list = new List<Character>();
				}
				list.Add(character);
			}
			else
			{
				if (list2 == null)
				{
					list2 = new List<Character>();
				}
				list2.Add(character);
			}
		}
		outcome_vars.Set("caught_spies", spies);
		outcome_vars.Set("caught_spies_killed", list3);
		outcome_vars.Set("caught_spies_imprisoned", list);
		outcome_vars.Set("caught_spies_escaped", list2);
		outcome_vars.Set("caught_any_spies", spies.Count != 0);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "from_event")
		{
			return from_event;
		}
		return base.GetVar(key, vars, as_value);
	}
}
