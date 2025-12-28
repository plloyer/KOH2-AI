namespace Logic;

public class ExportingFoodStatus : ImportExportFoodStatus
{
	public new delegate ExportingFoodStatus CreateByValues(Game game, float foodAmount, float gold, float commerceUpkeep);

	public ExportingFoodStatus(Def def)
		: base(def)
	{
	}

	public ExportingFoodStatus(Game game, float foodAmount, float gold, float commerceUpkeep)
		: base(game, foodAmount, gold, commerceUpkeep)
	{
		def = game.defs.Get<Def>(rtti.name);
	}

	public new static Status Create(Def def)
	{
		return new ExportingFoodStatus(def);
	}

	public new static ExportingFoodStatus Create(Game game, float foodAmount, float gold, float commerceUpkeep)
	{
		return new ExportingFoodStatus(game, foodAmount, gold, commerceUpkeep);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "gold")
		{
			return gold * (1f + base.own_character.GetStat(Stats.cs_export_food_gold_increase_perc) / 100f);
		}
		return base.GetVar(key, vars, as_value);
	}
}
