namespace Logic;

public class Incomes : IVars
{
	public BaseObject obj;

	public bool is_upkeep;

	public IncomePerResource[] per_resource = new IncomePerResource[13];

	public static ResourceType[] calc_order = new ResourceType[9]
	{
		ResourceType.WorkerSlots,
		ResourceType.Trade,
		ResourceType.Piety,
		ResourceType.Books,
		ResourceType.Levy,
		ResourceType.Gold,
		ResourceType.Food,
		ResourceType.Hammers,
		ResourceType.TownGuards
	};

	public IncomePerResource this[ResourceType rt] => per_resource[(int)rt];

	public static void CreateForKingdom(Kingdom k)
	{
		k.incomes = new Incomes();
		k.incomes.obj = k;
		k.upkeeps = new Incomes();
		k.upkeeps.obj = k;
		k.upkeeps.is_upkeep = true;
		Economy.Def def = k?.game?.economy?.def;
		if (def != null)
		{
			for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
			{
				IncomePerResource.Def def2 = def.incomes[(int)resourceType];
				IncomePerResource incomePerResource = IncomePerResource.CreateForKingdom(k, def2);
				k.incomes.per_resource[(int)resourceType] = incomePerResource;
				IncomePerResource.Def def3 = def.upkeeps[(int)resourceType];
				IncomePerResource incomePerResource2 = IncomePerResource.CreateForKingdom(k, def3);
				k.upkeeps.per_resource[(int)resourceType] = incomePerResource2;
			}
			for (int i = 0; i < k.realms.Count; i++)
			{
				CreateForRealm(k.realms[i], k);
			}
		}
	}

	public static void CreateForRealm(Realm r, Kingdom k)
	{
		r.incomes = new Incomes();
		r.incomes.obj = r;
		r.upkeeps = new Incomes();
		r.upkeeps.obj = r;
		r.upkeeps.is_upkeep = true;
		Economy.Def def = r?.game?.economy?.def;
		if (def != null)
		{
			for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
			{
				IncomePerResource.Def def2 = def.incomes[(int)resourceType];
				IncomePerResource incomePerResource = IncomePerResource.CreateForRealm(r, def2, k.incomes[resourceType]);
				r.incomes.per_resource[(int)resourceType] = incomePerResource;
				IncomePerResource.Def def3 = def.upkeeps[(int)resourceType];
				IncomePerResource incomePerResource2 = IncomePerResource.CreateForRealm(r, def3, k.upkeeps[resourceType]);
				r.upkeeps.per_resource[(int)resourceType] = incomePerResource2;
			}
			for (int i = 0; i < r.settlements.Count; i++)
			{
				CreateForSettlement(r.settlements[i], r);
			}
		}
	}

	public static void CreateForSettlement(Settlement s, Realm r)
	{
		s.incomes = new Incomes();
		s.incomes.obj = s;
		Economy.Def def = s?.game?.economy?.def;
		if (def != null && s.IsActiveSettlement())
		{
			for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
			{
				IncomePerResource.Def def2 = def.incomes[(int)resourceType];
				IncomePerResource incomePerResource = IncomePerResource.CreateForSettlement(s, def2, r.incomes[resourceType]);
				s.incomes.per_resource[(int)resourceType] = incomePerResource;
			}
		}
	}

	public static void CreateForBuilding(Building b)
	{
		Realm realm = b.castle.GetRealm();
		Economy.Def def = realm?.game?.economy?.def;
		if (def == null)
		{
			b.incomes = null;
			return;
		}
		b.incomes = new Incomes();
		b.incomes.obj = b;
		bool flag = true;
		for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
		{
			IncomePerResource.Def def2 = def.incomes[(int)resourceType];
			IncomePerResource incomePerResource = IncomePerResource.CreateForBuilding(b, def2, realm.incomes[resourceType]);
			if (incomePerResource != null)
			{
				flag = false;
				b.incomes.per_resource[(int)resourceType] = incomePerResource;
			}
		}
		if (flag)
		{
			b.incomes = null;
		}
	}

	public void Destroy()
	{
		for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
		{
			per_resource[(int)resourceType]?.SetParent(null);
		}
	}

	public void SetKingdom(Kingdom k)
	{
		Incomes parent_incomes = null;
		if (obj is Realm)
		{
			parent_incomes = (is_upkeep ? k.upkeeps : k.incomes);
		}
		for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
		{
			per_resource[(int)resourceType]?.SetKingdom(k, parent_incomes);
		}
	}

