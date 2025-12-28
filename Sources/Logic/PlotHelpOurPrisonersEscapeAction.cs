using System.Collections.Generic;

namespace Logic;

public class PlotHelpOurPrisonersEscapeAction : SpyPlot
{
	private List<Character> prisoners = new List<Character>();

	public PlotHelpOurPrisonersEscapeAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PlotHelpOurPrisonersEscapeAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character == null || !base.own_character.IsSpy())
		{
			return "no_spy";
		}
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		if (mission_kingdom == null)
		{
			return "not_in_a_kingdom";
		}
		bool flag = false;
		for (int num = mission_kingdom.prisoners.Count - 1; num >= 0; num--)
		{
			Character character = mission_kingdom.prisoners[num];
			if (character != null && character.GetKingdom() == own_kingdom)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return "_no_possible_targets";
		}
		return base.Validate(quick_out);
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "success")
		{
			Kingdom kingdom = base.own_character?.mission_kingdom;
			if (kingdom != null)
			{
				for (int num = kingdom.prisoners.Count - 1; num >= 0; num--)
				{
					Character character = kingdom.prisoners[num];
					if (character != null && character.GetKingdom() == own_kingdom)
					{
						character.Imprison(null);
						character.NotifyListeners("escaped_prison");
						prisoners.Add(character);
					}
				}
				base.own_character?.NotifyListeners("helped_our_prisoners_escape", kingdom);
			}
		}
		return base.ApplyEarlyOutcome(outcome);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("prisoner", (prisoners.Count == 1) ? prisoners[0] : null);
		outcome_vars.Set("prisoners", prisoners);
	}

	public override void Run()
	{
		prisoners.Clear();
		base.Run();
	}
}
