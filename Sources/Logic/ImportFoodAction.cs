namespace Logic;

public class ImportFoodAction : MerchantOpportunity
{
	public ImportFoodAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ImportFoodAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.mission_kingdom == null)
		{
			return "not_in_a_mission_kingdom";
		}
		if (own_kingdom.GetFood() > def.field.GetFloat("max_food"))
		{
			return "too_much_food";
		}
		if (own_kingdom.expenses[ResourceType.Food] < def.field.GetFloat("min_food_usage"))
		{
			return "too_little_consumption";
		}
		if (character.FindStatus<ImportingFoodStatus>() != null)
		{
			return "already_importing";
		}
		if (character.FindStatus<ExportingFoodStatus>() != null)
		{
			return "already_exporting";
		}
		return base.Validate(quick_out);
	}

	public override void Prepare()
	{
		base.Prepare();
		own_kingdom?.InvalidateIncomes();
	}

	public override void Run()
	{
		ImportingFoodStatus status = new ImportingFoodStatus(base.game, def.field.GetFloat("food_amount"), def.field.GetFloat("gold_upkeep"), def.field.GetFloat("commerce_upkeep"));
		base.own_character.AddStatus(status);
		own_kingdom?.InvalidateIncomes();
		base.Run();
	}
}