	public Resource ToResource(Resource out_res = null)
	{
		if (out_res == null)
		{
			out_res = new Resource();
		}
		for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
		{
			float value = per_resource[(int)resourceType]?.value.untaxed_value ?? 0f;
			out_res[resourceType] = value;
		}
		return out_res;
	}

	public void Invalidate(bool force_full_recalc = false)
	{
		for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
		{
			per_resource[(int)resourceType]?.Invalidate(force_full_recalc);
		}
	}

	public Incomes Calc()
	{
		using (new Stat.ForceCached("Incomes.Calc"))
		{
			if (is_upkeep)
			{
				CalcUpkeep();
			}
			else
			{
				CalcIncome();
			}
		}
		return this;
	}

	public void CalcIncome()
	{
		float governed_tax_mul = 1f;
		float non_governed_tax_mul = 1f;
		float non_governed_tax_mul2 = 1f;
		BaseObject baseObject = this.obj;
		if (baseObject == null)
		{
			goto IL_0058;
		}
		Kingdom kingdom;
		if (!(baseObject is Object obj))
		{
			if (!(baseObject is Building building))
			{
				goto IL_0058;
			}
			kingdom = building.castle?.GetKingdom();
		}
		else
		{
			kingdom = obj.GetKingdom();
		}
		goto IL_005a;
		IL_0058:
		kingdom = null;
		goto IL_005a;
		IL_005a:
		if (kingdom != null)
		{
			float taxMul = kingdom.GetTaxMul();
			governed_tax_mul = 1f;
			non_governed_tax_mul = taxMul;
			Settlement.Def def = kingdom.game.defs.Get<Settlement.Def>("Castle");
			if (def != null)
			{
				non_governed_tax_mul2 = def.no_governor_penalty * 0.01f;
			}
		}
		for (int i = 0; i < calc_order.Length; i++)
		{
			ResourceType resourceType = calc_order[i];
			IncomePerResource incomePerResource = per_resource[(int)resourceType];
			if (incomePerResource != null)
			{
				switch (resourceType)
				{
				case ResourceType.Gold:
					incomePerResource.CalcIncome(governed_tax_mul, non_governed_tax_mul);
					break;
				case ResourceType.Food:
				case ResourceType.Books:
				case ResourceType.Piety:
				case ResourceType.Trade:
				case ResourceType.Levy:
					incomePerResource.CalcIncome(1f, non_governed_tax_mul2);
					break;
				default:
					incomePerResource.CalcIncome();
					break;
				}
			}
		}
	}

	public void CalcUpkeep()
	{
		for (int i = 0; i < calc_order.Length; i++)
		{
			ResourceType resourceType = calc_order[i];
			per_resource[(int)resourceType]?.CalcUpkeep(governed: false);
		}
	}

	public Value GetVar(string key, IVars vars, bool as_value)
	{
		ResourceType type = Resource.GetType(key);
		if (type == ResourceType.None)
		{
			return Value.Unknown;
		}
		IncomePerResource incomePerResource = per_resource[(int)type];
		if (incomePerResource == null)
		{
			return Value.Null;
		}
		if (as_value)
		{
			return incomePerResource.value.taxed_value;
		}
		return new Value(incomePerResource);
	}

	public static string ValidStr(int valid)
	{
		if (valid < 0)
		{
			return "DIRTY";
		}
		return valid switch
		{
			0 => "dirty", 
			1 => "const", 
			2 => "CONST", 
			_ => "ERROR", 
		};
	}

	public override string ToString()
	{
		string text = (is_upkeep ? "Upkeeps" : "Incomes") + " of " + Object.ToString(obj) + ": ";
		bool flag = true;
		for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
		{
			IncomePerResource incomePerResource = per_resource[(int)resourceType];
			if (incomePerResource != null)
			{
				if (!flag)
				{
					text += ", ";
				}
				flag = false;
				text += $"{DT.FloatToStr(incomePerResource.value.untaxed_value, 3)} {resourceType}";
			}
		}
		if (flag)
		{
			text += "none";
		}
		return text;
	}

	public string Dump(bool verbose, string prefix, string new_line)
	{
		string text = prefix + ToString();
		new_line += "    ";
		for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
		{
			IncomePerResource incomePerResource = per_resource[(int)resourceType];
			if (incomePerResource != null && (verbose || !incomePerResource.value.IsZero()))
			{
				text += incomePerResource.Dump(verbose, new_line, new_line);
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
