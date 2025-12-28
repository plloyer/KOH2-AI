using System.Collections.Generic;

namespace Logic;

public class IncomePerResource : IVars
{
	public class Def
	{
		public bool is_upkeep;

		public ResourceType rt;

		public IncomeLocation.Def in_kingdom;

		public IncomeLocation.Def in_realm;

		public List<IncomeLocation.Def> in_settlement = new List<IncomeLocation.Def>();

		public List<IncomeLocation.Def> in_building = new List<IncomeLocation.Def>();

		public void Load(DT.Field field, ResourceType rt, Game game)
		{
			this.rt = rt;
			List<DT.Field> list = field.Children();
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					DT.Field field2 = list[i];
					if (!string.IsNullOrEmpty(field2.key))
					{
						IncomeLocation.Def def = new IncomeLocation.Def();
						def.Load(field2, this, game);
						switch (def.type)
						{
						case IncomeLocation.Type.Kingdom:
							in_kingdom = def;
							break;
						case IncomeLocation.Type.Realm:
							in_realm = def;
							break;
						case IncomeLocation.Type.Settlement:
							in_settlement.Add(def);
							break;
						case IncomeLocation.Type.Building:
							in_building.Add(def);
							break;
						default:
							Game.Log(field2.Path(include_file: true) + ": Unknown income locaction: '" + field2.key + "'", Game.LogType.Error);
							break;
						}
					}
				}
			}
			if (in_kingdom == null)
			{
				in_kingdom = new IncomeLocation.Def();
				in_kingdom.type = IncomeLocation.Type.Kingdom;
				in_kingdom.parent = this;
			}
			if (in_realm == null)
			{
				in_realm = new IncomeLocation.Def();
				in_realm.type = IncomeLocation.Type.Realm;
				in_realm.parent = this;
			}
		}
	}

	public Def def;

	public IVars obj;

	public Stats stats;

	public Stats kingdom_stats;

	public List<IncomeLocation> locations = new List<IncomeLocation>();

	public IncomePerResource parent;

	public List<IncomePerResource> children;

	public IncomeValue value;

	public int valid;

	public static bool disable_const_optimisations;

	public IncomePerResource()
	{
		value.Clear();
	}

	public static IncomePerResource CreateForKingdom(Kingdom k, Def def)
	{
		if (k == null || def == null)
		{
			return null;
		}
		IncomePerResource incomePerResource = new IncomePerResource();
		incomePerResource.def = def;
		incomePerResource.obj = k;
		incomePerResource.stats = (incomePerResource.kingdom_stats = k.stats);
		IncomeLocation incomeLocation = IncomeLocation.Create(def.in_kingdom, incomePerResource);
		if (incomeLocation == null)
		{
			return null;
		}
		incomePerResource.locations.Add(incomeLocation);
		return incomePerResource;
	}

	public static IncomePerResource CreateForRealm(Realm r, Def def, IncomePerResource parent)
	{
		if (r == null || def == null)
		{
			return null;
		}
		IncomePerResource incomePerResource = new IncomePerResource();
		incomePerResource.def = def;
		incomePerResource.obj = r;
		incomePerResource.stats = r.stats;
		incomePerResource.SetParent(parent);
		incomePerResource.kingdom_stats = r.GetKingdom()?.stats;
		IncomeLocation incomeLocation = IncomeLocation.Create(def.in_realm, incomePerResource);
		if (incomeLocation == null)
		{
			return null;
		}
		incomePerResource.locations.Add(incomeLocation);
		return incomePerResource;
	}

	public static IncomePerResource CreateForSettlement(Settlement s, Def def, IncomePerResource parent)
	{
		if (s == null || def == null)
		{
			return null;
		}
		IncomePerResource incomePerResource = new IncomePerResource();
		incomePerResource.def = def;
		incomePerResource.obj = s;
		incomePerResource.SetParent(parent);
		Realm realm = s.GetRealm();
		incomePerResource.stats = realm?.stats;
		incomePerResource.kingdom_stats = realm?.GetKingdom()?.stats;
		for (int i = 0; i < def.in_settlement.Count; i++)
		{
			IncomeLocation.Def def2 = def.in_settlement[i];
			if (s.MatchType(def2.field.key))
			{
				IncomeLocation incomeLocation = IncomeLocation.Create(def2, incomePerResource);
				if (incomeLocation != null)
				{
					incomePerResource.locations.Add(incomeLocation);
				}
			}
		}
		return incomePerResource;
	}

	public static IncomePerResource CreateForBuilding(Building b, Def def, IncomePerResource parent)
	{
		if (b == null || def == null)
		{
			return null;
		}
		IncomePerResource incomePerResource = new IncomePerResource();
		incomePerResource.def = def;
		incomePerResource.obj = b;
		Realm realm = b.castle?.GetRealm();
		incomePerResource.stats = realm?.stats;
		incomePerResource.kingdom_stats = realm?.GetKingdom()?.stats;
		for (int i = 0; i < def.in_building.Count; i++)
		{
			IncomeLocation.Def def2 = def.in_building[i];
			if (!(b.def.id != def2.field.key))
			{
				IncomeLocation incomeLocation = IncomeLocation.Create(def2, incomePerResource);
				if (incomeLocation != null)
				{
					incomePerResource.locations.Add(incomeLocation);
				}
			}
		}
		if (incomePerResource.locations.Count == 0)
		{
			return null;
		}
		incomePerResource.SetParent(parent);
		return incomePerResource;
	}

	public void SetParent(IncomePerResource parent)
	{
		if (this.parent != null)
		{
			this.parent.DelChild(this);
		}
		this.parent = parent;
		parent?.AddChild(this);
	}

	private void AddChild(IncomePerResource child)
	{
		if (children == null)
		{
			children = new List<IncomePerResource>();
		}
		children.Add(child);
	}

	private void DelChild(IncomePerResource child)
	{
		if (children != null)
		{
			children.Remove(child);
		}
	}

	public IncomeLocation FindLocation(string key)
	{
		if (key == null || locations == null)
		{
			return null;
		}
		for (int i = 0; i < locations.Count; i++)
		{
			IncomeLocation incomeLocation = locations[i];
			if (incomeLocation?.def?.field?.key == key)
			{
				return incomeLocation;
			}
		}
		return null;
	}

	public void SetKingdom(Kingdom k, Incomes parent_incomes)
	{
		if (parent_incomes != null)
		{
			SetParent(parent_incomes[def.rt]);
		}
		kingdom_stats = k?.stats;
		for (int i = 0; i < locations.Count; i++)
		{
			locations[i].ResolveKingdomStats();
		}
		if (children != null)
		{
			for (int j = 0; j < children.Count; j++)
			{
				children[j].SetKingdom(k, null);
			}
		}
	}

	private static bool IsRealmIncomeBlocked(Realm r, ResourceType rt)
	{
		if (rt == ResourceType.WorkerSlots || rt == ResourceType.Levy || rt == ResourceType.TownGuards)
		{
			return false;
		}
		if (r.IsOccupied())
		{
			return true;
		}
		if (r.IsDisorder())
		{
			return true;
		}
		if (r.castle?.battle != null && (uint)(rt - 1) <= 4u)
		{
			return true;
		}
		return false;
	}

	public void Invalidate(bool force_full_recalc = false)
	{
		valid = (force_full_recalc ? (-1) : 0);
		parent?.Invalidate();
		IVars vars = obj;
		if (vars == null)
		{
			return;
		}
		if (!(vars is Kingdom kingdom))
		{
			if (!(vars is Realm realm))
			{
				if (vars is Settlement settlement)
				{
					settlement.InvalidateResources();
				}
			}
			else
			{
				realm.income_valid = false;
			}
		}
		else
		{
			kingdom.income_valid = false;
		}
	}

	public IncomePerResource CalcIncome(float governed_tax_mul = 1f, float non_governed_tax_mul = 1f)
	{
		if (valid > 0 && !disable_const_optimisations)
		{
			return this;
		}
		bool flag = valid < 0;
		valid = 1;
		value.Clear();
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		if (obj is Settlement settlement)
		{
			if (settlement.level == 0)
			{
				value.base_value = 0f;
				value.untaxed_value = (value.taxed_value = 0f);
				return this;
			}
			flag4 = settlement.GetRealm()?.castle?.governor != null;
			value.base_value = settlement.production_flat[def.rt] + settlement.production_per_level[def.rt] * (float)settlement.level + settlement.production_from_buildings[def.rt];
		}
		else if (obj is Building building)
		{
			if (!building.IsWorking())
			{
				value.base_value = 0f;
				value.untaxed_value = (value.taxed_value = 0f);
				return this;
			}
			flag4 = building.castle?.governor != null;
		}
		else if (obj is Realm realm)
		{
			if (IsRealmIncomeBlocked(realm, def.rt))
			{
				value.base_value = 0f;
				value.untaxed_value = (value.taxed_value = 0f);
				return this;
			}
			flag3 = true;
			flag4 = realm.castle?.governor != null;
		}
		else if (obj is Kingdom)
		{
			flag2 = true;
		}
		if (children != null)
		{
			for (int i = 0; i < children.Count; i++)
			{
				IncomePerResource incomePerResource = children[i];
				if (flag)
				{
					incomePerResource.valid = -1;
				}
				incomePerResource.CalcIncome(governed_tax_mul, non_governed_tax_mul);
				if (incomePerResource.valid <= 0)
				{
					valid = 0;
				}
				if (!(incomePerResource.obj is Settlement settlement2) || !settlement2.IsOccupied() || !(settlement2.type == "Keep"))
				{
					value.base_value += (flag2 ? incomePerResource.value.taxed_value : incomePerResource.value.untaxed_value);
				}
			}
		}
		for (int j = 0; j < locations.Count; j++)
		{
			IncomeLocation incomeLocation = locations[j];
			if (flag)
			{
				incomeLocation.valid = -1;
			}
			incomeLocation.Calc(flag4);
			if (incomeLocation.valid <= 0)
			{
				valid = 0;
			}
			value.Add(incomeLocation.value);
		}
		float tax_mul = (flag2 ? 1f : (flag4 ? governed_tax_mul : non_governed_tax_mul));
		value.Calc(tax_mul, flag3 || flag2, flag3);
		return this;
	}

	public IncomePerResource CalcUpkeep(bool governed)
	{
		if (valid > 0 && !disable_const_optimisations)
		{
			return this;
		}
		bool flag = valid < 0;
		valid = 1;
		value.Clear();
		if (obj is Realm realm)
		{
			governed = realm.castle?.governor != null;
		}
		for (int i = 0; i < locations.Count; i++)
		{
			IncomeLocation incomeLocation = locations[i];
			if (flag)
			{
				incomeLocation.valid = -1;
			}
			incomeLocation.Calc(governed);
			if (incomeLocation.valid <= 0)
			{
				valid = 0;
			}
			value.Add(incomeLocation.value);
		}
		if (children != null)
		{
			for (int j = 0; j < children.Count; j++)
			{
				IncomePerResource incomePerResource = children[j];
				if (flag)
				{
					incomePerResource.valid = -1;
				}
				incomePerResource.CalcUpkeep(governed);
				if (incomePerResource.valid <= 0)
				{
					valid = 0;
				}
				value.base_value += incomePerResource.value.untaxed_value;
			}
		}
		value.Calc(1f);
		return this;
	}

	public float CalcTotal(string category, float unknow_val = 0f)
	{
		Realm realm = obj as Realm;
		Settlement settlement = obj as Settlement;
		bool flag = true;
		float num = 0f;
		bool flag2 = false;
		Realm realm2 = settlement?.GetRealm();
		if ((realm != null && IsRealmIncomeBlocked(realm, def.rt)) || (realm2 != null && IsRealmIncomeBlocked(realm2, def.rt)))
		{
			flag2 = true;
		}
		switch (category)
		{
		case "TOWN":
			if (settlement is Castle)
			{
				if (IsRealmIncomeBlocked(realm2, def.rt))
				{
					return 0f;
				}
				return value.untaxed_value - CalcTotal("GOVERNOR");
			}
			if (realm != null && realm.IsOccupied())
			{
				flag2 = true;
			}
			flag = false;
			break;
		case "SETTLEMENTS":
			if (settlement == null)
			{
				flag = false;
				break;
			}
			if (settlement.IsOccupied())
			{
				return 0f;
			}
			if (settlement is Castle)
			{
				return 0f;
			}
			return value.untaxed_value - CalcTotal("GOVERNOR");
		case "GOVERNOR":
			if (realm != null && realm.castle?.governor == null)
			{
				return 0f;
			}
			break;
		}
		if (children != null)
		{
			for (int i = 0; i < children.Count; i++)
			{
				IncomePerResource incomePerResource = children[i];
				if (!(incomePerResource.obj is Settlement settlement2) || !settlement2.IsOccupied())
				{
					float num2 = incomePerResource.CalcTotal(category, float.NaN);
					if (!float.IsNaN(num2))
					{
						num += num2;
						flag2 = true;
					}
				}
			}
		}
		if (flag)
		{
			for (int j = 0; j < locations.Count; j++)
			{
				float num3 = locations[j].CalcTotal(category, float.NaN);
				if (!float.IsNaN(num3))
				{
					num += num3;
					flag2 = true;
				}
			}
		}
		if (!flag2)
		{
			return unknow_val;
		}
		return num;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "obj":
			return new Value(obj);
		case "kingdom":
			return (obj as Object)?.GetKingdom();
		case "realm":
			return obj as Realm;
		case "TOTAL":
			return value.untaxed_value;
		case "TAXED":
			return value.taxed_value;
		case "BASE":
			return value.base_value;
		case "ADD":
			return value.flat_value;
		case "FLAT":
			return value.base_value + value.flat_value;
		case "PERC":
			return value.perc_value;
		case "PERC_TO_FLAT":
			return (value.base_value + value.flat_value) * value.perc_value * 0.01f;
		case "ROYAL_LANDS":
		{
			if (children == null)
			{
				return 0;
			}
			float num2 = 0f;
			for (int j = 0; j < children.Count; j++)
			{
				IncomePerResource incomePerResource2 = children[j];
				if (incomePerResource2.obj is Realm realm2 && realm2.castle?.governor != null)
				{
					num2 += incomePerResource2.value.untaxed_value;
				}
			}
			return num2;
		}
		case "NON_ROYAL_LANDS":
		{
			if (children == null)
			{
				return 0;
			}
			float num2 = 0f;
			for (int i = 0; i < children.Count; i++)
			{
				IncomePerResource incomePerResource = children[i];
				if (incomePerResource.obj is Realm realm && realm.castle?.governor == null)
				{
					num2 += incomePerResource.value.taxed_value;
				}
			}
			return num2;
		}
		default:
		{
			if (!as_value)
			{
				IncomeLocation incomeLocation = FindLocation(key);
				if (incomeLocation != null)
				{
					return new Value(incomeLocation);
				}
			}
			float num = CalcTotal(key, float.NaN);
			if (!float.IsNaN(num))
			{
				return num;
			}
			return Value.Unknown;
		}
		}
	}

	public override string ToString()
	{
		string text = (def.is_upkeep ? "upkeep" : "income");
		return $"[{Incomes.ValidStr(valid)}] {def.rt} {text} of {Object.ToString(obj)}: {value}";
	}

	public string Dump(bool verbose, string prefix, string new_line)
	{
		string text = prefix + ToString();
		new_line += "    ";
		for (int i = 0; i < locations.Count; i++)
		{
			IncomeLocation incomeLocation = locations[i];
			if (verbose || !incomeLocation.value.IsZero())
			{
				text += incomeLocation.Dump(verbose, new_line, new_line);
			}
		}
		if (children != null)
		{
			for (int j = 0; j < children.Count; j++)
			{
				IncomePerResource incomePerResource = children[j];
				if (verbose || !incomePerResource.value.IsZero())
				{
					text += incomePerResource.Dump(verbose, new_line, new_line);
				}
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
