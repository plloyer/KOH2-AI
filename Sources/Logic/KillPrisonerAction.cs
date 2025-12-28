namespace Logic;

public class KillPrisonerAction : PrisonAction
{
	public KillPrisonerAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new KillPrisonerAction(owner as Kingdom, def);
	}

	public override void Run()
	{
		if (base.target is Character character)
		{
			own_kingdom.NotifyListeners("prisoner_executed", character);
			character.OnPrisonActionAnalytics("executed");
			character.Die(new DeadStatus("executed_in_prison", character));
			base.Run();
		}
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		Character character = base.target as Character;
		character.FillDeadVars(outcome_vars);
		outcome_vars.Set("target_kingdom", target_kingdom);
		outcome_vars.Set("prisoner", character);
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		switch (outcome.key)
		{
		case "execute_prisoner_prisoner_trigger":
		case "execute_prisoner_kingdom_trigger":
		case "execute_prisoner_their_kingdom_trigger":
		case "execute_prisoner_our_kingdom_trigger":
		case "rel_change_executed_prisoner":
		case "rel_change_released_prisoner":
			return outcome.Apply(base.game, this);
		default:
			return false;
		}
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		switch (outcome.key)
		{
		case "execute_prisoner_prisoner_trigger":
		case "execute_prisoner_kingdom_trigger":
		case "execute_prisoner_their_kingdom_trigger":
		case "execute_prisoner_our_kingdom_trigger":
		case "rel_change_executed_prisoner":
		case "rel_change_released_prisoner":
			return false;
		default:
			return base.ApplyOutcome(outcome);
		}
	}
}
