using System.Collections.Generic;

namespace Logic;

public class PuppetArmyRevoltAction : PuppetPlot
{
	private List<Value> additionalRevoltLeaders = new List<Value>();

	public PuppetArmyRevoltAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetArmyRevoltAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		string text = ValidateLeader(puppet);
		if (text != "ok")
		{
			return text;
		}
		return base.ValidatePuppet(puppet);
	}

	private string ValidateLeader(Character c)
	{
		if (c == null)
		{
			return "is_null";
		}
		if (c.IsKing())
		{
			return "is_king";
		}
		if (c == c.game.religions?.catholic?.crusade?.leader)
		{
			return "is_crusader";
		}
		if (c == c.game.religions?.catholic?.head)
		{
			return "is_pope";
		}
		if (c.GetArmy() == null)
		{
			return "no_army";
		}
		if (c.GetArmy()?.rebel != null)
		{
			return "is_rebel";
		}
		if (c.GetArmy().battle != null)
		{
			return "in_battle";
		}
		return "ok";
	}

	private void CalcMassRevoltLeaders()
	{
		Character character = base.target as Character;
		additionalRevoltLeaders.Clear();
		Kingdom kingdom = character.GetKingdom();
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character2 = kingdom.court[i];
			if (ValidateLeader(character2) != "ok" || character2 == character)
			{
				continue;
			}
			Action action = character2.FindAction("PuppetArmyRevoltAdditionalAction");
			if (action == null)
			{
				continue;
			}
			if (character2 == character)
			{
				base.own_character.DelPuppet(character2);
			}
			action.args = new List<Value>();
			action.AddArg(ref action.args, base.own_character, 0);
			action.AddArg(ref action.args, character, 1);
			action.Execute(null);
			if ((action as PuppetArmyRevoltAdditionalAction).wasSuccessful)
			{
				additionalRevoltLeaders.Add(character2);
				if ((float)(additionalRevoltLeaders.Count + 1) > def.field.GetFloat("max_rebel_armies"))
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
			CalcMassRevoltLeaders();
			return true;
		}
		return base.ApplyEarlyOutcome(outcome);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("Marshal", base.game.defs.Get<CharacterClass.Def>("Marshal"));
		outcome_vars.Set("puppet_class", (base.target as Character).GetClass());
		outcome_vars.Set("revolt_leaders", additionalRevoltLeaders);
		outcome_vars.Set("revolt_leaders_num", additionalRevoltLeaders.Count + 1);
	}

	public override void Run()
	{
		Character character = base.target as Character;
		if (character.GetArmy().castle != null)
		{
			character.GetArmy().LeaveCastle(character.GetArmy().castle.GetRandomExitPoint());
		}
		base.own_character.DelPuppet(character);
		character.TurnIntoRebel("GeneralRebels");
		base.Run();
	}
}
