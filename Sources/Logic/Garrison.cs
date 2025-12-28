using System;
using System.Collections.Generic;

namespace Logic;

public class Garrison : IVars
{
	public List<Unit> units = new List<Unit>();

	public Settlement settlement;

	public Garrison(Settlement settlement)
	{
		this.settlement = settlement;
	}

	public int SlotCount()
	{
		if (settlement == null)
		{
			return 0;
		}
		return (int)settlement.GetRealm().GetStat(Stats.rs_garrison_slots);
	}

	public int MaxSlotCount()
	{
		Realm realm = settlement.GetRealm();
		if (realm == null)
		{
			settlement.Error("MaxSlotCount('garrison_slots'): Castle is missing a realm!");
			return 0;
		}
		if (realm.stats == null)
		{
			settlement.Error("GetStat('garrison_slots): Stats not initialized yet!");
			return 0;
		}
		Stat stat = realm.stats.Find("rs_garrison_slots");
		if (stat != null)
		{
			return (int)stat.def.max_value;
		}
		return 0;
	}

	public int GetManPower()
	{
		int num = 0;
		for (int i = 0; i < units.Count; i++)
		{
			Unit unit = units[i];
			if (unit != null)
			{
				int num2 = unit.manpower_alive_modified();
				num += num2;
			}
		}
		return num;
	}

	public void GetManpower(out int cur, out int max)
	{
		using (new Stat.ForceCached("Garrison.GetManpower"))
		{
			int num = 0;
			int num2 = 0;
			bool flag = settlement.battle != null;
			for (int i = 0; i < units.Count; i++)
			{
				Unit unit = units[i];
				if (unit != null && (!flag || !unit.IsDefeated()))
				{
					int num3 = unit.max_size_modified();
					num2 += (int)((float)num3 * unit.def.manpower_mul);
					num = ((!flag) ? (num + (int)((float)num3 * (1f - unit.damage) * unit.def.manpower_mul)) : (num + unit.num_alive()));
				}
			}
			if (flag)
			{
				cur = num;
				max = num2;
				return;
			}
			int manpowerForMilitiaDefenders = GetManpowerForMilitiaDefenders(this, (Unit.Def u, Garrison g) => u.GetMaxManPower(null, add_size: true, add_levies: true, add_stats: true, add_additional: true, add_levy_realm_defender: true, add_levy_excess_defender: true, add_town_guard_excess_defender: true, add_worker_excess_defender: true, add_siege_defense_defender: true, add_siege_defense_temporary_defender: true, add_rs_garrison_bonus: true, g));
			int manpowerForTownGuardDefenders = GetManpowerForTownGuardDefenders(this, (Unit.Def u, Garrison g) => u.GetMaxManPower(null, add_size: true, add_levies: true, add_stats: true, add_additional: true, add_levy_realm_defender: true, add_levy_excess_defender: true, add_town_guard_excess_defender: true, add_worker_excess_defender: true, add_siege_defense_defender: true, add_siege_defense_temporary_defender: true, add_rs_garrison_bonus: true, g));
			int manpowerForLevySquads = GetManpowerForLevySquads(this, (Unit.Def u, Garrison g) => u.GetMaxManPower(null, add_size: true, add_levies: true, add_stats: true, add_additional: true, add_levy_realm_defender: true, add_levy_excess_defender: true, add_town_guard_excess_defender: true, add_worker_excess_defender: true, add_siege_defense_defender: true, add_siege_defense_temporary_defender: true, add_rs_garrison_bonus: true, g));
			cur = num + manpowerForMilitiaDefenders + manpowerForTownGuardDefenders + manpowerForLevySquads;
			max = num2 + manpowerForMilitiaDefenders + manpowerForTownGuardDefenders + manpowerForLevySquads;
		}
	}

	public int CheckHire(Unit.Def unitDef, bool check_cost = true, bool check_max_slots = true)
	{
		if (!(settlement is Castle castle))
		{
			return 5;
		}
		if (castle.GetKingdom() == null)
		{
			return 5;
		}
		if (castle.battle != null)
		{
			return 4;
		}
		if (unitDef.buildPrerqusite != null && !unitDef.buildPrerqusite.Validate(castle))
		{
			return 3;
		}
		if (check_max_slots && units.Count >= SlotCount())
		{
			return 2;
		}
		if (check_cost && !castle.CheckUnitCost(unitDef))
		{
			return 1;
		}
		return 0;
	}

