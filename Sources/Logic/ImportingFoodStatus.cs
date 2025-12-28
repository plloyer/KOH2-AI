using System;

namespace Logic;

public class ImportingFoodStatus : ImportExportFoodStatus
{
	public new delegate ImportingFoodStatus CreateByValues(Game game, float foodAmount, float goldUpkeep, float commerceUpkeep);

	public ImportingFoodStatus(Def def)
		: base(def)
	{
	}

	public ImportingFoodStatus(Game game, float foodAmount, float goldUpkeep, float commerceUpkeep)
		: base(game, foodAmount, goldUpkeep, commerceUpkeep)
	{
		def = game.defs.Get<Def>(rtti.name);
	}

	public new static Status Create(Def def)
	{
		return new ImportingFoodStatus(def);
	}

	public new static ImportingFoodStatus Create(Game game, float foodAmount, float goldUpkeep, float commerceUpkeep)
	{
		return new ImportingFoodStatus(game, foodAmount, goldUpkeep, commerceUpkeep);
	}

	public override float CalcGoldUpkeep()
	{
		return (float)Math.Round(gold * (1f - base.own_kingdom.GetStat(Stats.ks_import_goods_and_food_upkeep_reduction_perc) / 100f));
	}
}
