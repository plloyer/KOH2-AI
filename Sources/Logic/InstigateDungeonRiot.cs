using System;

namespace Logic;

public class InstigateDungeonRiot : SpyPlot
{
	private Value actionTarget;

	private Value actionEscaped;

	private Value actionDied;

	public InstigateDungeonRiot(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new InstigateDungeonRiot(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character == null || !base.own_character.IsSpy())
		{
			return "no_spy";
		}
		if (base.own_character.mission_kingdom == null)
		{
			return "not_in_a_kingdom";
		}
		if (!HasValidPrisoners())
		{
			return "no_possible_targets";
		}
		return base.Validate(quick_out);
	}

	private bool HasValidPrisoners()
	{
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		if (Math.Max((float)mission_kingdom.royal_dungeon.prisoners.Count - mission_kingdom.GetStat(Stats.ks_prison_capacity), 0f) == 0f)
		{
			return false;
		}
		bool flag = false;
		foreach (Character prisoner in mission_kingdom.royal_dungeon.prisoners)
		{
			Action action = prisoner.actions?.Find("InspireRiotBaseAction");
			if (action != null && action.Validate() == "ok")
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		return true;
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "success")
		{
			Kingdom kingdom = base.own_character?.mission_kingdom;
			if (kingdom != null)
			{
				for (int i = 0; i < kingdom.prisoners.Count; i++)
				{
					Character character = kingdom.prisoners[i];
					if (character != null)
					{
						InspireRiotBaseAction inspireRiotBaseAction = character.actions.Find("InspireRiotBaseAction") as InspireRiotBaseAction;
						inspireRiotBaseAction?.ForceOutcomes("revolt");
						if (inspireRiotBaseAction != null && inspireRiotBaseAction.Execute(character))
						{
							actionTarget = character;
							actionEscaped = inspireRiotBaseAction.GetVar("escaped_prisoners");
							actionDied = inspireRiotBaseAction.GetVar("died_prisoners");
							base.own_character.NotifyListeners("dungeon_riot_instigated", base.own_character.mission_kingdom);
							break;
						}
						inspireRiotBaseAction?.ForceOutcomes(null);
					}
				}
			}
		}
		return base.ApplyEarlyOutcome(outcome);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("leader", actionTarget);
		outcome_vars.Set("escaped_prisoners", actionEscaped);
		outcome_vars.Set("died_prisoners", actionDied);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "cancel_condition")
		{
			return !HasValidPrisoners();
		}
		return base.GetVar(key, vars, as_value);
	}
}
