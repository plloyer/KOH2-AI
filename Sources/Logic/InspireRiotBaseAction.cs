using System.Collections.Generic;

namespace Logic;

public class InspireRiotBaseAction : Action
{
	private List<Character> escaped = new List<Character>();

	private List<Character> died = new List<Character>();

	private Kingdom prison_kingdom;

	public InspireRiotBaseAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new InspireRiotBaseAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (!(base.owner is Character character))
		{
			return "invalid owner";
		}
		if (character.prison_kingdom == null)
		{
			return "not_imprisoned";
		}
		return base.Validate(quick_out);
	}

	public override void CreateOutcomeVars()
	{
		target_kingdom = base.own_character.prison_kingdom ?? prison_kingdom;
		base.CreateOutcomeVars();
		base.own_character.FillDeadVars(outcome_vars, is_owner: true);
	}

	public override void AlterOutcomeChance(OutcomeDef outcome, IVars vars)
	{
		string key = outcome.key;
		if (key == "revolt")
		{
			int num = outcome.field.GetInt("required_rebel_pop", vars);
			if ((base.owner as Character).prison_kingdom.GetWeightedRebelliosRealm(num) == null)
			{
				outcome.chance = 0f;
			}
			else
			{
				outcome.chance = 100f;
			}
		}
		else
		{
			base.AlterOutcomeChance(outcome, vars);
		}
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (!(key == "revolt"))
		{
			if (key == "escape")
			{
				prison_kingdom = base.own_character.prison_kingdom;
				Character character = base.owner as Character;
				character.OnPrisonActionAnalytics("escaped_during_revolt", GetCost());
				character.Imprison(null);
				if (character.IsInSpecialCourt())
				{
					character.game.GetKingdom(character.GetSpecialCourtKingdomId())?.DelSpecialCourtMember(character);
				}
				character.NotifyListeners("escaped_prison");
				return true;
			}
			return base.ApplyEarlyOutcome(outcome);
		}
		prison_kingdom = base.own_character.prison_kingdom;
		escaped.Clear();
		died.Clear();
		int num = outcome.field.GetInt("required_rebel_pop", this);
		Character character2 = base.owner as Character;
		Kingdom kingdom = character2.prison_kingdom;
		character2.OnPrisonActionAnalytics("organized_revolt", GetCost());
		character2.Imprison(null);
		character2.NotifyListeners("escaped_prison");
		character2.NotifyListeners("revolt_inspired");
		Army army = character2.GetArmy();
		Realm weightedRebelliosRealm = kingdom.GetWeightedRebelliosRealm(num);
		if (weightedRebelliosRealm != null && character2.CanLeadArmy())
		{
			weightedRebelliosRealm.castle.population.RemoveVillagers(num, Population.Type.Rebel);
			if (army == null)
			{
				army = character2.SpawnArmy(weightedRebelliosRealm.castle);
			}
			else
			{
				army.SetPosition(weightedRebelliosRealm.castle.GetRandomExitPoint());
			}
			int num2 = base.game.Random(kingdom.royal_dungeon.def.inspire_riot_unit_count_min, kingdom.royal_dungeon.def.inspire_riot_unit_count_max + 1);
			List<Unit.Def> availableUnitTypes = weightedRebelliosRealm.castle.GetAvailableUnitTypes();
			for (int i = 0; i < num2; i++)
			{
				Unit.Def def = availableUnitTypes[base.game.Random(0, availableUnitTypes.Count)];
				army.AddUnit(def);
			}
			army.SetSupplies(army.CalcMaxSupplies() / 2f);
			if (War.CanStart(kingdom, character2.GetKingdom()))
			{
				kingdom.StartWarWith(character2.GetKingdom(), War.InvolvementReason.PrisonRevolt);
			}
		}
		for (int num3 = kingdom.royal_dungeon.prisoners.Count - 1; num3 >= 0; num3--)
		{
			Character character3 = kingdom.royal_dungeon.prisoners[num3];
			if (character3 != null)
			{
				if ((float)base.game.Random(0, 100) < kingdom.royal_dungeon.def.inspire_riot_death_chance && died.Count < kingdom.royal_dungeon.def.riot_max_died)
				{
					died.Add(character3);
					character2.OnPrisonActionAnalytics("died_during_revolt");
					character2.OnPrisonActionAnalytics("organized_revolt", GetCost());
					character3.Die(new DeadStatus("prison_break", character3));
				}
				else if (escaped.Count < kingdom.royal_dungeon.def.riot_max_escaped)
				{
					escaped.Add(character3);
					character2.OnPrisonActionAnalytics("escaped_during_revolt");
					character3.Imprison(null);
					character3.NotifyListeners("escaped_prison");
				}
			}
		}
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "escaped_prisoners":
			if (escaped.Count <= 0)
			{
				return Value.Null;
			}
			return new Value(escaped);
		case "died_prisoners":
			if (died.Count <= 0)
			{
				return Value.Null;
			}
			return new Value(died);
		case "death_chance":
			return (base.owner as Character).prison_kingdom.royal_dungeon.def.inspire_riot_death_chance;
		case "conspiracy_bonus":
		{
			Character character = base.owner as Character;
			return (character.GetSkill("Conspiracy") != null) ? character.prison_kingdom.royal_dungeon.def.inspire_riot_conspiracy_bonus : 0f;
		}
		default:
			return base.GetVar(key, vars, as_value);
		}
	}
}
