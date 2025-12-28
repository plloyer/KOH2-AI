namespace Logic;

public class TradeExpeditionAction : Action
{
	public TradeExpeditionAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new TradeExpeditionAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.statuses == null)
		{
			return "no_statuses";
		}
		int num = 0;
		if (character.statuses != null)
		{
			for (int i = 0; i < character.statuses.Count; i++)
			{
				if (character.statuses[i] is TradeExpeditionStatus)
				{
					num++;
				}
			}
		}
		if (def != null && num >= def.field.GetInt("max_expeditions"))
		{
			return "too_many_expeditions";
		}
		if (!own_kingdom.HasAdmiralty())
		{
			return "no_admiralty";
		}
		return base.Validate(quick_out);
	}

	public override void AlterOutcomeChance(OutcomeDef outcome, IVars vars)
	{
		string key = outcome.key;
		if (key == "establish_trade_colony")
		{
			float allocatedCommerce = own_kingdom.GetAllocatedCommerce();
			float maxCommerce = own_kingdom.GetMaxCommerce();
			DT.Field field = base.game.defs.Find<Status.Def>("TradeExpeditionStatus")?.field?.FindChild("upkeep")?.FindChild("trade");
			if (field != null)
			{
				Vars vars2 = new Vars();
				vars2.Set("own_kingdom", own_kingdom);
				vars2.Set("owner", base.own_character);
				vars2.Set("own_character", base.own_character);
				if (field.Float(vars2) + allocatedCommerce > maxCommerce)
				{
					outcome.chance = 0f;
				}
				return;
			}
		}
		base.AlterOutcomeChance(outcome, vars);
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		switch (outcome.key)
		{
		case "bring_a_lot_of_gold":
			own_kingdom.AddResources(KingdomAI.Expense.Category.Economy, ResourceType.Gold, outcome.field.FindChild("profit").Value(new Vars(this)));
			base.own_character?.NotifyListeners("trade_expedition_suceeded_gold");
			return true;
		case "establish_trade_colony":
			own_kingdom.AddResources(KingdomAI.Expense.Category.Economy, ResourceType.Gold, outcome.field.FindChild("profit").Value(new Vars(this)));
			base.own_character?.NotifyListeners("trade_expedition_suceeded_colony");
			base.own_character?.NotifyListeners("trade_colony_established");
			base.owner.AddStatus<TradeExpeditionStatus>(allow_multiple: true);
			return true;
		case "default":
			return true;
		default:
			return base.ApplyOutcome(outcome);
		}
	}

	public override void Prepare()
	{
		base.Prepare();
		own_kingdom?.InvalidateIncomes();
	}
}
