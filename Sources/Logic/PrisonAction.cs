namespace Logic;

public class PrisonAction : Action
{
	public PrisonAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PrisonAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		return "ok";
	}

	public override Character GetVoicingCharacter()
	{
		return (base.target as Character) ?? base.own_character;
	}

	public override IVars GetVoiceVars()
	{
		return GetVoicingCharacter();
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		if (base.target is Character character)
		{
			outcome_vars.Set("kingdom", character.GetKingdom());
		}
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "prisoner":
			return base.target as Character;
		case "relationship_release_war_mod":
		case "relationship_release_royalty_mod":
		case "relationship_execute_war_mod":
		case "relationship_execute_royalty_mod":
		case "relationship_execute":
		case "relationship_release":
			return own_kingdom.royal_dungeon.GetVar(key, this, as_value);
		case "rel_kingdom":
		{
			Kingdom kingdom = base.target?.GetKingdom();
			if (kingdom != null && kingdom.type == Kingdom.Type.Regular)
			{
				return kingdom;
			}
			return Value.Unknown;
		}
		default:
			return base.GetVar(key, vars, as_value);
		}
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "rel_change_executed_prisoner" || key == "rel_change_released_prisoner")
		{
			return outcome.Apply(base.game, this);
		}
		return false;
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "rel_change_executed_prisoner" || key == "rel_change_released_prisoner")
		{
			return false;
		}
		return base.ApplyOutcome(outcome);
	}
}
