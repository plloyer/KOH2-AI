using System.Collections.Generic;

namespace Logic;

public class IncomeLocation : IVars
{
	public enum Type
	{
		Unknwown,
		Kingdom,
		Realm,
		Settlement,
		Building
	}

	public class Def
	{
		public DT.Field field;

		public IncomePerResource.Def parent;

		public Type type;

		public List<IncomeModifier.Def> mods = new List<IncomeModifier.Def>();

		public void Load(DT.Field field, IncomePerResource.Def parent, Game game)
		{
			this.field = field;
			this.parent = parent;
			ResolveType(game);
			if (type != Type.Unknwown)
			{
				LoadMods(game);
			}
		}

		private void ResolveType(Game game)
		{
			switch (field.key)
			{
			case "Kingdom":
				type = Type.Kingdom;
				return;
			case "Realm":
				type = Type.Realm;
				return;
			case "Town":
			case "CoastalTown":
			case "CoastalSettlement":
			case "CoastalVillage":
			case "AllSettlements":
			case "ReligiousSettlement":
			case "MonasteryOrMosque":
				type = Type.Settlement;
				return;
			}
			if (game.defs.Find<Settlement.Def>(field.key) != null)
			{
				type = Type.Settlement;
			}
			else if (game.defs.Find<Building.Def>(field.key) != null)
			{
				type = Type.Building;
			}
			else
			{
				type = Type.Unknwown;
			}
		}

		private void LoadMods(Game game)
		{
			List<DT.Field> list = this.field.Children();
			if (list == null)
			{
				return;
			}
			Stats.Def def = game.defs.Find<Stats.Def>("KingdomStats");
			Stats.Def stats = ((this.field.key == "Kingdom") ? def : game.defs.Find<Stats.Def>("RealmStats"));
			for (int i = 0; i < list.Count; i++)
			{
				DT.Field field = list[i];
				if (!string.IsNullOrEmpty(field.key))
				{
					IncomeModifier.Def def2 = new IncomeModifier.Def();
					if (def2.Load(field, this, game, stats, def))
					{
						mods.Add(def2);
					}
				}
			}
		}
	}

	public Def def;

	public IncomePerResource parent;

	public List<IncomeModifier> mods = new List<IncomeModifier>();

	public IncomeValue value;

	public int valid;

	public IncomeLocation()
	{
		value.Clear();
	}

	public static IncomeLocation Create(Def def, IncomePerResource parent)
	{
		if (def == null || parent == null)
		{
			return null;
		}
		IncomeLocation incomeLocation = new IncomeLocation();
		incomeLocation.def = def;
		incomeLocation.parent = parent;
		for (int i = 0; i < def.mods.Count; i++)
		{
			IncomeModifier item = IncomeModifier.Create(def.mods[i], incomeLocation);
			incomeLocation.mods.Add(item);
		}
		return incomeLocation;
	}

	public void ResolveKingdomStats()
	{
		for (int i = 0; i < mods.Count; i++)
		{
			mods[i].ResolveKingdomStats();
		}
	}

	public IncomeModifier FindMod(string key)
	{
		for (int i = 0; i < mods.Count; i++)
		{
			IncomeModifier incomeModifier = mods[i];
			if (incomeModifier.def.field.key == key)
			{
				return incomeModifier;
			}
		}
		return null;
	}

	public void Calc(bool governed)
	{
		valid = 1;
		value.Clear();
		for (int i = 0; i < mods.Count; i++)
		{
			IncomeModifier incomeModifier = mods[i];
			incomeModifier.Calc(governed);
			if (incomeModifier.valid <= 0)
			{
				valid = 0;
			}
			if (incomeModifier.def.flat)
			{
				value.non_perc_value += incomeModifier.value;
			}
			else if (incomeModifier.def.perc)
			{
				value.perc_value += incomeModifier.value;
			}
			else
			{
				value.flat_value += incomeModifier.value;
			}
		}
	}

	public float CalcTotal(string category, float unknow_val = 0f)
	{
		float num = 0f;
		bool flag = false;
		for (int i = 0; i < mods.Count; i++)
		{
			IncomeModifier incomeModifier = mods[i];
			if (incomeModifier.MatchCategory(category))
			{
				flag = true;
				float num2 = ((!incomeModifier.def.perc) ? incomeModifier.value : ((parent.value.base_value + parent.value.flat_value) * incomeModifier.value * 0.01f + parent.value.non_perc_value));
				num += num2;
			}
		}
		if (!flag)
		{
			return unknow_val;
		}
		return num;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		float num = CalcTotal(key, float.NaN);
		if (!float.IsNaN(num))
		{
			return num;
		}
		return Value.Unknown;
	}

	public override string ToString()
	{
		string text = (def.parent.is_upkeep ? "upkeep" : "income");
		return $"[{Incomes.ValidStr(valid)}] [{def.field?.key ?? def.type.ToString()}] {def.parent.rt} {text} in {Object.ToString(parent.obj)}: {value}";
	}

	public string Dump(bool verbose, string prefix, string new_line)
	{
		string text = prefix + ToString();
		new_line += "    ";
		for (int i = 0; i < mods.Count; i++)
		{
			IncomeModifier incomeModifier = mods[i];
			if (verbose || incomeModifier.value != 0f)
			{
				text += incomeModifier.Dump(verbose, new_line, new_line);
			}
		}
		return text;
	}

	public string Dump(bool verbose)
	{
		return Dump(verbose, "", "\n");
	}

	public string DumpAll()
	{
		return Dump(verbose: true, "", "\n");
	}

	public string Dump()
	{
		return Dump(verbose: false, "", "\n");
	}
}
