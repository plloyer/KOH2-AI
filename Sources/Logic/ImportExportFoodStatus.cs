namespace Logic;

public class ImportExportFoodStatus : Status
{
	public delegate ImportExportFoodStatus CreateByValues(Game game, float foodAmount, float gold, float commerceUpkeep);

	public new class FullData : Status.FullData
	{
		public float foodAmount;

		public float gold;

		public float commerceUpkeep;

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!base.InitFrom(obj))
			{
				return false;
			}
			if (!(obj is ImportExportFoodStatus importExportFoodStatus))
			{
				return false;
			}
			foodAmount = importExportFoodStatus.foodAmount;
			gold = importExportFoodStatus.gold;
			commerceUpkeep = importExportFoodStatus.commerceUpkeep;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteFloat(foodAmount, "foodAmount");
			ser.WriteFloat(gold, "gold");
			ser.WriteFloat(commerceUpkeep, "commerceUpkeep");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			foodAmount = ser.ReadFloat("foodAmount");
			gold = ser.ReadFloat("gold");
			commerceUpkeep = ser.ReadFloat("commerceUpkeep");
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!base.ApplyTo(obj, game))
			{
				return false;
			}
			if (!(obj is ImportExportFoodStatus importExportFoodStatus))
			{
				return false;
			}
			importExportFoodStatus.foodAmount = foodAmount;
			importExportFoodStatus.gold = gold;
			importExportFoodStatus.commerceUpkeep = commerceUpkeep;
			return true;
		}
	}

	public float foodAmount;

	public float gold;

	public float commerceUpkeep;

	public ImportExportFoodStatus(Def def)
		: base(def)
	{
	}

	public ImportExportFoodStatus(Game game, float foodAmount, float gold, float commerceUpkeep)
		: base(null)
	{
		this.foodAmount = foodAmount;
		this.gold = gold;
		this.commerceUpkeep = commerceUpkeep;
		def = game.defs.Get<Def>(rtti.name);
	}

	public new static Status Create(Def def)
	{
		return new ImportExportFoodStatus(def);
	}

	public static ImportExportFoodStatus Create(Game game, float foodAmount, float gold, float commerceUpkeep)
	{
		return new ImportExportFoodStatus(game, foodAmount, gold, commerceUpkeep);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"fooodAmount" => foodAmount, 
			"gold" => CalcGoldUpkeep(), 
			"commerceUpkeep" => commerceUpkeep, 
			_ => base.GetVar(key, vars, as_value), 
		};
	}

	public virtual float CalcGoldUpkeep()
	{
		return gold;
	}
}
