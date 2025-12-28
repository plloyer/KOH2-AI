using System;
using System.Collections.Generic;

namespace Logic;

public class Settlement : MapObject
{
	[Serialization.State(21)]
	public class TypeState : Serialization.ObjectState
	{
		public string type;

		public static TypeState Create()
		{
			return new TypeState();
		}

		public static bool IsNeeded(Object obj)
		{
			return !(obj is Castle);
		}

		public override bool InitFrom(Object obj)
		{
			Settlement settlement = obj as Settlement;
			type = settlement.type;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(type, "type");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			type = ser.ReadStr("type");
		}

		public override void ApplyTo(Object obj)
		{
			Settlement obj2 = obj as Settlement;
			obj2.SetType(type, send_state: false);
			obj2?.GetRealm()?.castle?.population?.Recalc();
		}
	}

	[Serialization.State(23)]
	public class RazedState : Serialization.ObjectState
	{
		public bool razed;

		public static RazedState Create()
		{
			return new RazedState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Settlement).razed;
		}

		public override bool InitFrom(Object obj)
		{
			Settlement settlement = obj as Settlement;
			razed = settlement.razed;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(razed, "razed");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			razed = ser.ReadBool("razed");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Settlement).SetStateDestroyed(razed, send_state: false);
		}
	}

	[Serialization.State(24)]
	public class BattleState : Serialization.ObjectState
	{
		public NID battle;

		public static BattleState Create()
		{
			return new BattleState();
		}

		public static bool IsNeeded(Object obj)
		{
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Settlement settlement = obj as Settlement;
			battle = settlement.battle;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Battle>(battle, "battle");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			battle = ser.ReadNID<Battle>("battle");
		}

		public override void ApplyTo(Object obj)
		{
			Settlement obj2 = obj as Settlement;
			Battle battle = this.battle.Get<Battle>(obj.game);
			obj2.SetBattle(battle, send_state: false);
		}
	}

	[Serialization.State(25)]
	public class SiegeStatsState : Serialization.ObjectState
	{
		public Data resilience_condition;

		public Data siege_defense_condition;

		public static SiegeStatsState Create()
		{
			return new SiegeStatsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Settlement).keep_effects != null;
		}

		public override bool InitFrom(Object obj)
		{
			Settlement settlement = obj as Settlement;
			resilience_condition = settlement.keep_effects.resilience_condition.CreateData();
			siege_defense_condition = settlement.keep_effects.siege_defense_condition.CreateData();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(resilience_condition, "resilience_condition");
			ser.WriteData(siege_defense_condition, "siege_defense_condition");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			resilience_condition = ser.ReadData("resilience_condition");
			siege_defense_condition = ser.ReadData("siege_defense_condition");
		}

		public override void ApplyTo(Object obj)
		{
			Settlement settlement = obj as Settlement;
			settlement.InitKeepEffects();
			resilience_condition.ApplyTo(settlement.keep_effects.resilience_condition, settlement.game);
			siege_defense_condition.ApplyTo(settlement.keep_effects.siege_defense_condition, settlement.game);
		}
	}

	[Serialization.State(26)]
	public class GarrisonUnitsState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class UnitState : Serialization.ObjectSubstate
		{
			public int formation_row;

			public int formation_col;

			public float damage;

			public int level;

			public float experience;

			public bool mercenary;

			public UnitState()
			{
			}

			public UnitState(int idx, Unit unit)
			{
				substate_index = idx;
				formation_row = unit.battle_row;
				formation_col = unit.battle_col;
				damage = unit.damage;
				level = unit.level;
				experience = unit.experience;
				mercenary = unit.mercenary;
			}

			public static UnitState Create()
			{
				return new UnitState();
			}

			public static bool IsNeeded(Object obj)
			{
				Garrison garrison = (obj as Settlement)?.garrison;
				if (garrison != null)
				{
					return garrison.units.Count > 0;
				}
				return false;
			}

			public override bool InitFrom(Object obj)
			{
				Garrison garrison = (obj as Settlement)?.garrison;
				if (garrison == null || garrison.units == null || garrison.units.Count == 0 || substate_index >= garrison.units.Count)
				{
					return false;
				}
				Unit unit = garrison.units[substate_index];
				formation_row = unit.battle_row;
				formation_col = unit.battle_col;
				damage = unit.damage;
				level = unit.level;
				experience = unit.experience;
				mercenary = unit.mercenary;
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.Write7BitUInt(formation_row + 1, "formation_row");
				ser.Write7BitUInt(formation_col + 1, "formation_col");
				ser.WriteFloat(damage, "damage");
				ser.Write7BitUInt(level, "level");
				ser.WriteFloat(experience, "experience");
				ser.WriteBool(mercenary, "mercenary");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				formation_row = ser.Read7BitUInt("formation_row") - 1;
				formation_col = ser.Read7BitUInt("formation_col") - 1;
				damage = ser.ReadFloat("damage");
				level = ser.Read7BitUInt("level");
				experience = ser.ReadFloat("experience");
				if (Serialization.cur_version >= 12)
				{
					mercenary = ser.ReadBool("mercenary");
				}
			}

			public override void ApplyTo(Object obj)
			{
				Garrison garrison = (obj as Settlement)?.garrison;
				if (garrison == null)
				{
					Game.Log("Error applying unit damage state! " + (obj as Settlement).ToString() + " - no garrison.", Game.LogType.Error);
				}
				else if (substate_index < 0 || substate_index >= garrison.units.Count)
				{
					Game.Log("Error applying unit damage state #" + substate_index + " / " + garrison.units.Count + " to " + garrison.ToString(), Game.LogType.Error);
				}
				else
				{
					Unit unit = garrison.units[substate_index];
					unit.SetDamage(damage, send_state: false);
					unit.battle_row = formation_row;
					unit.battle_col = formation_col;
					unit.experience = experience;
					unit.level = level;
					unit.mercenary = mercenary;
					unit.SetGarrison(garrison);
				}
			}
		}

		private List<string> unit_defs = new List<string>();

		public static GarrisonUnitsState Create()
		{
			return new GarrisonUnitsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Garrison garrison = (obj as Settlement)?.garrison;
			if (garrison != null)
			{
				return garrison.units.Count > 0;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Garrison garrison = (obj as Settlement)?.garrison;
			int count = garrison.units.Count;
			for (int i = 0; i < count; i++)
			{
				Unit unit = garrison.units[i];
				unit_defs.Add(unit.def.dt_def.path);
				AddSubstate(new UnitState(i, unit));
			}
			return count > 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int count = unit_defs.Count;
			ser.Write7BitUInt(count, "unit_count");
			for (int i = 0; i < count; i++)
			{
				string val = unit_defs[i];
				ser.WriteStr(val, "unit_def_id_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("unit_count");
			for (int i = 0; i < num; i++)
			{
				string item = ser.ReadStr("unit_def_id_", i);
				unit_defs.Add(item);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Garrison garrison = (obj as Settlement)?.garrison;
			if (garrison == null)
			{
				return;
			}
			for (int i = 0; i < garrison.units.Count; i++)
			{
				garrison.units[i].army = null;
			}
			garrison.units.Clear();
			int count = unit_defs.Count;
			for (int j = 0; j < count; j++)
			{
				string text = unit_defs[j];
				Unit.Def def = obj.game.defs.Get<Unit.Def>(text);
				if (!def.valid)
				{
					garrison.settlement.Error("Unknown unit def: " + text);
					continue;
				}
				Unit unit = new Unit();
				unit.def = def;
				unit.salvo_def = obj.game.defs.Get<SalvoData.Def>(def.salvo_def);
				garrison.units.Add(unit);
			}
			garrison.settlement?.GetRealm()?.rebellionRisk.Recalc();
			if (garrison.settlement.started)
			{
				garrison.settlement.NotifyListeners("garisson_changed");
			}
		}
	}

	[Serialization.State(27)]
	public class OccupiedState : Serialization.ObjectState
	{
		public NID controller_nid;

		public bool forceOccupation;

		public static OccupiedState Create()
		{
			return new OccupiedState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is Settlement { keep_effects: not null }))
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Settlement settlement))
			{
				return false;
			}
			if (settlement.keep_effects != null)
			{
				controller_nid = settlement.keep_effects.GetController();
				forceOccupation = settlement.IsOccupied();
				return true;
			}
			return false;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(controller_nid, "controller_nid");
			ser.WriteBool(forceOccupation, "is_occupied");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			controller_nid = ser.ReadNID("controller_nid");
			forceOccupation = ser.ReadBool("is_occupied");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Settlement { keep_effects: not null } settlement)
			{
				settlement.keep_effects.SetOccupied(controller_nid.GetObj(obj.game), forceOccupation, send_state: false);
			}
		}
	}

	[Serialization.State(28)]
	public class AttackerGarrisonUnitsState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class UnitState : Serialization.ObjectSubstate
		{
			public int formation_row;

			public int formation_col;

			public float damage;

			public int level;

			public float experience;

			public UnitState()
			{
			}

			public UnitState(int idx, Unit unit)
			{
				substate_index = idx;
				formation_row = unit.battle_row;
				formation_col = unit.battle_col;
				damage = unit.damage;
				level = unit.level;
				experience = unit.experience;
			}

			public static UnitState Create()
			{
				return new UnitState();
			}

			public static bool IsNeeded(Object obj)
			{
				AttackerGarrison attackerGarrison = (obj as Settlement)?.attacker_garrison;
				if (attackerGarrison != null)
				{
					return attackerGarrison.units.Count > 0;
				}
				return false;
			}

			public override bool InitFrom(Object obj)
			{
				AttackerGarrison attackerGarrison = (obj as Settlement)?.attacker_garrison;
				if (attackerGarrison == null || attackerGarrison.units == null || attackerGarrison.units.Count == 0 || substate_index >= attackerGarrison.units.Count)
				{
					return false;
				}
				Unit unit = attackerGarrison.units[substate_index];
				formation_row = unit.battle_row;
				formation_col = unit.battle_col;
				damage = unit.damage;
				level = unit.level;
				experience = unit.experience;
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.Write7BitUInt(formation_row + 1, "formation_row");
				ser.Write7BitUInt(formation_col + 1, "formation_col");
				ser.WriteFloat(damage, "damage");
				ser.Write7BitUInt(level, "level");
				ser.WriteFloat(experience, "experience");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				formation_row = ser.Read7BitUInt("formation_row") - 1;
				formation_col = ser.Read7BitUInt("formation_col") - 1;
				damage = ser.ReadFloat("damage");
				level = ser.Read7BitUInt("level");
				experience = ser.ReadFloat("experience");
			}

			public override void ApplyTo(Object obj)
			{
				AttackerGarrison attackerGarrison = (obj as Settlement)?.attacker_garrison;
				if (attackerGarrison == null)
				{
					Game.Log("Error applying unit damage state! " + (obj as Settlement).ToString() + " - no garrison.", Game.LogType.Error);
				}
				else if (substate_index < 0 || substate_index >= attackerGarrison.units.Count)
				{
					Game.Log("Error applying unit damage state #" + substate_index + " / " + attackerGarrison.units.Count + " to " + attackerGarrison.ToString(), Game.LogType.Error);
				}
				else
				{
					Unit unit = attackerGarrison.units[substate_index];
					unit.SetDamage(damage, send_state: false);
					unit.battle_row = formation_row;
					unit.battle_col = formation_col;
					unit.experience = experience;
					unit.level = level;
					unit.SetGarrison(attackerGarrison);
				}
			}
		}

		private List<string> unit_defs = new List<string>();

		public static AttackerGarrisonUnitsState Create()
		{
			return new AttackerGarrisonUnitsState();
		}

		public static bool IsNeeded(Object obj)
		{
			AttackerGarrison attackerGarrison = (obj as Settlement)?.attacker_garrison;
			if (attackerGarrison != null)
			{
				return attackerGarrison.units.Count > 0;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			AttackerGarrison attackerGarrison = (obj as Settlement)?.attacker_garrison;
			int count = attackerGarrison.units.Count;
			for (int i = 0; i < count; i++)
			{
				Unit unit = attackerGarrison.units[i];
				unit_defs.Add(unit.def.dt_def.path);
				AddSubstate(new UnitState(i, unit));
			}
			return count > 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int count = unit_defs.Count;
			ser.Write7BitUInt(count, "unit_count");
			for (int i = 0; i < count; i++)
			{
				string val = unit_defs[i];
				ser.WriteStr(val, "unit_def_id_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("unit_count");
			for (int i = 0; i < num; i++)
			{
				string item = ser.ReadStr("unit_def_id_", i);
				unit_defs.Add(item);
			}
		}

		public override void ApplyTo(Object obj)
		{
			AttackerGarrison attackerGarrison = (obj as Settlement)?.attacker_garrison;
			if (attackerGarrison == null)
			{
				return;
			}
			for (int i = 0; i < attackerGarrison.units.Count; i++)
			{
				attackerGarrison.units[i].army = null;
			}
			attackerGarrison.units.Clear();
			int count = unit_defs.Count;
			for (int j = 0; j < count; j++)
			{
				string text = unit_defs[j];
				Unit.Def def = obj.game.defs.Get<Unit.Def>(text);
				if (!def.valid)
				{
					attackerGarrison.settlement.Error("Unknown unit def: " + text);
					continue;
				}
				Unit unit = new Unit();
				unit.def = def;
				unit.salvo_def = obj.game.defs.Get<SalvoData.Def>(def.salvo_def);
				attackerGarrison.units.Add(unit);
			}
			attackerGarrison.settlement?.GetRealm()?.rebellionRisk.Recalc();
			if (attackerGarrison.settlement.started)
			{
				attackerGarrison.settlement.NotifyListeners("garisson_changed");
			}
		}
	}

	[Serialization.State(29)]
	public class DefenderBonusCacheState : Serialization.ObjectState
	{
		public int siege_defense_garrison_manpower;

		public int siege_defense_temp_defender_manpower;

		public int levy_manpower;

		public int levy_squads;

		public int excess_levy_manpower;

		public int town_guard_squads;

		public int excess_town_guard_manpower;

		public int worker_squads;

		public int excess_worker_manpower;

		public static DefenderBonusCacheState Create()
		{
			return new DefenderBonusCacheState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is Settlement settlement))
			{
				return false;
			}
			if (settlement.siege_defense_garrison_manpower != 0)
			{
				return true;
			}
			if (settlement.siege_defense_temp_defender_manpower != 0)
			{
				return true;
			}
			if (settlement.levy_manpower != 0)
			{
				return true;
			}
			if (settlement.levy_squads != 0)
			{
				return true;
			}
			if (settlement.excess_levy_manpower != 0)
			{
				return true;
			}
			if (settlement.town_guard_squads != 0)
			{
				return true;
			}
			if (settlement.excess_town_guard_manpower != 0)
			{
				return true;
			}
			if (settlement.worker_squads != 0)
			{
				return true;
			}
			if (settlement.excess_worker_manpower != 0)
			{
				return true;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Settlement settlement))
			{
				return false;
			}
			siege_defense_garrison_manpower = settlement.siege_defense_garrison_manpower;
			siege_defense_temp_defender_manpower = settlement.siege_defense_temp_defender_manpower;
			levy_manpower = settlement.levy_manpower;
			levy_squads = settlement.levy_squads;
			excess_levy_manpower = settlement.excess_levy_manpower;
			town_guard_squads = settlement.town_guard_squads;
			excess_town_guard_manpower = settlement.excess_town_guard_manpower;
			worker_squads = settlement.worker_squads;
			excess_worker_manpower = settlement.excess_worker_manpower;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(siege_defense_garrison_manpower, "siege_defense_garrison_manpower");
			ser.Write7BitUInt(siege_defense_temp_defender_manpower, "siege_defense_temp_defender_manpower");
			ser.Write7BitUInt(levy_manpower, "levy_manpower");
			ser.Write7BitUInt(levy_squads, "levy_squads");
			ser.Write7BitUInt(excess_levy_manpower, "excess_levy_manpower");
			ser.Write7BitUInt(town_guard_squads, "town_guard_squads");
			ser.Write7BitUInt(excess_town_guard_manpower, "excess_town_guard_manpower");
			ser.Write7BitUInt(worker_squads, "worker_squads");
			ser.Write7BitUInt(excess_worker_manpower, "excess_worker_manpower");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			siege_defense_garrison_manpower = ser.Read7BitUInt("siege_defense_garrison_manpower");
			siege_defense_temp_defender_manpower = ser.Read7BitUInt("siege_defense_temp_defender_manpower");
			levy_manpower = ser.Read7BitUInt("levy_manpower");
			levy_squads = ser.Read7BitUInt("levy_squads");
			excess_levy_manpower = ser.Read7BitUInt("excess_levy_manpower");
			town_guard_squads = ser.Read7BitUInt("town_guard_squads");
			excess_town_guard_manpower = ser.Read7BitUInt("excess_town_guard_manpower");
			worker_squads = ser.Read7BitUInt("worker_squads");
			excess_worker_manpower = ser.Read7BitUInt("excess_worker_manpower");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Settlement settlement)
			{
				settlement.siege_defense_garrison_manpower = siege_defense_garrison_manpower;
				settlement.siege_defense_temp_defender_manpower = siege_defense_temp_defender_manpower;
				settlement.levy_manpower = levy_manpower;
				settlement.levy_squads = levy_squads;
				settlement.excess_levy_manpower = excess_levy_manpower;
				settlement.town_guard_squads = town_guard_squads;
				settlement.excess_town_guard_manpower = excess_town_guard_manpower;
				settlement.worker_squads = worker_squads;
				settlement.excess_worker_manpower = excess_worker_manpower;
			}
		}
	}

	public class Def : Logic.Def
	{
		public string name;

		public float radius = 7f;

		public PerLevelValues production;

		public string piety_type;

		public float repair_hammers = 1200f;

		public PerLevelValues upgrade_cost;

		public PerLevelValues attrition;

		public float siege_food_penalty = 2f;

		public float no_governor_penalty = 10f;

		public float population_growth_base = 10f;

		public float population_growth_from_surplus_food = 1f;

		public float population_growth_threshold = 500f;

		public float starting_food_perc = 100f;

		public float trade_center_gold_coastal = 3f;

		public float trade_center_gold_settlement = 1f;

		public float trade_center_gold_castle = 2f;

		public float trade_center_books_castle = 3f;

		public float trade_center_gold_per_good = 1f;

		public float trade_center_gold_per_commerce = 0.5f;

		public string[] enable_features;

		public List<string> default_features;

		public List<string> secoundary_features;

		public List<string> exclusive_features;

		public int max_identical_settlement_per_realm = 4;

		public int max_allowed_features_per_settlement_type = 1;

		public int min_restriction_settlements = 4;

		public int max_restriction_settlements = 8;

		public float settlement_repeating_chance_mul = 1.5f;

		public bool is_active_settlement;

		public static int def_count;

		public int max_temporary_defender_squads = 5;

		public float siege_defense_max_map = 500f;

		public float siege_defense_garrison_bonus = 50f;

		public float siege_defense_temp_defender_bonus;

		public float garrison_levy_manpower_bonus = 0.5f;

		public float garrison_squad_per_levy = 1f;

		public float garrison_levy_excess_manpower_bonus = 5f;

		public float garrison_squad_per_town_guard = 1f;

		public float garrison_town_guard_excess_manpower_bonus = 10f;

		public float garrison_squad_per_worker = 1f;

		public float garrison_worker_excess_manpower_bonus = 1f;

		public float food_copy_mod = 1f;

		public bool take_max_food;

		public int ai_eval_military = -1;

		public int ai_eval_food = -1;

		public int ai_eval_religion = -1;

		public int ai_eval_trade = -1;

		public override bool Load(Game game)
		{
			def_count++;
			DT.Field field = base.field;
			max_temporary_defender_squads = field.GetInt("max_temporary_defender_squads", null, max_temporary_defender_squads);
			siege_defense_max_map = field.GetFloat("siege_defense_max_map", null, siege_defense_max_map);
			siege_defense_garrison_bonus = field.GetFloat("siege_defense_garrison_bonus", null, siege_defense_garrison_bonus);
			siege_defense_temp_defender_bonus = field.GetFloat("siege_defense_temp_defender_bonus", null, siege_defense_temp_defender_bonus);
			garrison_levy_manpower_bonus = field.GetFloat("garrison_levy_manpower_bonus", null, garrison_levy_manpower_bonus);
			garrison_squad_per_levy = field.GetFloat("garrison_squad_per_levy", null, garrison_squad_per_levy);
			garrison_levy_excess_manpower_bonus = field.GetFloat("garrison_levy_excess_manpower_bonus", null, garrison_levy_excess_manpower_bonus);
			garrison_squad_per_town_guard = field.GetFloat("garrison_squad_per_town_guard", null, garrison_squad_per_town_guard);
			garrison_town_guard_excess_manpower_bonus = field.GetFloat("garrison_town_guard_excess_manpower_bonus", null, garrison_town_guard_excess_manpower_bonus);
			garrison_squad_per_worker = field.GetFloat("garrison_squad_per_worker", null, garrison_squad_per_worker);
			garrison_worker_excess_manpower_bonus = field.GetFloat("garrison_worker_excess_manpower_bonus", null, garrison_worker_excess_manpower_bonus);
			food_copy_mod = field.GetFloat("food_copy_mod", null, food_copy_mod);
			take_max_food = field.GetBool("take_max_food", null, take_max_food);
			ai_eval_military = field.GetInt("ai_eval_military", null, ai_eval_military);
			ai_eval_food = field.GetInt("ai_eval_food", null, ai_eval_food);
			ai_eval_religion = field.GetInt("ai_eval_religion", null, ai_eval_religion);
			ai_eval_trade = field.GetInt("ai_eval_trade", null, ai_eval_trade);
			name = field.key;
			radius = field.GetFloat("radius", null, radius);
			production = PerLevelValues.Parse<Resource>(field.FindChild("production"), null, no_null: true);
			piety_type = field.GetString("piety_type", null, null);
			repair_hammers = field.GetFloat("repair_hammers", null, repair_hammers);
			upgrade_cost = PerLevelValues.Parse<Resource>(field.FindChild("upgrade_cost"), null, no_null: true);
			attrition = PerLevelValues.Parse<int>(field.FindChild("attrition"), null, no_null: true);
			siege_food_penalty = field.GetFloat("siege_food_penalty", null, siege_food_penalty);
			no_governor_penalty = field.GetFloat("no_governor_penalty", null, no_governor_penalty);
			population_growth_base = field.GetFloat("population_growth_base", null, population_growth_base);
			population_growth_from_surplus_food = field.GetFloat("population_growth_from_surplus_food", null, population_growth_from_surplus_food);
			population_growth_threshold = field.GetFloat("population_growth_threshold", null, population_growth_threshold);
			starting_food_perc = field.GetFloat("starting_food_perc", null, starting_food_perc);
			trade_center_gold_coastal = field.GetFloat("trade_center_gold_coastal", null, trade_center_gold_coastal);
			trade_center_gold_settlement = field.GetFloat("trade_center_gold_settlement", null, trade_center_gold_settlement);
			trade_center_gold_castle = field.GetFloat("trade_center_gold_castle", null, trade_center_gold_castle);
			trade_center_books_castle = field.GetFloat("trade_center_books_castle", null, trade_center_books_castle);
			trade_center_gold_per_good = field.GetFloat("trade_center_gold_per_good", null, trade_center_gold_per_good);
			trade_center_gold_per_commerce = field.GetFloat("trade_center_gold_per_commerce", null, trade_center_gold_per_commerce);
			max_identical_settlement_per_realm = field.GetInt("max_identical_settlement_per_realm", null, max_identical_settlement_per_realm);
			max_allowed_features_per_settlement_type = field.GetInt("max_allowed_features_per_settlement_type", null, max_allowed_features_per_settlement_type);
			min_restriction_settlements = field.GetInt("min_restriction_settlements", null, min_restriction_settlements);
			max_restriction_settlements = field.GetInt("max_restriction_settlements", null, max_restriction_settlements);
			settlement_repeating_chance_mul = field.GetFloat("settlement_repeating_chance_mul", null, settlement_repeating_chance_mul);
			is_active_settlement = field.GetBool("is_active_settlement", null, is_active_settlement);
			enable_features = null;
			default_features = null;
			exclusive_features = null;
			secoundary_features = null;
			DT.Field field2 = field.FindChild("enable_feature");
			if (field2 != null)
			{
				List<string> list = field2.Keys();
				enable_features = new string[list.Count];
				for (int i = 0; i < list.Count; i++)
				{
					string text = list[i];
					enable_features[i] = text;
					string text2 = field2.FindChild(text).Type();
					if (text2 == "default")
					{
						if (default_features == null)
						{
							default_features = new List<string>();
						}
						default_features.Add(text);
					}
					if (text2 == "exclusive")
					{
						if (exclusive_features == null)
						{
							exclusive_features = new List<string>();
						}
						exclusive_features.Add(text);
					}
					if (text2 == "secondary")
					{
						if (secoundary_features == null)
						{
							secoundary_features = new List<string>();
						}
						secoundary_features.Add(text);
					}
				}
			}
			return true;
		}
	}

	public delegate Settlement CreateSettlement(Game game, Point position, string setType);

	private const int STATES_IDX = 20;

	private const int EVENTS_IDX = 36;

	public Def def;

	public DT.Field field;

	public Garrison garrison;

	public AttackerGarrison attacker_garrison;

	public Incomes incomes;

	private Resource resources;

	private bool resources_valid;

	public int realm_id;

	public string type;

	public bool coastal;

	public bool forest;

	public Resource production_flat = new Resource();

	public Resource production_per_level = new Resource();

	public Resource production_from_buildings = new Resource();

	public Resource production_from_Trade_center = new Resource();

	public KeepEffects keep_effects;

	public Battle battle;

	public float razedPenaltyPerc;

	public int siege_defense_garrison_manpower;

	public int siege_defense_temp_defender_manpower;

	public int levy_manpower;

	public int levy_squads;

	public int excess_levy_manpower;

	public int town_guard_squads;

	public int excess_town_guard_manpower;

	public int worker_squads;

	public int excess_worker_manpower;

	private static Random rndGen = new Random();

	private static List<string> types = null;

	private static List<string> allowedRandomTypes = new List<string>();

	public int level { get; private set; } = 1;

	public bool razed => level <= 0;

	public Settlement(Multiplayer multiplayer)
		: base(multiplayer)
	{
		InitKeepEffects();
	}

	public new static Object Create(Multiplayer multiplayer)
	{
		return new Settlement(multiplayer);
	}

	public Realm GetRealm()
	{
		return game.GetRealm(realm_id);
	}

	public override string TypeToStr()
	{
		return type;
	}

	public Settlement(Game game, Point position, string setType)
		: base(game, position, 0)
	{
		type = setType;
	}

	public static Settlement Create(Game game, Point position, string setType)
	{
		if (setType == "Castle")
		{
			return new Castle(game, position);
		}
		return new Village(game, position, setType);
	}

	public bool MatchType(string type_filter)
	{
		if (type == type_filter)
		{
			return true;
		}
		if (type == "Empty")
		{
			return false;
		}
		for (DT.Field field = def?.field.based_on; field != null; field = field.based_on)
		{
			if (field.key == type_filter)
			{
				return true;
			}
		}
		switch (type_filter)
		{
		case "All":
		case "AllSettlements":
			return !(this is Castle);
		case "Town":
			return this is Castle;
		case "CoastalTown":
			if (this is Castle && GetRealm() != null)
			{
				return GetRealm().HasCostalCastle();
			}
			return false;
		case "CoastalSettlement":
			if (coastal)
			{
				return !(this is Castle);
			}
			return false;
		case "CoastalVillage":
			if (coastal)
			{
				return type == "Village";
			}
			return false;
		case "KingdomReligiousSettlement":
			if (def.piety_type != null)
			{
				return Religion.MatchPietyType(def.piety_type, GetKingdom());
			}
			return false;
		case "MonasteryOrMosque":
			if (!(type == "Monastery"))
			{
				return type == "Mosque";
			}
			return true;
		default:
			return false;
		}
	}

	public void SetType(string type, bool send_state = true)
	{
		if (this.type == type)
		{
			game.path_finding.UpdateEmptySettlementsPathData(this);
			return;
		}
		Realm realm = GetRealm();
		Kingdom kingdom = realm?.GetKingdom();
		if (kingdom != null)
		{
			realm.DeactivateBuildings(temporary: true);
		}
		this.type = type;
		def = game.defs.Get<Def>(type);
		field.base_path = def.field.key;
		field.based_on = def.field;
		if (send_state)
		{
			SendState<TypeState>();
		}
		if (incomes != null)
		{
			incomes.Destroy();
			Incomes.CreateForSettlement(this, realm);
		}
		ResetResources();
		InitKeepEffects();
		if (realm != null)
		{
			realm.RefreshTags();
			realm.castle?.RefreshBuildableDistricts();
			kingdom?.RecalcBuildingStates();
			realm.InvalidateIncomes();
		}
		game.path_finding.UpdateEmptySettlementsPathData(this);
		NotifyListeners("type_changed");
	}

	public virtual void Load(DT.Field field)
	{
		def = game.defs.Get<Def>(type);
		this.field = field;
		this.field.base_path = def.field.key;
		this.field.based_on = def.field;
		int result = 0;
		int.TryParse(field.key, out result);
		if (result == 0)
		{
			Warning("Settlement " + field.key + " has no id");
		}
		else
		{
			SetNid(result);
		}
		realm_id = game.GetNearbyLandRealm(position);
		Realm realm = GetRealm();
		if (realm != null)
		{
			kingdom_id = realm.kingdom_id;
			realm.AddSettlement(this);
		}
		else
		{
			Warning("Settlement has no realm");
		}
		coastal = field.GetBool("coastal");
		forest = field.GetBool("forest");
		PopulateResources();
	}

	public static List<string> GetTypes(DT dt)
	{
		if (types != null)
		{
			return types;
		}
		DT.Def def = dt?.FindDef("Settlement");
		if (def == null || def.defs == null)
		{
			types = new List<string>(2);
			types.Add("Random");
			types.Add("Castle");
			return types;
		}
		types = new List<string>(20);
		types.Add("Random");
		types.Add("Castle");
		for (int i = 0; i < def.defs.Count; i++)
		{
			DT.Def def2 = def.defs[i];
			if (def2.path != "Castle" && !def2.path.Contains("."))
			{
				types.Add(def2.path);
			}
		}
		return types;
	}

	public static void ClearTypesCache()
	{
		types = null;
	}

	public int EvalStrength()
	{
		if (garrison == null)
		{
			return 0;
		}
		int num = 0;
		num += garrison.GetManpowerForSquads((Unit u) => u.manpower_base_size());
		num += garrison.GetManpowerForMilitiaDefenders((Unit.Def u) => u.manpower_base_size());
		num += garrison.GetManpowerForLevySquads((Unit.Def u) => u.manpower_base_size());
		num += garrison.GetManpowerForTownGuardDefenders((Unit.Def u) => u.manpower_base_size());
		float num2 = 0f;
		num2 += garrison.GetStatForSquads((Unit u) => u.siege_strength_modified());
		num2 += garrison.GetStatForMilitiaDefenders((Unit.Def u) => u.siege_strength_modified(battle, use_battle_bonuses: false, 1, 0, null, garrison));
		num2 += garrison.GetStatForLevySquads((Unit.Def u) => u.siege_strength_modified(battle, use_battle_bonuses: false, 1, 0, null, garrison));
		num2 += garrison.GetStatForTownGuardDefenders((Unit.Def u) => u.siege_strength_modified(battle, use_battle_bonuses: false, 1, 0, null, garrison));
		if (num == 0)
		{
			return 0;
		}
		garrison.GetManpower(out var cur, out var max);
		float num3 = (float)cur / (float)num;
		float num4 = (float)cur / (float)max;
		return (int)(num2 * num4 * num3);
	}

	public static string GetRandomType(DT dt, Realm realm = null)
	{
		return GetRandomType(dt.Find("SettlementsRandomizationWeights"), realm);
	}

	public static string GetRandomType(DT.Field randsField, Realm r = null)
	{
		if (randsField == null)
		{
			return "Village";
		}
		int num = randsField.value;
		allowedRandomTypes.Clear();
		if (r != null)
		{
			foreach (Settlement settlement in r.settlements)
			{
				if (allowedRandomTypes.Count == num)
				{
					break;
				}
				if (randsField.FindChild(settlement.type) != null && !allowedRandomTypes.Contains(settlement.type))
				{
					allowedRandomTypes.Add(settlement.type);
				}
			}
		}
		int num3;
		if (allowedRandomTypes.Count == num)
		{
			int num2 = 0;
			for (int i = 0; i < randsField.children.Count; i++)
			{
				string value_str = randsField.children[i].value_str;
				if (allowedRandomTypes.Contains(randsField.children[i].key))
				{
					num2 += (int)DT.ParseFloat(value_str);
				}
			}
			num3 = rndGen.Next(0, num2) + 1;
		}
		else
		{
			num3 = rndGen.Next(0, 100) + 1;
		}
		float num4 = 0f;
		for (int j = 0; j < randsField.children.Count; j++)
		{
			string value_str2 = randsField.children[j].value_str;
			if (allowedRandomTypes.Count != num || allowedRandomTypes.Contains(randsField.children[j].key))
			{
				num4 += DT.ParseFloat(value_str2);
				if ((float)num3 <= num4)
				{
					return ParseType(randsField.dt, randsField.children[j].key);
				}
			}
		}
		return "Village";
	}

	public bool FixReligion()
	{
		if (string.IsNullOrEmpty(def?.piety_type))
		{
			return false;
		}
		string text = Religion.ReligiousSettlementType(game, GetRealm()?.religion);
		if (text == null)
		{
			return false;
		}
		if (text == type)
		{
			return false;
		}
		SetType(text, base.started);
		return true;
	}

	public static string Validate(Game game, DT.Field settlment_def, bool supress_warrings = false)
	{
		if (game == null)
		{
			return "null_game";
		}
		if (string.IsNullOrEmpty(settlment_def.key))
		{
			return "null_key";
		}
		if (settlment_def.GetPoint("position") == Point.Invalid)
		{
			if (!supress_warrings)
			{
				Game.Log("Settlement " + settlment_def.key + " has no position", Game.LogType.Error);
			}
			return "invalid_position";
		}
		string base_path = settlment_def.base_path;
		if (((base_path != "Settlement") ? ParseType(game.dt, base_path) : "Random") == null)
		{
			if (!supress_warrings)
			{
				Game.Log("Settlement " + settlment_def.key + " has unknown type", Game.LogType.Error);
			}
			return "unknown_type";
		}
		return "ok";
	}

	public override IRelationCheck GetStanceObj()
	{
		if (keep_effects != null)
		{
			return keep_effects.GetController();
		}
		if (GetRealm().IsOccupied())
		{
			return this;
		}
		return base.GetStanceObj();
	}

	public override RelationUtils.Stance GetStance(Crusade c)
	{
		if (c == null)
		{
			return RelationUtils.Stance.Peace;
		}
		Realm realm = GetRealm();
		if (!realm.IsOccupied())
		{
			return GetStanceObj().GetStance(c);
		}
		if (c == realm.controller)
		{
			return RelationUtils.Stance.Peace;
		}
		return realm.controller.GetStance(c);
	}

	public override RelationUtils.Stance GetStance(Kingdom k)
	{
		if (k == null)
		{
			return RelationUtils.Stance.Peace;
		}
		Realm realm = GetRealm();
		if (!realm.IsOccupied())
		{
			return GetStanceObj().GetStance(k);
		}
		if (k == realm.controller || k == realm.GetKingdom())
		{
			return RelationUtils.Stance.Peace;
		}
		return realm.controller.GetStance(k);
	}

	public override RelationUtils.Stance GetStance(Rebellion r)
	{
		if (r == null)
		{
			return RelationUtils.Stance.Peace;
		}
		Realm realm = GetRealm();
		if (!realm.IsOccupied())
		{
			return GetStanceObj().GetStance(r);
		}
		return realm.controller.GetStance(r);
	}

	public override RelationUtils.Stance GetStance(Settlement s)
	{
		if (s == null)
		{
			return RelationUtils.Stance.Peace;
		}
		Realm realm = GetRealm();
		if (!realm.IsOccupied())
		{
			return GetStanceObj().GetStance(s);
		}
		return realm.controller.GetStance(s);
	}

	public override float GetRadius()
	{
		if (def == null)
		{
			return 7f;
		}
		return def.radius;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"settlement" => this, 
			"def" => def, 
			"kingdom" => GetKingdom(), 
			"realm" => GetRealm(), 
			"piety_icon" => GetKingdom()?.GetPietyIcon(), 
			"is_castle" => type == "Castle", 
			"battle" => battle, 
			"is_in_battle" => battle != null, 
			_ => base.GetVar(key, vars, as_value), 
		};
	}

	public static string ParseType(DT dt, string stype)
	{
		if (string.IsNullOrEmpty(stype))
		{
			return null;
		}
		if (stype.Equals("Town", StringComparison.OrdinalIgnoreCase))
		{
			return "Castle";
		}
		List<string> list = GetTypes(dt);
		for (int i = 1; i < list.Count; i++)
		{
			string text = list[i];
			if (stype.Equals(text, StringComparison.OrdinalIgnoreCase))
			{
				return text;
			}
		}
		return null;
	}

	public void ResetResources()
	{
		production_flat.Clear();
		production_per_level.Clear();
		PopulateResources();
		GetRealm()?.InvalidateIncomes();
	}

	private void PopulateResources()
	{
		production_flat.Add(def.production.GetResources(level, per_level: false), 1f);
		production_per_level.Add(def.production.per_level.obj_val as Resource, 1f);
		if (!string.IsNullOrEmpty(def.piety_type))
		{
			Kingdom kingdom = GetKingdom();
			Religion.FixPietyType(production_flat, def.piety_type, kingdom);
			Religion.FixPietyType(production_per_level, def.piety_type, kingdom);
		}
	}

	protected void InitKeepEffects()
	{
		if (garrison == null)
		{
			garrison = new Garrison(this);
		}
		if (attacker_garrison == null)
		{
			attacker_garrison = new AttackerGarrison(this);
		}
		if (type == "Keep" || type == "Castle")
		{
			if (keep_effects == null)
			{
				keep_effects = new KeepEffects(this);
			}
		}
		else if (keep_effects != null)
		{
			keep_effects.Destroy();
			keep_effects = null;
		}
	}

	public bool IsActiveSettlement()
	{
		return def?.is_active_settlement ?? false;
	}

	public bool IsOccupied()
	{
		if (keep_effects != null)
		{
			return keep_effects.IsOccupied();
		}
		return false;
	}

	public void InvalidateResources()
	{
		resources_valid = false;
	}

	public Resource GetResources(bool force_recalc = false)
	{
		if (!force_recalc && resources_valid)
		{
			return resources;
		}
		resources_valid = true;
		incomes.Calc();
		if (resources == null)
		{
			resources = new Resource();
		}
		incomes.ToResource(resources);
		resources_valid = true;
		CacheTempDefenderStats();
		return resources;
	}

	public override void SetKingdom(int kid)
	{
		if (kid != kingdom_id)
		{
			keep_effects?.SetOccupied(game.GetKingdom(kid));
			kingdom_id = kid;
			NotifyListeners("kingdom_changed");
		}
	}

	public virtual void SetBattle(Battle battle, bool send_state = true)
	{
		this.battle = battle;
		if (send_state)
		{
			SendState<BattleState>();
		}
		NotifyListeners("battle_changed");
	}

	public virtual void SetLevel(int lvl)
	{
		level = lvl;
		ResetResources();
		NotifyListeners("level_changed");
	}

	public virtual void SetStateDestroyed(bool isRazed, bool send_state = true)
	{
		if (razed != isRazed)
		{
			SetLevel((!isRazed) ? 1 : 0);
			GetRealm()?.InvalidateIncomes();
			NotifyListeners("razed", isRazed);
			if (send_state)
			{
				SendState<RazedState>();
			}
		}
	}

	public Point GetRandomExitPoint(bool try_exit_outside_town = true, bool check_water = false)
	{
		PathData pathData = game.path_finding?.data;
		if (pathData == null)
		{
			return position;
		}
		PPos pPos = pathData.GetRandomExitPoint(position, 20f, check_water);
		if (try_exit_outside_town && pathData.GetNode(pPos).town)
		{
			PPos normalized = (pPos - position).GetNormalized();
			PPos pPos2 = pPos;
			for (int i = 0; i < 20; i++)
			{
				pPos2 += normalized;
				if (pathData.GetNode(pPos).town || !pathData.IsPassable(pPos))
				{
					pPos = pPos2;
					break;
				}
			}
		}
		return pPos;
	}

	public void StartKeepOccupyCheck()
	{
		if (IsAuthority())
		{
			Timer.Start(this, "keep_occupy_check", 900f, restart: true);
		}
	}

	public override void OnTimer(Timer timer)
	{
		string name = timer.name;
		if (name == "keep_occupy_check")
		{
			if (type != "Keep")
			{
				return;
			}
			Realm realm = GetRealm();
			Kingdom kingdom = keep_effects.GetController().GetKingdom();
			bool flag = false;
			for (int i = 0; i < realm.armies.Count; i++)
			{
				if (realm.armies[i].kingdom_id == kingdom.id)
				{
					flag = true;
					break;
				}
			}
			if (!flag && keep_effects != null && keep_effects.SetOccupied(realm.controller))
			{
				Vars vars = new Vars();
				Kingdom kingdom2 = realm.controller.GetKingdom();
				vars.Set("old_kingdom", kingdom);
				vars.Set("new_kingdom", kingdom2);
				vars.Set("goto_target", this);
				FireEvent("keep_auto_control_change", vars, kingdom?.id ?? 0, kingdom2?.id ?? 0);
			}
		}
		else
		{
			base.OnTimer(timer);
		}
	}

	public override void OnInit()
	{
		base.OnInit();
		InitKeepEffects();
	}

	protected override void OnStart()
	{
		base.OnStart();
		InitKeepEffects();
	}

	public override Value GetDumpStateValue()
	{
		return type + " in " + GetRealm()?.name + "," + GetKingdom()?.Name;
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("razed", razed.ToString());
		dump.Append("battle", battle?.ToString());
		if (keep_effects != null)
		{
			dump.Append("resilience_condition", keep_effects?.resilience_condition?.Get());
			dump.Append("siege_defense_condition", keep_effects?.siege_defense_condition?.Get());
			dump.Append("active_defence_recovery_boost", keep_effects?.active_defence_recovery_boost.ToString());
		}
		if (garrison != null && garrison.units != null && garrison.units.Count > 0)
		{
			dump.OpenSection("garrison");
			for (int i = 0; i < garrison.units.Count; i++)
			{
				dump.Append(garrison.units[i]?.def?.field?.key);
			}
			dump.CloseSection("garrison");
		}
		dump.Append("attacker_garrison_slots_count", attacker_garrison?.SlotCount());
	}

	public void CacheTempDefenderStats()
	{
		if (IsAuthority())
		{
			int num = siege_defense_garrison_manpower;
			int num2 = siege_defense_temp_defender_manpower;
			int num3 = levy_manpower;
			int num4 = levy_squads;
			int num5 = excess_levy_manpower;
			int num6 = town_guard_squads;
			int num7 = excess_town_guard_manpower;
			int num8 = worker_squads;
			int num9 = excess_worker_manpower;
			CalcTempDefenderStats(out siege_defense_garrison_manpower, out siege_defense_temp_defender_manpower, out levy_manpower, out levy_squads, out excess_levy_manpower, out town_guard_squads, out excess_town_guard_manpower, out worker_squads, out excess_worker_manpower);
			if (siege_defense_garrison_manpower != num || siege_defense_temp_defender_manpower != num2 || levy_manpower != num3 || levy_squads != num4 || excess_levy_manpower != num5 || town_guard_squads != num6 || excess_town_guard_manpower != num7 || worker_squads != num8 || excess_worker_manpower != num9)
			{
				SendState<DefenderBonusCacheState>();
			}
		}
	}

	public virtual void CalcTempDefenderStats(out int siege_defense_garrison_manpower, out int siege_defense_temp_defender_manpower, out int levy_manpower, out int levy_squads, out int excess_levy, out int town_guard_squads, out int excess_town_guard, out int worker_squads, out int excess_worker)
	{
		Realm realm = GetRealm();
		Resource resource = GetResources();
		int levy = 0;
		int town_guards = 0;
		int workers = 0;
		if (resource != null)
		{
			levy = (int)resource.Get(ResourceType.Levy);
			town_guards = (int)resource.Get(ResourceType.TownGuards);
			workers = (int)resource.Get(ResourceType.WorkerSlots);
		}
		if (realm != null && realm.IsDisorder())
		{
			workers = 0;
		}
		SumTempDefenders(levy, town_guards, workers, out siege_defense_garrison_manpower, out siege_defense_temp_defender_manpower, out levy_manpower, out levy_squads, out excess_levy, out town_guard_squads, out excess_town_guard, out worker_squads, out excess_worker);
	}

	protected void SumTempDefenders(int levy, int town_guards, int workers, out int siege_defense_garrison_manpower, out int siege_defense_temp_defender_manpower, out int levy_manpower, out int levy_squads, out int excess_levy, out int town_guard_squads, out int excess_town_guard, out int worker_squads, out int excess_worker)
	{
		int temp_defenders = 0;
		levy_squads = 0;
		excess_levy = 0;
		town_guard_squads = 0;
		excess_town_guard = 0;
		worker_squads = 0;
		excess_worker = 0;
		siege_defense_garrison_manpower = 0;
		siege_defense_temp_defender_manpower = 0;
		levy_manpower = 0;
		Realm realm = GetRealm();
		if (def != null && realm != null)
		{
			CalcAddTempDefender((int)((float)levy * def.garrison_squad_per_levy), def.garrison_levy_excess_manpower_bonus, ref levy_squads, ref excess_levy, ref temp_defenders);
			CalcAddTempDefender((int)((float)town_guards * def.garrison_squad_per_town_guard), def.garrison_town_guard_excess_manpower_bonus, ref town_guard_squads, ref excess_town_guard, ref temp_defenders);
			CalcAddTempDefender((int)((float)workers * def.garrison_squad_per_worker), def.garrison_worker_excess_manpower_bonus, ref worker_squads, ref excess_worker, ref temp_defenders);
			if (realm.income != null)
			{
				levy_manpower = (int)(realm.income.Get(ResourceType.Levy) * def.garrison_levy_manpower_bonus);
			}
			if (keep_effects != null)
			{
				float num = keep_effects.siege_defense_condition.Get();
				float stat = realm.GetStat(Stats.rs_siege_defense);
				float v = num * stat / 100f;
				siege_defense_garrison_manpower = (int)game.Map(v, 0f, def.siege_defense_max_map, 0f, def.siege_defense_garrison_bonus);
				siege_defense_temp_defender_manpower = (int)game.Map(v, 0f, def.siege_defense_max_map, 0f, def.siege_defense_temp_defender_bonus);
			}
		}
	}

	private void CalcAddTempDefender(int count, float manpower_per_excess, ref int squads, ref int excess, ref int temp_defenders)
	{
		int val = def.max_temporary_defender_squads - temp_defenders;
		squads += Math.Min(count, val);
		excess = (int)((float)(count - squads) * manpower_per_excess);
		temp_defenders += squads;
	}

	public Object GetController()
	{
		if (keep_effects != null)
		{
			return keep_effects.GetController();
		}
		return GetRealm()?.controller ?? null;
	}

	public bool IsAllyOrTeammate(Kingdom obj)
	{
		if (IsAlly(obj))
		{
			return true;
		}
		Kingdom kingdom = GetKingdom();
		Game.Team team = game.teams.Get(kingdom);
		if (team != null)
		{
			return team == game.teams.Get(obj);
		}
		return false;
	}
}
