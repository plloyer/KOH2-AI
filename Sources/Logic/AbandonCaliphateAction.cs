namespace Logic;

public class AbandonCaliphateAction : Action
{
	public AbandonCaliphateAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AbandonCaliphateAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return "not_a_kindom";
		}
		if (!kingdom.is_muslim || !kingdom.IsCaliphate())
		{
			return "not_caliphate";
		}
		return "ok";
	}

	public override void Run()
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom.jihad != null)
		{
			War.EndJihad(kingdom, "caliphate_abandoned", end_war: false);
		}
		kingdom.caliphate = false;
		Religion.RefreshModifiers(kingdom);
		kingdom.SendState<Kingdom.ReligionState>();
		kingdom.FireEvent("religion_changed", null);
		base.game.religions.FireEvent("caliphate_abandoned", kingdom);
		base.Run();
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (!(key == "rel_change_with_muslim_friends"))
		{
			if (key == "rel_change_with_muslim_enemies")
			{
				Value value = outcome.field.value;
				float num = outcome.field.GetFloat("rel_treshold");
				foreach (Kingdom kingdom in own_kingdom.game.kingdoms)
				{
					if (!kingdom.IsDefeated() && kingdom.is_muslim && kingdom != own_kingdom && kingdom.GetRelationship(own_kingdom) < num)
					{
						own_kingdom.AddRelationModifier(kingdom, "rel_change_with_muslim_enemies", null, value);
					}
				}
				return true;
			}
			return base.ApplyOutcome(outcome);
		}
		own_kingdom.SubResources(KingdomAI.Expense.Category.Religion, GetCost());
		Value value2 = outcome.field.value;
		float num2 = outcome.field.GetFloat("rel_treshold");
		foreach (Kingdom kingdom2 in own_kingdom.game.kingdoms)
		{
			if (!kingdom2.IsDefeated() && kingdom2.is_muslim && kingdom2 != own_kingdom && kingdom2.GetRelationship(own_kingdom) > num2)
			{
				own_kingdom.AddRelationModifier(kingdom2, "rel_change_with_muslim_friends", null, value2);
			}
		}
		return true;
	}
}
