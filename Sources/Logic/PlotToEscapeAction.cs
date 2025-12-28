using System.Collections.Generic;

namespace Logic;

public class PlotToEscapeAction : Action
{
	public bool mass_escape_allowed;

	private List<Character> escaped = new List<Character>();

	public PlotToEscapeAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PlotToEscapeAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (!(base.owner is Character character))
		{
			return "invalid owner";
		}
		if (character.prison_kingdom == null)
		{
			return "not imprisoned";
		}
		return base.Validate(quick_out);
	}

	public override void CreateOutcomeVars()
	{
		target_kingdom = base.own_character.prison_kingdom;
		base.CreateOutcomeVars();
		base.own_character.FillDeadVars(outcome_vars, is_owner: true);
	}

	public override void Run()
	{
		Character character = base.owner as Character;
		Kingdom prison_kingdom = character.prison_kingdom;
		character.Imprison(null);
		character.NotifyListeners("escape_plot_successful");
		character.NotifyListeners("escaped_prison");
		character.OnPrisonActionAnalytics("escaped");
		if (character.IsInSpecialCourt())
		{
			character.game.GetKingdom(character.GetSpecialCourtKingdomId())?.DelSpecialCourtMember(character);
		}
		if (mass_escape_allowed)
		{
			for (int num = prison_kingdom.royal_dungeon.prisoners.Count - 1; num >= 0; num--)
			{
				Character character2 = prison_kingdom.royal_dungeon.prisoners[num];
				if (character2 != null && character2.kingdom_id == character.kingdom_id && (float)base.game.Random(0, 100) < prison_kingdom.royal_dungeon.def.plot_escape_together_chance)
				{
					escaped.Add(character2);
					character2.Imprison(null);
					character2.NotifyListeners("escaped_prison");
					character2.OnPrisonActionAnalytics("escaped_during_mass_escape");
					if (character.IsInSpecialCourt())
					{
						character.game.GetKingdom(character.GetSpecialCourtKingdomId())?.DelSpecialCourtMember(character);
					}
				}
			}
		}
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "escaped_prisoners")
		{
			if (escaped.Count <= 0)
			{
				return Value.Null;
			}
			return new Value(Action.get_prisoners_text(escaped));
		}
		return base.GetVar(key, vars, as_value);
	}
}
