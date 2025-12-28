namespace Logic;

public class TortureAction : PrisonAction
{
	public TortureAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new TortureAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.target != null && !ValidateTarget(base.target))
		{
			return "invalid_target";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		if (target.GetKingdom().type == Kingdom.Type.RebelFaction)
		{
			return false;
		}
		return base.ValidateTarget(target);
	}

	public override Kingdom CalcTargetKingdom(Object target)
	{
		if (!(target is Character character))
		{
			return null;
		}
		if (character.reveal_kingdom != 0)
		{
			return base.game.GetKingdom(character.reveal_kingdom);
		}
		return character.GetKingdom();
	}

	public override void AlterOutcomeChance(OutcomeDef outcome, IVars vars)
	{
		if (!(base.target is Character character))
		{
			base.AlterOutcomeChance(outcome, vars);
			return;
		}
		switch (outcome.key)
		{
		case "success":
			if (character.reveal_kingdom == 0)
			{
				outcome.chance = 0f;
			}
			break;
		case "kingdom_revealed":
			if (character.reveal_kingdom == 0 || character.reveal_master != null)
			{
				outcome.chance = 0f;
			}
			break;
		case "master_revealed":
			if (character.reveal_master == null)
			{
				outcome.chance = 0f;
			}
			break;
		default:
			base.AlterOutcomeChance(outcome, vars);
			break;
		}
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		Character character = base.target as Character;
		switch (outcome.key)
		{
		case "nothing_found":
			character?.OnPrisonActionAnalytics("tortured_nothing_found");
			return true;
		case "kingdom_revealed":
			character?.OnPrisonActionAnalytics("tortured_kingdom_revealed");
			return true;
		case "master_revealed":
			character?.OnPrisonActionAnalytics("tortured_master_revealed");
			character?.reveal_master?.Imprison(own_kingdom, recall: true, send_state: true, "master_revealed");
			return true;
		case "target_killed":
			character?.OnPrisonActionAnalytics("tortured_killed");
			character?.Die(new DeadStatus("perish_during_torture", character));
			return true;
		default:
			return base.ApplyOutcome(outcome);
		}
	}

	public override void ApplyOutcomeEffects()
	{
		base.ApplyOutcomeEffects();
		if (base.target is Character character && (character.reveal_kingdom != 0 || character.reveal_master != null))
		{
			character.SetRevealKingdom(null, send_state: false);
			character.SetRevealMaster(null);
		}
	}
}
