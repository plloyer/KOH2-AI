namespace Logic;

public class SpyPlot : CharacterOpportunity
{
	public SpyPlot(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new SpyPlot(owner as Character, def);
	}

	public override string ValidateMissionKingdom()
	{
		Kingdom kingdom = base.own_character?.mission_kingdom;
		if (kingdom != null)
		{
			int num = (kingdom.is_player ? base.game.rules.espionage_vs_players_max_severity : base.game.rules.espionage_vs_AI_max_severity);
			if (def.severity_id > num)
			{
				if (def.opportunity != null)
				{
					return "severity";
				}
				return "_severity";
			}
		}
		return base.ValidateMissionKingdom();
	}

	public override bool ValidateTarget(Object target)
	{
		Kingdom kingdom = target?.GetKingdom();
		if (kingdom != null)
		{
			int num = (kingdom.is_player ? base.game.rules.espionage_vs_players_max_severity : base.game.rules.espionage_vs_AI_max_severity);
			if (def.severity_id > num)
			{
				return false;
			}
		}
		return base.ValidateTarget(target);
	}

	public override Resource GetCost(Object target, IVars vars = null)
	{
		Resource cost = base.GetCost(target, vars);
		cost?.Mul(1f + base.own_character.GetStat(Stats.cs_plotting_cost_perc) / 100f);
		if ((object)cost != null)
		{
			cost.Round(def.field.GetFloat("cost_rounding_precision", this, 50f));
			return cost;
		}
		return cost;
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (!(key == "owner_imprisoned"))
		{
			if (key == "owner_killed")
			{
				base.own_character.NotifyListeners("spy_capture_killed");
				return true;
			}
			return base.ApplyEarlyOutcome(outcome);
		}
		base.own_character.NotifyListeners("spy_capture_imprisoned");
		return true;
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		switch (outcome.key)
		{
		case "success_undetected":
			return true;
		case "fail_undetected":
			return true;
		case "invalidated":
			return true;
		case "success_revealed":
		case "fail_revealed":
		case "plot_revealed":
			base.own_character?.SetRevealedInKingdom(base.own_character?.mission_kingdom);
			return true;
		default:
			return base.ApplyOutcome(outcome);
		}
	}
}