	public bool Hire(Unit.Def unitDef)
	{
		if (!(settlement is Castle castle))
		{
			return false;
		}
		if (CheckHire(unitDef, check_cost: false) != 0)
		{
			return false;
		}
		Resource unitCost = castle.GetUnitCost(unitDef, castle.army);
		if (!castle.CheckUnitCost(unitDef, unitCost))
		{
			return false;
		}
		if (!castle.IsAuthority())
		{
			castle.SendEvent(new Castle.HireGarrisonUnitEvent(unitDef));
			return true;
		}
		Unit unit = AddUnit(unitDef);
		if (unit == null)
		{
			return false;
		}
		unitDef.OnHiredAnalytics(null, this, unitCost, "garrison");
		Realm realm = castle.GetRealm();
		int num = (int)realm.GetStat(Stats.rs_initial_troops_level);
		if (unit.def.type == Unit.Type.Militia && realm.HasTag("peasant_start_bonus"))
		{
			num++;
		}
		unit.AddExperience(unit.def.experience_to_next.GetFloat(num));
		unit.AddExperience(realm.GetStat(Stats.rs_initial_troop_experience));
		if (unitCost != null)
		{
			int count = Math.Min((int)unitCost[ResourceType.Workers], castle.population.GetWorkers());
			castle.population.RemoveVillagers(count, Population.Type.Worker);
			castle.AddFood(0f - unitCost[ResourceType.Food]);
			Kingdom kingdom = castle.GetKingdom();
			kingdom.SubResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, unitCost[ResourceType.Gold], send_state: false);
			kingdom.SubResources(KingdomAI.Expense.Category.Military, ResourceType.Levy, unitCost[ResourceType.Levy], send_state: false);
			kingdom.SendState<Kingdom.ResourcesState>();
		}
		if (castle.IsAuthority())
		{
			int num2 = unit.Index();
			if (num2 != -1)
			{
				settlement?.SendSubstate<Settlement.GarrisonUnitsState.UnitState>(num2);
			}
		}
		return true;
	}

	public Unit AddUnit(string def_id, bool mercenary = false, bool send_state = true)
	{
		if (settlement == null)
		{
			return null;
		}
		Unit.Def def = settlement.game.defs.Get<Unit.Def>(def_id);
		return AddUnit(def, mercenary, send_state);
	}

	public Unit AddUnit(Unit.Def def, bool mercenary = false, bool send_state = true, bool check_slots = true)
	{
		if (def == null)
		{
			return null;
		}
		Unit unit = new Unit();
		unit.def = def;
		unit.salvo_def = settlement.game.defs.Get<SalvoData.Def>(def.salvo_def);
		unit.mercenary = mercenary;
		AddUnit(unit, send_state, check_slots);
		return unit;
	}

	public bool AddUnit(Unit unit, bool send_state = true, bool check_slots = true, int slot_index = -1)
	{
		if (settlement == null)
		{
			return false;
		}
		if (units.Count >= SlotCount() && check_slots)
		{
			return false;
		}
		unit.SetGarrison(this);
		if (slot_index >= 0 && slot_index < units.Count)
		{
			units.Insert(slot_index, unit);
		}
		else
		{
			units.Add(unit);
		}
		settlement?.GetRealm()?.InvalidateIncomes();
		settlement?.GetRealm()?.rebellionRisk.Recalc();
		if (send_state)
		{
			settlement.SendState<Settlement.GarrisonUnitsState>();
		}
		settlement.NotifyListeners("garisson_changed");
		return true;
	}

	public bool CanMergeUnits(Unit source, Unit target)
	{
		if (source == null || target == null)
		{
			return false;
		}
		if (source == target)
		{
			return false;
		}
		if (source.def != target.def)
		{
			return false;
		}
		if (source.def.type == Unit.Type.Noble)
		{
			return false;
		}
		if (target.def.type == Unit.Type.Noble)
		{
			return false;
		}
		if (source.garrison == null && source.garrison == null)
		{
			return false;
		}
		if (source.garrison.IsDefeated() || target.garrison.IsDefeated())
		{
			return false;
		}
		if (source.damage <= 0f || target.damage <= 0f)
		{
			return false;
		}
		return true;
	}

	public void MergeUnits(Unit source, Unit target, bool send_state)
	{
		if (!CanMergeUnits(source, target))
		{
			return;
		}
		if (!settlement.IsAuthority() && send_state)
		{
			MapObject target_owner = target.garrison?.settlement;
			settlement.SendEvent(new Castle.MergeUnitsEvent(source.Index(), target.Index(), target_owner));
			return;
		}
		float num = source.damage + target.damage;
		float num2 = Math.Min(2f - num, 1f);
		target.damage = 1f - num2;
		source.damage = num - target.damage;
		int num3 = (int)((float)source.max_manpower_modified() * source.health);
		if (source.damage >= 1f || num3 == 0)
		{
			if (source.experience > target.experience)
			{
				target.AddExperience(source.experience - target.experience);
			}
			DelUnit(source, send_state: false);
		}
		if (settlement.IsAuthority())
		{
			if (send_state)
			{
				settlement.SendState<Settlement.GarrisonUnitsState>();
			}
			target.OnAssignedAnalytics(null, this, "army", "merge");
		}
		settlement.NotifyListeners("units_changed");
		settlement.GetRealm()?.rebellionRisk.Recalc();
	}

	public bool TransferUnit(MapObject dest, Unit unit, Unit swap_target)
	{
		if (unit == null)
		{
			return false;
		}
		if (dest == null)
		{
			return false;
		}
		if (unit.def.type == Unit.Type.Noble)
		{
			return false;
		}
		if (settlement.battle != null)
		{
			return false;
		}
		if (!settlement.IsAuthority())
		{
			int swap_unit_idx = -1;
			if (dest is Army)
			{
				swap_unit_idx = (dest as Army).units.IndexOf(swap_target);
			}
			else if (dest is Castle)
			{
				swap_unit_idx = (dest as Castle).garrison.units.IndexOf(swap_target);
			}
			settlement.SendEvent(new Castle.TransferUnitEvent(dest, units.IndexOf(unit), swap_unit_idx));
			return false;
		}
		if (dest != null && dest is Castle { garrison: var garrison })
		{
			if (garrison == null)
			{
				return false;
			}
			int num = units.IndexOf(unit);
			int num2 = ((swap_target != null) ? garrison.units.IndexOf(swap_target) : (-1));
			if (garrison == this && num >= 0 && num2 >= 0)
			{
				SwapUnits(num, num2);
				return true;
			}
			return false;
		}
		Game.Log($"Transferring units to a {dest?.GetType()} is not supported!", Game.LogType.Error);
		return false;
	}

	public void SwapUnits(int idx1, int idx2, bool send_state = true)
	{
		Unit unit = units[idx1];
		Unit unit2 = units[idx2];
		if ((unit?.def == null || unit.def.type != Unit.Type.Noble) && (unit2?.def == null || unit2.def.type != Unit.Type.Noble))
		{
			units[idx1] = unit2;
			units[idx2] = unit;
			if (send_state)
			{
				settlement.SendState<Army.UnitsState>();
			}
			if (settlement.started && send_state)
			{
				settlement.NotifyListeners("units_changed");
			}
		}
	}

	public void DelUnit(Unit unit, bool send_state = true)
	{
		if (!settlement.IsAuthority() && send_state)
		{
			int idx = units.IndexOf(unit);
			settlement.SendEvent(new Castle.DelGarrisonUnitEvent(idx));
		}
		else if (units.Remove(unit))
		{
			settlement?.GetRealm()?.InvalidateIncomes();
			if (send_state)
			{
				settlement.SendState<Settlement.GarrisonUnitsState>();
			}
			settlement.NotifyListeners("garisson_changed");
		}
	}

	public void DelUnit(int idx, bool send_state = true)
	{
		bool flag = idx >= 0 && idx < units.Count;
		if (!settlement.IsAuthority() && send_state)
		{
			settlement.SendEvent(new Castle.DelGarrisonUnitEvent(idx));
		}
		else if (flag)
		{
			units.RemoveAt(idx);
			if (send_state)
			{
				settlement.SendState<Settlement.GarrisonUnitsState>();
			}
			settlement.NotifyListeners("garisson_changed");
		}
	}

	public void RestUnit(Unit unit, bool send_state = true)
	{
		if (unit != null && units.Contains(unit))
		{
			unit.SetDamage(0f, send_state: false);
			if (send_state)
			{
				settlement.SendState<Settlement.GarrisonUnitsState>();
			}
			settlement.NotifyListeners("garisson_changed");
		}
	}

	public void RestUnits(bool send_state = true)
	{
		for (int i = 0; i < units.Count; i++)
		{
			units[i].SetDamage(0f, send_state: false);
		}
		if (send_state)
		{
			settlement.SendState<Settlement.GarrisonUnitsState>();
		}
		settlement.NotifyListeners("garisson_changed");
	}

	public Unit GetUnit(int idx)
	{
		if (idx < 0 || idx >= units.Count)
		{
			return null;
		}
		return units[idx];
	}

	public bool CanReplenish()
	{
		if (units == null)
		{
			return false;
		}
		if (units.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < units.Count; i++)
		{
			if (units[i].damage > 0f)
			{
				return true;
			}
		}
		return false;
	}

	public int GetManpowerForSquads(Func<Unit, int> manpower_func, int from = 0, int to = int.MaxValue)
	{
		to = Math.Min(to, units.Count);
		int num = 0;
		for (int i = from; i < to; i++)
		{
			Unit unit = units[i];
			if (unit != null)
			{
				num += manpower_func(unit);
			}
		}
		return num;
	}

	public float GetStatForSquads(Func<Unit, float> manpower_func, int from = 0, int to = int.MaxValue)
	{
		to = Math.Min(to, units.Count);
		float num = 0f;
		for (int i = from; i < to; i++)
		{
			Unit unit = units[i];
			if (unit != null)
			{
				num += manpower_func(unit);
			}
		}
		return num;
	}

	public int GetManpowerForMilitiaDefenders(Func<Unit.Def, int> manpower_func)
	{
		Unit.Def def = MilitiaDef();
		if (def == null)
		{
			return 0;
		}
		return manpower_func(def) * settlement.worker_squads;
	}

	public float GetStatForMilitiaDefenders(Func<Unit.Def, float> manpower_func)
	{
		Unit.Def def = MilitiaDef();
		if (def == null)
		{
			return 0f;
		}
		return manpower_func(def) * (float)settlement.worker_squads;
	}

	public int GetManpowerForMilitiaDefenders(Garrison garrison, Func<Unit.Def, Garrison, int> manpower_func)
	{
		Unit.Def def = MilitiaDef();
		if (def == null)
		{
			return 0;
		}
		return manpower_func(def, garrison) * settlement.worker_squads;
	}

	public float GetStatForMilitiaDefenders(Garrison garrison, Func<Unit.Def, Garrison, float> manpower_func)
	{
		Unit.Def def = MilitiaDef();
		if (def == null)
		{
			return 0f;
		}
		return manpower_func(def, garrison) * (float)settlement.worker_squads;
	}

	public int GetManpowerForTownGuardDefenders(Func<Unit.Def, int> manpower_func)
	{
		Unit.Def def = GuardDef();
		if (def == null)
		{
			return 0;
		}
		return manpower_func(def) * settlement.town_guard_squads;
	}

	public float GetStatForTownGuardDefenders(Func<Unit.Def, float> manpower_func)
	{
		Unit.Def def = GuardDef();
		if (def == null)
		{
			return 0f;
		}
		return manpower_func(def) * (float)settlement.town_guard_squads;
	}

	public int GetManpowerForTownGuardDefenders(Garrison garrison, Func<Unit.Def, Garrison, int> manpower_func)
	{
		Unit.Def def = GuardDef();
		if (def == null)
		{
			return 0;
		}
		return manpower_func(def, garrison) * settlement.town_guard_squads;
	}

	public float GetStatForTownGuardDefenders(Garrison garrison, Func<Unit.Def, Garrison, float> manpower_func)
	{
		Unit.Def def = GuardDef();
		if (def == null)
		{
			return 0f;
		}
		return manpower_func(def, garrison) * (float)settlement.town_guard_squads;
	}

	public int GetManpowerForLevySquads(Func<Unit.Def, int> manpower_func)
	{
		Unit.Def def = LevyInfantryDef();
		Unit.Def def2 = LevyDefenseDef();
		Unit.Def def3 = LevyRangedDef();
		if (def == null && def2 == null && def3 == null)
		{
			return 0;
		}
		Battle.Def def4 = settlement.game.defs.GetBase<Battle.Def>();
		int num = settlement.levy_squads;
		int num2 = 0;
		while (num > 0)
		{
			if (def2 != null)
			{
				for (int i = 0; i < def4.defense_levy; i++)
				{
					num2 += manpower_func(def2);
					num--;
					if (num == 0)
					{
						return num2;
					}
				}
			}
			if (def != null)
			{
				for (int j = 0; j < def4.infantry_levy; j++)
				{
					num2 += manpower_func(def);
					num--;
					if (num == 0)
					{
						return num2;
					}
				}
			}
			if (def3 == null)
			{
				continue;
			}
			for (int k = 0; k < def4.ranged_levy; k++)
			{
				num2 += manpower_func(def3);
				num--;
				if (num == 0)
				{
					return num2;
				}
			}
		}
		return num2;
	}

	public float GetStatForLevySquads(Func<Unit.Def, float> manpower_func)
	{
		Unit.Def def = LevyInfantryDef();
		Unit.Def def2 = LevyDefenseDef();
		Unit.Def def3 = LevyRangedDef();
		if (def == null && def2 == null && def3 == null)
		{
			return 0f;
		}
		Battle.Def def4 = settlement.game.defs.GetBase<Battle.Def>();
		int num = settlement.levy_squads;
		float num2 = 0f;
		while (num > 0)
		{
			if (def2 != null)
			{
				for (int i = 0; i < def4.defense_levy; i++)
				{
					num2 += manpower_func(def2);
					num--;
					if (num == 0)
					{
						return num2;
					}
				}
			}
			if (def != null)
			{
				for (int j = 0; j < def4.infantry_levy; j++)
				{
					num2 += manpower_func(def);
					num--;
					if (num == 0)
					{
						return num2;
					}
				}
			}
			if (def3 == null)
			{
				continue;
			}
			for (int k = 0; k < def4.ranged_levy; k++)
			{
				num2 += manpower_func(def3);
				num--;
				if (num == 0)
				{
					return num2;
				}
			}
		}
		return num2;
	}

	public int GetManpowerForLevySquads(Garrison garrison, Func<Unit.Def, Garrison, int> manpower_func)
	{
		Unit.Def def = LevyInfantryDef();
		Unit.Def def2 = LevyDefenseDef();
		Unit.Def def3 = LevyRangedDef();
		if (def == null && def2 == null && def3 == null)
		{
			return 0;
		}
		Battle.Def def4 = settlement.game.defs.GetBase<Battle.Def>();
		int num = settlement.levy_squads;
		int num2 = 0;
		while (num > 0)
		{
			if (def2 != null)
			{
				for (int i = 0; i < def4.defense_levy; i++)
				{
					num2 += manpower_func(def2, garrison);
					num--;
					if (num == 0)
					{
						return num2;
					}
				}
			}
			if (def != null)
			{
				for (int j = 0; j < def4.infantry_levy; j++)
				{
					num2 += manpower_func(def, garrison);
					num--;
					if (num == 0)
					{
						return num2;
					}
				}
			}
			if (def3 == null)
			{
				continue;
			}
			for (int k = 0; k < def4.ranged_levy; k++)
			{
				num2 += manpower_func(def3, garrison);
				num--;
				if (num == 0)
				{
					return num2;
				}
			}
		}
		return num2;
	}

	public float GetStatForLevySquads(Garrison garrison, Func<Unit.Def, Garrison, float> manpower_func)
	{
		Unit.Def def = LevyInfantryDef();
		Unit.Def def2 = LevyDefenseDef();
		Unit.Def def3 = LevyRangedDef();
		if (def == null && def2 == null && def3 == null)
		{
			return 0f;
		}
		Battle.Def def4 = settlement.game.defs.GetBase<Battle.Def>();
		int num = settlement.levy_squads;
		float num2 = 0f;
		while (num > 0)
		{
			if (def2 != null)
			{
				for (int i = 0; i < def4.defense_levy; i++)
				{
					num2 += manpower_func(def2, garrison);
					num--;
					if (num == 0)
					{
						return num2;
					}
				}
			}
			if (def != null)
			{
				for (int j = 0; j < def4.infantry_levy; j++)
				{
					num2 += manpower_func(def, garrison);
					num--;
					if (num == 0)
					{
						return num2;
					}
				}
			}
			if (def3 == null)
			{
				continue;
			}
			for (int k = 0; k < def4.ranged_levy; k++)
			{
				num2 += manpower_func(def3, garrison);
				num--;
				if (num == 0)
				{
					return num2;
				}
			}
		}
		return num2;
	}

	public bool IsDefeated()
	{
		for (int i = 0; i < units.Count; i++)
		{
			if (!units[i].IsDefeated())
			{
				return false;
			}
		}
		return true;
	}

	public Unit.Def MilitiaDef()
	{
		return (settlement.GetRealm()?.castle?.available_units)?.GetMilitiaDef();
	}

	public Unit.Def GuardDef()
	{
		float stat = settlement.GetRealm().GetStat(Stats.rs_guard_level);
		return settlement.GetRealm().castle.available_units.GetTownGuardDef((int)stat);
	}

	public Unit.Def LevyInfantryDef()
	{
		return ((settlement.GetRealm()?.castle)?.available_units)?.GetLevyDef(Unit.Type.Infantry);
	}

	public Unit.Def LevyDefenseDef()
	{
		return ((settlement.GetRealm()?.castle)?.available_units)?.GetLevyDef(Unit.Type.Defense);
	}

	public Unit.Def LevyRangedDef()
	{
		return ((settlement.GetRealm()?.castle)?.available_units)?.GetLevyDef(Unit.Type.Ranged);
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "settlement":
			return settlement;
		case "manpower":
			return GetManpowerForSquads((Unit u) => u.manpower_alive_modified());
		case "max_manpower":
			return GetManpowerForSquads((Unit u) => u.max_manpower_modified());
		case "garrison_manpower":
		case "manpower_base":
			return GetManpowerForSquads((Unit u) => u.manpower_base_size());
		case "manpower_levies":
			return GetManpowerForSquads((Unit u) => u.manpower_base_levies());
		case "manpower_bonus":
			return GetManpowerForSquads((Unit u) => u.manpower_bonus());
		case "max_manpower_healthy":
			return GetManpowerForSquads((Unit u) => u.num_healthy());
		case "max_manpower_dead":
			return GetManpowerForSquads((Unit u) => u.num_dead());
		case "garrison_squads_amount":
			return (units != null) ? units.Count : 0;
		case "garrison_siege_defense_manpower":
			return GetManpowerForSquads((Unit u) => u.manpower_siege_defense_bonus());
		case "garrison_rs_garrison_manpower":
			return GetManpowerForSquads((Unit u) => u.manpower_rs_garrison_bonus());
		case "garrison_levies_manpower":
			return GetManpowerForSquads((Unit u) => u.manpower_province_levies_for_garrison());
		case "garrison_excess_guards_manpower":
			return GetManpowerForSquads((Unit u) => u.manpower_excess_town_guard_bonus());
		case "garrison_excess_population_manpower":
			return GetManpowerForSquads((Unit u) => u.manpower_excess_worker_bonus());
		case "guard_squads":
			return settlement.town_guard_squads;
		case "town_guards_manpower":
			return GetManpowerForTownGuardDefenders((Unit.Def u) => u.manpower_base_size());
		case "town_guards_siege_defense_manpower":
			return GetManpowerForTownGuardDefenders(this, (Unit.Def u, Garrison g) => u.manpower_siege_defense_bonus(null, g));
		case "town_guards_siege_defense_temp_manpower":
			return GetManpowerForTownGuardDefenders(this, (Unit.Def u, Garrison g) => u.manpower_siege_defense_temp_bonus(null, g));
		case "town_guards_rs_garrison_manpower":
			return GetManpowerForTownGuardDefenders(this, (Unit.Def u, Garrison g) => u.manpower_rs_garrison_bonus(null, g));
		case "town_guards_excess_guards_manpower":
			return GetManpowerForTownGuardDefenders(this, (Unit.Def u, Garrison g) => u.manpower_excess_town_guard_bonus(null, g));
		case "town_guards_excess_population_manpower":
			return GetManpowerForTownGuardDefenders(this, (Unit.Def u, Garrison g) => u.manpower_excess_worker_bonus(null, g));
		case "town_guards_levies_manpower":
			return GetManpowerForTownGuardDefenders(this, (Unit.Def u, Garrison g) => u.manpower_province_levies_for_garrison(null, g));
		case "peasant_squads":
			return settlement.worker_squads;
		case "peasant_defenders_manpower":
			return GetManpowerForMilitiaDefenders((Unit.Def u) => u.manpower_base_size());
		case "peasant_defenders_siege_defense_manpower":
			return GetManpowerForMilitiaDefenders(this, (Unit.Def u, Garrison g) => u.manpower_siege_defense_bonus(null, g));
		case "peasant_defenders_siege_defense_temp_manpower":
			return GetManpowerForMilitiaDefenders(this, (Unit.Def u, Garrison g) => u.manpower_siege_defense_temp_bonus(null, g));
		case "peasant_excess_guards_manpower":
			return GetManpowerForMilitiaDefenders(this, (Unit.Def u, Garrison g) => u.manpower_excess_town_guard_bonus(null, g));
		case "peasant_defenders_rs_garrison_manpower":
			return GetManpowerForMilitiaDefenders(this, (Unit.Def u, Garrison g) => u.manpower_rs_garrison_bonus(null, g));
		case "peasant_excess_population_manpower":
			return GetManpowerForMilitiaDefenders(this, (Unit.Def u, Garrison g) => u.manpower_excess_worker_bonus(null, g));
		case "peasant_excess_levy_manpower":
			return GetManpowerForMilitiaDefenders(this, (Unit.Def u, Garrison g) => u.manpower_excess_levies_bonus(null, g));
		case "peasant_levies_manpower":
			return GetManpowerForMilitiaDefenders(this, (Unit.Def u, Garrison g) => u.manpower_province_levies_for_garrison(null, g));
		case "levy_squads":
			return settlement.levy_squads;
		case "levy_defenders_manpower":
			return GetManpowerForLevySquads((Unit.Def u) => u.manpower_base_size());
		case "levy_defenders_excess_levy_manpower":
			return GetManpowerForLevySquads(this, (Unit.Def u, Garrison g) => u.manpower_excess_levies_bonus(null, g));
		case "levy_defenders_excess_population_manpower":
			return GetManpowerForLevySquads(this, (Unit.Def u, Garrison g) => u.manpower_excess_worker_bonus(null, g));
		case "levy_defenders_levies_manpower":
			return GetManpowerForLevySquads(this, (Unit.Def u, Garrison g) => u.manpower_province_levies_for_garrison(null, g));
		case "levy_defenders_siege_defense_manpower":
			return GetManpowerForLevySquads(this, (Unit.Def u, Garrison g) => u.manpower_siege_defense_bonus(null, g));
		case "levy_defenders_siege_defense_temp_manpower":
			return GetManpowerForLevySquads(this, (Unit.Def u, Garrison g) => u.manpower_siege_defense_temp_bonus(null, g));
		case "levy_defenders_rs_garrison_manpower":
			return GetManpowerForLevySquads(this, (Unit.Def u, Garrison g) => u.manpower_rs_garrison_bonus(null, g));
		default:
			return Value.Unknown;
		}
	}
}
