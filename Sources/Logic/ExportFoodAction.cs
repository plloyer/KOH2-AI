namespace Logic;

public class ExportFoodAction : MerchantOpportunity
{
	public ExportFoodAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ExportFoodAction(owner as Character, def);
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
		if (own_kingdom.GetFood() < def.field.GetFloat("min_food"))
		{
			return "too_much_food";
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
		ExportingFoodStatus status = new ExportingFoodStatus(base.game, def.field.GetFloat("food_amount"), def.field.GetFloat("gold_gain_modded", this), def.field.GetFloat("commerce_upkeep"));
		base.own_character.AddStatus(status);
		own_kingdom?.InvalidateIncomes();
		base.Run();
	}
}
